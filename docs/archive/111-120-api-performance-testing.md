# Feature 112: API Performance Testing
> **Status:** Done

## Overview

Establish a repeatable API performance testing infrastructure for the Budget Experiment application. The goal is to catch performance regressions, establish baseline latency/throughput metrics for critical endpoints, and validate that optimizations (e.g., Feature 111 — `AsNoTracking`, concurrent queries) deliver measurable improvements. This first pass focuses exclusively on HTTP API endpoints — no UI or client-side performance testing.

## Problem Statement

### Current State

- **Zero performance tests.** The project has comprehensive unit and integration tests but no way to measure response time, throughput, or memory behavior under load.
- **No baselines.** Without baselines, there is no way to detect regressions or quantify the impact of architectural changes (e.g., Feature 111 optimizations).
- **Hot-path endpoints are untested under concurrency.** Endpoints like `/api/v1/calendar`, `/api/v1/transactions`, and `/api/v1/budgets` are used on nearly every page load but have never been exercised under concurrent user load.
- **Deployment target is a Raspberry Pi.** The production environment is resource-constrained, making performance awareness essential — a 50ms regression that is invisible on a developer workstation can become a 500ms regression on a Pi.

### Target State

- A dedicated performance test project that can be run locally or in CI.
- Baseline metrics (p50, p95, p99 latency; throughput; error rate) for critical API endpoints.
- Configurable load profiles: smoke, load, stress, and spike.
- HTML/JSON reports for historical comparison.
- Clear documentation so any contributor can run performance tests.

---

## Tool Selection: NBomber

### Why NBomber?

