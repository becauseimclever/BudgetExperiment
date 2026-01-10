# Feature: Recurring Transactions

## Overview
Enable users to define recurring transactions (e.g., payday deposits, monthly bills, subscriptions) that automatically generate transaction entries on a schedule.

## User Stories

### US-001: Create Recurring Transaction
**As a** user  
**I want to** define a recurring transaction with a schedule  
**So that** I don't have to manually enter predictable income and expenses

### US-002: View Recurring Transactions
**As a** user  
**I want to** see a list of all my recurring transactions  
**So that** I can manage my regular income and expenses in one place

### US-003: Edit Recurring Transaction
**As a** user  
**I want to** modify an existing recurring transaction  
**So that** I can adjust amounts, schedules, or descriptions when things change

### US-004: Delete Recurring Transaction
**As a** user  
**I want to** remove a recurring transaction  
**So that** I can stop generating entries for cancelled subscriptions or changed circumstances

### US-005: Skip/Pause Recurring Transaction
**As a** user  
**I want to** skip the next occurrence or pause a recurring transaction  
**So that** I can handle exceptions without deleting the entire schedule

### US-006: View Projected Balance with Recurring Transactions
**As a** user  
**I want to** see my projected future balance including scheduled recurring transactions  
**So that** I can plan ahead and avoid overdrafts

