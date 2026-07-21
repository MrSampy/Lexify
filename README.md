# Lexify — AI-powered Word Learning Platform

Персональная платформа для изучения иностранных слов с AI-форматированием, умными тестами и Spaced Repetition.

## Описание

Пользователь вводит слова в произвольном формате → AI нормализует ввод → пользователь редактирует и сохраняет блок → генерируется интерактивный тест. Spaced Repetition (SM-2) напоминает о словах в нужный момент.

**Ключевые фичи:**
- AI-форматирование слов через Ollama (локально) с fallback на OpenAI
- SSE-стриминг для отображения прогресса AI в реальном времени
- 5 типов вопросов: translate, fill-in-the-blank, multi-select, open answer
- Spaced Repetition по алгоритму SM-2
- Поддержка английского, норвежского, украинского и других языков

## Стек технологий

| Компонент | Технология |
|---|---|
| Backend | ASP.NET Core 8, Clean Architecture, CQRS + MediatR |
| База данных | PostgreSQL 16 (JSONB, Full-text search, RLS) |
| Кэш / Очередь | Redis 7, Hangfire |
| AI | Ollama Cloud (gemma4:31b), OpenAI-compatible |
| Frontend | React 18 + TypeScript, Feature-Sliced Design |
| State | Zustand + TanStack Query v5 |
| UI | shadcn/ui + Tailwind CSS |

## Структура проекта

```
lexify/
├── backend/              # ASP.NET Core решение
│   ├── src/
│   │   ├── Lexify.Domain/          # Сущности, Value Objects, интерфейсы репозиториев
│   │   ├── Lexify.Application/     # CQRS Commands/Queries, MediatR handlers
│   │   ├── Lexify.Infrastructure/  # EF Core, Redis, Ollama/OpenAI клиенты
│   │   └── Lexify.API/             # ASP.NET Controllers, Middleware, Program.cs
│   └── tests/
│       ├── Lexify.Domain.Tests/
│       ├── Lexify.Application.Tests/
│       └── Lexify.API.Tests/
├── frontend/             # React SPA (Feature-Sliced Design)
│   └── src/
│       ├── app/          # Роутинг, провайдеры
│       ├── pages/        # Страницы приложения
│       ├── widgets/      # Самодостаточные блоки UI
│       ├── features/     # Действия пользователя
│       ├── entities/     # Бизнес-сущности (word, block, test)
│       └── shared/       # UI-kit, api-client, утилиты
├── docs/                 # Документация и архитектурные решения
├── Info/                 # Проектная документация
└── docker-compose.yml    # PostgreSQL, Redis, Ollama
```

## Быстрый старт

### Требования

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### 1. Запустить инфраструктуру

```bash
docker-compose up -d postgres redis
```

Запускает: PostgreSQL 16 (порт 5432), Redis 7 (порт 6379). AI работает через Ollama Cloud (см. ниже), не в Docker.

### 2. Настроить Ollama Cloud

Приложение использует **Ollama Cloud** (`https://ollama.com`) через OpenAI-совместимый эндпоинт
`/v1/chat/completions` — локальная модель и GPU не нужны. Модель по умолчанию: **`gemma4:31b`**.

API-ключ **не хранится в репозитории**. Для локальной разработки задайте его через user-secrets:

```bash
dotnet user-secrets init --project backend/src/Lexify.API   # один раз (добавит UserSecretsId)
dotnet user-secrets set "AiProviders:0:ApiKey" "<ваш-ключ-Ollama-Cloud>" --project backend/src/Lexify.API
```

В Docker/prod ключ берётся из переменной окружения `OLLAMA_API_KEY` (см. `.env.example`).

**Локальный Ollama вместо облака** (опционально): установите с https://ollama.com/download/windows,
`ollama serve`, `ollama pull gemma4:31b`, затем в `appsettings.Development.json` поменяйте `BaseUrl`
провайдера на `http://localhost:11434` и оставьте `ApiKey` пустым.

### 3. Запустить Backend

```bash
cd backend
dotnet restore
dotnet ef database update --project src/Lexify.Infrastructure --startup-project src/Lexify.API
dotnet run --project src/Lexify.API
```

API доступен на `http://localhost:5000`, Swagger — `http://localhost:5000/swagger`.

### 4. Запустить Frontend

```bash
cd frontend
npm install
npm run dev
```

Приложение доступно на `http://localhost:5173`.

## Эксплуатация (production)

### Бэкапы БД

Прод-стек (`docker-compose.prod.yml`) поднимает два сервиса:

- `postgres-backup` — `pg_dump` по расписанию (`BACKUP_SCHEDULE`, по умолчанию раз в сутки) с
  ротацией daily/weekly/monthly в volume `pgbackups`.
- `backup-offsite` — выгружает эти дампы в S3-совместимое хранилище (`S3_*` в `.env`).

Локальная копия нужна для быстрого отката, offsite — на случай потери самого VPS.

### Проверка и восстановление

**Непроверенный бэкап — это не бэкап.** Прогоните проверку один раз сразу после деплоя, до того
как в системе появятся реальные данные:

```bash
./scripts/restore-db.sh          # восстановит свежий дамп во временную БД и покажет число строк
```

Скрипт создаёт одноразовую базу, заливает туда дамп, печатает количество пользователей/блоков/слов
и удаляет её. Живые данные не трогаются.

Реальное восстановление (**разрушительно**, заменяет боевую БД, требует подтверждения):

```bash
./scripts/restore-db.sh --into-production /backups/daily/lexify-YYYYMMDD.sql.gz
```

### Invite-only и лимит на AI

Настраиваются в админке (`/admin/settings`), передеплой не нужен:

| Ключ | Что делает |
|---|---|
| `features.registration_enabled` | `false` — открытая регистрация закрыта |
| `features.invite_code` | код, по которому можно зарегистрироваться при закрытой регистрации (пусто = регистрация закрыта полностью) |
| `ai.max_calls_per_user_per_day` | сколько AI-вызовов в сутки (UTC) может сделать один пользователь; `0` — без лимита |

Лимит на AI защищает кошелёк владельца: ключ Ollama Cloud общий, и без потолка один пользователь
может израсходовать его за день.

## Архитектура

Детали архитектуры: [`Info/lexify-architecture.md`](Info/lexify-architecture.md)  
Схема базы данных: [`Info/lexify-database.md`](Info/lexify-database.md)  
Список задач: [`Info/lexify-tasks.md`](Info/lexify-tasks.md)

## Лицензия

MIT
