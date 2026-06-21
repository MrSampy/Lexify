using Lexify.Application.Admin.Dtos;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Admin.Queries.GetLanguages;

public sealed record GetLanguagesQuery(bool IncludeInactive = true)
    : IRequest<Result<IReadOnlyList<LanguageDto>>>;
