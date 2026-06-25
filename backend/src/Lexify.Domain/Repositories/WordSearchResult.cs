namespace Lexify.Domain.Repositories;

public sealed record WordSearchResult(
    Guid WordId,
    Guid BlockId,
    string BlockTitle,
    string Term,
    string Translation,
    string WordType,
    double Rank);
