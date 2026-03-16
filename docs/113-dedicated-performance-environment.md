# Feature 113: Dedicated Performance Test Environment (Pi 5)
> **Status:** Planning

## Overview

Establish a dedicated Raspberry Pi 5 as a self-hosted GitHub Actions runner purpose-built for performance testing. This replaces the current approach of running NBomber load tests in-process on GitHub-hosted runners (2 vCPU / 7GB RAM), which conflates test infrastructure overhead with application performance — producing unreliable metrics that measure runner starvation rather than app behaviour.

The Pi 5 is **dedicated exclusively to performance testing** — no production applications or containers will run on it, ensuring zero resource contention during test runs. It will run the application as a deployed container (real PostgreSQL, real HTTP, real I/O) and execute performance tests against it, producing metrics that reflect actual production-like conditions.

While the initial focus is BudgetExperiment, the runner infrastructure is designed to support **multiple projects** — other applications may use the same Pi 5 for their own performance test suites in the future.

## Problem Statement

### Current State

- Performance tests use `WebApplicationFactory` with an **in-memory EF Core database** — no real I/O, no connection pooling, no query plan behaviour.
- NBomber (load generator) and the application under test share the **same 2 vCPU GitHub runner** — at 100 req/s the runner is fully saturated, causing 30+ minute runs and misleading latency numbers.
- Stress test results reflect CI runner resource limits, not application performance characteristics.
- No way to measure real HTTP stack latency (Kestrel, middleware pipeline, serialisation).
- Baselines generated on shared GitHub runners are inherently noisy — other tenants, cold starts, and variable I/O affect results.

### Target State

- A dedicated Pi 5 self-hosted runner executes performance tests against a **running containerised instance** with a real PostgreSQL database.
- Tests exercise the full HTTP stack: DNS resolution (via Ubiquiti Cloud Gateway) → TCP → Kestrel → middleware → EF Core → PostgreSQL → serialisation → response.
- Baselines are stable and comparable across runs because the hardware is dedicated and consistent.
- The existing `performance.yml` workflow gains a new job (or mode) that targets the Pi 5 runner.
- In-process `WebApplicationFactory` tests remain available for fast smoke gates on GitHub-hosted runners.

---

## User Stories

### Self-Hosted Runner Setup

#### US-113-001: Pi 5 as GitHub Actions Runner
**As a** developer  
**I want to** have a dedicated Pi 5 registered as a self-hosted GitHub Actions runner  
**So that** performance tests run on consistent, dedicated hardware

**Acceptance Criteria:**
- [ ] Pi 5 registered as a self-hosted runner with label `perf-runner`
- [ ] Runner configured with auto-start on boot (systemd service)
- [ ] Runner has Docker installed and can pull images from ghcr.io
- [ ] Runner has .NET 10 SDK installed for running test projects
- [ ] Runner is online and visible in the repository's Settings → Actions → Runners

#### US-113-002: Performance Test Target Deployment
**As a** developer  
**I want to** deploy the application container on the Pi 5 before tests run  
**So that** tests execute against a real deployed instance

**Acceptance Criteria:**
- [ ] docker-compose file deploys app + PostgreSQL on the Pi 5
- [ ] Database is seeded with deterministic performance test data
- [ ] Health check confirms the app is ready before tests begin
- [ ] Container is torn down after tests complete (clean state per run)

### Out-of-Process Testing

#### US-113-003: Run NBomber Tests Against a Live URL
**As a** developer  
**I want to** point the existing performance test scenarios at a running HTTP endpoint  
**So that** I get real network + database latency measurements

**Acceptance Criteria:**
- [ ] Tests accept a base URL via environment variable (e.g., `PERF_TARGET_URL`)
- [ ] When `PERF_TARGET_URL` is set, tests use a plain `HttpClient` targeting that URL instead of `WebApplicationFactory`
- [ ] When `PERF_TARGET_URL` is not set, tests fall back to the existing in-process `WebApplicationFactory` behaviour
- [ ] Authentication is handled (test user token or auth bypass for perf environment)
- [ ] All existing scenarios (Smoke, Load, Stress, Spike) work in both modes

