using Lexify.Application.Common;
using Lexify.Application.Tests.Dtos;
using MediatR;

namespace Lexify.Application.Tests.Commands.GenerateTest;

public sealed record GenerateTestCommand(
    Guid UserId,
    IReadOnlyList<Guid> BlockIds,
    IReadOnlyList<string> QuestionTypes,
    int QuestionCount)
    : IRequest<Result<GenerateTestResult>>;
