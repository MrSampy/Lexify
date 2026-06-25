# Lexify — Архитектура приложения

> Документ описывает полную архитектуру системы: Clean Architecture + CQRS + паттерны на бэкенде, Feature-Sliced Design на фронтенде.

---

## Содержание

1. [Принципы и выбор подходов](#1-принципы-и-выбор-подходов)
2. [Backend — Clean Architecture](#2-backend--clean-architecture)
   - 2.1 [Слои и зависимости](#21-слои-и-зависимости)
   - 2.2 [Domain Layer](#22-domain-layer)
   - 2.3 [Application Layer (CQRS + MediatR)](#23-application-layer-cqrs--mediatr)
   - 2.4 [Infrastructure Layer](#24-infrastructure-layer)
   - 2.5 [Presentation Layer (API)](#25-presentation-layer-api)
   - 2.6 [Паттерны и их применение](#26-паттерны-и-их-применение)
   - 2.7 [Полная структура проекта](#27-полная-структура-проекта)
3. [Frontend — Feature-Sliced Design](#3-frontend--feature-sliced-design)
   - 3.1 [Почему FSD](#31-почему-fsd)
   - 3.2 [Слои FSD](#32-слои-fsd)
   - 3.3 [Полная структура проекта](#33-полная-структура-проекта)
   - 3.4 [Паттерны состояния](#34-паттерны-состояния)
   - 3.5 [Типизация и контракты](#35-типизация-и-контракты)
4. [Взаимодействие Frontend ↔ Backend](#4-взаимодействие-frontend--backend)
5. [Сквозные паттерны (Cross-cutting)](#5-сквозные-паттерны-cross-cutting)
6. [Диаграммы потоков](#6-диаграммы-потоков)

---

## 1. Принципы и выбор подходов

### Ключевые принципы

| Принцип                   | Применение                                                       |
| ------------------------- | ---------------------------------------------------------------- |
| **Dependency Rule**       | Зависимости направлены только внутрь (к Domain)                  |
| **Single Responsibility** | Каждый класс/модуль — одна причина изменения                     |
| **Open/Closed**           | Новые AI-провайдеры добавляются без изменения существующего кода |
| **Interface Segregation** | `IWordRepository` отдельно от `IBlockRepository`                 |
| **Dependency Inversion**  | Application зависит от абстракций, не от EF Core напрямую        |

### Почему Clean Architecture для бэкенда

Для Lexify это обоснованный выбор по трём причинам:

1. **Сменяемость AI-провайдера** — сегодня Ollama, завтра другая модель или облако. Интерфейс в Application, реализация в Infrastructure — замена без касания бизнес-логики.
2. **Тестируемость** — Domain и Application тестируются без БД и HTTP. Юнит-тесты работают в изоляции.
3. **Рост команды** — чёткие границы слоёв устраняют споры «куда положить этот код».

### Почему Feature-Sliced Design для фронтенда

FSD — архитектурная методология, специально созданная для React-приложений средней и большой сложности. В отличие от grouping-by-type (папки `components/`, `hooks/`, `pages/`), FSD группирует по **фичам и слоям**, что даёт:

1. **Изоляция фич** — изменение TestRunner не ломает WordImport
2. **Явные зависимости** — нижние слои не импортируют из верхних (машинально контролируется линтером)
3. **Масштабирование** — добавление новой страницы не требует трогать существующий код

---

## 2. Backend — Clean Architecture

### 2.1 Слои и зависимости

```
┌─────────────────────────────────────────────────────┐
│              Presentation Layer                      │
│         (Controllers, Middleware, DTOs)              │
└────────────────────────┬────────────────────────────┘
                         │ depends on
┌────────────────────────▼────────────────────────────┐
│              Application Layer                       │
│   (Commands, Queries, Handlers, Interfaces,          │
│    Validators, Mappings)                             │
└────────────────────────┬────────────────────────────┘
                         │ depends on
┌────────────────────────▼────────────────────────────┐
│                Domain Layer                          │
│   (Entities, Value Objects, Domain Events,           │
│    Domain Services, Repository Interfaces)           │
└─────────────────────────────────────────────────────┘
                         ▲
                         │ implements
┌────────────────────────┴────────────────────────────┐
│             Infrastructure Layer                     │
│   (EF Core, Redis, Ollama client, OpenAI client,    │
│    Email, Hangfire, Migrations)                      │
└─────────────────────────────────────────────────────┘
```

**Правило зависимостей:** стрелки идут только внутрь. Infrastructure реализует интерфейсы, объявленные в Domain/Application, но Domain ничего не знает об EF Core или Ollama.

---

### 2.2 Domain Layer

Ядро системы. Содержит бизнес-сущности и правила. Не зависит ни от чего.

#### Entities

```csharp
// Lexify.Domain/Entities/Word.cs
public class Word : BaseEntity
{
    public Guid BlockId { get; private set; }
    public string Term { get; private set; }
    public string Translation { get; private set; }
    public WordType WordType { get; private set; }
    public string? Notes { get; private set; }
    public string? ExampleSentence { get; private set; }
    public bool ConfidenceFlag { get; private set; }
    public int SortOrder { get; private set; }

    // SM-2 Spaced Repetition
    public float EaseFactor { get; private set; } = 2.5f;
    public int IntervalDays { get; private set; } = 1;
    public int Repetitions { get; private set; } = 0;
    public DateTime NextReviewAt { get; private set; } = DateTime.UtcNow;

    // Domain Events
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    // Фабричный метод — единственный способ создать слово
    public static Word Create(
        Guid blockId,
        string term,
        string translation,
        WordType wordType,
        bool confidenceFlag = false)
    {
        if (string.IsNullOrWhiteSpace(term))
            throw new DomainException("Term cannot be empty");
        if (string.IsNullOrWhiteSpace(translation))
            throw new DomainException("Translation cannot be empty");

        var word = new Word
        {
            Id = Guid.NewGuid(),
            BlockId = blockId,
            Term = term.Trim(),
            Translation = translation.Trim(),
            WordType = wordType,
            ConfidenceFlag = confidenceFlag,
            CreatedAt = DateTime.UtcNow
        };

        word._domainEvents.Add(new WordCreatedEvent(word.Id, blockId));
        return word;
    }

    // Доменный метод — обновление по результату повторения (SM-2)
    public void ApplyReviewResult(int quality)
    {
        if (quality < 0 || quality > 5)
            throw new DomainException("Quality must be between 0 and 5");

        float newEase = EaseFactor + (0.1f - (5 - quality) * (0.08f + (5 - quality) * 0.02f));
        newEase = Math.Max(1.3f, newEase);

        if (quality < 3)
        {
            Repetitions = 0;
            IntervalDays = 1;
        }
        else
        {
            Repetitions++;
            IntervalDays = Repetitions switch
            {
                1 => 1,
                2 => 6,
                _ => (int)Math.Round(IntervalDays * newEase)
            };
        }

        EaseFactor = newEase;
        NextReviewAt = DateTime.UtcNow.AddDays(IntervalDays);
        _domainEvents.Add(new WordReviewedEvent(Id, quality, IntervalDays));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

```csharp
// Lexify.Domain/Entities/WordBlock.cs
public class WordBlock : BaseEntity
{
    public Guid UserId { get; private set; }
    public int LanguageId { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public int WordCount { get; private set; }

    private readonly List<Word> _words = new();
    public IReadOnlyList<Word> Words => _words;

    public static WordBlock Create(Guid userId, int languageId, string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Block title cannot be empty");

        return new WordBlock
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LanguageId = languageId,
            Title = title.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AddWord(Word word)
    {
        if (word.BlockId != Id)
            throw new DomainException("Word does not belong to this block");
        _words.Add(word);
        WordCount++;
    }

    public void Rename(string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            throw new DomainException("Title cannot be empty");
        Title = newTitle.Trim();
    }
}
```

#### Value Objects

```csharp
// Lexify.Domain/ValueObjects/Language.cs
public record Language(int Id, string Code, string Name)
{
    public static Language English => new(1, "en", "English");
    public static Language Norwegian => new(2, "no", "Norwegian");
    public static Language Ukrainian => new(3, "uk", "Ukrainian");
}
```

```csharp
// Lexify.Domain/ValueObjects/TestScore.cs
public record TestScore(int Correct, int Total)
{
    public float Percentage => Total == 0 ? 0 : (float)Correct / Total;
    public bool IsPassing => Percentage >= 0.6f;

    public static TestScore From(IEnumerable<bool> results)
    {
        var list = results.ToList();
        return new TestScore(list.Count(x => x), list.Count);
    }
}
```

#### Enums

```csharp
// Lexify.Domain/Enums/WordType.cs
public enum WordType { Word, Phrase, Idiom, Expression }

// Lexify.Domain/Enums/QuestionType.cs
public enum QuestionType
{
    TranslateToNative,
    TranslateToForeign,
    FillInSentence,
    MultiSelectTheme,
    OpenAnswer
}

// Lexify.Domain/Enums/UserRole.cs
public enum UserRole { User, Moderator, Admin }
```

#### Domain Events

```csharp
// Lexify.Domain/Events/
public record WordCreatedEvent(Guid WordId, Guid BlockId) : IDomainEvent;
public record WordReviewedEvent(Guid WordId, int Quality, int NewIntervalDays) : IDomainEvent;
public record TestCompletedEvent(Guid TestId, Guid UserId, TestScore Score) : IDomainEvent;
```

#### Repository Interfaces (объявлены в Domain)

```csharp
// Lexify.Domain/Repositories/IWordRepository.cs
public interface IWordRepository
{
    Task<Word?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Word>> GetByBlockIdAsync(Guid blockId, CancellationToken ct = default);
    Task<IReadOnlyList<Word>> GetDueForReviewAsync(Guid userId, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<Word>> GetDistractorPoolAsync(Guid userId, string languageCode, int count, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Word> words, CancellationToken ct = default);
    Task UpdateAsync(Word word, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

// Lexify.Domain/Repositories/IWordBlockRepository.cs
public interface IWordBlockRepository
{
    Task<WordBlock?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<WordBlock?> GetByIdWithWordsAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<WordBlock>> GetByUserIdAsync(Guid userId, BlockFilterDto filter, CancellationToken ct = default);
    Task<WordBlock> AddAsync(WordBlock block, CancellationToken ct = default);
    Task UpdateAsync(WordBlock block, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

// Lexify.Domain/Repositories/IUnitOfWork.cs
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
```

#### Domain Service

```csharp
// Lexify.Domain/Services/SpacedRepetitionService.cs
// Доменный сервис — логика, которая не принадлежит одной сущности
public class SpacedRepetitionService
{
    public WordReviewUpdate Calculate(Word word, int quality)
    {
        // Логика SM-2 — принадлежит домену, не инфраструктуре
        float newEase = Math.Max(1.3f,
            word.EaseFactor + (0.1f - (5 - quality) * (0.08f + (5 - quality) * 0.02f)));

        var (repetitions, intervalDays) = quality < 3
            ? (0, 1)
            : (word.Repetitions + 1, word.Repetitions switch
            {
                0 => 1,
                1 => 6,
                _ => (int)Math.Round(word.IntervalDays * newEase)
            });

        return new WordReviewUpdate(newEase, intervalDays, repetitions,
            DateTime.UtcNow.AddDays(intervalDays));
    }
}
```

---

### 2.3 Application Layer (CQRS + MediatR)

Application Layer реализует use cases через паттерн **CQRS** (Command Query Responsibility Segregation) с библиотекой **MediatR**. Каждый use case — отдельный класс.

#### Структура CQRS

```
Application/
├── Features/
│   ├── Words/
│   │   ├── Commands/
│   │   │   ├── ImportWords/
│   │   │   │   ├── ImportWordsCommand.cs
│   │   │   │   ├── ImportWordsCommandHandler.cs
│   │   │   │   └── ImportWordsCommandValidator.cs
│   │   │   ├── FormatWords/
│   │   │   │   ├── FormatWordsCommand.cs
│   │   │   │   └── FormatWordsCommandHandler.cs
│   │   │   └── ReviewWord/
│   │   │       ├── ReviewWordCommand.cs
│   │   │       └── ReviewWordCommandHandler.cs
│   │   └── Queries/
│   │       ├── GetWordsByBlock/
│   │       │   ├── GetWordsByBlockQuery.cs
│   │       │   └── GetWordsByBlockQueryHandler.cs
│   │       └── GetDueForReview/
│   │           ├── GetDueForReviewQuery.cs
│   │           └── GetDueForReviewQueryHandler.cs
│   ├── Blocks/
│   ├── Tests/
│   ├── Auth/
│   └── Admin/
├── Common/
│   ├── Behaviors/
│   │   ├── ValidationBehavior.cs
│   │   ├── LoggingBehavior.cs
│   │   ├── CachingBehavior.cs
│   │   └── TransactionBehavior.cs
│   ├── Interfaces/
│   │   ├── IAIProvider.cs
│   │   ├── ICacheService.cs
│   │   ├── IEmailService.cs
│   │   └── ICurrentUserService.cs
│   ├── Mappings/
│   │   └── WordMappingProfile.cs
│   └── Models/
│       ├── PagedResult.cs
│       └── Result.cs
└── DependencyInjection.cs
```

#### Command пример — ImportWords

```csharp
// Commands/ImportWords/ImportWordsCommand.cs
public record ImportWordsCommand(
    Guid BlockId,
    IReadOnlyList<WordInputDto> Words
) : IRequest<Result<ImportWordsResult>>;

public record WordInputDto(
    string Term,
    string Translation,
    string WordType,
    bool ConfidenceFlag,
    string? Notes
);

public record ImportWordsResult(int ImportedCount, IReadOnlyList<Guid> WordIds);
```

```csharp
// Commands/ImportWords/ImportWordsCommandHandler.cs
public class ImportWordsCommandHandler
    : IRequestHandler<ImportWordsCommand, Result<ImportWordsResult>>
{
    private readonly IWordBlockRepository _blockRepo;
    private readonly IWordRepository _wordRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public ImportWordsCommandHandler(
        IWordBlockRepository blockRepo,
        IWordRepository wordRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser)
    {
        _blockRepo = blockRepo;
        _wordRepo = wordRepo;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Result<ImportWordsResult>> Handle(
        ImportWordsCommand request, CancellationToken ct)
    {
        // 1. Загрузить блок и проверить владельца
        var block = await _blockRepo.GetByIdAsync(request.BlockId, ct);
        if (block is null)
            return Result<ImportWordsResult>.NotFound("Block not found");
        if (block.UserId != _currentUser.UserId)
            return Result<ImportWordsResult>.Forbidden();

        // 2. Создать доменные сущности
        var words = request.Words.Select(dto => Word.Create(
            blockId: request.BlockId,
            term: dto.Term,
            translation: dto.Translation,
            wordType: Enum.Parse<WordType>(dto.WordType, ignoreCase: true),
            confidenceFlag: dto.ConfidenceFlag
        )).ToList();

        // 3. Сохранить
        await _uow.BeginTransactionAsync(ct);
        await _wordRepo.AddRangeAsync(words, ct);
        await _uow.SaveChangesAsync(ct);
        await _uow.CommitAsync(ct);

        return Result<ImportWordsResult>.Ok(
            new ImportWordsResult(words.Count, words.Select(w => w.Id).ToList()));
    }
}
```

```csharp
// Commands/ImportWords/ImportWordsCommandValidator.cs
public class ImportWordsCommandValidator : AbstractValidator<ImportWordsCommand>
{
    public ImportWordsCommandValidator()
    {
        RuleFor(x => x.BlockId).NotEmpty();
        RuleFor(x => x.Words)
            .NotEmpty().WithMessage("Word list cannot be empty")
            .Must(w => w.Count <= 200).WithMessage("Maximum 200 words per import");

        RuleForEach(x => x.Words).ChildRules(word =>
        {
            word.RuleFor(w => w.Term)
                .NotEmpty()
                .MaximumLength(500);
            word.RuleFor(w => w.Translation)
                .NotEmpty()
                .MaximumLength(500);
            word.RuleFor(w => w.WordType)
                .Must(t => Enum.TryParse<WordType>(t, true, out _))
                .WithMessage("Invalid word type");
        });
    }
}
```

#### Command пример — FormatWords (с AI + SSE стримингом)

```csharp
// Commands/FormatWords/FormatWordsCommand.cs
// Стриминговые команды используют IAsyncEnumerable через особый handler
public record FormatWordsCommand(
    string RawText,
    string TargetLanguage,
    string NativeLanguage
) : IStreamRequest<FormatWordsChunk>;

public record FormatWordsChunk(string Stage, string? Data, bool IsFinal);
```

```csharp
// Commands/FormatWords/FormatWordsCommandHandler.cs
public class FormatWordsCommandHandler
    : IStreamRequestHandler<FormatWordsCommand, FormatWordsChunk>
{
    private readonly IAIProvider _ai;
    private readonly AIResponseValidator _validator;

    public async IAsyncEnumerable<FormatWordsChunk> Handle(
        FormatWordsCommand request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        yield return new FormatWordsChunk("parsing", null, false);

        var buffer = new StringBuilder();
        await foreach (var chunk in _ai.StreamFormatWordsAsync(
            request.RawText, request.TargetLanguage, request.NativeLanguage, ct))
        {
            buffer.Append(chunk);
            yield return new FormatWordsChunk("streaming", chunk, false);
        }

        var rawJson = buffer.ToString();
        var validationResult = _validator.Validate(rawJson, request.RawText);

        if (!validationResult.IsValid)
        {
            yield return new FormatWordsChunk("error", validationResult.ErrorMessage, true);
            yield break;
        }

        yield return new FormatWordsChunk("done", rawJson, true);
    }
}
```

#### Query пример — GetWordsByBlock

```csharp
// Queries/GetWordsByBlock/GetWordsByBlockQuery.cs
public record GetWordsByBlockQuery(
    Guid BlockId,
    int Page = 1,
    int PageSize = 50,
    string? Search = null
) : IRequest<Result<PagedResult<WordDto>>>;
```

```csharp
// Queries/GetWordsByBlock/GetWordsByBlockQueryHandler.cs
public class GetWordsByBlockQueryHandler
    : IRequestHandler<GetWordsByBlockQuery, Result<PagedResult<WordDto>>>
{
    private readonly IWordRepository _wordRepo;
    private readonly IWordBlockRepository _blockRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public async Task<Result<PagedResult<WordDto>>> Handle(
        GetWordsByBlockQuery request, CancellationToken ct)
    {
        var block = await _blockRepo.GetByIdAsync(request.BlockId, ct);
        if (block is null) return Result<PagedResult<WordDto>>.NotFound();
        if (block.UserId != _currentUser.UserId) return Result<PagedResult<WordDto>>.Forbidden();

        var words = await _wordRepo.GetByBlockIdAsync(request.BlockId, ct);
        var dtos = _mapper.Map<IReadOnlyList<WordDto>>(words);

        return Result<PagedResult<WordDto>>.Ok(
            PagedResult<WordDto>.From(dtos, request.Page, request.PageSize));
    }
}
```

#### Pipeline Behaviors

Behaviors — это middleware для MediatR pipeline. Оборачивают каждый Handler автоматически.

```csharp
// Common/Behaviors/ValidationBehavior.cs
// Автоматическая валидация через FluentValidation перед каждым Handler
public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}
```

```csharp
// Common/Behaviors/LoggingBehavior.cs
// Логирует каждый запрос с временем выполнения
public class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUser;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {Request} for user {UserId}",
            requestName, _currentUser.UserId);

        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (sw.ElapsedMilliseconds > 500)
            _logger.LogWarning("Slow request: {Request} took {ElapsedMs}ms",
                requestName, sw.ElapsedMilliseconds);

        return response;
    }
}
```

```csharp
// Common/Behaviors/CachingBehavior.cs
// Кэширует результаты Query-запросов в Redis
public class CachingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheable
{
    private readonly ICacheService _cache;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var cached = await _cache.GetAsync<TResponse>(request.CacheKey, ct);
        if (cached is not null) return cached;

        var response = await next();
        await _cache.SetAsync(request.CacheKey, response, request.CacheDuration, ct);
        return response;
    }
}

// Маркерный интерфейс для кэшируемых запросов
public interface ICacheable
{
    string CacheKey { get; }
    TimeSpan CacheDuration { get; }
}
```

#### Application Interfaces (объявлены здесь, реализованы в Infrastructure)

```csharp
// Common/Interfaces/IAIProvider.cs
public interface IAIProvider
{
    IAsyncEnumerable<string> StreamFormatWordsAsync(
        string rawText, string targetLang, string nativeLang, CancellationToken ct = default);

    Task<TestGenerationResult> GenerateTestQuestionsAsync(
        IEnumerable<WordDto> words, IEnumerable<string> questionTypes,
        int count, CancellationToken ct = default);

    Task<string> SuggestBlockTitleAsync(
        IEnumerable<string> terms, string language, CancellationToken ct = default);

    Task<bool> IsAvailableAsync(CancellationToken ct = default);
}

// Common/Interfaces/ICacheService.cs
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
    Task InvalidateByPrefixAsync(string prefix, CancellationToken ct = default);
}

// Common/Interfaces/ICurrentUserService.cs
public interface ICurrentUserService
{
    Guid UserId { get; }
    string Email { get; }
    UserRole Role { get; }
    bool IsAuthenticated { get; }
}
```

#### Result паттерн

```csharp
// Common/Models/Result.cs
// Явный результат операции вместо исключений для ожидаемых случаев
public class Result<T>
{
    public T? Value { get; private init; }
    public bool IsSuccess { get; private init; }
    public string? ErrorMessage { get; private init; }
    public ResultErrorType ErrorType { get; private init; }

    public static Result<T> Ok(T value) =>
        new() { Value = value, IsSuccess = true };

    public static Result<T> NotFound(string message = "Not found") =>
        new() { IsSuccess = false, ErrorMessage = message, ErrorType = ResultErrorType.NotFound };

    public static Result<T> Forbidden(string message = "Forbidden") =>
        new() { IsSuccess = false, ErrorMessage = message, ErrorType = ResultErrorType.Forbidden };

    public static Result<T> Failure(string message) =>
        new() { IsSuccess = false, ErrorMessage = message, ErrorType = ResultErrorType.Failure };
}

public enum ResultErrorType { NotFound, Forbidden, Validation, Failure }
```

---

### 2.4 Infrastructure Layer

Реализует все интерфейсы из Domain и Application. Знает о EF Core, Redis, HTTP-клиентах.

#### EF Core — AppDbContext

```csharp
// Infrastructure/Data/AppDbContext.cs
public class AppDbContext : DbContext
{
    private readonly ICurrentUserService _currentUser;

    public DbSet<User> Users => Set<User>();
    public DbSet<WordBlock> WordBlocks => Set<WordBlock>();
    public DbSet<Word> Words => Set<Word>();
    public DbSet<Test> Tests => Set<Test>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<TestAttempt> TestAttempts => Set<TestAttempt>();
    public DbSet<AiCallLog> AiCallLogs => Set<AiCallLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Global Query Filter — автоматическая фильтрация по владельцу
        modelBuilder.Entity<WordBlock>()
            .HasQueryFilter(b => b.UserId == _currentUser.UserId || _currentUser.Role == UserRole.Admin);

        modelBuilder.Entity<Word>()
            .HasQueryFilter(w => w.Block.UserId == _currentUser.UserId || _currentUser.Role == UserRole.Admin);
    }

    // Автоматическое обновление UpdatedAt перед сохранением
    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        // Публикация Domain Events
        var domainEvents = ChangeTracker.Entries<Word>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        var result = await base.SaveChangesAsync(ct);

        foreach (var domainEvent in domainEvents)
            await _mediator.Publish(domainEvent, ct);

        return result;
    }
}
```

#### EF Core Configuration

```csharp
// Infrastructure/Data/Configurations/WordConfiguration.cs
public class WordConfiguration : IEntityTypeConfiguration<Word>
{
    public void Configure(EntityTypeBuilder<Word> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Term).HasMaxLength(500).IsRequired();
        builder.Property(w => w.Translation).HasMaxLength(500).IsRequired();
        builder.Property(w => w.WordType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(w => w.BlockId);
        builder.HasIndex(w => w.NextReviewAt)
            .HasFilter("next_review_at <= NOW()");

        // Full-text search index
        builder.HasIndex(w => new { w.Term, w.Translation })
            .HasDatabaseName("idx_words_fts")
            .HasMethod("GIN");

        builder.Ignore(w => w.DomainEvents);
    }
}
```

#### Repository Implementation

```csharp
// Infrastructure/Data/Repositories/WordRepository.cs
public class WordRepository : IWordRepository
{
    private readonly AppDbContext _db;

    public WordRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Word>> GetDueForReviewAsync(
        Guid userId, int limit, CancellationToken ct = default)
    {
        return await _db.Words
            .Include(w => w.Block)
            .Where(w => w.Block.UserId == userId && w.NextReviewAt <= DateTime.UtcNow)
            .OrderBy(w => w.NextReviewAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Word>> GetDistractorPoolAsync(
        Guid userId, string languageCode, int count, CancellationToken ct = default)
    {
        // Случайная выборка для distractors из всех блоков юзера
        return await _db.Words
            .Include(w => w.Block)
            .ThenInclude(b => b.Language)
            .Where(w => w.Block.UserId == userId && w.Block.Language.Code == languageCode)
            .OrderBy(_ => EF.Functions.Random())
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<Word> words, CancellationToken ct = default)
    {
        await _db.Words.AddRangeAsync(words, ct);
    }
    // ...
}
```

#### AI Provider — Ollama

```csharp
// Infrastructure/AI/OllamaProvider.cs
public class OllamaProvider : IAIProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<OllamaProvider> _logger;
    private readonly OllamaOptions _options;

    public async IAsyncEnumerable<string> StreamFormatWordsAsync(
        string rawText, string targetLang, string nativeLang,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var prompt = BuildFormatPrompt(rawText, targetLang, nativeLang);
        var request = new OllamaChatRequest
        {
            Model = _options.Model,  // "qwen3:8b"
            Messages = new[]
            {
                new OllamaMessage { Role = "system", Content = "/no_think\nReturn ONLY valid JSON." },
                new OllamaMessage { Role = "user", Content = prompt }
            },
            Options = new OllamaModelOptions
            {
                Temperature = 0.1f,
                NumCtx = 4096
            },
            Stream = true
        };

        var response = await _http.PostAsJsonAsync("/api/chat", request, ct);
        response.EnsureSuccessStatusCode();

        await foreach (var line in response.Content.ReadAsStream(ct).ReadLinesAsync(ct))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var chunk = JsonSerializer.Deserialize<OllamaStreamChunk>(line);
            if (chunk?.Message?.Content is not null)
                yield return chunk.Message.Content;
            if (chunk?.Done == true) break;
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync("/api/tags", ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private string BuildFormatPrompt(string rawText, string targetLang, string nativeLang) =>
        $"""
        Input language: {targetLang}
        Translation language: {nativeLang}

        Parse each line into: term, translation, wordType (word/phrase/idiom/expression).
        Mark confidenceFlag=true if user added '?' to the translation.

        User input:
        {rawText}
        """;
}
```

#### AI Orchestrator — Fallback паттерн

```csharp
// Infrastructure/AI/AIOrchestrator.cs
// Паттерн Chain of Responsibility / Fallback
public class AIOrchestrator : IAIProvider
{
    private readonly OllamaProvider _ollama;
    private readonly OpenAIProvider _openai;
    private readonly IAiCallLogRepository _logs;
    private readonly ILogger<AIOrchestrator> _logger;

    public async IAsyncEnumerable<string> StreamFormatWordsAsync(
        string rawText, string targetLang, string nativeLang,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var provider = await _ollama.IsAvailableAsync(ct) ? "ollama" : "openai";
        var primary = provider == "ollama" ? (IAIProvider)_ollama : _openai;

        var sw = Stopwatch.StartNew();
        var success = true;
        string? error = null;

        await foreach (var chunk in primary.StreamFormatWordsAsync(rawText, targetLang, nativeLang, ct))
            yield return chunk;

        // Логирование вызова
        await _logs.AddAsync(new AiCallLog
        {
            CallType = "format_words",
            Provider = provider,
            Model = provider == "ollama" ? "qwen3:8b" : "gpt-4o-mini",
            DurationMs = (int)sw.ElapsedMilliseconds,
            Success = success,
            ErrorMessage = error
        }, ct);
    }
}
```

#### Cache Service — Redis

```csharp
// Infrastructure/Cache/RedisCacheService.cs
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync(key);
        return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, ttl ?? TimeSpan.FromMinutes(5));
    }

    public async Task InvalidateByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = server.Keys(pattern: $"{prefix}*").ToArray();
        if (keys.Any()) await _db.KeyDeleteAsync(keys);
    }
}
```

---

### 2.5 Presentation Layer (API)

Тонкий слой. Контроллеры только принимают HTTP-запрос, формируют Command/Query и отправляют в MediatR. Никакой бизнес-логики.

```csharp
// Presentation/Controllers/WordsController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WordsController : ControllerBase
{
    private readonly IMediator _mediator;

    // POST /api/words/format — SSE стриминг
    [HttpPost("format")]
    public async Task FormatWords(
        [FromBody] FormatWordsRequest request, CancellationToken ct)
    {
        Response.Headers.Add("Content-Type", "text/event-stream");
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("X-Accel-Buffering", "no");

        var command = new FormatWordsCommand(
            request.RawText, request.LanguageHint ?? "en", request.NativeLanguage);

        await foreach (var chunk in _mediator.CreateStream(command, ct))
        {
            await Response.WriteAsync($"event: {chunk.Stage}\n", ct);
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(chunk)}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
    }

    // POST /api/blocks/{blockId}/import
    [HttpPost("blocks/{blockId:guid}/import")]
    public async Task<IActionResult> ImportWords(
        Guid blockId, [FromBody] ImportWordsRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new ImportWordsCommand(blockId, request.Words), ct);

        return result.ErrorType switch
        {
            ResultErrorType.NotFound  => NotFound(result.ErrorMessage),
            ResultErrorType.Forbidden => Forbid(),
            ResultErrorType.Validation => BadRequest(result.ErrorMessage),
            _ when result.IsSuccess   => Ok(result.Value),
            _                         => StatusCode(500, result.ErrorMessage)
        };
    }
}
```

#### Middleware

```csharp
// Presentation/Middleware/ExceptionMiddleware.cs
// Глобальный обработчик исключений — не пропускает 500 с деталями наружу
public class ExceptionMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try { await next(context); }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = 422;
            await context.Response.WriteAsJsonAsync(new
            {
                errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            });
        }
        catch (DomainException ex)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { message = "Internal server error" });
        }
    }
}
```

```csharp
// Presentation/Middleware/CurrentUserMiddleware.cs
// Заполняет ICurrentUserService из JWT claims
public class CurrentUserMiddleware : IMiddleware
{
    private readonly ICurrentUserService _currentUser;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            _currentUser.UserId = Guid.Parse(context.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            _currentUser.Email = context.User.FindFirst(ClaimTypes.Email)!.Value;
            _currentUser.Role = Enum.Parse<UserRole>(context.User.FindFirst("role")!.Value);
        }
        await next(context);
    }
}
```

---

### 2.6 Паттерны и их применение

| Паттерн | Где используется | Зачем |
|---|---|---|
| **CQRS** | Application Layer | Разделение чтения и записи; разная оптимизация |
| **MediatR Pipeline** | Application Layer | Сквозная логика (validation, logging, caching) без дублирования |
| **Repository** | Domain + Infrastructure | Абстракция доступа к данным; тестируемость |
| **Unit of Work** | Infrastructure | Транзакции через несколько репозиториев |
| **Domain Events** | Domain | Слабая связность между агрегатами |
| **Result<T>** | Application | Явная обработка ошибок без исключений для бизнес-случаев |
| **Specification** | Domain/Infrastructure | Переиспользуемые условия фильтрации (в будущем) |
| **Factory Method** | Domain Entities | Единственный способ создать сущность с инвариантами |
| **Chain of Responsibility** | AI Orchestrator | Fallback Ollama → OpenAI |
| **Decorator** | Cache + Logging behaviors | Добавление поведения без изменения оригинала |
| **Global Query Filter** | EF Core | Автоматическая фильтрация по tenant/owner |

---

### 2.7 Полная структура проекта

```
Lexify.sln
├── src/
│   ├── Lexify.Domain/
│   │   ├── Entities/
│   │   │   ├── BaseEntity.cs
│   │   │   ├── User.cs
│   │   │   ├── Word.cs
│   │   │   ├── WordBlock.cs
│   │   │   ├── Test.cs
│   │   │   ├── Question.cs
│   │   │   ├── QuestionOption.cs
│   │   │   ├── TestAttempt.cs
│   │   │   └── AttemptAnswer.cs
│   │   ├── ValueObjects/
│   │   │   ├── Language.cs
│   │   │   └── TestScore.cs
│   │   ├── Enums/
│   │   │   ├── WordType.cs
│   │   │   ├── QuestionType.cs
│   │   │   └── UserRole.cs
│   │   ├── Events/
│   │   │   ├── IDomainEvent.cs
│   │   │   ├── WordCreatedEvent.cs
│   │   │   ├── WordReviewedEvent.cs
│   │   │   └── TestCompletedEvent.cs
│   │   ├── Repositories/
│   │   │   ├── IWordRepository.cs
│   │   │   ├── IWordBlockRepository.cs
│   │   │   ├── ITestRepository.cs
│   │   │   └── IUnitOfWork.cs
│   │   ├── Services/
│   │   │   └── SpacedRepetitionService.cs
│   │   └── Exceptions/
│   │       └── DomainException.cs
│   │
│   ├── Lexify.Application/
│   │   ├── Features/
│   │   │   ├── Words/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── ImportWords/
│   │   │   │   │   ├── FormatWords/
│   │   │   │   │   ├── CreateWord/
│   │   │   │   │   ├── UpdateWord/
│   │   │   │   │   ├── DeleteWord/
│   │   │   │   │   └── ReviewWord/
│   │   │   │   └── Queries/
│   │   │   │       ├── GetWordsByBlock/
│   │   │   │       ├── GetDueForReview/
│   │   │   │       └── SearchWords/
│   │   │   ├── Blocks/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateBlock/
│   │   │   │   │   ├── UpdateBlock/
│   │   │   │   │   └── DeleteBlock/
│   │   │   │   └── Queries/
│   │   │   │       ├── GetBlocks/
│   │   │   │       └── GetBlockById/
│   │   │   ├── Tests/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── GenerateTest/
│   │   │   │   │   ├── StartAttempt/
│   │   │   │   │   ├── SubmitAnswer/
│   │   │   │   │   └── FinishAttempt/
│   │   │   │   └── Queries/
│   │   │   │       ├── GetTests/
│   │   │   │       ├── GetTestById/
│   │   │   │       └── GetAttemptResults/
│   │   │   ├── Auth/
│   │   │   │   └── Commands/
│   │   │   │       ├── Register/
│   │   │   │       ├── Login/
│   │   │   │       └── RefreshToken/
│   │   │   └── Admin/
│   │   │       ├── Queries/
│   │   │       │   ├── GetDashboardStats/
│   │   │       │   ├── GetUsers/
│   │   │       │   └── GetAiLogs/
│   │   │       └── Commands/
│   │   │           ├── SuspendUser/
│   │   │           ├── UpdateSystemSetting/
│   │   │           └── ExportUserData/
│   │   ├── Common/
│   │   │   ├── Behaviors/
│   │   │   │   ├── ValidationBehavior.cs
│   │   │   │   ├── LoggingBehavior.cs
│   │   │   │   ├── CachingBehavior.cs
│   │   │   │   └── TransactionBehavior.cs
│   │   │   ├── Interfaces/
│   │   │   │   ├── IAIProvider.cs
│   │   │   │   ├── ICacheService.cs
│   │   │   │   ├── IEmailService.cs
│   │   │   │   └── ICurrentUserService.cs
│   │   │   ├── Mappings/
│   │   │   │   ├── WordMappingProfile.cs
│   │   │   │   ├── BlockMappingProfile.cs
│   │   │   │   └── TestMappingProfile.cs
│   │   │   └── Models/
│   │   │       ├── Result.cs
│   │   │       ├── PagedResult.cs
│   │   │       └── ICacheable.cs
│   │   └── DependencyInjection.cs
│   │
│   ├── Lexify.Infrastructure/
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/
│   │   │   │   ├── WordConfiguration.cs
│   │   │   │   ├── WordBlockConfiguration.cs
│   │   │   │   └── TestConfiguration.cs
│   │   │   ├── Repositories/
│   │   │   │   ├── WordRepository.cs
│   │   │   │   ├── WordBlockRepository.cs
│   │   │   │   ├── TestRepository.cs
│   │   │   │   └── UnitOfWork.cs
│   │   │   └── Migrations/
│   │   ├── AI/
│   │   │   ├── OllamaProvider.cs
│   │   │   ├── OpenAIProvider.cs
│   │   │   ├── AIOrchestrator.cs
│   │   │   ├── AIResponseValidator.cs
│   │   │   └── Models/
│   │   │       ├── OllamaChatRequest.cs
│   │   │       └── OllamaStreamChunk.cs
│   │   ├── Cache/
│   │   │   └── RedisCacheService.cs
│   │   ├── Identity/
│   │   │   ├── CurrentUserService.cs
│   │   │   └── JwtService.cs
│   │   ├── Jobs/
│   │   │   ├── GenerateTestJob.cs
│   │   │   └── SendReviewReminderJob.cs
│   │   └── DependencyInjection.cs
│   │
│   └── Lexify.API/
│       ├── Controllers/
│       │   ├── AuthController.cs
│       │   ├── WordsController.cs
│       │   ├── BlocksController.cs
│       │   ├── TestsController.cs
│       │   ├── ReviewController.cs
│       │   └── AdminController.cs
│       ├── Middleware/
│       │   ├── ExceptionMiddleware.cs
│       │   ├── CurrentUserMiddleware.cs
│       │   └── RateLimitingMiddleware.cs
│       ├── Filters/
│       │   └── AdminOnlyFilter.cs
│       ├── Program.cs
│       └── appsettings.json
│
└── tests/
    ├── Lexify.Domain.Tests/
    │   ├── WordTests.cs
    │   └── SpacedRepetitionServiceTests.cs
    ├── Lexify.Application.Tests/
    │   ├── ImportWordsCommandTests.cs
    │   └── GenerateTestCommandTests.cs
    └── Lexify.API.Tests/
        └── WordsControllerIntegrationTests.cs
```

---

## 3. Frontend — Feature-Sliced Design

### 3.1 Почему FSD

**Feature-Sliced Design** — методология архитектуры UI-приложений. Основные идеи:

1. **Слои** (layers) — фиксированная иерархия: `app → pages → widgets → features → entities → shared`
2. **Слайсы** (slices) — разбиение каждого слоя по бизнес-доменам: `words`, `tests`, `auth`
3. **Сегменты** (segments) — тип кода внутри слайса: `ui`, `model`, `api`, `lib`, `config`

**Правило зависимостей FSD:** нижние слои не могут импортировать из верхних. `entities/word` не знает о `features/import-words`. `shared` не знает ни о чём выше себя.

```
app        ← orchestration, routing, providers
pages      ← page components (одна страница = один слайс)
widgets    ← самодостаточные блоки UI (Header, Sidebar, TestCard)
features   ← действия пользователя (import-words, run-test, review-word)
entities   ← бизнес-сущности (word, block, test, user)
shared     ← переиспользуемое (ui-kit, api-client, utils, типы)
```

---

### 3.2 Слои FSD

#### `shared` — переиспользуемая база

```
shared/
├── api/
│   ├── base.ts           ← axios/fetch instance с interceptors
│   ├── types.ts          ← PagedResult<T>, ApiError и т.д.
│   └── index.ts
├── ui/
│   ├── Button/
│   ├── Input/
│   ├── Table/
│   ├── Badge/
│   ├── Spinner/
│   ├── SSEListener/      ← React-компонент для SSE-стримов
│   └── index.ts
├── lib/
│   ├── levenshtein.ts    ← нечёткое сравнение для open-answer
│   ├── hash.ts           ← SHA-256 для content_hash
│   └── format.ts         ← форматирование дат, процентов
├── config/
│   └── routes.ts         ← константы маршрутов
└── types/
    └── index.ts          ← общие TypeScript-типы
```

```typescript
// shared/api/base.ts
import axios from 'axios';
import { useAuthStore } from '@/entities/user/model/store';

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  withCredentials: true,  // для refresh token cookie
});

// Автоматическое обновление access token
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      const refreshed = await useAuthStore.getState().refreshToken();
      if (refreshed) return apiClient.request(error.config);
      useAuthStore.getState().logout();
    }
    return Promise.reject(error);
  }
);
```

#### `entities` — бизнес-сущности

```
entities/
├── word/
│   ├── api/
│   │   └── wordApi.ts        ← GET /api/blocks/:id/words
│   ├── model/
│   │   ├── types.ts          ← Word, WordType, WordDto
│   │   ├── store.ts          ← Zustand: слова текущего блока
│   │   └── selectors.ts      ← производные состояния
│   ├── ui/
│   │   ├── WordBadge.tsx     ← тег типа слова (word/idiom/...)
│   │   └── WordRow.tsx       ← строка в таблице слов
│   └── index.ts              ← публичный API слайса
├── block/
│   ├── api/
│   │   └── blockApi.ts
│   ├── model/
│   │   ├── types.ts          ← WordBlock, BlockFilter
│   │   └── store.ts
│   ├── ui/
│   │   ├── BlockCard.tsx
│   │   └── BlockLanguageBadge.tsx
│   └── index.ts
├── test/
│   ├── api/
│   │   └── testApi.ts
│   ├── model/
│   │   ├── types.ts          ← Test, Question, QuestionType, TestAttempt
│   │   └── store.ts
│   ├── ui/
│   │   ├── QuestionCard.tsx
│   │   └── TestScoreBadge.tsx
│   └── index.ts
└── user/
    ├── model/
    │   ├── types.ts          ← User, UserRole
    │   └── store.ts          ← auth state, accessToken
    └── index.ts
```

```typescript
// entities/word/model/types.ts
export type WordType = 'word' | 'phrase' | 'idiom' | 'expression';

export interface Word {
  id: string;
  blockId: string;
  term: string;
  translation: string;
  wordType: WordType;
  notes: string | null;
  confidenceFlag: boolean;
  sortOrder: number;
  // SM-2 fields
  easeFactor: number;
  intervalDays: number;
  nextReviewAt: string;
}

export interface WordBlock {
  id: string;
  userId: string;
  languageId: number;
  title: string;
  description: string | null;
  wordCount: number;
  createdAt: string;
  tags: string[];
}
```

```typescript
// entities/word/model/store.ts
import { create } from 'zustand';
import { immer } from 'zustand/middleware/immer';
import type { Word } from './types';

interface WordStore {
  words: Word[];
  pendingWords: Word[];         // отформатированные, ещё не сохранённые
  setPendingWords: (words: Word[]) => void;
  updatePendingWord: (index: number, patch: Partial<Word>) => void;
  clearPending: () => void;
}

export const useWordStore = create<WordStore>()(
  immer((set) => ({
    words: [],
    pendingWords: [],
    setPendingWords: (words) => set((s) => { s.pendingWords = words; }),
    updatePendingWord: (index, patch) =>
      set((s) => { Object.assign(s.pendingWords[index], patch); }),
    clearPending: () => set((s) => { s.pendingWords = []; }),
  }))
);
```

#### `features` — действия пользователя

Каждая фича — одно действие. Самодостаточна: знает о нужных entities, но не о других features.

```
features/
├── import-words/             ← ввод сырого текста + вызов AI
│   ├── api/
│   │   └── formatWords.ts   ← SSE-стриминг к /api/words/format
│   ├── model/
│   │   ├── store.ts         ← состояние шагов импорта
│   │   └── validation.ts    ← правила валидации перед отправкой
│   ├── ui/
│   │   ├── RawTextInput.tsx
│   │   ├── FormatProgress.tsx  ← отображение SSE-прогресса
│   │   └── WordPreviewTable.tsx ← редактируемая таблица результата
│   └── index.ts
├── run-test/                 ← прохождение теста
│   ├── model/
│   │   ├── store.ts         ← текущий вопрос, ответы, таймер
│   │   └── answerChecker.ts ← логика проверки open-answer (Levenshtein)
│   ├── ui/
│   │   ├── SingleChoiceQuestion.tsx
│   │   ├── MultiSelectQuestion.tsx
│   │   ├── FillInBlankQuestion.tsx
│   │   └── OpenAnswerQuestion.tsx
│   └── index.ts
├── generate-test/            ← создание нового теста
│   ├── api/
│   │   └── generateTest.ts
│   ├── model/
│   │   └── store.ts         ← выбранные блоки, типы вопросов
│   ├── ui/
│   │   ├── BlockSelector.tsx
│   │   └── QuestionTypeSelector.tsx
│   └── index.ts
├── review-word/              ← сессия spaced repetition
│   ├── api/
│   │   └── reviewApi.ts
│   ├── ui/
│   │   ├── ReviewCard.tsx
│   │   └── QualityRater.tsx  ← оценка 0-5
│   └── index.ts
├── auth/
│   ├── api/
│   │   └── authApi.ts
│   ├── ui/
│   │   ├── LoginForm.tsx
│   │   └── RegisterForm.tsx
│   └── index.ts
└── admin-ai-monitor/
    ├── api/
    │   └── aiLogsApi.ts
    ├── ui/
    │   ├── AiMetricsChart.tsx
    │   └── AiLogTable.tsx
    └── index.ts
```

```typescript
// features/import-words/api/formatWords.ts
// SSE-стриминг с сервера
export async function* streamFormatWords(
  rawText: string,
  targetLanguage: string,
  nativeLanguage: string,
  signal?: AbortSignal
): AsyncGenerator<FormatChunk> {
  const response = await fetch('/api/words/format', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ rawText, languageHint: targetLanguage, nativeLanguage }),
    signal,
  });

  const reader = response.body!.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    buffer += decoder.decode(value, { stream: true });

    const lines = buffer.split('\n');
    buffer = lines.pop() ?? '';

    for (const line of lines) {
      if (line.startsWith('data: ')) {
        const data = JSON.parse(line.slice(6));
        yield data as FormatChunk;
      }
    }
  }
}
```

```typescript
// features/import-words/model/store.ts
type ImportStep = 'input' | 'formatting' | 'preview' | 'saving' | 'done';

interface ImportWordsStore {
  step: ImportStep;
  rawText: string;
  targetLanguage: string;
  nativeLanguage: string;
  formatProgress: string;
  formattedWords: PendingWord[];
  suggestedTitle: string;
  error: string | null;

  setRawText: (text: string) => void;
  startFormatting: () => void;
  setFormatProgress: (msg: string) => void;
  setFormattedResult: (result: FormatResult) => void;
  updateWord: (index: number, patch: Partial<PendingWord>) => void;
  setError: (msg: string) => void;
  reset: () => void;
}

export const useImportWordsStore = create<ImportWordsStore>()(
  persist(                    // persist в sessionStorage — защита от перезагрузки
    immer((set) => ({
      step: 'input',
      rawText: '',
      // ...
      startFormatting: () => set((s) => { s.step = 'formatting'; s.error = null; }),
      setFormattedResult: (result) => set((s) => {
        s.formattedWords = result.words;
        s.suggestedTitle = result.suggestedTitle;
        s.step = 'preview';
      }),
    })),
    { name: 'import-words-draft', storage: createJSONStorage(() => sessionStorage) }
  )
);
```

#### `widgets` — самодостаточные блоки UI

```
widgets/
├── AppHeader/
│   ├── ui/AppHeader.tsx
│   └── index.ts
├── Sidebar/
│   ├── ui/Sidebar.tsx
│   └── index.ts
├── BlockList/              ← список блоков с фильтрами (использует entities/block + features)
│   ├── ui/
│   │   ├── BlockList.tsx
│   │   └── BlockFilters.tsx
│   └── index.ts
├── TestCard/               ← карточка теста с прогресс-баром
├── ReviewDueBanner/        ← баннер «сегодня N слов к повторению»
└── AdminNav/               ← навигация админ-панели
```

#### `pages` — страницы приложения

```
pages/
├── Dashboard/
│   └── ui/DashboardPage.tsx
├── BlockList/
│   └── ui/BlockListPage.tsx
├── BlockDetail/
│   └── ui/BlockDetailPage.tsx
├── WordImport/             ← оркестрирует features/import-words
│   └── ui/WordImportPage.tsx
├── TestCreate/
│   └── ui/TestCreatePage.tsx
├── TestRunner/             ← оркестрирует features/run-test
│   └── ui/TestRunnerPage.tsx
├── TestResults/
│   └── ui/TestResultsPage.tsx
├── ReviewSession/
│   └── ui/ReviewSessionPage.tsx
├── Login/
│   └── ui/LoginPage.tsx
└── Admin/
    ├── Dashboard/
    ├── Users/
    ├── AiMonitor/
    └── Settings/
```

#### `app` — входная точка

```
app/
├── providers/
│   ├── QueryProvider.tsx    ← TanStack Query
│   ├── RouterProvider.tsx   ← React Router
│   └── ThemeProvider.tsx
├── router/
│   ├── routes.tsx           ← определение маршрутов
│   └── guards/
│       ├── AuthGuard.tsx    ← редирект на /login если нет токена
│       └── AdminGuard.tsx   ← редирект если нет роли admin
└── index.tsx
```

---

### 3.3 Полная структура проекта

```
lexify-frontend/
├── src/
│   ├── app/
│   ├── pages/
│   ├── widgets/
│   ├── features/
│   ├── entities/
│   └── shared/
├── public/
├── index.html
├── vite.config.ts
├── tsconfig.json
├── .eslintrc.json           ← eslint-plugin-boundaries для FSD
└── package.json
```

**eslint-plugin-boundaries** автоматически проверяет правила зависимостей FSD при сохранении — попытка импортировать из верхнего слоя в нижний даст ошибку линтера.

---

### 3.4 Паттерны состояния

| Что | Где хранится | Почему |
|---|---|---|
| Отформатированные слова (черновик) | Zustand + sessionStorage | Выживает перезагрузку; очищается при сохранении |
| Список блоков (серверные данные) | TanStack Query cache | Автоматический re-fetch, stale-while-revalidate |
| Текущий вопрос в тесте | Zustand (в памяти) | Чистый reset после завершения теста |
| Access token | Zustand (в памяти) | Не в localStorage — безопаснее |
| Refresh token | HttpOnly cookie | Недоступен из JS |
| Настройки темы/языка | localStorage | Долгосрочные пользовательские пref-ы |

#### TanStack Query — серверное состояние

```typescript
// entities/block/api/blockApi.ts
export const blockKeys = {
  all: ['blocks'] as const,
  list: (filters: BlockFilter) => [...blockKeys.all, 'list', filters] as const,
  detail: (id: string) => [...blockKeys.all, 'detail', id] as const,
};

export function useBlocks(filters: BlockFilter) {
  return useQuery({
    queryKey: blockKeys.list(filters),
    queryFn: () => apiClient.get('/api/blocks', { params: filters }).then(r => r.data),
    staleTime: 1000 * 60 * 5,  // 5 минут
  });
}

export function useImportWordsMutation(blockId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (words: PendingWord[]) =>
      apiClient.post(`/api/blocks/${blockId}/import`, { words }),
    onSuccess: () => {
      // Инвалидация кэша блока после импорта
      queryClient.invalidateQueries({ queryKey: blockKeys.detail(blockId) });
    },
  });
}
```

---

### 3.5 Типизация и контракты

Типы генерируются из OpenAPI-схемы бэкенда через `openapi-typescript`:

```json
// package.json scripts
{
  "generate:types": "openapi-typescript http://localhost:5000/swagger/v1/swagger.json -o src/shared/types/api.generated.ts"
}
```

Это гарантирует синхронность типов фронтенда с фактическим API без ручного дублирования.

---

## 4. Взаимодействие Frontend ↔ Backend

### Стандартный REST-запрос

```
React Component
  → useQuery / useMutation (TanStack Query)
    → apiClient (axios + interceptors)
      → ASP.NET Controller
        → MediatR.Send(Command/Query)
          → ValidationBehavior → LoggingBehavior → CachingBehavior
            → CommandHandler / QueryHandler
              → Repository → PostgreSQL
              ← Result<T>
          ← IActionResult
        ← JSON response
      ← axios response
    ← TanStack Query cache update
  ← React re-render
