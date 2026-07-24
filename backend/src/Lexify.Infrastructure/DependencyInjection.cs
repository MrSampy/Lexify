using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Lexify.Application.Abstractions;
using Lexify.Domain.Repositories;
using Lexify.Infrastructure.AI;
using Lexify.Infrastructure.Jobs;
using Lexify.Infrastructure.Persistence;
using Lexify.Infrastructure.Persistence.Repositories;
using Lexify.Infrastructure.Persistence.Seeders;
using Lexify.Infrastructure.Services;
using Lexify.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;

namespace Lexify.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<DataSeeder>();

        // JWT settings
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>()
            ?? throw new InvalidOperationException("JWT settings not configured.");

        // Authentication
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    // A small skew, not zero: with a 15-minute access token, a phone whose clock runs a
                    // little fast has its token rejected early, and every such rejection is a round trip
                    // through refresh (or, before the refresh fixes, a sign-out).
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddHttpContextAccessor();

        // Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasherService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddSingleton<IUnsubscribeTokenService, UnsubscribeTokenService>();

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddScoped<ILoginTwoFactorCodeRepository, LoginTwoFactorCodeRepository>();
        services.AddScoped<IWordBlockRepository, WordBlockRepository>();
        services.AddScoped<IBlockShareRepository, BlockShareRepository>();
        services.AddScoped<IWordRepository, WordRepository>();
        services.AddScoped<IAiCallLogRepository, AiCallLogRepository>();
        services.AddScoped<IReviewLogRepository, ReviewLogRepository>();
        services.AddScoped<ITestRepository, TestRepository>();
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<ITestAttemptRepository, TestAttemptRepository>();
        services.AddScoped<IAttemptAnswerRepository, AttemptAnswerRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Hangfire — background jobs (PostgreSQL storage)
        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(opts =>
                opts.UseNpgsqlConnection(connectionString)));
        services.AddHangfireServer(opts => opts.WorkerCount = 2);
        services.AddScoped<GenerateTestJob>();
        services.AddScoped<CleanupRefreshTokensJob>();
        services.AddScoped<CleanupPasswordResetTokensJob>();
        services.AddScoped<CleanupEmailVerificationTokensJob>();
        services.AddScoped<SendEmailVerificationJob>();
        services.AddScoped<CleanupTwoFactorCodesJob>();
        services.AddScoped<Send2faCodeJob>();
        services.AddScoped<AnonymizeDeletedUsersJob>();
        services.AddScoped<SendReviewRemindersJob>();
        services.AddScoped<SendWelcomeEmailJob>();
        services.AddScoped<SendPasswordResetEmailJob>();
        services.AddScoped<CleanupAiLogsJob>();
        services.AddScoped<IBackgroundJobService, HangfireBackgroundJobService>();

        // Email
        services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));
        services.AddScoped<IEmailService, SmtpEmailService>();

        // AI providers — an ordered list, tried in turn until one succeeds (see AIOrchestrator)
        var aiProviders = configuration.GetSection("AiProviders").Get<List<AiProviderSettings>>()
            ?? [];

        // Drop keyless clones of a keyed endpoint: the extra Ollama keys (OLLAMA_API_KEY_2..4) are
        // optional, so an unset one would otherwise leave an entry with no Authorization header that
        // just 401s and adds a wasted step + latency to the rotation. A provider that is legitimately
        // keyless on its own endpoint (e.g. a local Lemonade) has a unique BaseUrl and is kept.
        aiProviders = aiProviders
            .Where(p => !string.IsNullOrEmpty(p.ApiKey)
                        || !aiProviders.Any(other => !ReferenceEquals(other, p)
                                                     && other.BaseUrl == p.BaseUrl
                                                     && !string.IsNullOrEmpty(other.ApiKey)))
            .ToList();

        // Bind the filtered list (not the raw section) so the orchestrator iterates exactly the
        // providers we register HTTP clients for below.
        services.Configure<List<AiProviderSettings>>(opts =>
        {
            opts.Clear();
            opts.AddRange(aiProviders);
        });

        // Rotates the starting key per operation so load spreads across interchangeable keys.
        services.AddSingleton<AiProviderRotation>();

        // AI HTTP clients with Polly: 2 retries on transient errors
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(2, attempt => TimeSpan.FromSeconds(attempt * 2));

        foreach (var providerSettings in aiProviders)
        {
            services.AddHttpClient($"ai:{providerSettings.Name}", client =>
                {
                    client.BaseAddress = new Uri(providerSettings.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(providerSettings.TimeoutSeconds);
                    if (!string.IsNullOrEmpty(providerSettings.ApiKey))
                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {providerSettings.ApiKey}");
                })
                .AddPolicyHandler(retryPolicy);
        }

        services.AddScoped<IAIProvider, AIOrchestrator>();

        // Piper TTS — one HTTP client to the sidecar; the retry policy above is reused.
        services.Configure<PiperSettings>(configuration.GetSection("Piper"));
        var piperSettings = configuration.GetSection("Piper").Get<PiperSettings>() ?? new PiperSettings();
        services.AddHttpClient("piper", client =>
            {
                client.BaseAddress = new Uri(piperSettings.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(piperSettings.TimeoutSeconds);
            })
            .AddPolicyHandler(retryPolicy);
        services.AddScoped<ITtsService, PiperTtsService>();

        // Feedback attachments live on a mounted volume, not in the database.
        services.Configure<FeedbackStorageSettings>(configuration.GetSection("FeedbackStorage"));
        services.AddScoped<IFeedbackAttachmentStorage, LocalFeedbackAttachmentStorage>();

        // Redis connection and cache service (optional — falls back to no-op when not configured)
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
                StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString));
            services.AddSingleton<ICacheService, RedisCacheService>();
        }
        else
        {
            services.AddSingleton<ICacheService, NullCacheService>();
        }

        services.AddScoped<IAdminStatsRepository, AdminStatsRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
        services.AddScoped<ILanguageRepository, LanguageRepository>();

        return services;
    }
}
