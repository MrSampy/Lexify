using System.Runtime.CompilerServices;
using System.Text;
using Lexify.Application.Abstractions;
using Lexify.Application.AI.Dtos;
using MediatR;

namespace Lexify.Application.AI.Commands.FormatWords;

public sealed class FormatWordsCommandHandler(IAIProvider aiProvider)
    : IStreamRequestHandler<FormatWordsCommand, FormatWordsSseEvent>
{
    private const int MaxRetries = 2;

    // Local 8B models translate short lists far more accurately than long ones — a big prompt
    // dilutes their attention and quality degrades (observed: garbled translations, dropped lines).
    // Batching keeps each LLM call small; batches run sequentially to avoid overloading the
    // single local inference server.
    private const int BatchSize = 6;

    public async IAsyncEnumerable<FormatWordsSseEvent> Handle(
        FormatWordsCommand request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var lines = request.RawText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.Length > 0)
            .ToList();

        if (lines.Count == 0)
        {
            yield return new FormatWordsSseEvent("error", ErrorMessage: "Input text is empty.");
            yield break;
        }

        var batches = lines
            .Chunk(BatchSize)
            .Select(chunk => string.Join('\n', chunk))
            .ToList();

        yield return new FormatWordsSseEvent("parsing");

        var allWords = new List<FormatWordItem>();
        string? firstBatchTitle = null;
        string? lastError = null;
        var failedBatches = 0;

        foreach (var batchText in batches)
        {
            FormatWordsResult? batchResult = null;

            for (int attempt = 0; attempt <= MaxRetries && batchResult is null; attempt++)
            {
                var sb = new StringBuilder();

                await foreach (var chunk in aiProvider.StreamFormatWordsAsync(
                    batchText, request.TargetLanguage, request.NativeLanguage, cancellationToken))
                {
                    sb.Append(chunk);
                    // Only stream chunks on the first attempt of each batch; retries are silent
                    if (attempt == 0)
                        yield return new FormatWordsSseEvent("streaming", Chunk: chunk);
                }

                var validation = AIResponseValidator.Validate(sb.ToString(), batchText);
                if (validation.IsValid)
                    batchResult = validation.ParsedResult;
                else
                    lastError = validation.ErrorMessage;
            }

            if (batchResult is null)
            {
                failedBatches++;
                continue;
            }

            allWords.AddRange(batchResult.Words);
            firstBatchTitle ??= batchResult.SuggestedTitle;
        }

        if (allWords.Count == 0)
        {
            yield return new FormatWordsSseEvent(
                "error", ErrorMessage: lastError ?? "AI failed to format any words.");
            yield break;
        }

        // A single batch already produced a title for the whole input; for multiple batches ask
        // for a title over all recognized terms so it reflects the full list, not just batch one.
        var title = firstBatchTitle;
        if (batches.Count > 1)
        {
            var terms = allWords.Select(w => w.Term).ToList();
            title = await aiProvider.SuggestBlockTitleAsync(terms, request.TargetLanguage, cancellationToken)
                    ?? firstBatchTitle;
        }

        var errorMessage = failedBatches > 0
            ? $"{failedBatches} of {batches.Count} batches failed; some lines may be missing."
            : null;

        yield return new FormatWordsSseEvent(
            "done",
            Result: new FormatWordsResult(allWords, title),
            ErrorMessage: errorMessage);
    }
}
