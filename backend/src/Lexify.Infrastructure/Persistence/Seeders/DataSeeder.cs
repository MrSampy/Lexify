using Lexify.Application.Abstractions;
using Lexify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lexify.Infrastructure.Persistence.Seeders;

public sealed partial class DataSeeder(
    AppDbContext db,
    IConfiguration configuration,
    IPasswordHasher passwordHasher,
    ILogger<DataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedLanguagesAsync(ct);
        await SeedSystemSettingsAsync(ct);
        await SeedAdminAsync(ct);
    }

    private async Task SeedLanguagesAsync(CancellationToken ct)
    {
        if (await db.Languages.AnyAsync(ct))
            return;

        var languages = new Language[]
        {
            new("en", "English",    "English",    isActive: true, sortOrder: 1),
            new("no", "Norwegian",  "Norsk",      isActive: true, sortOrder: 2),
            new("uk", "Ukrainian",  "Українська", isActive: true, sortOrder: 3),
            new("ru", "Russian",    "Русский",    isActive: true, sortOrder: 4),
            new("de", "German",     "Deutsch",    isActive: true, sortOrder: 5),
            new("pl", "Polish",     "Polski",     isActive: true, sortOrder: 6),
            new("fr", "French",     "Français",   isActive: true, sortOrder: 7),
            new("es", "Spanish",    "Español",    isActive: true, sortOrder: 8),
            new("it", "Italian",    "Italiano",   isActive: true, sortOrder: 9),
        };

        db.Languages.AddRange(languages);
        await db.SaveChangesAsync(ct);
        LogLanguagesSeeded(logger, languages.Length);
    }

    private async Task SeedSystemSettingsAsync(CancellationToken ct)
    {
        if (await db.SystemSettings.AnyAsync(ct))
            return;

        var settings = new SystemSetting[]
        {
            new("ai.primary_model",              "gemma3:27b", "string", "Active Ollama model"),
            new("ai.fallback_enabled",           "true",     "bool",   "Enable OpenAI fallback"),
            new("ai.rate_limit_per_minute",      "10",       "int",    "Per-user AI request limit per minute"),
            new("features.registration_enabled", "true",     "bool",   "Allow new user registrations"),
            new("features.max_words_per_block",  "200",      "int",    "Max words per block (0 = unlimited)"),
            new("features.max_blocks_per_user",  "0",        "int",    "Max blocks per user (0 = unlimited)"),
            new("test.max_questions",            "50",       "int",    "Max questions per test"),
            new("maintenance.enabled",           "false",    "bool",   "Maintenance mode"),
        };

        db.SystemSettings.AddRange(settings);
        await db.SaveChangesAsync(ct);
        LogSettingsSeeded(logger, settings.Length);
    }

    /// <summary>
    /// Reads the admin account from configuration ("Admin" section), so the same values work whether
    /// they come from appsettings, user-secrets, or the Admin__Email / Admin__Password environment
    /// variables the containers pass. A pre-computed BCrypt hash may be supplied via Admin:PasswordHash
    /// instead of Admin:Password when the plaintext should never reach the environment.
    /// </summary>
    private async Task SeedAdminAsync(CancellationToken ct)
    {
        var email = configuration["Admin:Email"];
        var password = configuration["Admin:Password"];
        var configuredHash = configuration["Admin:PasswordHash"];

        var passwordHash = !string.IsNullOrWhiteSpace(configuredHash)
            ? configuredHash
            : !string.IsNullOrWhiteSpace(password)
                ? passwordHasher.Hash(password)
                : null;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(passwordHash))
        {
            LogAdminSkipped(logger);
            return;
        }

        var normalizedEmail = email.ToLowerInvariant().Trim();
        if (await db.Users.AnyAsync(u => u.Email == normalizedEmail, ct))
        {
            LogAdminExists(logger, normalizedEmail);
            return;
        }

        var admin = new User(normalizedEmail, passwordHash, displayName: "Admin",
            role: User.Roles.Admin, status: User.Statuses.Active);

        db.Users.Add(admin);
        await db.SaveChangesAsync(ct);
        LogAdminSeeded(logger, normalizedEmail);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded {Count} languages.")]
    static partial void LogLanguagesSeeded(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded {Count} system settings.")]
    static partial void LogSettingsSeeded(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Admin:Email or Admin:Password/Admin:PasswordHash not set — skipping admin seed.")]
    static partial void LogAdminSkipped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Admin user {Email} already exists — skipping.")]
    static partial void LogAdminExists(ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded admin user {Email}.")]
    static partial void LogAdminSeeded(ILogger logger, string email);
}
