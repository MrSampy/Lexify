using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Blocks.Commands.ExportBlock;

public sealed record ExportBlockCommand(Guid BlockId, Guid UserId) : IRequest<Result<ExportBlockResult>>;

public sealed record ExportBlockResult(string FileName, string ContentType, byte[] Content);
