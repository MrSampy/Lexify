using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Words.Queries.SearchWords;

public sealed record SearchWordsQuery(
    Guid UserId,
    string Q,
    short? LanguageId = null,
    int Limit = 20
) : IRequest<Result<IReadOnlyList<SearchWordDto>>>;
