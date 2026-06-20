namespace Lexify.Application.Common;

public interface IResult
{
    bool IsSuccess { get; }
    ResultStatus Status { get; }
    string? ErrorMessage { get; }
}
