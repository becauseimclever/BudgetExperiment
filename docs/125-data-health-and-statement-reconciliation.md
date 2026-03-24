# Feature 125: Data Health & Statement Reconciliation
> **Status:** Planning  
> **Priority:** High  
> **Effort:** Large (2 sub-features, ~12 vertical slices)

## Overview

Users cannot verify their app data matches reality. The existing reconciliation system only covers recurring transactions (~30% of total), leaving the majority of transactions unreconciled. There is no way to input a statement balance, track which transactions have cleared the bank, or detect common data quality issues like duplicates, outliers, or missing import ranges. This feature addresses both sides of the problem: proactive data quality detection (125a) and standard bank statement reconciliation (125b).

## Problem Statement

### Current State

- **Reconciliation is recurring-only.** `ReconciliationMatch` links imported transactions to recurring transaction instances. This covers ~30% of transactions. One-off purchases, irregular income, refunds, and ad-hoc transfers are completely outside the reconciliation workflow.
- **No cleared/uncleared status.** The `Transaction` entity has no `IsCleared` flag or `ClearedDate`. There is no way to mark a transaction as having appeared on a bank statement.
- **No statement balance input.** Users cannot enter the closing balance from their bank statement. There is no way to compare app-computed totals against the bank's figure.
- **No running cleared balance.** The app computes account balance from `InitialBalance + sum(transactions)` but cannot show the sum of only cleared transactions — the standard reconciliation metric.
- **No reconciliation completion or history.** There is no concept of "finishing" a reconciliation period, locking cleared transactions, or storing a `ReconciliationRecord` for audit trail.
- **No data quality detection.** Duplicate transactions (same date, amount, description) are only checked during CSV import. Post-import duplicates, amount outliers, date gaps in import ranges, and orphaned/uncategorized transaction summaries are not surfaced anywhere.
- **User pain is real.** The user reports "a mix of all" data quality issues: missing transactions, wrong amounts, duplicates, wrong dates. Without tooling to detect these, the user must manually cross-reference bank statements line by line.

### Target State

- **125a: Data Health Dashboard** — A proactive analysis page that surfaces data quality issues: duplicate transactions, amount outliers, date gaps per account, and orphaned/uncategorized transaction summaries. Each issue includes inline fix actions (merge, edit, delete).
- **125b: Statement Reconciliation Workflow** — Standard bank reconciliation: mark transactions as cleared, input statement balance, view running cleared balance, see the difference, and complete reconciliation with a locked record. Full history of past reconciliation records per account.

---

## Sub-Feature 125a: Data Health Dashboard

### User Stories

#### US-125a-001: Detect Duplicate Transactions
**As a** user with imported transactions  
**I want** the system to identify likely duplicates (same date, amount, description within an account)  
**So that** I can merge or remove them without manually scanning every transaction

**Acceptance Criteria:**
- [ ] Detects transactions within the same account sharing identical `Date`, `Amount`, and normalized `Description`
- [ ] Also detects near-duplicates: same `Date` and `Amount` with description similarity ≥ 0.85 (Levenshtein or token overlap)
- [ ] Groups duplicates into clusters (2+ transactions per cluster)
- [ ] Each cluster shows all member transactions with their import batch, external reference, and category
- [ ] Excludes transfer pairs (`TransferId` is not null) from duplicate detection
- [ ] Returns results sorted by cluster size descending (worst duplicates first)

#### US-125a-002: Detect Amount Outliers
**As a** user  
**I want** the system to flag transactions with amounts significantly different from the historical pattern for that description/merchant  
**So that** I can spot data entry errors or import corruption quickly

**Acceptance Criteria:**
- [ ] Groups transactions by normalized description (case-insensitive, trimmed)
- [ ] For each group with ≥ 5 historical transactions, computes mean and standard deviation of absolute amounts
- [ ] Flags transactions where `|amount - mean| > 3 × stddev` as outliers
- [ ] Each outlier shows: the transaction, the historical mean, the deviation factor, and the group it belongs to
- [ ] Allows the user to dismiss a flagged outlier (not shown again unless amount changes)
- [ ] Does not flag the first occurrence of a new merchant (no history = no outlier)

#### US-125a-003: Detect Date Gaps
**As a** user who imports bank statements periodically  
**I want** the system to identify date ranges with no transactions per account  
**So that** I can spot missing import periods before they become reconciliation problems

**Acceptance Criteria:**
- [ ] For each account, identifies the earliest and latest transaction dates
- [ ] Scans for gaps > N calendar days with zero transactions (N configurable, default 7)
- [ ] Excludes accounts with < 30 days of transaction history (too new to analyze)
- [ ] Each gap shows: account name, gap start date, gap end date, gap duration in days
- [ ] Sorted by gap duration descending (largest gaps first)

#### US-125a-004: Orphaned & Uncategorized Summary
**As a** user  
**I want** a summary of uncategorized and orphaned transactions (no account, no category)  
**So that** I can prioritize cleanup work

**Acceptance Criteria:**
- [ ] Shows count and total amount of uncategorized transactions per account
- [ ] Shows count and total amount of transactions with null `CategoryId` across all accounts
- [ ] "Orphaned" = transactions whose `AccountId` references a deleted or inactive account (if applicable)
- [ ] Click-through navigates to the filtered transaction list for that group
- [ ] Summary refreshes when the dashboard loads (no caching for MVP)

