using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using EvStorionX.Infrastructure.Persistence;
using EvStorionX.MockEv.Generator.Generation;
using EvStorionX.MockEv.Generator.Options;

var parseResult = Parser.Default.ParseArguments<GeneratorOptions>(args);

await parseResult.WithParsedAsync(async opts =>
{
    var connectionString =
        opts.Connection
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
        ?? throw new InvalidOperationException(
            "Provide --connection <connstr> or set the ConnectionStrings__Default environment variable.");

    var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices(services =>
        {
            services.AddInfrastructure(connectionString);

            services.AddSingleton(opts);
            services.AddTransient<EvDataFaker>();
            services.AddTransient<BlobWriter>();
            services.AddTransient<IdentityMapWriter>();
            services.AddTransient<Seeder>();
            services.AddHostedService<GeneratorWorker>();
        })
        .UseConsoleLifetime()
        .Build();

    await host.RunAsync();
});
