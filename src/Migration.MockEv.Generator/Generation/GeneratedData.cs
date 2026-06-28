using EvStorionX.Domain.Entities;

namespace EvStorionX.MockEv.Generator.Generation;

/// <summary>Blob bytes paired with the SIS part metadata that references them.</summary>
public record SisPartWithData(SisPart Part, byte[] Bytes);

/// <summary>All data produced by <see cref="EvDataFaker"/> in a single generation pass.</summary>
public record GeneratedData(
    List<Archive>          Archives,
    List<Item>             Items,
    List<SisPartWithData>  Parts,
    /// <summary>UPN → storionX target archive ID (orphaned UPNs are intentionally absent).</summary>
    Dictionary<string, string> IdentityMap,
    IReadOnlySet<string>   OrphanedUpns
);
