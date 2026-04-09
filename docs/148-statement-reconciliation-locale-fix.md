# Feature 148: Fix Bare `.ToString("C")` in Statement Reconciliation UI

> **Status:** Proposed  
> **Severity:** 🔴 Critical — F-001  
> **Audit Source:** `docs/audit/2026-04-09-full-principle-audit.md`

---

## Overview

Seven instances of bare `.ToString("C")` exist in four Statement Reconciliation Razor components. These calls format monetary values without an explicit `IFormatProvider`, so users whose browser locale is not `en-US` see incorrect currency symbols, decimal separators, or grouping characters. The `FormatCurrency()` extension method exists and is used correctly throughout the rest of the codebase — this feature replaces every reconciliation-scoped violation and adds bUnit tests to prevent regression.

This is a **display-layer financial trust issue**. No calculation logic is incorrect, but presenting "$1,234.56" as "¤1,234.56" or "1.234,56 €" in a reconciliation flow directly undermines the user's confidence in their own financial data.

---

## Problem Statement

### Current State

- `ReconciliationBalanceBar.razor` calls `.ToString("C")` on three monetary values (lines 5, 9, 21).
- `ClearableTransactionRow.razor` calls `.ToString("C")` on one monetary value (line 21).
- `ReconciliationHistory.razor` calls `.ToString("C")` on two monetary values (lines 67, 68).
- `ReconciliationDetail.razor` calls `.ToString("C")` on one monetary value (line 54).
- `CultureService.CurrentCulture` is available in Client but was never injected into these components.

### Target State

- All 7 instances replaced with `.FormatCurrency(CultureService.CurrentCulture)`.
- `CultureService` injected into each affected component.
- bUnit tests in `BudgetExperiment.Client.Tests` assert that each component renders the correct currency symbol and format for a non-default locale (e.g., `de-DE`).
- No feature flag needed — this is a pure bug fix with no behavior change for `en-US` users.

---

## User Stories

### US-148-001: Correct Currency Display for Non-US Locales

**As a** user with a European or non-US browser locale  
**I want to** see correctly formatted currency values in the Statement Reconciliation screens  
**So that** I trust the numbers I see match my local currency format and the reconciliation totals are accurate

**Acceptance Criteria:**
- [ ] All 7 bare `.ToString("C")` calls in reconciliation components are replaced with `.FormatCurrency(CultureService.CurrentCulture)`
- [ ] Components inject `CultureService` and pass its `CurrentCulture` to `FormatCurrency()`
- [ ] `de-DE` locale renders "1.234,56 €" (not "$1,234.56" or "¤1,234.56")
- [ ] `en-US` locale continues to render "$1,234.56" (no regression)
- [ ] bUnit tests cover culture-correct rendering for at least one non-US locale

---

## Technical Design

### Affected Files

| File | Location | Instances |
|------|----------|-----------|
| `ReconciliationBalanceBar.razor` | `src/BudgetExperiment.Client/Shared/StatementReconciliation/` | 3 (lines 5, 9, 21) |
| `ClearableTransactionRow.razor` | `src/BudgetExperiment.Client/Shared/StatementReconciliation/` | 1 (line 21) |
| `ReconciliationHistory.razor` | `src/BudgetExperiment.Client/Pages/StatementReconciliation/` | 2 (lines 67, 68) |
| `ReconciliationDetail.razor` | `src/BudgetExperiment.Client/Pages/StatementReconciliation/` | 1 (line 54) |

### Change Pattern

Each affected component needs:

1. `@inject CultureService CultureService` directive added (if not already present).
2. All occurrences of `someDecimal.ToString("C")` replaced with `someDecimal.FormatCurrency(CultureService.CurrentCulture)`.

```razor
@* Before *@
@balance.ToString("C")

@* After *@
@balance.FormatCurrency(CultureService.CurrentCulture)
```

### No API or Domain Changes

This fix is entirely in the Client layer. No DTO changes, no service changes, no migrations.

---

## Implementation Plan

### Phase 1: Replace All Bare `.ToString("C")` Calls

