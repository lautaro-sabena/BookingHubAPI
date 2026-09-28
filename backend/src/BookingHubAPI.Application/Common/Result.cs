namespace BookingHubAPI.Application.Common;

/// <summary>Category of an expected, non-exceptional failure of an application use case.</summary>
public enum ErrorKind
{
    Validation,
    NotFound,
    Forbidden,
    Conflict
}

/// <summary>An expected failure: a category the caller can map to a transport response, plus a message.</summary>
public sealed record Error(ErrorKind Kind, string Message)
{
    public static Error Validation(string message) => new(ErrorKind.Validation, message);
    public static Error NotFound(string message) => new(ErrorKind.NotFound, message);
    public static Error Forbidden(string message) => new(ErrorKind.Forbidden, message);
    public static Error Conflict(string message) => new(ErrorKind.Conflict, message);
}

/// <summary>Outcome of a use case that returns no value.</summary>
public class Result
{
    protected Result(Error? error)
    {
        Error = error;
    }

    public bool IsSuccess => Error is null;

    public bool IsFailure => !IsSuccess;

    /// <summary>The failure; null when <see cref="IsSuccess"/>.</summary>
    public Error? Error { get; }

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error);
}

/// <summary>Outcome of a use case that returns a value on success.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, Error? error) : base(error)
    {
        _value = value;
    }

    /// <summary>The success value. Throws when the result is a failure.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result has no value.");

    public static Result<T> Success(T value) => new(value, null);

    public static new Result<T> Failure(Error error) => new(default, error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
}
