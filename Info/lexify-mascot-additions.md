# План новых картинок и анимаций Лекси

> Аудит текущих ассетов маскота + список того, что стоит добавить, + готовые
> генерационные промпты. Источники: `lexify-mascot.md` (спека персонажа),
> `lexify-mascot-prompt.md` (базовый промпт и правила консистентности).
> Промпты ниже используют тот же CHARACTER-блок, что и `lexify-mascot-prompt.md`,
> чтобы персонаж не «плыл» между картинками.

---

## 1. Что есть сейчас (инвентаризация)

Файлы: `frontend/src/shared/ui/mascot/poses/`. Каждая поза — PNG (512×512,
прозрачный) + SVG; три позы дополнительно имеют анимированный WebM.

| # | Поза | WebM | Где используется |
|---|---|---|---|
| 1 | `greeting` | — | Login, Dashboard (idle / 0 due), Feedback, ConversationSetup, Unsubscribe (успех), **аватар ответов в чате Lexi** |
| 2 | `celebrate` | ✅ (1×) | VerifyEmail (успех), Review (сессия завершена), Conversation (завершён), Feedback (отправлено) |
| 3 | `sleep` | ✅ (loop) | Review (nothing due), SharedBlock (загрузка), Unsubscribe (успех) |
| 4 | `confused` | — | VerifyEmail (ошибка), Unsubscribe (ошибка), Stats (problem words) |
| 5 | `diving` | ✅ (loop) | AI-лоадеры (импорт слов, генерация теста), **индикатор «печатает» в чате Lexi** |
| 6 | `lost` | — | 404, ConversationHistory (пусто), Conversation (ошибка) |
| 7 | `builder` | — | **НИГДЕ** — мёртвый ассет |
| 8 | `scientist` | — | AdminAudit (пусто), AdminFeedback (пусто) |
| 9 | `pointing` | — | Dashboard (есть due), CheckEmail |

Итого: **9 поз, 3 анимации.**

---

## 2. Проблемы, которые видно из аудита

1. **`builder` не подключён.** Поза нарисована, но экраны, под которые она
   делалась, не собраны: **страница обслуживания (503)** и **PWA-тост «доступна
   новая версия»** (спека §3.1 п.11–12). Это не «нарисовать новое», а «дать
   ассету дом» — но без этих экранов картинка простаивает.
2. **`greeting` перегружен** (7 мест). Особенно спорно — как аватар реплик в
   чате Lexi: флагманская фича делит картинку с формой логина.
3. **`celebrate.webm` — одна и та же анимация в 4 экранах успеха.** Пользователь
   видит буквально идентичное конфетти и в конце ревью, и после фидбэка. Момент
   рекорда/стрика (спека §6, ключ `mascot.streak` уже есть в i18n) заслуживает
   отдельного «трофейного» жеста.
4. **`diving` работает и лоадером, и индикатором «печатает» в чате.** Для чата
   уместнее отдельный мини-жест «думает/печатает».
5. **Экраны без маскота вообще:** Register, SearchResults (пустой поиск),
   Profile, TestResults (экран результата теста — идеальное место для празднования,
   сейчас пусто), BlockList/BlockDetail (пустой блок — «добавь слова»).
6. **Письма без картинок.** `EmailTemplates.cs` — только текстовый заголовок
   `Lexify`. Спека §3.2 требует Лекси в welcome / reminder / reset / verify.
7. **Нет соц-превью (OG-image).** Ссылки на расшаренные блоки `/s/{token}` и
   страница логина шарятся без картинки.

---

## 3. Что добавить

### 3.1 Новые статичные позы (приоритет ↓)

| Приоритет | id | Назначение (заменяет / закрывает) |
|---|---|---|
| P0 | `talking` | Аватар ответов Lexi в чате (вместо `greeting`) — флагман получает своё лицо |
| P0 | `trophy` | Стрик-рекорды, новый рекорд, высокий балл теста, экран результатов теста (отделить от общего `celebrate`) |
| P1 | `thinking` | Индикатор «печатает…» в чате (вместо `diving`); «AI размышляет» |
| P1 | `reading` | Старт ревью / «продолжай учиться»; тёплая альтернатива `sleep` для «всё повторено». Отличается от `scientist` (без очков, ученик, не эксперт) |
| P1 | `writing` | Пустой блок «добавь первые слова», интро импорта слов (BlockDetail / WordImport) |
| P2 | `searching` | Пустой результат поиска (SearchResults) — «ничего не нашлось» с лупой |
| P2 | `neutral` | Футер письма reset password (спека: «нейтральный, маленький»), дефолтный аватар профиля |
| P3 | `farewell` | Выход из аккаунта / Unsubscribe «до встречи» (сейчас переиспользует sleep+greeting) |

