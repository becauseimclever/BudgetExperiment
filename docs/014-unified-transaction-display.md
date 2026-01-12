# Feature 014: Unified Transaction Display

## Overview

Ensure all financial activities — transactions, recurring transactions, one-time transfers, and recurring transfers — are consistently displayed on both the **Account Transactions List** and the **Calendar** views. Currently, some item types may not be fully visible or may be inconsistently represented across views.

## Current State Analysis

### Data Types in the System

| Type | Description | Example |
|------|-------------|---------|
| **Transaction** | One-time financial entry | Grocery purchase |
| **Recurring Transaction** | Scheduled repeating entry | Monthly Netflix subscription |
| **Transfer** | Paired transactions moving money between accounts | Move $500 from Checking to Savings |
| **Recurring Transfer** | Scheduled repeating transfer between accounts | Monthly savings contribution |

### How They're Stored

- **Transaction**: Single `Transaction` entity with optional `RecurringTransactionId` link
- **Recurring Transaction**: `RecurringTransaction` entity + `RecurringTransactionException` for modifications
- **Transfer**: Two linked `Transaction` entities sharing a `TransferId`, with `TransferDirection` (Source/Destination)
- **Recurring Transfer**: `RecurringTransfer` entity + `RecurringTransferException` for modifications

### Current Display Implementation

#### Calendar View (`Calendar.razor` → `CalendarGridService`)
| Type | Calendar Grid (Totals) | Day Detail (Items) |
|------|----------------------|---------------------|
| Transaction | ✅ Included in `ActualTotal` | ✅ Displayed |
| Recurring Transaction | ✅ Included in `ProjectedTotal` | ✅ Displayed (type="recurring") |
| Transfer | ✅ Included in `ActualTotal` | ✅ Displayed with transfer badge |
| Recurring Transfer | ✅ Included in `ProjectedTotal` | ✅ Displayed (type="recurring-transfer") |

#### Account Transactions List (`AccountTransactions.razor` → `CalendarGridService.GetAccountTransactionListAsync`)
| Type | Listed | Type Field | Transfer Badge |
|------|--------|------------|----------------|
| Transaction | ✅ | "transaction" | ✅ If `IsTransfer` |
| Recurring Transaction | ✅ | "recurring" | ❌ N/A |
| Transfer | ✅ | "transaction" | ✅ |
| Recurring Transfer | ✅ | "recurring-transfer" | ✅ |

### Identified Gaps

1. **Realized Recurring Transfers**: When a recurring transfer is "realized" (converted to actual transactions), are both the source and destination transactions properly linked back?

2. **Deduplication Logic**: Need to verify that realized items don't show twice (once as recurring projection, once as actual transaction)

3. **UI Consistency**: Ensure transfer indicators and recurring indicators are shown consistently across views

4. **Summary Calculations**: Verify that summaries correctly separate:
   - Actual vs. Projected
   - Income vs. Expenses
   - Transfer vs. Non-transfer amounts

---

## Requirements

### REQ-001: All Item Types Visible on Calendar
**As a** user viewing the calendar  
**I want to** see totals that include all financial activities  
**So that** I get an accurate picture of my daily financial position

**Acceptance Criteria:**
- [ ] Daily totals include: transactions, recurring instances, transfers, recurring transfer instances
- [ ] `ActualTotal` = sum of all realized transactions (including realized transfers)
- [ ] `ProjectedTotal` = sum of all unrealized recurring instances (transactions + transfers)
- [ ] Clicking a day shows all items with appropriate type indicators

### REQ-002: All Item Types Visible on Account Transactions
**As a** user viewing an account's transactions  
**I want to** see all transactions, recurring projections, transfers, and recurring transfer projections  
**So that** I understand all money movement for this account

