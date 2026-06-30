# EvStorionX Migration Tool

A production-grade, end-to-end email-archive migration pipeline that moves items from **Enterprise Vault (EV)** into **storionX**. The system discovers EV archives from MySQL, rehydrates SIS part files into storionX message format, calls the storionX ingest API with full Polly-based retry/circuit-breaker protection, and records every outcome in an immutable audit log — with checkpoint/resume so a failed run picks up exactly where it left off.

Includes a **mock EV data generator**, a **mock storionX server** with configurable chaos, a **REST API** for triggering and monitoring runs, and a **React dashboard** for real-time visibility.

---

## Quick Start

```bash
git clone <repo-url>
cd ev-storionx-migration

# 1. Start MySQL and wait for it to be healthy
docker compose up -d mysql

# 2. Apply EF Core migrations (creates all tables)
docker compose run --rm tools \
  dotnet ef database update \
    --project src/Migration.Infrastructure \
    --startup-project src/Migration.Api \
    -- --environment Development \
       "ConnectionStrings:Default=Server=mysql;Port=3306;Database=migration_db;User=root;Password=devpassword;"

# 3. Seed mock EV data (archives + items + blob files)
docker compose run --rm generator -- \
  --seed 42 --archives 20 --blob-dir /data/blobs

# 4. Start all services
docker compose up -d mockstorionx api frontend
```

| Service | URL |
|---------|-----|
| Migration API | http://localhost:8080 |
| OpenAPI schema | http://localhost:8080/openapi/v1.json |
| Mock storionX | http://localhost:8081 |
| Dashboard | http://localhost:5173 |

> **Run your first migration immediately:**
> ```bash
> curl -s -X POST http://localhost:8080/runs | jq .
> ```
> Then open the dashboard and watch it complete in real time.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      Migration.Api                          │
│  POST /runs  ──►  MigrationOrchestrator                     │
│                        │                                    │
│           ┌────────────┼────────────┐                       │
│           ▼            ▼            ▼                       │
│       IDiscovery   IRehydrator  IStateStore                 │
│       (EfDiscovery) (Rehydrator) (EfStateStore)             │
│           │            │                                    │
│           ▼            ▼                                    │
│       MySQL DB    SIS blob files  ──►  ITransformer         │
│                                           │                 │
│                                    IStorionXClient          │
│                                    (Polly retry + CB)       │
│                                           │                 │
└───────────────────────────────────────────┼─────────────────┘
                                            ▼
                              ┌─────────────────────────┐
                              │   Mock storionX (8081)  │
                              │   POST /ingest          │
                              │   GET  /stats           │
                              │   Chaos: 503 injection  │
                              └─────────────────────────┘
```

Each run streams audit events to MySQL and checkpoints progress every N items. A crashed or cancelled run resumes from the exact item where it stopped by reusing the same `runId`.

---

## Technology Stack

| Layer | Technology | Why |
|---|---|---|
| Runtime | .NET 10 | Latest LTS; primary constructors, `TypedResults`, `params` collections |
| ORM | EF Core 9 + Pomelo 9 | EF Core 10 + Pomelo 10 not yet stable; 9.x battle-tested on MySQL 8.4 |
| Database | MySQL 8.4 | Matches typical enterprise EV environments |
| Resilience | `Microsoft.Extensions.Http.Resilience` | Polly v8 via `AddStandardResilienceHandler`; retry + circuit breaker |
| HTTP layer | ASP.NET Core Minimal API | Low ceremony; `TypedResults` drives OpenAPI schema inference |
| Test data | Bogus (Faker) | Reproducible seeding via `--seed`; deterministic fixtures |
| Frontend | React 19 + Vite 8 | Fast HMR, concurrent features, lazy routes |
| Styling | Tailwind CSS v4 + shadcn/ui | `@import "tailwindcss"` — no config file needed |
| Data fetching | TanStack React Query 5 | Poll active runs, cache invalidation on mutation, skeleton states |
| API types | openapi-typescript 7 | `pnpm gen:api` → zero manual interface duplication |

---

## Folder Structure

```
ev-storionx-migration/
├── src/
│   ├── Migration.Domain/           # Entities, enums, value objects (no dependencies)
│   ├── Migration.Application/      # Pipeline, abstractions, DTOs, transforms
│   ├── Migration.Infrastructure/   # EF Core, Pomelo, StorionX HTTP client, state store
│   ├── Migration.Api/              # Minimal API host; run endpoints + OpenAPI
│   ├── Migration.MockStorionX/     # In-memory storionX stub with rate limiting + chaos
│   └── Migration.MockEv.Generator/ # CLI seed tool — writes archives, items, SIS blobs
├── tests/
│   ├── Migration.UnitTests/        # xUnit + FluentAssertions + Moq (in-memory fakes)
│   └── Migration.IntegrationTests/ # Testcontainers.MySql + WebApplicationFactory
├── frontend/                       # React dashboard (see frontend/README.md)
├── data/
│   ├── blobs/                      # SIS part binary files (committed; reproducible via --seed 42)
│   └── mapping.json                # EV → storionX identity map
└── docker/
    ├── backend.Dockerfile          # Multi-stage: restore → build → api/mockstorionx/generator/tools
    └── frontend.Dockerfile         # pnpm dev server (dev) + nginx (prod)
