using System.Runtime.CompilerServices;
using System.Text;
using Lexify.Application.Abstractions;
using MediatR;

namespace Lexify.Application.AI.Commands.FormatWords;

public sealed class FormatWordsCommandHandler(IAIProvider aiProvider)
    : IStreamRequestHandler<FormatWordsCommand, FormatWordsSseEvent>
{
    private const int MaxRetries = 2;

    public async IAsyncEnumerable<FormatWordsSseEvent> Handle(
        FormatWordsCommand request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            yield return new FormatWordsSseEvent("parsing");

            var sb = new StringBuilder();

            await foreach (var chunk in aiProvider.StreamFormatWordsAsync(
                request.RawText, request.TargetLanguage, request.NativeLanguage, cancellationToken))
            {
                sb.Append(chunk);
                // Only stream chunks on first attempt; retries are silent
                if (attempt == 0)
                    yield return new FormatWordsSseEvent("streaming", Chunk: chunk);
            }

            var validation = AIResponseValidator.Validate(sb.ToString(), request.RawText);

            if (validation.IsValid)
            {
                yield return new FormatWordsSseEvent("done", Result: validation.ParsedResult);
                yield break;
            }

            if (attempt == MaxRetries)
                yield return new FormatWordsSseEvent("error", ErrorMessage: validation.ErrorMessage);
        }
    }
}