**Tasks:**
- [ ] Open `ReconciliationBalanceBar.razor`; add `@inject CultureService CultureService`; replace 3 instances of `.ToString("C")` with `.FormatCurrency(CultureService.CurrentCulture)`
- [ ] Open `ClearableTransactionRow.razor`; add inject if absent; replace 1 instance
- [ ] Open `ReconciliationHistory.razor`; add inject if absent; replace 2 instances
- [ ] Open `ReconciliationDetail.razor`; add inject if absent; replace 1 instance
- [ ] Run `dotnet build src/BudgetExperiment.Client/BudgetExperiment.Client.csproj` — expect zero errors

**Commit:**
```
fix(client): replace bare ToString("C") in reconciliation components

Replaces 7 instances across 4 files with FormatCurrency(CultureService.CurrentCulture).
Injects CultureService where missing.

Fixes: F-001 (2026-04-09 audit)
Refs: §38 Engineering Guide
```

---

### Phase 2: bUnit Tests

**Tasks:**
- [ ] Create `ReconciliationBalanceBarLocaleTests.cs` in `tests/BudgetExperiment.Client.Tests/Shared/StatementReconciliation/`
  - Set `CultureInfo.CurrentCulture` to `de-DE` in test constructor
  - Render `ReconciliationBalanceBar` with known decimal values
  - Assert rendered HTML contains `"."` as thousands separator and `","` as decimal (European format)
  - Assert rendered HTML does NOT contain `"$"` symbol
- [ ] Create `ClearableTransactionRowLocaleTests.cs`
  - Same locale setup
  - Assert currency format for `de-DE`
- [ ] Create `ReconciliationHistoryLocaleTests.cs`
  - Assert two values format correctly under `de-DE`
- [ ] Create `ReconciliationDetailLocaleTests.cs`
  - Assert one value formats correctly under `de-DE`
- [ ] Add regression tests asserting `en-US` still renders `$` prefix
- [ ] Run `dotnet test tests/BudgetExperiment.Client.Tests/BudgetExperiment.Client.Tests.csproj --filter "Category!=Performance"` — all pass

**Test naming convention:**
```
{Component}_Render_WithDeDeLocale_FormatsDecimalWithEuroSymbol
{Component}_Render_WithEnUsLocale_FormatsDecimalWithDollarSymbol
```

**Commit:**
```
test(client): bUnit locale tests for reconciliation currency formatting

Verifies de-DE and en-US cultures produce correct currency output
in all 4 affected reconciliation components.

Refs: §37, §38 Engineering Guide
```

---

### Phase 3: Verification

**Tasks:**
- [ ] Run full test suite: `dotnet test --filter "Category!=Performance"` — all green
- [ ] Run `dotnet format` — no style violations
- [ ] Manually verify in browser under `de-DE` locale (dev server): reconciliation totals display with `€` and European separators

---

## Testing Strategy

### Unit / Component Tests (bUnit)

- `ReconciliationBalanceBar_Render_WithDeDeLocale_FormatsThreeValuesCorrectly`
- `ReconciliationBalanceBar_Render_WithEnUsLocale_FormatsThreeValuesCorrectly`
- `ClearableTransactionRow_Render_WithDeDeLocale_FormatsAmountCorrectly`
- `ReconciliationHistory_Render_WithDeDeLocale_FormatsLineAmountsCorrectly`
- `ReconciliationDetail_Render_WithDeDeLocale_FormatsAmountCorrectly`

### No Integration Tests Required

This is a pure rendering bug fix. No database, API, or service behavior changes.

---

## Security Considerations

None — this change affects display formatting only.

---

## Performance Considerations

`FormatCurrency()` delegates to `decimal.ToString(format, culture)` — negligible overhead.

---

## References

- [2026-04-09 Full Principle Audit — F-001](../docs/audit/2026-04-09-full-principle-audit.md#f-001-critical--bare-tostringc-in-statement-reconciliation-ui)
- Engineering Guide §38 (Localization — currency formatting rule)
- Engineering Guide §37 (Culture-Sensitive Formatting in Tests)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-04-09 | Initial spec from Vic audit F-001 | Alfred (Lead) |
