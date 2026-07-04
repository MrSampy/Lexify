using Lexify.Application.Behaviors;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Blocks.Commands.ImportBlockFromCsv;

public sealed record ImportBlockFromCsvCommand(
    string Title,
    short LanguageId,
    string CsvContent
) : IRequest<Result<Guid>>, IInvalidatesBlocksCache;
