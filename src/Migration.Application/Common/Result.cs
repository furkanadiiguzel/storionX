namespace EvStorionX.Application.Common;

/// <summary>
/// Discriminated union representing either success with a value or a typed failure.
/// Use instead of throwing exceptions for expected, recoverable error cases.
/// </summary>
public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly string? _errorCode;
    private readonly string? _errorMessage;

    private Result(T value)
    {
        IsSuccess = true;
        _value = value;
    }

    private Result(string errorCode, string errorMessage)
    {
        IsSuccess = false;
        _errorCode = errorCode;
        _errorMessage = errorMessage;
    }

    /// <summary><see langword="true"/> when the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary><see langword="true"/> when the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>The success value. Throws <see cref="InvalidOperationException"/> if the result is a failure.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot access Value on a failed result (code: {_errorCode}).");

    /// <summary>Machine-readable error code; non-null only on failure.</summary>
    public string? ErrorCode => _errorCode;

    /// <summary>Human-readable error description; non-null only on failure.</summary>
    public string? ErrorMessage => _errorMessage;

#pragma warning disable CA1000 // static factory methods on generic type are intentional
    /// <summary>Creates a successful result carrying <paramref name="value"/>.</summary>
    public static Result<T> Ok(T value) => new(value);

    /// <summary>Creates a failure result with <paramref name="errorCode"/> and <paramref name="errorMessage"/>.</summary>
    public static Result<T> Fail(string errorCode, string errorMessage) => new(errorCode, errorMessage);
#pragma warning restore CA1000

    /// <summary>Deconstructs into success flag and value (value is default on failure).</summary>
    public void Deconstruct(out bool isSuccess, out T? value)
    {
        isSuccess = IsSuccess;
        value = _value;
    }
}

/// <summary>Non-generic result for operations that produce no value on success.</summary>
public readonly struct Result
{
    private readonly string? _errorCode;
    private readonly string? _errorMessage;

    private Result(string errorCode, string errorMessage)
    {
        IsSuccess = false;
        _errorCode = errorCode;
        _errorMessage = errorMessage;
    }

    /// <summary><see langword="true"/> when the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary><see langword="true"/> when the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Machine-readable error code; non-null only on failure.</summary>
    public string? ErrorCode => _errorCode;

    /// <summary>Human-readable error description; non-null only on failure.</summary>
    public string? ErrorMessage => _errorMessage;

    /// <summary>The singleton successful result.</summary>
    public static Result Ok() => default;

    /// <summary>Creates a failure result with <paramref name="errorCode"/> and <paramref name="errorMessage"/>.</summary>
    public static Result Fail(string errorCode, string errorMessage) => new(errorCode, errorMessage);
}
