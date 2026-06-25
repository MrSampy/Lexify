namespace Lexify.Application.Words.Queries.SearchWords;

public sealed record SearchWordDto(
    Guid WordId,
    Guid BlockId,
    string BlockTitle,
    string Term,
    string Translation,
    string WordType,
    double Rank);
