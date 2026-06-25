using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Blocks.Commands.ImportBlockFromCsv;

public sealed class ImportBlockFromCsvCommandHandler(
    IWordBlockRepository blockRepository,
    IWordRepository wordRepository,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ImportBlockFromCsvCommand, Result<Guid>>
{
    private const int MaxWords = 500;

    public async Task<Result<Guid>> Handle(
        ImportBlockFromCsvCommand request, CancellationToken cancellationToken)
    {
        var rows = ParseCsv(request.CsvContent);
        if (rows.Count == 0)
            return Result.Failure<Guid>("CSV file is empty or contains no data rows.");
        if (rows.Count > MaxWords)
            return Result.Failure<Guid>($"CSV must not exceed {MaxWords} words. Found {rows.Count}.");

        var block = WordBlock.Create(currentUser.UserId, request.LanguageId, request.Title);
        await blockRepository.AddAsync(block, cancellationToken);
        // Flush to get DB-generated block Id before creating words that reference it
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var words = new List<Word>(rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            var cols = rows[i];
            if (cols.Length < 2 || string.IsNullOrWhiteSpace(cols[0]) || string.IsNullOrWhiteSpace(cols[1]))
                continue;

            var wordType = cols.Length > 2 && Word.WordTypes.All.Contains(cols[2].Trim())
                ? cols[2].Trim()
                : Word.WordTypes.Word;

            var word = new Word(
                block.Id,
                cols[0].Trim(),
                cols[1].Trim(),
                wordType,
                cols.Length > 3 && !string.IsNullOrWhiteSpace(cols[3]) ? cols[3].Trim() : null,
                cols.Length > 4 && !string.IsNullOrWhiteSpace(cols[4]) ? cols[4].Trim() : null,
                sortOrder: i + 1);

            words.Add(word);
        }

        if (words.Count == 0)
            return Result.Failure<Guid>("CSV contains no valid word rows (term and translation are required).");

        await wordRepository.AddRangeAsync(words, cancellationToken);
        return Result.Ok(block.Id);
    }

    /// <summary>RFC 4180–compliant CSV parser. Skips the header row.</summary>
    private static List<string[]> ParseCsv(string content)
    {
        var rows = new List<string[]>();
        var lines = content.ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        bool isHeader = true;

        foreach (var line in lines)
        {
            if (isHeader) { isHeader = false; continue; } // skip header
            var fields = SplitCsvLine(line);
            if (fields.Length > 0)
                rows.Add(fields);
        }

        return rows;
    }

    private static string[] SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var i = 0;
        while (i <= line.Length)
        {
            string field;
            if (i < line.Length && line[i] == '"')
            {
                // Quoted field
                i++; // skip opening quote
                var sb = new System.Text.StringBuilder();
                while (i < line.Length)
                {
                    if (line[i] == '"')
                    {
                        i++;
                        if (i < line.Length && line[i] == '"') { sb.Append('"'); i++; } // escaped quote
                        else break; // closing quote
                    }
                    else { sb.Append(line[i]); i++; }
                }
                field = sb.ToString();
                if (i < line.Length && line[i] == ',') i++; // skip comma
            }
            else
            {
                var end = line.IndexOf(',', i);
                if (end == -1) { field = line[i..]; i = line.Length + 1; }
                else { field = line[i..end]; i = end + 1; }
            }
            fields.Add(field);
        }
        return [.. fields];
    }
}
