# Деплой Lexify в production

Целевая конфигурация: домен **lexify-app.com**, VPS HOSTiQ **193.111.62.158**.

Стек на сервере: Caddy (TLS) → nginx/SPA → backend → postgres/redis/piper + бэкапы.
Всё описано в [`docker-compose.prod.yml`](../docker-compose.prod.yml); образы собирает GitHub Actions
и кладёт в ghcr.io, сервер только делает `pull`.

---

## 0. Что уже готово в репозитории

| Есть | Файл |
|---|---|
| Прод-стек (7 сервисов + бэкапы) | `docker-compose.prod.yml` |
| Автоматический Let's Encrypt | `Caddyfile` |
| Шаблон секретов | `.env.example` |
| CI: build → ghcr → SSH deploy | `.github/workflows/deploy.yml` |
| Проверка/восстановление бэкапа | `scripts/restore-db.sh` |
| Prometheus + Grafana (опционально) | `docker-compose.monitoring.yml` |

Делать нужно: подготовить сервер, DNS, секреты, один раз запустить стек, включить CI.

---

## 1. Сервер: важное предупреждение

По письму от хостера на VPS предустановлены **Ubuntu 20 LTS + панель Webuzo**. Оба пункта — проблема:

1. **Ubuntu 20.04 закончил поддержку в апреле 2025** — обновлений безопасности нет.
2. **Webuzo занимает порты 80/443** (свой Apache/nginx/LiteSpeed). Caddy не сможет подняться,
   а Let's Encrypt не выдаст сертификат.

Если переустановка возможна — переустановите ОС на чистую Ubuntu 24.04 LTS без панели (в SolusVM,
`https://vpspanel.twinservers.net:5656` → Reinstall / Rebuild) и идите по основному пути с Caddy.

