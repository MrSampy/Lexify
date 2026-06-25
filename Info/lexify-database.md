# Lexify — Структура базы данных

> PostgreSQL 16. Все таблицы, поля, типы, ограничения, индексы, триггеры и политики безопасности.

---

## Содержание

1. [Обзор и принципы](#1-обзор-и-принципы)
2. [ERD — Диаграмма связей](#2-erd--диаграмма-связей)
3. [Группы таблиц](#3-группы-таблиц)
4. [Таблицы — Пользователи и аутентификация](#4-таблицы--пользователи-и-аутентификация)
   - 4.1 [users](#41-users)
   - 4.2 [refresh_tokens](#42-refresh_tokens)
5. [Таблицы — Справочники](#5-таблицы--справочники)
   - 5.1 [languages](#51-languages)
   - 5.2 [tags](#52-tags)
6. [Таблицы — Контент (слова и блоки)](#6-таблицы--контент-слова-и-блоки)
   - 6.1 [word_blocks](#61-word_blocks)
   - 6.2 [block_tags](#62-block_tags)
   - 6.3 [words](#63-words)
7. [Таблицы — Тестирование](#7-таблицы--тестирование)
   - 7.1 [tests](#71-tests)
   - 7.2 [test_blocks](#72-test_blocks)
   - 7.3 [questions](#73-questions)
   - 7.4 [question_options](#74-question_options)
   - 7.5 [test_attempts](#75-test_attempts)
   - 7.6 [attempt_answers](#76-attempt_answers)
8. [Таблицы — Служебные и административные](#8-таблицы--служебные-и-административные)
   - 8.1 [ai_call_logs](#81-ai_call_logs)
   - 8.2 [system_settings](#82-system_settings)
   - 8.3 [audit_logs](#83-audit_logs)
9. [Индексы](#9-индексы)
10. [Триггеры и функции](#10-триггеры-и-функции)
11. [Политики Row-Level Security](#11-политики-row-level-security)
12. [Enum-типы](#12-enum-типы)
13. [Полная миграция (DDL)](#13-полная-миграция-ddl)
14. [Сидовые данные](#14-сидовые-данные)
15. [Советы по производительности](#15-советы-по-производительности)

---

## 1. Обзор и принципы

### СУБД и расширения

```sql
-- Расширения, необходимые до создания схемы
CREATE EXTENSION IF NOT EXISTS "pgcrypto";   -- gen_random_uuid()
CREATE EXTENSION IF NOT EXISTS "pg_trgm";    -- нечёткий поиск ILIKE с индексом
CREATE EXTENSION IF NOT EXISTS "unaccent";   -- поиск без учёта диакритики (å, ø, ü)
```

### Принципы проектирования

| Принцип | Применение |
|---|---|
| **UUID как PK** | Все основные таблицы используют `gen_random_uuid()`. Безопасно для публичных API, не раскрывает порядок создания записей. |
| **SERIAL для справочников** | `languages`, `tags` — небольшие таблицы, где читаемость числового ID важнее. |
| **TIMESTAMPTZ везде** | Все временные поля в UTC с timezone. Не `TIMESTAMP`. |
| **Soft delete через статус** | Пользователи не удаляются физически, только `status = 'deleted'`. Данные анонимизируются через 30 дней. |
| **Денормализация там, где нужна скорость** | `word_count` в `word_blocks` поддерживается триггером. Не делать COUNT на каждый список. |
| **CASCADE по умолчанию** | Удаление пользователя удаляет всё его: блоки → слова → тесты → попытки. |
| **NOT NULL по умолчанию** | NULL допускается только там, где семантика требует «отсутствие значения». |

### Соглашения по именованию

```
Таблицы:         snake_case, множественное число          (word_blocks, test_attempts)
Поля:            snake_case, единственное число            (user_id, created_at)
Индексы:         idx_{table}_{column(s)}                  (idx_words_block_id)
Уникальные:      uq_{table}_{column(s)}                   (uq_users_email)
Foreign Keys:    fk_{table}_{referenced_table}            (fk_words_block)
Триггеры:        trg_{table}_{action}                     (trg_words_word_count)
Функции:         fn_{purpose}                             (fn_update_word_count)
```

---

## 2. ERD — Диаграмма связей

```
┌──────────────┐         ┌───────────────┐
│   languages  │         │  system_       │
│  ─────────── │         │  settings     │
│  id (PK)     │         └───────────────┘
│  code        │
│  name        │    ┌─────────────────────────────────────────┐
│  is_active   │    │               users                      │
└──────┬───────┘    │  ─────────────────────────────────────── │
       │ 1          │  id (PK)          email (UQ)             │
       │            │  password_hash    display_name           │
       │            │  role             status                 │
       │            │  last_active_at   created_at             │
       │            └──────────────────┬──────────────────────┘
       │                               │ 1
       │                    ┌──────────┼────────────────┐
       │                    │          │                │
       │                   1│         1│               1│
       │            ┌───────▼──┐  ┌───▼──────┐  ┌─────▼──────┐
       │            │  tags    │  │  tests   │  │  refresh_  │
       │            │  ─────── │  │  ─────── │  │  tokens    │
       │            │  id (PK) │  │  id (PK) │  └────────────┘
       │            │  user_id │  │  user_id │
       │            │  name    │  │  title   │
       │            └────┬─────┘  │  status  │
       │                 │ N      └────┬─────┘
       │                 │            │ 1
       │            ┌────▼──────┐    N│
       │            │block_tags │ ┌───▼──────────┐
       │            │  ──────── │ │  test_blocks │
       │            │block_id   │ │  ─────────── │
       │            │tag_id     │ │  test_id     │
       │            └────┬──────┘ │  block_id    │
       │                 │        └──────────────┘
       │                 │ N              N↑
       │ N        ┌──────▼───────────────┐│
       └──────────►    word_blocks        ◄┘
                  │  ────────────────────  │
                  │  id (PK)               │
                  │  user_id (FK)          │
                  │  language_id (FK)      │
                  │  title                 │
                  │  word_count (denorm)   │
                  └──────────┬────────────┘
                             │ 1
                             │ N
                  ┌──────────▼────────────┐
                  │        words           │
                  │  ──────────────────── │
                  │  id (PK)              │
                  │  block_id (FK)        │
                  │  term                 │
                  │  translation          │
                  │  word_type            │
                  │  confidence_flag      │
                  │  ease_factor (SM-2)   │
                  │  interval_days (SM-2) │
                  │  next_review_at       │
                  └──────────┬────────────┘
                             │ 1
                             │ N (nullable)
                  ┌──────────▼────────────┐
                  │       questions        │
                  │  ──────────────────── │
                  │  id (PK)              │
                  │  test_id (FK)         │
                  │  word_id (FK,NULL)    │
                  │  question_type        │
                  │  question_text        │
                  │  correct_answer       │
                  │  content_hash (UQ)    │
                  └──────────┬────────────┘
                             │ 1         │ 1
                          N  │           │ N
              ┌──────────────▼──┐  ┌─────▼──────────────┐
              │ question_options│  │   test_attempts     │
              │  ────────────── │  │  ────────────────── │
              │  id (PK)        │  │  id (PK)            │
              │  question_id    │  │  test_id (FK)       │
              │  option_text    │  │  user_id (FK)       │
              │  is_correct     │  │  score              │
              └─────────────────┘  └──────────┬──────────┘
                                              │ 1
                                              │ N
                                   ┌──────────▼──────────┐
                                   │   attempt_answers   │
                                   │  ────────────────── │
                                   │  id (PK)            │
                                   │  attempt_id (FK)    │
                                   │  question_id (FK)   │
                                   │  given_answer       │
                                   │  is_correct         │
                                   │  time_spent_ms      │
                                   └─────────────────────┘
```

**Служебные таблицы** (без прямых связей с основными):

```
ai_call_logs   → логирование AI-вызовов (user_id FK nullable)
audit_logs     → аудит действий администраторов (admin_id FK)
system_settings → конфигурация приложения (key-value)
```

---

## 3. Группы таблиц

| Группа | Таблицы | Назначение |
|---|---|---|
| **Auth** | `users`, `refresh_tokens` | Аккаунты и сессии |
| **Справочники** | `languages`, `tags` | Неизменяемые или редко меняемые данные |
| **Контент** | `word_blocks`, `block_tags`, `words` | Основные данные приложения |
| **Тестирование** | `tests`, `test_blocks`, `questions`, `question_options`, `test_attempts`, `attempt_answers` | Квизы и их прохождение |
| **Служебные** | `ai_call_logs`, `system_settings`, `audit_logs` | Мониторинг и администрирование |

---

## 4. Таблицы — Пользователи и аутентификация

### 4.1 `users`

Центральная таблица. Все основные сущности привязаны к `users.id`.

```sql
CREATE TABLE users (
    id               UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    email            VARCHAR(255) NOT NULL,
    password_hash    TEXT         NOT NULL,
    display_name     VARCHAR(100),
    role             VARCHAR(20)  NOT NULL DEFAULT 'user',
        -- 'user' | 'moderator' | 'admin'
    status           VARCHAR(20)  NOT NULL DEFAULT 'active',
        -- 'active' | 'suspended' | 'deleted'
    last_active_at   TIMESTAMPTZ,                -- обновляется при каждом API-запросе
    deleted_at       TIMESTAMPTZ,                -- заполняется при soft delete
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_users_email   UNIQUE (email),
    CONSTRAINT chk_users_role   CHECK (role   IN ('user', 'moderator', 'admin')),
    CONSTRAINT chk_users_status CHECK (status IN ('active', 'suspended', 'deleted'))
);
```

| Поле | Тип | NULL | Описание |
|---|---|---|---|
| `id` | UUID | NO | PK, генерируется автоматически |
| `email` | VARCHAR(255) | NO | Уникальный, нормализован в lowercase перед сохранением |
| `password_hash` | TEXT | NO | bcrypt, cost factor 12 |
| `display_name` | VARCHAR(100) | YES | Отображаемое имя, необязательное |
| `role` | VARCHAR(20) | NO | Роль: `user` / `moderator` / `admin` |
| `status` | VARCHAR(20) | NO | Статус аккаунта |
| `last_active_at` | TIMESTAMPTZ | YES | Последний успешный API-запрос |
| `deleted_at` | TIMESTAMPTZ | YES | Дата soft delete, NULL если активен |
| `created_at` | TIMESTAMPTZ | NO | Дата регистрации |
| `updated_at` | TIMESTAMPTZ | NO | Обновляется триггером |

**Индексы:**

```sql
CREATE UNIQUE INDEX uq_users_email    ON users (LOWER(email));
CREATE INDEX idx_users_role           ON users (role);
CREATE INDEX idx_users_status         ON users (status) WHERE status != 'active';
CREATE INDEX idx_users_last_active    ON users (last_active_at DESC NULLS LAST);
```

**Политика soft delete:**

При `status = 'deleted'` данные пользователя анонимизируются через 30 дней фоновой задачей:

```sql
-- Фоновая задача Hangfire вызывает эту функцию раз в сутки
CREATE OR REPLACE FUNCTION fn_anonymize_deleted_users()
RETURNS void AS $$
BEGIN
    UPDATE users
    SET
        email        = 'deleted_' || id || '@removed.invalid',
        display_name = NULL,
        password_hash = 'DELETED'
    WHERE
        status     = 'deleted'
        AND deleted_at < NOW() - INTERVAL '30 days'
        AND email NOT LIKE 'deleted_%';
END;
$$ LANGUAGE plpgsql;
```

---

### 4.2 `refresh_tokens`

Хранит refresh-токены для ротации JWT. При каждом обновлении access token старый refresh-токен инвалидируется.

```sql
CREATE TABLE refresh_tokens (
    id           UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id      UUID         NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash   VARCHAR(64)  NOT NULL,
        -- SHA-256 от самого токена; токен хранится только на клиенте (HttpOnly cookie)
    expires_at   TIMESTAMPTZ  NOT NULL,
    revoked_at   TIMESTAMPTZ,                -- NULL = активный, дата = отозванный
    replaced_by  UUID         REFERENCES refresh_tokens(id),
        -- цепочка ротации: старый токен знает о своей замене
    ip_address   INET,
    user_agent   TEXT,
    created_at   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_refresh_token_hash UNIQUE (token_hash)
);
```

| Поле | Тип | NULL | Описание |
|---|---|---|---|
| `id` | UUID | NO | PK |
| `user_id` | UUID | NO | FK → `users.id` CASCADE |
| `token_hash` | VARCHAR(64) | NO | SHA-256 от токена. Токен на клиенте, хэш в БД |
| `expires_at` | TIMESTAMPTZ | NO | Срок жизни: 30 дней |
| `revoked_at` | TIMESTAMPTZ | YES | NULL = активный |
| `replaced_by` | UUID | YES | ID нового токена при ротации |
| `ip_address` | INET | YES | IP при выдаче (для аудита) |
| `user_agent` | TEXT | YES | User-Agent браузера/приложения |

**Индексы:**

```sql
CREATE UNIQUE INDEX uq_refresh_token_hash ON refresh_tokens (token_hash);
CREATE INDEX idx_refresh_tokens_user      ON refresh_tokens (user_id);
CREATE INDEX idx_refresh_tokens_active    ON refresh_tokens (user_id, expires_at)
    WHERE revoked_at IS NULL;
```

**Очистка истёкших токенов** — фоновая задача раз в сутки:

```sql
CREATE OR REPLACE FUNCTION fn_cleanup_refresh_tokens()
RETURNS void AS $$
BEGIN
    DELETE FROM refresh_tokens
    WHERE expires_at < NOW() - INTERVAL '7 days';
        -- 7 дней буфер для аудита после истечения
END;
$$ LANGUAGE plpgsql;
```

---

## 5. Таблицы — Справочники

### 5.1 `languages`

Список поддерживаемых языков. Управляется через админ-панель.

```sql
CREATE TABLE languages (
    id          SMALLSERIAL  PRIMARY KEY,
    code        VARCHAR(5)   NOT NULL,
        -- BCP 47: 'en', 'no', 'uk', 'ru', 'de', 'pl' и т.д.
    name        VARCHAR(50)  NOT NULL,
    native_name VARCHAR(50)  NOT NULL,
        -- Название на самом языке: 'English', 'Norsk', 'Українська'
    is_active   BOOLEAN      NOT NULL DEFAULT TRUE,
        -- Неактивные языки скрыты в UI, но данные остаются
    sort_order  SMALLINT     NOT NULL DEFAULT 0,

    CONSTRAINT uq_languages_code UNIQUE (code),
    CONSTRAINT chk_languages_code CHECK (code ~ '^[a-z]{2,5}$')
);
```

| Поле | Тип | NULL | Описание |
|---|---|---|---|
| `id` | SMALLSERIAL | NO | PK (маленький int, не UUID — это справочник) |
| `code` | VARCHAR(5) | NO | BCP 47 код языка |
| `name` | VARCHAR(50) | NO | Название на английском |
| `native_name` | VARCHAR(50) | NO | Название на родном языке |
| `is_active` | BOOLEAN | NO | Отображать ли язык в UI |
| `sort_order` | SMALLINT | NO | Порядок в выпадающем списке |

**Индексы:**

```sql
CREATE UNIQUE INDEX uq_languages_code ON languages (code);
CREATE INDEX idx_languages_active     ON languages (is_active, sort_order);
```

---

### 5.2 `tags`

Пользовательские теги для блоков слов. Принадлежат конкретному пользователю.

```sql
CREATE TABLE tags (
    id       SERIAL       PRIMARY KEY,
    user_id  UUID         NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name     VARCHAR(50)  NOT NULL,
        -- нормализуется: trim, lowercase, без символа #
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_tags_user_name UNIQUE (user_id, name),
    CONSTRAINT chk_tags_name CHECK (name ~ '^[a-z0-9а-яёіїєa-z_-]{1,50}$')
);
```

| Поле | Тип | NULL | Описание |
|---|---|---|---|
| `id` | SERIAL | NO | PK |
| `user_id` | UUID | NO | FK → `users.id` CASCADE |
| `name` | VARCHAR(50) | NO | Нормализованное имя тега |
| `created_at` | TIMESTAMPTZ | NO | Дата создания |

**Индексы:**

```sql
CREATE UNIQUE INDEX uq_tags_user_name ON tags (user_id, name);
CREATE INDEX idx_tags_user_id         ON tags (user_id);
```

---

## 6. Таблицы — Контент (слова и блоки)

### 6.1 `word_blocks`

Тематический блок слов. Один пользователь — много блоков. Один блок — один язык.

```sql
CREATE TABLE word_blocks (
    id           UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id      UUID          NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    language_id  SMALLINT      NOT NULL REFERENCES languages(id),
    title        VARCHAR(200)  NOT NULL,
        -- Предлагается AI, может быть изменено пользователем
    description  TEXT,
    word_count   INT           NOT NULL DEFAULT 0,
        -- Денормализовано: поддерживается триггером trg_words_word_count
    created_at   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_word_blocks_title CHECK (LENGTH(TRIM(title)) > 0),
    CONSTRAINT chk_word_blocks_count CHECK (word_count >= 0)
);
```

| Поле | Тип | NULL | Описание |
|---|---|---|---|
| `id` | UUID | NO | PK |
| `user_id` | UUID | NO | FK → `users.id` CASCADE |
| `language_id` | SMALLINT | NO | FK → `languages.id` |
| `title` | VARCHAR(200) | NO | Название блока |
| `description` | TEXT | YES | Необязательное описание |
| `word_count` | INT | NO | Счётчик слов, обновляется триггером |
| `created_at` | TIMESTAMPTZ | NO | Дата создания |
| `updated_at` | TIMESTAMPTZ | NO | Обновляется триггером |

**Индексы:**

```sql
CREATE INDEX idx_word_blocks_user_id     ON word_blocks (user_id);
CREATE INDEX idx_word_blocks_language    ON word_blocks (user_id, language_id);
CREATE INDEX idx_word_blocks_updated     ON word_blocks (user_id, updated_at DESC);
```

---

### 6.2 `block_tags`

Связующая таблица M:N между блоками и тегами.

```sql
CREATE TABLE block_tags (
    block_id  UUID     NOT NULL REFERENCES word_blocks(id) ON DELETE CASCADE,
    tag_id    INTEGER  NOT NULL REFERENCES tags(id) ON DELETE CASCADE,

    PRIMARY KEY (block_id, tag_id)
);
```

| Поле | Тип | NULL | Описание |
|---|---|---|---|
| `block_id` | UUID | NO | FK → `word_blocks.id` CASCADE |
| `tag_id` | INTEGER | NO | FK → `tags.id` CASCADE |

**Индексы:**

```sql
-- PK уже создаёт индекс на (block_id, tag_id)
CREATE INDEX idx_block_tags_tag ON block_tags (tag_id);
    -- Позволяет быстро находить все блоки по тегу
```

---

### 6.3 `words`

Основная таблица данных. Хранит слова, переводы, метаданные и SM-2 параметры для spaced repetition.

```sql
CREATE TABLE words (
    id                UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    block_id          UUID          NOT NULL REFERENCES word_blocks(id) ON DELETE CASCADE,

    -- Лингвистические данные
    term              TEXT          NOT NULL,
        -- Оригинальное слово или фраза на изучаемом языке
    translation       TEXT          NOT NULL,
        -- Перевод на родной язык пользователя
    word_type         VARCHAR(20)   NOT NULL DEFAULT 'word',
        -- 'word' | 'phrase' | 'idiom' | 'expression'
    notes             TEXT,
        -- Контекст, комментарии пользователя
    example_sentence  TEXT,
        -- Пример предложения (может быть добавлен AI)
    confidence_flag   BOOLEAN       NOT NULL DEFAULT FALSE,
        -- TRUE: AI пометила перевод как неоднозначный ('erode - підірвати?')
    confidence_note   TEXT,
        -- Текст пояснения от AI: 'Может быть: підривати, розмивати'
    sort_order        INT           NOT NULL DEFAULT 0,
    created_at        TIMESTAMPTZ   NOT NULL DEFAULT NOW(),

    -- SM-2 Spaced Repetition параметры
    ease_factor       FLOAT         NOT NULL DEFAULT 2.5,
        -- Коэффициент лёгкости [1.3, ∞). Default 2.5. Снижается при ошибках.
    interval_days     INT           NOT NULL DEFAULT 1,
        -- Текущий интервал до следующего повторения в днях
    repetitions       INT           NOT NULL DEFAULT 0,
        -- Сколько раз подряд ответ был успешным (quality >= 3)
    next_review_at    TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
        -- Когда слово снова появится в сессии повторения

    CONSTRAINT chk_words_type      CHECK (word_type IN ('word', 'phrase', 'idiom', 'expression')),
    CONSTRAINT chk_words_ease      CHECK (ease_factor >= 1.3),
    CONSTRAINT chk_words_interval  CHECK (interval_days >= 1),
    CONSTRAINT chk_words_reps      CHECK (repetitions >= 0),
    CONSTRAINT chk_words_term      CHECK (LENGTH(TRIM(term)) > 0),
    CONSTRAINT chk_words_trans     CHECK (LENGTH(TRIM(translation)) > 0)
);
```

| Поле | Тип | NULL | Описание |
|---|---|---|---|
| `id` | UUID | NO | PK |
| `block_id` | UUID | NO | FK → `word_blocks.id` CASCADE |
| `term` | TEXT | NO | Слово/фраза на изучаемом языке |
| `translation` | TEXT | NO | Перевод. Может содержать `/` для вариантов |
| `word_type` | VARCHAR(20) | NO | Тип: word / phrase / idiom / expression |
| `notes` | TEXT | YES | Контекст и пояснения |
| `example_sentence` | TEXT | YES | Пример использования в предложении |
| `confidence_flag` | BOOLEAN | NO | AI была неуверена в переводе |
| `confidence_note` | TEXT | YES | Альтернативные варианты от AI |
| `sort_order` | INT | NO | Порядок внутри блока |
| `created_at` | TIMESTAMPTZ | NO | Дата добавления |
| `ease_factor` | FLOAT | NO | SM-2: коэффициент лёгкости [1.3, ∞) |
| `interval_days` | INT | NO | SM-2: интервал в днях |
| `repetitions` | INT | NO | SM-2: счётчик успешных повторений подряд |
| `next_review_at` | TIMESTAMPTZ | NO | SM-2: дата следующего повторения |

**Индексы:**

```sql
CREATE INDEX idx_words_block_id    ON words (block_id);
CREATE INDEX idx_words_sort        ON words (block_id, sort_order);
CREATE INDEX idx_words_created     ON words (block_id, created_at DESC);

-- Частичный индекс для spaced repetition — только «просроченные» слова
CREATE INDEX idx_words_due_review  ON words (next_review_at)
    WHERE next_review_at <= NOW();

-- Частичный индекс для фильтрации по confidence_flag в админке
CREATE INDEX idx_words_confidence  ON words (block_id)
    WHERE confidence_flag = TRUE;

-- Full-text search: unaccent снимает диакритику (å→a, ø→o, ї→и)
CREATE INDEX idx_words_fts ON words USING GIN (
    to_tsvector('simple',
        unaccent(term) || ' ' || unaccent(translation)
    )
);

-- Trigram-индекс для быстрого ILIKE-поиска (pg_trgm)
CREATE INDEX idx_words_term_trgm        ON words USING GIN (term gin_trgm_ops);
CREATE INDEX idx_words_translation_trgm ON words USING GIN (translation gin_trgm_ops);
```

---

## 7. Таблицы — Тестирование

### 7.1 `tests`

Тест по одному или нескольким блокам слов. Создаётся фоновой задачей через Hangfire.

```sql
CREATE TABLE tests (
    id          UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     UUID          NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title       VARCHAR(200)  NOT NULL,
    status      VARCHAR(20)   NOT NULL DEFAULT 'generating',
        -- 'generating' | 'ready' | 'archived'
    question_count INT,
        -- Заполняется после генерации
    created_at  TIMESTAMPTZ   NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_tests_status CHECK (status IN ('generating', 'ready', 'archived'))
);
```

| Поле | Тип | NULL | Описание |
|---|---|---|---|
| `id` | UUID | NO | PK |
| `user_id` | UUID | NO | FK → `users.id` CASCADE |
| `title` | VARCHAR(200) | NO | Название теста |
| `status` | VARCHAR(20) | NO | Статус генерации |
| `question_count` | INT | YES | NULL пока `status = 'generating'` |
| `created_at` | TIMESTAMPTZ | NO | Дата создания |

**Индексы:**

```sql
CREATE INDEX idx_tests_user_id ON tests (user_id);
CREATE INDEX idx_tests_status  ON tests (user_id, status);
CREATE INDEX idx_tests_created ON tests (user_id, created_at DESC);
```

---

### 7.2 `test_blocks`

Связующая таблица M:N: какие блоки слов включены в тест.

```sql
CREATE TABLE test_blocks (
    test_id   UUID  NOT NULL REFERENCES tests(id) ON DELETE CASCADE,
    block_id  UUID  NOT NULL REFERENCES word_blocks(id) ON DELETE CASCADE,

    PRIMARY KEY (test_id, block_id)
);
```

| Поле | Тип | NULL | Описание |
|---|---|---|---|
| `test_id` | UUID | NO | FK → `tests.id` CASCADE |
| `block_id` | UUID | NO | FK → `word_blocks.id` CASCADE |

**Индексы:**

```sql
CREATE INDEX idx_test_blocks_block ON test_blocks (block_id);
```

---

### 7.3 `questions`

Один вопрос теста. Привязан к исходному слову (`word_id`) для обновления SM-2 после теста.

```sql
CREATE TABLE questions (
    id              UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    test_id         UUID          NOT NULL REFERENCES tests(id) ON DELETE CASCADE,
    word_id         UUID          REFERENCES words(id) ON DELETE SET NULL,
        -- NULL: слово удалено, но вопрос остаётся в истории
    question_type   VARCHAR(30)   NOT NULL,
        -- 'translate_to_native'  — выбрать перевод иностранного слова
        -- 'translate_to_foreign' — выбрать слово по переводу
        -- 'fill_in_sentence'     — вставить слово в предложение
        -- 'multi_select_theme'   — выбрать все слова по теме
        -- 'open_answer'          — вписать перевод вручную
    question_text   TEXT          NOT NULL,
    correct_answer  TEXT          NOT NULL,
    sort_order      INT           NOT NULL DEFAULT 0,
    content_hash    VARCHAR(64)   NOT NULL,
        -- SHA-256(question_type || '|' || question_text)
        -- Используется для дедупликации при регенерации

    CONSTRAINT chk_questions_type CHECK (
        question_type IN (
            'translate_to_native', 'translate_to_foreign',
            'fill_in_sentence', 'multi_select_theme', 'open_answer'
        )
    )
);
```

| Поле | Тип | NULL | Описание |
|---|---|---|---|
| `id` | UUID | NO | PK |
| `test_id` | UUID | NO | FK → `tests.id` CASCADE |
| `word_id` | UUID | YES | FK → `words.id` SET NULL при удалении слова |
| `question_type` | VARCHAR(30) | NO | Тип вопроса |
| `question_text` | TEXT | NO | Текст вопроса |
| `correct_answer` | TEXT | NO | Правильный ответ (строка) |
| `sort_order` | INT | NO | Порядок в тесте |
| `content_hash` | VARCHAR(64) | NO | SHA-256 для дедупликации |

**Индексы:**

```sql
CREATE INDEX idx_questions_test_id      ON questions (test_id);
CREATE INDEX idx_questions_word_id      ON questions (word_id) WHERE word_id IS NOT NULL;
CREATE INDEX idx_questions_hash         ON questions (content_hash);
CREATE INDEX idx_questions_sort         ON questions (test_id, sort_order);
```

---

### 7.4 `question_options`

Варианты ответов для вопросов с выбором (single choice, multi-select). Для `open_answer` строк нет.

```sql
CREATE TABLE question_options (
    id           UUID      PRIMARY KEY DEFAULT gen_random_uuid(),
    question_id  UUID      NOT NULL REFERENCES questions(id) ON DELETE CASCADE,
    option_text  TEXT      NOT NULL,
    is_correct   BOOLEAN   NOT NULL DEFAULT FALSE,
    sort_order   INT       NOT NULL DEFAULT 0
);
```

| Поле | Тип | NULL | Описание |
|---|---|---|---|
| `id` | UUID | NO | PK |
| `question_id` | UUID | NO | FK → `questions.id` CASCADE |
| `option_text` | TEXT | NO | Текст варианта ответа |
| `is_correct` | BOOLEAN | NO | Правильный ли этот вариант |
| `sort_order` | INT | NO | Порядок отображения |

> Для `translate_to_native` и `translate_to_foreign` — 4 варианта, 1 правильный.  
> Для `multi_select_theme` — 6–8 вариантов, 2–4 правильных.

**Индексы:**

```sql
CREATE INDEX idx_question_options_question ON question_options (question_id);
CREATE INDEX idx_question_options_correct  ON question_options (question_id) WHERE is_correct = TRUE;
```

---

### 7.5 `test_attempts`

Одна попытка прохождения теста. Один тест можно пройти несколько раз.

```sql
CREATE TABLE test_attempts (
    id               UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    test_id          UUID          NOT NULL REFERENCES tests(id) ON DELETE CASCADE,
    user_id          UUID          NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    started_at       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    finished_at      TIMESTAMPTZ,
        -- NULL: попытка ещё не завершена
    score            FLOAT,
        -- Процент правильных ответов [0.0, 1.0]. NULL до завершения.
    total_questions  INT,
    correct_answers  INT,

    CONSTRAINT chk_attempts_score CHECK (score IS NULL OR (score >= 0.0 AND score <= 1.0)),
    CONSTRAINT chk_attempts_counts CHECK (
        (total_questions IS NULL AND correct_answers IS NULL)
        OR (total_questions >= 0 AND correct_answers >= 0
            AND correct_answers <= total_questions)
    )
);
```

| Поле | Тип | NULL | Описание |
|---|---|---|---|
| `id` | UUID | NO | PK |
| `test_id` | UUID | NO | FK → `tests.id` CASCADE |
| `user_id` | UUID | NO | FK → `users.id` CASCADE |
| `started_at` | TIMESTAMPTZ | NO | Время начала |
| `finished_at` | TIMESTAMPTZ | YES | NULL = не завершена |
| `score` | FLOAT | YES | Результат [0.0..1.0] |
| `total_questions` | INT | YES | Всего вопросов |
| `correct_answers` | INT | YES | Правильных ответов |

**Индексы:**

```sql
CREATE INDEX idx_attempts_test_id    ON test_attempts (test_id);
CREATE INDEX idx_attempts_user_id    ON test_attempts (user_id);
CREATE INDEX idx_attempts_started    ON test_attempts (user_id, started_at DESC);
CREATE INDEX idx_attempts_incomplete ON test_attempts (user_id)
    WHERE finished_at IS NULL;
```

---

### 7.6 `attempt_answers`

Каждый ответ пользователя в рамках попытки. Используется для детального разбора ошибок и статистики.

```sql
CREATE TABLE attempt_answers (
    id             UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    attempt_id     UUID          NOT NULL REFERENCES test_attempts(id) ON DELETE CASCADE,
    question_id    UUID          NOT NULL REFERENCES questions(id) ON DELETE CASCADE,
    given_answer   TEXT          NOT NULL,
        -- Для single/multi: текст выбранного варианта или JSON-массив
        -- Для open_answer: введённый пользователем текст
    is_correct     BOOLEAN       NOT NULL,
    time_spent_ms  INT,
        -- Время ответа в мс. Используется для выявления «сложных» слов.
    answered_at    TIMESTAMPTZ   NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_attempt_question UNIQUE (attempt_id, question_id),
        -- Один ответ на вопрос в рамках попытки
    CONSTRAINT chk_answers_time CHECK (time_spent_ms IS NULL OR time_spent_ms >= 0)
);
```

| Поле | Тип | NULL | Описание |
|---|---|---|---|
| `id` | UUID | NO | PK |
| `attempt_id` | UUID | NO | FK → `test_attempts.id` CASCADE |
| `question_id` | UUID | NO | FK → `questions.id` CASCADE |
| `given_answer` | TEXT | NO | Ответ пользователя |
| `is_correct` | BOOLEAN | NO | Правильный ли ответ |
| `time_spent_ms` | INT | YES | Время ответа в миллисекундах |
| `answered_at` | TIMESTAMPTZ | NO | Временная метка ответа |

**Индексы:**

```sql
CREATE INDEX idx_attempt_answers_attempt   ON attempt_answers (attempt_id);
CREATE INDEX idx_attempt_answers_question  ON attempt_answers (question_id);
CREATE INDEX idx_attempt_answers_wrong     ON attempt_answers (attempt_id)
    WHERE is_correct = FALSE;
```

---

## 8. Таблицы — Служебные и административные

### 8.1 `ai_call_logs`

Лог каждого вызова AI. Источник данных для мониторинга в админ-панели.

```sql
CREATE TABLE ai_call_logs (
    id             UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id        UUID          REFERENCES users(id) ON DELETE SET NULL,
        -- NULL: пользователь удалён, но лог остаётся для статистики
    call_type      VARCHAR(30)   NOT NULL,
        -- 'format_words' | 'generate_test' | 'suggest_title'
    provider       VARCHAR(20)   NOT NULL,
        -- 'ollama' | 'openai'
    model          VARCHAR(50)   NOT NULL,
        -- 'qwen3:8b' | 'gpt-4o-mini' и т.д.
    input_tokens   INT,
    output_tokens  INT,
    duration_ms    INT           NOT NULL,
    success        BOOLEAN       NOT NULL,
    error_message  TEXT,
        -- NULL при успехе
    input_hash     VARCHAR(64),
        -- SHA-256 входного текста (не сам текст — только хэш для дедупликации)
    created_at     TIMESTAMPTZ   NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_ai_logs_provider CHECK (provider IN ('ollama', 'openai')),
    CONSTRAINT chk_ai_logs_type     CHECK (
        call_type IN ('format_words', 'generate_test', 'suggest_title')
    ),
    CONSTRAINT chk_ai_logs_duration CHECK (duration_ms >= 0)
);
```

| Поле | Тип | NULL | Описание |
|---|---|---|---|
| `id` | UUID | NO | PK |
| `user_id` | UUID | YES | FK → `users.id` SET NULL |
| `call_type` | VARCHAR(30) | NO | Тип запроса к AI |
| `provider` | VARCHAR(20) | NO | Провайдер: ollama / openai |
| `model` | VARCHAR(50) | NO | Имя модели |
| `input_tokens` | INT | YES | Токены входа (NULL если неизвестно) |
| `output_tokens` | INT | YES | Токены выхода |
| `duration_ms` | INT | NO | Время ответа в мс |
| `success` | BOOLEAN | NO | Успешен ли вызов |
| `error_message` | TEXT | YES | Текст ошибки |
| `input_hash` | VARCHAR(64) | YES | SHA-256 входного текста |
| `created_at` | TIMESTAMPTZ | NO | Время вызова |

**Индексы:**

```sql
CREATE INDEX idx_ai_logs_created   ON ai_call_logs (created_at DESC);
CREATE INDEX idx_ai_logs_user      ON ai_call_logs (user_id) WHERE user_id IS NOT NULL;
CREATE INDEX idx_ai_logs_provider  ON ai_call_logs (provider, success);
CREATE INDEX idx_ai_logs_type      ON ai_call_logs (call_type, created_at DESC);
CREATE INDEX idx_ai_logs_errors    ON ai_call_logs (created_at DESC)
    WHERE success = FALSE;
```

**Партиционирование** (при > 1M строк в месяц):

```sql
-- Партиция по месяцам через pg_partman (в будущем)
-- ai_call_logs_2025_01, ai_call_logs_2025_02, ...
-- Автоматическое удаление партиций старше 12 месяцев
```

---

### 8.2 `system_settings`

Key-value хранилище настроек приложения. Кэшируется в Redis (TTL 60 сек).

```sql
CREATE TABLE system_settings (
    key          VARCHAR(100)  PRIMARY KEY,
        -- Иерархический формат: 'ai.primary_model', 'features.max_words_per_block'
    value        TEXT          NOT NULL,
    value_type   VARCHAR(10)   NOT NULL,
        -- 'string' | 'bool' | 'int' | 'json'
    description  TEXT,
    updated_at   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_by   UUID          REFERENCES users(id) ON DELETE SET NULL,

    CONSTRAINT chk_settings_type CHECK (value_type IN ('string', 'bool', 'int', 'json'))
);
```

| Поле | Тип | NULL | Описание |
|---|---|---|---|
| `key` | VARCHAR(100) | NO | PK, иерархический ключ |
| `value` | TEXT | NO | Значение (всегда строка, тип определяет `value_type`) |
| `value_type` | VARCHAR(10) | NO | Тип для десериализации |
| `description` | TEXT | YES | Описание для UI администратора |
| `updated_at` | TIMESTAMPTZ | NO | Дата последнего изменения |
| `updated_by` | UUID | YES | FK → `users.id` Кто изменил |

**Начальные значения** (seed):

```sql
INSERT INTO system_settings (key, value, value_type, description) VALUES
    ('ai.primary_model',              'qwen3:8b',  'string', 'Активная модель Ollama'),
    ('ai.fallback_enabled',           'true',      'bool',   'Включён ли OpenAI fallback'),
    ('ai.rate_limit_per_minute',      '10',        'int',    'Лимит AI-запросов на юзера в минуту'),
    ('features.registration_enabled', 'true',      'bool',   'Открыта ли регистрация'),
    ('features.max_words_per_block',  '200',       'int',    'Максимум слов в одном блоке (0=∞)'),
    ('features.max_blocks_per_user',  '0',         'int',    'Максимум блоков на юзера (0=∞)'),
    ('test.max_questions',            '50',        'int',    'Максимум вопросов в тесте'),
    ('maintenance.enabled',           'false',     'bool',   'Режим обслуживания');
```

---

### 8.3 `audit_logs`

Журнал всех административных действий. Неизменяемый (нет UPDATE, нет DELETE).

```sql
CREATE TABLE audit_logs (
    id           UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    admin_id     UUID          NOT NULL REFERENCES users(id),
        -- Нет CASCADE — записи аудита хранятся даже после удаления администратора
    action       VARCHAR(100)  NOT NULL,
        -- 'user.suspend', 'user.delete', 'user.role_change',
        -- 'settings.update', 'block.delete', 'word.edit'
    target_type  VARCHAR(50),
        -- 'user' | 'block' | 'word' | 'setting' | 'language'
    target_id    TEXT,
        -- UUID или ключ изменённого объекта
    old_value    JSONB,
        -- Состояние объекта ДО изменения
    new_value    JSONB,
        -- Состояние объекта ПОСЛЕ изменения
    ip_address   INET,
    user_agent   TEXT,
    created_at   TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);
```

| Поле | Тип | NULL | Описание |
|---|---|---|---|
| `id` | UUID | NO | PK |
| `admin_id` | UUID | NO | FK → `users.id` (без CASCADE) |
| `action` | VARCHAR(100) | NO | Тип действия |
| `target_type` | VARCHAR(50) | YES | Тип объекта |
| `target_id` | TEXT | YES | ID объекта |
| `old_value` | JSONB | YES | Состояние до |
| `new_value` | JSONB | YES | Состояние после |
| `ip_address` | INET | YES | IP администратора |
| `user_agent` | TEXT | YES | User-Agent |
| `created_at` | TIMESTAMPTZ | NO | Время действия |

> `audit_logs` — **append-only**. Приложение никогда не делает UPDATE или DELETE на эту таблицу.  
> Политика хранения: 2 года. Архивация в S3 через pg_dump partition.

**Индексы:**

```sql
CREATE INDEX idx_audit_admin      ON audit_logs (admin_id);
CREATE INDEX idx_audit_target     ON audit_logs (target_type, target_id);
CREATE INDEX idx_audit_action     ON audit_logs (action, created_at DESC);
CREATE INDEX idx_audit_created    ON audit_logs (created_at DESC);
```

---

## 9. Индексы

### Сводная таблица всех индексов

| Таблица | Индекс | Тип | Назначение |
|---|---|---|---|
| `users` | `uq_users_email` | UNIQUE | Проверка уникальности email при регистрации |
| `users` | `idx_users_role` | BTREE | Фильтрация по роли в admin-панели |
| `users` | `idx_users_last_active` | BTREE | Метрика активных пользователей |
| `refresh_tokens` | `uq_refresh_token_hash` | UNIQUE | Быстрый lookup токена при refresh |
| `refresh_tokens` | `idx_refresh_tokens_active` | PARTIAL | Только активные токены |
| `languages` | `uq_languages_code` | UNIQUE | Уникальность кода языка |
| `tags` | `uq_tags_user_name` | UNIQUE | Уникальность тега в рамках юзера |
| `word_blocks` | `idx_word_blocks_user_id` | BTREE | Список блоков пользователя |
| `word_blocks` | `idx_word_blocks_language` | BTREE | Фильтр по языку |
| `block_tags` | `idx_block_tags_tag` | BTREE | Блоки по тегу |
| `words` | `idx_words_block_id` | BTREE | Слова блока |
| `words` | `idx_words_due_review` | PARTIAL | SM-2: только просроченные слова |
| `words` | `idx_words_fts` | GIN | Full-text search |
| `words` | `idx_words_term_trgm` | GIN | ILIKE-поиск по term |
| `words` | `idx_words_translation_trgm` | GIN | ILIKE-поиск по translation |
| `tests` | `idx_tests_user_id` | BTREE | Тесты пользователя |
| `questions` | `idx_questions_hash` | BTREE | Дедупликация при регенерации |
| `attempt_answers` | `idx_attempt_answers_attempt` | BTREE | Ответы по попытке |
| `ai_call_logs` | `idx_ai_logs_errors` | PARTIAL | Только ошибки для мониторинга |
| `audit_logs` | `idx_audit_created` | BTREE | Журнал в хронологическом порядке |

---

## 10. Триггеры и функции

### 10.1 Поддержание `word_count`

Денормализованное поле `word_blocks.word_count` обновляется автоматически.

```sql
CREATE OR REPLACE FUNCTION fn_update_word_count()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        UPDATE word_blocks
        SET word_count = word_count + 1,
            updated_at = NOW()
        WHERE id = NEW.block_id;

    ELSIF TG_OP = 'DELETE' THEN
        UPDATE word_blocks
        SET word_count = GREATEST(word_count - 1, 0),
            updated_at = NOW()
        WHERE id = OLD.block_id;

    ELSIF TG_OP = 'UPDATE' AND OLD.block_id != NEW.block_id THEN
        -- Перенос слова в другой блок (редкий случай)
        UPDATE word_blocks
        SET word_count = GREATEST(word_count - 1, 0), updated_at = NOW()
        WHERE id = OLD.block_id;

        UPDATE word_blocks
        SET word_count = word_count + 1, updated_at = NOW()
        WHERE id = NEW.block_id;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_words_word_count
AFTER INSERT OR DELETE OR UPDATE OF block_id ON words
FOR EACH ROW EXECUTE FUNCTION fn_update_word_count();
```

### 10.2 Автоматическое обновление `updated_at`

```sql
CREATE OR REPLACE FUNCTION fn_set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Применяем ко всем таблицам с полем updated_at
CREATE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();

CREATE TRIGGER trg_word_blocks_updated_at
    BEFORE UPDATE ON word_blocks
    FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();
```

### 10.3 Нормализация email при вставке

```sql
CREATE OR REPLACE FUNCTION fn_normalize_email()
RETURNS TRIGGER AS $$
BEGIN
    NEW.email = LOWER(TRIM(NEW.email));
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_users_normalize_email
    BEFORE INSERT OR UPDATE OF email ON users
    FOR EACH ROW EXECUTE FUNCTION fn_normalize_email();
```

### 10.4 Обновление `question_count` в тесте

```sql
CREATE OR REPLACE FUNCTION fn_update_question_count()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        UPDATE tests
        SET question_count = COALESCE(question_count, 0) + 1
        WHERE id = NEW.test_id;
    ELSIF TG_OP = 'DELETE' THEN
        UPDATE tests
        SET question_count = GREATEST(COALESCE(question_count, 1) - 1, 0)
        WHERE id = OLD.test_id;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_questions_count
AFTER INSERT OR DELETE ON questions
FOR EACH ROW EXECUTE FUNCTION fn_update_question_count();
```

### 10.5 Обновление `last_active_at` пользователя

Вызывается из приложения через `SELECT fn_touch_user_activity($1)` после каждого аутентифицированного запроса (не чаще 1 раза в 5 минут — логика на уровне приложения).

```sql
CREATE OR REPLACE FUNCTION fn_touch_user_activity(p_user_id UUID)
RETURNS void AS $$
BEGIN
    UPDATE users
    SET last_active_at = NOW()
    WHERE id = p_user_id
      AND (last_active_at IS NULL OR last_active_at < NOW() - INTERVAL '5 minutes');
END;
$$ LANGUAGE plpgsql;
```

---

## 11. Политики Row-Level Security

RLS добавляет защиту на уровне БД: даже если приложение забудет добавить WHERE user_id = ..., PostgreSQL не вернёт чужие данные.

```sql
-- Включить RLS на основных таблицах
ALTER TABLE word_blocks    ENABLE ROW LEVEL SECURITY;
ALTER TABLE words          ENABLE ROW LEVEL SECURITY;
ALTER TABLE tests          ENABLE ROW LEVEL SECURITY;
ALTER TABLE test_attempts  ENABLE ROW LEVEL SECURITY;
ALTER TABLE attempt_answers ENABLE ROW LEVEL SECURITY;
ALTER TABLE tags           ENABLE ROW LEVEL SECURITY;

-- Роль приложения (используется пулом соединений)
CREATE ROLE lexify_app LOGIN PASSWORD '...';

-- word_blocks: видит только свои блоки
CREATE POLICY pol_word_blocks_owner ON word_blocks
    FOR ALL TO lexify_app
    USING (user_id = current_setting('app.current_user_id', TRUE)::UUID);

-- words: видит только слова из своих блоков
CREATE POLICY pol_words_owner ON words
    FOR ALL TO lexify_app
    USING (
        block_id IN (
            SELECT id FROM word_blocks
            WHERE user_id = current_setting('app.current_user_id', TRUE)::UUID
        )
    );

-- tests: видит только свои тесты
CREATE POLICY pol_tests_owner ON tests
    FOR ALL TO lexify_app
    USING (user_id = current_setting('app.current_user_id', TRUE)::UUID);

-- Установка текущего пользователя (вызывается при начале транзакции)
-- В коде: await db.ExecuteAsync("SELECT set_config('app.current_user_id', @UserId, TRUE)", ...)
```

> **Для администраторов**: приложение использует отдельную роль `lexify_admin` без RLS-ограничений.

---

## 12. Enum-типы

PostgreSQL `ENUM` не используются — вместо них VARCHAR с CHECK-ограничениями. Это упрощает миграции (добавить новое значение = изменить CHECK, не ALTER TYPE).

### Значения полей-перечислений

```
users.role:
    'user'         — обычный пользователь
    'moderator'    — модератор (ограниченный доступ к admin-панели)
    'admin'        — полный доступ к admin-панели

users.status:
    'active'       — нормальный аккаунт
    'suspended'    — заблокирован администратором
    'deleted'      — мягкое удаление, данные анонимизируются через 30 дней

words.word_type:
    'word'         — одиночное слово (biased, authenticity)
    'phrase'       — устойчивое словосочетание (to commodify)
    'idiom'        — идиома (to bite someone's head off)
    'expression'   — выражение/фраза (to be petrified of)

tests.status:
    'generating'   — фоновая задача Hangfire ещё генерирует вопросы
    'ready'        — тест готов к прохождению
    'archived'     — архивирован пользователем

questions.question_type:
    'translate_to_native'  — выбрать перевод иностранного слова
    'translate_to_foreign' — выбрать слово по переводу
    'fill_in_sentence'     — вставить слово в предложение
    'multi_select_theme'   — выбрать все слова темы (multi-select)
    'open_answer'          — вписать ответ вручную

ai_call_logs.call_type:
    'format_words'   — форматирование введённых слов
    'generate_test'  — генерация вопросов теста
    'suggest_title'  — предложение названия блока

ai_call_logs.provider:
    'ollama'   — локальная модель (qwen3:8b)
    'openai'   — OpenAI API (gpt-4o-mini, fallback)

system_settings.value_type:
    'string'   — строковое значение
    'bool'     — булево ('true' / 'false')
    'int'      — целое число
    'json'     — JSON-объект или массив
```

---

## 13. Полная миграция (DDL)

Порядок создания таблиц (учитывает зависимости FK):

```sql
-- =========================================================
-- 0. Расширения
-- =========================================================
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";
CREATE EXTENSION IF NOT EXISTS "unaccent";

-- =========================================================
-- 1. Справочники (нет зависимостей)
-- =========================================================
CREATE TABLE languages (
    id          SMALLSERIAL  PRIMARY KEY,
    code        VARCHAR(5)   NOT NULL,
    name        VARCHAR(50)  NOT NULL,
    native_name VARCHAR(50)  NOT NULL,
    is_active   BOOLEAN      NOT NULL DEFAULT TRUE,
    sort_order  SMALLINT     NOT NULL DEFAULT 0,
    CONSTRAINT uq_languages_code UNIQUE (code),
    CONSTRAINT chk_languages_code CHECK (code ~ '^[a-z]{2,5}$')
);

CREATE TABLE system_settings (
    key          VARCHAR(100) PRIMARY KEY,
    value        TEXT         NOT NULL,
    value_type   VARCHAR(10)  NOT NULL,
    description  TEXT,
    updated_at   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_by   UUID,        -- FK добавим после создания users
    CONSTRAINT chk_settings_type CHECK (value_type IN ('string', 'bool', 'int', 'json'))
);

-- =========================================================
-- 2. Пользователи
-- =========================================================
CREATE TABLE users (
    id               UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    email            VARCHAR(255) NOT NULL,
    password_hash    TEXT         NOT NULL,
    display_name     VARCHAR(100),
    role             VARCHAR(20)  NOT NULL DEFAULT 'user',
    status           VARCHAR(20)  NOT NULL DEFAULT 'active',
    last_active_at   TIMESTAMPTZ,
    deleted_at       TIMESTAMPTZ,
    created_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_users_email   UNIQUE (email),
    CONSTRAINT chk_users_role   CHECK (role   IN ('user', 'moderator', 'admin')),
    CONSTRAINT chk_users_status CHECK (status IN ('active', 'suspended', 'deleted'))
);

-- Теперь добавляем FK в system_settings
ALTER TABLE system_settings
    ADD CONSTRAINT fk_settings_updated_by
    FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE SET NULL;

CREATE TABLE refresh_tokens (
    id           UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id      UUID         NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash   VARCHAR(64)  NOT NULL,
    expires_at   TIMESTAMPTZ  NOT NULL,
    revoked_at   TIMESTAMPTZ,
    replaced_by  UUID         REFERENCES refresh_tokens(id),
    ip_address   INET,
    user_agent   TEXT,
    created_at   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_refresh_token_hash UNIQUE (token_hash)
);

CREATE TABLE tags (
    id          SERIAL       PRIMARY KEY,
    user_id     UUID         NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name        VARCHAR(50)  NOT NULL,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_tags_user_name UNIQUE (user_id, name)
);

-- =========================================================
-- 3. Контент
-- =========================================================
CREATE TABLE word_blocks (
    id           UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id      UUID          NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    language_id  SMALLINT      NOT NULL REFERENCES languages(id),
    title        VARCHAR(200)  NOT NULL,
    description  TEXT,
    word_count   INT           NOT NULL DEFAULT 0,
    created_at   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_word_blocks_title CHECK (LENGTH(TRIM(title)) > 0),
    CONSTRAINT chk_word_blocks_count CHECK (word_count >= 0)
);

CREATE TABLE block_tags (
    block_id  UUID     NOT NULL REFERENCES word_blocks(id) ON DELETE CASCADE,
    tag_id    INTEGER  NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
    PRIMARY KEY (block_id, tag_id)
);

CREATE TABLE words (
    id                UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    block_id          UUID          NOT NULL REFERENCES word_blocks(id) ON DELETE CASCADE,
    term              TEXT          NOT NULL,
    translation       TEXT          NOT NULL,
    word_type         VARCHAR(20)   NOT NULL DEFAULT 'word',
    notes             TEXT,
    example_sentence  TEXT,
    confidence_flag   BOOLEAN       NOT NULL DEFAULT FALSE,
    confidence_note   TEXT,
    sort_order        INT           NOT NULL DEFAULT 0,
    created_at        TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    ease_factor       FLOAT         NOT NULL DEFAULT 2.5,
    interval_days     INT           NOT NULL DEFAULT 1,
    repetitions       INT           NOT NULL DEFAULT 0,
    next_review_at    TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_words_type      CHECK (word_type IN ('word', 'phrase', 'idiom', 'expression')),
    CONSTRAINT chk_words_ease      CHECK (ease_factor >= 1.3),
    CONSTRAINT chk_words_interval  CHECK (interval_days >= 1),
    CONSTRAINT chk_words_reps      CHECK (repetitions >= 0),
    CONSTRAINT chk_words_term      CHECK (LENGTH(TRIM(term)) > 0),
    CONSTRAINT chk_words_trans     CHECK (LENGTH(TRIM(translation)) > 0)
);

-- =========================================================
-- 4. Тестирование
-- =========================================================
CREATE TABLE tests (
    id             UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id        UUID          NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title          VARCHAR(200)  NOT NULL,
    status         VARCHAR(20)   NOT NULL DEFAULT 'generating',
    question_count INT,
    created_at     TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_tests_status CHECK (status IN ('generating', 'ready', 'archived'))
);

CREATE TABLE test_blocks (
    test_id   UUID  NOT NULL REFERENCES tests(id) ON DELETE CASCADE,
    block_id  UUID  NOT NULL REFERENCES word_blocks(id) ON DELETE CASCADE,
    PRIMARY KEY (test_id, block_id)
);

CREATE TABLE questions (
    id              UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    test_id         UUID          NOT NULL REFERENCES tests(id) ON DELETE CASCADE,
    word_id         UUID          REFERENCES words(id) ON DELETE SET NULL,
    question_type   VARCHAR(30)   NOT NULL,
    question_text   TEXT          NOT NULL,
    correct_answer  TEXT          NOT NULL,
    sort_order      INT           NOT NULL DEFAULT 0,
    content_hash    VARCHAR(64)   NOT NULL,
    CONSTRAINT chk_questions_type CHECK (
        question_type IN (
            'translate_to_native', 'translate_to_foreign',
            'fill_in_sentence', 'multi_select_theme', 'open_answer'
        )
    )
);

CREATE TABLE question_options (
    id           UUID      PRIMARY KEY DEFAULT gen_random_uuid(),
    question_id  UUID      NOT NULL REFERENCES questions(id) ON DELETE CASCADE,
    option_text  TEXT      NOT NULL,
    is_correct   BOOLEAN   NOT NULL DEFAULT FALSE,
    sort_order   INT       NOT NULL DEFAULT 0
);

CREATE TABLE test_attempts (
    id               UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    test_id          UUID          NOT NULL REFERENCES tests(id) ON DELETE CASCADE,
    user_id          UUID          NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    started_at       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    finished_at      TIMESTAMPTZ,
    score            FLOAT,
    total_questions  INT,
    correct_answers  INT,
    CONSTRAINT chk_attempts_score CHECK (score IS NULL OR (score >= 0.0 AND score <= 1.0))
);

CREATE TABLE attempt_answers (
    id             UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    attempt_id     UUID          NOT NULL REFERENCES test_attempts(id) ON DELETE CASCADE,
    question_id    UUID          NOT NULL REFERENCES questions(id) ON DELETE CASCADE,
    given_answer   TEXT          NOT NULL,
    is_correct     BOOLEAN       NOT NULL,
    time_spent_ms  INT,
    answered_at    TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_attempt_question UNIQUE (attempt_id, question_id),
    CONSTRAINT chk_answers_time CHECK (time_spent_ms IS NULL OR time_spent_ms >= 0)
);

-- =========================================================
-- 5. Служебные таблицы
-- =========================================================
CREATE TABLE ai_call_logs (
    id             UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id        UUID          REFERENCES users(id) ON DELETE SET NULL,
    call_type      VARCHAR(30)   NOT NULL,
    provider       VARCHAR(20)   NOT NULL,
    model          VARCHAR(50)   NOT NULL,
    input_tokens   INT,
    output_tokens  INT,
    duration_ms    INT           NOT NULL,
    success        BOOLEAN       NOT NULL,
    error_message  TEXT,
    input_hash     VARCHAR(64),
    created_at     TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_ai_logs_provider CHECK (provider IN ('ollama', 'openai')),
    CONSTRAINT chk_ai_logs_type     CHECK (
        call_type IN ('format_words', 'generate_test', 'suggest_title')
    ),
    CONSTRAINT chk_ai_logs_duration CHECK (duration_ms >= 0)
);

CREATE TABLE audit_logs (
    id           UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    admin_id     UUID          NOT NULL REFERENCES users(id),
    action       VARCHAR(100)  NOT NULL,
    target_type  VARCHAR(50),
    target_id    TEXT,
    old_value    JSONB,
    new_value    JSONB,
    ip_address   INET,
    user_agent   TEXT,
    created_at   TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

-- =========================================================
-- 6. Все индексы
-- =========================================================
CREATE UNIQUE INDEX uq_users_email            ON users (LOWER(email));
CREATE INDEX idx_users_role                   ON users (role);
CREATE INDEX idx_users_status                 ON users (status) WHERE status != 'active';
CREATE INDEX idx_users_last_active            ON users (last_active_at DESC NULLS LAST);
CREATE UNIQUE INDEX uq_refresh_token_hash     ON refresh_tokens (token_hash);
CREATE INDEX idx_refresh_tokens_user          ON refresh_tokens (user_id);
CREATE INDEX idx_refresh_tokens_active        ON refresh_tokens (user_id, expires_at)
    WHERE revoked_at IS NULL;
CREATE UNIQUE INDEX uq_languages_code         ON languages (code);
CREATE INDEX idx_languages_active             ON languages (is_active, sort_order);
CREATE UNIQUE INDEX uq_tags_user_name         ON tags (user_id, name);
CREATE INDEX idx_tags_user_id                 ON tags (user_id);
CREATE INDEX idx_word_blocks_user_id          ON word_blocks (user_id);
CREATE INDEX idx_word_blocks_language         ON word_blocks (user_id, language_id);
CREATE INDEX idx_word_blocks_updated          ON word_blocks (user_id, updated_at DESC);
CREATE INDEX idx_block_tags_tag               ON block_tags (tag_id);
CREATE INDEX idx_words_block_id               ON words (block_id);
CREATE INDEX idx_words_sort                   ON words (block_id, sort_order);
CREATE INDEX idx_words_created                ON words (block_id, created_at DESC);
CREATE INDEX idx_words_due_review             ON words (next_review_at)
    WHERE next_review_at <= NOW();
CREATE INDEX idx_words_confidence             ON words (block_id)
    WHERE confidence_flag = TRUE;
CREATE INDEX idx_words_fts ON words USING GIN (
    to_tsvector('simple', unaccent(term) || ' ' || unaccent(translation))
);
CREATE INDEX idx_words_term_trgm              ON words USING GIN (term gin_trgm_ops);
CREATE INDEX idx_words_translation_trgm       ON words USING GIN (translation gin_trgm_ops);
CREATE INDEX idx_tests_user_id                ON tests (user_id);
CREATE INDEX idx_tests_status                 ON tests (user_id, status);
CREATE INDEX idx_tests_created                ON tests (user_id, created_at DESC);
CREATE INDEX idx_test_blocks_block            ON test_blocks (block_id);
CREATE INDEX idx_questions_test_id            ON questions (test_id);
CREATE INDEX idx_questions_word_id            ON questions (word_id) WHERE word_id IS NOT NULL;
CREATE INDEX idx_questions_hash               ON questions (content_hash);
CREATE INDEX idx_questions_sort               ON questions (test_id, sort_order);
CREATE INDEX idx_question_options_question    ON question_options (question_id);
CREATE INDEX idx_question_options_correct     ON question_options (question_id)
    WHERE is_correct = TRUE;
CREATE INDEX idx_attempts_test_id             ON test_attempts (test_id);
CREATE INDEX idx_attempts_user_id             ON test_attempts (user_id);
CREATE INDEX idx_attempts_started             ON test_attempts (user_id, started_at DESC);
CREATE INDEX idx_attempts_incomplete          ON test_attempts (user_id)
    WHERE finished_at IS NULL;
CREATE INDEX idx_attempt_answers_attempt      ON attempt_answers (attempt_id);
CREATE INDEX idx_attempt_answers_question     ON attempt_answers (question_id);
CREATE INDEX idx_attempt_answers_wrong        ON attempt_answers (attempt_id)
    WHERE is_correct = FALSE;
CREATE INDEX idx_ai_logs_created              ON ai_call_logs (created_at DESC);
CREATE INDEX idx_ai_logs_user                 ON ai_call_logs (user_id)
    WHERE user_id IS NOT NULL;
CREATE INDEX idx_ai_logs_provider             ON ai_call_logs (provider, success);
CREATE INDEX idx_ai_logs_type                 ON ai_call_logs (call_type, created_at DESC);
CREATE INDEX idx_ai_logs_errors               ON ai_call_logs (created_at DESC)
    WHERE success = FALSE;
CREATE INDEX idx_audit_admin                  ON audit_logs (admin_id);
CREATE INDEX idx_audit_target                 ON audit_logs (target_type, target_id);
CREATE INDEX idx_audit_action                 ON audit_logs (action, created_at DESC);
CREATE INDEX idx_audit_created                ON audit_logs (created_at DESC);

-- =========================================================
-- 7. Триггеры
-- =========================================================
-- (функции описаны в разделе 10)
CREATE TRIGGER trg_words_word_count
    AFTER INSERT OR DELETE OR UPDATE OF block_id ON words
    FOR EACH ROW EXECUTE FUNCTION fn_update_word_count();

CREATE TRIGGER trg_questions_count
    AFTER INSERT OR DELETE ON questions
    FOR EACH ROW EXECUTE FUNCTION fn_update_question_count();

CREATE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();

CREATE TRIGGER trg_word_blocks_updated_at
    BEFORE UPDATE ON word_blocks
    FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();

CREATE TRIGGER trg_users_normalize_email
    BEFORE INSERT OR UPDATE OF email ON users
    FOR EACH ROW EXECUTE FUNCTION fn_normalize_email();
```

---

## 14. Сидовые данные

```sql
-- Языки (начальный набор)
INSERT INTO languages (code, name, native_name, is_active, sort_order) VALUES
    ('en', 'English',    'English',    TRUE,  1),
    ('no', 'Norwegian',  'Norsk',      TRUE,  2),
    ('uk', 'Ukrainian',  'Українська', TRUE,  3),
    ('ru', 'Russian',    'Русский',    TRUE,  4),
    ('de', 'German',     'Deutsch',    TRUE,  5),
    ('pl', 'Polish',     'Polski',     TRUE,  6),
    ('fr', 'French',     'Français',   TRUE,  7),
    ('es', 'Spanish',    'Español',    TRUE,  8),
    ('it', 'Italian',    'Italiano',   TRUE,  9),
    ('sv', 'Swedish',    'Svenska',    FALSE, 10);

-- Системные настройки
INSERT INTO system_settings (key, value, value_type, description) VALUES
    ('ai.primary_model',              'qwen3:8b',  'string', 'Активная модель Ollama'),
    ('ai.fallback_enabled',           'true',      'bool',   'Включён ли OpenAI fallback'),
    ('ai.rate_limit_per_minute',      '10',        'int',    'Лимит AI-запросов на юзера/мин'),
    ('features.registration_enabled', 'true',      'bool',   'Открыта ли регистрация'),
    ('features.max_words_per_block',  '200',       'int',    'Максимум слов в блоке (0=∞)'),
    ('features.max_blocks_per_user',  '0',         'int',    'Максимум блоков на юзера (0=∞)'),
    ('test.max_questions',            '50',        'int',    'Максимум вопросов в тесте'),
    ('test.min_words_required',       '5',         'int',    'Минимум слов для создания теста'),
    ('maintenance.enabled',           'false',     'bool',   'Режим обслуживания');
```

---

## 15. Советы по производительности

### Частичные индексы — недооценённый инструмент

Вместо одного большого индекса по всей таблице, используем частичные — они меньше и быстрее:

```sql
-- Плохо: индексирует ВСЕ слова, хотя повторяются только те, у кого next_review_at <= NOW()
CREATE INDEX ON words (next_review_at);

-- Хорошо: индексирует только «просроченные» слова — их всегда меньшинство
CREATE INDEX idx_words_due_review ON words (next_review_at)
    WHERE next_review_at <= NOW();
```

### Анализ медленных запросов

```sql
-- Включить логирование медленных запросов в postgresql.conf
log_min_duration_statement = 200  -- логировать запросы > 200ms

-- Просмотр самых тяжёлых запросов (требует pg_stat_statements)
SELECT
    query,
    calls,
    total_exec_time::NUMERIC(10,2) AS total_ms,
    mean_exec_time::NUMERIC(10,2)  AS avg_ms,
    rows / calls                   AS avg_rows
FROM pg_stat_statements
ORDER BY mean_exec_time DESC
LIMIT 10;
```

### VACUUM и статистика

```sql
-- Настройки autovacuum для высоконагруженных таблиц
ALTER TABLE words          SET (autovacuum_vacuum_scale_factor = 0.05);
ALTER TABLE ai_call_logs   SET (autovacuum_vacuum_scale_factor = 0.05);
ALTER TABLE attempt_answers SET (autovacuum_vacuum_scale_factor = 0.05);
-- По умолчанию 0.2 (20%) — при 100k строк VACUUM запускается каждые 20k изменений.
-- 0.05 (5%) — каждые 5k изменений, что лучше для таблиц с частыми обновлениями.
```

### Размер данных (оценка)

| Таблица | Строк / 1000 юзеров | Средний размер строки | Итого |
|---|---|---|---|
| `words` | ~500k | ~200 bytes | ~100 MB |
| `attempt_answers` | ~2M | ~100 bytes | ~200 MB |
| `ai_call_logs` | ~100k / месяц | ~200 bytes | ~20 MB/мес |
| `audit_logs` | ~10k | ~500 bytes | ~5 MB |
| Всё остальное | — | — | ~50 MB |

При 1000 активных пользователей — итого ~400 MB. PostgreSQL на $10/месяц справится.
