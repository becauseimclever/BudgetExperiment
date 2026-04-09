# Orchestration Log: barbara-test-value-audit

**Timestamp:** 2026-03-23T22:59:11Z  
**Agent:** Barbara (Sonnet, background)  
**Model:** claude-sonnet-4.5

## Task
Comprehensive test quality audit — identify low-value tests and verify service coverage.

## Results

### Low-Value Tests Identified: 68

#### Categories
1. **Framework behavior tests (17):** Tests of EF Core/xUnit behavior, not application logic
   - Example: "can serialize DateTime to JSON"
   - Recommendation: Remove — framework correctness is vendor's responsibility

2. **Mock-only tests (22):** Assert mock calls without exercising real logic
   - Example: "UpdateAsync calls _repository.SaveAsync exactly once"
   - Recommendation: Enhance with real assertions OR archive as design validation

3. **Duplicate/nearly identical tests (18):** Same code path tested multiple ways
   - Example: Two `Validation_InvalidInput_ThrowsException` variants
   - Recommendation: Convert to [Theory] with multiple `[InlineData]` cases

4. **Vanity enum tests (12):** Test `(int)Enum.Value == N` assertions
   - Example: `BudgetScopeTests` — purely compile-time verification
   - Recommendation: Delete entirely (compilation proves correctness)

#### Critical Service Gaps Confirmed
✓ **RecurringTransactionInstanceService** — No tests exist
✓ **UserSettingsService** — No tests exist

**Finding:** These 2 services have business logic but zero coverage. Must be addressed.

### Status
✓ **Complete**

All findings documented in decision history. Gap analysis fed to Lucius for test implementation.
