namespace EvStorionX.Infrastructure.MockEv;

/// <summary>Configuration for <see cref="FilePartReader"/>.</summary>
public sealed class FilePartReaderOptions
{
    /// <summary>Directory that contains the <c>&lt;partId&gt;.bin</c> blob files.</summary>
    public string BlobDir { get; set; } = "./data/blobs";
}