#### US-125a-005: Inline Fix Actions
**As a** user viewing a data quality issue  
**I want** to fix it directly from the dashboard without navigating away  
**So that** cleanup is fast and frictionless

**Acceptance Criteria:**
- [ ] **Merge duplicates:** Select primary transaction, merge others into it (transfers category, keeps primary's data, deletes others)
- [ ] **Edit amount:** Inline amount editor on outlier transactions, saves via existing transaction update
- [ ] **Correct date:** Inline date editor on flagged transactions
- [ ] **Delete:** Delete a duplicate or erroneous transaction with confirmation dialog
- [ ] After any fix action, the affected issue cluster refreshes automatically
- [ ] Fix actions use existing `TransactionService` update/delete — no new write endpoints needed for MVP

---

## Sub-Feature 125b: Statement Reconciliation Workflow

### User Stories

#### US-125b-001: Mark Transaction as Cleared
**As a** user reconciling a bank statement  
**I want** to mark individual transactions as cleared (appeared on my statement)  
**So that** I can track which transactions the bank has processed

**Acceptance Criteria:**
- [ ] `Transaction` entity gains `IsCleared` (bool, default `false`) and `ClearedDate` (DateOnly?, null when uncleared)
- [ ] Clicking a transaction's checkbox sets `IsCleared = true` and `ClearedDate = today` (user's local date)
- [ ] Unchecking sets `IsCleared = false` and `ClearedDate = null`
- [ ] Cleared state persists across page reloads
- [ ] Clearing a transaction does not affect its category, amount, or other properties
- [ ] Bulk toggle: "Mark all visible as cleared" and "Unmark all visible" buttons

#### US-125b-002: Enter Statement Balance
**As a** user  
**I want** to input my bank statement's closing balance for a specific account and statement date  
**So that** the system can compute the difference between my records and the bank's

**Acceptance Criteria:**
- [ ] Statement balance input: account selector, statement date (DateOnly), closing balance (decimal)
- [ ] Stored per account per statement date (not per reconciliation — user may re-enter before completing)
- [ ] Validates: amount is a valid decimal, statement date is not in the future, account exists
- [ ] Only one active (uncompleted) statement balance per account at a time
- [ ] Previous statement balances are read-only once a reconciliation is completed

#### US-125b-003: Running Cleared Balance
**As a** user  
**I want** to see the sum of all cleared transactions plus the account's initial balance  
**So that** I can compare my cleared total to the statement balance in real time

**Acceptance Criteria:**
- [ ] Cleared balance = `Account.InitialBalance + sum(cleared transactions where Date <= statementDate)`
- [ ] Displayed prominently on the reconciliation page, updates in real time as transactions are cleared/uncleared
- [ ] Difference indicator: `Statement Balance - Cleared Balance` shown with color coding (green = $0.00, red = nonzero)
- [ ] When difference reaches $0.00, show a visual "Balanced!" indicator encouraging completion

#### US-125b-004: Complete Reconciliation
**As a** user who has balanced their account  
**I want** to finalize the reconciliation, locking cleared transactions and creating a permanent record  
**So that** I have an audit trail and previously reconciled transactions don't get accidentally modified

**Acceptance Criteria:**
- [ ] "Complete Reconciliation" button enabled only when difference is $0.00
- [ ] Completing creates a `ReconciliationRecord` with: account ID, statement date, statement balance, cleared balance, transaction count, completed timestamp, user ID
- [ ] All currently cleared transactions for that account are marked as reconciled (new `ReconciliationRecordId` FK on Transaction)
- [ ] Reconciled transactions show a lock icon in the UI; editing amount or date requires explicit "unlock" action with warning
- [ ] Reconciled transactions cannot be deleted without first unlocking
- [ ] Completion confirmation dialog shows summary: N transactions, statement balance, date range

#### US-125b-005: Reconciliation History
**As a** user  
**I want** to view a history of completed reconciliations per account  
**So that** I can audit past statement balances and see when each reconciliation was performed

**Acceptance Criteria:**
- [ ] History list per account: statement date, statement balance, transaction count, completed timestamp
- [ ] Click-through shows all transactions that were part of that reconciliation
- [ ] History is read-only (completed reconciliations cannot be modified)
- [ ] Sorted by statement date descending (most recent first)
- [ ] Supports pagination (page=1, pageSize=20)

#### US-125b-006: Bulk Clear/Unclear Actions
**As a** user with many transactions to reconcile  
**I want** bulk actions to clear or unclear groups of transactions  
**So that** reconciliation is efficient for accounts with high transaction volume

**Acceptance Criteria:**
- [ ] "Mark all cleared" marks all visible (filtered) transactions as cleared
- [ ] "Unmark all" unclears all visible (filtered) transactions that are not yet reconciled (locked)
- [ ] Reconciled (locked) transactions are excluded from bulk unclear
- [ ] Bulk actions update the running cleared balance immediately
- [ ] Confirmation dialog for bulk actions showing count of affected transactions

---

## Technical Design

### Domain Model Changes

#### Transaction Entity (Modified)

