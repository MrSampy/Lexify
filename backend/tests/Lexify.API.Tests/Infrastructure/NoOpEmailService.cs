using Lexify.Application.Abstractions;

namespace Lexify.API.Tests.Infrastructure;

public sealed class NoOpEmailService : IEmailService
{
    public Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
