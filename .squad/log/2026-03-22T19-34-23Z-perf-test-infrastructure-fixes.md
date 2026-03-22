# Session Log: Performance Test Infrastructure Fixes

**Timestamp:** 2026-03-22T19:34:23Z

**Related PR:** https://github.com/becauseimclever/BudgetExperiment/pull/51

---

## Summary

Three agents (Lucius, Barbara×2) executed coordinated fixes to the performance test infrastructure and CI pipeline.

### Lucius: GitHub Actions Version Fixes
- Fixed broken action versions (`checkout@v6→v4`, `setup-dotnet@v5→v4`, `cache@v5→v4`, `upload-artifact@v7→v4`)
- Impact: Performance CI workflow can now execute
- Commit: 7cec094

### Barbara Round 1: Test Data Accumulation and Classification
- Fixed `TestDataSeeder` to reset database per test (was accumulating data 5× by end of test class)
- Removed racy static `FirstAccountId`; changed `SeedAsync` to return account ID
- Reclassified 2 CategorizationEngine tests out of Performance category (no timing assertions; correctness only)
- Commit: cf62096

### Barbara Round 2: Latency Thresholds and Relative Dates
- Added P99 latency thresholds to all stress/spike tests:
  - `Transactions_StressTest`: P99 < 5000ms
  - `Calendar_StressTest`: P99 < 10000ms
  - `Transactions_SpikeTest`: P99 < 8000ms
- Replaced all hardcoded date literals with `DateTime.UtcNow`-relative expressions in scenarios and seeder
- Impact: Performance regressions now detectable; test datasets remain relevant over time
- Commit: d325b44

---

## Technical Decisions

1. **Seeder Reset Pattern:** EF Core in-memory DB shared across test methods requires explicit reset via `EnsureDeletedAsync()`
2. **Latency Thresholds:** Multipliers of baseline P99 (5×, ~3×, 8×) provide regression detection while tolerating Testcontainers/in-memory variance
3. **Relative Dates:** `DateTime.UtcNow` basis ensures test datasets remain aligned with scenario queries indefinitely

---

## Testing Status
All changes include passing unit and integration tests. Performance test baseline not yet committed (tracked in decisions.md 6.2).

---

## Next Steps (Not in this session)
- Baseline.json must be generated and committed before regression detection is active (see decisions.md)
- E2E performance tests should be gated behind opt-in workflow (see decisions.md 6.8)
