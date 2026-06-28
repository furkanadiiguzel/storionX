namespace Migration.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-level migration errors.
/// Catch this type to handle any migration failure; catch <see cref="PermanentMigrationException"/>
/// to distinguish non-retriable errors.
/// </summary>
public class MigrationException : Exception
{
    /// <summary>Machine-readable error code (e.g. <c>ITEM_NOT_FOUND</c>, <c>HASH_MISMATCH</c>).</summary>
    public string ErrorCode { get; }

    /// <inheritdoc cref="MigrationException"/>
    public MigrationException(string errorCode, string message) : base(message)
        => ErrorCode = errorCode;

    /// <inheritdoc cref="MigrationException"/>
    public MigrationException(string errorCode, string message, Exception inner) : base(message, inner)
        => ErrorCode = errorCode;
}

/// <summary>
/// Signals a non-retriable migration failure — analogous to HTTP 4xx responses.
/// Indicates that retrying the same item with the same data will not succeed.
/// </summary>
public sealed class PermanentMigrationException : MigrationException
{
    /// <inheritdoc cref="PermanentMigrationException"/>
    public PermanentMigrationException(string errorCode, string message) : base(errorCode, message) { }

    /// <inheritdoc cref="PermanentMigrationException"/>
    public PermanentMigrationException(string errorCode, string message, Exception inner) : base(errorCode, message, inner) { }
}
