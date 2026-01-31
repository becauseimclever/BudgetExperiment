# Feature 057: Calendar Initial Balance Bug Fix
> **Status:** 🗒️ Planning

## Overview

Fix a bug where the calendar view does not correctly reflect an account's initial balance. The running balance calculations in the calendar are incorrect when the initial balance date falls on or after the calendar grid's start date.

## Problem Statement

The calendar grid's running balance (end-of-day balance) does not include the account's initial balance under certain conditions, leading to incorrect balance displays that confuse users and undermine trust in the financial overview.

### Current State

- `BalanceCalculationService.GetBalanceBeforeDateAsync` uses `InitialBalanceDate < date` (strictly less than) when determining which accounts' initial balances to include
- The calendar grid start date is calculated as the Sunday before the first of the month
- If an account's `InitialBalanceDate` equals the grid start date, the initial balance is excluded from the running balance calculation
- If an account's `InitialBalanceDate` falls within the visible calendar grid, the initial balance may not be reflected at the correct point
- Related: Doc 041 exists for E2E testing this scenario but the underlying bug prevents correct behavior

### Target State

- The initial balance is correctly incorporated into the running balance on the calendar
- When viewing a month where an account was created, the running balance reflects the initial balance from the `InitialBalanceDate` onward
- The end-of-day balance on the `InitialBalanceDate` includes the initial balance plus any transactions on that day
- E2E tests validate this behavior (see doc 041)

---

## User Stories

### Calendar Balance Accuracy

#### US-057-001: Initial balance reflected on start date
**As a** user  
**I want to** see my account's initial balance reflected in the calendar starting from my initial balance date  
**So that** the running balance accurately represents my actual account balance

**Acceptance Criteria:**
- [ ] When filtering by a specific account, the initial balance is included on or after `InitialBalanceDate`
- [ ] The end-of-day balance on `InitialBalanceDate` equals initial balance + transactions on that day
- [ ] Days before `InitialBalanceDate` show $0 balance (or are appropriately indicated)

#### US-057-002: All accounts aggregated correctly
**As a** user  
**I want to** see the correct aggregated balance across all accounts on the calendar  
**So that** I have an accurate overview of my total financial position

**Acceptance Criteria:**
- [ ] When viewing "All Accounts", each account's initial balance is included from its respective start date
- [ ] Running balance correctly accumulates across all accounts

---

## Technical Design

### Root Cause Analysis

The bug is in `BalanceCalculationService.GetBalanceBeforeDateAsync`:

```csharp
// Current (buggy) logic:
var initialBalanceSum = accounts
    .Where(a => a.InitialBalanceDate < date)  // Excludes accounts starting ON the date
    .Sum(a => a.InitialBalance.Amount);
```

This excludes the initial balance when querying for a date equal to `InitialBalanceDate`. For the calendar:
- Grid start date is the Sunday before the 1st of the month
- If account's `InitialBalanceDate == gridStartDate`, the initial balance is excluded

### Proposed Fix

The `GetBalanceBeforeDateAsync` method name implies "balance before date" (exclusive), which is semantically correct. The issue is how the calendar uses this:

**Option A: Adjust calendar logic**
- The calendar should call `GetBalanceBeforeDateAsync` with the day BEFORE the grid starts for the "prior balance"
- Then the initial balance on days within the grid should be handled separately

**Option B: Handle initial balance in running balance calculation**
- When building grid days, if a day equals an account's `InitialBalanceDate`, include the initial balance in that day's calculations
- This requires passing account info to the grid builder

**Option C: Create a new method `GetBalanceAsOfDateAsync` (inclusive)**
- A companion method that includes the initial balance when `InitialBalanceDate <= date`
- Calendar uses this for the grid start date

**Recommended: Option A + refinement of existing logic**
- The current architecture should be audited to ensure:
  1. Days before `InitialBalanceDate` correctly show zero/no balance for that account
  2. The `InitialBalanceDate` day includes the initial balance
  3. Running balance accumulates correctly from there

### Architecture Changes

- Minor modifications to `BalanceCalculationService` or `CalendarGridService`
- No new services required

### Domain Model

- No changes to domain entities

### API Endpoints

- No endpoint changes; existing `/api/v1/calendar/grid` returns corrected data

### Database Changes

- No database changes

### UI Components

- No UI component changes; the calendar already displays the data returned from the API

---

## Implementation Plan

### Phase 1: Write failing unit tests (TDD)

**Objective:** Create unit tests that expose the bug

**Tasks:**
- [ ] Add test: Account initial balance date equals grid start date → initial balance should be included from that day
- [ ] Add test: Account initial balance date is within grid range → balance appears on correct day
- [ ] Add test: Multiple accounts with different start dates → running balance is correct
- [ ] Verify existing tests still define expected behavior

**Commit:**
```bash
git add .
git commit -m "test(calendar): add failing tests for initial balance edge cases

- Test initial balance date == grid start date
- Test initial balance within visible grid range
- Test multi-account aggregation with different start dates

Refs: #057"
```

### Phase 2: Fix BalanceCalculationService

**Objective:** Correct the initial balance inclusion logic

**Tasks:**
- [ ] Analyze if `GetBalanceBeforeDateAsync` semantic should change or a new method is needed
- [ ] Implement fix ensuring initial balance is included correctly
- [ ] Ensure the fix handles "balance before" vs "balance as of" semantics correctly
- [ ] All unit tests pass

**Commit:**
```bash
git add .
git commit -m "fix(accounts): correct initial balance calculation for balance queries

- Ensure initial balance is included when InitialBalanceDate matches query logic
- Handle edge case where account starts on calendar grid start date

Refs: #057"
```

### Phase 3: Update CalendarGridService if needed

**Objective:** Ensure calendar running balances use correct balance calculation

**Tasks:**
- [ ] Verify `CalendarGridService` correctly applies starting balance
- [ ] Ensure days before an account's `InitialBalanceDate` don't include that account's balance
- [ ] Integration tests pass

**Commit:**
```bash
git add .
git commit -m "fix(calendar): ensure running balance reflects initial balance correctly

- Grid days correctly incorporate initial balance from InitialBalanceDate
- Running balance accumulates accurately across calendar grid

Refs: #057"
```

### Phase 4: Manual testing and validation

**Objective:** Validate fix works in real scenario

**Tasks:**
- [ ] Create test account with `InitialBalanceDate` equal to a month's first Sunday
- [ ] Verify calendar shows correct running balance
- [ ] Test with multiple accounts having different start dates
- [ ] Document any edge cases found

**Commit:**
```bash
git add .
git commit -m "docs(calendar): document initial balance calendar behavior

Refs: #057"
```

---

## Testing Scenarios

| Scenario | Initial Balance Date | Grid Start | Expected Behavior |
|----------|---------------------|------------|-------------------|
| Account starts before grid | Jan 1 | Dec 29 | Initial balance included in Dec 29 starting balance |
| Account starts on grid start | Dec 29 | Dec 29 | Initial balance appears starting Dec 29 |
| Account starts within grid | Jan 5 | Dec 29 | Dec 29-Jan 4 show $0, Jan 5+ includes initial balance |
| Account starts after grid | Feb 1 | Dec 29 | Entire grid shows $0 for that account |

---

## Related Documents

- [041: Validate Starting Balance in Calendar (E2E)](041-validate-starting-balance-calendar-e2e.md) - E2E test coverage for this feature

---

## Notes

- This bug may have been masked when accounts have `InitialBalanceDate` well before typical calendar viewing
- Edge case is most visible when creating new accounts or viewing historical months near account creation
