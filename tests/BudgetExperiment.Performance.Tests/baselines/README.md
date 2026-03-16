# Performance Baselines

This directory stores committed performance baselines used for regression detection.

## File Format

`baseline.json` follows this schema:

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
    }
  ]
}
```

## Updating the Baseline

1. Run a full load test suite (scheduled workflow or manual dispatch with "Load" profile).
2. Download the CSV report artifact from the GitHub Actions run.
3. Run the BaselineComparer in generate mode:
   ```powershell
   dotnet run --project tests/BudgetExperiment.Performance.Tests/Tools/BaselineComparer/ -- --generate <report.csv> --output baselines/baseline.json --commit-sha $(git rev-parse HEAD)
   ```
4. Commit and push the updated baseline file.
