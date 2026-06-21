using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Admin.Commands.ToggleLanguage;

public sealed class ToggleLanguageCommandHandler(
    ILanguageRepository languageRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ToggleLanguageCommand, Result>
{
    public async Task<Result> Handle(ToggleLanguageCommand request, CancellationToken cancellationToken)
    {
        var language = await languageRepository.GetByCodeAsync(request.Code, cancellationToken);
        if (language is null)
            return Result.NotFound($"Language '{request.Code}' not found.");

        language.Toggle();
        await languageRepository.UpdateAsync(language, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
