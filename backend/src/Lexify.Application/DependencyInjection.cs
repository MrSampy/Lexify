using System.Reflection;
using FluentValidation;
using Lexify.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Lexify.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
            // Outside TransactionBehavior: cache is cleared only after SaveChanges succeeds
            cfg.AddOpenBehavior(typeof(BlocksCacheInvalidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);
        services.AddAutoMapper(assembly);

        return services;
    }
}