**Acceptance Criteria:**
- [ ] List includes: transactions, recurring instances, realized transfers, recurring transfer instances
- [ ] Each item shows appropriate icon (recurring badge, transfer badge, or both)
- [ ] Summary correctly calculates totals for all item types
- [ ] Items are sorted by date (descending) with proper deduplication

### REQ-003: Proper Deduplication
**As a** user  
**I want to** see each financial event only once  
**So that** I don't get confused by duplicate entries

**Acceptance Criteria:**
- [ ] When a recurring transaction is realized, only show the realized transaction (not the projected instance)
- [ ] When a recurring transfer is realized, only show the realized transfer transactions (not the projected instances)
- [ ] Deduplication works correctly for skipped/modified instances

### REQ-004: Visual Indicators
**As a** user  
**I want to** easily identify the type of each financial entry  
**So that** I understand what I'm looking at

**Acceptance Criteria:**
- [ ] Recurring items show recurring icon (🔄 or similar)
- [ ] Transfer items show transfer icon (↔️ or similar)
- [ ] Recurring transfers show both icons
- [ ] Modified recurring instances show "modified" badge
- [ ] Transfer direction indicated (→ outgoing, ← incoming)

### REQ-005: Correct Summary Calculations
**As a** user viewing summaries  
**I want to** see accurate totals  
**So that** I can trust the numbers

**Acceptance Criteria:**
- [ ] `TotalIncome` = positive amounts (including incoming transfers)
- [ ] `TotalExpenses` = negative amounts (including outgoing transfers)
- [ ] `TotalAmount` = net of all items
- [ ] `TransactionCount` = actual realized items
- [ ] `RecurringCount` = projected/unrealized items
- [ ] `CurrentBalance` = InitialBalance + all realized transactions

---

## Technical Investigation Needed

### Verify Current Behavior

#### Test 1: Calendar Day Detail with Mixed Items
Create test data:
1. Regular transaction on Jan 15
2. Recurring transaction that occurs on Jan 15
3. Transfer (source side) on Jan 15
4. Recurring transfer that occurs on Jan 15

Expected: Day detail shows 4 items with correct types and badges

#### Test 2: Account Transactions List with Mixed Items
Same test data as above, filtered to one account

Expected: List shows all 4 items with correct types, badges, and summary

#### Test 3: Deduplication - Realized Recurring Transaction
1. Create recurring transaction (monthly on 15th)
2. "Realize" it by creating linked transaction with `RecurringTransactionId`

Expected: Only the realized transaction shows, not the projected instance

#### Test 4: Deduplication - Realized Recurring Transfer
1. Create recurring transfer (monthly on 15th)
2. "Realize" it by creating linked transfer transactions with `RecurringTransferId`

Expected: Only the realized transfer transactions show, not the projected instances

---

## Implementation Plan

### Phase 1: Audit & Test Current Behavior
1. Write integration tests for each scenario above
2. Document actual vs. expected behavior
3. Identify specific gaps

### Phase 2: Fix Calendar Grid Service (if needed)
1. Ensure `GetCalendarGridAsync` includes all item types in totals
2. Ensure `GetDayDetailAsync` returns all item types with correct metadata
3. Fix any deduplication bugs

### Phase 3: Fix Account Transaction List Service (if needed)
1. Ensure `GetAccountTransactionListAsync` includes all item types
2. Ensure proper deduplication for all scenarios
3. Ensure summary calculations are correct

### Phase 4: UI Enhancements (if needed)
1. Ensure `TransactionTable` component handles all item types
2. Ensure `DayDetail` component handles all item types
3. Add any missing visual indicators

### Phase 5: Test & Document
1. Run all integration tests
2. Manual testing across scenarios
3. Update feature document with completion status

---

## Files to Review/Modify

### Application Layer
- [CalendarGridService.cs](../src/BudgetExperiment.Application/Services/CalendarGridService.cs)
  - `GetCalendarGridAsync` - calendar totals
  - `GetDayDetailAsync` - day detail items
  - `GetAccountTransactionListAsync` - account list items