### US-007: Edit Single Instance
**As a** user  
**I want to** edit a single occurrence of a recurring transaction (e.g., this month's electric bill was higher)  
**So that** I can reflect reality without changing the entire series

### US-008: Delete Single Instance
**As a** user  
**I want to** skip/delete a single occurrence without affecting the series  
**So that** I can handle one-time exceptions (e.g., paycheck came early, bill was waived)

### US-009: Edit This and Future Instances
**As a** user  
**I want to** edit this occurrence and all future occurrences  
**So that** I can handle permanent changes (e.g., rent increased starting this month)

---

## Domain Model

### New Entity: `RecurringTransaction`

```
RecurringTransaction
├── Id: Guid
├── AccountId: Guid (FK → Account)
├── Description: string
├── Amount: MoneyValue
├── RecurrencePattern: RecurrencePattern (value object)
├── StartDate: DateOnly
├── EndDate: DateOnly? (null = indefinite)
├── NextOccurrence: DateOnly
├── IsActive: bool
├── CreatedAtUtc: DateTime
├── UpdatedAtUtc: DateTime
└── LastGeneratedDate: DateOnly? (tracks last auto-generated transaction)
```

### New Value Object: `RecurrencePattern`

```
RecurrencePattern
├── Frequency: RecurrenceFrequency (enum)
├── Interval: int (e.g., every 2 weeks)
├── DayOfMonth: int? (for monthly: 1-31, use last day if > month length)
├── DayOfWeek: DayOfWeek? (for weekly)
└── MonthOfYear: int? (for yearly: 1-12)
```

### New Enum: `RecurrenceFrequency`

```csharp
public enum RecurrenceFrequency
{
    Daily,
    Weekly,
    BiWeekly,
    Monthly,
    Quarterly,
    Yearly
}
```

### New Entity: `RecurringTransactionException`

Stores modifications to individual instances without changing the series definition.

```
RecurringTransactionException
├── Id: Guid
├── RecurringTransactionId: Guid (FK → RecurringTransaction)
├── OriginalDate: DateOnly (the scheduled date being modified)
├── ExceptionType: ExceptionType (enum)
├── ModifiedAmount: MoneyValue? (null = use series amount)
├── ModifiedDescription: string? (null = use series description)
├── ModifiedDate: DateOnly? (null = use original date, allows rescheduling)
├── CreatedAtUtc: DateTime
└── UpdatedAtUtc: DateTime
```

### New Enum: `ExceptionType`

```csharp
public enum ExceptionType
{
    Modified,  // Instance has custom values (amount, description, or date)
    Skipped    // Instance is excluded from generation
}
```

### Transaction Link

When a recurring transaction generates an actual `Transaction`, link it back:

```
Transaction (existing, extended)
├── ... existing properties ...
├── RecurringTransactionId: Guid? (FK → RecurringTransaction, null for manual transactions)
└── RecurringInstanceDate: DateOnly? (the scheduled date this was generated for)
```

This enables:
- Tracking which transactions came from recurring definitions
- Editing a generated transaction updates/creates an exception automatically
- Re-generating doesn't duplicate already-created transactions

---

## API Design

### Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/recurring-transactions` | List all recurring transactions |
| GET | `/api/v1/recurring-transactions/{id}` | Get single recurring transaction |
| POST | `/api/v1/recurring-transactions` | Create recurring transaction |
| PUT | `/api/v1/recurring-transactions/{id}` | Update recurring transaction |
| DELETE | `/api/v1/recurring-transactions/{id}` | Delete recurring transaction |
| POST | `/api/v1/recurring-transactions/{id}/skip` | Skip next occurrence |
| POST | `/api/v1/recurring-transactions/{id}/pause` | Pause recurring transaction |
| POST | `/api/v1/recurring-transactions/{id}/resume` | Resume paused transaction |
| GET | `/api/v1/recurring-transactions/projected?from={date}&to={date}` | Get projected transactions |
| GET | `/api/v1/recurring-transactions/{id}/instances?from={date}&to={date}` | Get instances with exceptions |
| PUT | `/api/v1/recurring-transactions/{id}/instances/{date}` | Modify single instance |
| DELETE | `/api/v1/recurring-transactions/{id}/instances/{date}` | Skip/delete single instance |
| PUT | `/api/v1/recurring-transactions/{id}/instances/{date}/future` | Edit this and all future |

### Request/Response DTOs

#### CreateRecurringTransactionRequest
```json
{
  "accountId": "guid",
  "description": "Monthly Rent",
  "amount": -1500.00,
  "frequency": "Monthly",
  "interval": 1,
  "dayOfMonth": 1,
  "startDate": "2026-02-01",
  "endDate": null
}
```

#### RecurringTransactionResponse
```json
{
  "id": "guid",
  "accountId": "guid",
  "accountName": "Checking",
  "description": "Monthly Rent",
  "amount": -1500.00,
  "frequency": "Monthly",
  "interval": 1,
  "dayOfMonth": 1,
  "dayOfWeek": null,
  "startDate": "2026-02-01",
  "endDate": null,
  "nextOccurrence": "2026-02-01",
  "isActive": true
}
```

#### ModifyInstanceRequest
```json
{
  "amount": -1650.00,
  "description": "Monthly Rent (increased)",
  "date": "2026-03-01"
}
```
All fields optional - only provided fields override the series defaults.

#### RecurringInstanceResponse
```json
{
  "scheduledDate": "2026-03-01",
  "effectiveDate": "2026-03-01",
  "amount": -1650.00,
  "description": "Monthly Rent (increased)",
  "isModified": true,
  "isSkipped": false,
  "isGenerated": false,
  "generatedTransactionId": null
}
```

#### EditFutureInstancesRequest
```json
{
  "amount": -1600.00,
  "description": "Monthly Rent"
}
```
Updates the series definition and clears future exceptions.

---

## Client UI Design

### Navigation
- Add "Recurring" menu item under existing navigation

### Pages/Components

#### RecurringTransactionsPage
- List view of all recurring transactions
- Columns: Description, Account, Amount, Frequency, Next Due, Status, Actions
- Add button to create new recurring transaction
- Row actions: Edit, Skip, Pause/Resume, Delete

#### RecurringTransactionDialog
- Modal form for create/edit
- Fields:
  - Account (dropdown)
  - Description (text)
  - Amount (currency input, negative for expenses)
  - Frequency (dropdown)
  - Interval (number, default 1)
  - Day of Month/Week (conditional based on frequency)
  - Start Date (date picker)
  - End Date (optional date picker)

#### EditInstanceDialog
- Shown when user edits a projected or generated recurring transaction
- Prompt: "Edit this occurrence, this and future, or all?"
- Options:
  - **This occurrence only** → Creates/updates exception for this date
  - **This and future occurrences** → Updates series definition, clears future exceptions
  - **All occurrences** → Edits the series definition (including past exception cleanup option)
- Fields (pre-filled with current/series values):
  - Amount (currency input)
  - Description (text)
  - Date (date picker, for rescheduling this instance)

#### Instance Indicators in Calendar
- Standard instance: Normal appearance
- Modified instance: Badge or icon indicating custom values
- Skipped instance: Strikethrough or grayed out (optional to show)
- Generated (actual transaction exists): Solid vs dashed border

#### CalendarIntegration
- Show recurring transactions as projected entries in calendar view
- Distinguish visually from actual transactions (e.g., dashed border, different color)

---

## Transaction Generation Strategy

### Option A: On-Demand Generation (Recommended)
- Generate transactions when user views calendar or transaction list
- Store `LastGeneratedDate` to track what's been created
- User confirms or auto-confirms generated transactions

### Option B: Background Job
- Scheduled job runs daily to generate due transactions
- Requires background processing infrastructure

### Recommendation
Start with **Option A** for simplicity. Add background job later if needed.

---

## Implementation Plan (TDD Order)

### Phase 1: Domain Layer
1. [ ] Create `RecurrenceFrequency` enum
2. [ ] Create `RecurrencePattern` value object with validation
3. [ ] Create `RecurringTransaction` entity with domain logic
4. [ ] Create `ExceptionType` enum
5. [ ] Create `RecurringTransactionException` entity
6. [ ] Add `IRecurringTransactionRepository` interface
7. [ ] Add `IRecurringTransactionExceptionRepository` interface
8. [ ] Unit tests for recurrence date calculation logic
9. [ ] Unit tests for exception application logic (merging exceptions with series)

### Phase 2: Infrastructure Layer
1. [ ] Add `RecurringTransactionConfiguration` for EF Core
2. [ ] Add `RecurringTransactionExceptionConfiguration` for EF Core
3. [ ] Create migration for `RecurringTransactions` table
4. [ ] Create migration for `RecurringTransactionExceptions` table
5. [ ] Add `RecurringTransactionId` column to `Transactions` table
6. [ ] Implement `RecurringTransactionRepository`
7. [ ] Implement `RecurringTransactionExceptionRepository`
8. [ ] Integration tests for repositories

### Phase 3: Application Layer
1. [ ] Create DTOs (request/response including instance DTOs)
2. [ ] Create `IRecurringTransactionService` interface
3. [ ] Implement `RecurringTransactionService`
4. [ ] Implement instance projection logic (merge series + exceptions)
5. [ ] Implement "edit this and future" logic (series update + exception cleanup)
6. [ ] Add mapping extensions
7. [ ] Unit tests with mocked repository
8. [ ] Unit tests for exception merging scenarios

### Phase 4: API Layer
1. [ ] Create `RecurringTransactionsController`
2. [ ] Add validation
3. [ ] Integration tests for endpoints

### Phase 5: Client Layer
1. [ ] Create `IRecurringTransactionApi` interface
2. [ ] Implement API client
3. [ ] Create `RecurringTransactionsPage`
4. [ ] Create `RecurringTransactionDialog` component
5. [ ] Create `EditInstanceDialog` component with "this/future/all" prompt
6. [ ] Add navigation menu item
7. [ ] Integrate with calendar view (projected transactions)
8. [ ] Add visual indicators for modified/skipped instances
9. [ ] Wire up instance editing from calendar interactions

---

## Database Schema

```sql
CREATE TABLE "RecurringTransactions" (
    "Id" uuid PRIMARY KEY,
    "AccountId" uuid NOT NULL REFERENCES "Accounts"("Id") ON DELETE CASCADE,
    "Description" varchar(500) NOT NULL,
    "Amount" decimal(18,2) NOT NULL,
    "Frequency" int NOT NULL,
    "Interval" int NOT NULL DEFAULT 1,
    "DayOfMonth" int NULL,
    "DayOfWeek" int NULL,
    "MonthOfYear" int NULL,
    "StartDate" date NOT NULL,
    "EndDate" date NULL,
    "NextOccurrence" date NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "LastGeneratedDate" date NULL,
    "CreatedAtUtc" timestamp NOT NULL,
    "UpdatedAtUtc" timestamp NOT NULL
);

CREATE INDEX "IX_RecurringTransactions_AccountId" ON "RecurringTransactions"("AccountId");
CREATE INDEX "IX_RecurringTransactions_NextOccurrence" ON "RecurringTransactions"("NextOccurrence");

-- Exceptions table for individual instance modifications
CREATE TABLE "RecurringTransactionExceptions" (
    "Id" uuid PRIMARY KEY,
    "RecurringTransactionId" uuid NOT NULL REFERENCES "RecurringTransactions"("Id") ON DELETE CASCADE,
    "OriginalDate" date NOT NULL,
    "ExceptionType" int NOT NULL,
    "ModifiedAmount" decimal(18,2) NULL,
    "ModifiedDescription" varchar(500) NULL,
    "ModifiedDate" date NULL,
    "CreatedAtUtc" timestamp NOT NULL,
    "UpdatedAtUtc" timestamp NOT NULL,
    UNIQUE("RecurringTransactionId", "OriginalDate")
);

CREATE INDEX "IX_RecurringTransactionExceptions_RecurringTransactionId" 
    ON "RecurringTransactionExceptions"("RecurringTransactionId");

-- Link generated transactions back to recurring definition
ALTER TABLE "Transactions"
ADD COLUMN "RecurringTransactionId" uuid NULL REFERENCES "RecurringTransactions"("Id") ON DELETE SET NULL,
ADD COLUMN "RecurringInstanceDate" date NULL;

CREATE INDEX "IX_Transactions_RecurringTransactionId" ON "Transactions"("RecurringTransactionId");
```

---

## Edge Cases & Validation

1. **End of Month Handling**: If `DayOfMonth` = 31 and month has 30 days, use last day of month
2. **Leap Year**: Handle Feb 29 for yearly recurrences
3. **Past Start Date**: Allow start dates in the past (calculate next occurrence from today)
4. **Overlapping Generations**: Prevent duplicate transaction generation
5. **Deleted Account**: Cascade delete recurring transactions when account is deleted
6. **Amount Validation**: Allow both positive (income) and negative (expense) amounts
7. **Exception for Past Date**: Allow creating exceptions for past dates (correcting history)
8. **Exception Conflicts**: One exception per (RecurringTransactionId, OriginalDate) - update if exists
9. **Edit Future Clears Exceptions**: When editing "this and future", delete exceptions >= selected date
10. **Rescheduled Instance**: ModifiedDate moves the instance; OriginalDate tracks which slot it came from
11. **Generated Transaction Edited**: If user edits an already-generated transaction, auto-create exception
12. **Series Deletion**: Deleting series deletes all exceptions (CASCADE) but keeps generated transactions (SET NULL on FK)

---

## Future Enhancements

- [ ] Recurring transaction templates (common patterns like "Biweekly Paycheck")
- [ ] Notifications/reminders for upcoming recurring transactions
- [ ] Auto-categorization of generated transactions
- [ ] Bulk operations (pause all, skip all for date range)
- [ ] Import recurring patterns from transaction history analysis
- [ ] Exception history/audit log (track what was changed when)
- [ ] Undo exception (revert instance to series defaults)
- [ ] Copy exception to future instances (e.g., "apply this change to next 3 months")