#### US-113-004: CI Workflow Integration
**As a** developer  
**I want to** trigger performance tests on the Pi 5 via the existing GitHub Actions workflow  
**So that** I can run real-hardware benchmarks on-demand or on schedule

**Acceptance Criteria:**
- [ ] `performance.yml` workflow has a new job targeting `runs-on: [self-hosted, perf-runner]`
- [ ] Scheduled runs (weekly) use the Pi 5 for Load + Stress profiles
- [ ] PR smoke tests continue to run on GitHub-hosted runners (fast feedback)
- [ ] Manual dispatch allows choosing runner target (GitHub-hosted vs Pi 5)
- [ ] Job timeout of 20 minutes prevents runaway test runs
- [ ] Baseline comparison runs against Pi 5 baselines (separate baseline file)

---

## Technical Design

### Architecture

```
┌─────────────────────────────────────────────────────┐
│  GitHub Actions                                     │
│                                                     │
│  ┌─────────────────┐    ┌────────────────────────┐  │
│  │ GitHub-hosted    │    │ Self-hosted Pi 5       │  │
│  │ ubuntu-latest    │    │ label: perf-runner     │  │
│  │                  │    │                        │  │
│  │ PR → Smoke tests │    │ Schedule/Manual →      │  │
│  │ (in-process,     │    │ Load/Stress/Spike      │  │
│  │  WebAppFactory)  │    │ (out-of-process,       │  │
│  │                  │    │  real HTTP + Postgres)  │  │
│  └─────────────────┘    └──────────┬─────────────┘  │
│                                    │                 │
└────────────────────────────────────┼─────────────────┘
                                     │
                          ┌──────────▼─────────────┐
                          │  Pi 5 Docker            │
                          │                         │
                          │  ┌───────────────────┐  │
                          │  │ PostgreSQL 16      │  │
                          │  │ (test database)    │  │
                          │  └────────▲──────────┘  │
                          │           │              │
                          │  ┌────────┴──────────┐  │
                          │  │ BudgetExperiment   │  │
                          │  │ API Container      │  │
                          │  │ http://localhost:   │  │
                          │  │ 5199               │  │
                          │  └───────────────────┘  │
                          └─────────────────────────┘
```

### Test Execution Modes

| Mode | Runner | Target | Database | Use Case |
|------|--------|--------|----------|----------|
| **In-Process** | GitHub-hosted | WebApplicationFactory | EF In-Memory | PR smoke gate (fast) |
| **Out-of-Process** | Pi 5 self-hosted | `http://localhost:5199` | PostgreSQL | Load/Stress baselines |
| **Remote** | Any | User-provided URL | Production DB | Ad-hoc testing against deployed instances |

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `PERF_TARGET_URL` | _(unset)_ | Base URL of the target instance. When set, bypasses WebApplicationFactory. |
| `PERF_AUTH_TOKEN` | _(unset)_ | Bearer token for authentication. Required for out-of-process mode unless auth is disabled. |
| `PERF_SEED_DATA` | `true` | Whether to seed the database before tests. Disable if database is pre-seeded. |

### PerformanceWebApplicationFactory Changes

The factory gain a static helper to determine mode:

```csharp
public static class PerformanceTestMode
{
    public static bool IsOutOfProcess =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PERF_TARGET_URL"));

    public static string TargetUrl =>
        Environment.GetEnvironmentVariable("PERF_TARGET_URL")
        ?? throw new InvalidOperationException("PERF_TARGET_URL not set");

    public static HttpClient CreateClient(PerformanceWebApplicationFactory? factory)
    {
        if (IsOutOfProcess)
        {
            var client = new HttpClient { BaseAddress = new Uri(TargetUrl) };
            var token = Environment.GetEnvironmentVariable("PERF_AUTH_TOKEN");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        // Existing in-process path
        return factory!.CreateApiClient();
    }
}
```

### Docker Compose for Performance Environment

