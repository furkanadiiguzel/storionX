using CommandLine;

namespace EvStorionX.MockEv.Generator.Options;

public sealed class GeneratorOptions
{
    [Option("seed", Default = 42, HelpText = "Random seed — same seed produces identical output.")]
    public int Seed { get; set; }

    [Option("archives", Default = 20, HelpText = "Number of EV archives to generate.")]
    public int Archives { get; set; }

    [Option("items-per-archive-min", Default = 50, HelpText = "Min items per archive.")]
    public int ItemsPerArchiveMin { get; set; }

    [Option("items-per-archive-max", Default = 500, HelpText = "Max items per archive.")]
    public int ItemsPerArchiveMax { get; set; }

    [Option("parts", Default = 200, HelpText = "Total unique SIS parts (dedup pool size).")]
    public int Parts { get; set; }

    [Option("blob-dir", Default = "./data/blobs", HelpText = "Directory for .bin blob files.")]
    public string BlobDir { get; set; } = "./data/blobs";

    [Option("connection", Required = false, HelpText = "MySQL connection string. Falls back to ConnectionStrings__Default env var.")]
    public string? Connection { get; set; }

    [Option("reset", Default = false, HelpText = "TRUNCATE all tables before seeding (idempotent re-run).")]
    public bool Reset { get; set; }
}
