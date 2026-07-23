using Hangfire;
using Lexify.Application;
using Lexify.Infrastructure;
using Lexify.Infrastructure.Jobs;
using Lexify.Infrastructure.Persistence.Seeders;
using Lexify.API.Middleware;
using Lexify.API.RateLimit;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;
using Prometheus;
using Serilog;
using Serilog.Formatting.Compact;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Lexify.API")
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.File(
        new CompactJsonFormatter(),
        path: "logs/lexify-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14));

// ── Config sanity: refuse to boot production with dev secrets ─────────────────
if (builder.Environment.IsProduction())
{
    var jwtSecret = builder.Configuration["Jwt:SecretKey"] ?? string.Empty;
    if (jwtSecret.Length < 32 || jwtSecret.Contains("dev-secret", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException(
            "Jwt:SecretKey is missing, shorter than 32 characters, or a known dev value. " +
            "Set a strong secret via environment variables (Jwt__SecretKey) before running in Production.");
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── Health checks ─────────────────────────────────────────────────────────────
var connString = builder.Configuration.GetConnectionString("DefaultConnection");
var redisString = builder.Configuration.GetConnectionString("Redis");

var healthBuilder = builder.Services.AddHealthChecks();
if (!string.IsNullOrEmpty(connString))
    healthBuilder.AddNpgSql(connString, name: "postgres", tags: ["db"]);
if (!string.IsNullOrEmpty(redisString))
    healthBuilder.AddRedis(redisString, name: "redis", tags: ["cache"]);
// Degraded (not Unhealthy) so a dead LLM doesn't take the whole /health endpoint red —
// the rest of the app works without AI.
healthBuilder.AddCheck<Lexify.Infrastructure.Health.AiProviderHealthCheck>(
    "ai", failureStatus: HealthStatus.Degraded, tags: ["ai"]);

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("auth",     new OpenApiInfo { Title = "Lexify – Auth",     Version = "v1" });
    options.SwaggerDoc("content",  new OpenApiInfo { Title = "Lexify – Content",  Version = "v1" });
    options.SwaggerDoc("learning", new OpenApiInfo { Title = "Lexify – Learning", Version = "v1" });
    options.SwaggerDoc("admin",    new OpenApiInfo { Title = "Lexify – Admin",    Version = "v1" });

    options.DocInclusionPredicate((docName, api) =>
    {
        var controller = api.ActionDescriptor.RouteValues["controller"]?.ToLowerInvariant();
        return docName switch
        {
            "auth"     => controller == "auth",
            "content"  => controller is "blocks" or "words" or "search" or "tags" or "stats" or "feedback",
            "learning" => controller is "review" or "tests" or "attempts",
            "admin"    => controller == "admin",
            _          => false
        };
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT access token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

// ── Rate limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy<string, AiRateLimiterPolicy>(AiRateLimiterPolicy.PolicyName);
    options.AddPolicy<string, AuthRateLimiterPolicy>(AuthRateLimiterPolicy.PolicyName);
    options.AddPolicy<string, TwoFactorRateLimiterPolicy>(TwoFactorRateLimiterPolicy.PolicyName);
    options.AddPolicy<string, AccountEmailRateLimiterPolicy>(AccountEmailRateLimiterPolicy.PolicyName);
    options.AddPolicy<string, TtsRateLimiterPolicy>(TtsRateLimiterPolicy.PolicyName);
    options.AddPolicy<string, FeedbackRateLimiterPolicy>(FeedbackRateLimiterPolicy.PolicyName);
});

// ── CORS ─────────────────────────────────────────────────────────────────────
// In production the SPA is served from the same origin as the API (nginx proxies /api/* to the
// backend), so CORS is not exercised there. It only matters for the Vite dev server, which runs on
// a different port — hence the origin comes from config ("Frontend:Url") rather than being hardcoded.
var frontendUrl = builder.Configuration["Frontend:Url"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(frontendUrl)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseSerilogRequestLogging();

await DatabaseInitializer.InitializeAsync(app.Services);

// Hangfire: global retry policy + recurring jobs (skipped in the Testing environment)
if (!app.Environment.IsEnvironment("Testing"))
{
    GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 3, DelaysInSeconds = [5, 25, 125] });

    var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
    recurringJobs.AddOrUpdate<CleanupRefreshTokensJob>(
        "cleanup-refresh-tokens", job => job.RunAsync(CancellationToken.None), Cron.Daily);
    recurringJobs.AddOrUpdate<CleanupPasswordResetTokensJob>(
        "cleanup-password-reset-tokens", job => job.RunAsync(CancellationToken.None), Cron.Daily);
    recurringJobs.AddOrUpdate<CleanupEmailVerificationTokensJob>(
        "cleanup-email-verification-tokens", job => job.RunAsync(CancellationToken.None), Cron.Daily);
    recurringJobs.AddOrUpdate<CleanupTwoFactorCodesJob>(
        "cleanup-two-factor-codes", job => job.RunAsync(CancellationToken.None), Cron.Daily);
    recurringJobs.AddOrUpdate<AnonymizeDeletedUsersJob>(
        "anonymize-deleted-users", job => job.RunAsync(CancellationToken.None), Cron.Daily);
    recurringJobs.AddOrUpdate<SendReviewRemindersJob>(
        "send-review-reminders", job => job.RunAsync(CancellationToken.None), "0 8 * * *");
    recurringJobs.AddOrUpdate<CleanupAiLogsJob>(
        "cleanup-ai-logs", job => job.RunAsync(CancellationToken.None), Cron.Monthly);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(ui =>
    {
        ui.SwaggerEndpoint("/swagger/auth/swagger.json",     "Auth");
        ui.SwaggerEndpoint("/swagger/content/swagger.json",  "Content – Blocks & Words");
        ui.SwaggerEndpoint("/swagger/learning/swagger.json", "Learning – Review & Tests");
        ui.SwaggerEndpoint("/swagger/admin/swagger.json",    "Admin");
    });
}

// Behind the Caddy → nginx → backend chain the app only ever sees plain HTTP on the last hop.
// Without this, UseHttpsRedirection would see scheme=http and bounce every request into a redirect
// loop, and client IPs (used by the rate limiter) would all collapse to the proxy's address.
// KnownNetworks/KnownProxies are cleared because the proxy's container IP is not stable.
var forwardedHeaders = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,

    // TWO proxies sit in front, not one: the edge (Caddy, or a panel-managed Apache) forwards to the
    // frontend container, whose nginx appends the edge's address to X-Forwarded-For before calling
    // the backend. The default ForwardLimit of 1 pops only the last hop, leaving RemoteIpAddress set
    // to the edge proxy — identical for every visitor. AuthRateLimiterPolicy partitions on that
    // address, so its 10-requests-per-15-minutes budget became global: a handful of users, or one
    // person testing login, locks /api/auth/* for everyone with 429s.
    // Raising the limit to 2 unwinds both hops and yields the real client IP. It stays spoof-safe:
    // a client-supplied X-Forwarded-For gets the true address appended by Apache, so the two entries
    // consumed here are the ones the proxies wrote, never the attacker's.
    ForwardLimit = 2
};
forwardedHeaders.KnownNetworks.Clear();
forwardedHeaders.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaders);

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseHttpsRedirection();
app.UseCors();
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.UseMiddleware<CurrentUserMiddleware>();
// After authentication so admins (identified by their role claim) bypass maintenance mode.
app.UseMiddleware<MaintenanceModeMiddleware>();
app.UseAuthorization();

// Integration tests drive many accounts and submissions through one in-process host, which the
// per-IP auth limiter would reject long before the behaviour under test is reached.
if (!builder.Configuration.GetValue("RateLimiting:Disabled", false))
    app.UseRateLimiter();
app.UseHttpMetrics();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new Lexify.Infrastructure.HangfireAuthFilter()]
});
app.MapControllers();
app.MapHealthChecks("/api/health");
app.MapMetrics("/metrics");
app.Run();

public partial class Program { }
