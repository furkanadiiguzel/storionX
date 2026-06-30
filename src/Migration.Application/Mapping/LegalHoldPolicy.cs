namespace EvStorionX.Application.Mapping;

/// <summary>Determines how archives under legal hold are handled during migration.</summary>
public enum LegalHoldPolicy
{
    /// <summary>Legal-hold archives are retained in EV and excluded from migration.</summary>
    Retain,

    /// <summary>Legal-hold archives are migrated to storionX with the hold flag preserved.</summary>
    Migrate,
}
