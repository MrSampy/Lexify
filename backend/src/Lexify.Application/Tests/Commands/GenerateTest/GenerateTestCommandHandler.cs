using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Application.Tests.Dtos;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Tests.Commands.GenerateTest;

public sealed class GenerateTestCommandHandler(
    IWordBlockRepository blockRepository,
    IWordRepository wordRepository,
    ITestRepository testRepository,
    ISystemSettingRepository settingRepository,
    IBackgroundJobService backgroundJobService,
    IAiQuotaService aiQuota,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GenerateTestCommand, Result<GenerateTestResult>>
{
    public async Task<Result<GenerateTestResult>> Handle(
        GenerateTestCommand request, CancellationToken cancellationToken)
    {
        // Checked before the test row is created: generation is handed to a background job, so a
        // test persisted here would sit in "generating" forever if the job then hit the cap.
        var quota = await aiQuota.CheckAsync(request.UserId, cancellationToken);
        if (quota.IsExceeded)
            return Result.Failure<GenerateTestResult>(
                $"Daily AI limit reached ({quota.Used}/{quota.Limit}). It resets at midnight UTC.");

        // Runtime cap on top of the validator's static 5–50 range, so the admin can lower it live.
        var maxQuestions = await settingRepository.GetIntAsync(
            SystemSetting.Keys.TestMaxQuestions, fallback: 50, cancellationToken);
        if (maxQuestions > 0 && request.QuestionCount > maxQuestions)
            return Result.Failure<GenerateTestResult>(
                $"Question count {request.QuestionCount} exceeds the current limit of {maxQuestions}.");

        // Verify all blocks exist and belong to the requesting user
        var blocks = new List<WordBlock>();
        foreach (var blockId in request.BlockIds)
        {
            var block = await blockRepository.GetByIdAsync(blockId, cancellationToken);
            if (block is null)
                return Result.NotFound<GenerateTestResult>($"Block {blockId} not found.");
            if (block.UserId != request.UserId)
                return Result.Forbidden<GenerateTestResult>("You do not have access to one or more blocks.");
            blocks.Add(block);
        }

        // Ensure minimum word count across all selected blocks
        int totalWords = 0;
        foreach (var blockId in request.BlockIds)
            totalWords += await wordRepository.CountByBlockIdAsync(blockId, null, cancellationToken);

        if (totalWords < 5)
            return Result.Failure<GenerateTestResult>(
                $"At least 5 words are required to generate a test. Selected blocks contain {totalWords}.");

        // Derive a human-readable title
        var title = blocks.Count == 1
            ? $"Test: {blocks[0].Title}"
            : $"Test: {blocks[0].Title} +{blocks.Count - 1} more";

        // Persist the test in "generating" status
        var test = Test.Create(request.UserId, title);
        await testRepository.AddAsync(test, cancellationToken);

        var testBlocks = request.BlockIds
            .Select(blockId => new TestBlock(test.Id, blockId))
            .ToList();
        await testRepository.AddTestBlocksAsync(testBlocks, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Hand off to background job; this returns immediately
        backgroundJobService.EnqueueGenerateTest(
            test.Id,
            request.UserId,
            [.. request.BlockIds],
            [.. request.QuestionTypes],
            request.QuestionCount);

        return Result.Ok(new GenerateTestResult(test.Id, test.Status));
    }
}
