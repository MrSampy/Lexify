using System.Security.Cryptography;
using Lexify.Application.Abstractions;
using Lexify.Application.Blocks.Common;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Blocks.Commands.CreateBlockShare;

public sealed class CreateBlockShareCommandHandler(
    IWordBlockRepository blockRepository,
    IBlockShareRepository shareRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<CreateBlockShareCommand, Result<BlockShareDto>>
{
    public async Task<Result<BlockShareDto>> Handle(
        CreateBlockShareCommand request, CancellationToken cancellationToken)
    {
        var block = await blockRepository.GetByIdAsync(request.BlockId, cancellationToken);
        if (block is null)
            return Result.NotFound<BlockShareDto>("Block not found.");

        if (block.UserId != currentUser.UserId)
            return Result.Forbidden<BlockShareDto>("You do not have access to this block.");

        // Idempotent: pressing "share" twice must not invalidate the link already pasted into a chat.
        var existing = await shareRepository.GetActiveByBlockIdAsync(block.Id, cancellationToken);
        if (existing is not null)
            return Result.Ok(ToDto(existing));

        var share = new BlockShare(block.Id, currentUser.UserId, GenerateToken());
        await shareRepository.AddAsync(share, cancellationToken);

        return Result.Ok(ToDto(share));
    }

    /// <summary>
    /// 24 random bytes, base64url-encoded: the token is the only thing guarding the block, so it has to
    /// be far beyond guessing and safe to drop into a URL unescaped.
    /// </summary>
    private static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static BlockShareDto ToDto(BlockShare share) =>
        new(share.Token, share.CreatedAt, share.ViewCount, share.CopyCount);
}