**Если переустановить нельзя** (панель обязана остаться) — это рабочий сценарий, см.
[Приложение A](#приложение-a--деплой-за-панелью-webuzo). Коротко: публичный вход остаётся за
веб-сервером панели, Caddy не запускается, Docker-стек слушает только `127.0.0.1:8081`.

Минимальные ресурсы: **2 vCPU / 4 GB RAM / 40 GB диска**. Piper (TTS) — самый прожорливый по
диску образ; при 2 GB RAM отключите его (`PIPER_ENABLED=false`).

Фактическая конфигурация этого VPS (SolusVM, узел eukvm24): KVM, **4 GB RAM, 2 GB swap, 50 GB
диска**. Проходит с запасом; узкое место — RAM, см. [A.7](#a7-что-учесть-дальше).

---

## 2. Базовая настройка сервера

Подключение (пароль root из письма хостера):

```bash
ssh root@193.111.62.158
```

### 2.1. Пользователь и SSH-ключи

Пароль root, присланный письмом, считаем скомпрометированным — меняем и переходим на ключи.

```bash
adduser deploy
usermod -aG sudo deploy
```

С локальной машины (Windows, PowerShell):

```powershell
ssh-keygen -t ed25519 -C "lexify-deploy" -f $HOME\.ssh\lexify_deploy
type $HOME\.ssh\lexify_deploy.pub | ssh root@193.111.62.158 "mkdir -p /home/deploy/.ssh && cat >> /home/deploy/.ssh/authorized_keys && chown -R deploy:deploy /home/deploy/.ssh && chmod 700 /home/deploy/.ssh && chmod 600 /home/deploy/.ssh/authorized_keys"
```

Проверьте вход `ssh -i ~/.ssh/lexify_deploy deploy@193.111.62.158` **до** того, как отключите пароли.
Затем в `/etc/ssh/sshd_config`:

```
PermitRootLogin no
PasswordAuthentication no
```

```bash
systemctl restart ssh
```

### 2.1a. Смена порта SSH (опционально)

Не защищает от целевой атаки — nmap найдёт порт за минуту, — но убирает ~99% ботового шума из
логов, после чего в `lastb` становятся видны настоящие попытки. При key-only входе основной риск
всё равно не перебор, а 0-day в OpenSSH, от которого порт не спасает; так что это про читаемость
логов, а не про защиту.

```bash
sudo sed -i 's/^#\?Port .*/Port <новый-порт>/' /etc/ssh/sshd_config
sudo ufw allow <новый-порт>/tcp        # ДО перезапуска sshd
sudo sshd -t && sudo systemctl restart ssh
```

Проверьте вход из **нового окна**, не закрывая текущую сессию, и только потом
`sudo ufw delete allow 22/tcp`.

Порт нужно поменять ещё в четырёх местах, иначе что-нибудь тихо сломается:

| Где | Что |
|---|---|
| GitHub → Environments → production | секрет `DEPLOY_PORT`; без него workflow подставит 22 и деплой упадёт |
| `~/.ssh/config` локально | `Host lexify` c `Port`, чтобы не писать `-p` каждый раз |
| **fail2ban** | в джейле `sshd` по умолчанию `port = ssh` → 22 из `/etc/services`. Бан будет вешаться на 22-й порт, а стучатся в новый — защита фактически отключается. Добавьте в `/etc/fail2ban/jail.local` секцию `[sshd]` с `port = <новый-порт>` и перезапустите сервис |
| ufw | старое правило `22/tcp` снять, новое проверить через `ufw status numbered` |

### 2.2. Обновления, файрвол, fail2ban

```bash
apt update && apt upgrade -y
apt install -y ufw fail2ban curl git

ufw default deny incoming
ufw default allow outgoing
ufw allow 22/tcp    # см. «Смена порта SSH» ниже, если порт уже перенесён
ufw allow 80/tcp
ufw allow 443/tcp
ufw enable

systemctl enable --now fail2ban
```

Порты Postgres/Redis/Grafana наружу **не открываем** — они доступны только внутри docker-сети.

### 2.3. Docker

⚠️ **На Ubuntu 20.04 `curl -fsSL https://get.docker.com | sh` не отрабатывает до конца.** Скрипт
ставит семь пакетов одной командой, среди них `docker-model-plugin`, который под focal не собран;
`apt-get` атомарен, поэтому падает вся установка (`E: Unable to locate package docker-model-plugin`),
и в системе не появляется ни группы `docker`, ни юнита `docker.service`.

Скрипт при этом успевает прописать репозиторий и ключ, так что достаточно доставить пакеты вручную:

```bash
curl -fsSL https://get.docker.com | sh      # добавит репозиторий, установка упадёт — это нормально
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin docker-buildx-plugin
sudo usermod -aG docker deploy
sudo systemctl enable --now docker
docker --version && docker compose version  # нужен compose v2 (плагин), не docker-compose
```

Членство в группе `docker` действует только с нового входа — после `usermod` переподключитесь по SSH.

Проверка, что 80/443 свободны (должно быть пусто):

```bash
ss -tlnp | grep -E ':80|:443'
```

### 2.4. Swap (если RAM ≤ 4 GB)

На этом VPS swap уже выделен хостером (2 GB) — проверьте `swapon --show` и, если вывод непустой,
шаг пропустите. Иначе:

```bash
fallocate -l 2G /swapfile && chmod 600 /swapfile && mkswap /swapfile && swapon /swapfile
echo '/swapfile none swap sw 0 0' >> /etc/fstab
```

---

## 3. DNS для lexify-app.com

### 3.0. Исходное состояние (проверено 19.07.2026)

| Что | Состояние |
|---|---|
| Делегирование | работает |
| NS | `ns1.openprovider.nl`, `ns2.openprovider.be`, `ns3.openprovider.eu` |
| SOA serial | `2026071601` (зона создана 16.07.2026) |
| A `@`, A `www`, MX | отсутствуют — зона пустая |
| Default TTL | 3600 (1 час) |

`ns*.openprovider.*` — дефолтные DNS-серверы регистратора-бэкенда, через который HOSTiQ оформляет
домены. Автоматического направления домена на DNS хостинга (о котором пишет статья HOSTiQ «Где
работать с DNS-записями домена») здесь не произошло — вероятно, домен и VPS заказывались отдельно.

**Редактировать зону Openprovider через панель HOSTiQ нельзя** — DNS-менеджмент у них отдельная
услуга. Значит, неймсерверы всё-таки нужно переключить: на `pdns1/pdns2.hostiq.ua`, а зону вести в
SolusVM, рядом с сервером.

### 3.1. Создать зону в SolusVM

**Сначала зона, потом переключение NS.** Обратный порядок оставляет окно, в котором домен
делегирован на серверы без зоны — резолверы получают SERVFAIL и кэшируют его.

1. SolusVM (`https://vpspanel.twinservers.net:5656`) → верхнее меню **DNS**.
2. Поле *Add New Domain* → `lexify-app.com` (без `www`, без `http://`, без точки в конце) → *Add*.
3. Откройте зону. Создаются только две NS-записи (`Records 2/150`), A-записи **не** проставляются
   автоматически — добавьте их в блоке *A Records* вручную.

⚠️ **В поле *Domain* пишется короткое имя — панель сама дописывает домен.** Ввод полного
`www.lexify-app.com` создаёт запись `www.lexify-app.com.lexify-app.com`, которая молча ни на что не
влияет: ошибки нет, сайт просто не резолвится.

| Domain | IP Address | TTL |
|---|---|---|
| *(пусто, либо `@` — зависит от сборки панели)* | `193.111.62.158` | 3600 |
| `www` | `193.111.62.158` | 3600 |

TTL по умолчанию 14400 (4 часа) — на время настройки ставьте 3600, иначе цена опечатки в IP это
четыре часа ожидания. После запуска можно вернуть.

Проверять результат можно сразу — зона попадает на `pdns*.hostiq.ua` мгновенно, не дожидаясь смены
делегирования:

```powershell
Resolve-DnsName lexify-app.com     -Server pdns1.hostiq.ua
Resolve-DnsName www.lexify-app.com -Server pdns1.hostiq.ua
```

CAA-записи в SolusVM добавить нельзя (есть только NS/A/AAAA/MX/CNAME/TXT/SRV) — раздел 3.4
для этой панели неприменим.

### 3.2. Переключить неймсерверы

Аккаунт HOSTiQ → **Мои домены** → **«Детали»** (круглая иконка в правом конце строки; клик по
названию домена ведёт на сам сайт, а не в карточку) → раздел **«NS серверы»**:

1. Переключатель → **«Указать NS другого сервиса или VPS от HOSTiQ.ua»**.
2. Впишите:
   ```
   pdns1.hostiq.ua
   pdns2.hostiq.ua
   ```
3. *Сохранить изменения*.

Альтернатива, если DNS удобнее держать в аккаунте HOSTiQ, а не в SolusVM: переключатель
«Установить NS для управления DNS с HOSTiQ.ua без хостинга» → кнопка «Управление DNS» → те же две
A-записи. Тогда шаг 3.1 не нужен. Обе схемы равноценны; SolusVM выбран потому, что держит DNS рядом
с сервером и не требует обращения в поддержку.

Смена делегирования расходится обычно за 1–6 часов (формально до 48). Домен новый и почти не
запрашивался, поэтому кэши резолверов холодные — на практике будет ближе к нижней границе.

### 3.3. Проверить

Две отдельные проверки — сначала зона, потом делегирование. Windows, PowerShell:

```powershell
# 1. Зона на новых серверах — отвечает сразу после шага 3.1, не дожидаясь распространения
Resolve-DnsName lexify-app.com     -Server pdns1.hostiq.ua
Resolve-DnsName www.lexify-app.com -Server pdns1.hostiq.ua

# 2. Делегирование переключилось — NS должны смениться на pdns1/pdns2.hostiq.ua
Resolve-DnsName lexify-app.com -Type NS -Server 8.8.8.8

# 3. Итог: публичный резолвер отдаёт нужный IP
Resolve-DnsName lexify-app.com -Server 8.8.8.8
```

Linux/macOS: `dig +short lexify-app.com @pdns1.hostiq.ua`.

Проверка 1 — сигнал, что зона заполнена правильно; она проходит мгновенно. Проверки 2 и 3 требуют
распространения. Если 1 отвечает, а 3 нет — просто ждите, ошибки нет.

**Не переходите к выпуску сертификата, пока `8.8.8.8` не отдаёт правильный IP.** Let's Encrypt
проверяет владение доменом через публичный DNS: пока запись не разошлась, выпуск будет падать, а у
LE есть лимит — 5 неудачных попыток на домен в час.

### 3.4. CAA (опционально, но полезно)

Сейчас CAA-записей нет, то есть сертификат может выпустить любой центр сертификации. Если хотите
ограничить — добавьте запись, **обязательно** включив в неё Let's Encrypt, иначе выпуск перестанет
работать:

| Тип | Имя | Значение |
|---|---|---|
| CAA | `@` | `0 issue "letsencrypt.org"` |

> `Caddyfile` обслуживает только `{$DOMAIN}`. Если пойдёте по пути с Caddy и захотите, чтобы
> `www.lexify-app.com` тоже открывался, замените первую строку блока на
> `lexify-app.com, www.lexify-app.com {`. В сценарии с Webuzo оба имени добавляются в панели.

---

## 4. Первый деплой (вручную)

### 4.1. Разложить стек

```bash
sudo mkdir -p /opt/lexify && sudo chown deploy:deploy /opt/lexify
cd /opt/lexify
curl -fsSLO https://raw.githubusercontent.com/MrSampy/Lexify/main/docker-compose.prod.yml
curl -fsSLO https://raw.githubusercontent.com/MrSampy/Lexify/main/Caddyfile
curl -fsSL  https://raw.githubusercontent.com/MrSampy/Lexify/main/.env.example -o .env
```

### 4.2. Сгенерировать секреты

```bash
openssl rand -base64 32   # POSTGRES_PASSWORD
openssl rand -base64 32   # REDIS_PASSWORD
openssl rand -base64 48   # JWT_SECRET  (минимум 32 символа, иначе backend откажется стартовать)
openssl rand -base64 24   # ADMIN_PASSWORD
```

`Program.cs` содержит защиту: в Production при слабом/дефолтном `Jwt__SecretKey` приложение
падает на старте намеренно.

### 4.3. Заполнить `.env`

```bash
chmod 600 .env
nano .env
```

Ключевые значения для вашего домена:

```dotenv
DOMAIN=lexify-app.com
ACME_EMAIL=Sergiy.Kolosov@in-core.com.ua

POSTGRES_PASSWORD=<из openssl>
REDIS_PASSWORD=<из openssl>
DATABASE_URL=Host=postgres;Port=5432;Database=lexify;Username=lexify;Password=<тот же POSTGRES_PASSWORD>
REDIS_URL=redis:6379,password=<тот же REDIS_PASSWORD>

JWT_SECRET=<из openssl>

ADMIN_EMAIL=Sergiy.Kolosov@in-core.com.ua
ADMIN_PASSWORD=<из openssl>

SMTP_FROM_ADDRESS=noreply@lexify-app.com
OLLAMA_API_KEY=<ключ с https://ollama.com>
```

Пароли в `DATABASE_URL`/`REDIS_URL` должны **совпадать** с `POSTGRES_PASSWORD`/`REDIS_PASSWORD` —
это дублирование в шаблоне, самая частая ошибка.

⚠️ Compose требует непустыми: `OLLAMA_API_KEY`, `SMTP_HOST`, `S3_*`, `ACME_EMAIL`.
Пустой `OLLAMA_API_KEY` в `.env.example` — стек не поднимется, пока не подставите свой.

⚠️ **Никогда не делайте `set -a; . ./.env; set +a`.** Выглядит удобным способом достать пароль для
разовой команды — кладёт прод. В bash `;` разделяет команды, поэтому строка
`DATABASE_URL=Host=postgres;Port=5432;...;Password=...` присваивает переменной только
`Host=postgres`, а хвост пытается выполнить как команды. С `set -a` огрызок ещё и экспортируется, а
`docker compose` ставит окружение шелла **выше** файла `.env` — backend получает строку подключения
без пароля и падает с `No password has been provided but the backend requires one
(in SASL/SCRAM-SHA-256)`, а nginx отдаёт `502`. Сам файл при этом цел, и симптом исчезает после
выхода из шелла — что делает диагностику особенно запутанной.

Правильный способ достать одно значение:

```bash
REDIS_PASSWORD=$(grep -E '^REDIS_PASSWORD=' /opt/lexify/.env | cut -d= -f2-)
```

### 4.4. Доступ к образам ghcr.io

Проще всего сделать пакеты публичными: GitHub → репозиторий → *Packages* → `backend` / `frontend` /
`piper` → *Package settings* → *Change visibility* → Public.

Если оставляете приватными — авторизуйтесь на сервере classic-токеном с правом `read:packages`:

```bash
echo "<PAT>" | docker login ghcr.io -u MrSampy --password-stdin
```

### 4.5. Собрать образы и запустить

Один раз запустите workflow `Deploy` (push в `main` или *Run workflow*), чтобы образы появились в
ghcr, затем на сервере:

```bash
cd /opt/lexify
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs -f caddy backend
```

Миграции EF и сидинг (языки, системные настройки, админ) выполняет `DatabaseInitializer` при
старте backend — отдельная команда не нужна.

### 4.6. Проверить

```bash
curl -I https://lexify-app.com            # 200, валидный сертификат
curl  https://lexify-app.com/api/health   # Healthy
```

Откройте `https://lexify-app.com`, войдите админом и **сразу смените пароль** в профиле.

---

## 5. Почта (обязательно до публичного запуска)

Без SMTP молча ломаются сброс пароля, welcome-письма и напоминания о повторении.

1. Заведите аккаунт у провайдера (Resend / Brevo / Mailgun / SendGrid).
2. Верифицируйте домен `lexify-app.com` — провайдер выдаст DKIM/SPF записи, добавьте их в DNS-зону.
3. Добавьте DMARC: TXT `_dmarc` → `v=DMARC1; p=none; rua=mailto:Sergiy.Kolosov@in-core.com.ua`.
4. Заполните в `.env`: `SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_PASSWORD`,
   `SMTP_FROM_ADDRESS=noreply@lexify-app.com`.
5. `docker compose -f docker-compose.prod.yml up -d backend` и проверьте сброс пароля живым письмом.

Отправка с непроверенного домена уедет в спам — шаг 2 не пропускать.

---

## 6. Бэкапы

Стек уже поднимает `postgres-backup` (локальные дампы с ротацией) и `backup-offsite` (выгрузка в
S3-совместимое хранилище). Нужно только создать бакет — дешевле всего Backblaze B2 или Cloudflare R2 —
и заполнить `S3_ENDPOINT`, `S3_BUCKET`, `S3_ACCESS_KEY_ID`, `S3_SECRET_ACCESS_KEY`.

**Проверьте восстановление до появления реальных данных** — непроверенный бэкап бэкапом не является:

```bash
cd /opt/lexify
./scripts/restore-db.sh      # разворачивает свежий дамп во временную БД и печатает счётчики
```

Скрипт лежит в репозитории — скопируйте `scripts/` на сервер или запускайте из клона.

---

## 7. Включить автодеплой (CI/CD)

В GitHub → *Settings* → *Environments* → создайте окружение **production** и добавьте секреты:

| Секрет | Значение |
|---|---|
| `DEPLOY_HOST` | `193.111.62.158` |
| `DEPLOY_USER` | `deploy` |
| `DEPLOY_SSH_KEY` | приватный ключ `~/.ssh/lexify_deploy` целиком |

После этого каждый push в `main`: собирает три образа → пушит в ghcr → копирует
`docker-compose.prod.yml` + `Caddyfile` на сервер → `pull && up -d`.
Файл `.env` workflow не трогает — секреты живут только на сервере.

Рекомендую включить в окружении *Required reviewers*, чтобы прод не выкатывался случайным пушем.

---

## 8. После запуска

### Настройки в админке (`/admin/settings`, передеплой не нужен)

| Ключ | Зачем |
|---|---|
| `features.registration_enabled` | `false` — закрыть открытую регистрацию на время обкатки |
| `features.invite_code` | код для регистрации при закрытой регистрации |
| `ai.max_calls_per_user_per_day` | потолок AI-вызовов на пользователя; защищает ваш общий ключ Ollama от выжигания за сутки |
| `features.tts_enabled` | мягкий выключатель Piper |

### Мониторинг (опционально)

```bash
docker compose -f docker-compose.prod.yml -f docker-compose.monitoring.yml up -d
```

⚠️ `docker-compose.monitoring.yml` публикует порты 9090 (Prometheus) и 3000 (Grafana) на хост.
Docker пишет правила напрямую в iptables и **обходит ufw**. Либо смените маппинг на
`127.0.0.1:3000:3000` и ходите через SSH-туннель, либо не поднимайте мониторинг на публичном IP.
Не забудьте `GRAFANA_PASSWORD` в `.env`.

### Безопасность — что проверено в коде

- `/hangfire` проксируется nginx наружу, но `HangfireAuthFilter` пускает только пользователей с
  ролью `Admin`; браузерная навигация JWT не несёт, так что дашборд фактически закрыт.
- `/metrics` nginx не проксирует — Prometheus ходит по внутренней сети.
- Swagger включается только в Development — в проде не отдаётся.
- Postgres/Redis/Piper/frontend без публикации портов, наружу торчит только Caddy.

### Обслуживание

```bash
# логи
docker compose -f docker-compose.prod.yml logs -f backend

# рестарт одного сервиса
docker compose -f docker-compose.prod.yml restart backend

# обновление вручную (если не через CI)
docker compose -f docker-compose.prod.yml pull && docker compose -f docker-compose.prod.yml up -d

# автообновления безопасности ОС
apt install -y unattended-upgrades && dpkg-reconfigure -plow unattended-upgrades
```

---

## Чек-лист запуска

- [ ] ОС переустановлена на Ubuntu 24.04, Webuzo отсутствует, порты 80/443 свободны
- [ ] SSH по ключу, вход root и пароли отключены, ufw + fail2ban активны
- [ ] `dig +short lexify-app.com` → `193.111.62.158`
- [ ] `.env` заполнен, `chmod 600`, пароли в `DATABASE_URL`/`REDIS_URL` совпадают
- [ ] Образы в ghcr собраны и доступны серверу
- [ ] `https://lexify-app.com/api/health` → Healthy, сертификат валиден
- [ ] Пароль админа сменён после первого входа
- [ ] SMTP настроен, SPF/DKIM/DMARC в DNS, сброс пароля проверен живым письмом
- [ ] S3-бакет создан, `restore-db.sh` отработал успешно
- [ ] Секреты `DEPLOY_*` добавлены в GitHub environment `production`
- [ ] `ai.max_calls_per_user_per_day` выставлен ненулевым

---

## Приложение A — деплой за панелью Webuzo

Для случая, когда ОС переустановить нельзя и Webuzo должен остаться.

**Что такое Webuzo:** панель управления хостингом от Softaculous (аналог cPanel). Ставит свой
LAMP-стек (Apache или nginx + MySQL + PHP), веб-морду на портах 2002/2004, управляет доменами,
почтой и SSL. Её веб-сервер занимает 80/443 — из-за этого Caddy не может подняться.

**Решение:** веб-сервер панели становится единственным публичным входом и делает то, что делал
Caddy, — терминирует TLS и проксирует на приложение. Docker-стек публикует только
`127.0.0.1:8081` (frontend/nginx), остальные сервисы наружу не смотрят вообще.

```
интернет :443 → Apache/nginx Webuzo (TLS, Let's Encrypt из панели)
                     ↓ proxy
              127.0.0.1:8081 → frontend (nginx) → backend:8080
```

Стек к этому уже подготовлен: сервис `caddy` вынесен в профиль `edge` и без `--profile edge` не
стартует, а `frontend` публикует порт на loopback. Обычная команда `docker compose -f
docker-compose.prod.yml up -d` (та же, что выполняет CI) поднимает всё **без** Caddy.

### A.1. Ubuntu 20 без поддержки

Раз ОС остаётся — подключите бесплатный Ubuntu Pro (ESM даёт обновления безопасности для 20.04
до 2030 года, бесплатно до 5 машин; токен на `ubuntu.com/pro`):

```bash
pro attach <ваш-токен>
pro enable esm-infra
apt update && apt upgrade -y
```

### A.2. Диагностика: кто держит порты

```bash
ss -tlnp | grep -E ":80 |:443 "
systemctl list-units --type=service --state=running | grep -Ei "apache|nginx|litespeed|httpd"
```

Webuzo обычно ставит стек в `/usr/local/apps/` — там будет `apache` **или** `nginx`. От этого
зависит выбор конфига в шаге A.5.

### A.3. Домен и сертификат — через панель

В панели Webuzo (`https://193.111.62.158:2004`):

1. *Domains* → добавьте `lexify-app.com` (и `www.lexify-app.com`).
2. *SSL/TLS* → *Let's Encrypt* → выпустите сертификат на оба имени, включите авто-обновление.
3. Проверьте, что `https://lexify-app.com` открывает заглушку панели — значит TLS работает и
   осталось только перенаправить трафик на приложение.

DNS-записи (шаг 3 основной инструкции) должны быть настроены **до** выпуска сертификата.

**Кнопки Install/Renew в панели могут отдать `Oops! There was an error while connecting to Server`
при фактически успешной операции.** Проверять результат нужно не по диалогу, а снаружи:

```bash
echo | openssl s_client -connect <IP>:443 -servername lexify-app.com 2>/dev/null \
  | openssl x509 -noout -subject -issuer -dates -ext subjectAltName
```

Автопродление идёт **не через веб-интерфейс**, а по cron (`/etc/cron.d/lets_encrypt`), который
вызывает CLI напрямую — панельная ошибка на него не влияет. Проверить механизм можно в любой момент:

```bash
sudo /usr/local/emps/bin/php /usr/local/webuzo/cli.php --lets_encrypt --action=renew_all --force=1
# Verified Port 80 → механизм рабочий; "Renew date is not yet passed" — штатный ответ до 30 дней
```

**Диагностика ACME: не проверяйте несуществующим именем файла.** Запрос к
`/.well-known/acme-challenge/<любое-имя>` вернёт `200` от nginx — но это проксированная страница
ошибки `/error/404.html`, а не проксированный ACME-путь. Выглядит как сломанное исключение, хотя
исключение работает. Корректная проверка — положить реальный файл:

```bash
sudo mkdir -p /home/<user>/public_html/.well-known/acme-challenge
echo ok | sudo tee /home/<user>/public_html/.well-known/acme-challenge/testfile
curl http://<домен>/.well-known/acme-challenge/testfile    # ждём ok и Server: Apache
sudo rm -rf /home/<user>/public_html/.well-known
```

### A.4. Файрвол

Webuzo обычно ставит **CSF** вместо ufw — не настраивайте оба сразу. Проверьте:

```bash
csf -l 2>/dev/null && echo "CSF активен — правьте /etc/csf/csf.conf, ufw не трогайте"
```

Наружу нужны только 22, 80, 443 и порт панели. Порты 2002/2004 разумно ограничить своим IP.

### A.5. Проксирование на приложение

Правки вносите через интерфейс панели (*Domains* → домен → *Custom vhost / Config*), иначе панель
перезапишет файл при следующей перегенерации конфигов.

**Если Apache** (подтверждено на этом сервере: порты 80/443 держит `httpd`) — учтите, что Webuzo
ставит **свою сборку** в `/usr/local/apps/apache2/`, а не системный пакет. Поэтому здесь нет
`a2enmod` и нет `/etc/apache2/`; модули либо уже вкомпилированы, либо включаются через панель.

Проверка наличия модулей:

```bash
/usr/local/apps/apache2/bin/httpd -M 2>/dev/null | grep -i -E 'proxy|headers'
```

Нужны `proxy_module`, `proxy_http_module`, `headers_module`. Затем в vhost домена (блок `:443`):

Webuzo генерирует vhost'ы в `/usr/local/apps/apache2/etc/conf.d/webuzoVH.conf` и **перезаписывает
этот файл**. Не правьте его. В каждом vhost панель оставляет точку расширения:

```
IncludeOptional /var/webuzo-data/apache2/custom/domains/<домен>.conf
```

Файла нет — создаём его сами, панель его не трогает:

```bash
sudo mkdir -p /var/webuzo-data/apache2/custom/domains
sudo tee /var/webuzo-data/apache2/custom/domains/lexify-app.com.conf > /dev/null <<'EOF'
ProxyPreserveHost On
RequestHeader set X-Forwarded-Proto expr=%{REQUEST_SCHEME}

# ACME-проверки Let's Encrypt должен обслуживать Apache, а не контейнер: иначе nginx вернёт
# index.html вместо токена и автопродление сломается молча, через 90 дней после выпуска.
# (Webuzo добавляет такое же исключение сам, ниже по vhost — здесь оно продублировано.)
ProxyPass /.well-known/ !

# flushpackets=on обязателен для SSE (POST /api/words/format) — без него поток придёт одним
# куском в самом конце, и импорт слов будет выглядеть зависшим.
ProxyPass        / http://127.0.0.1:8081/ flushpackets=on timeout=300
ProxyPassReverse / http://127.0.0.1:8081/
EOF

sudo /usr/local/apps/apache2/bin/httpd -t          # ждём Syntax OK
sudo /usr/local/apps/apache2/bin/apachectl -k graceful
```

⚠️ **Файл подключается в оба vhost — и `:80`, и `:443`.** Разделить их через
`<If "%{SERVER_PORT} == '443'">` нельзя: Apache отвечает `ProxyPass cannot occur within <If>
section`. Поэтому проксируются оба порта, схема передаётся динамически через
`expr=%{REQUEST_SCHEME}`, а редирект http→https делает само приложение — для этого в
`docker-compose.prod.yml` задан `ASPNETCORE_HTTPS_PORT=443`. Без этой переменной ASP.NET не может
вычислить публичный порт, пишет в лог `Failed to determine the https port for redirect` и
**молча отдаёт приложение по открытому HTTP**.

**Если nginx** — в server-блок домена (`listen 443 ssl`):

```nginx
location / {
    proxy_pass         http://127.0.0.1:8081;
    proxy_http_version 1.1;
    proxy_set_header   Host              $host;
    proxy_set_header   X-Real-IP         $remote_addr;
    proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
    proxy_set_header   X-Forwarded-Proto https;

    # SSE: без этого поток буферизуется и приходит одним куском
    proxy_buffering    off;
    proxy_cache        off;
    proxy_read_timeout 300s;
}
```

`X-Forwarded-Proto: https` — обязателен. Backend за прокси видит только plain HTTP на последнем
хопе; без этого заголовка `UseHttpsRedirection` уводит запросы в бесконечный редирект, а
rate-limiter считает всех пользователей одним IP.

Перезапустите веб-сервер средствами панели (или `systemctl reload apache2` / `nginx`).

### A.6. Запуск и проверка

```bash
cd /opt/lexify
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d    # без --profile edge → Caddy не стартует
ss -tlnp | grep 8081                               # должен слушать ТОЛЬКО 127.0.0.1

curl -I  https://lexify-app.com
curl     https://lexify-app.com/api/health         # Healthy
```

`Caddyfile` на сервере в этом сценарии не используется — можно оставить, он ни на что не влияет.

### A.7. Что учесть дальше

- **`ACME_EMAIL` в `.env` можно не заполнять** — сертификатом занимается панель.
- **Порт 8081 не должен быть доступен снаружи.** Проверьте с локальной машины:
  `curl http://193.111.62.158:8081` — должно быть отказано в соединении.
- **MySQL и PHP от Webuzo приложению не нужны** — если панель позволяет, остановите их, они
  просто едят RAM. Приложение использует Postgres из Docker.
- **RAM.** Замер до установки Docker: из 3.8 GB занято 265 MB, доступно 3.3 GB — стек Webuzo
  (Apache, MariaDB, PHP 8.1/8.2, exim, dovecot) в простое почти ничего не ест. Нашим контейнерам
  нужно ~1.5–2 GB, запас есть, Piper можно не выключать. Следите за `docker stats` и `free -h`
  первую неделю; при OOM сначала `PIPER_ENABLED=false`, затем остановка MariaDB/PHP панели.
- **На сервере уже есть почтовый стек** (exim + dovecot). Использовать его как SMTP для приложения
  не стоит: письма с нового VPS-адреса без репутации массово уходят в спам. Внешний провайдер из
  шага 5 остаётся правильным выбором.
- **CSF и Docker конфликтуют по iptables.** Docker пишет свои правила напрямую, а `csf -r`
  перезагружает таблицы и может порвать сеть контейнеров (симптом: контейнеры перестают ходить
  наружу и друг к другу). В `/etc/csf/csf.conf` включите `DOCKER = "1"` (в свежих версиях CSF есть
  штатная поддержка) и после каждого `csf -r` проверяйте `docker compose ps` и `/api/health`.
- **Docker на Ubuntu 20.** `get.docker.com` для focal пока работает; если установка отвалится,
  ставьте из официального репозитория Docker вручную с явным указанием дистрибутива `focal`.
- **Автообновление панели** может перегенерировать vhost и снести правки из A.5. После каждого
  обновления Webuzo проверяйте, что сайт открывается, а не заглушка панели.