### Contracts
- [DayDetailItemDto.cs](../src/BudgetExperiment.Contracts/Dtos/DayDetailItemDto.cs)
- [TransactionListItemDto.cs](../src/BudgetExperiment.Contracts/Dtos/TransactionListItemDto.cs)

### Client Components
- [TransactionTable.razor](../src/BudgetExperiment.Client/Components/Display/TransactionTable.razor)
- [DayDetail.razor](../src/BudgetExperiment.Client/Components/Display/DayDetail.razor) (if exists)
- [Calendar.razor](../src/BudgetExperiment.Client/Pages/Calendar.razor)
- [AccountTransactions.razor](../src/BudgetExperiment.Client/Pages/AccountTransactions.razor)

### Tests
- `CalendarGridServiceTests.cs` - add integration tests for mixed item scenarios

---

## Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              DATABASE                                    │
├─────────────────┬─────────────────┬─────────────────┬───────────────────┤
│   Transaction   │   Recurring     │  Recurring      │   Recurring       │
│                 │   Transaction   │  Transfer       │   Exceptions      │
└────────┬────────┴────────┬────────┴────────┬────────┴─────────┬─────────┘
         │                 │                 │                   │
         ▼                 ▼                 ▼                   ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                     CalendarGridService                                  │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ 1. Fetch transactions for date range                            │   │
│  │ 2. Fetch recurring transactions → project occurrences           │   │
│  │ 3. Fetch recurring transfers → project occurrences              │   │
│  │ 4. Deduplicate: remove projected if realized exists             │   │
│  │ 5. Calculate summaries                                          │   │
│  └─────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────┘
         │                           │                           │
         ▼                           ▼                           ▼
┌─────────────────┐     ┌─────────────────────┐     ┌─────────────────────┐
│ CalendarGridDto │     │   DayDetailDto      │     │ TransactionListDto  │
│ (monthly view)  │     │   (single day)      │     │ (account list)      │
└────────┬────────┘     └──────────┬──────────┘     └──────────┬──────────┘
         │                         │                            │
         ▼                         ▼                            ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                          CLIENT (Blazor)                                 │
│  ┌────────────────┐  ┌────────────────┐  ┌──────────────────────────┐  │
│  │ Calendar.razor │  │ DayDetail      │  │ AccountTransactions.razor│  │
│  │ (CalendarGrid) │  │ Component      │  │ (TransactionTable)       │  │
│  └────────────────┘  └────────────────┘  └──────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Item Type Reference

| Type Field | Source | IsTransfer | IsRecurring | Icon(s) |
|------------|--------|------------|-------------|---------|
| `"transaction"` | Transaction entity | `false` | `false` | — |
| `"transaction"` | Transaction entity (transfer) | `true` | `false` | ↔️ |
| `"transaction"` | Transaction from RecurringTransaction | `false` | `false`* | — |
| `"transaction"` | Transaction from RecurringTransfer | `true` | `false`* | ↔️ |
| `"recurring"` | Projected RecurringTransaction | `false` | `true` | 🔄 |
| `"recurring-transfer"` | Projected RecurringTransfer | `true` | `true` | 🔄↔️ |

*Note: Realized transactions have `RecurringTransactionId` or `RecurringTransferId` set but are displayed as regular transactions since they're now "actual" not "projected"

---

## Success Criteria

1. ✅ All 4 item types visible on calendar day detail
2. ✅ All 4 item types visible on account transactions list
3. ✅ Deduplication works correctly for all scenarios
4. ✅ Visual indicators are consistent and informative
5. ✅ Summary calculations are accurate
6. ✅ Integration tests cover all scenarios
7. ✅ No regressions to existing functionality

---

**Document Version**: 1.1  
**Created**: 2026-01-11  
**Updated**: 2026-01-11  
**Status**: ✅ Completed  
**Author**: Engineering Team
