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
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddHttpContextAccessor();

        // Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasherService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IWordBlockRepository, WordBlockRepository>();
        services.AddScoped<IWordRepository, WordRepository>();
        services.AddScoped<IAiCallLogRepository, AiCallLogRepository>();
        services.AddScoped<ITestRepository, TestRepository>();
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<ITestAttemptRepository, TestAttemptRepository>();
        services.AddScoped<IAttemptAnswerRepository, AttemptAnswerRepository>();
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
        services.AddScoped<IBackgroundJobService, HangfireBackgroundJobService>();

        // AI settings
        services.Configure<OllamaSettings>(configuration.GetSection("Ollama"));
        services.Configure<OpenAISettings>(configuration.GetSection("OpenAI"));

        // AI HTTP clients with Polly: 2 retries on transient errors, 120s timeout
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(2, attempt => TimeSpan.FromSeconds(attempt * 2));

        var ollamaTimeout = TimeSpan.FromSeconds(
            configuration.GetSection("Ollama").GetValue<int>("TimeoutSeconds", 120));

        var openAiTimeout = TimeSpan.FromSeconds(
            configuration.GetSection("OpenAI").GetValue<int>("TimeoutSeconds", 120));

        services.AddHttpClient("ollama", (sp, client) =>
            {
                var settings = configuration.GetSection("Ollama").Get<OllamaSettings>() ?? new OllamaSettings();
                client.BaseAddress = new Uri(settings.BaseUrl);
                client.Timeout = ollamaTimeout;
            })
            .AddPolicyHandler(retryPolicy);

        services.AddHttpClient("openai", (sp, client) =>
            {
                var settings = configuration.GetSection("OpenAI").Get<OpenAISettings>() ?? new OpenAISettings();
                client.BaseAddress = new Uri(settings.BaseUrl);
                client.Timeout = openAiTimeout;
                if (!string.IsNullOrEmpty(settings.ApiKey))
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {settings.ApiKey}");
            })
            .AddPolicyHandler(retryPolicy);

        // AI providers
        services.AddScoped<OllamaProvider>();
        services.AddScoped<OpenAIProvider>();
        services.AddScoped<IAIProvider, AIOrchestrator>();

        // Redis connection (optional — used for rate limiting and caching)
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
                StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString));
        }

        return services;
    }
}
