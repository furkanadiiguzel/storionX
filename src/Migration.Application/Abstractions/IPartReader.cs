using EvStorionX.Domain.Entities;

namespace EvStorionX.Application.Abstractions;

/// <summary>Reads raw SIS part bytes and metadata from the EV vault store.</summary>
public interface IPartReader
{
    /// <summary>Fetches the raw bytes for <paramref name="partId"/> from the backing store.</summary>
    Task<ReadOnlyMemory<byte>> ReadPartAsync(string partId, CancellationToken ct);

    /// <summary>Returns the <see cref="SisPart"/> descriptor (hash, size, ref) without downloading bytes.</summary>
    Task<SisPart> GetMetadataAsync(string partId, CancellationToken ct);
}
