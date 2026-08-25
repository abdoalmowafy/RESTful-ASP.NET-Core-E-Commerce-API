namespace ECommerce.Infrastructure.Abstractions;

public class Result
{
    public Result(bool isSucceed, Error error)
    {
        if ((isSucceed && error != Error.None) || (!isSucceed && error == Error.None))
            throw new InvalidOperationException("A successful result must carry no error and a failed result must carry one");

        IsSucceed = isSucceed;
        Error = error;
    }

    public bool IsSucceed { get; }
    public bool IsFailure => !IsSucceed;
    public Error Error { get; }

    public static Result Succeed() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<TValue> Succeed<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    public Result(TValue? value, bool isSucceed, Error error) : base(isSucceed, error)
        => _value = value;

    public TValue Value => IsSucceed ? _value! : throw new InvalidOperationException("Failure results cannot have value");
}
