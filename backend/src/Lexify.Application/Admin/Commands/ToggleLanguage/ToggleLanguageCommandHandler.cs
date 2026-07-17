using System.Text.Json;
using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Admin.Commands.ToggleLanguage;

public sealed class ToggleLanguageCommandHandler(
    ILanguageRepository languageRepository,
    IAuditService auditService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ToggleLanguageCommand, Result>
{
    public async Task<Result> Handle(ToggleLanguageCommand request, CancellationToken cancellationToken)
    {
        var language = await languageRepository.GetByCodeAsync(request.Code, cancellationToken);
        if (language is null)
            return Result.NotFound($"Language '{request.Code}' not found.");

        var wasActive = language.IsActive;
        language.Toggle();
        await languageRepository.UpdateAsync(language, cancellationToken);

        await auditService.LogAsync(
            "toggle_language", "Language", language.Code,
            oldValueJson: JsonSerializer.Serialize(wasActive ? "active" : "inactive"),
            newValueJson: JsonSerializer.Serialize(language.IsActive ? "active" : "inactive"),
            ct: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