```csharp
// src/BudgetExperiment.Domain/Accounts/Transaction.cs — new properties
public bool IsCleared { get; private set; }
public DateOnly? ClearedDate { get; private set; }
public Guid? ReconciliationRecordId { get; private set; }

// New methods
public void MarkCleared(DateOnly clearedDate)
{
    IsCleared = true;
    ClearedDate = clearedDate;
}

public void MarkUncleared()
{
    if (ReconciliationRecordId is not null)
    {
        throw new DomainException("Cannot unclear a reconciled transaction. Unlock it first.",
            DomainExceptionType.InvalidOperation);
    }

    IsCleared = false;
    ClearedDate = null;
}

public void LockToReconciliation(Guid reconciliationRecordId)
{
    if (!IsCleared)
    {
        throw new DomainException("Cannot reconcile an uncleared transaction.",
            DomainExceptionType.InvalidOperation);
    }

    ReconciliationRecordId = reconciliationRecordId;
}

public void UnlockFromReconciliation()
{
    ReconciliationRecordId = null;
}
```

#### ReconciliationRecord (New Aggregate)

```csharp
// src/BudgetExperiment.Domain/Reconciliation/ReconciliationRecord.cs
public sealed class ReconciliationRecord
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public DateOnly StatementDate { get; private set; }
    public MoneyValue StatementBalance { get; private set; }
    public MoneyValue ClearedBalance { get; private set; }
    public int TransactionCount { get; private set; }
    public DateTime CompletedAtUtc { get; private set; }
    public Guid CompletedByUserId { get; private set; }
    public BudgetScope Scope { get; private set; }
    public Guid? OwnerUserId { get; private set; }

    public static ReconciliationRecord Create(
        Guid accountId,
        DateOnly statementDate,
        MoneyValue statementBalance,
        MoneyValue clearedBalance,
        int transactionCount,
        Guid completedByUserId,
        BudgetScope scope,
        Guid? ownerUserId)
    {
        if (statementBalance != clearedBalance)
        {
            throw new DomainException(
                "Cannot complete reconciliation: statement balance does not match cleared balance.",
                DomainExceptionType.ValidationError);
        }

        return new ReconciliationRecord
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId,
            StatementDate = statementDate,
            StatementBalance = statementBalance,
            ClearedBalance = clearedBalance,
            TransactionCount = transactionCount,
            CompletedAtUtc = DateTime.UtcNow,
            CompletedByUserId = completedByUserId,
            Scope = scope,
            OwnerUserId = ownerUserId,
        };
    }
}
```

#### StatementBalance (New Value Object or Entity)

```csharp
// src/BudgetExperiment.Domain/Reconciliation/StatementBalance.cs
public sealed class StatementBalance
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public DateOnly StatementDate { get; private set; }
    public MoneyValue Balance { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static StatementBalance Create(Guid accountId, DateOnly statementDate, MoneyValue balance)
    {
        return new StatementBalance
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId,
            StatementDate = statementDate,
            Balance = balance,
            IsCompleted = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
    }

    public void UpdateBalance(MoneyValue balance)
    {
        if (IsCompleted)
        {
            throw new DomainException(
                "Cannot modify a completed statement balance.",
                DomainExceptionType.InvalidOperation);
        }

        Balance = balance;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkCompleted()
    {
        IsCompleted = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
```

### Repository Interfaces

```csharp
// src/BudgetExperiment.Domain/Repositories/IReconciliationRecordRepository.cs
public interface IReconciliationRecordRepository : IReadRepository<ReconciliationRecord>, IWriteRepository<ReconciliationRecord>
{
    Task<IReadOnlyList<ReconciliationRecord>> GetByAccountAsync(
        Guid accountId, CancellationToken ct);

    Task<ReconciliationRecord?> GetLatestByAccountAsync(
        Guid accountId, CancellationToken ct);
}

// src/BudgetExperiment.Domain/Repositories/IStatementBalanceRepository.cs
public interface IStatementBalanceRepository : IReadRepository<StatementBalance>, IWriteRepository<StatementBalance>
{
    Task<StatementBalance?> GetActiveByAccountAsync(
        Guid accountId, CancellationToken ct);
}
```

### ITransactionRepository Extensions

```csharp
// New methods on existing ITransactionRepository
Task<IReadOnlyList<Transaction>> GetClearedByAccountAsync(
    Guid accountId, DateOnly? upToDate, CancellationToken ct);

Task<MoneyValue> GetClearedBalanceSumAsync(
    Guid accountId, DateOnly? upToDate, CancellationToken ct);

Task<IReadOnlyList<Transaction>> GetByReconciliationRecordAsync(
    Guid reconciliationRecordId, CancellationToken ct);

Task<IReadOnlyList<IGrouping<string, Transaction>>> GetPotentialDuplicatesAsync(
    Guid? accountId, CancellationToken ct);
```

### Service Layer

