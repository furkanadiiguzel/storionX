namespace EvStorionX.Application.Transform;

/// <summary>Configuration for <see cref="EvToStorionXTransformer"/>.</summary>
public sealed class TransformerOptions
{
    /// <summary>Semantic version of the migration tooling, embedded in every chain-of-custody record.</summary>
    public string ToolVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Identifier of the current migration run, embedded in every chain-of-custody record.
    /// Should be overwritten per run by the orchestrator before transformation begins.
    /// </summary>
    public Guid RunId { get; set; } = Guid.Empty;
}
