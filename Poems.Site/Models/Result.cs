using Microsoft.AspNetCore.Mvc;

namespace Poems.Site.Models;

public class Result
{
    public bool IsSuccess { get; }
    public int StatusCode { get; }
    public string? Message { get; }

    protected Result(bool isSuccess, int statusCode, string? message)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Message = message;
    }

    public static Result Success(int statusCode = 200) => new Result(true,
        statusCode, null);

    public static Result Failure(string message, int statusCode = 400)
        => new Result(false, statusCode, message);
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, int statusCode, string? message, T? value)
        : base(isSuccess, statusCode, message)
    {
        Value = value;
    }

    public static Result<T> Success(T content, int statusCode = 200)
        => new Result<T>(true, statusCode, null, content);

    public static Result<T> Failure(string message, int statusCode = 400, T? content = default)
        => new Result<T>(false, statusCode, message, content);
}

public static class ResultExtensions
{
    public static ActionResult<T> ToActionResult<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? new ObjectResult(result.Value) { StatusCode = result.StatusCode }
            : new ObjectResult(result.Message) { StatusCode = result.StatusCode };
    }
}