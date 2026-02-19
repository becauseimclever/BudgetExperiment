# Feature 072: Transaction List — Current Balance Calculation Bug
> **Status:** Complete  
> **Priority:** High (bug)  
> **Estimated Effort:** Small (< 1 day)  
> **Dependencies:** None

## Overview

Fix a bug where the "Current Balance" displayed at the top of the account transaction list page is incorrect when the date range does not cover the full transaction history. The calculation ignores all transactions before the selected start date, producing a misleading balance.

## Problem Statement

### Bug: Current Balance Ignores Transactions Before Start Date

The "Current Balance" shown in the summary banner on the `AccountTransactions` page uses an incorrect formula. It sums only the transactions visible in the current date range and adds the account's initial balance, completely ignoring any transactions that occurred before the selected start date.

**Root cause:** In `TransactionListService.cs`, the current balance is calculated as:

```csharp
var totalAmount = sortedItems.Sum(i => i.Amount.Amount);
var currentBalance = account.InitialBalance.Amount + totalAmount;
```

`sortedItems` contains only transactions within the filtered date range (`startDate` to `endDate`). When the user narrows the date range (e.g., views only the current month), all prior transactions are excluded from `totalAmount`, making `currentBalance` wrong.

Meanwhile, the **running balance per row** is already calculated correctly using `BalanceCalculationService.GetBalanceBeforeDateAsync`, which accounts for all transactions prior to `startDate`:

```csharp
var startingBalance = await _balanceCalculationService.GetBalanceBeforeDateAsync(
    startDate, accountId, cancellationToken);

var balanceSeed = startingBalance.Amount;
if (startDate <= account.InitialBalanceDate)
{
    balanceSeed += account.InitialBalance.Amount;
}

var runningBalance = balanceSeed;
foreach (var item in sortedForBalance)
{
    runningBalance += item.Amount.Amount;
    item.RunningBalance = new MoneyDto { Currency = "USD", Amount = runningBalance };
}
```

After the loop, `runningBalance` holds the correct final balance as of the end of the date range, but this value is never used for `CurrentBalance`.

**Example:** Account created with $5,000 initial balance on 2025-01-01. Throughout 2025, various transactions bring the true balance to $3,200. When viewing February 2026 transactions (say, totalling −$500), the current balance shows $5,000 + (−$500) = $4,500 instead of the correct $2,700 ($3,200 − $500).

### Current State

- `CurrentBalance` in the summary is computed as `InitialBalance + sum(items in date range)`, ignoring all transactions before the start date.
- The per-row running balance is correct because it uses `balanceSeed` (which includes pre-range transactions).
- The two values are inconsistent: the last row's running balance does not match the "Current Balance" displayed in the banner.

### Target State

- `CurrentBalance` in the summary equals the final running balance value (i.e., `balanceSeed + sum(items in date range)`), which represents the true account balance as of the end date.
- The "Current Balance" in the banner matches the last transaction row's running balance.
- Behavior is correct regardless of the selected date range.

---

## User Stories

### Current Balance Fix

#### US-072-001: Correct Current Balance Regardless of Date Range
**As a** budget user  
**I want** the "Current Balance" at the top of my transaction list to reflect the true account balance as of the end of the selected date range  
**So that** I can trust the summary figure matches my real account balance.

**Acceptance Criteria:**
- [ ] When the date range covers the full transaction history, the current balance equals `InitialBalance + sum(all transactions)`.
- [ ] When the date range starts after existing transactions, the current balance still includes those earlier transactions in the calculation.
- [ ] The "Current Balance" in the summary matches the running balance of the last (most recent) transaction row.
- [ ] Existing unit tests for running balance remain passing.

---

## Technical Design

### Architecture Changes

No architectural changes required. This is a single-line fix in the Application layer.

### Domain Model

No domain changes.

### API Endpoints

No endpoint changes. The existing `GET /api/v1/accounts/{id}/transactions` response shape is unchanged; only the value of `Summary.CurrentBalance` will be corrected.

### Database Changes

None.

### UI Components

No client-side changes. The `AccountTransactions.razor` page already renders `transactionList.Summary.CurrentBalance` — the fix is server-side only.

---

## Implementation Plan

### Phase 1: Fix Current Balance Calculation

**Objective:** Replace the incorrect `CurrentBalance` formula with the already-computed correct `runningBalance` value.

**Tasks:**
- [ ] Write a failing unit test that verifies `CurrentBalance` accounts for transactions before the start date
- [ ] Fix the calculation in `TransactionListService.cs` — replace `account.InitialBalance.Amount + totalAmount` with the final `runningBalance` value
- [ ] Verify existing tests still pass
- [ ] Add an additional test confirming `CurrentBalance` matches the last row's running balance

**Commit:**
```bash
git add .
git commit -m "fix(application): correct current balance to include pre-range transactions

- CurrentBalance now uses the final running balance instead of
  InitialBalance + sum(visible items only)
- Add unit tests verifying current balance across date range scenarios

Refs: #072"
```

---

## Conventional Commit Reference

| Type | When to Use | SemVer Impact | Example |
|------|-------------|---------------|---------|
| `fix` | Bug fix | Patch | `fix(application): correct current balance calculation` |
| `test` | Adding or fixing tests | None | `test(application): add current balance date range tests` |