```

---

## Running Commands

### Development (local dotnet)

```bash
# Start dependencies
docker compose up -d mysql mockstorionx

# Run API (connects to MySQL on 3307)
dotnet run --project src/Migration.Api

# Run frontend dev server
cd frontend && pnpm dev
```

### Tests

```bash
# Unit tests only — no external dependencies
dotnet test tests/Migration.UnitTests

# All tests — Testcontainers spins up MySQL automatically
dotnet test

# Inside Docker
docker compose run --rm tools dotnet test
```

### Build

```bash
dotnet build -c Release
cd frontend && pnpm build
```

### EF Core Migrations

```bash
# Generate a new migration
dotnet ef migrations add <Name> \
  --project src/Migration.Infrastructure \
  --startup-project src/Migration.Api

# Apply to local MySQL (port 3307)
dotnet ef database update \
  --project src/Migration.Infrastructure \
  --startup-project src/Migration.Api
```

---

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/runs` | Start a migration run; optional body `{ runId, dryRun }` |
| `GET` | `/runs` | List all in-memory runs (resets on API restart) |
| `GET` | `/runs/{id}` | Poll status and summary of a run |
| `POST` | `/runs/{id}/resume` | Cancel and restart from last checkpoint |
| `GET` | `/runs/{id}/audit` | Full audit log; `?format=csv` for download |
| `GET` | `/runs/{id}/reconciliation` | Cross-reference local records vs storionX `/stats` |
| `GET` | `/archives` | EV archives discovered in MySQL |

Interactive docs: **http://localhost:8080/openapi/v1.json** — import into Bruno, Insomnia, or Postman.

