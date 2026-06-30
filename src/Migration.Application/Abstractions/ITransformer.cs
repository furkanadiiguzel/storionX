using EvStorionX.Application.Dto;
using EvStorionX.Domain.Entities;

namespace EvStorionX.Application.Abstractions;

/// <summary>Converts a rehydrated EV item into a storionX-compatible ingest message.</summary>
public interface ITransformer
{
    /// <summary>
    /// Builds a <see cref="StorionXMessage"/> ready for submission to storionX.
    /// This is a pure, synchronous mapping — no I/O is performed.
    /// </summary>
    StorionXMessage Transform(Item item, Archive archive, RehydratedItem content, string targetArchive);
}
