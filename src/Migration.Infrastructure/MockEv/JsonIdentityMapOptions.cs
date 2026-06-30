namespace EvStorionX.Infrastructure.MockEv;

/// <summary>Configuration for <see cref="JsonIdentityMap"/>.</summary>
public sealed class JsonIdentityMapOptions
{
    /// <summary>Path to the <c>mapping.json</c> file produced by the mock EV generator.</summary>
    public string MappingFilePath { get; set; } = "./data/mapping.json";
}
