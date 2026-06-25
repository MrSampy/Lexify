using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using Hangfire;
using Hangfire.InMemory;
using Lexify.Application.Abstractions;
using Lexify.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Lexify.API.Tests.Infrastructure;

/// <summary>
/// Shared test server backed by a Testcontainers PostgreSQL instance.
/// JWT validation uses the development configuration (appsettings.Development.json).
/// </summary>
public sealed class LexifyWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Known secret from appsettings.Development.json; used to sign tokens in tests.
    public const string DevJwtSecret   = "dev-secret-key-change-in-production-min-32-chars";
    public const string DevJwtIssuer   = "lexify-dev";
    public const string DevJwtAudience = "lexify-dev";

    private readonly PostgreSqlContainer _pgContainer = new PostgreSqlBuilder()
        .WithDatabase("lexify_test")
        .WithUsername("postgres")
        .WithPassword("test123")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
        .Build();

    public async Task InitializeAsync()
    {
        await _pgContainer.StartAsync();
        // Force host startup (migrations + seeding + IHostedService) to complete
        // before the first test request, avoiding a race between DB initialization
        // and the initial HTTP request.
        using var _ = CreateClient();
    }

    public new async Task DisposeAsync()
    {
        await _pgContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Inject JWT and other settings that would normally come from appsettings.Development.json.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"]                   = DevJwtIssuer,
                ["Jwt:Audience"]                 = DevJwtAudience,
                ["Jwt:SecretKey"]                = DevJwtSecret,
                ["Jwt:AccessTokenExpiryMinutes"] = "60",
                ["Jwt:RefreshTokenExpiryDays"]   = "30",
                ["Admin:Email"]                  = "admin@lexify.test",
                ["Admin:Password"]               = "Admin1234!",
                ["Admin:DisplayName"]            = "Test Admin",
            });
        });

        builder.ConfigureServices(services =>
        {
            // ── Replace DbContext ──────────────────────────────────────────────────
            foreach (var d in services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                d.ServiceType == typeof(AppDbContext)).ToList())
            {
                services.Remove(d);
            }
            services.AddDbContext<AppDbContext>(opts =>
                opts.UseNpgsql(_pgContainer.GetConnectionString()));

            // ── Replace Hangfire PostgreSQL storage with in-memory ─────────────────
            foreach (var d in services.Where(d => d.ServiceType == typeof(JobStorage)).ToList())
                services.Remove(d);
            // Use a factory so InMemoryStorage is created after LoggerFactory is ready.
            services.AddSingleton<JobStorage>(_ => new InMemoryStorage());

            // ── Replace IBackgroundJobService with no-op ──────────────────────────
            var bgDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IBackgroundJobService));
            if (bgDesc != null) services.Remove(bgDesc);
            services.AddScoped<IBackgroundJobService, NoOpBackgroundJobService>();

            // ── Replace IEmailService with no-op ──────────────────────────────────
            var emailDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (emailDesc != null) services.Remove(emailDesc);
            services.AddScoped<IEmailService, NoOpEmailService>();

            // ── Replace IAIProvider with streaming stub ───────────────────────────
            var aiDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IAIProvider));
            if (aiDesc != null) services.Remove(aiDesc);
            services.AddScoped<IAIProvider, StreamingStubAIProvider>();
        });
    }

    /// <summary>
    /// Registers a unique user, logs them in, and returns an HttpClient pre-configured
    /// with their Bearer token plus the raw access and refresh tokens.
    /// </summary>
    public async Task<(HttpClient Client, string AccessToken, string RefreshToken)>
        CreateAuthenticatedClientAsync(string? email = null, string password = "Password1!")
    {
        email ??= $"test-{Guid.NewGuid():N}@example.com";

        var client = CreateClient();

        var registerResp = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password, displayName = "Test User" });
        registerResp.EnsureSuccessStatusCode();

        var loginResp = await client.PostAsJsonAsync("/api/auth/login",
            new { email, password });
        loginResp.EnsureSuccessStatusCode();

        var json = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken  = json.GetProperty("accessToken").GetString()!;
        var refreshToken = json.GetProperty("refreshToken").GetString()!;

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        return (client, accessToken, refreshToken);
    }
}
