namespace Lexify.Application.Common;

/// <summary>Result for commands that return no value.</summary>
public class Result : IResult
{
    public bool IsSuccess { get; }
    public ResultStatus Status { get; }
    public string? ErrorMessage { get; }

    protected Result(bool isSuccess, ResultStatus status, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Status = status;
        ErrorMessage = errorMessage;
    }

    public static Result Ok() => new(true, ResultStatus.Ok, null);
    public static Result NotFound(string? errorMessage = null) => new(false, ResultStatus.NotFound, errorMessage);
    public static Result Forbidden(string? errorMessage = null) => new(false, ResultStatus.Forbidden, errorMessage);
    public static Result Failure(string errorMessage) => new(false, ResultStatus.Failure, errorMessage);

    // Generic factories live on the non-generic class to satisfy CA1000.
    public static Result<T> Ok<T>(T value) => new(value);
    public static Result<T> NotFound<T>(string? errorMessage = null) => new(false, ResultStatus.NotFound, default, errorMessage);
    public static Result<T> Forbidden<T>(string? errorMessage = null) => new(false, ResultStatus.Forbidden, default, errorMessage);
    public static Result<T> Failure<T>(string errorMessage) => new(false, ResultStatus.Failure, default, errorMessage);
}

/// <summary>Result for commands/queries that return a value.</summary>
public sealed class Result<T> : Result
{
    public T? Value { get; }

    internal Result(T value) : base(true, ResultStatus.Ok, null) => Value = value;

    internal Result(bool isSuccess, ResultStatus status, T? value, string? errorMessage)
        : base(isSuccess, status, errorMessage) => Value = value;
}