```

### SSE-стриминг (форматирование слов)

```
WordImportPage (нажал "Форматировать")
  → features/import-words: streamFormatWords() [AsyncGenerator]
    → fetch POST /api/words/format
      → WordsController.FormatWords()
        → IMediator.CreateStream(FormatWordsCommand)
          → FormatWordsCommandHandler
            → IAIProvider.StreamFormatWordsAsync()
              → Ollama HTTP stream (qwen3:8b /no_think)
                ← chunks...
              → AIResponseValidator.Validate()
            ← IAsyncEnumerable<FormatWordsChunk>
          ← SSE events: "event: progress\ndata: {...}\n\n"
        ← Response stream flush
      ← ReadableStream
    ← yield FormatChunk (stage, data)
  → useImportWordsStore.setFormatProgress() / setFormattedResult()
  → WordPreviewTable re-render
```

### Фоновая генерация теста

```
TestCreatePage (нажал "Создать тест")
  → POST /api/tests/generate
    → TestsController → GenerateTestCommand → Handler
      → Hangfire.Enqueue(GenerateTestJob)
      ← { testId, status: "generating" }
    ← 202 Accepted

TestCreatePage (polling раз в 2 сек)
  → GET /api/tests/{id}
    ← { status: "generating" }  (несколько раз)
    ← { status: "ready", ... }  (готово)
  → navigate(`/tests/${testId}`)
