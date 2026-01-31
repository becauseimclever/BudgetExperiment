# Feature 038: Reconciliation Status Endpoint Parameter Mismatch Bug
> **Status:** Complete

## Overview

Fix a bug where the Reconciliation page fails to load because the client sends incorrect query parameters to the API. The client is sending `startDate` and `endDate` parameters, but the API expects `year` and `month` parameters.

## Problem Statement

When navigating to the Reconciliation page, users see an error message indicating the reconciliation data failed to load. The page cannot display the expected vs actual transactions or any reconciliation status.

### Error Message

```
Failed to load tolerances: ExpectedStartOfValueNotFound, < Path: $ | LineNumber: 0 | BytePositionInLine: 0.
```

This JSON deserialization error occurs because the API returns an HTML error page (starting with `<`) instead of valid JSON. The client's JSON parser fails when it encounters HTML instead of the expected `ReconciliationStatusDto` response.

### Current State

- Client `ReconciliationApiService.GetStatusAsync()` sends query parameters: `?startDate=2026-01-01&endDate=2026-01-31`
- API `ReconciliationController.GetStatusAsync()` expects query parameters: `?year=2026&month=1`
- The mismatch causes the API to fail validation (year and month are 0, which fails the range check)
- Users cannot use the Reconciliation feature at all

### Target State

- Client sends the correct `year` and `month` query parameters to match the API contract
- The Reconciliation page loads successfully and displays status information
- The interface and implementation remain consistent

---

## Technical Design

### Root Cause Analysis

The client service was implemented with a date range interface (`startDate`, `endDate`), but the API was implemented with a simplified month-based interface (`year`, `month`). This disconnect was likely caused by the client being developed based on the original feature spec while the API took a simplified approach.

### Fix Approach

Update the client to match the API contract. The client already uses the selected month/year from the UI, so we'll:

1. Change `IReconciliationApiService.GetStatusAsync()` signature from `(DateOnly startDate, DateOnly endDate, ...)` to `(int year, int month, ...)`
2. Update `ReconciliationApiService.GetStatusAsync()` to send `?year=X&month=Y` instead of date strings
3. Update the Reconciliation page to pass `year` and `month` directly instead of constructing `DateOnly` values

### Affected Files

| File | Change |
|------|--------|
| `src/BudgetExperiment.Client/Services/IReconciliationApiService.cs` | Update interface signature |
| `src/BudgetExperiment.Client/Services/ReconciliationApiService.cs` | Update implementation to use year/month params |
| `src/BudgetExperiment.Client/Pages/Reconciliation.razor` | Update call site to pass year/month |

---

## Implementation Plan

### Phase 1: Fix Client Service Interface and Implementation

**Objective:** Align client service with API contract

**Tasks:**
- [x] Update `IReconciliationApiService.GetStatusAsync()` signature to accept `int year, int month` instead of `DateOnly startDate, DateOnly endDate`
- [x] Update `ReconciliationApiService.GetStatusAsync()` to build URL with `?year=X&month=Y`
- [x] Update `Reconciliation.razor` to pass `selectedYear` and `selectedMonth` directly

**Commit:**
```bash
git add .
git commit -m "fix(client): correct reconciliation status API query parameters

- Change GetStatusAsync to accept year/month instead of date range
- Client now sends ?year=X&month=Y matching API contract
- Resolves reconciliation page load failure

Fixes: #038"
```

---

## Testing Strategy

### Unit Tests

- [x] Verify `ReconciliationApiService` builds correct URL with year/month params

### Manual Testing Checklist

- [ ] Navigate to Reconciliation page
- [ ] Verify page loads without errors
- [ ] Select different months and verify data loads correctly
- [ ] Verify account filter still works (if supported by API)

---

## References

- [Feature 028: Recurring Transaction Reconciliation](./archive/028-recurring-transaction-reconciliation.md)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-25 | Initial draft - bug identified and documented | @copilot |
| 2026-01-26 | Implemented client API alignment fix | @copilot |
| 2026-01-30 | Added unit tests for ReconciliationApiService URL building | @copilot |
