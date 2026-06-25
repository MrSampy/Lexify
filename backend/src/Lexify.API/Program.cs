using Hangfire;
using Lexify.Application;
using Lexify.Infrastructure;
using Lexify.Infrastructure.Jobs;
using Lexify.Infrastructure.Persistence.Seeders;
using Lexify.API.Middleware;
using Lexify.API.RateLimit;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger — one page per functional area, each requiring Bearer login
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("auth",     new OpenApiInfo { Title = "Lexify – Auth",     Version = "v1" });
    options.SwaggerDoc("content",  new OpenApiInfo { Title = "Lexify – Content",  Version = "v1" });
    options.SwaggerDoc("learning", new OpenApiInfo { Title = "Lexify – Learning", Version = "v1" });
    options.SwaggerDoc("admin",    new OpenApiInfo { Title = "Lexify – Admin",    Version = "v1" });

    // Route each controller to its page
    options.DocInclusionPredicate((docName, api) =>
    {
        var controller = api.ActionDescriptor.RouteValues["controller"]?.ToLowerInvariant();
        return docName switch
        {
            "auth"     => controller == "auth",
            "content"  => controller is "blocks" or "words" or "search" or "tags" or "stats",
            "learning" => controller is "review" or "tests" or "attempts",
            "admin"    => controller == "admin",
            _          => false
        };
    });

    // Bearer auth on every page
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

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy<string, AiRateLimiterPolicy>(AiRateLimiterPolicy.PolicyName);
});

// CORS — allow the Vite dev server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services);

// Hangfire: global retry policy + recurring jobs
GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 3, DelaysInSeconds = [5, 25, 125] });

var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
recurringJobs.AddOrUpdate<CleanupRefreshTokensJob>(
    "cleanup-refresh-tokens", job => job.RunAsync(CancellationToken.None), Cron.Daily);
recurringJobs.AddOrUpdate<AnonymizeDeletedUsersJob>(
    "anonymize-deleted-users", job => job.RunAsync(CancellationToken.None), Cron.Daily);
recurringJobs.AddOrUpdate<SendReviewRemindersJob>(
    "send-review-reminders", job => job.RunAsync(CancellationToken.None), "0 8 * * *");
recurringJobs.AddOrUpdate<CleanupAiLogsJob>(
    "cleanup-ai-logs", job => job.RunAsync(CancellationToken.None), Cron.Monthly);

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

app.UseHttpsRedirection();
app.UseCors();
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.UseMiddleware<CurrentUserMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new Lexify.Infrastructure.HangfireAuthFilter()]
});
app.MapControllers();
app.Run();
