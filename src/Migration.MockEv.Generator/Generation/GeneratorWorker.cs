using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EvStorionX.Infrastructure.Persistence;
using EvStorionX.MockEv.Generator.Options;

namespace EvStorionX.MockEv.Generator.Generation;

internal sealed partial class GeneratorWorker(
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime lifetime,
    GeneratorOptions opts,
    ILogger<GeneratorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            LogStarting(logger, opts.Seed, opts.Archives, opts.Parts, opts.Reset);

            await using var scope = scopeFactory.CreateAsyncScope();
            var seeder = scope.ServiceProvider.GetRequiredService<Seeder>();
            var blobs = scope.ServiceProvider.GetRequiredService<BlobWriter>();
            var mapWriter = scope.ServiceProvider.GetRequiredService<IdentityMapWriter>();

            var data = EvDataFaker.Generate(opts);

            LogGenerated(logger, data.Archives.Count, data.Items.Count,
                data.Parts.Count, data.OrphanedUpns.Count);

            await blobs.WriteAsync(data.Parts, opts.BlobDir, stoppingToken);
            await mapWriter.WriteAsync(data, opts.BlobDir, opts.Seed, stoppingToken);
            await seeder.SeedAsync(data, opts.Reset, stoppingToken);

            LogAllDone(logger);
        }
        catch (Exception ex)
        {
            LogFailed(logger, ex);
            Environment.ExitCode = 1;
        }
        finally
        {
            lifetime.StopApplication();
        }
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Starting generator — seed={Seed}, archives={A}, parts={P}, reset={R}")]
    private static partial void LogStarting(ILogger logger, int seed, int a, int p, bool r);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Generated {A} archives, {I} items, {P} SIS parts. Orphaned UPNs: {O}")]
    private static partial void LogGenerated(ILogger logger, int a, int i, int p, int o);

    [LoggerMessage(Level = LogLevel.Information, Message = "All done. Exiting.")]
    private static partial void LogAllDone(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Generator failed.")]
    private static partial void LogFailed(ILogger logger, Exception ex);
}
