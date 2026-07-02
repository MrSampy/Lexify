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
| AI | Ollama (qwen3:8b), OpenAI fallback |
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

Запускает: PostgreSQL 16 (порт 5432), Redis 7 (порт 6379). Ollama запускается нативно (см. ниже), не в Docker.

### 2. Установить Ollama и скачать AI-модель

**Основной способ (локальная разработка): нативный Ollama на Windows**, а не в Docker — меньше накладных расходов и прямой доступ к GPU-ускорению. NPU из Task Manager пока не используется Ollama (нет backend'а под NPU) — ускорение будет только CPU/GPU.

```bash
# Установка: https://ollama.com/download/windows, или winget install Ollama.Ollama
ollama pull qwen3:8b
```

`Ollama__BaseUrl` в `appsettings.Development.json` уже указывает на `http://localhost:11434`, поэтому `dotnet run` подхватит нативный Ollama без правок конфига.

**Запасной способ: Ollama в Docker** (старый способ, по-прежнему работает):

```bash
docker-compose up -d          # поднимет также контейнер ollama на порту 11434
docker exec -it lexify-ollama ollama pull qwen3:8b
```

Нативный и Docker-Ollama не могут одновременно занимать порт 11434 — перед переключением остановите один из них.

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

## Архитектура

Детали архитектуры: [`Info/lexify-architecture.md`](Info/lexify-architecture.md)  
Схема базы данных: [`Info/lexify-database.md`](Info/lexify-database.md)  
Список задач: [`Info/lexify-tasks.md`](Info/lexify-tasks.md)

## Лицензия

MIT