### 3.2 Новые анимации (WebM, 512×512, ≤300 КБ, бесшовный луп кроме одноразовых)

| Приоритет | Анимация | Из позы | Где |
|---|---|---|---|
| P0 | `talking` (loop) | `talking` | Аватар Lexi, пока стримится ответ в чате |
| P1 | `thinking` (loop) | `thinking` | Индикатор «печатает…» / «AI работает» |
| P1 | `trophy` (1×) | `trophy` | Тост стрика/рекорда (искры + мелкий подскок) |
| P2 | `reading` (loop) | `reading` | Старт ревью / study-эмпти |
| P3 | `wave` (loop) | `greeting` | Hero логина/дашборда (сейчас только CSS-float) |

### 3.3 Не-позовые ассеты

- **Логотип-голова для писем** — переиспользовать `Info/mascot/icon.png` (уже есть,
  голова с жабрами) как inline-картинку 96–128px в шапке всех email-шаблонов.
  Отдельный промпт не нужен — нужен только рендер `icon` в PNG под письма.
- **OG / social share (1200×630)** — Лекси + вордмарк «Lexify» + тэглайн, на
  фоне-подложке акцента. Для `/s/{token}` и логина.
- **Дефолтный аватар профиля** — та же `icon`-голова или новая поза `neutral`.

---

## 4. Готовые промпты для генерации

### 4.1 Общий блок персонажа (подставляется перед каждой POSE-строкой)

> Тот же CHARACTER BASE, что в `lexify-mascot-prompt.md` §B. Всегда прикладывать
> character sheet как image reference, чтобы персонаж не «плыл».

```
"Lexi" the axolotl mascot — same character as the reference sheet: chubby
pastel-pink (#FBD9E0) axolotl with an oversized round head, three pairs of
fluffy fern-leaf gills in rose pink (#F096AC), small black oval eyes with
white highlight dots (glossy look), tiny kawaii ":3" mouth, round pink
blush, thick curled tail, bold uniform dark (#2B2226) outline. Flat 2D
kawaii sticker style, solid fills, no gradients, no realistic shadows,
transparent background, 1024x1024, centered, full body visible.

POSE:
```

### 4.2 POSE-строки для новых поз

