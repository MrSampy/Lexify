# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## Commands

### Backend (.NET 8)

```bash
# Run API (from repo root)
dotnet run --project backend/src/Lexify.API

# Build entire solution
dotnet build backend/Lexify.slnx

# Run all tests
dotnet test backend/Lexify.slnx

# Run tests for a single project
dotnet test backend/tests/Lexify.Domain.Tests
dotnet test backend/tests/Lexify.Application.Tests
dotnet test backend/tests/Lexify.API.Tests

# Apply EF migrations
dotnet ef database update --project backend/src/Lexify.Infrastructure --startup-project backend/src/Lexify.API

# Add a new migration
dotnet ef migrations add <MigrationName> --project backend/src/Lexify.Infrastructure --startup-project backend/src/Lexify.API
```

### Frontend (React + Vite)

```bash
cd frontend
npm run dev        # dev server on http://localhost:5173
npm run build      # tsc -b && vite build (type-check + bundle)
npm run lint       # eslint (FSD boundary rules enforced)
npm run preview    # preview production build
```

### Infrastructure (Docker)

```bash
# Start PostgreSQL 16 and Redis (AI runs on Ollama Cloud — see AI section)
docker compose up -d postgres redis
```

### AI — Ollama Cloud

The app uses **Ollama Cloud** (`https://ollama.com`) via its OpenAI-compatible `/v1/chat/completions`
endpoint — no local model download or GPU needed. Default model: **`gemma4:31b`** (multilingual,
non-thinking instruct → clean streaming JSON). The provider entry lives in the `AiProviders` list in
`appsettings.Development.json` (`BaseUrl: https://ollama.com`).

**The API key is never committed.** Supply it locally via user-secrets (overrides the empty `ApiKey`
in appsettings at runtime):

```bash
dotnet user-secrets init --project backend/src/Lexify.API   # one-time (adds UserSecretsId)
dotnet user-secrets set "AiProviders:0:ApiKey" "<your-ollama-cloud-key>" --project backend/src/Lexify.API
```

In Docker/prod the key comes from the `OLLAMA_API_KEY` env var (see `.env.example`), bound to
`AiProviders__0__ApiKey`. Up to **three more keys** can be set there (`OLLAMA_API_KEY_2..4`, bound to
`AiProviders__1..3`) to spread load: `AIOrchestrator` round-robins the starting key per request across
keys sharing an endpoint and falls back to the rest on error/429. Extra keys are optional — unset ones
are dropped at startup, and distinct-endpoint providers (e.g. a Lemonade fallback) keep their order.

**Local Ollama instead of cloud** (optional): install from https://ollama.com/download/windows,
`ollama serve`, `ollama pull gemma4:31b`, then set the provider's `BaseUrl` to `http://localhost:11434`
and leave `ApiKey` empty.

---

## Architecture

### Monorepo layout

```
backend/   — .NET 8 Clean Architecture solution (Lexify.slnx)
frontend/  — React 19 + TypeScript SPA (Feature-Sliced Design)
Info/      — Architecture and task spec documents (source of truth for phases)
```

### Backend — Clean Architecture

Four projects; dependency direction: `API → Application → Domain ← Infrastructure`.

| Project | Role |
|---|---|
| `Lexify.Domain` | Entities, repository interfaces, domain services, value objects, domain events. No framework references. |
| `Lexify.Application` | CQRS handlers (MediatR), FluentValidation validators, AutoMapper profiles, pipeline behaviors, `Result<T>` pattern. |
| `Lexify.Infrastructure` | EF Core (Npgsql), repository implementations, JWT/BCrypt services, Hangfire jobs, Ollama/OpenAI HTTP clients (Polly). |
| `Lexify.API` | Controllers, middleware, Swagger (4 pages), rate limiting, CORS. |

**CQRS pipeline** (every MediatR request goes through all registered behaviors in order):
`ValidationBehavior` → `LoggingBehavior` → `CachingBehavior` → `TransactionBehavior` → Handler

**Result pattern**: handlers return `Result` / `Result<T>` (never throw for business logic). `BaseApiController.ToActionResult()` maps `ResultStatus` to HTTP status codes:
- `Ok` → 200, `NotFound` → 404, `Forbidden` → 403, `Failure` → 400

**Error handling middleware** (`ExceptionMiddleware`):
- `DomainException` → 400
- `ValidationException` (FluentValidation) → 422 with field-level errors dict
- Unhandled → 500

**DI entry points**: `builder.Services.AddApplication()` and `builder.Services.AddInfrastructure(config)`.

