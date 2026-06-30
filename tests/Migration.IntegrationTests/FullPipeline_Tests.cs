extern alias MigrationApi;
extern alias MockStorionX;

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using EvStorionX.Infrastructure.Persistence;
using EvStorionX.IntegrationTests.Fixtures;

namespace EvStorionX.IntegrationTests;

/// <summary>
/// End-to-end integration tests that spin up both APIs in-process with a real MySQL container.
/// </summary>
[Collection("MySQL")]
public sealed class FullPipeline_Tests(MysqlContainerFixture mysql) : IAsyncLifetime
{
    private WebApplicationFactory<MigrationApi::Program>?  _api;
    private WebApplicationFactory<MockStorionX::Program>? _mockStorionX;

    public Task InitializeAsync()
    {
        _mockStorionX = new WebApplicationFactory<MockStorionX::Program>();

        _api = new WebApplicationFactory<MigrationApi::Program>()
            .WithWebHostBuilder(b =>
            {
                b.UseSetting("ConnectionStrings:Default", mysql.ConnectionString);
                b.UseSetting("StorionXClient:BaseUrl",
                    _mockStorionX.Server.BaseAddress.ToString().TrimEnd('/'));
                b.UseSetting("FilePartReader:BasePath",
                    Path.Combine(AppContext.BaseDirectory, "TestData", "blobs"));
                b.UseSetting("JsonIdentityMap:FilePath",
                    Path.Combine(AppContext.BaseDirectory, "TestData", "mapping.json"));
            });

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_api is not null)         await _api.DisposeAsync();
        if (_mockStorionX is not null) await _mockStorionX.DisposeAsync();
    }

    [Fact]
    public async Task PostRuns_DryRun_CompletesWithNoFailures()
    {
        EnsureTestData();

        using var client = _api!.CreateClient();

        // Start a dry-run (no data needed in mock EvStorage or storionX)
        var startResp = await client.PostAsJsonAsync("/runs", new { DryRun = true });
        startResp.IsSuccessStatusCode.Should().BeTrue($"POST /runs returned {startResp.StatusCode}");

        var startBody = await startResp.Content.ReadFromJsonAsync<JsonElement>();
        var runIdStr  = startBody.GetProperty("runId").GetString()!;
        var runId     = Guid.Parse(runIdStr);

        // Poll GET /runs/{id} until the run completes (max 30s)
        var deadline  = DateTime.UtcNow.AddSeconds(30);
        string? status = null;
        while (DateTime.UtcNow < deadline && status is not ("Completed" or "Failed"))
        {
            await Task.Delay(250);
            var poll = await client.GetAsync($"/runs/{runId}");
            if (!poll.IsSuccessStatusCode) continue;
            var body = await poll.Content.ReadFromJsonAsync<JsonElement>();
            body.TryGetProperty("status", out var s);
            status = s.GetString();
        }

        status.Should().Be("Completed", "the dry-run should finish within 30 seconds");

        // Audit log should be non-empty
        var auditResp = await client.GetAsync($"/runs/{runId}/audit");
        auditResp.IsSuccessStatusCode.Should().BeTrue();
        var auditBody = await auditResp.Content.ReadFromJsonAsync<JsonElement[]>();
        auditBody.Should().NotBeNullOrEmpty("at least RunStarted and RunCompleted events should be recorded");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void EnsureTestData()
    {
        var blobsDir    = Path.Combine(AppContext.BaseDirectory, "TestData", "blobs");
        var mappingPath = Path.Combine(AppContext.BaseDirectory, "TestData", "mapping.json");

        Directory.CreateDirectory(blobsDir);

        if (!File.Exists(mappingPath))
            File.WriteAllText(mappingPath,
                """{"alice@contoso.com": "user_mailbox:alice@contoso.com"}""");
    }
}

[CollectionDefinition("MySQL")]
public sealed class MySqlCollectionDefinition : ICollectionFixture<MysqlContainerFixture> { }