A new `docker-compose.perf.yml` that deploys the app + PostgreSQL on the Pi 5 with a dedicated port (5199) to avoid conflicts with any production instance:

```yaml
# docker-compose.perf.yml — Performance test environment (Pi 5)
services:
  perf-postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: perftest
      POSTGRES_PASSWORD: perftest
      POSTGRES_DB: budgetperf
    tmpfs:
      - /var/lib/postgresql/data  # RAM-backed for speed, data is ephemeral
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U perftest"]
      interval: 5s
      timeout: 3s
      retries: 5

  perf-app:
    image: ghcr.io/becauseimclever/budgetexperiment:latest
    depends_on:
      perf-postgres:
        condition: service_healthy
    ports:
      - "5199:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__AppDb=Host=perf-postgres;Port=5432;Database=budgetperf;Username=perftest;Password=perftest
      - Authentication__Authentik__Enabled=false
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 5s
      timeout: 3s
      retries: 10
      start_period: 15s
```

### Workflow Changes (`performance.yml`)

Add a new `performance-dedicated` job that runs on the self-hosted Pi 5:

```yaml
  performance-dedicated:
    name: Performance Tests (Pi 5)
    runs-on: [self-hosted, perf-runner]
    if: >
      github.event_name == 'schedule' ||
      (github.event_name == 'workflow_dispatch' && github.event.inputs.runner == 'pi5')
    timeout-minutes: 20
    env:
      PERF_TARGET_URL: http://localhost:5199

    steps:
      - name: Checkout repository
        uses: actions/checkout@v6

      - name: Start performance environment
        run: |
          docker compose -f docker-compose.perf.yml pull
          docker compose -f docker-compose.perf.yml up -d
          # Wait for app health
          for i in $(seq 1 30); do
            curl -sf http://localhost:5199/health && break
            sleep 2
          done

      - name: Seed test data
        run: |
          # POST to a seeding endpoint or run a seeder tool
          dotnet run --project tests/BudgetExperiment.Performance.Tests/Tools/PerfDataSeeder/ \
            -- --url http://localhost:5199

      - name: Run performance tests
        run: |
          dotnet test tests/BudgetExperiment.Performance.Tests/BudgetExperiment.Performance.Tests.csproj \
            --configuration Release \
            --filter "${{ steps.profile.outputs.filter }}" \
            --logger "trx" \
            --results-directory ./TestResults

      - name: Tear down performance environment
        if: always()
        run: docker compose -f docker-compose.perf.yml down -v
```

### Workflow Input Changes

Add runner choice to `workflow_dispatch`:

```yaml
  workflow_dispatch:
    inputs:
      profile:
        description: 'Load profile to run'
        required: false
        default: 'Load'
        type: choice
        options: [Smoke, Load, Stress, Spike]
      runner:
        description: 'Runner target'
        required: false
        default: 'github'
        type: choice
        options:
          - github    # GitHub-hosted (in-process, fast)
          - pi5       # Pi 5 self-hosted (out-of-process, real)
```

### Separate Baselines

Since Pi 5 hardware differs from GitHub runners, baselines must be separate:

```
tests/BudgetExperiment.Performance.Tests/baselines/
  baseline.json           # GitHub-hosted runner baseline (existing)
  baseline-pi5.json       # Pi 5 dedicated runner baseline (new)
```

The workflow selects the correct baseline file based on the runner target.

---

## Implementation Plan

### Phase 1: Out-of-Process Test Support

**Objective:** Enable the existing test suite to run against a live HTTP endpoint via `PERF_TARGET_URL`.

**Tasks:**
- [ ] Create `PerformanceTestMode` helper class
- [ ] Update all test classes to use `PerformanceTestMode.CreateClient()` instead of directly calling `factory.CreateApiClient()`
- [ ] Handle data seeding: skip `TestDataSeeder.SeedAsync()` when in out-of-process mode (data must be pre-seeded)
- [ ] Add integration test verifying out-of-process mode works against a local instance
- [ ] Verify all 13 tests pass in both modes locally

