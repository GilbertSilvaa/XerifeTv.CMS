namespace XerifeTv.CMS.Modules.Common;

public sealed record Error(string Code, string? Description = null)
{
    public static readonly Error None = new(string.Empty);
}

public class Result<T>
{
    private Result(bool isSuccess, Error error, T? data = default)
    {
        if (!isSuccess && error == Error.None || isSuccess && error != Error.None)
            throw new ArgumentException("Invalid error", nameof(error));

        IsSuccess = isSuccess;
        Error = error;
        Data = data;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }
    public T? Data { get; } = default;

    public static Result<T> Success(T data) => new(true, Error.None, data);
    public static Result<T> Failure(Error error) => new(false, error);
}
