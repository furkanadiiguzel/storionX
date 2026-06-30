using EvStorionX.Application.Abstractions;
using EvStorionX.Domain.Entities;

namespace EvStorionX.Application.Pipeline;

/// <summary>A single unit of work queued by the producer and consumed by the worker pool.</summary>
public sealed record ItemWorkUnit(
    /// <summary>Parent archive that owns the item.</summary>
    Archive Archive,

    /// <summary>The item to migrate.</summary>
    Item Item,

    /// <summary>Pre-resolved storionX target archive identifier.</summary>
    string TargetArchive,

    /// <summary>Policy decision that allowed this item to proceed.</summary>
    PolicyDecision Policy
);
