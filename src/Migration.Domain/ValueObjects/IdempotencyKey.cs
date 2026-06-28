namespace EvStorionX.Domain.ValueObjects;

/// <summary>
/// Stable, content-addressed key that uniquely identifies a migration unit across retries.
/// Format: <c>ev:&lt;vaultStore&gt;:&lt;archiveId&gt;:&lt;itemId&gt;</c>.
/// </summary>
public readonly record struct IdempotencyKey
{
    private readonly string _value;

    private IdempotencyKey(string value) => _value = value;

    /// <summary>Creates an <see cref="IdempotencyKey"/> from its three constituent parts.</summary>
    public static IdempotencyKey Create(string vaultStore, string archiveId, string itemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vaultStore);
        ArgumentException.ThrowIfNullOrWhiteSpace(archiveId);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        return new IdempotencyKey($"ev:{vaultStore}:{archiveId}:{itemId}");
    }

    /// <summary>Parses a previously serialised key string back into an <see cref="IdempotencyKey"/>.</summary>
    /// <exception cref="FormatException">Thrown when the string does not conform to the expected format.</exception>
    public static IdempotencyKey Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var parts = value.Split(':', 4);
        if (parts.Length != 4 || parts[0] != "ev")
            throw new FormatException($"Invalid IdempotencyKey format: '{value}'. Expected 'ev:<vaultStore>:<archiveId>:<itemId>'.");
        return new IdempotencyKey(value);
    }

    /// <inheritdoc/>
    public override string ToString() => _value;

    /// <summary>Implicit conversion to <see cref="string"/> for persistence convenience.</summary>
    public static implicit operator string(IdempotencyKey key) => key._value;
}