**Commit:**
```bash
git commit -m "feat(perf): add out-of-process test mode via PERF_TARGET_URL

- PerformanceTestMode detects in-process vs out-of-process
- All scenarios work against live HTTP endpoints
- Falls back to WebApplicationFactory when PERF_TARGET_URL unset

Refs: #113"
```

---

### Phase 2: Performance Docker Compose

**Objective:** Create a docker-compose file for the ephemeral performance test environment.

**Tasks:**
- [ ] Create `docker-compose.perf.yml` with app + PostgreSQL (tmpfs-backed)
- [ ] Use a dedicated port (5199) to avoid conflicts
- [ ] Auth disabled for performance environment
- [ ] Verify: `docker compose -f docker-compose.perf.yml up -d` starts cleanly on Pi 5
- [ ] Verify: health endpoint responds within 30 seconds

**Commit:**
```bash
git commit -m "feat(perf): add docker-compose.perf.yml for dedicated test environment

- Ephemeral PostgreSQL on tmpfs for speed
- App container with auth disabled
- Port 5199 to avoid production conflicts

Refs: #113"
```

---

### Phase 3: Data Seeding for Out-of-Process

**Objective:** Seed the performance database via HTTP (since we can't access the in-memory DB from outside).

**Tasks:**
- [ ] Option A: Create a small `PerfDataSeeder` console tool that POSTs test data via API endpoints
- [ ] Option B: Add a `/api/v1/admin/seed-perf-data` endpoint (only enabled in non-Production or via feature flag)
- [ ] Evaluate: direct SQL seed script (`psql` into the perf-postgres container) may be simplest and fastest
- [ ] Ensure deterministic data matches `TestDataSeeder` volumes (750 transactions, 4 accounts, etc.)

**Commit:**
```bash
git commit -m "feat(perf): add out-of-process data seeding for Pi 5 environment

- Seed script creates deterministic test data via SQL
- Matches TestDataSeeder volumes for comparable baselines

Refs: #113"
```

---

### Phase 4: Pi 5 Self-Hosted Runner Setup

**Objective:** Register the Pi 5 as a GitHub Actions self-hosted runner.

**Tasks:**
- [ ] Install GitHub Actions runner on Pi 5 (ARM64 Linux)
- [ ] Configure labels: `self-hosted`, `linux`, `ARM64`, `perf-runner` (project-agnostic; additional per-project labels can be added later)
- [ ] Set up as systemd service for auto-start on boot
- [ ] Install prerequisites: Docker, Docker Compose, .NET 10 SDK, `curl`
- [ ] Authenticate with ghcr.io for image pulls
- [ ] Verify runner appears online in repo Settings → Actions → Runners
- [ ] Document setup steps in `docs/PERF-RUNNER-SETUP.md`

**Commit:**
```bash
git commit -m "docs(perf): add Pi 5 self-hosted runner setup guide

- Installation and systemd service configuration
- Prerequisites and GHCR authentication
- Runner labels for workflow targeting

Refs: #113"
```

---

### Phase 5: Workflow Integration

**Objective:** Update `performance.yml` to support the Pi 5 runner.

**Tasks:**
- [ ] Add `runner` input to `workflow_dispatch`
- [ ] Add `performance-dedicated` job targeting `[self-hosted, perf-runner]`
- [ ] Deploy → seed → test → teardown lifecycle in the job
- [ ] Scheduled runs use Pi 5 for Load + Stress; smoke stays on GitHub-hosted
- [ ] Add `timeout-minutes: 20` to the Pi 5 job
- [ ] Upload reports as artifacts (same as existing job)
- [ ] Add job timeout to existing GitHub-hosted job as well (15 minutes)

**Commit:**
```bash
git commit -m "feat(perf): integrate Pi 5 self-hosted runner into performance workflow

- New performance-dedicated job on [self-hosted, perf-runner]
- Runner choice in workflow_dispatch inputs
- Scheduled runs target Pi 5 for Load/Stress profiles
- Job timeouts for both runner types

Refs: #113"
```

---

### Phase 6: Separate Baselines & Reporting

**Objective:** Maintain distinct baselines for each runner environment.

**Tasks:**
- [ ] Create `baseline-pi5.json` after first successful Pi 5 run
- [ ] Update `BaselineComparer` to accept a `--baseline` path argument (already supported)
- [ ] Workflow selects correct baseline file based on runner
- [ ] Performance summary in PR comment indicates which runner was used
- [ ] Updated thresholds for Pi 5 (may need different values due to ARM64 + real DB)

**Commit:**
```bash
git commit -m "feat(perf): add Pi 5 baseline tracking and runner-aware reporting

- Separate baseline-pi5.json for dedicated runner
- Workflow selects baseline by runner target
- PR comments indicate runner environment

Refs: #113"
```

---

## Testing Strategy

- **Phase 1 validation:** Run all 13 performance tests locally in both modes (set `PERF_TARGET_URL=http://localhost:5099` while running the API locally)
- **Phase 2 validation:** `docker compose -f docker-compose.perf.yml up -d` → health check passes → run tests → teardown
- **Phase 5 validation:** Manual workflow dispatch targeting Pi 5 → verify all artifacts uploaded → baseline comparison runs

## Security Considerations

- The perf-postgres container uses **ephemeral tmpfs storage** — no test data persists after teardown.
- Auth is disabled in the perf environment — this is acceptable because:
  - The container binds to `localhost` only (not exposed externally).
  - The environment is ephemeral (destroyed after each test run).
  - No real user data is involved (deterministic seed data only).
- The self-hosted runner should be configured with minimal repository permissions.
- Docker credentials for ghcr.io should use a scoped PAT with `read:packages` only.

## Performance Considerations

- **tmpfs for PostgreSQL** eliminates disk I/O variability on the Pi 5's SD card / USB storage.
- **Dedicated hardware** ensures no resource contention between load generator and application.
- **Pi 5 8GB (4-core Cortex-A76 @ 2.4GHz)** can comfortably handle the stress profile's 100 req/s while running PostgreSQL — unlike the 2 vCPU GitHub runners. Estimated workload memory is ~1.75GB (OS + runner + .NET + app container + PostgreSQL tmpfs), leaving ample headroom.
- Baselines from the Pi 5 will be **more stable** across runs, enabling tighter regression thresholds over time.

## Open Questions

1. **Database location:** Should PostgreSQL run on the same Pi 5 or a separate device? Same Pi is simpler; separate would isolate DB I/O from app CPU. Start with same Pi, optimise later if needed.
2. **Image version:** Should perf tests always use `:latest` or pin to the commit SHA being tested? SHA pinning is more accurate but requires building the image first.
3. **Network latency:** Since load generator and app are on the same machine, results won't include real network latency. Acceptable trade-off for consistency.
4. **Multi-project port allocation:** When additional projects use this runner, establish a port convention (e.g., BudgetExperiment: 5199, next project: 5299) to avoid conflicts.

## Constraints & Environment Notes

- **Hardware:** Raspberry Pi 5 **8GB** model (~$135). The 16GB variant is unnecessary — total workload memory is well under 4GB. The savings are better spent on active cooling or a USB 3.0 SSD for the OS.
- **Dedicated hardware:** The Pi 5 runs performance tests only — no production applications, no shared workloads. This is a hard constraint, not a suggestion.
- **DNS resolution:** The local network uses a **Ubiquiti Cloud Gateway** for DNS. There is no local DNS server. Tests targeting the Pi should use `localhost` (when load generator and app are co-located) or IP addresses / hostnames resolvable by the Cloud Gateway. DNS resolution latency through the gateway is a known variable but expected to be negligible for local network targets.
- **Multi-project readiness:** The runner is initially set up for BudgetExperiment but should support additional applications in the future. Design decisions (port ranges, compose file naming like `docker-compose.perf.yml`, baseline directories per project, runner labels) should accommodate this without requiring runner reconfiguration.
