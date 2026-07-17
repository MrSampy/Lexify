using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Blocks.Commands.CreateBlock;

public sealed class CreateBlockCommandHandler(
    IWordBlockRepository blockRepository,
    ISystemSettingRepository settingRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<CreateBlockCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateBlockCommand request, CancellationToken cancellationToken)
    {
        var maxBlocks = await settingRepository.GetIntAsync(
            SystemSetting.Keys.MaxBlocksPerUser, fallback: 0, cancellationToken);
        if (maxBlocks > 0)
        {
            var owned = await blockRepository.CountByUserIdAsync(currentUser.UserId, ct: cancellationToken);
            if (owned >= maxBlocks)
                return Result.Failure<Guid>($"Block limit reached ({owned}/{maxBlocks}).");
        }

        var block = WordBlock.Create(
            currentUser.UserId,
            request.LanguageId,
            request.Title,
            request.Description);

        await blockRepository.AddAsync(block, cancellationToken);

        return Result.Ok(block.Id);
    }
}
