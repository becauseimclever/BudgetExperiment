# Decision: Financial Accuracy Audit Framework

**Author:** Alfred (Lead)
**Date:** 2026-07-17
**Status:** Proposed

## Context

The user requires absolute certainty that the system handles finances accurately. This decision codifies the invariants we commit to testing, the precision standard, and test ownership.

## Decisions

### 1. Precision Standard

All monetary arithmetic uses `decimal` exclusively. The `MoneyValue` value object enforces 2-decimal rounding with `MidpointRounding.AwayFromZero` on construction. No `float` or `double` is permitted in any money computation path.

### 2. Committed Invariants

We commit to testing and maintaining these 10 invariants:

| ID | Invariant | Summary |
|---|---|---|
| INV-1 | Account Balance Identity | `Balance = InitialBalance + Σ(Transactions)` |
| INV-2 | Transfer Net-Zero | Source + Destination amounts sum to zero |
| INV-3 | MoneyValue Arithmetic Closure | Addition/subtraction is exact; mixed currency rejected |
| INV-4 | Budget Progress Consistency | Remaining = Target - Spent; thresholds are correct |
| INV-5 | Paycheck Allocation Conservation | `Remaining + Shortfall + TotalPerPaycheck = PaycheckAmount` |
| INV-6 | Per-Bill Calculation Identity | Annual = Amount × Multiplier; PerPaycheck = Annual ÷ Periods |
| INV-7 | Recurring Projection No-Double-Count | Projected + Realized = Expected occurrences |
| INV-8 | Kakeibo Category Assignment | Expense → one bucket; Income/Transfer → null |
| INV-9 | Report Aggregate Consistency | Report totals = sum of category totals = sum of transactions |
| INV-10 | Reconciliation Integrity | Confidence bounds enforced; no many-to-many linking |

### 3. Test Project Ownership

| Test Project | Invariants Owned |
|---|---|
| `BudgetExperiment.Domain.Tests` | INV-1, INV-3, INV-4, INV-5, INV-6, INV-8 (domain), INV-10 |
| `BudgetExperiment.Application.Tests` | INV-2, INV-7, INV-8 (report grouping), INV-9 |
| `BudgetExperiment.Api.Tests` / `Infrastructure.Tests` | Integration-level verification of INV-1, INV-2 end-to-end |

### 4. Accuracy Test Location

All accuracy-focused tests live in an `Accuracy/` folder within their test project, or use the `AccuracyTests` filename suffix.

### 5. Identified Gaps (for Barbara)

Five gaps documented in `docs/ACCURACY-FRAMEWORK.md` Section 6, prioritized P1–P3. Most critical: paycheck conservation law assertion and recurring projection no-double-count cross-cutting test.

## Reference

Full specification: `docs/ACCURACY-FRAMEWORK.md`
