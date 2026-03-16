# BudgetExperiment.Performance.Tests

API performance tests using [NBomber](https://nbomber.com/) for load/stress testing.

## Overview

This project measures HTTP API endpoint performance using NBomber scenarios with
configurable load profiles. Tests run in-process via `WebApplicationFactory` (no
running server required) and produce HTML/JSON reports with latency percentiles.

## Quick Start

```powershell
# Run smoke tests (fast — ~10 seconds per scenario)
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.Performance.Tests\BudgetExperiment.Performance.Tests.csproj --filter "Category=Performance&Category=Smoke"
```

## Load Profiles

| Profile | Users | Duration | Purpose |
|---------|-------|----------|---------|
| **Smoke** | 1 req/s | 10s | Sanity check — zero errors, basic latency |
| **Load** | 10 req/s | 35s | Baseline measurement — p95/p99 thresholds |
| **Stress** | Ramp to 100 req/s | 120s | Find breaking point — error rate only |
| **Spike** | 5 → 50 → 5 req/s | 30s | Burst recovery — error rate < 5% |

## Reports

Reports are generated in `tests/BudgetExperiment.Performance.Tests/reports/` as
HTML and JSON files. Open the HTML reports in a browser to view latency
percentile graphs, throughput, and error breakdowns.

## CI Integration

- **PR checks:** Smoke profile runs automatically (lightweight gate).
- **Weekly schedule:** Full load profile runs on Sunday 03:00 UTC.
- **Manual:** Any profile via workflow dispatch.

Performance tests are excluded from the standard CI test run via the existing
`FullyQualifiedName!~E2E` and `Category!=Performance` filters.

## Endpoints Tested

| Scenario | Endpoint | Priority |
|----------|----------|----------|
| `health_check` | `GET /health` | P3 |
| `get_accounts` | `GET /api/v1/accounts` | P0 |
| `get_transactions` | `GET /api/v1/transactions` | P0 |
| `get_calendar` | `GET /api/v1/calendar/grid` | P0 |
| `get_budgets` | `GET /api/v1/budgets` | P1 |

## Baseline Tracking & Regression Detection

Performance baselines capture known-good metrics (latency percentiles, throughput,
error rate) and compare them against new runs to detect regressions.

### How It Works

1. **Generate a baseline** from a performance run's CSV report.
2. **Commit the baseline JSON** to `baselines/`.
3. **CI compares** each new run against the baseline using `BaselineComparer`.
4. **Regressions** that exceed configured thresholds fail the workflow step.

### BaselineComparer Tool

A standalone .NET console app under `Tools/BaselineComparer/`.

```powershell
# Generate a new baseline from a CSV report
dotnet run --project Tools/BaselineComparer/ -- \
  --report reports/2025-01-01_load_transactions/csv_report.csv \
  --generate baselines/load-baseline.json

# Compare a run against the baseline (CI does this automatically)
dotnet run --project Tools/BaselineComparer/ -- \
  --report reports/2025-01-01_load_transactions/csv_report.csv \
  --baseline baselines/load-baseline.json \
  --thresholds perf-thresholds.json
```

Exit code 1 indicates a regression was detected.

### Regression Thresholds

Configured in `perf-thresholds.json`:

| Threshold | Default | Description |
|-----------|---------|-------------|
| `maxLatencyRegressionPercent` | 15 | Max allowed p50/p95/p99 increase (%) |
| `maxThroughputRegressionPercent` | 10 | Max allowed throughput decrease (%) |
| `maxErrorRateAbsolute` | 1.0 | Max absolute error rate (%) |
| `failOnRegression` | true | Whether regressions fail the CI step |

### Updating the Baseline

After intentional changes that affect performance (new features, schema changes):

1. Run the full load profile locally or trigger the scheduled CI workflow.
2. Review the NBomber HTML report to confirm metrics look correct.
3. Generate a new baseline: `dotnet run --project Tools/BaselineComparer/ -- --report <csv> --generate baselines/load-baseline.json`
4. Commit and push the updated baseline JSON.

See `baselines/README.md` for the JSON schema documentation.
