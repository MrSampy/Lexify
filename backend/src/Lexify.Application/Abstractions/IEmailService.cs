namespace Lexify.Application.Abstractions;

public interface IEmailService
{
    Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default);
}
