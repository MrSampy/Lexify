using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexify.Infrastructure.Persistence.Seeders;

public static partial class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(ct);

        var seeder = sp.GetRequiredService<DataSeeder>();
        await seeder.SeedAsync(ct);

        var logger = sp.GetRequiredService<ILogger<AppDbContext>>();
        LogInitialized(logger);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Database initialization complete.")]
    static partial void LogInitialized(ILogger logger);
}
