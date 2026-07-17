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

    /// <summary>
    /// Inserts only the keys that are missing, rather than bailing out when the table is non-empty.
    /// An all-or-nothing seed would never deliver a setting added after the first deployment — and a
    /// key with no row cannot be edited in the admin UI (UpdateSystemSetting only updates existing
    /// rows), so e.g. closing registration would strand the operator with no way to set an invite
    /// code. Existing rows are left untouched: operators' edits must survive a redeploy.
    /// </summary>
    private async Task SeedSystemSettingsAsync(CancellationToken ct)
    {
        var settings = new SystemSetting[]
        {
            new("ai.primary_model",              "gemma3:27b", "string", "Active Ollama model"),
            new("ai.fallback_enabled",           "true",     "bool",   "Enable OpenAI fallback"),
            new("ai.rate_limit_per_minute",      "10",       "int",    "Per-user AI request limit per minute"),
            new(SystemSetting.Keys.MaxAiCallsPerUserPerDay, "50", "int",
                "Per-user AI calls allowed per UTC day (0 = unlimited). Caps AI spend per user."),
            new(SystemSetting.Keys.RegistrationEnabled, "true", "bool", "Allow new user registrations"),
            new(SystemSetting.Keys.InviteCode,   "",         "string",
                "Shared invite code. Only used when registration is disabled; empty then closes sign-up entirely."),
            new(SystemSetting.Keys.MaxWordsPerBlock, "200",  "int",    "Max words per block (0 = unlimited)"),
            new(SystemSetting.Keys.MaxBlocksPerUser, "0",    "int",    "Max blocks per user (0 = unlimited)"),
            new(SystemSetting.Keys.TestMaxQuestions, "50",   "int",    "Max questions per test"),
            new(SystemSetting.Keys.MaintenanceEnabled, "false", "bool",
                "Maintenance mode: non-admin API requests get 503 (sign-in and health stay open)"),
            new(SystemSetting.Keys.TtsEnabled,   "true",     "bool",
                "Enable server-side neural TTS (Piper). Off = clients use browser speech synthesis."),
        };

        var existingKeys = await db.SystemSettings
            .Select(s => s.Key)
            .ToListAsync(ct);

        var missing = settings
            .Where(s => !existingKeys.Contains(s.Key))
            .ToArray();

        if (missing.Length == 0)
            return;

        db.SystemSettings.AddRange(missing);
        await db.SaveChangesAsync(ct);
        LogSettingsSeeded(logger, missing.Length);
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
