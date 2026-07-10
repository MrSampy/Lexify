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

    // ImportLineParser already extracts term/translation deterministically for the overwhelming
    // majority of lines — the LLM's job per batch is enrichment, not parsing, so batches can be
    // larger than the old raw-text-parsing flow tolerated. Batches still run sequentially to avoid
    // overloading the single local inference server.
    private const int BatchSize = 10;

    public async IAsyncEnumerable<FormatWordsSseEvent> Handle(
        FormatWordsCommand request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var parsedLines = ImportLineParser.Parse(request.RawText);

        if (parsedLines.Count == 0)
        {
            yield return new FormatWordsSseEvent("error", ErrorMessage: "Input text is empty.");
            yield break;
        }

        var batches = parsedLines.Chunk(BatchSize).ToList();

        yield return new FormatWordsSseEvent("parsing");

        var allWords = new List<FormatWordItem>();
        string? firstBatchTitle = null;
        string? lastError = null;
        var failedBatches = 0;

        foreach (var batch in batches)
        {
            FormatWordsResult? batchResult = null;

            for (int attempt = 0; attempt <= MaxRetries && batchResult is null; attempt++)
            {
                var sb = new StringBuilder();

                await foreach (var chunk in aiProvider.StreamEnrichWordsAsync(
                    batch, request.TargetLanguage, request.NativeLanguage, cancellationToken))
                {
                    sb.Append(chunk);
                    // Only stream chunks on the first attempt of each batch; retries are silent
                    if (attempt == 0)
                        yield return new FormatWordsSseEvent("streaming", Chunk: chunk);
                }

                var validation = AIResponseValidator.ValidateEnrichment(sb.ToString(), batch);
                if (validation.IsValid)
                    batchResult = validation.ParsedResult;
                else
                    lastError = validation.ErrorMessage;
            }

            // Every retry failed — fall back to parser-only output rather than dropping the whole
            // batch. Only lines the parser itself couldn't split (raw passthrough) are actually lost.
            if (batchResult is null)
            {
                failedBatches++;
                batchResult = AIResponseValidator.DegradeToParsedOnly(batch);
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
            ? $"{failedBatches} of {batches.Count} batches: AI enrichment failed, used basic parsing only (no word type, notes, or example sentence); any unparseable lines in those batches were dropped."
            : null;

        yield return new FormatWordsSseEvent(
            "done",
            Result: new FormatWordsResult(allWords, title),
            ErrorMessage: errorMessage);
    }
}
