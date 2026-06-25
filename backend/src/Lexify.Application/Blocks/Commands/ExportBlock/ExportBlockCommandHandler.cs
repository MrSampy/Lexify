using System.Text;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Blocks.Commands.ExportBlock;

public sealed class ExportBlockCommandHandler(
    IWordBlockRepository blockRepository,
    IWordRepository wordRepository)
    : IRequestHandler<ExportBlockCommand, Result<ExportBlockResult>>
{
    public async Task<Result<ExportBlockResult>> Handle(
        ExportBlockCommand request, CancellationToken cancellationToken)
    {
        var block = await blockRepository.GetByIdAsync(request.BlockId, cancellationToken);
        if (block is null) return Result.NotFound<ExportBlockResult>("Block not found.");
        if (block.UserId != request.UserId) return Result.Forbidden<ExportBlockResult>("Access denied.");

        var words = await wordRepository.GetByBlockIdAsync(
            request.BlockId, null, 0, int.MaxValue, cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("term,translation,wordType,notes,exampleSentence");
        foreach (var w in words)
        {
            csv.AppendLine(string.Join(",",
                EscapeCsv(w.Term),
                EscapeCsv(w.Translation),
                EscapeCsv(w.WordType),
                EscapeCsv(w.Notes ?? ""),
                EscapeCsv(w.ExampleSentence ?? "")));
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        var safeName = string.Concat(block.Title.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        var fileName = $"{safeName}.csv";

        return Result.Ok(new ExportBlockResult(fileName, "text/csv", bytes));
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