```csharp
// src/BudgetExperiment.Application/DataHealth/IDataHealthService.cs
public interface IDataHealthService
{
    Task<DataHealthReport> AnalyzeAsync(Guid? accountId, CancellationToken ct);
    Task<IReadOnlyList<DuplicateCluster>> FindDuplicatesAsync(Guid? accountId, CancellationToken ct);
    Task<IReadOnlyList<AmountOutlier>> FindOutliersAsync(Guid? accountId, CancellationToken ct);
    Task<IReadOnlyList<DateGap>> FindDateGapsAsync(Guid? accountId, int minGapDays, CancellationToken ct);
    Task<UncategorizedSummary> GetUncategorizedSummaryAsync(CancellationToken ct);
    Task MergeDuplicatesAsync(Guid primaryTransactionId, IReadOnlyList<Guid> duplicateIds, CancellationToken ct);
    Task DismissOutlierAsync(Guid transactionId, CancellationToken ct);
}

// src/BudgetExperiment.Application/Reconciliation/IStatementReconciliationService.cs
public interface IStatementReconciliationService
{
    Task<TransactionDto> MarkClearedAsync(Guid transactionId, DateOnly clearedDate, CancellationToken ct);
    Task<TransactionDto> MarkUnclearedAsync(Guid transactionId, CancellationToken ct);
    Task<IReadOnlyList<TransactionDto>> BulkMarkClearedAsync(
        IReadOnlyList<Guid> transactionIds, DateOnly clearedDate, CancellationToken ct);
    Task<IReadOnlyList<TransactionDto>> BulkMarkUnclearedAsync(
        IReadOnlyList<Guid> transactionIds, CancellationToken ct);
    Task<StatementBalanceDto> SetStatementBalanceAsync(
        Guid accountId, DateOnly statementDate, decimal balance, CancellationToken ct);
    Task<StatementBalanceDto?> GetActiveStatementBalanceAsync(Guid accountId, CancellationToken ct);
    Task<ClearedBalanceDto> GetClearedBalanceAsync(Guid accountId, DateOnly? upToDate, CancellationToken ct);
    Task<ReconciliationRecordDto> CompleteReconciliationAsync(
        Guid accountId, CancellationToken ct);
    Task<IReadOnlyList<ReconciliationRecordDto>> GetReconciliationHistoryAsync(
        Guid accountId, int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyList<TransactionDto>> GetReconciliationTransactionsAsync(
        Guid reconciliationRecordId, CancellationToken ct);
    Task UnlockTransactionAsync(Guid transactionId, CancellationToken ct);
}
```

### API Endpoints

#### Data Health (125a)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/datahealth/report` | Full data health report (all issue types) |
| GET | `/api/v1/datahealth/report?accountId={id}` | Data health report filtered to one account |
| GET | `/api/v1/datahealth/duplicates` | Duplicate transaction clusters |
| GET | `/api/v1/datahealth/duplicates?accountId={id}` | Duplicates for one account |
| GET | `/api/v1/datahealth/outliers` | Amount outlier transactions |
| GET | `/api/v1/datahealth/outliers?accountId={id}` | Outliers for one account |
| GET | `/api/v1/datahealth/date-gaps` | Date gap analysis per account |
| GET | `/api/v1/datahealth/date-gaps?minGapDays={n}` | Date gaps with custom threshold |
| GET | `/api/v1/datahealth/uncategorized` | Uncategorized/orphaned summary |
| POST | `/api/v1/datahealth/merge-duplicates` | Merge duplicate transactions into primary |
| POST | `/api/v1/datahealth/dismiss-outlier/{transactionId}` | Dismiss an outlier flag |

#### Statement Reconciliation (125b)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/statement-reconciliation/clear` | Mark transaction(s) as cleared |
| POST | `/api/v1/statement-reconciliation/unclear` | Mark transaction(s) as uncleared |
| POST | `/api/v1/statement-reconciliation/bulk-clear` | Bulk mark cleared |
| POST | `/api/v1/statement-reconciliation/bulk-unclear` | Bulk mark uncleared |
| GET | `/api/v1/statement-reconciliation/cleared-balance?accountId={id}&upToDate={date}` | Running cleared balance |
| PUT | `/api/v1/statement-reconciliation/statement-balance` | Set/update statement balance |
| GET | `/api/v1/statement-reconciliation/statement-balance?accountId={id}` | Get active statement balance |
| POST | `/api/v1/statement-reconciliation/complete` | Complete reconciliation (lock + record) |
| GET | `/api/v1/statement-reconciliation/history?accountId={id}` | Reconciliation history |
| GET | `/api/v1/statement-reconciliation/history/{recordId}/transactions` | Transactions in a completed reconciliation |
| POST | `/api/v1/statement-reconciliation/unlock/{transactionId}` | Unlock a reconciled transaction |

### Database Changes

#### New Tables

```sql
-- ReconciliationRecords
CREATE TABLE "ReconciliationRecords" (
    "Id" uuid PRIMARY KEY,
    "AccountId" uuid NOT NULL REFERENCES "Accounts"("Id"),
    "StatementDate" date NOT NULL,
    "StatementBalance_Amount" numeric NOT NULL,
    "StatementBalance_Currency" text NOT NULL,
    "ClearedBalance_Amount" numeric NOT NULL,
    "ClearedBalance_Currency" text NOT NULL,
    "TransactionCount" integer NOT NULL,
    "CompletedAtUtc" timestamp with time zone NOT NULL,
    "CompletedByUserId" uuid NOT NULL,
    "Scope" integer NOT NULL DEFAULT 0,
    "OwnerUserId" uuid
);

-- StatementBalances
CREATE TABLE "StatementBalances" (
    "Id" uuid PRIMARY KEY,
    "AccountId" uuid NOT NULL REFERENCES "Accounts"("Id"),
    "StatementDate" date NOT NULL,
    "Balance_Amount" numeric NOT NULL,
    "Balance_Currency" text NOT NULL,
    "IsCompleted" boolean NOT NULL DEFAULT false,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL
);
```