| id | Строка для промпта (после `POSE:`) |
|---|---|
| `talking` | happily talking, mouth open in a friendly speech shape, one front paw raised in a small explaining gesture, a light-blue (#CDEBF5) speech bubble with three small dots floating beside the head, warm engaged expression, eyes bright with highlights |
| `trophy` | proudly holding up a small golden trophy cup with both paws, eyes closed in happy arches (^ ^), big cheerful smile, a few tiny warm-orange (#FB923C) and yellow (#FBBF24) sparkle stars around the trophy, chest puffed with pride |
| `thinking` | thoughtful pondering pose, one front paw resting on the chin, head tilted slightly up, one gill curled like a raised eyebrow, a light-blue (#CDEBF5) thought bubble with three small dots above the head, calm focused expression |
| `reading` | sitting cozily and reading a small open book held in both paws, eyes looking down at the pages, gentle relaxed smile, gills soft and calm, absorbed and content expression |
| `writing` | writing on a small note card / notebook with a pencil held in one paw, the other paw steadying the card, tongue-tip-out focused cute expression, tiny confident smile, eager-to-help energy |
| `searching` | curiously looking through a large round magnifying glass held in one paw, one eye enlarged and glossy through the lens, leaning forward inquisitively, other paw shading the brow, playful searching expression |
| `neutral` | standing calmly facing forward, both stubby paws relaxed at the sides, soft gentle friendly smile, no props, neutral welcoming resting pose, eyes with bright highlights |
| `farewell` | waving goodbye softly with one raised front paw, eyes closed in a warm gentle arc, tender small smile, slight friendly head tilt, cozy see-you-soon mood |

### 4.3 Негативный промпт (общий, из `lexify-mascot-prompt.md` §C)

```
blue body, realistic, 3D render, gradients, heavy shadows, fur, teeth,
human hands, text, watermark, background scenery, gray colors, dull
colors, extra limbs, empty dead eyes without highlights, more or fewer
than 6 gills, simple ball-tipped gill stalks, sad or crying expression,
aggressive expression
```

### 4.4 Промпты анимаций (image-to-video: Kling / Runway / Pika / Luma)

> Как и раньше: вход — поза на хромакей-зелёном `#00B140`
> (`Info/mascot-animation/{pose}-green.png`), длительность 5 с, 1:1,
> motion strength 2–4/10, режим loop где есть. Негативный промпт анимаций —
> из `lexify-mascot-prompt.md` §E4. Постобработка (вырезание зелёного, луп,
> экспорт в 512×512 WebM) — §E5.

**`talking` (бесшовный луп):**
```
Flat 2D cartoon sticker animation. The cute pink axolotl talks cheerfully in
place — its small mouth opens and closes gently as if speaking, the explaining
paw makes a tiny up-and-down gesture, and the three dots inside the light-blue
speech bubble pulse in sequence; the fern-like gill fronds sway very slightly.
Smooth, calm, looping motion. Static camera, no zoom, character stays centered,
exact flat cartoon style and proportions preserved, solid green background
completely unchanged.
```

**`thinking` (бесшовный луп):**
```
Flat 2D cartoon sticker animation. The pink axolotl ponders in place — it tilts
its head slightly, the paw taps the chin softly, and the three dots inside the
light-blue thought bubble appear one by one and cycle; the gill fronds ripple
gently. Minimal, cozy, looping motion. Static camera, no zoom, character stays
centered, exact flat cartoon style preserved, solid green background completely
unchanged.
```

**`trophy` (одноразовое проигрывание):**
```
Flat 2D cartoon sticker animation. The pink axolotl gives a small joyful hop
while holding up the golden trophy; the sparkle stars around the trophy twinkle
and pop brightly, then settle; the gill fronds bounce softly with the hop. The
character stays centered and keeps its exact flat cartoon style and proportions.
Static camera, no zoom, no pan, the solid green background stays completely
unchanged.
```

**`reading` (бесшовный луп):**
```
Flat 2D cartoon sticker animation. The pink axolotl reads calmly in place — its
eyes move slowly across the page, the book pages flutter as if turning very
gently, its body breathes with a subtle rise and fall, the gill fronds sway
slightly. Calm, minimal, cozy, seamless looping motion. Static camera, no zoom,
character stays centered, exact flat cartoon style preserved, solid green
background completely unchanged.
```

---

## 5. Не-картинки, которые нужно собрать, чтобы ассеты заработали

Эти пункты — код, не генерация, но без них картинки простаивают:

1. **Страница обслуживания (503)** + **PWA-тост обновления** → подключить
   существующую позу `builder` (спека §3.1 п.11–12, ключи `mascot.maintenance`,
   `mascot.update` уже в i18n).
2. **Чат Lexi** → заменить `greeting`/`diving` на `talking`/`thinking`.
3. **TestResults** → добавить `trophy`/`celebrate` на экран результата теста.
4. **Register / SearchResults / пустой блок** → `greeting` / `searching` / `writing`.
5. **EmailTemplates.cs** → inline `icon`-голова в шапке; `neutral` в футере reset.
6. **Стрик-рекорд** → тост с `trophy` (ключ `mascot.streak` уже есть, но не вызывается).

---

## 6. Чек-лист приёмки

Тот же, что в `lexify-mascot-prompt.md` («Чек-лист приёмки каждой картинки»):
тело `#FBD9E0`, ровно 3 пары папоротниковых жабр, глаза с белыми бликами, рот
`:3`, ровный контур `#2B2226`, плоская заливка без градиентов, прозрачный фон,
силуэт читается в 32 px, эмоция соответствует позе, ничего пугающего/грустного.
