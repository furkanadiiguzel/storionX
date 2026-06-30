using EvStorionX.Application.Dto;
using EvStorionX.Domain.Entities;

namespace EvStorionX.Application.Abstractions;

/// <summary>Migration outcome for a single archive/item pair.</summary>
public enum PolicyOutcome
{
    /// <summary>Item should be ingested into storionX.</summary>
    Proceed,

    /// <summary>Owner mailbox has no mapping — item is orphaned.</summary>
    Orphaned,

    /// <summary>Item is under legal hold and must be retained in EV.</summary>
    LegalHoldRetain,

    /// <summary>Item is under legal hold but policy mandates it is migrated anyway.</summary>
    LegalHoldMigrate,

    /// <summary>Item falls outside the active migration filter criteria.</summary>
    SkippedByFilter,
}

/// <summary>Immutable result returned by <see cref="IPolicyEngine.Decide"/>.</summary>
public sealed record PolicyDecision(PolicyOutcome Outcome, string Reason);

/// <summary>Evaluates whether a given item should be migrated, skipped, or retained.</summary>
public interface IPolicyEngine
{
    /// <summary>
    /// Applies retention, legal-hold, and filter rules to decide how to handle
    /// <paramref name="item"/> within its parent <paramref name="archive"/>.
    /// </summary>
    PolicyDecision Decide(Archive archive, Item item, MigrationFilters filters);
}