#### Transaction Table Additions

```sql
ALTER TABLE "Transactions" ADD COLUMN "IsCleared" boolean NOT NULL DEFAULT false;
ALTER TABLE "Transactions" ADD COLUMN "ClearedDate" date;
ALTER TABLE "Transactions" ADD COLUMN "ReconciliationRecordId" uuid REFERENCES "ReconciliationRecords"("Id");

CREATE INDEX "IX_Transactions_IsCleared" ON "Transactions" ("IsCleared") WHERE "IsCleared" = true;
CREATE INDEX "IX_Transactions_ReconciliationRecordId" ON "Transactions" ("ReconciliationRecordId") WHERE "ReconciliationRecordId" IS NOT NULL;
```

### UI/UX Components

#### Data Health Dashboard (125a)

| Component | Route | Purpose |
|-----------|-------|---------|
| `DataHealth.razor` | `/datahealth` | Main dashboard page with tabbed sections |
| `DataHealthViewModel.cs` | — | ViewModel for dashboard state and actions |
| `DuplicateClusterCard.razor` | — | Displays a group of duplicate transactions with merge action |
| `OutlierCard.razor` | — | Displays an outlier transaction with historical context and dismiss/edit |
| `DateGapCard.razor` | — | Displays a date gap with account name and date range |
| `UncategorizedSummaryCard.razor` | — | Summary card with counts and click-through links |
| `InlineAmountEditor.razor` | — | Inline amount editing component |
| `InlineDateEditor.razor` | — | Inline date editing component |

**Page Layout:**
```
┌──────────────────────────────────────────────────┐
│  Data Health Dashboard          [Account Filter ▼]│
├──────────────────────────────────────────────────┤
│  Summary: 3 duplicates · 1 outlier · 2 gaps     │
├──────────────────────────────────────────────────┤
│  [Duplicates] [Outliers] [Date Gaps] [Uncat.]   │
├──────────────────────────────────────────────────┤
│  ┌─ Duplicate Cluster ────────────────────────┐  │
│  │ ☐ 2024-03-15  -$42.50  AMAZON MKTPL  (A)  │  │
│  │ ☐ 2024-03-15  -$42.50  AMAZON MKTPL  (B)  │  │
│  │        [Merge Selected] [Delete Selected]  │  │
│  └────────────────────────────────────────────┘  │
│  ┌─ Duplicate Cluster ────────────────────────┐  │
│  │ ...                                         │  │
│  └────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────┘
```

#### Statement Reconciliation (125b)

| Component | Route | Purpose |
|-----------|-------|---------|
| `StatementReconciliation.razor` | `/statement-reconciliation` | Main reconciliation workspace |
| `StatementReconciliationViewModel.cs` | — | ViewModel for reconciliation state, balance computation, completion |
| `StatementBalanceInput.razor` | — | Statement date + balance input form |
| `ReconciliationBalanceBar.razor` | — | Cleared balance vs statement balance with difference indicator |
| `ClearableTransactionRow.razor` | — | Transaction row with clear/unclear checkbox |
| `ReconciliationHistory.razor` | `/statement-reconciliation/history` | List of completed reconciliation records |
| `ReconciliationHistoryViewModel.cs` | — | ViewModel for history list and detail |
| `ReconciliationDetail.razor` | `/statement-reconciliation/history/{id}` | Transactions within a completed reconciliation |

**Page Layout:**
```
┌──────────────────────────────────────────────────┐
│  Statement Reconciliation     [Account ▼] [Hist.]│
├──────────────────────────────────────────────────┤
│  Statement Date: [2024-03-31]  Balance: [$1,234] │
├──────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────┐ │
│  │ Cleared Balance:     $1,187.50              │ │
│  │ Statement Balance:   $1,234.00              │ │
│  │ Difference:          -$46.50   ● UNBALANCED │ │
│  └─────────────────────────────────────────────┘ │
├──────────────────────────────────────────────────┤
│  [Mark All Cleared] [Unmark All]     Filter: All │
├──────────────────────────────────────────────────┤
│  ☑ 2024-03-01  -$150.00  Rent Payment     ✓ Clr │
│  ☑ 2024-03-03   -$42.50  Grocery Store    ✓ Clr │
│  ☐ 2024-03-15   -$46.50  Gas Station          │
│  ☐ 2024-03-28   -$89.00  Electric Bill        │
│  🔒 2024-02-28 -$200.00  Insurance    Reconciled │
├──────────────────────────────────────────────────┤
│        [Complete Reconciliation] (disabled)       │
└──────────────────────────────────────────────────┘
```

---

## Acceptance Criteria (Testable)

### 125a: Data Health

