# Lexify — Полный список задач

> Задачи разбиты по фазам, модулям и слоям. Каждая задача атомарна — один PR, один коммит.  
> Зависимости указаны явно. Задачи без зависимостей можно выполнять параллельно.

---

## Содержание

- [Фаза 0 — Инфраструктура и настройка проекта](#фаза-0--инфраструктура-и-настройка-проекта)
- [Фаза 1 — База данных](#фаза-1--база-данных)
- [Фаза 2 — Backend: ядро и аутентификация](#фаза-2--backend-ядро-и-аутентификация)
- [Фаза 3 — Backend: слова и блоки](#фаза-3--backend-слова-и-блоки)
- [Фаза 4 — Backend: AI-интеграция](#фаза-4--backend-ai-интеграция)
- [Фаза 5 — Backend: тестирование (квизы)](#фаза-5--backend-тестирование-квизы)
- [Фаза 6 — Backend: Spaced Repetition](#фаза-6--backend-spaced-repetition)
- [Фаза 7 — Backend: Админ-панель API](#фаза-7--backend-админ-панель-api)
- [Фаза 8 — Frontend: инфраструктура и shared](#фаза-8--frontend-инфраструктура-и-shared)
- [Фаза 9 — Frontend: аутентификация](#фаза-9--frontend-аутентификация)
- [Фаза 10 — Frontend: блоки и слова](#фаза-10--frontend-блоки-и-слова)
- [Фаза 11 — Frontend: AI-импорт слов](#фаза-11--frontend-ai-импорт-слов)
- [Фаза 12 — Frontend: тесты](#фаза-12--frontend-тесты)
- [Фаза 13 — Frontend: Spaced Repetition](#фаза-13--frontend-spaced-repetition)
- [Фаза 14 — Frontend: Админ-панель UI](#фаза-14--frontend-админ-панель-ui)
- [Фаза 15 — Поиск, теги, экспорт](#фаза-15--поиск-теги-экспорт)
- [Фаза 16 — Уведомления и фоновые задачи](#фаза-16--уведомления-и-фоновые-задачи)
- [Фаза 17 — Тесты (автотесты)](#фаза-17--тесты-автотесты)
- [Фаза 18 — DevOps и деплой](#фаза-18--devops-и-деплой)
- [Фаза 19 — v2: мобильное приложение](#фаза-19--v2-мобильное-приложение)
- [Сводная таблица фаз](#сводная-таблица-фаз)

---

## Условные обозначения

- `[ ]` — не начата
- `[~]` — в процессе
- `[x]` — выполнена
- **Depends:** — задачи, которые должны быть завершены перед началом
- ⚡ — критический путь MVP

---

## Фаза 0 — Инфраструктура и настройка проекта

### 0.1 Репозиторий и структура

- [ ] ⚡ Создать монорепозиторий: `lexify/` с папками `backend/`, `frontend/`, `docs/`
- [ ] ⚡ Добавить `.gitignore` для .NET, Node, JetBrains, VS Code
- [ ] ⚡ Настроить `README.md` с описанием, структурой и инструкцией по запуску
- [ ] Настроить `.editorconfig` для единого форматирования

### 0.2 Backend — инициализация .NET решения

- [ ] ⚡ Создать solution `Lexify.sln`
- [ ] ⚡ Создать проекты: `Lexify.Domain`, `Lexify.Application`, `Lexify.Infrastructure`, `Lexify.API`
- [ ] ⚡ Создать тестовые проекты: `Lexify.Domain.Tests`, `Lexify.Application.Tests`, `Lexify.API.Tests`
- [ ] ⚡ Настроить ссылки между проектами (ProjectReference) согласно Clean Architecture
- [ ] Добавить `Directory.Build.props` с общими настройками (nullable, warnings as errors, LangVersion)
- [ ] Добавить `.editorconfig` для C# (Roslyn правила)

### 0.3 Backend — установка NuGet-пакетов

- [ ] ⚡ `Lexify.API`: `Microsoft.AspNetCore.OpenApi`, `Swashbuckle.AspNetCore`, `Serilog.AspNetCore`
- [ ] ⚡ `Lexify.Application`: `MediatR`, `FluentValidation.DependencyInjectionExtensions`, `AutoMapper.Extensions.Microsoft.DependencyInjection`
- [ ] ⚡ `Lexify.Infrastructure`: `Microsoft.EntityFrameworkCore.Design`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `StackExchange.Redis`, `Hangfire.PostgreSql`, `BCrypt.Net-Next`
- [ ] `Lexify.Infrastructure`: `Microsoft.AspNetCore.Authentication.JwtBearer`

### 0.4 Frontend — инициализация

- [ ] ⚡ Создать проект: `npm create vite@latest frontend -- --template react-ts`
- [ ] ⚡ Установить зависимости: `zustand`, `@tanstack/react-query`, `axios`, `react-router-dom`, `zod`, `react-hook-form`
- [ ] ⚡ Установить UI: `tailwindcss`, `@shadcn/ui` (init), `lucide-react`
- [ ] Установить dev-зависимости: `eslint-plugin-boundaries`, `@typescript-eslint/eslint-plugin`, `prettier`
- [ ] ⚡ Настроить структуру папок FSD: `app/`, `pages/`, `widgets/`, `features/`, `entities/`, `shared/`
- [ ] Настроить `tsconfig.json`: path aliases (`@/` → `src/`)
- [ ] Настроить `eslint-plugin-boundaries`: правила зависимостей FSD
- [ ] Настроить `prettier` + pre-commit hook через `husky` + `lint-staged`

### 0.5 Локальная инфраструктура

- [ ] ⚡ Создать `docker-compose.yml`: PostgreSQL 16, Redis 7, Ollama
- [ ] ⚡ Добавить `docker-compose.override.yml` для dev-окружения (порты, volumes)
- [ ] ⚡ Настроить Ollama: pull модели `qwen3:8b`
- [ ] Добавить скрипт `scripts/setup.sh` (или `.bat`): запуск docker-compose + применение миграций
- [ ] Создать `appsettings.Development.json` с локальными connection strings (не коммитить секреты)
- [ ] Добавить `.env.example` для frontend с переменными окружения

---

## Фаза 1 — База данных

**Depends:** Фаза 0.2, 0.5

### 1.1 Расширения и базовая настройка

- [ ] ⚡ Настроить `AppDbContext`: подключение Npgsql, применение конфигураций из Assembly
- [ ] ⚡ Создать миграцию: включить расширения `pgcrypto`, `pg_trgm`, `unaccent`
- [ ] ⚡ Добавить базовый класс `BaseEntity` с полями `Id`, `CreatedAt`, `UpdatedAt`

### 1.2 Таблицы справочников

- [ ] ⚡ Создать `IEntityTypeConfiguration<Language>` + миграцию для `languages`
- [ ] Создать `IEntityTypeConfiguration<Tag>` + миграцию для `tags`
- [ ] Создать `IEntityTypeConfiguration<SystemSetting>` + миграцию для `system_settings`

### 1.3 Пользователи и аутентификация

- [ ] ⚡ Создать `IEntityTypeConfiguration<User>` + миграцию для `users`
  - поля: id, email, password_hash, display_name, role, status, last_active_at, deleted_at
  - UNIQUE INDEX на `LOWER(email)`
  - CHECK constraints для role, status
- [ ] ⚡ Создать `IEntityTypeConfiguration<RefreshToken>` + миграцию для `refresh_tokens`
  - поля: id, user_id, token_hash, expires_at, revoked_at, replaced_by, ip_address, user_agent
  - PARTIAL INDEX на активные токены

### 1.4 Контент

- [ ] ⚡ Создать `IEntityTypeConfiguration<WordBlock>` + миграцию для `word_blocks`
- [ ] Создать миграцию для `block_tags` (M:N)
- [ ] ⚡ Создать `IEntityTypeConfiguration<Word>` + миграцию для `words`
  - SM-2 поля: ease_factor, interval_days, repetitions, next_review_at
  - PARTIAL INDEX для spaced repetition (next_review_at <= NOW())
  - GIN индекс для full-text search с unaccent
  - GIN индексы для trigram-поиска (pg_trgm)

### 1.5 Тестирование

- [ ] ⚡ Создать `IEntityTypeConfiguration<Test>` + миграцию для `tests`
- [ ] Создать миграцию для `test_blocks` (M:N)
- [ ] ⚡ Создать `IEntityTypeConfiguration<Question>` + миграцию для `questions`
  - CHECK constraint для question_type
  - INDEX на content_hash
- [ ] ⚡ Создать `IEntityTypeConfiguration<QuestionOption>` + миграцию для `question_options`
- [ ] ⚡ Создать `IEntityTypeConfiguration<TestAttempt>` + миграцию для `test_attempts`
- [ ] ⚡ Создать `IEntityTypeConfiguration<AttemptAnswer>` + миграцию для `attempt_answers`
  - UNIQUE на (attempt_id, question_id)

### 1.6 Служебные таблицы

- [ ] Создать `IEntityTypeConfiguration<AiCallLog>` + миграцию для `ai_call_logs`
- [ ] Создать `IEntityTypeConfiguration<AuditLog>` + миграцию для `audit_logs`

### 1.7 Триггеры и функции

- [ ] ⚡ Создать миграцию: функция `fn_update_word_count` + триггер `trg_words_word_count`
- [ ] Создать миграцию: функция `fn_update_question_count` + триггер `trg_questions_count`
- [ ] ⚡ Создать миграцию: функция `fn_set_updated_at` + триггеры для `users`, `word_blocks`
- [ ] Создать миграцию: функция `fn_normalize_email` + триггер для `users`
- [ ] Создать миграцию: функция `fn_touch_user_activity`
- [ ] Создать миграцию: функция `fn_anonymize_deleted_users`
- [ ] Создать миграцию: функция `fn_cleanup_refresh_tokens`

### 1.8 Row-Level Security

- [ ] Создать миграцию: роль `lexify_app` + роль `lexify_admin`
- [ ] Создать миграцию: RLS политики для `word_blocks`, `words`, `tests`, `test_attempts`, `attempt_answers`, `tags`

### 1.9 Seed данные

- [ ] ⚡ Создать `DataSeeder`: начальные `languages` (en, no, uk, ru, de, pl, fr, es, it)
- [ ] Создать `DataSeeder`: начальные `system_settings`
- [ ] Создать `DataSeeder`: admin-аккаунт из переменных окружения

---

## Фаза 2 — Backend: ядро и аутентификация

**Depends:** Фаза 1

### 2.1 Domain Layer — общее

- [ ] ⚡ Создать интерфейс `IDomainEvent`
- [ ] ⚡ Создать класс `DomainException`
- [ ] ⚡ Реализовать `BaseEntity` с поддержкой Domain Events (список + методы Add/Clear)

### 2.2 Domain Layer — сущности

- [ ] ⚡ Реализовать `User` entity: Factory Method `Create()`, метод `Suspend()`, метод `Delete()`
- [ ] ⚡ Реализовать `WordBlock` entity: `Create()`, `Rename()`, `AddWord()`
- [ ] ⚡ Реализовать `Word` entity: `Create()`, `ApplyReviewResult()` (SM-2), `UpdateTranslation()`
- [ ] ⚡ Реализовать `Test` entity: `Create()`, `MarkReady()`, `Archive()`
- [ ] Реализовать `Question` entity: `Create()` с вычислением `content_hash`
- [ ] Реализовать `TestAttempt` entity: `Start()`, `Finish(score)`
- [ ] ⚡ Реализовать Value Object `Language`
- [ ] Реализовать Value Object `TestScore`
- [ ] Реализовать Domain Events: `WordCreatedEvent`, `WordReviewedEvent`, `TestCompletedEvent`

### 2.3 Domain Layer — Repository Interfaces

- [ ] ⚡ Создать `IUserRepository`
- [ ] ⚡ Создать `IWordRepository` (включая `GetDistractorPoolAsync`, `GetDueForReviewAsync`)
- [ ] ⚡ Создать `IWordBlockRepository`
- [ ] ⚡ Создать `ITestRepository`
- [ ] ⚡ Создать `IUnitOfWork`

### 2.4 Domain Layer — Domain Services

- [ ] Реализовать `SpacedRepetitionService` с алгоритмом SM-2

### 2.5 Application Layer — общая инфраструктура

- [ ] ⚡ Реализовать `Result<T>` с методами `Ok`, `NotFound`, `Forbidden`, `Failure`
- [ ] ⚡ Реализовать `PagedResult<T>`
- [ ] ⚡ Создать интерфейс `ICurrentUserService`
- [ ] ⚡ Создать интерфейс `ICacheService`
- [ ] Создать интерфейс `IEmailService`
- [ ] ⚡ Реализовать `ValidationBehavior` (MediatR Pipeline)
- [ ] ⚡ Реализовать `LoggingBehavior` (MediatR Pipeline)
- [ ] Реализовать `CachingBehavior` с интерфейсом `ICacheable` (MediatR Pipeline)
- [ ] Реализовать `TransactionBehavior` (MediatR Pipeline)
- [ ] Настроить `DependencyInjection.cs` в Application: MediatR, FluentValidation, AutoMapper, Behaviors

### 2.6 Application Layer — Auth Commands

- [ ] ⚡ Реализовать `RegisterCommand` + `RegisterCommandHandler` + `RegisterCommandValidator`
  - хэширование пароля bcrypt, создание User entity, сохранение
- [ ] ⚡ Реализовать `LoginCommand` + `LoginCommandHandler`
  - проверка пароля, генерация JWT access token (15 мин) + refresh token (30 дней)
  - запись refresh token в БД (хэш)
- [ ] ⚡ Реализовать `RefreshTokenCommand` + `RefreshTokenCommandHandler`
  - поиск по хэшу, проверка срока, ротация (revoke старый, создать новый)
- [ ] Реализовать `LogoutCommand` + `LogoutCommandHandler`
  - отзыв refresh token по хэшу

### 2.7 Infrastructure Layer — Auth

- [ ] ⚡ Реализовать `JwtService`: генерация и валидация JWT, claims (userId, email, role)
- [ ] ⚡ Реализовать `CurrentUserService`: читает claims из `IHttpContextAccessor`
- [ ] ⚡ Реализовать `UserRepository`
- [ ] ⚡ Реализовать `RefreshTokenRepository`

### 2.8 Presentation Layer — Auth

- [ ] ⚡ Создать `AuthController`: `POST /register`, `POST /login`, `POST /refresh`, `POST /logout`
- [ ] ⚡ Настроить JWT-аутентификацию в `Program.cs`
- [ ] ⚡ Создать `ExceptionMiddleware`: маппинг `DomainException` → 400, `ValidationException` → 422, необработанных → 500
- [ ] ⚡ Создать `CurrentUserMiddleware`: заполнение `ICurrentUserService` из claims
- [ ] ⚡ Настроить Swagger с поддержкой JWT Bearer в `Program.cs`
- [ ] Настроить CORS для фронтенда в `Program.cs`

---

## Фаза 3 — Backend: слова и блоки

**Depends:** Фаза 2

### 3.1 Infrastructure Layer — Repositories

- [ ] ⚡ Реализовать `WordBlockRepository`:
  - `GetByIdAsync`, `GetByIdWithWordsAsync`, `GetByUserIdAsync` (пагинация + фильтр по языку/тегу)
  - `AddAsync`, `UpdateAsync`, `DeleteAsync`
- [ ] ⚡ Реализовать `WordRepository`:
  - `GetByIdAsync`, `GetByBlockIdAsync` (пагинация + поиск)
  - `GetDistractorPoolAsync` (случайная выборка из всех блоков юзера одного языка)
  - `AddRangeAsync`, `UpdateAsync`, `DeleteAsync`
- [ ] Реализовать `UnitOfWork`

### 3.2 Application Layer — Block Commands

- [ ] ⚡ Реализовать `CreateBlockCommand` + Handler + Validator
- [ ] ⚡ Реализовать `UpdateBlockCommand` + Handler + Validator (rename, description)
- [ ] ⚡ Реализовать `DeleteBlockCommand` + Handler (проверка владельца)
- [ ] Реализовать `ImportWordsCommand` + Handler + Validator
  - принимает массив отформатированных слов, создаёт Word entity для каждого
  - максимум 200 слов за раз (validator)
- [ ] Реализовать `CreateWordCommand` + Handler + Validator (ручное добавление)
- [ ] Реализовать `UpdateWordCommand` + Handler + Validator
- [ ] Реализовать `DeleteWordCommand` + Handler

### 3.3 Application Layer — Block Queries

- [ ] ⚡ Реализовать `GetBlocksQuery` + Handler
  - пагинация, фильтр по language_id и tag
  - реализовать `ICacheable` (ключ: `blocks:{userId}:{filters}`, TTL: 5 мин)
- [ ] ⚡ Реализовать `GetBlockByIdQuery` + Handler (блок + слова с пагинацией)
- [ ] Реализовать `GetWordsByBlockQuery` + Handler (пагинация, поиск)

### 3.4 Application Layer — AutoMapper

- [ ] ⚡ Создать `WordBlock → WordBlockDto` mapping profile
- [ ] ⚡ Создать `Word → WordDto` mapping profile

### 3.5 Presentation Layer — Words & Blocks Controller

- [ ] ⚡ Создать `BlocksController`:
  - `GET /api/blocks` (список с пагинацией)
  - `GET /api/blocks/{id}` (детали + слова)
  - `POST /api/blocks` (создать)
  - `PATCH /api/blocks/{id}` (обновить)
  - `DELETE /api/blocks/{id}`
- [ ] ⚡ Создать `WordsController`:
  - `GET /api/blocks/{id}/words` (список слов)
  - `POST /api/blocks/{id}/words` (добавить вручную)
  - `PATCH /api/blocks/{id}/words/{wordId}` (редактировать)
  - `DELETE /api/blocks/{id}/words/{wordId}`
  - `POST /api/blocks/{id}/import` (массовый импорт)
- [ ] Маппинг `Result<T>` → `IActionResult` в обоих контроллерах

---

## Фаза 4 — Backend: AI-интеграция

**Depends:** Фаза 2, Фаза 3

### 4.1 Application Layer — AI интерфейсы

- [ ] ⚡ Создать `IAIProvider` интерфейс:
  - `StreamFormatWordsAsync` (IAsyncEnumerable)
  - `GenerateTestQuestionsAsync`
  - `SuggestBlockTitleAsync`
  - `IsAvailableAsync`
- [ ] Создать DTO: `FormatWordsResult`, `FormatWordItem`, `TestGenerationResult`

### 4.2 Infrastructure Layer — Ollama

- [ ] ⚡ Реализовать `OllamaProvider : IAIProvider`
  - HTTP-клиент к `localhost:11434/api/chat`
  - стриминг через `IAsyncEnumerable<string>` (читаем поток построчно)
  - промпт для форматирования с `/no_think` и температурой 0.1
  - промпт для генерации теста с `/think` и температурой 0.6
  - промпт для предложения названия блока
- [ ] ⚡ Реализовать `OpenAIProvider : IAIProvider` (fallback)
  - HTTP-клиент к `api.openai.com`
  - те же методы, те же промпты
- [ ] ⚡ Реализовать `AIOrchestrator : IAIProvider`
  - `IsAvailableAsync` для определения активного провайдера
  - логирование вызова в `ai_call_logs` через `IAiCallLogRepository`
  - автоматический fallback при недоступности Ollama
- [ ] ⚡ Реализовать `AIResponseValidator`
  - проверка: JSON валиден
  - проверка: распознано >= 50% входных строк
  - возврат деталей ошибки для диагностики
- [ ] Реализовать `AiCallLogRepository`
- [ ] Настроить `IHttpClientFactory` для Ollama и OpenAI с timeout и retry (Polly)
- [ ] Зарегистрировать AI-сервисы в `DependencyInjection.cs`

### 4.3 Application Layer — FormatWords

- [ ] ⚡ Реализовать `FormatWordsCommand` + стриминговый Handler
  - вызов `IAIProvider.StreamFormatWordsAsync`
  - валидация ответа через `AIResponseValidator`
  - retry-логика: 2 попытки при невалидном JSON
  - эмит событий: `parsing` → `streaming` → `done` / `error`
- [ ] Реализовать `FormatWordsCommandValidator`:
  - rawText не пустой, длина <= 10000 символов
  - targetLanguage — валидный код языка

### 4.4 Presentation Layer — AI endpoints

- [ ] ⚡ Добавить в `WordsController` endpoint `POST /api/words/format`
  - SSE: установить заголовки `Content-Type: text/event-stream`, `Cache-Control: no-cache`
  - итерировать `IAsyncEnumerable` и записывать SSE-события в Response.Body
  - Flush после каждого события
- [ ] ⚡ Добавить Rate Limiting для AI-эндпоинтов: 10 запросов / минуту / пользователь
  - реализовать через `System.Threading.RateLimiting` + Redis (sliding window)

---

## Фаза 5 — Backend: тестирование (квизы)

**Depends:** Фаза 3, Фаза 4

### 5.1 Infrastructure Layer

- [ ] ⚡ Реализовать `TestRepository`:
  - `GetByIdAsync`, `GetByIdWithQuestionsAsync`
  - `GetByUserIdAsync` (пагинация)
  - `AddAsync`, `UpdateAsync`
- [ ] Реализовать `QuestionRepository`: `AddRangeAsync`, `GetByTestIdAsync`
- [ ] Реализовать `TestAttemptRepository`:
  - `AddAsync`, `GetByIdWithAnswersAsync`
  - `GetByUserIdAsync`
- [ ] Реализовать `AttemptAnswerRepository`: `AddAsync`, `GetByAttemptIdAsync`

### 5.2 Application Layer — Generate Test

- [ ] ⚡ Реализовать `GenerateTestCommand` + Handler:
  - принимает `blockIds[]`, `questionTypes[]`, `questionCount`
  - проверяет минимум 5 слов в выбранных блоках
  - ставит Hangfire-задачу `GenerateTestJob`
  - возвращает `{ testId, status: "generating" }`
- [ ] ⚡ Реализовать `GenerateTestJob` (Hangfire):
  - загружает слова из блоков
  - загружает уже использованные `content_hash` предыдущих тестов (для дедупликации)
  - вызывает `IAIProvider.GenerateTestQuestionsAsync`
  - строит distractors: приоритет 1 — из других блоков, 2 — из текущего, 3 — AI
  - вычисляет `content_hash` для каждого вопроса
  - сохраняет `questions` + `question_options`
  - обновляет `tests.status = 'ready'`
- [ ] Реализовать `GenerateTestCommandValidator`:
  - blockIds не пустые, максимум 10 блоков
  - questionCount от 5 до 50
  - все blockIds принадлежат текущему пользователю

### 5.3 Application Layer — Run Test

- [ ] ⚡ Реализовать `GetTestByIdQuery` + Handler (тест + вопросы, без правильных ответов в ответе)
- [ ] Реализовать `GetTestsQuery` + Handler (список с пагинацией, фильтр по статусу)
- [ ] ⚡ Реализовать `StartAttemptCommand` + Handler:
  - создаёт `TestAttempt` со `started_at = NOW()`
  - возвращает `attemptId`
- [ ] ⚡ Реализовать `SubmitAnswerCommand` + Handler:
  - принимает `attemptId`, `questionId`, `givenAnswer`, `timeSpentMs`
  - для `open_answer`: проверка через Levenshtein (≤ 2 символа) + split по `/`, `,`
  - для `single_choice` / `multi_select`: сравнение с `is_correct` из `question_options`
  - сохраняет `AttemptAnswer`
  - возвращает `{ isCorrect, correctAnswer }` (для показа фидбека)
- [ ] ⚡ Реализовать `FinishAttemptCommand` + Handler:
  - вычисляет `TestScore` из всех ответов
  - обновляет `test_attempts`: `finished_at`, `score`, `total_questions`, `correct_answers`
  - для слов с неправильными ответами: уменьшает SM-2 `ease_factor`, сбрасывает `interval_days = 1`
  - публикует `TestCompletedEvent`
- [ ] Реализовать `GetAttemptResultsQuery` + Handler (ответы + правильные ответы для разбора)
- [ ] Реализовать утилитарный класс `LevenshteinDistance`

### 5.4 Presentation Layer — Tests Controller

- [ ] ⚡ Создать `TestsController`:
  - `GET /api/tests` (список)
  - `GET /api/tests/{id}` (тест с вопросами)
  - `POST /api/tests/generate` (запуск генерации)
  - `DELETE /api/tests/{id}`
- [ ] ⚡ Создать `AttemptsController`:
  - `POST /api/tests/{id}/attempts` (начать попытку)
  - `POST /api/attempts/{id}/answer` (ответить на вопрос)
  - `POST /api/attempts/{id}/finish` (завершить)
  - `GET /api/attempts/{id}` (результаты)

---

## Фаза 6 — Backend: Spaced Repetition

**Depends:** Фаза 3

### 6.1 Application Layer

- [ ] Реализовать `GetDueForReviewQuery` + Handler:
  - слова с `next_review_at <= NOW()` для текущего юзера
  - лимит 20 за раз, сортировка по `next_review_at ASC`
- [ ] Реализовать `ReviewWordCommand` + Handler:
  - принимает `wordId`, `quality` (0–5)
  - делегирует `SpacedRepetitionService.Calculate()`
  - обновляет `ease_factor`, `interval_days`, `repetitions`, `next_review_at`
  - публикует `WordReviewedEvent`
- [ ] Реализовать `ReviewWordCommandValidator`: quality от 0 до 5, wordId не пустой

### 6.2 Presentation Layer

- [ ] Создать `ReviewController`:
  - `GET /api/review/due` (слова к повторению)
  - `POST /api/review/rate` (оценить слово)

---

## Фаза 7 — Backend: Админ-панель API

**Depends:** Фаза 2, Фаза 3, Фаза 4

### 7.1 Admin Guard

- [ ] Создать `AdminOnlyFilter` (ActionFilter): проверяет `role == 'admin' || role == 'moderator'`
- [ ] Создать `AdminOnlyAttribute` для удобного применения к контроллерам

### 7.2 Queries — статистика дашборда

- [ ] Реализовать `GetDashboardStatsQuery` + Handler:
  - всего пользователей, активных за 7/30 дней
  - всего слов, блоков, тестов
  - топ-5 языков по количеству блоков
  - количество AI-вызовов за 24ч / 7 дней
  - кэш в Redis: TTL 5 минут
- [ ] Реализовать `GetRegistrationsChartQuery` + Handler (данные по дням за 30 дней)
- [ ] Реализовать `GetAiCallsChartQuery` + Handler (вызовы по часам за 24ч)

### 7.3 Queries/Commands — управление пользователями

- [ ] Реализовать `GetAdminUsersQuery` + Handler:
  - пагинация, фильтр по роли / статусу / поиск по email
  - для каждого: количество блоков, слов, тестов (subquery / join)
- [ ] Реализовать `SuspendUserCommand` + Handler (статус → `suspended`)
- [ ] Реализовать `RestoreUserCommand` + Handler (статус → `active`)
- [ ] Реализовать `DeleteUserCommand` + Handler (статус → `deleted`, `deleted_at = NOW()`)
- [ ] Реализовать `ChangeUserRoleCommand` + Handler
- [ ] Реализовать `ImpersonateUserCommand` + Handler:
  - генерирует JWT с дополнительным claim `impersonated_by: adminId`
  - запись в `audit_logs`

### 7.4 Commands — системные настройки

- [ ] Реализовать `GetSystemSettingsQuery` + Handler
- [ ] Реализовать `UpdateSystemSettingCommand` + Handler:
  - десериализация по `value_type`
  - инвалидация Redis-кэша
  - запись в `audit_logs`

### 7.5 Queries — AI мониторинг

- [ ] Реализовать `GetAiLogsQuery` + Handler:
  - пагинация, фильтр по provider / call_type / success / дата
- [ ] Реализовать `GetAiStatsQuery` + Handler:
  - среднее время ответа по типу, процент ошибок, количество fallback

### 7.6 Управление языками

- [ ] Реализовать `GetLanguagesQuery` + Handler (все, включая неактивные — для admin)
- [ ] Реализовать `AddLanguageCommand` + Handler
- [ ] Реализовать `ToggleLanguageCommand` + Handler (включить/выключить)

### 7.7 Presentation Layer

- [ ] Создать `AdminController` с роутами `/api/admin/*`
  - stats/dashboard, stats/registrations, stats/ai-calls
  - users (GET, PATCH role, PATCH suspend, DELETE, POST impersonate)
  - settings (GET, PATCH)
  - ai/logs, ai/stats, ai/status
  - languages (GET, POST, PATCH)

---

## Фаза 8 — Frontend: инфраструктура и shared

**Depends:** Фаза 0.4

### 8.1 shared/api

- [ ] ⚡ Реализовать `apiClient` (axios instance): baseURL, withCredentials, content-type
- [ ] ⚡ Реализовать interceptor: автоматический refresh токена при 401 + retry запроса
- [ ] ⚡ Реализовать interceptor: добавление `Authorization: Bearer {token}` из Zustand store
- [ ] Определить общие типы: `PagedResult<T>`, `ApiError`, `Result<T>`

### 8.2 shared/ui

- [ ] ⚡ Настроить shadcn/ui: добавить компоненты `Button`, `Input`, `Textarea`, `Badge`, `Dialog`, `Table`, `Select`, `Checkbox`, `Spinner`, `Toast`
- [ ] Реализовать `SSEListener` — React-хук `useSSE(url, onChunk, onDone, onError)`
- [ ] Реализовать `ConfidenceBadge` — иконка/метка для слов с `confidence_flag`
- [ ] Реализовать `LanguageBadge` — флаг + код языка

### 8.3 shared/lib

- [ ] Реализовать `levenshtein(a, b): number`
- [ ] Реализовать `formatDate(date, locale)` и `formatPercent(value)`
- [ ] Реализовать `debounce(fn, delay)`
- [ ] Реализовать `sha256(text): Promise<string>` (Web Crypto API)

### 8.4 shared/config

- [ ] Определить `ROUTES` константы: все пути приложения
- [ ] Создать `env.ts`: типизированные переменные окружения (VITE_API_URL)

---

## Фаза 9 — Frontend: аутентификация

**Depends:** Фаза 8, Фаза 2.8

### 9.1 entities/user

- [ ] ⚡ Определить типы `User`, `UserRole`
- [ ] ⚡ Реализовать Zustand store `useAuthStore`:
  - состояние: `user`, `accessToken`, `isAuthenticated`
  - методы: `setAuth()`, `logout()`, `refreshToken()`
  - accessToken хранится только в памяти (не localStorage)

### 9.2 features/auth

- [ ] ⚡ Реализовать `authApi`: `login()`, `register()`, `logout()`, `refresh()`
- [ ] ⚡ Реализовать `LoginForm`: поля email/password, валидация Zod, обработка ошибок API
- [ ] Реализовать `RegisterForm`: поля email/password/display_name, валидация

### 9.3 app/router

- [ ] ⚡ Реализовать `AuthGuard`: редирект на `/login` если нет токена
- [ ] Реализовать `AdminGuard`: редирект на `/` если нет роли admin/moderator
- [ ] ⚡ Настроить `RouterProvider` с маршрутами: `/login`, `/register`, защищённые роуты

### 9.4 pages

- [ ] ⚡ Реализовать `LoginPage`: центрированная форма, ссылка на регистрацию
- [ ] Реализовать `RegisterPage`

---

## Фаза 10 — Frontend: блоки и слова

**Depends:** Фаза 9, Фаза 3.5

### 10.1 entities/block

- [ ] ⚡ Определить типы `WordBlock`, `BlockFilter`
- [ ] ⚡ Реализовать `blockApi`: `getBlocks()`, `getBlockById()`, `createBlock()`, `updateBlock()`, `deleteBlock()`
- [ ] ⚡ Реализовать TanStack Query хуки: `useBlocks(filter)`, `useBlock(id)`, `useCreateBlockMutation()`, `useDeleteBlockMutation()`
- [ ] Реализовать `BlockCard` UI-компонент: заголовок, язык, количество слов, теги, кнопки

### 10.2 entities/word

- [ ] ⚡ Определить типы `Word`, `WordType`
- [ ] ⚡ Реализовать `wordApi`: `getWords()`, `createWord()`, `updateWord()`, `deleteWord()`, `importWords()`
- [ ] ⚡ Реализовать TanStack Query хуки: `useWords(blockId)`, `useCreateWordMutation()`, `useUpdateWordMutation()`, `useDeleteWordMutation()`, `useImportWordsMutation()`
- [ ] Реализовать `WordRow` — строка таблицы с inline-редактированием
- [ ] Реализовать `WordTypeBadge` — цветной бейдж типа слова

### 10.3 widgets/BlockList

- [ ] Реализовать `BlockList`: сетка карточек `BlockCard` с пагинацией
- [ ] Реализовать `BlockFilters`: выпадающие списки языка и тега

### 10.4 pages

- [ ] ⚡ Реализовать `BlockListPage`: виджет `BlockList` + кнопка «Создать блок» + `BlockFilters`
- [ ] ⚡ Реализовать `BlockDetailPage`:
  - заголовок блока (inline-редактирование)
  - таблица слов `WordRow` с пагинацией
  - кнопки: «Добавить слово», «Импортировать», «Создать тест»
  - фильтр по `confidence_flag` («показать только спорные»)
- [ ] Реализовать `CreateBlockModal` (диалог для быстрого создания блока)

---

## Фаза 11 — Frontend: AI-импорт слов

**Depends:** Фаза 10, Фаза 4.4

### 11.1 features/import-words

- [ ] ⚡ Реализовать `formatWords.ts`: SSE-клиент через `fetch` + `ReadableStream`
  - парсинг `event: ...` / `data: ...` построчно
  - возвращает `AsyncGenerator<FormatChunk>`
- [ ] ⚡ Реализовать Zustand store `useImportWordsStore`:
  - шаги: `input` → `formatting` → `preview` → `saving` → `done`
  - состояние: rawText, targetLanguage, nativeLanguage, formattedWords, suggestedTitle, error
  - `persist` в `sessionStorage` (защита от перезагрузки)
- [ ] Реализовать `validateImportInput`: rawText не пустой, выбраны языки

### 11.2 UI-компоненты

- [ ] ⚡ Реализовать `RawTextInput`: большой textarea + селекторы языков
- [ ] ⚡ Реализовать `FormatProgress`: анимация + стриминговые сообщения от SSE
- [ ] ⚡ Реализовать `WordPreviewTable`: редактируемая таблица
  - колонки: term, translation, word_type (select), confidence (иконка)
  - строки с `confidence_flag: true` — желтый фон
  - inline-редактирование каждой ячейки
  - кнопки: удалить строку, добавить строку вручную
- [ ] Реализовать `ImportErrorBanner`: показ ошибки AI + кнопка «Попробовать снова»
- [ ] Реализовать `BlockTitleInput`: поле с предложенным AI названием блока

### 11.3 pages

- [ ] ⚡ Реализовать `WordImportPage`:
  - Шаг 1: `RawTextInput` + кнопка «Форматировать»
  - Шаг 2: `FormatProgress` (SSE в реальном времени)
  - Шаг 3: `WordPreviewTable` + `BlockTitleInput` + кнопка «Сохранить блок»
  - при сохранении: `useImportWordsMutation` → редирект на `/blocks/{id}`
  - при перезагрузке страницы: показывать «Восстановить черновик?»

---

## Фаза 12 — Frontend: тесты

**Depends:** Фаза 10, Фаза 5.4

### 12.1 entities/test

- [ ] ⚡ Определить типы `Test`, `Question`, `QuestionType`, `TestAttempt`, `AttemptAnswer`
- [ ] ⚡ Реализовать `testApi`: `getTests()`, `getTestById()`, `generateTest()`, `deleteTest()`
- [ ] ⚡ Реализовать `attemptApi`: `startAttempt()`, `submitAnswer()`, `finishAttempt()`, `getAttemptResults()`
- [ ] Реализовать TanStack Query хуки для тестов и попыток

### 12.2 features/generate-test

- [ ] ⚡ Реализовать Zustand store `useGenerateTestStore`:
  - выбранные блоки, типы вопросов, количество вопросов
- [ ] Реализовать `BlockSelector`: список блоков с чекбоксами
- [ ] Реализовать `QuestionTypeSelector`: чекбоксы типов вопросов
- [ ] Реализовать polling: `useTestStatusPoller(testId)` — опрашивает `GET /api/tests/{id}` раз в 2 сек до `status === 'ready'`

### 12.3 features/run-test

- [ ] ⚡ Реализовать Zustand store `useTestRunnerStore`:
  - currentQuestionIndex, answers, isFinished
  - метод `submitAnswer(questionId, answer, timeMs)`
- [ ] ⚡ Реализовать `SingleChoiceQuestion`: 4 кнопки, клик → подсветка + показ правильного
- [ ] ⚡ Реализовать `FillInBlankQuestion`: input с подсказкой `___`, кнопка «Проверить»
- [ ] Реализовать `MultiSelectQuestion`: чекбоксы + кнопка «Проверить»
- [ ] Реализовать `OpenAnswerQuestion`: input + клиентская Levenshtein-проверка для UX (окончательная проверка — на сервере)
- [ ] Реализовать `AnswerFeedback`: зелёный/красный баннер + правильный ответ + notes слова
- [ ] Реализовать `TestProgressBar`: шаг X из Y + процент правильных

### 12.4 pages

- [ ] ⚡ Реализовать `TestCreatePage`: выбор блоков + типов + количества → кнопка «Создать»
  - после создания: показ спиннера + polling статуса → редирект на `/tests/{id}` когда ready
- [ ] Реализовать `TestListPage`: список тестов с фильтром по статусу
- [ ] ⚡ Реализовать `TestRunnerPage`: оркестрирует вопросы → `features/run-test` компоненты
- [ ] ⚡ Реализовать `TestResultsPage`:
  - итоговый балл + кольцевая диаграмма (recharts)
  - список неправильных ответов с правильными
  - кнопки: «Пройти снова», «Вернуться к блоку»

---

## Фаза 13 — Frontend: Spaced Repetition

**Depends:** Фаза 10, Фаза 6

### 13.1 features/review-word

- [ ] Реализовать `reviewApi`: `getDueWords()`, `rateWord(wordId, quality)`
- [ ] Реализовать TanStack Query хуки: `useDueWords()`, `useRateWordMutation()`
- [ ] Реализовать `ReviewCard`:
  - лицевая сторона: term слова
  - обратная сторона: translation + notes + example_sentence
  - кнопка «Показать перевод»
- [ ] Реализовать `QualityRater`: 6 кнопок (0–5) с подписями («Забыл», «Сложно», «Нормально», «Легко», «Отлично», «Идеально»)

### 13.2 widgets

- [ ] Реализовать `ReviewDueBanner`: баннер на Dashboard — «Сегодня к повторению: N слов» + кнопка «Начать»

### 13.3 pages

- [ ] Реализовать `ReviewSessionPage`:
  - показывает слова одно за одним
  - прогресс: X из N пройдено
  - по завершении: итог (сколько сложных, лёгких)

---

## Фаза 14 — Frontend: Админ-панель UI

**Depends:** Фаза 9, Фаза 7

### 14.1 entities/admin

- [ ] Определить типы `AdminUser`, `AiCallLog`, `AuditLog`, `SystemSetting`
- [ ] Реализовать `adminApi`: все запросы к `/api/admin/*`

### 14.2 features/admin-ai-monitor

- [ ] Реализовать `AiMetricsChart` (recharts): avg duration по времени, success rate
- [ ] Реализовать `AiLogTable` (@tanstack/react-table): пагинация + фильтры

### 14.3 features/admin-users

- [ ] Реализовать `UsersTable`: все колонки + действия (suspend, delete, change role)
- [ ] Реализовать `UserDetailModal`: детали + история действий

### 14.4 widgets/AdminNav

- [ ] Реализовать `AdminNav`: sidebar с разделами (Dashboard, Users, AI Monitor, Settings, Languages, Audit)

### 14.5 pages/Admin

- [ ] Реализовать `AdminDashboardPage`: 4 метрики-карточки + 2 графика
- [ ] Реализовать `AdminUsersPage`: таблица юзеров + модалки действий
- [ ] Реализовать `AdminAiMonitorPage`: метрики + таблица логов
- [ ] Реализовать `AdminSettingsPage`: таблица настроек + inline-редактирование
- [ ] Реализовать `AdminLanguagesPage`: список языков + добавить/включить/выключить
- [ ] Реализовать `AdminAuditPage`: таблица аудита с фильтрами

---

## Фаза 15 — Поиск, теги, экспорт

**Depends:** Фаза 3, Фаза 10

### 15.1 Backend — Поиск

- [ ] Реализовать `SearchWordsQuery` + Handler:
  - PostgreSQL full-text search через GIN-индекс + `unaccent`
  - trigram fallback для коротких запросов (pg_trgm)
  - фильтр по языку
- [ ] Добавить в `WordsController`: `GET /api/search?q=...&lang=en`

### 15.2 Backend — Теги

- [ ] Реализовать `TagRepository`
- [ ] Реализовать `AddTagToBlockCommand` + Handler
- [ ] Реализовать `RemoveTagFromBlockCommand` + Handler
- [ ] Реализовать `GetUserTagsQuery` + Handler
- [ ] Добавить эндпоинты тегов в `BlocksController`

### 15.3 Backend — Экспорт/Импорт

- [ ] Реализовать `ExportBlockCommand` + Handler (CSV формат)
- [ ] Реализовать `ImportBlockFromCsvCommand` + Handler:
  - парсинг CSV, создание блока и слов
  - валидация: максимум 500 слов в файле
- [ ] Добавить эндпоинты: `GET /api/blocks/{id}/export?format=csv`, `POST /api/blocks/import`

### 15.4 Frontend — Поиск

- [ ] Добавить `SearchBar` в header: debounce 300ms, глобальный поиск
- [ ] Реализовать `SearchResultsPage` или `SearchDropdown` с результатами

### 15.5 Frontend — Теги

- [ ] Реализовать `TagInput`: autocomplete из существующих тегов + создание новых
- [ ] Добавить теги в `BlockCard` и `BlockDetailPage`
- [ ] Добавить фильтр по тегу в `BlockFilters`

### 15.6 Frontend — Экспорт/Импорт

- [ ] Реализовать кнопку «Экспорт CSV» в `BlockDetailPage`
- [ ] Реализовать `CsvImportModal`: drag&drop загрузка файла + превью первых 10 строк

---

## Фаза 16 — Уведомления и фоновые задачи

**Depends:** Фаза 6, Фаза 7

### 16.1 Backend — Hangfire настройка

- [ ] Настроить Hangfire: PostgreSQL storage, dashboard на `/hangfire` (только для admin)
- [ ] Настроить retry-политику: 3 попытки с экспоненциальным backoff
- [ ] Зарегистрировать recurring jobs

### 16.2 Backend — Recurring Jobs

- [ ] Реализовать `CleanupRefreshTokensJob` (ежесуточно): удаление истёкших токенов
- [ ] Реализовать `AnonymizeDeletedUsersJob` (ежесуточно): анонимизация через 30 дней
- [ ] Реализовать `SendReviewRemindersJob` (ежедневно в 8:00):
  - находит юзеров у которых `review due count > 0`
  - отправляет email через `IEmailService`
- [ ] Реализовать `CleanupAiLogsJob` (раз в месяц): удаление логов старше 12 месяцев

### 16.3 Backend — Email Service

- [ ] Создать интерфейс `IEmailService` с методом `SendAsync(to, subject, htmlBody)`
- [ ] Реализовать `SmtpEmailService` (через MailKit)
- [ ] Реализовать шаблоны писем: reminder, welcome, password-reset

### 16.4 Frontend — Dashboard виджет напоминания

- [ ] `ReviewDueBanner` показывает счётчик только если `dueWords.length > 0`
- [ ] Добавить `DashboardPage` с виджетами: ReviewDueBanner, последние блоки, последние тесты, статистика недели

---

## Фаза 17 — Тесты (автотесты)

**Depends:** Фаза 2, Фаза 3, Фаза 5

### 17.1 Domain Tests

- [ ] `WordTests`: тест `Word.Create()` с невалидными данными бросает `DomainException`
- [ ] `WordTests`: тест `Word.ApplyReviewResult()` — корректные SM-2 вычисления для quality 0, 3, 5
- [ ] `WordTests`: тест `Word.ApplyReviewResult()` — ease_factor не опускается ниже 1.3
- [ ] `TestScoreTests`: тест `TestScore.From()` — корректный процент
- [ ] `SpacedRepetitionServiceTests`: тест SM-2 алгоритма против эталонных значений

### 17.2 Application Tests

- [ ] `ImportWordsCommandTests`: успешный импорт 5 слов → 5 записей в репозитории
- [ ] `ImportWordsCommandTests`: импорт > 200 слов → `ValidationException`
- [ ] `ImportWordsCommandTests`: чужой blockId → `Result.Forbidden`
- [ ] `ImportWordsCommandTests`: несуществующий blockId → `Result.NotFound`
- [ ] `SubmitAnswerCommandTests`: correct open answer (exact) → `isCorrect: true`
- [ ] `SubmitAnswerCommandTests`: answer with 1 typo → `isCorrect: true` (Levenshtein)
- [ ] `SubmitAnswerCommandTests`: answer with 3 typos → `isCorrect: false`
- [ ] `GenerateTestCommandTests`: < 5 слов → `ValidationException`
- [ ] `GenerateTestCommandTests`: чужие blockIds → `ValidationException` / `Forbidden`
- [ ] `FinishAttemptCommandTests`: 3 из 5 правильных → score: 0.6
- [ ] `ReviewWordCommandTests`: quality 5 → ease_factor увеличился
- [ ] `ReviewWordCommandTests`: quality 0 → repetitions сброшен в 0, interval_days = 1

### 17.3 Integration Tests (WebApplicationFactory)

- [ ] `AuthControllerTests`: register → login → получить protected endpoint
- [ ] `AuthControllerTests`: expired access token → 401
- [ ] `AuthControllerTests`: refresh token → новый access token
- [ ] `BlocksControllerTests`: создать блок → получить список → блок в списке
- [ ] `BlocksControllerTests`: запрос чужого блока → 404
- [ ] `WordsControllerTests`: импорт 10 слов → GET блок → word_count == 10
- [ ] `TestsControllerTests`: создать тест → статус generating → (mock Hangfire) → статус ready
- [ ] `FormatWordsTests`: SSE endpoint → получить события parsing, streaming, done

---

## Фаза 18 — DevOps и деплой

**Depends:** Фаза 17

### 18.1 Docker

- [ ] Написать `Dockerfile` для backend (multi-stage: build + runtime)
- [ ] Написать `Dockerfile` для frontend (build + nginx)
- [ ] Обновить `docker-compose.yml` для production: все сервисы + healthchecks
- [ ] Добавить `nginx.conf`: проксирование `/api/*` → backend, `/` → frontend SPA
- [ ] Настроить healthcheck эндпоинт: `GET /api/health`

### 18.2 CI/CD

- [ ] Создать GitHub Actions workflow: `build-and-test.yml`
  - запуск на каждый PR: `dotnet build`, `dotnet test`, `npm run build`, `npm run lint`
- [ ] Создать workflow `deploy.yml`:
  - запуск на push в `main`
  - сборка Docker images
  - пуш в registry
  - деплой (SSH + docker-compose pull + up)
- [ ] Настроить secrets в GitHub: DATABASE_URL, REDIS_URL, JWT_SECRET, OPENAI_API_KEY

### 18.3 Мониторинг

- [ ] Добавить `Serilog` + sink в файл + sink в seq (или Loki)
- [ ] Настроить `structured logging`: каждый лог содержит RequestId, UserId, TraceId
- [ ] Добавить `prometheus-net`: метрики HTTP запросов, DB connections, Hangfire jobs
- [ ] Создать `docker-compose.monitoring.yml`: Prometheus + Grafana

### 18.4 Безопасность

- [ ] Добавить `Helmet`-аналог для ASP.NET: Security Headers middleware (HSTS, CSP, X-Frame-Options)
- [ ] Настроить rate limiting для auth эндпоинтов: 10 попыток / 15 минут / IP
- [ ] Настроить backup PostgreSQL: ежесуточный dump в S3/Backblaze
- [ ] Провести audit: проверить все эндпоинты на наличие авторизации и ownership check

---

## Фаза 19 — v2: мобильное приложение

**Depends:** Фаза 18 (стабильный API)

### 19.1 Инициализация

- [ ] Создать React Native проект: `npx create-expo-app mobile --template blank-typescript`
- [ ] Настроить навигацию: `expo-router` или `react-navigation`
- [ ] Переиспользовать `shared/api`, `shared/lib`, `entities/` типы из web-проекта (monorepo shared package)

### 19.2 Аутентификация

- [ ] Реализовать хранение токенов в `expo-secure-store` (Keychain/Keystore)
- [ ] Адаптировать `authApi` для мобильного окружения

### 19.3 Основные экраны

- [ ] Экран: BlockList (список блоков)
- [ ] Экран: BlockDetail (слова блока)
- [ ] Экран: WordImport (ввод + форматирование, без SSE-preview — только результат)
- [ ] Экран: TestCreate + TestRunner
- [ ] Экран: ReviewSession
- [ ] Экран: Dashboard (статистика + due words)

### 19.4 Offline режим

- [ ] Настроить `@tanstack/react-query` с persistQueryClient + AsyncStorage
- [ ] Реализовать оффлайн-стратегию: кэш блоков и слов доступен без интернета
- [ ] Синхронизация ответов после восстановления соединения

### 19.5 Push-уведомления

- [ ] Настроить `expo-notifications`
- [ ] Backend: `POST /api/mobile/register-push-token` — сохранить токен устройства
- [ ] Интегрировать в `SendReviewRemindersJob`: push вместо email для мобильных юзеров

---

## Сводная таблица фаз

| Фаза | Название | Недели | Зависит от | MVP |
|---|---|---|---|---|
| 0 | Инфраструктура | 0.5 | — | ⚡ |
| 1 | База данных | 1 | 0 | ⚡ |
| 2 | Backend: Auth | 1 | 1 | ⚡ |
| 3 | Backend: Слова/Блоки | 1 | 2 | ⚡ |
| 4 | Backend: AI | 1 | 2, 3 | ⚡ |
| 5 | Backend: Тесты | 1 | 3, 4 | ⚡ |
| 6 | Backend: Spaced Rep. | 0.5 | 3 | — |
| 7 | Backend: Admin API | 1 | 2, 3, 4 | — |
| 8 | Frontend: Shared | 0.5 | 0 | ⚡ |
| 9 | Frontend: Auth | 0.5 | 8, 2 | ⚡ |
| 10 | Frontend: Блоки/Слова | 1 | 9, 3 | ⚡ |
| 11 | Frontend: AI-импорт | 1 | 10, 4 | ⚡ |
| 12 | Frontend: Тесты | 1.5 | 10, 5 | ⚡ |
| 13 | Frontend: Spaced Rep. | 0.5 | 10, 6 | — |
| 14 | Frontend: Admin UI | 1 | 9, 7 | — |
| 15 | Поиск, теги, экспорт | 1 | 3, 10 | — |
| 16 | Уведомления / Jobs | 0.5 | 6, 7 | — |
| 17 | Автотесты | 1 | 2–5 | — |
| 18 | DevOps | 1 | 17 | — |
| 19 | Мобилка v2 | 4+ | 18 | — |

**MVP (фазы 0–5, 8–12):** ~9–10 недель  
**v1.1 (+ фазы 6, 13–16):** ещё 3–4 недели  
**v1.2 (+ фазы 17–18):** ещё 2 недели  
**v2.0 (фаза 19):** 4+ недели отдельного трека

---

## Критический путь MVP

```
0 (Инфраструктура)
    → 1 (БД)
        → 2 (Auth Backend)
            → 3 (Words/Blocks Backend)
                → 4 (AI Backend)    → 5 (Tests Backend)
                                            ↓
8 (Frontend Shared)                         |
    → 9 (Frontend Auth)                     |
        → 10 (Frontend Blocks)              |
            → 11 (Frontend AI Import)       |
            → 12 (Frontend Tests) ←─────────┘
```

Задачи с ⚡ лежат на критическом пути. Всё остальное можно делать параллельно или откладывать.