**Mock storionX endpoints** (http://localhost:8081):

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/ingest` | Accept a message; returns 200/409/422/503 |
| `GET` | `/stats` | Ingest counters and dedup statistics |
| `GET` | `/health` | Docker healthcheck |
| `POST` | `/admin/reset` | Wipe in-memory state (integration test teardown) |

---

## Configuration

### Migration.Api

| Variable | Default | Description |
|---|---|---|
| `ConnectionStrings__Default` | *(required)* | MySQL connection string |
| `StorionXClient__BaseUrl` | `http://localhost:8081` | Target storionX URL |
| `StorionXClient__MaxRetryAttempts` | `5` | Polly retry count (exponential back-off) |
| `StorionXClient__BaseDelayMs` | `500` | Base retry delay in ms |
| `StorionXClient__CircuitBreakerFailureRatio` | `0.5` | CB opens when 50% of requests fail |
| `StorionXClient__CircuitBreakerSamplingSeconds` | `30` | CB sampling window |
| `Orchestrator__MaxParallelWorkers` | `8` | Concurrent item tasks |
| `Orchestrator__CheckpointEveryN` | `100` | Checkpoint interval |
| `Orchestrator__DryRun` | `false` | Simulate without calling `/ingest` |
| `PolicyOptions__LegalHoldPolicy` | `Retain` | `Retain` skips legal-hold items; `Migrate` includes them |
| `FilePartReader__BlobDir` | `/data/blobs` | SIS part binary file directory |
| `JsonIdentityMap__MappingFile` | `/data/mapping.json` | EV → storionX identity map |

### Mock storionX

| Variable | Default | Description |
|---|---|---|
| `RateLimit__RequestsPerSecond` | `20` | Token-bucket steady rate |
| `RateLimit__BurstCapacity` | `40` | Burst headroom |
| `Chaos__Transient503Percent` | `5` (dev: `0`) | % of requests that return 503 |

### Frontend

| Variable | Default | Description |
|---|---|---|
| `VITE_API_URL` | `http://localhost:8080` | Migration.Api base URL |

Regenerate TypeScript types after any API schema change:

```bash
pnpm --prefix frontend gen:api
```

---

## Design Assumptions

Explicit V1 tradeoffs (full rationale in inline comments throughout the codebase):

- **V1 — EF Core Discovery.** EV archives are loaded from MySQL (populated by the generator). In production this would be replaced by a real EV STEP-compliant API client.
- **V2 — File-based SIS parts.** `FilePartReader` reads `.bin` blobs from disk. Real EV would serve parts over its archive retrieval API.
- **V3 — JSON identity map.** The EV → storionX ID mapping loads from a static JSON file at startup. At scale this needs a database table with incremental sync.
- **V4 — In-memory run tracking.** `RunTracker` is a `ConcurrentDictionary` inside the API process. Runs are lost on restart; checkpoint data in MySQL is preserved and a run can be resumed with its original `runId`.
- **V5 — LRU part cache.** SIS parts are cached in-process (1,000-slot LRU). A distributed cache (Redis) is needed for multi-worker deployments.
- **V6 — `IOptionsSnapshot` per scope.** `OrchestratorOptions.RunId` is injected per DI scope via `IPostConfigureOptions`. This ties the run lifecycle to the HTTP request scope — acceptable for a single-process tool.
- **V7 — Polly standard resilience.** `AddStandardResilienceHandler` provides retry + circuit breaker. Thresholds are tunable via configuration without code changes.
- **V8 — Mock storionX chaos.** Configurable 503 injection exercises the retry pipeline. `Chaos__Transient503Percent=0` in Development for a clean demo.
- **EF Core 9 on .NET 10.** The runtime targets `net10.0` but EF Core and Pomelo are pinned to 9.x because `Pomelo.EntityFrameworkCore.MySql` 10.x is not yet available. This is a deliberate, documented decision — not an oversight.

---

## What I Would Do With More Time

1. **Real EV connector** — Replace `EfDiscovery` and `FilePartReader` with a STEP-approved EV API client using its archive enumeration and item retrieval endpoints.
2. **Part-aware streaming upload** — Stream SIS parts with their MIME boundaries instead of assembling them in memory; essential for items with many large attachments.
3. **Distributed workers** — Publish archive work items to a message queue (e.g. Azure Service Bus) so multiple worker pods parallelise across archives while the queue provides durable back-pressure.
4. **Circuit breaker visibility in the UI** — Surface Polly circuit breaker state (Closed / Open / Half-Open) in the dashboard so operators see sustained target failures before the run faults.
5. **Real-time SignalR progress** — Replace the 2-second HTTP poll in `useRun` with a SignalR hub that pushes audit events as they are written, enabling a live per-item progress feed.
6. **Multi-tenant identity map** — The current JSON file is single-tenant. Production needs a per-tenant, database-backed mapping with incremental synchronisation from the EV identity service.

---

## Example Output

**Run summary — `GET /runs/{id}` after completion:**

```json
{
  "runId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Completed",
  "startedAt": "2026-06-30T14:02:11.432Z",
  "summary": {
    "totalArchives": 20,
    "totalItems": 4287,
    "migrated": 4201,
    "alreadyPresent": 42,
    "orphaned": 18,
    "skipped": 14,
    "failed": 12,
    "finishedAtUtc": "2026-06-30T14:04:38.901Z",
    "byArchive": {
      "arch-001": { "migrated": 312, "alreadyPresent": 3, "skipped": 0, "failed": 1 }
    }
  }
}
```

**Audit CSV — `GET /runs/{id}/audit?format=csv`:**

```csv
Id,TimestampUtc,EventType,ItemId,Payload,RunId
3c4d1e2f-...,2026-06-30T14:02:12.100Z,ItemMigrated,ev-item-00001,"{""storionXId"":""sx-abc123""}",3fa85f64-...
7a8b9c0d-...,2026-06-30T14:02:12.340Z,ItemAlreadyPresent,ev-item-00002,"{""storionXId"":""sx-xyz789""}",3fa85f64-...
1e2f3a4b-...,2026-06-30T14:02:13.780Z,ItemPermanentFailed,ev-item-00007,"{""error"":""422 Unprocessable Entity""}",3fa85f64-...
```

---

## Known Limitations

- **Run state lost on restart.** `GET /runs` returns empty after the API process restarts. Checkpoint data in MySQL is preserved — resume works if you know the `runId`.
- **Reconciliation is mock-only.** `GET /runs/{id}/reconciliation` compares local audit records against mock storionX's aggregate `/stats`. A production reconciliation would need per-item cross-referencing.
- **Blob files committed to git.** `data/blobs/` and `data/mapping.json` are in the repo for demo convenience. Production should mount from a shared volume or object store.
- **No authentication.** All endpoints are unauthenticated. Add ASP.NET Core auth middleware before any network-accessible deployment.
- **Single-process orchestrator.** The orchestrator runs inside the API process, limited by a single machine's CPU/memory. Large-scale migrations need distributed workers.

---

## License

UNLICENSED — internal assessment project.