| ID | Criterion | Test Type |
|----|-----------|-----------|
| AC-125a-01 | `DataHealthService.FindDuplicatesAsync` returns clusters of transactions with identical date + amount + normalized description within the same account | Unit |
| AC-125a-02 | Near-duplicate detection (same date + amount, description similarity ≥ 0.85) groups correctly | Unit |
| AC-125a-03 | Transfer transactions (`TransferId != null`) are excluded from duplicate detection | Unit |
| AC-125a-04 | Outlier detection flags transactions > 3σ from historical mean for same merchant with ≥ 5 history | Unit |
| AC-125a-05 | Outlier detection does not flag merchants with < 5 transactions | Unit |
| AC-125a-06 | Date gap detection finds gaps > N days between consecutive transactions per account | Unit |
| AC-125a-07 | Date gap detection skips accounts with < 30 days of history | Unit |
| AC-125a-08 | Uncategorized summary returns correct counts and totals per account | Unit |
| AC-125a-09 | `MergeDuplicatesAsync` transfers category from deleted transactions to primary, deletes duplicates | Unit |
| AC-125a-10 | `GET /api/v1/datahealth/report` returns all issue types in a single response | Integration |
| AC-125a-11 | `GET /api/v1/datahealth/duplicates?accountId={id}` filters to the specified account | Integration |
| AC-125a-12 | `POST /api/v1/datahealth/merge-duplicates` with invalid primary ID returns 404 | Integration |
| AC-125a-13 | Data health dashboard renders all four tabs with correct counts in summary bar | bUnit |

### 125b: Statement Reconciliation

| ID | Criterion | Test Type |
|----|-----------|-----------|
| AC-125b-01 | `Transaction.MarkCleared(date)` sets `IsCleared = true` and `ClearedDate = date` | Unit |
| AC-125b-02 | `Transaction.MarkUncleared()` on a reconciled transaction throws `DomainException` | Unit |
| AC-125b-03 | `Transaction.MarkUncleared()` on a non-reconciled cleared transaction resets both fields | Unit |
| AC-125b-04 | `ReconciliationRecord.Create()` throws when statement balance ≠ cleared balance | Unit |
| AC-125b-05 | `ReconciliationRecord.Create()` succeeds when balances match, sets all fields correctly | Unit |
| AC-125b-06 | `StatementBalance.UpdateBalance()` on a completed balance throws `DomainException` | Unit |
| AC-125b-07 | `StatementReconciliationService.CompleteReconciliationAsync` creates record and locks all cleared transactions | Unit |
| AC-125b-08 | Cleared balance computation = `InitialBalance + sum(cleared transactions where Date ≤ statementDate)` | Unit |
| AC-125b-09 | Bulk clear marks all specified transactions and returns updated DTOs | Unit |
| AC-125b-10 | Bulk unclear skips reconciled (locked) transactions and returns only affected DTOs | Unit |
| AC-125b-11 | `POST /api/v1/statement-reconciliation/complete` returns 422 when difference ≠ 0 | Integration |
| AC-125b-12 | `POST /api/v1/statement-reconciliation/complete` returns 201 with ReconciliationRecordDto when balanced | Integration |
| AC-125b-13 | `GET /api/v1/statement-reconciliation/history?accountId={id}` returns paginated records sorted by date desc | Integration |
| AC-125b-14 | Locked transactions show lock icon and reject inline edits to amount/date in the UI | bUnit |
| AC-125b-15 | Balance bar updates in real time as transactions are checked/unchecked | bUnit |
| AC-125b-16 | "Complete Reconciliation" button is disabled when difference ≠ $0.00 | bUnit |
| AC-125b-17 | After completion, newly reconciled transactions are excluded from bulk unclear | Integration |

---

## Implementation Plan

Each slice is a vertical cut delivering testable, deployable value from domain through UI.

### Slice 1: Transaction Cleared State (Domain + Persistence)

**Objective:** Add `IsCleared`, `ClearedDate`, and `ReconciliationRecordId` to Transaction entity with domain rules and EF migration.

**Tasks:**
- [ ] Add `IsCleared`, `ClearedDate`, `ReconciliationRecordId` properties to `Transaction`
- [ ] Add `MarkCleared`, `MarkUncleared`, `LockToReconciliation`, `UnlockFromReconciliation` domain methods
- [ ] Write unit tests: AC-125b-01, AC-125b-02, AC-125b-03
- [ ] Add EF configuration for new columns (including filtered indexes)
- [ ] Create migration
- [ ] Add `GetClearedByAccountAsync` and `GetClearedBalanceSumAsync` to `ITransactionRepository`
- [ ] Implement in `TransactionRepository`

**Commit:**
```bash
git commit -m "feat(domain): add cleared state to Transaction entity

- IsCleared, ClearedDate, ReconciliationRecordId properties
- MarkCleared, MarkUncleared, Lock/Unlock domain methods
- Domain rule: cannot unclear a reconciled transaction
- EF migration with filtered indexes
- Repository methods for cleared balance queries

Refs: #125"
```

---

### Slice 2: ReconciliationRecord Aggregate + StatementBalance

**Objective:** Create the new domain types for reconciliation completion and statement balance input.

**Tasks:**
- [ ] Create `ReconciliationRecord` entity with factory method and validation
- [ ] Create `StatementBalance` entity with update/complete lifecycle
- [ ] Write unit tests: AC-125b-04, AC-125b-05, AC-125b-06
- [ ] Create `IReconciliationRecordRepository` and `IStatementBalanceRepository`
- [ ] Add EF configurations and migration
- [ ] Implement repositories

**Commit:**
```bash
git commit -m "feat(domain): ReconciliationRecord and StatementBalance aggregates

- ReconciliationRecord with balance-match validation
- StatementBalance with completed-state protection
- Repository interfaces and EF implementations
- Migration for new tables

Refs: #125"
```

---

### Slice 3: Statement Reconciliation Service (Clear/Unclear)

