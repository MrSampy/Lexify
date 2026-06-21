using Lexify.Application.Common;
using Lexify.Application.Tests.Dtos;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Tests.Queries.GetTestById;

public sealed class GetTestByIdQueryHandler(
    ITestRepository testRepository)
    : IRequestHandler<GetTestByIdQuery, Result<TestDto>>
{
    public async Task<Result<TestDto>> Handle(GetTestByIdQuery request, CancellationToken cancellationToken)
    {
        var test = await testRepository.GetByIdWithQuestionsAsync(request.TestId, cancellationToken);
        if (test is null)
            return Result.NotFound<TestDto>("Test not found.");

        if (test.UserId != request.UserId)
            return Result.Forbidden<TestDto>("You do not have access to this test.");

        var questionDtos = test.Questions
            .OrderBy(q => q.SortOrder)
            .Select(q => new QuestionDto(
                q.Id,
                q.QuestionType,
                q.QuestionText,
                q.SortOrder,
                q.Options
                    .OrderBy(o => o.SortOrder)
                    .Select(o => new QuestionOptionDto(o.Id, o.OptionText, o.SortOrder))
                    .ToList()))
            .ToList();

        var dto = new TestDto(
            test.Id, test.Title, test.Status, test.QuestionCount, test.CreatedAt, questionDtos);

        return Result.Ok(dto);
    }
}
