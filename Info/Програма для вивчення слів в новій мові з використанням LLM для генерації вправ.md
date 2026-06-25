# Lexify — Система изучения слов с AI

> Персональная платформа для изучения иностранных слов с AI-форматированием, умными тестами и spaced repetition.

---

## Содержание

1. [Обзор проекта](#1-обзор-проекта)
2. [Стек технологий](#2-стек-технологий)
3. [Архитектура системы](#3-архитектура-системы)
4. [База данных](#4-база-данных)
5. [Backend API](#5-backend-api)
6. [AI-интеграция](#6-ai-интеграция)
7. [Выбор модели Ollama](#7-выбор-модели-ollama)
8. [Frontend](#8-frontend)
9. [Тестирование (квизы)](#9-тестирование-квизы)
10. [Spaced Repetition](#10-spaced-repetition)
11. [Аутентификация и авторизация](#11-аутентификация-и-авторизация)
12. [Админ-панель](#12-админ-панель)
13. [Уязвимости и их решения](#13-уязвимости-и-их-решения)
14. [Дорожная карта (MVP → v2)](#14-дорожная-карта-mvp--v2)

---

## 1. Обзор проекта

### Суть

Пользователь вводит слова в произвольном формате (как записал в тетради или скопировал из текста). AI нормализует ввод, предлагает структуру, юзер редактирует и сохраняет блок. Затем по одному или нескольким блокам генерируется интерактивный тест.

### Ключевые сценарии использования

- Студент учит английский: вводит 20 слов после урока → AI форматирует → тест через 10 минут
- Пользователь учит норвежский: смешанный ввод (norsk + переводы на украинский) → AI определяет языки → сохраняет в правильный блок
- Повторение: система напоминает о словах, которые пора повторить по алгоритму spaced repetition

### Основные сущности

| Сущность | Описание |
|---|---|
| `User` | Аккаунт пользователя |
| `Language` | Язык (EN, NO, UK, RU и др.) |
| `WordBlock` | Тематический блок слов (принадлежит юзеру, один язык) |
| `Word` | Слово/фраза/идиома внутри блока |
| `Tag` | Произвольный тег для блока (`#work`, `#B2`) |
| `Test` | Тест по одному или нескольким блокам |
| `Question` | Вопрос в тесте (разных типов) |
| `TestAttempt` | Одна попытка прохождения теста |
| `ReviewSchedule` | Расписание повторений (SM-2) |

---

## 2. Стек технологий

### Backend

| Компонент | Технология | Причина выбора |
|---|---|---|
| Фреймворк | ASP.NET Core 8 | Родной стек, высокая производительность |
| ORM | Entity Framework Core 8 | Миграции, LINQ, хорошо интегрируется с .NET |
| БД | PostgreSQL 16 | JSONB для хранения AI-метаданных, full-text search |
| Кэш | Redis 7 | Кэш тестов, токены сессий, rate limiting |
| Очередь задач | Hangfire (на Redis) | Фоновая генерация тестов, напоминания |
| AI-клиент | HTTP client → Ollama API | Прямой вызов, без лишних абстракций |
| Стриминг | Server-Sent Events (SSE) | Стриминг AI-ответов на фронт |
| Auth | ASP.NET Identity + JWT | Стандарт, хорошо документирован |

### Frontend

| Компонент | Технология |
|---|---|
| Фреймворк | React 18 + TypeScript |
| State management | Zustand |
| Data fetching | TanStack Query v5 |
| UI | shadcn/ui + Tailwind CSS |
| Routing | React Router v6 |
| Формы | React Hook Form + Zod |
| Стриминг | EventSource API (SSE) |

### AI / Инфраструктура

| Компонент | Технология |
|---|---|
| LLM (основная) | Ollama — локально (`qwen2.5:7b` или `llama3.2`) |
| LLM (fallback) | OpenAI API (GPT-4o-mini) — при недоступности Ollama |
| Модель для тестов | Та же модель, отдельные промпты |

### Мобильное приложение (v2)

React Native — максимальный реюз логики и компонентов с web. Обращается к тому же REST API без изменений на бэкенде.

---

## 3. Архитектура системы

```
┌─────────────────────────────────────────────────────────┐
│                    Frontend (React SPA)                  │
│  Word Input │ Block Manager │ Test Runner │ Statistics   │
└──────────────────────┬──────────────────────────────────┘
                       │ REST / SSE
┌──────────────────────▼──────────────────────────────────┐
│                  Backend (ASP.NET Core 8)                │
│                                                          │
│  AuthController  WordsController  TestsController        │
│       │               │                │                 │
│  ┌────▼───────────────▼────────────────▼──────────┐     │
│  │              Application Layer                  │     │
│  │  AuthService  WordService  TestService          │     │
│  │  AIOrchestrator  ReviewScheduler               │     │
│  └────┬───────────────┬────────────────┬──────────┘     │
│       │               │                │                 │
│  ┌────▼──────┐  ┌─────▼────┐  ┌───────▼──────────┐     │
│  │ PostgreSQL│  │  Redis   │  │  Hangfire Queue  │     │
│  └───────────┘  └──────────┘  └──────────────────┘     │
└─────────────────────────────┬───────────────────────────┘
                               │ HTTP
┌──────────────────────────────▼──────────────────────────┐
│                       AI Layer                           │
│  Ollama (local, primary)  │  OpenAI API (fallback)      │
└─────────────────────────────────────────────────────────┘
```

### Принципы архитектуры

- **Vertical Slice Architecture** — каждая фича (Words, Tests, Auth) — отдельная папка со своими Command/Query (CQRS через MediatR)
- **AI как внешний сервис** — `IAIProvider` интерфейс, две реализации: `OllamaProvider` и `OpenAIProvider`. Fallback автоматический.
- **Stateless backend** — всё состояние в PostgreSQL / Redis. Готово к горизонтальному масштабированию.

### Структура проекта (Backend)

```
Lexify.API/
├── Features/
│   ├── Auth/
│   │   ├── Commands/LoginCommand.cs
│   │   ├── Commands/RegisterCommand.cs
│   │   └── AuthController.cs
│   ├── Words/
│   │   ├── Commands/ImportWordsCommand.cs
│   │   ├── Commands/FormatWordsCommand.cs
│   │   ├── Queries/GetBlocksQuery.cs
│   │   └── WordsController.cs
│   ├── Tests/
│   │   ├── Commands/GenerateTestCommand.cs
│   │   ├── Commands/SubmitAttemptCommand.cs
│   │   ├── Queries/GetTestQuery.cs
│   │   └── TestsController.cs
│   └── Review/
│       ├── Queries/GetDueWordsQuery.cs
│       └── ReviewController.cs
├── Infrastructure/
│   ├── AI/
│   │   ├── IAIProvider.cs
│   │   ├── OllamaProvider.cs
│   │   └── OpenAIProvider.cs
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   └── Migrations/
│   └── Cache/
│       └── RedisCacheService.cs
└── Common/
    ├── Middleware/
    │   ├── AuthorizationMiddleware.cs
    │   └── RateLimitingMiddleware.cs
    └── Exceptions/
```

---

## 4. База данных

### Схема

```sql
-- Языки
CREATE TABLE languages (
    id          SMALLSERIAL PRIMARY KEY,
    code        VARCHAR(5)  NOT NULL UNIQUE,  -- 'en', 'no', 'uk', 'ru'
    name        VARCHAR(50) NOT NULL           -- 'English', 'Norwegian'
);

-- Пользователи
CREATE TABLE users (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    email           VARCHAR(255) NOT NULL UNIQUE,
    password_hash   TEXT        NOT NULL,
    display_name    VARCHAR(100),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Блоки слов
CREATE TABLE word_blocks (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    language_id     SMALLINT    NOT NULL REFERENCES languages(id),
    title           VARCHAR(200) NOT NULL,       -- генерирует AI, юзер может изменить
    description     TEXT,
    word_count      INT         NOT NULL DEFAULT 0,  -- денормализовано для скорости
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Теги
CREATE TABLE tags (
    id      SERIAL      PRIMARY KEY,
    user_id UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name    VARCHAR(50) NOT NULL,
    UNIQUE(user_id, name)
);

-- Связь блок ↔ теги
CREATE TABLE block_tags (
    block_id    UUID    REFERENCES word_blocks(id) ON DELETE CASCADE,
    tag_id      INT     REFERENCES tags(id) ON DELETE CASCADE,
    PRIMARY KEY (block_id, tag_id)
);

-- Слова
CREATE TABLE words (
    id                  UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    block_id            UUID        NOT NULL REFERENCES word_blocks(id) ON DELETE CASCADE,
    term                TEXT        NOT NULL,   -- оригинальное слово/фраза
    translation         TEXT        NOT NULL,   -- перевод
    word_type           VARCHAR(20) NOT NULL DEFAULT 'word',
        -- ENUM: 'word' | 'phrase' | 'idiom' | 'expression'
    notes               TEXT,                   -- примечания, контекст
    example_sentence    TEXT,                   -- пример использования (AI может добавить)
    confidence_flag     BOOLEAN     NOT NULL DEFAULT FALSE,  -- AI пометила перевод как неуверенный
    sort_order          INT         NOT NULL DEFAULT 0,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    -- Spaced repetition (SM-2)
    ease_factor         FLOAT       NOT NULL DEFAULT 2.5,
    interval_days       INT         NOT NULL DEFAULT 1,
    repetitions         INT         NOT NULL DEFAULT 0,
    next_review_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Тесты
CREATE TABLE tests (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title       VARCHAR(200) NOT NULL,
    status      VARCHAR(20) NOT NULL DEFAULT 'ready',
        -- 'generating' | 'ready' | 'archived'
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Блоки, включённые в тест
CREATE TABLE test_blocks (
    test_id     UUID    REFERENCES tests(id) ON DELETE CASCADE,
    block_id    UUID    REFERENCES word_blocks(id) ON DELETE CASCADE,
    PRIMARY KEY (test_id, block_id)
);

-- Вопросы теста
CREATE TABLE questions (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    test_id         UUID        NOT NULL REFERENCES tests(id) ON DELETE CASCADE,
    word_id         UUID        REFERENCES words(id) ON DELETE SET NULL,  -- источник вопроса
    question_type   VARCHAR(30) NOT NULL,
        -- 'translate_to_native' | 'translate_to_foreign'
        -- 'fill_in_sentence' | 'multi_select_theme'
        -- 'open_answer' | 'match_pairs'
    question_text   TEXT        NOT NULL,
    correct_answer  TEXT        NOT NULL,
    sort_order      INT         NOT NULL DEFAULT 0,
    content_hash    VARCHAR(64) NOT NULL  -- SHA-256 для дедупликации вопросов
);

-- Варианты ответов (для single/multi-choice)
CREATE TABLE question_options (
    id          UUID    PRIMARY KEY DEFAULT gen_random_uuid(),
    question_id UUID    NOT NULL REFERENCES questions(id) ON DELETE CASCADE,
    option_text TEXT    NOT NULL,
    is_correct  BOOLEAN NOT NULL DEFAULT FALSE,
    sort_order  INT     NOT NULL DEFAULT 0
);

-- Попытки прохождения теста
CREATE TABLE test_attempts (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    test_id         UUID        NOT NULL REFERENCES tests(id) ON DELETE CASCADE,
    user_id         UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    started_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    finished_at     TIMESTAMPTZ,
    score           FLOAT,          -- процент правильных (0.0 – 1.0)
    total_questions INT,
    correct_answers INT
);

-- Ответы пользователя в попытке
CREATE TABLE attempt_answers (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    attempt_id      UUID        NOT NULL REFERENCES test_attempts(id) ON DELETE CASCADE,
    question_id     UUID        NOT NULL REFERENCES questions(id) ON DELETE CASCADE,
    given_answer    TEXT        NOT NULL,
    is_correct      BOOLEAN     NOT NULL,
    time_spent_ms   INT,        -- время ответа в мс (для статистики сложных слов)
    answered_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Индексы
CREATE INDEX idx_words_block_id       ON words(block_id);
CREATE INDEX idx_words_next_review    ON words(next_review_at) WHERE next_review_at <= NOW();
CREATE INDEX idx_word_blocks_user_id  ON word_blocks(user_id);
CREATE INDEX idx_tests_user_id        ON tests(user_id);
CREATE INDEX idx_questions_content    ON questions(content_hash);
CREATE INDEX idx_attempt_answers      ON attempt_answers(attempt_id);

-- Full-text search по словам
CREATE INDEX idx_words_fts ON words USING GIN (
    to_tsvector('simple', term || ' ' || translation)
);
```

### Денормализация `word_count`

Поле `word_count` в `word_blocks` обновляется триггером при INSERT/DELETE в `words`. Это позволяет отображать количество слов в списке блоков без COUNT-запросов.

```sql
CREATE OR REPLACE FUNCTION update_block_word_count()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        UPDATE word_blocks SET word_count = word_count + 1 WHERE id = NEW.block_id;
    ELSIF TG_OP = 'DELETE' THEN
        UPDATE word_blocks SET word_count = word_count - 1 WHERE id = OLD.block_id;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_word_count
AFTER INSERT OR DELETE ON words
FOR EACH ROW EXECUTE FUNCTION update_block_word_count();
```

---

## 5. Backend API

### Аутентификация

```
POST /api/auth/register
POST /api/auth/login       → { accessToken, refreshToken }
POST /api/auth/refresh
POST /api/auth/logout
```

### Блоки слов

```
GET    /api/blocks                     → список блоков юзера (пагинация + фильтр по языку/тегу)
GET    /api/blocks/{id}                → блок + слова
POST   /api/blocks                     → создать блок вручную
PATCH  /api/blocks/{id}                → переименовать, изменить теги
DELETE /api/blocks/{id}

GET    /api/blocks/{id}/words          → слова с пагинацией
POST   /api/blocks/{id}/words          → добавить слово вручную
PATCH  /api/blocks/{id}/words/{wordId} → редактировать слово
DELETE /api/blocks/{id}/words/{wordId}
```

### AI-форматирование (ключевой эндпоинт)

```
POST /api/words/format
```

**Request:**
```json
{
  "rawText": "biased -упереджений\nerode - підірвати?\nauthenticity - автентичність",
  "languageHint": "en",       // необязательно — AI определит сама
  "nativeLanguage": "uk"      // язык переводов
}
```

**Response (SSE stream):**
```
event: progress
data: {"stage": "parsing", "message": "Анализирую структуру..."}

event: progress
data: {"stage": "formatting", "message": "Форматирую слова..."}

event: result
data: {
  "suggestedTitle": "Emotions & Social Behavior",
  "detectedLanguage": "en",
  "words": [
    {
      "term": "biased",
      "translation": "упереджений",
      "wordType": "word",
      "confidenceFlag": false
    },
    {
      "term": "erode",
      "translation": "підірвати",
      "wordType": "word",
      "confidenceFlag": true,
      "confidenceNote": "Перевод неоднозначен: может быть 'розмивати', 'підривати'"
    }
  ]
}

event: done
data: {}
```

**Почему SSE?** При 20+ словах Ollama отвечает 5–15 секунд. SSE позволяет показывать прогресс в реальном времени, не блокируя UI.

### Сохранение отформатированных слов

```
POST /api/blocks/{id}/import
```

```json
{
  "words": [ /* отформатированный массив из /format */ ]
}
```

Этот эндпоинт — отдельный от `/format`, чтобы юзер мог редактировать перед сохранением.

### Тесты

```
POST /api/tests/generate
```

```json
{
  "blockIds": ["uuid1", "uuid2"],
  "questionTypes": ["translate_to_native", "fill_in_sentence", "multi_select_theme"],
  "questionCount": 20
}
```

Генерация тяжёлая → фоновая задача через Hangfire. Возвращает `testId` и статус `generating`. Клиент поллит `GET /api/tests/{id}` или слушает SSE.

```
GET    /api/tests                     → список тестов юзера
GET    /api/tests/{id}                → тест с вопросами
DELETE /api/tests/{id}

POST   /api/tests/{id}/attempts       → начать попытку → { attemptId }
POST   /api/attempts/{id}/answer      → ответить на вопрос
POST   /api/attempts/{id}/finish      → завершить → score, разбор ошибок
GET    /api/attempts/{id}             → результаты попытки
```

### Spaced Repetition

```
GET  /api/review/due          → слова к повторению сегодня
POST /api/review/rate         → оценить слово после повторения
```

```json
// POST /api/review/rate
{
  "wordId": "uuid",
  "quality": 4   // 0-5 по SM-2: 0=полный провал, 5=идеально
}
```

### Поиск

```
GET /api/search?q=bite&lang=en    → full-text search по словам и переводам
```

### Экспорт / Импорт

```
GET  /api/blocks/{id}/export?format=csv
GET  /api/blocks/{id}/export?format=anki
POST /api/blocks/import           → multipart/form-data, CSV
```

---

## 6. AI-интеграция

### Интерфейс провайдера

```csharp
public interface IAIProvider
{
    IAsyncEnumerable<string> StreamFormatWordsAsync(
        string rawText,
        string targetLanguage,
        string nativeLanguage,
        CancellationToken ct = default);

    Task<TestGenerationResult> GenerateTestQuestionsAsync(
        IEnumerable<WordDto> words,
        IEnumerable<string> questionTypes,
        int count,
        CancellationToken ct = default);

    Task<string> SuggestBlockTitleAsync(
        IEnumerable<string> terms,
        string language,
        CancellationToken ct = default);
}
```

### Промпт для форматирования слов

```
You are a language learning assistant. The user has entered words in a free format.
Your task:

1. Parse each line and extract: term, translation.
2. Detect the word type: 'word', 'phrase', 'idiom', or 'expression'.
3. If the user marked a translation with '?' — set confidenceFlag: true.
4. Normalize the term: trim whitespace, fix obvious typos.
5. Suggest a short thematic title for this block (in English, max 5 words).
6. IMPORTANT: Return ONLY valid JSON. No markdown, no preamble.

Input language: {targetLanguage}
Translation language: {nativeLanguage}

Return format:
{
  "suggestedTitle": "...",
  "words": [
    {
      "term": "...",
      "translation": "...",
      "wordType": "word|phrase|idiom|expression",
      "confidenceFlag": false,
      "confidenceNote": "..." // only if confidenceFlag is true
    }
  ]
}

User input:
{rawText}
```

### Промпт для генерации теста

```
You are a language quiz generator. Generate {count} questions for the given words.
Allowed question types: {questionTypes}

Rules:
- For 'translate_to_native': ask to choose the correct translation
- For 'fill_in_sentence': write a natural sentence with a blank, user fills in the word
- For 'multi_select_theme': ask which words belong to a given theme (2+ correct answers)
- Each choice question needs exactly 4 options (1 correct + 3 distractors)
- Distractors must be plausible but clearly wrong
- RETURN ONLY valid JSON, no markdown

Words:
{wordsJson}

Return format:
{
  "questions": [
    {
      "wordTerm": "...",
      "questionType": "...",
      "questionText": "...",
      "correctAnswer": "...",
      "options": ["...", "...", "...", "..."]  // for choice questions only
    }
  ]
}
```

### Fallback-логика

```csharp
public class AIOrchestrator : IAIProvider
{
    private readonly OllamaProvider _ollama;
    private readonly OpenAIProvider _openai;

    public async IAsyncEnumerable<string> StreamFormatWordsAsync(...)
    {
        if (await _ollama.IsAvailableAsync())
        {
            await foreach (var chunk in _ollama.StreamFormatWordsAsync(...))
                yield return chunk;
        }
        else
        {
            // Ollama недоступна — используем OpenAI
            await foreach (var chunk in _openai.StreamFormatWordsAsync(...))
                yield return chunk;
        }
    }
}
```

### Валидация AI-ответа

Если AI вернула некорректный JSON или количество слов < 50% от входного — не сохранять, вернуть ошибку с деталями:

```csharp
public class AIResponseValidator
{
    public ValidationResult Validate(FormatResult result, string rawInput)
    {
        var inputLineCount = rawInput.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;

        if (result.Words.Count < inputLineCount * 0.5)
            return ValidationResult.Fail("AI вернула менее 50% слов. Попробуйте снова или разбейте на части.");

        if (result.Words.Any(w => string.IsNullOrWhiteSpace(w.Term)))
            return ValidationResult.Fail("Некоторые слова не распознаны. Проверьте формат ввода.");

        return ValidationResult.Ok();
    }
}
```

---

## 7. Выбор модели Ollama

### Требования к модели

Для задач Lexify модель должна уметь:

- Работать с украинским, норвежским (Bokmål) и английским одновременно
- Точно следовать инструкциям промпта и возвращать валидный JSON без markdown-обёрток
- Генерировать качественные distractors (правдоподобные, но неправильные переводы)
- Понимать идиомы и фразовые глаголы, а не только отдельные слова
- Работать достаточно быстро на потребительском железе (8–16 GB VRAM / RAM)

### Рекомендуемая модель: `qwen3:8b`

**Почему Qwen3, а не Qwen2.5 или Llama?**

Qwen3 — последнее поколение серии (апрель 2025). По сравнению с Qwen2.5 оно добавляет явное переключение режима «размышления»: `/think` для сложных задач и `/no_think` для быстрых структурных. Для Lexify это напрямую полезно: форматирование слов не требует глубокого reasoning, а генерация качественных тестов — требует.

Qwen3 нативно поддерживает **119 языков**, включая английский, украинский и Norwegian Bokmål — именно те три, которые нужны проекту. На бенчмарке BenchMAX по многоязычным задачам Qwen3 опережает Llama 3.1 аналогичного размера.

```bash
ollama pull qwen3:8b
```

### Сравнение вариантов

| Модель | VRAM / RAM | Скорость | Качество | Рекомендация |
|---|---|---|---|---|
| `qwen3:1.7b` | ~2 GB | очень быстро | слабое | только для тестирования |
| `qwen3:4b` | ~3 GB | быстро | хорошее | минимум для продакшна |
| **`qwen3:8b`** | **~6 GB** | **нормально** | **отличное** | **рекомендуется** |
| `qwen3:14b` | ~10 GB | медленно | превосходное | если есть GPU 12+ GB |
| `qwen3:30b-a3b` | ~20 GB (MoE) | быстро (MoE) | превосходное | сервер с 24+ GB VRAM |

`qwen3:8b` — оптимальный баланс: помещается в 8 GB RAM без GPU (CPU offload, ~3–5 tok/s), или работает быстро на видеокарте от RTX 3060 и выше (~25–40 tok/s).

### Режимы использования в Lexify

Qwen3 поддерживает явное переключение thinking-режима прямо в промптах:

```
# Форматирование слов — быстрый режим, reasoning не нужен
System: /no_think
Return ONLY valid JSON. Parse the user's word list...

# Генерация теста — включаем thinking для качества distractors
System: /think
Generate 20 quiz questions. Consider plausible distractors...
```

Это позволяет форматирование делать за 3–6 секунд, а генерацию теста — за 10–20 секунд с лучшим качеством вопросов.

### Настройки запуска

```json
// Параметры для форматирования (/no_think) — стабильный JSON
{
  "model": "qwen3:8b",
  "options": {
    "temperature": 0.1,
    "top_p": 0.9,
    "num_ctx": 4096,
    "num_predict": 2048
  }
}

// Параметры для генерации теста (/think) — разнообразие вопросов
{
  "model": "qwen3:8b",
  "options": {
    "temperature": 0.6,
    "top_p": 0.95,
    "num_ctx": 8192,
    "num_predict": 4096
  }
}
```

Низкая температура (0.1) при форматировании гарантирует стабильный JSON. Более высокая (0.6) при генерации теста даёт разнообразие вопросов и distractors.

### Fallback на OpenAI

Если Ollama недоступна или возвращает невалидный ответ дважды подряд — автоматически переключаемся на `gpt-4o-mini`. Конфигурация в `appsettings.json`:

```json
"AI": {
  "Primary": {
    "Provider": "Ollama",
    "BaseUrl": "http://localhost:11434",
    "Model": "qwen3:8b"
  },
  "Fallback": {
    "Provider": "OpenAI",
    "Model": "gpt-4o-mini",
    "MaxRetries": 2
  }
}
```

---

## 8. Frontend

### Страницы

| Путь | Компонент | Описание |
|---|---|---|
| `/` | `Dashboard` | Сводка: блоки, слова к повторению, статистика |
| `/blocks` | `BlockList` | Все блоки с фильтрацией по языку/тегу |
| `/blocks/new` | `WordImport` | Ввод и форматирование слов |
| `/blocks/:id` | `BlockDetail` | Слова блока, управление |
| `/tests` | `TestList` | Список тестов |
| `/tests/new` | `TestCreate` | Выбор блоков и типов вопросов |
| `/tests/:id` | `TestRunner` | Прохождение теста |
| `/tests/:id/results` | `TestResults` | Результаты с разбором ошибок |
| `/review` | `ReviewSession` | Сессия spaced repetition |
| `/settings` | `Settings` | Языки, AI-настройки |

### Компонент WordImport (ключевой)

Флоу: textarea → кнопка «Форматировать» → SSE-стриминг → превью в таблице → редактирование → «Сохранить в блок».

```
┌─────────────────────────────────────────────────┐
│  Вставьте слова (любой формат)                  │
│  ┌───────────────────────────────────────────┐  │
│  │ biased -упереджений                       │  │
│  │ erode - підірвати?                        │  │
│  │ authenticity - автентичність              │  │
│  └───────────────────────────────────────────┘  │
│  Язык слов: [English ▼]  Язык переводов: [UK ▼] │
│  [✨ Форматировать через AI]                     │
└─────────────────────────────────────────────────┘

              ↓ (после форматирования)

┌─────────────────────────────────────────────────┐
│  Название блока: [Emotions & Social Behavior  ] │
│                                                  │
│  Слово              Перевод       Тип    ⚠️      │
│  ──────────────────────────────────────────────  │
│  biased             упереджений   word           │
│  erode              підірвати     word    ⚠️     │ ← confidenceFlag
│  authenticity       автентичність word           │
│  to commodify       комерціалізувати phrase       │
│  ...                                             │
│                                                  │
│  ⚠️ 2 слова требуют проверки                    │
│                                                  │
│  [← Назад]              [Сохранить блок →]      │
└─────────────────────────────────────────────────┘
```

Строки с `confidenceFlag: true` подсвечены желтым. Каждую строку можно редактировать inline.

### Компонент TestRunner

Поддерживаемые типы вопросов UI:

- **Single choice** — 4 кнопки-варианта, клик → подсветка правильного/неправильного
- **Multi select** — чекбоксы + кнопка «Проверить»
- **Fill in the blank** — поле ввода + нечёткая проверка (строчные буквы, лишние пробелы игнорируются)
- **Match pairs** — drag & drop (v2)

После каждого ответа показывается правильный ответ + `notes` слова если есть.

---

## 9. Тестирование (квизы)

### Типы вопросов

| Тип | Описание | Пример |
|---|---|---|
| `translate_to_native` | Перевести с иностранного | «Что значит *chuffed*?» → варианты |
| `translate_to_foreign` | Перевести с родного | «Как по-английски *вразливий*?» |
| `fill_in_sentence` | Слово в контексте | «She felt ___ after hearing the news» → pumped / dumbstruck / ... |
| `multi_select_theme` | Выбрать все слова темы | «Все слова связанные со страхом» → чекбоксы |
| `open_answer` | Открытый ответ | Ввести перевод вручную |

### Distractor-стратегия (решение уязвимости)

Для качественных ложных вариантов используется приоритетная выборка:

```
Приоритет 1: Слова из других блоков того же юзера (того же языка)
             → самые правдоподобные distractors
Приоритет 2: Слова из текущего блока (другие слова)
Приоритет 3: AI генерирует правдоподобные ложные переводы
             → только если слов в пуле < 3
```

Это означает, что база distractor'ов растёт по мере накопления слов пользователем.

### Дедупликация вопросов

Перед сохранением вопроса вычисляется `content_hash = SHA-256(questionType + questionText)`. Если хеш уже есть в тесте — вопрос заменяется новым. Это предотвращает повторяющиеся вопросы при регенерации.

### Оценка открытых ответов

Открытый ответ считается правильным если:
1. Точное совпадение (case-insensitive, trim)
2. Расстояние Левенштейна ≤ 2 символа (опечатки)
3. Совпадение любого из переводов если `translation` содержит «/» или «,»

```csharp
public bool IsOpenAnswerCorrect(string given, string correct)
{
    var g = given.Trim().ToLowerInvariant();
    var variants = correct.ToLowerInvariant()
        .Split(new[] { '/', ',', ';' }, StringSplitOptions.TrimEntries);

    return variants.Any(v =>
        v == g || LevenshteinDistance(v, g) <= 2);
}
```

---

## 10. Spaced Repetition

Алгоритм SM-2. Каждое слово имеет три поля: `ease_factor`, `interval_days`, `next_review_at`.

### Алгоритм

```csharp
public WordReviewUpdate CalculateNextReview(Word word, int quality)
{
    // quality: 0-5
    // 0-2: провал (слово не вспомнил)
    // 3: вспомнил с трудом
    // 4: вспомнил нормально
    // 5: идеально

    float newEase = word.EaseFactor +
        (0.1f - (5 - quality) * (0.08f + (5 - quality) * 0.02f));

    newEase = Math.Max(1.3f, newEase); // минимум 1.3

    int newInterval;
    int newRepetitions;

    if (quality < 3)
    {
        // Провал: сбросить интервал
        newRepetitions = 0;
        newInterval = 1;
    }
    else
    {
        newRepetitions = word.Repetitions + 1;
        newInterval = word.Repetitions switch
        {
            0 => 1,
            1 => 6,
            _ => (int)Math.Round(word.IntervalDays * newEase)
        };
    }

    return new WordReviewUpdate
    {
        EaseFactor = newEase,
        IntervalDays = newInterval,
        Repetitions = newRepetitions,
        NextReviewAt = DateTime.UtcNow.AddDays(newInterval)
    };
}
```

### Dashboard виджет

На главной странице показывается: «Сегодня к повторению: 12 слов» с кнопкой «Начать сессию». Сессия повторения — отдельный режим без таймера, фокус на одном слове. После оценки юзером (0-5) слово убирается из очереди.

---

## 11. Аутентификация и авторизация

### JWT-конфигурация

- Access token: 15 минут
- Refresh token: 30 дней, хранится в HttpOnly cookie (не в localStorage)
- Refresh token rotation: при каждом обновлении старый инвалидируется

### Защита эндпоинтов

Каждый эндпоинт проверяет `user_id` из JWT:

```csharp
// Пример — получить блок
public async Task<WordBlock> GetBlockAsync(Guid blockId, Guid requestingUserId)
{
    var block = await _db.WordBlocks
        .FirstOrDefaultAsync(b => b.Id == blockId)
        ?? throw new NotFoundException();

    // ОБЯЗАТЕЛЬНО: проверить владельца
    if (block.UserId != requestingUserId)
        throw new ForbiddenException();

    return block;
}
```

Middleware выносит эту проверку в общий слой — `ResourceOwnershipMiddleware` автоматически проверяет `user_id` для всех маршрутов `/api/blocks/{id}` и `/api/tests/{id}`.

---

## 12. Админ-панель

### Назначение

Админ-панель — отдельный защищённый раздел для управления платформой. Она не является частью пользовательского UI и доступна только пользователям с ролью `admin`. На старте (персональный проект) она нужна в первую очередь для мониторинга AI-слоя и управления пользователями. По мере роста платформы — становится инструментом контроля качества и контента.

### Роли

```sql
-- Добавить поле role в таблицу users
ALTER TABLE users ADD COLUMN role VARCHAR(20) NOT NULL DEFAULT 'user';
-- Возможные значения: 'user' | 'moderator' | 'admin'

CREATE INDEX idx_users_role ON users(role);
```

Middleware блокирует доступ к `/admin/*` для всех, кроме `admin` и `moderator` (с ограниченными правами).

### Разделы админ-панели

---

#### 12.1. Дашборд (обзор)

Главная страница с ключевыми метриками платформы в реальном времени:

```
┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│  Всего      │ │  Активных   │ │  Слов в БД  │ │  Тестов     │
│  юзеров     │ │  за 7 дней  │ │  (total)    │ │  создано    │
│  1 248      │ │     87      │ │  94 310     │ │   3 401     │
└─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘
```

- График регистраций по дням (последние 30 дней)
- График AI-вызовов: форматирование vs генерация тестов
- Топ-5 языков по количеству блоков
- Среднее количество слов на юзера

Реализация: отдельные `GET /api/admin/stats/*` эндпоинты с Redis-кэшем на 5 минут.

---

#### 12.2. Управление пользователями

**Список пользователей** с пагинацией и фильтрацией:

```
GET /api/admin/users?page=1&limit=50&role=user&q=serhii
```

Для каждого пользователя отображается:

| Поле | Описание |
|---|---|
| Email, display_name | Идентификация |
| Дата регистрации | Когда зарегистрировался |
| Последняя активность | Последний API-запрос |
| Блоков / Слов / Тестов | Сводка контента |
| Роль | user / moderator / admin |
| Статус | active / suspended / deleted |

**Действия над пользователем:**

- Изменить роль (`PATCH /api/admin/users/{id}/role`)
- Приостановить аккаунт — заблокировать логин без удаления данных (`PATCH /api/admin/users/{id}/suspend`)
- Удалить аккаунт с каскадным удалением данных (`DELETE /api/admin/users/{id}`)
- Сбросить пароль — отправить письмо с одноразовой ссылкой
- Войти от имени пользователя (impersonate) для диагностики проблем — с явной пометкой в логах

**Impersonation** реализуется через отдельный токен с claim `impersonated_by: adminId`. Все действия под impersonation логируются отдельно и не влияют на статистику активности юзера.

---

#### 12.3. Управление контентом

Просмотр блоков слов и отдельных слов по всем пользователям. Нужно для:

- Выявления спама или неприемлемого контента
- Диагностики проблем с AI-форматированием (почему слово попало в `confidenceFlag`)
- Ручной корректировки в крайних случаях

```
GET /api/admin/blocks?userId=...&lang=en&flagged=true
GET /api/admin/words?blockId=...
PATCH /api/admin/words/{id}       → ручное исправление
DELETE /api/admin/blocks/{id}     → удалить спам-блок
```

**Флаги контента:** Любой юзер может пожаловаться на некорректный публичный контент (в будущем — shared blocks). Модератор видит список жалоб в этом разделе.

---

#### 12.4. Мониторинг AI

Самый важный раздел на старте — позволяет понять, как работает AI-слой.

**Лог AI-вызовов:**

```sql
-- Новая таблица для логирования
CREATE TABLE ai_call_logs (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID        REFERENCES users(id) ON DELETE SET NULL,
    call_type       VARCHAR(30) NOT NULL,  -- 'format_words' | 'generate_test' | 'suggest_title'
    provider        VARCHAR(20) NOT NULL,  -- 'ollama' | 'openai'
    model           VARCHAR(50) NOT NULL,  -- 'qwen3:8b' | 'gpt-4o-mini'
    input_tokens    INT,
    output_tokens   INT,
    duration_ms     INT         NOT NULL,
    success         BOOLEAN     NOT NULL,
    error_message   TEXT,
    input_hash      VARCHAR(64),           -- SHA-256 входного текста (без самого текста)
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_ai_logs_created  ON ai_call_logs(created_at DESC);
CREATE INDEX idx_ai_logs_provider ON ai_call_logs(provider, success);
```

**Метрики в UI:**

- Среднее время ответа Ollama по типу запроса (график по часам)
- Процент успешных вызовов vs fallback на OpenAI
- Топ ошибок AI (группировка по `error_message`)
- Количество JSON-валидационных ошибок (сигнал проблемы с промптом)
- Стоимость OpenAI fallback (токены × цена `gpt-4o-mini`)

**Управление моделью:**

```
GET  /api/admin/ai/status          → статус Ollama (доступна / недоступна)
POST /api/admin/ai/model           → сменить активную модель
POST /api/admin/ai/test-prompt     → протестировать промпт прямо из UI
GET  /api/admin/ai/logs            → лог вызовов с фильтрацией
```

Раздел «Тест промпта» — форма с textarea для ввода сырого текста слов и кнопкой запуска. Ответ AI отображается с подсветкой JSON и метриками (время, токены). Позволяет отлаживать промпты без кода.

---

#### 12.5. Управление языками

Справочник языков — редко меняется, но нужен интерфейс для добавления новых без деплоя:

```
GET    /api/admin/languages
POST   /api/admin/languages      → { code: "pl", name: "Polish" }
PATCH  /api/admin/languages/{id} → включить/выключить язык
```

При добавлении нового языка — автоматическая проверка, что `qwen3:8b` его поддерживает (через тестовый AI-вызов).

---

#### 12.6. Системные настройки

Конфигурация без перезапуска сервера, хранится в БД:

| Ключ                            | Тип    | Описание                             |
| ------------------------------- | ------ | ------------------------------------ |
| `ai.primary_model`              | string | Активная модель Ollama               |
| `ai.fallback_enabled`           | bool   | Включён ли OpenAI fallback           |
| `ai.rate_limit_per_minute`      | int    | Лимит AI-запросов на юзера           |
| `features.registration_enabled` | bool   | Открыта ли регистрация               |
| `features.max_words_per_block`  | int    | Лимит слов в одном блоке             |
| `features.max_blocks_per_user`  | int    | Лимит блоков на юзера (0 = безлимит) |
| `test.max_questions`            | int    | Максимум вопросов в одном тесте      |
| `maintenance.enabled`           | bool   | Режим обслуживания (блокирует API)   |

```sql
CREATE TABLE system_settings (
    key         VARCHAR(100) PRIMARY KEY,
    value       TEXT         NOT NULL,
    value_type  VARCHAR(10)  NOT NULL,  -- 'string' | 'bool' | 'int'
    description TEXT,
    updated_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_by  UUID         REFERENCES users(id)
);
```

Настройки кэшируются в Redis с TTL 60 секунд. При изменении через UI — кэш инвалидируется немедленно.

---

#### 12.7. Логи и аудит

Все административные действия записываются в таблицу аудита:

```sql
CREATE TABLE audit_logs (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    admin_id    UUID        NOT NULL REFERENCES users(id),
    action      VARCHAR(100) NOT NULL,  -- 'user.suspend', 'settings.update', etc.
    target_type VARCHAR(50),            -- 'user', 'block', 'setting'
    target_id   TEXT,                   -- id объекта
    old_value   JSONB,                  -- состояние до
    new_value   JSONB,                  -- состояние после
    ip_address  INET,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

В UI — таблица с фильтрацией по дате, администратору, типу действия. Экспорт в CSV для compliance.

---

#### 12.8. Экспорт данных (GDPR)

При поступлении запроса от пользователя на выгрузку его данных:

```
POST /api/admin/gdpr/export/{userId}
```

Фоновая задача через Hangfire собирает все данные юзера (профиль, блоки, слова, тесты, попытки) в ZIP-архив с JSON-файлами и отправляет на email. Аналогично — запрос на удаление:

```
POST /api/admin/gdpr/delete/{userId}
```

Мягкое удаление (`status = 'deleted'`, данные анонимизируются) с полным удалением через 30 дней.

---

### Стек для админ-панели

Отдельный React-маршрут `/admin/*` в том же SPA. Компоненты:

- `recharts` — графики метрик
- `@tanstack/react-table` — таблицы с пагинацией и сортировкой
- Роль проверяется на фронте (скрыть навигацию) И на бэкенде (реальная защита)

Никакого отдельного деплоя — та же сборка, защищённая guard'ом маршрута.

---

## 13. Уязвимости и их решения

### 1. Качество AI-ответа

**Проблема:** Ollama может вернуть неполный JSON, неправильно разбить строки или потерять часть слов.

**Решение:**
- Явная инструкция в промпте: «Return ONLY valid JSON, no markdown, no preamble»
- Валидация на бэкенде: если распознано < 50% строк входного текста — ошибка + возврат сырого ответа AI для диагностики
- Retry-логика: 2 автоматических повтора с тем же промптом при невалидном JSON
- На фронте: показывать «сырой» результат AI с кнопкой «Редактировать вручную» как fallback

### 2. Distractor-голод на старте

**Проблема:** Когда слов мало (первые блоки), невозможно набрать 3 качественных ложных варианта для вопроса.

**Решение:**
- AI генерирует правдоподобные ложные переводы явно: «Generate 3 plausible but wrong translations for: *chuffed*»
- Минимальный порог для создания теста: 5 слов в выбранных блоках
- При < 10 слов — показывать только типы вопросов `translate_to_native` и `open_answer`

### 3. Повторяющиеся вопросы

**Проблема:** При повторной генерации теста по тем же блокам вопросы выходят одинаковые.

**Решение:**
- `content_hash` для каждого вопроса (SHA-256)
- При генерации передавать AI список уже использованных `questionTexts` из предыдущих тестов этого юзера по тем же блокам
- В промпте: «Do NOT generate questions with these texts: {usedQuestions}»

### 4. Медленная генерация — блокировка UI

**Проблема:** Ollama работает 5–30 секунд. Синхронный ответ заблокирует пользователя.

**Решение:**
- Форматирование слов: SSE стриминг — юзер видит прогресс в реальном времени
- Генерация теста: Hangfire фоновая задача → клиент поллит статус раз в 2 секунды → когда статус `ready`, тест становится доступен
- Индикаторы прогресса на каждом этапе
- Timeout 120 секунд; если превышен — пользователю предлагается использовать OpenAI

### 5. Отсутствие поиска при большом количестве слов

**Проблема:** После 500+ слов найти нужное без поиска невозможно.

**Решение:**
- Full-text search через PostgreSQL GIN-индекс с самого начала (`idx_words_fts`)
- Эндпоинт `GET /api/search?q=...&lang=en` ищет одновременно по `term` и `translation`
- На фронте — поле поиска с debounce 300ms

### 6. Безопасность: доступ к чужим данным

**Проблема:** Без проверки владельца любой авторизованный юзер может запросить чужие блоки по UUID.

**Решение:**
- `ResourceOwnershipMiddleware` автоматически проверяет `user_id` для всех `/api/blocks/{id}` и `/api/tests/{id}`
- В EF Core — глобальный query filter: `modelBuilder.Entity<WordBlock>().HasQueryFilter(b => b.UserId == _currentUserId)`
- Возвращать `404 Not Found` (а не `403 Forbidden`) чтобы не раскрывать существование ресурса

### 7. Rate limiting на AI-вызовы

**Проблема:** Один юзер может спамить запросами на форматирование, перегружая Ollama.

**Решение:**
- Redis-based rate limiting: 10 AI-запросов в минуту на пользователя
- Отдельный лимит на генерацию тестов: 5 тестов в час
- Hangfire очередь с одним воркером для генерации тестов — задачи выполняются последовательно

```csharp
services.AddRateLimiter(options =>
{
    options.AddPolicy("ai-format", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            context.User.GetUserId(),
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6
            }));
});
```

### 8. Потеря данных при редактировании

**Проблема:** Юзер отформатировал 30 слов, начал редактировать, случайно обновил страницу — всё потеряно.

**Решение:**
- Состояние форматирования сохраняется в `sessionStorage` браузера
- При перезагрузке страницы — предложение восстановить несохранённую сессию
- «Черновик» блока: auto-save в localStorage каждые 30 секунд

### 9. Языковая неоднозначность

**Проблема:** «erode» — слово неоднозначное, AI ставит `confidenceFlag: true`. Юзер не знает как исправить.

**Решение:**
- При `confidenceFlag: true` показывать `confidenceNote` с альтернативными переводами
- Кнопка «Предложить лучший перевод» — открывает модал с AI-подсказками
- Отдельный эндпоинт `POST /api/words/{id}/suggest-translation` — AI предлагает 3 варианта

### 10. Мобильная версия в будущем

**Проблема:** Если архитектуру не продумать сейчас, адаптация под мобилку потребует рефакторинга.

**Решение:**
- Весь бизнес-логика — в API, без завязки на Web-специфику
- Refresh token в HttpOnly cookie — работает и на мобилке через WebView, для нативного приложения — Keychain / Keystore
- Offline-режим: Service Worker кэширует блоки слов и тесты; ответы синхронизируются при восстановлении сети
- React Native c react-query — тот же стейт-менеджмент, переиспользование хуков

---

## 14. Дорожная карта (MVP → v2)

### MVP (Sprint 1–3, ~6 недель)

- [ ] Auth: регистрация, логин, JWT refresh
- [ ] Ввод слов → AI-форматирование через SSE → превью
- [ ] Сохранение блока, CRUD слов
- [ ] Генерация теста по блоку (2 типа вопросов: `translate_to_native`, `fill_in_sentence`)
- [ ] Прохождение теста, сохранение результатов
- [ ] Базовая страница статистики
- [ ] Админ-панель: дашборд, управление пользователями, мониторинг AI

### v1.1 (Sprint 4–5)

- [ ] Spaced repetition (SM-2) + сессия повторения
- [ ] Все 5 типов вопросов
- [ ] Теги для блоков
- [ ] Full-text поиск
- [ ] Export CSV / Import CSV
- [ ] Админ-панель: системные настройки, лог аудита

### v1.2 (Sprint 6)

- [ ] Тест по нескольким блокам одновременно
- [ ] Статистика по каждому слову (% правильных, время ответа)
- [ ] Напоминания (email / push) о словах к повторению
- [ ] Тёмная тема
- [ ] GDPR: экспорт и удаление данных пользователя

### v2.0

- [ ] React Native мобильное приложение
- [ ] Offline-режим (Service Worker / local SQLite)
- [ ] Тесты по грамматическим правилам (не только слова)
- [ ] Shared blocks — публичные блоки слов между пользователями
- [ ] AI-генерация примеров предложений для каждого слова
- [ ] Админ-панель: модерация shared blocks, жалобы

---

## Приложение: Пример полного сценария

```
1. Юзер открывает /blocks/new
2. Вставляет 18 слов в textarea (смешанный формат)
3. Выбирает: язык слов — English, язык переводов — Ukrainian
4. Нажимает "Форматировать через AI"

5. SSE: "Анализирую структуру..." → "Форматирую..." → результат
6. Превью: таблица с 18 строками, 2 строки подсвечены жёлтым (confidenceFlag)
7. AI предложила название: "Emotions & Reactions"
8. Юзер редактирует:
   - Исправляет перевод "erode" на "підривати"
   - Меняет название блока на "Emotions & Behavior"
9. Нажимает "Сохранить блок"

10. POST /api/blocks → 201 Created
11. POST /api/blocks/{id}/import → 200 OK

12. Юзер переходит на /tests/new
13. Выбирает блок "Emotions & Behavior"
14. Выбирает типы: translate_to_native + fill_in_sentence
15. 20 вопросов → нажимает "Создать тест"

16. POST /api/tests/generate → { testId, status: "generating" }
17. Hangfire запускает генерацию фоново
18. Через 12 секунд: GET /api/tests/{id} → status: "ready"
19. Юзер начинает тест → TestRunner

20. Проходит 20 вопросов, получает 85%
21. Результаты: 17/20, ошибки показаны с правильными ответами
22. Система обновляет next_review_at для 3 слов на завтра
```
