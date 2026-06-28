using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EvStorionX.Infrastructure.Persistence;
using EvStorionX.MockEv.Generator.Options;

namespace EvStorionX.MockEv.Generator.Generation;

internal sealed class GeneratorWorker(
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime lifetime,
    GeneratorOptions opts,
    ILogger<GeneratorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation(
                "Starting generator — seed={Seed}, archives={A}, parts={P}, reset={R}",
                opts.Seed, opts.Archives, opts.Parts, opts.Reset);

            await using var scope = scopeFactory.CreateAsyncScope();
            var faker     = scope.ServiceProvider.GetRequiredService<EvDataFaker>();
            var seeder    = scope.ServiceProvider.GetRequiredService<Seeder>();
            var blobs     = scope.ServiceProvider.GetRequiredService<BlobWriter>();
            var mapWriter = scope.ServiceProvider.GetRequiredService<IdentityMapWriter>();

            var data = faker.Generate(opts);

            logger.LogInformation(
                "Generated {A} archives, {I} items, {P} SIS parts. Orphaned UPNs: {O}",
                data.Archives.Count, data.Items.Count, data.Parts.Count, data.OrphanedUpns.Count);

            await blobs.WriteAsync(data.Parts, opts.BlobDir, stoppingToken);
            await mapWriter.WriteAsync(data, opts.BlobDir, opts.Seed, stoppingToken);
            await seeder.SeedAsync(data, opts.Reset, stoppingToken);

            logger.LogInformation("All done. Exiting.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Generator failed.");
            Environment.ExitCode = 1;
        }
        finally
        {
            lifetime.StopApplication();
        }
    }
}