```

---

## 5. Сквозные паттерны (Cross-cutting)

### Логирование

```
Backend:  Serilog → structured logs → файл / Console / seq
          Каждый лог содержит: UserId, RequestId, TraceId
          LoggingBehavior логирует каждый MediatR-запрос с временем

Frontend: console.error только в dev
          В prod: отправка ошибок в /api/errors endpoint (будущее)
```

### Обработка ошибок

```
Domain Exception → 400 Bad Request (бизнес-правило нарушено)
Validation Exception → 422 Unprocessable Entity (невалидные входные данные)
Not Found → 404 Not Found
Forbidden → 403 Forbidden (но 404 для ресурсов — не раскрывать существование)
Unexpected → 500 Internal Server Error (без деталей наружу)

Frontend:
  - 401 → автоматический refresh token → retry
  - 422 → показать ошибки валидации у полей формы
  - 5xx → toast с «Что-то пошло не так, попробуйте позже»
```

### Rate Limiting

```
/api/words/format       → 10 запросов / минута / пользователь (Redis sliding window)
/api/tests/generate     → 5 запросов / час / пользователь
/api/auth/login         → 10 попыток / 15 минут / IP (защита от brute force)
/api/admin/*            → без лимита для admin-роли
```

### Кэширование

```
Redis TTL:
  GET /api/blocks           → 5 минут (инвалидируется при создании/изменении блока)
  GET /api/blocks/:id       → 2 минуты
  GET /api/admin/stats      → 5 минут
  System settings           → 60 секунд

TanStack Query:
  blocks list               → staleTime: 5 min
  block detail              → staleTime: 2 min
  due review words          → staleTime: 1 min (часто меняется)
```

---

## 6. Диаграммы потоков

### Поток: импорт слов

```
[Пользователь вводит текст]
        │
        ▼
[RawTextInput + язык-селекторы]
        │ click "Форматировать"
        ▼
[FormatProgress — SSE listener]
   event: parsing  → "Анализирую..."
   event: streaming → чанки JSON
   event: done    → полный JSON
        │
        ▼
[AIResponseValidator]
   ✓ valid  → WordPreviewTable
   ✗ error  → ErrorBanner + "Попробовать снова"
        │
        ▼
[WordPreviewTable — редактируемая таблица]
   ⚠️  confidenceFlag строки подсвечены
   Название блока редактируемо
        │ click "Сохранить"
        ▼
[POST /api/blocks/:id/import]
        │
        ▼
[Redirect → /blocks/:id]
```

### Поток: прохождение теста

```
[TestRunner загружает тест]
        │
        ▼
[QuestionCard — тип определяет компонент]
   SingleChoice → 4 кнопки
   MultiSelect  → чекбоксы
   FillInBlank  → input + проверка
        │ ответ дан
        ▼
[POST /api/attempts/:id/answer]
        │
        ▼
[Показать правильный ответ + notes слова]
        │ следующий вопрос
        ▼
[Все вопросы пройдены]
        │
        ▼
[POST /api/attempts/:id/finish]
        │
        ▼
[TestResultsPage — score + разбор ошибок]
   → обновление next_review_at для слов с ошибками
```