After evaluating the tools recommended by [Microsoft's ASP.NET Core load testing documentation](https://learn.microsoft.com/aspnet/core/test/load-tests), **NBomber** is the best fit for this project:

| Criterion | NBomber | k6 | JMeter | Bombardier |
|---|---|---|---|---|
| **Language** | C# (native .NET) | JavaScript | Java/XML | Go CLI |
| **Integration with existing test infra** | Excellent — NuGet package, xUnit compatible, can use `WebApplicationFactory` for in-process testing | Separate JS scripts, no .NET integration | Heavy XML config, separate JVM runtime | CLI only, no programmatic control |
| **Scenario modeling** | Rich scenario/step API with think time, data feeds, assertions | Good scripting but separate ecosystem | GUI-based, complex for code-first teams | Single URL bombardment only |
| **Reporting** | Built-in HTML, JSON, CSV, console reports with percentile breakdowns | CLI + cloud dashboard | GUI reports | CLI output only |
| **CI/CD friendly** | Yes — runs as a dotnet test, exit code on threshold failures | Yes — CLI based | Heavyweight, needs JVM | Yes but limited assertions |
| **Learning curve for .NET devs** | Minimal — write C#, use familiar patterns | Moderate — learn k6 JS API | High — XML, GUI, JVM | Low but limited |
| **In-process testing** | Yes — can create `HttpClient` from `WebApplicationFactory` for zero-network-overhead benchmarks | No — must hit a running server | No | No |

**Decision:** Use **NBomber** as the primary performance testing tool. It is pure .NET, integrates with our existing xUnit infrastructure, supports both in-process (`WebApplicationFactory`) and out-of-process (running API) testing, and generates rich reports.

### NBomber Concepts

- **Scenario**: A named workload definition (e.g., "get_transactions"). Contains one or more steps.
- **Step**: A single operation within a scenario (e.g., send GET request, parse response).
- **Load Simulation**: Controls how virtual users are injected — constant, ramp-up, spike, etc.
- **Assertion / Threshold**: Pass/fail criteria (e.g., p99 < 500ms, error rate < 1%).
- **Report**: Auto-generated HTML/JSON with latency percentiles, throughput, data transfer, and error breakdowns.

---

## Critical Endpoints to Test

Prioritized by traffic frequency and computational complexity:

| Priority | Endpoint | Method | Why |
|----------|----------|--------|-----|
| **P0** | `/api/v1/transactions` | GET | Highest traffic — loaded on every account view. Pagination, filtering. |
| **P0** | `/api/v1/calendar` | GET | Hot path — 9 sequential DB queries (see Feature 111). Most complex read endpoint. |
| **P0** | `/api/v1/accounts` | GET | Loaded on nearly every page. Lightweight but high frequency. |
| **P1** | `/api/v1/budgets` | GET | Budget dashboard — moderate complexity, multiple joins. |
| **P1** | `/api/v1/transactions` | POST | Primary write path. Important for import workflows. |
| **P1** | `/api/v1/recurring-transactions` | GET | Calendar and dashboard dependency. |
| **P2** | `/api/v1/reports` | GET | Aggregation-heavy; potential slow path with large datasets. |
| **P2** | `/api/v1/import` | POST | Bulk operation — relevant for stress testing (large CSV uploads). |
| **P2** | `/api/v1/suggestions` | GET | AI-coupled — variable latency depending on backend. |
| **P3** | `/health` | GET | Baseline smoke test — should always be < 10ms. |

---

## Load Profiles

### Smoke Test
- **Purpose:** Verify the system works under minimal load. Sanity check.
- **Load:** 1 virtual user, 10 seconds duration.
- **Thresholds:** All requests succeed (0% error rate), p99 < 1000ms.

### Load Test
- **Purpose:** Simulate expected production traffic. Establish baselines.
- **Load:** 10–20 concurrent users, 60 seconds duration, 5-second ramp-up.
- **Thresholds:** p95 < 500ms, p99 < 1000ms, error rate < 1%.

### Stress Test
- **Purpose:** Find the breaking point. How many concurrent users before degradation?
- **Load:** Ramp from 10 to 100 users over 120 seconds.
- **Thresholds:** Observe degradation curve — no hard pass/fail, but log where p99 exceeds 2000ms.

### Spike Test
- **Purpose:** Simulate sudden traffic bursts (e.g., all family members open the app simultaneously).
- **Load:** 5 users baseline → spike to 50 users for 10 seconds → back to 5 users.
- **Thresholds:** Recovery time < 10 seconds after spike. Error rate during spike < 5%.

---

## Technical Design

### Project Structure

```
tests/
  BudgetExperiment.Performance.Tests/
    BudgetExperiment.Performance.Tests.csproj
    Scenarios/
      HealthCheckScenario.cs         ← P3: Baseline smoke
      AccountsScenario.cs            ← P0: GET /accounts
      TransactionsScenario.cs        ← P0: GET & POST /transactions
      CalendarScenario.cs            ← P0: GET /calendar
      BudgetsScenario.cs             ← P1: GET /budgets
    Infrastructure/
      PerformanceTestBase.cs         ← Shared setup: HttpClient, auth, config
      TestDataSeeder.cs              ← Seed realistic data volumes for testing
    Profiles/
      SmokeProfile.cs                ← Smoke test load simulation config
      LoadProfile.cs                 ← Standard load test config
      StressProfile.cs               ← Stress test config
      SpikeProfile.cs                ← Spike test config
    nbomber-config.json              ← Optional external config overrides
    README.md                        ← How to run performance tests
```

### NuGet Dependencies

```xml
<PackageReference Include="NBomber" Version="6.*" />
<PackageReference Include="NBomber.Http" Version="6.*" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.*" />
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="xunit.runner.visualstudio" Version="3.*" />
```

### Test Modes

#### 1. In-Process (WebApplicationFactory)

Uses `WebApplicationFactory` to spin up the API in-process. No actual HTTP port needed — `HttpClient` talks directly to the test server. Best for CI, development machines, and isolating API performance from network noise.

```csharp
public class PerformanceTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly HttpClient Client;

    public PerformanceTestBase(CustomWebApplicationFactory factory)
    {
        Client = factory.CreateApiClient();
    }
}
```

**Pros:** Fast, no network overhead, deterministic, works in CI without port binding.  
**Cons:** Doesn't test real HTTP stack (Kestrel, middleware pipeline fully), uses SQLite instead of PostgreSQL.

#### 2. Out-of-Process (Running API)

Hits a real running API instance (local or remote). Tests the full stack including Kestrel, PostgreSQL, and middleware.

```csharp
var baseUrl = Environment.GetEnvironmentVariable("PERF_TEST_URL")
    ?? "http://localhost:5099";
var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
```

**Pros:** Tests the real stack. Can target the Raspberry Pi deployment.  
**Cons:** Requires a running server, results vary with hardware and network.

**Recommendation:** Start with in-process for CI and baseline regression detection. Add out-of-process as a secondary mode for deployment validation.

### Example Scenario: Transactions GET

```csharp
public class TransactionsScenario
{
    public static ScenarioProps Create(HttpClient client)
    {
        return Scenario.Create("get_transactions", async context =>
        {
            var request = Http.CreateRequest("GET", "/api/v1/transactions")
                .WithHeader("Authorization", "Bearer test-token");

            var response = await Http.Send(client, request);

            return response;
        })
        .WithLoadSimulations(
            Simulation.Inject(
                rate: 10,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(60)));
    }
}
```

### Example Test Runner

```csharp
[Trait("Category", "Performance")]
public class LoadTests : PerformanceTestBase
{
    public LoadTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact(Skip = "Run manually — not part of CI by default")]
    public void Transactions_Load_Test()
    {
        var scenario = TransactionsScenario.Create(Client);

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Json)
            .Run();

        // Assert baseline thresholds
        var stats = result.ScenarioStats[0];
        Assert.True(stats.Ok.Request.Percent >= 99, 
            $"Success rate {stats.Ok.Request.Percent}% is below 99% threshold");
        Assert.True(stats.Ok.Latency.Percent95 < 500, 
            $"p95 latency {stats.Ok.Latency.Percent95}ms exceeds 500ms threshold");
    }
}
```

### Authentication in Tests

The existing `CustomWebApplicationFactory` already provides auto-authentication with a test user. Performance tests reuse this infrastructure — no real OIDC tokens needed for in-process testing. For out-of-process tests against a real deployment, a service account token or test user credential would be injected via environment variable.

### Data Seeding

Performance tests need realistic data volumes to be meaningful. A `TestDataSeeder` will create:
- 3–5 accounts
- 500–1000 transactions spread across 6 months
- 10–15 recurring transactions
- 5–10 budget categories with goals
- 3–5 categorization rules

This mirrors a real household's data volume and ensures query performance reflects actual usage patterns.

---

## Implementation Plan

### Phase 1: Project Scaffolding

**Objective:** Create the performance test project, add NBomber dependencies, establish base infrastructure.

**Tasks:**
- [x] Create `tests/BudgetExperiment.Performance.Tests/` project
- [x] Add NuGet references: `NBomber`, `NBomber.Http`, `Microsoft.AspNetCore.Mvc.Testing`
- [x] Add project reference to `BudgetExperiment.Api` (for `WebApplicationFactory`)
- [x] Create `PerformanceTestBase` class reusing `CustomWebApplicationFactory`
- [x] Create `TestDataSeeder` for realistic data volumes
- [x] Add project to solution
- [x] Verify project builds and NBomber initializes

**Commit:**
```
feat(perf): scaffold performance test project with NBomber

- Add BudgetExperiment.Performance.Tests project
- Configure NBomber and NBomber.Http dependencies
- Create PerformanceTestBase with WebApplicationFactory integration
- Add TestDataSeeder for realistic data volumes

Refs: #112
```

### Phase 2: Smoke & Health Check Scenarios

**Objective:** Implement the simplest scenario to validate the pipeline end-to-end.

**Tasks:**
- [x] Create `HealthCheckScenario` targeting `GET /health`
- [x] Create `SmokeProfile` (1 user, 10 seconds)
- [x] Create smoke test runner with basic assertions
- [x] Verify HTML report generation
- [x] Document how to run in README.md

**Commit:**
```
feat(perf): add health check smoke test scenario

- Implement HealthCheckScenario with NBomber
- Add SmokeProfile load simulation
- Verify report generation pipeline
- Add performance test README

Refs: #112
```

### Phase 3: P0 Read Endpoint Scenarios

**Objective:** Cover the three highest-traffic read endpoints.

**Tasks:**
- [x] Create `AccountsScenario` — `GET /api/v1/accounts`
- [x] Create `TransactionsScenario` — `GET /api/v1/transactions`
- [x] Create `CalendarScenario` — `GET /api/v1/calendar`
- [x] Create `LoadProfile` (10–20 users, 60 seconds)
- [x] Add threshold assertions (p95 < 500ms, error rate < 1%)
- [x] Run and capture initial baselines

**Commit:**
```
feat(perf): add P0 read endpoint load test scenarios

- Add Accounts, Transactions, Calendar scenarios
- Configure LoadProfile with 10-20 concurrent users
- Establish p95/p99 latency thresholds
- Capture initial baseline metrics

Refs: #112
```

### Phase 4: Write Path & Stress Scenarios

**Objective:** Test write endpoints and push the system to find breaking points.

**Tasks:**
- [x] Create `TransactionsScenario` write variant — `POST /api/v1/transactions`
- [x] Create `BudgetsScenario` — `GET /api/v1/budgets`
- [x] Create `StressProfile` (ramp 10→100 users over 120 seconds)
- [x] Create `SpikeProfile` (baseline 5, spike to 50, recover)
- [x] Run stress tests, document degradation thresholds

**Commit:**
```
feat(perf): add write path and stress test scenarios

- Add transaction creation load scenario
- Add budgets read scenario
- Implement stress and spike load profiles
- Document degradation thresholds

Refs: #112
```

### Phase 5: CI Workflow & Reporting

**Objective:** Run performance tests automatically on a schedule and on every PR, with reports delivered as artifacts and PR comments.

**Tasks:**
- [x] Add `[Trait("Category", "Performance")]` to all tests
- [x] Ensure performance tests are excluded from default `dotnet test` runs (the existing CI filter already excludes non-unit tests)
- [x] Create dedicated GitHub Actions workflow `.github/workflows/performance.yml` (see design below)
- [x] Configure **scheduled runs** (weekly cron) for full load profile
- [x] Configure **PR-triggered runs** for smoke profile (lightweight gate)
- [x] Upload NBomber HTML/JSON reports as workflow artifacts
- [x] Post a performance summary as a sticky PR comment (latency percentiles + throughput)
- [x] Update project README with CI performance testing instructions

**Commit:**
```
feat(perf): add GitHub Actions performance workflow

- Create performance.yml with scheduled and PR triggers
- Run smoke profile on PRs, full load profile on schedule
- Upload HTML/JSON reports as artifacts
- Post performance summary as sticky PR comment

Refs: #112
```

#### GitHub Actions Workflow Design: `performance.yml`

```yaml
name: Performance Tests

on:
  # Scheduled: full load test suite weekly (Sunday 03:00 UTC)
  schedule:
    - cron: '0 3 * * 0'

  # PR: smoke profile as a lightweight gate
  pull_request:
    branches: [main]

  # Manual: run any profile on-demand
  workflow_dispatch:
    inputs:
      profile:
        description: 'Load profile to run (Smoke, Load, Stress, Spike)'
        required: false
        default: 'Load'
        type: choice
        options: [Smoke, Load, Stress, Spike]

concurrency:
  group: perf-${{ github.ref }}
  cancel-in-progress: true

jobs:
  performance:
    name: Performance Tests
    runs-on: ubuntu-latest
    permissions:
      contents: read
      pull-requests: write
      checks: write
    env:
      ConnectionStrings__AppDb: "Host=localhost;Database=test;Username=test;Password=test"

    steps:
      - name: Checkout repository
        uses: actions/checkout@v6
        with:
          fetch-depth: 0

      - name: Setup .NET
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: '10.0.x'

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj', '**/Directory.Build.props') }}
          restore-keys: |
            ${{ runner.os }}-nuget-

      - name: Restore & Build
        run: |
          dotnet restore BudgetExperiment.sln
          dotnet build BudgetExperiment.sln --configuration Release --no-restore

      # Determine which profile to run
      # PR → Smoke (fast gate), Schedule → Load (full baseline), Manual → user choice
      - name: Determine profile
        id: profile
        run: |
          if [ "${{ github.event_name }}" = "pull_request" ]; then
            echo "filter=Category=Performance&Category=Smoke" >> $GITHUB_OUTPUT
            echo "name=Smoke" >> $GITHUB_OUTPUT
          elif [ "${{ github.event_name }}" = "schedule" ]; then
            echo "filter=Category=Performance" >> $GITHUB_OUTPUT
            echo "name=Load" >> $GITHUB_OUTPUT
          else
            echo "filter=Category=Performance&Category=${{ github.event.inputs.profile || 'Load' }}" >> $GITHUB_OUTPUT
            echo "name=${{ github.event.inputs.profile || 'Load' }}" >> $GITHUB_OUTPUT
          fi

      - name: Run performance tests
        run: |
          dotnet test tests/BudgetExperiment.Performance.Tests/BudgetExperiment.Performance.Tests.csproj \
            --configuration Release \
            --no-build \
            --filter "${{ steps.profile.outputs.filter }}" \
            --logger "trx" \
            --results-directory ./TestResults

      # NBomber writes reports to tests/BudgetExperiment.Performance.Tests/reports/
      - name: Upload performance reports
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: performance-reports-${{ steps.profile.outputs.name }}-${{ github.run_number }}
          path: tests/BudgetExperiment.Performance.Tests/reports/
          retention-days: 90

      # Compare against baseline and generate summary (Phase 6)
      - name: Evaluate baseline regression
        if: always()
        run: |
          REPORT_JSON=$(find tests/BudgetExperiment.Performance.Tests/reports/ -name '*.json' | head -1)
          if [ -f "$REPORT_JSON" ] && [ -f "tests/BudgetExperiment.Performance.Tests/baselines/baseline.json" ]; then
            dotnet run --project tests/BudgetExperiment.Performance.Tests/Tools/BaselineComparer/ \
              -- "$REPORT_JSON" "tests/BudgetExperiment.Performance.Tests/baselines/baseline.json" \
              > perf-summary.md
          elif [ -f "$REPORT_JSON" ]; then
            echo "## Performance Results (No Baseline Yet)" > perf-summary.md
            echo "" >> perf-summary.md
            echo "No baseline file found. Run a scheduled build and commit the baseline to enable regression detection." >> perf-summary.md
            echo "" >> perf-summary.md
            echo "Raw report uploaded as artifact." >> perf-summary.md
          else
            echo "## Performance Results" > perf-summary.md
            echo "No report JSON found." >> perf-summary.md
          fi
          cat perf-summary.md >> $GITHUB_STEP_SUMMARY

      - name: Post performance PR comment
        if: github.event_name == 'pull_request' && always()
        uses: marocchino/sticky-pull-request-comment@v2
        with:
          header: performance
          recreate: true
          path: perf-summary.md

      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: perf-test-results
          path: ./TestResults/*.trx
          retention-days: 7
```

#### Report Delivery

| Trigger | Profile | Report Delivery |
|---------|---------|------------------|
| **Scheduled (weekly)** | Full Load | HTML/JSON artifact (90-day retention), GitHub Step Summary |
| **Pull Request** | Smoke | Sticky PR comment with latency percentiles + pass/fail, artifact |
| **Manual dispatch** | User choice | Artifact + Step Summary |

Scheduled run results are also visible in the Actions tab and can be configured to send email notifications via GitHub's built-in notification settings (Settings → Notifications → Actions → "Send notifications for failed workflows only" or "All workflows"). For richer reporting, a future enhancement could push metrics to a dashboard.

### Phase 6: Baseline Tracking & Quality Gate

**Objective:** Establish a committed performance baseline and fail PRs that regress beyond acceptable thresholds.

**Tasks:**
- [x] Define baseline JSON schema (scenario name, p50/p95/p99 latency, throughput, error rate, timestamp)
- [x] Create `BaselineComparer` tool (simple .NET console app or script) that reads the NBomber JSON report and the baseline file, compares metrics, and outputs a Markdown summary with pass/fail
- [ ] Create initial baseline by running scheduled workflow and committing the JSON output
- [x] Add regression thresholds to a config file (`perf-thresholds.json`)
- [x] Fail the PR workflow step if any metric regresses beyond the configured threshold
- [x] Document the baseline update process

**Commit:**
```
feat(perf): add baseline tracking and quality gate

- Define baseline JSON schema for performance metrics
- Create BaselineComparer tool for regression detection
- Add perf-thresholds.json with configurable regression limits
- Fail PR checks when performance regresses beyond threshold

Refs: #112
```

#### Baseline JSON Schema

```json
{
  "generatedAt": "2026-03-15T03:00:00Z",
  "commitSha": "abc1234",
  "scenarios": [
    {
      "name": "get_transactions",
      "p50Ms": 12.5,
      "p95Ms": 45.2,
      "p99Ms": 89.1,
      "throughputRps": 850.0,
      "errorPercent": 0.0
    },
    {
      "name": "get_calendar",
      "p50Ms": 28.3,
      "p95Ms": 95.7,
      "p99Ms": 180.4,
      "throughputRps": 320.0,
      "errorPercent": 0.0
    }
  ]
}
```

#### Regression Thresholds (`perf-thresholds.json`)

```json
{
  "maxLatencyRegressionPercent": 15,
  "maxThroughputRegressionPercent": 10,
  "maxErrorRateAbsolute": 1.0,
  "failOnRegression": true
}
```

- **Latency regression:** If any scenario's p95 or p99 increases by more than 15% vs. baseline → fail.
- **Throughput regression:** If any scenario's RPS drops by more than 10% vs. baseline → fail.
- **Error rate:** If any scenario's error rate exceeds 1% absolute → fail regardless of baseline.
- **`failOnRegression`:** Set to `false` to run in advisory mode (report-only) while establishing initial baselines.

#### Baseline Update Process

1. A scheduled (weekly) run produces a new NBomber JSON report.
2. If the run is clean (all thresholds pass), the maintainer can update the baseline:
   ```powershell
   # Download the latest report artifact from the Actions run
   # Copy to baselines/ and commit
   cp downloaded-report.json tests/BudgetExperiment.Performance.Tests/baselines/baseline.json
   git add tests/BudgetExperiment.Performance.Tests/baselines/baseline.json
   git commit -m "perf: update performance baseline from run #<run-number>"
   ```
3. Alternatively, a future enhancement could auto-update the baseline on `main` pushes when all gates pass.

#### Quality Gate Flow

```
PR Opened
  │
  ▼
Run Smoke Performance Tests (in-process, ~30 seconds)
  │
  ▼
NBomber generates JSON report
  │
  ▼
BaselineComparer reads report + baselines/baseline.json
  │
  ├─ No baseline file → advisory mode, post "no baseline" comment, pass
  │
  ├─ All metrics within threshold → post ✅ summary comment, pass
  │
  └─ Regression detected → post ❌ summary comment with details, fail step
       │
       └─ PR author reviews: fix regression or justify + update baseline
```

#### Example PR Comment Output

```markdown
## ⚡ Performance Report — Smoke Profile

| Scenario | p95 (ms) | Baseline p95 | Δ | Status |
|----------|----------|-------------|---|--------|
| get_transactions | 42.1 | 45.2 | -6.9% | ✅ |
| get_calendar | 110.5 | 95.7 | +15.5% | ❌ +15.5% exceeds 15% threshold |
| get_accounts | 8.2 | 9.1 | -9.9% | ✅ |
| health_check | 1.1 | 1.0 | +10.0% | ✅ |

**Result: FAIL** — 1 scenario exceeds regression threshold.

Baseline: `abc1234` (2026-03-08) | Report: artifact #42
```

---

## Running Performance Tests

### Quick Start (In-Process)

```powershell
# Run smoke tests only
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.Performance.Tests\BudgetExperiment.Performance.Tests.csproj --filter "Category=Performance&Category=Smoke"

# Run full load test suite
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.Performance.Tests\BudgetExperiment.Performance.Tests.csproj --filter "Category=Performance"
```

### Against a Running API (Out-of-Process)

```powershell
# Start the API
dotnet run --project c:\ws\BudgetExperiment\src\BudgetExperiment.Api\BudgetExperiment.Api.csproj --configuration Release

# In another terminal, run performance tests against it
$env:PERF_TEST_URL = "http://localhost:5099"
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.Performance.Tests\BudgetExperiment.Performance.Tests.csproj --filter "Category=Performance"
```

### Against Raspberry Pi Deployment

```powershell
$env:PERF_TEST_URL = "https://budget.becauseimclever.com"
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.Performance.Tests\BudgetExperiment.Performance.Tests.csproj --filter "Category=Performance&Category=Smoke"
```

### View Reports

Reports are generated in `tests/BudgetExperiment.Performance.Tests/reports/` as HTML files. Open in a browser to view latency percentiles, throughput graphs, and error breakdowns.

---

## Success Criteria

- [x] Performance test project builds and runs without errors
- [x] Health check smoke test passes (p99 < 100ms, 0% errors)
- [x] Baseline metrics captured for all P0 endpoints
- [x] HTML reports generated with latency percentile breakdowns
- [x] Tests are excluded from normal `dotnet test` runs (opt-in via trait filter)
- [x] README documents how to run each test profile
- [x] Stress test identifies the concurrency level where p99 exceeds 2000ms
- [x] GitHub Actions workflow runs performance tests on a weekly schedule
- [x] Smoke profile runs automatically on every PR to `main`
- [x] Performance summary posted as a sticky PR comment
- [x] HTML/JSON reports uploaded as workflow artifacts with 90-day retention
- [x] Baseline JSON file committed and used for regression comparison
- [x] PRs that regress beyond configured thresholds (15% latency, 10% throughput) fail the check
- [x] Quality gate runs in advisory mode (pass-through) when no baseline exists yet

---

## Future Considerations (Out of Scope for This Feature)

- **BenchmarkDotNet microbenchmarks:** For specific hot methods (e.g., mapping, serialization) — complements NBomber's HTTP-level testing.
- **Database-specific benchmarks:** Isolate EF Core query performance from HTTP overhead.
- **Client-side performance:** Blazor WebAssembly load time, render performance, bundle size tracking.
- **Comparison testing:** Run the same scenarios before/after Feature 111 optimizations to quantify gains.
- **k6 as an alternative:** If scenarios grow complex enough to warrant k6's scripting flexibility or Grafana Cloud integration.
- **Auto-update baselines:** Automatically commit updated baseline on `main` when all gates pass.
- **Metrics dashboard:** Push performance metrics to a time-series dashboard (e.g., Grafana) for trend visualization.
- **Out-of-process CI testing:** Run performance tests against the staging/demo deployment (requires coordinating with deployment workflow).

---

## References

- [Microsoft: ASP.NET Core Load/Stress Testing](https://learn.microsoft.com/aspnet/core/test/load-tests)
- [Microsoft: ASP.NET Core Best Practices — Performance](https://learn.microsoft.com/aspnet/core/fundamentals/best-practices)
- [NBomber Documentation](https://nbomber.com/docs/getting-started/overview)
- [NBomber.Http Plugin](https://nbomber.com/docs/nbomber/http)
- [Feature 111: Pragmatic Performance Optimizations](111-pragmatic-performance-optimizations.md) — the optimizations this testing will validate
