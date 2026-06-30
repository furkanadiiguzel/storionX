namespace EvStorionX.Application.Mapping;

/// <summary>Configuration options for <see cref="PolicyEngine"/>.</summary>
public sealed class PolicyOptions
{
    /// <summary>How archives under legal hold should be treated. Defaults to <see cref="LegalHoldPolicy.Retain"/>.</summary>
    public LegalHoldPolicy LegalHoldPolicy { get; set; } = LegalHoldPolicy.Retain;
}
