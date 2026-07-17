using System.Text.Json;
using Lexify.Application.Abstractions;
using Lexify.Application.Admin.Dtos;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Admin.Commands.AddLanguage;

public sealed class AddLanguageCommandHandler(
    ILanguageRepository languageRepository,
    IAuditService auditService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddLanguageCommand, Result<LanguageDto>>
{
    public async Task<Result<LanguageDto>> Handle(AddLanguageCommand request, CancellationToken cancellationToken)
    {
        var existing = await languageRepository.GetByCodeAsync(request.Code, cancellationToken);
        if (existing is not null)
            return Result.Failure<LanguageDto>($"Language with code '{request.Code}' already exists.");

        var language = new Language(request.Code, request.Name, request.NativeName, true, request.SortOrder);
        await languageRepository.AddAsync(language, cancellationToken);

        await auditService.LogAsync(
            "add_language", "Language", language.Code,
            newValueJson: JsonSerializer.Serialize($"{language.Name} ({language.NativeName})"),
            ct: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(new LanguageDto(
            language.Id, language.Code, language.Name,
            language.NativeName, language.IsActive, language.SortOrder));
    }
}