**Objective:** Application service for marking transactions cleared/uncleared with bulk support.

**Tasks:**
- [ ] Create `IStatementReconciliationService` interface
- [ ] Implement `MarkClearedAsync`, `MarkUnclearedAsync`, `BulkMarkClearedAsync`, `BulkMarkUnclearedAsync`
- [ ] Implement `GetClearedBalanceAsync` (InitialBalance + sum of cleared)
- [ ] Write unit tests: AC-125b-08, AC-125b-09, AC-125b-10
- [ ] Add DTOs to Contracts: `ClearedBalanceDto`, `StatementBalanceDto`

**Commit:**
```bash
git commit -m "feat(app): statement reconciliation service — clear/unclear

- IStatementReconciliationService with mark/bulk operations
- Cleared balance computation from InitialBalance + cleared sum
- Bulk unclear skips reconciled transactions
- Contract DTOs for balance data

Refs: #125"
```

---

### Slice 4: Statement Reconciliation Service (Complete + History)

**Objective:** Reconciliation completion workflow and history queries.

**Tasks:**
- [ ] Implement `SetStatementBalanceAsync`, `GetActiveStatementBalanceAsync`
- [ ] Implement `CompleteReconciliationAsync` — validate balance match, create record, lock transactions
- [ ] Implement `GetReconciliationHistoryAsync`, `GetReconciliationTransactionsAsync`
- [ ] Implement `UnlockTransactionAsync`
- [ ] Write unit tests: AC-125b-07
- [ ] Add DTOs: `ReconciliationRecordDto`

**Commit:**
```bash
git commit -m "feat(app): reconciliation completion and history

- Complete workflow: validate balance, create record, lock transactions
- Statement balance CRUD with completed-state protection
- History queries with pagination
- Unlock reconciled transactions with explicit action

Refs: #125"
```

---

### Slice 5: Statement Reconciliation API

**Objective:** REST endpoints for the full statement reconciliation workflow.

**Tasks:**
- [ ] Create `StatementReconciliationController` with all endpoints from the API table
- [ ] Wire DI registration
- [ ] Write integration tests: AC-125b-11, AC-125b-12, AC-125b-13, AC-125b-17
- [ ] Update `IBudgetApiService` client with all new API calls

**Commit:**
```bash
git commit -m "feat(api): statement reconciliation endpoints

- StatementReconciliationController with clear/unclear/complete/history
- Statement balance CRUD endpoints
- Unlock reconciled transaction endpoint
- Integration tests for completion and history

Refs: #125"
```

---

### Slice 6: Statement Reconciliation UI

**Objective:** Blazor components for the reconciliation workspace.

**Tasks:**
- [ ] Create `StatementReconciliationViewModel` (balance state, clear/unclear, completion)
- [ ] Create `StatementReconciliation.razor` page
- [ ] Create `StatementBalanceInput.razor`, `ReconciliationBalanceBar.razor`, `ClearableTransactionRow.razor`
- [ ] Write ViewModel tests: balance updates on clear, completion button disabled when unbalanced
- [ ] Write bUnit tests: AC-125b-14, AC-125b-15, AC-125b-16
- [ ] Add navigation sidebar link

**Commit:**
```bash
git commit -m "feat(client): statement reconciliation UI

- Reconciliation workspace with balance bar and clearable rows
- Statement balance input form
- Real-time cleared balance vs statement balance
- Complete button enabled only at $0.00 difference
- Lock icon for reconciled transactions

Refs: #125"
```

---

### Slice 7: Reconciliation History UI

**Objective:** History page for completed reconciliation records.

**Tasks:**
- [ ] Create `ReconciliationHistoryViewModel`
- [ ] Create `ReconciliationHistory.razor` page with paginated list
- [ ] Create `ReconciliationDetail.razor` page (transaction list for a completed record)
- [ ] Write ViewModel tests and bUnit tests
- [ ] Link from main reconciliation page

**Commit:**
```bash
git commit -m "feat(client): reconciliation history page

- Paginated list of completed reconciliation records
- Detail view with locked transactions
- Navigation from main reconciliation page

Refs: #125"
```

---

### Slice 8: Data Health Service (Duplicates + Outliers)

**Objective:** Application service for duplicate detection and amount outlier analysis.

**Tasks:**
- [ ] Create `IDataHealthService` interface
- [ ] Implement `FindDuplicatesAsync` — exact and near-duplicate detection (description similarity)
- [ ] Implement `FindOutliersAsync` — mean/stddev computation per merchant, 3σ threshold
- [ ] Write unit tests: AC-125a-01 through AC-125a-05
- [ ] Add DTOs: `DuplicateCluster`, `AmountOutlier`, `DataHealthReport`

**Commit:**
```bash
git commit -m "feat(app): data health service — duplicate and outlier detection

- Exact duplicate clustering (date + amount + description)
- Near-duplicate detection with description similarity
- Amount outlier detection (3σ from merchant mean)
- Transfer transactions excluded from duplicates

Refs: #125"
```

---

### Slice 9: Data Health Service (Date Gaps + Uncategorized)

**Objective:** Date gap analysis and uncategorized/orphaned summary.

