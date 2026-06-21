using Lexify.Application.Admin.Dtos;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Admin.Queries.GetLanguages;

public sealed class GetLanguagesQueryHandler(ILanguageRepository languageRepository)
    : IRequestHandler<GetLanguagesQuery, Result<IReadOnlyList<LanguageDto>>>
{
    public async Task<Result<IReadOnlyList<LanguageDto>>> Handle(
        GetLanguagesQuery request, CancellationToken cancellationToken)
    {
        var languages = await languageRepository.GetAllAsync(request.IncludeInactive, cancellationToken);

        IReadOnlyList<LanguageDto> dtos = languages
            .Select(l => new LanguageDto(l.Id, l.Code, l.Name, l.NativeName, l.IsActive, l.SortOrder))
            .ToList();

        return Result.Ok(dtos);
    }
}