**AI**: `IAIProvider` is fulfilled by `AIOrchestrator`, which walks the ordered `AiProviders` config list — every entry speaks the OpenAI-compatible `/v1/chat/completions` protocol (Ollama, Lemonade, OpenAI, …) via the shared `OpenAiCompatibleClient`, trying each in turn and falling back on failure. Default dev chain: Ollama Cloud (`gemma4:31b`) → Lemonade. HTTP clients have a 2-retry Polly policy and per-provider timeout.

**Background jobs**: Hangfire with PostgreSQL storage, 2 workers. `IBackgroundJobService` is the abstraction; `GenerateTestJob` is the only job so far.

**Caching**: `ICacheService` backed by Redis when `ConnectionStrings:Redis` is configured, otherwise `NullCacheService` (no-op). Queries opt in by implementing `ICacheable`.

### Frontend — Feature-Sliced Design (FSD)

Layers (top → bottom, dependency direction goes down only):

```
app → pages → widgets → features → entities → shared
```

ESLint (`eslint-plugin-boundaries`) enforces this at import time — a lower layer cannot import from a higher one. The one deliberate exception: `shared/api/base.ts` cannot import `entities/user`, so it uses a **callback injection pattern** (`setAuthHandlers()`) that `entities/user` calls at boot.

**`shared/` layout** (Phase 8 — complete):

| Sub-dir | Contents |
|---|---|
| `shared/api` | `apiClient` (axios), `setAuthHandlers()`, `PagedResult<T>` / `ApiError` / `Result<T>` types |
| `shared/config` | `ROUTES` constants, `env` (typed `VITE_API_URL`, `VITE_API_TIMEOUT_MS`) |
| `shared/lib` | `levenshtein`, `formatDate`, `formatPercent`, `debounce`, `sha256` |
| `shared/ui` | shadcn/ui re-exports, `Spinner`, `ConfidenceBadge`, `LanguageBadge`, `useSSE` hook |

shadcn/ui components live in `src/components/ui/` and are re-exported from `shared/ui/index.ts`. Install new components with `npx shadcn@latest add <name>` from the `frontend/` directory, then move from the literal `@/components/ui/` directory that shadcn creates into `src/components/ui/`.

**Path alias**: `@/` resolves to `frontend/src/` (configured in `vite.config.ts` and `tsconfig.app.json`).

**`useSSE` hook**: uses `fetch` + `ReadableStream` (not `EventSource`) because the SSE endpoint (`POST /api/words/format`) requires a POST body. Parses `data:` lines, handles `[DONE]` sentinel.

**State**: Zustand (not yet wired — Phase 9). Access tokens are stored in memory only (never `localStorage`).

**Data fetching**: TanStack Query v5 (not yet wired — Phase 9+).

### Key domain concepts

- **WordBlock** — a named collection of words in one language, owned by one user.
- **Word** — a vocabulary item with SM-2 spaced-repetition fields (`easeFactor`, `intervalDays`, `nextReviewAt`). `confidenceFlag` marks words that need review.
- **Test** — AI-generated quiz linked to one or more blocks. Status lifecycle: `Generating → Ready → Archived`. Generation is async via Hangfire.
- **TestAttempt** — one user's run through a test; answers submitted one at a time; finishing triggers SM-2 updates.
- **SM-2**: implemented in `Domain/Services/SpacedRepetitionService.cs`. Quality is 0–5; ease factor and interval recalculated on each `ReviewWordCommand`.

### API surface

| Group | Base path | Swagger page |
|---|---|---|
| Auth | `/api/auth` | auth |
| Blocks + Words | `/api/blocks`, `/api/blocks/{id}/words` | content |
| Tests + Attempts + Review | `/api/tests`, `/api/attempts`, `/api/review` | learning |
| Admin | `/api/admin` | admin |

Rate limiting (Redis sliding window): `POST /api/words/format` — 10 req/min/user; `POST /api/tests/generate` — 5 req/hr/user.

CORS allows `http://localhost:5173` (Vite dev server) with credentials.

### Database

PostgreSQL 16. EF Core migrations are in `Lexify.Infrastructure/Persistence/Migrations/`. `DatabaseInitializer` runs migrations and `DataSeeder` (9 languages, 8 system settings, admin user from env) on startup.

Notable SQL features used: `unaccent()` wrapped in `IMMUTABLE` function, GIN full-text search index, `pg_trgm` trigram index, RLS policies for `lexify_app` and `lexify_admin` roles, DB-level triggers for word/question counts and email normalization.

### Phase tracking

`Info/lexify-tasks.md` is the authoritative task list (19 phases). `Info/lexify-architecture.md` is the architecture spec. Always consult these before starting a new phase.

Current status: Phases 0–2 (backend infra, DB, auth) and Phase 8 (frontend shared layer) are complete. Phase 3 (backend words/blocks) and Phase 9 (frontend auth) are next.