**Tasks:**
- [ ] Implement `FindDateGapsAsync` — per-account gap detection with configurable threshold
- [ ] Implement `GetUncategorizedSummaryAsync` — counts and totals per account
- [ ] Implement `AnalyzeAsync` — orchestrates all four analyses into `DataHealthReport`
- [ ] Implement `MergeDuplicatesAsync`, `DismissOutlierAsync`
- [ ] Write unit tests: AC-125a-06 through AC-125a-09

**Commit:**
```bash
git commit -m "feat(app): data health — date gaps, uncategorized, and fix actions

- Date gap detection per account with configurable threshold
- Uncategorized/orphaned transaction summary
- Full DataHealthReport aggregation
- Merge duplicates and dismiss outlier actions

Refs: #125"
```

---

### Slice 10: Data Health API

**Objective:** REST endpoints for data health analysis and fix actions.

**Tasks:**
- [ ] Create `DataHealthController` with all endpoints from the API table
- [ ] Wire DI registration
- [ ] Write integration tests: AC-125a-10, AC-125a-11, AC-125a-12
- [ ] Update `IBudgetApiService` client

**Commit:**
```bash
git commit -m "feat(api): data health endpoints

- DataHealthController with report, duplicates, outliers, date-gaps, uncategorized
- Merge duplicates and dismiss outlier action endpoints
- Integration tests for filtering and error cases

Refs: #125"
```

---

### Slice 11: Data Health Dashboard UI

**Objective:** Blazor page for data health visualization and inline fix actions.

**Tasks:**
- [ ] Create `DataHealthViewModel`
- [ ] Create `DataHealth.razor` page with tabbed layout
- [ ] Create `DuplicateClusterCard.razor`, `OutlierCard.razor`, `DateGapCard.razor`, `UncategorizedSummaryCard.razor`
- [ ] Create `InlineAmountEditor.razor`, `InlineDateEditor.razor`
- [ ] Write ViewModel tests and bUnit tests: AC-125a-13
- [ ] Add navigation sidebar link

**Commit:**
```bash
git commit -m "feat(client): data health dashboard

- Tabbed dashboard with duplicates, outliers, gaps, uncategorized
- Inline fix actions: merge, edit amount, edit date, delete
- Summary bar with issue counts
- Account filter

Refs: #125"
```

---

### Slice 12: Polish & Cross-Feature Integration

**Objective:** Final integration, cross-linking, and polish.

**Tasks:**
- [ ] Add data health issue count badge to sidebar navigation
- [ ] Link duplicate detection results to statement reconciliation (duplicates may cause balance mismatches)
- [ ] Update existing `Reconciliation.razor` page with link to new statement reconciliation
- [ ] OpenAPI spec documentation for all new endpoints
- [ ] XML comments for public API surface
- [ ] Update README feature list if applicable

**Commit:**
```bash
git commit -m "feat: data health and reconciliation polish

- Navigation badges for data health issues
- Cross-links between existing and new reconciliation pages
- OpenAPI documentation for new endpoints

Refs: #125"
```

---

## Out of Scope

- **Automatic bank feed imports** (OFX/Plaid integration) — This feature assumes manual CSV import or manual entry. Automatic bank feeds are a separate future feature.
- **Multi-currency reconciliation** — Reconciliation assumes all transactions within an account use the same currency. Cross-currency matching is deferred.
- **Recurring transaction reconciliation changes** — The existing `ReconciliationMatch` system (Feature ~080) is left as-is. This feature adds statement-level reconciliation alongside it, not replacing it.
- **AI-powered anomaly detection** — The outlier detection uses statistical methods (mean/stddev). ML-based anomaly detection is a future enhancement.
- **Partial reconciliation** — A reconciliation must balance to $0.00 difference before completion. Partial completion (with documented variance) is deferred.
- **Bank statement PDF/image parsing** — Statement balance is entered manually. OCR or PDF parsing is a separate feature.
- **Reconciliation across multiple accounts** — Each reconciliation is per-account. Cross-account reconciliation (e.g., matching transfers between accounts) is deferred.

---

## Open Questions

1. **Cleared balance computation performance** — Should cleared balance be computed on every UI interaction (real-time) or cached with periodic refresh? For accounts with thousands of transactions, a materialized sum may be needed. Recommend starting with real-time computation and adding caching if performance testing reveals issues.

2. **Duplicate detection scope** — Should duplicate detection run across all accounts or only within a single account? Cross-account duplicates (e.g., same transaction imported into two accounts) may indicate an import error. Recommend within-account first, cross-account as a follow-up.

3. **Outlier dismissal persistence** — Where should dismissed outlier flags be stored? Options: (a) a `DismissedOutlier` table, (b) a JSON field on Transaction, (c) an application setting. Recommend (a) for clean separation.

4. **Statement balance date vs. reconciliation period** — Should the reconciliation page use a specific statement date or a month/year period? Banks issue statements on varying cycles (monthly, weekly, custom). Recommend statement date (most flexible) with a date range filter for transactions.

5. **Interaction with existing reconciliation page** — The existing `Reconciliation.razor` page handles recurring transaction matching. Should statement reconciliation be a separate page or integrated into the same page? Recommend separate page (`/statement-reconciliation`) with cross-links, since the workflows are fundamentally different (matching vs. clearing).

6. **ReconciliationRecord immutability** — Once a reconciliation is completed, should it be truly immutable (no edits, no deletes) or should there be an "undo completion" action? Recommend immutable with an "unlock transaction" escape hatch for individual corrections, but no full undo of the record itself.
