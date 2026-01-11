# Feature: Recurring Transactions

## Implementation Status

| Phase | Status | Notes |
|-------|--------|-------|
| Phase 1: Domain | ✅ Complete | Entities, value objects, repository interface |
| Phase 2: Infrastructure | ✅ Complete | EF configs, migration, repository impl |
| Phase 3: Application | ✅ Complete | DTOs, service, mapping |
| Phase 4: API | ✅ Complete | Controller with all endpoints |
| Phase 5: Client | ✅ Complete | Page, forms, nav, calendar integration |

**Completed:** 2026-01-10

### Files Created/Modified

**Domain Layer:**
- `RecurrenceFrequency.cs` - Enum (Daily, Weekly, BiWeekly, Monthly, Quarterly, Yearly)
- `RecurrencePattern.cs` - Value object with factory methods and date calculation
- `RecurringTransaction.cs` - Entity with CRUD and projection methods
- `RecurringTransactionException.cs` - Entity for instance modifications
- `ExceptionType.cs` - Enum (Modified, Skipped)
- `IRecurringTransactionRepository.cs` - Repository interface
- `Transaction.cs` - Extended with RecurringTransactionId link

**Infrastructure Layer:**
- `RecurringTransactionConfiguration.cs` - EF Core config
- `RecurringTransactionExceptionConfiguration.cs` - EF Core config
- `TransactionConfiguration.cs` - Updated with FK
- `RecurringTransactionRepository.cs` - Repository implementation
- `20260110070008_AddRecurringTransactions.cs` - Database migration

**Application Layer:**
- `RecurringTransactionDto.cs` - DTOs for create, update, response, instances
- `DomainToDtoMapper.cs` - Extended with recurring transaction mapping
- `RecurringTransactionService.cs` - Full service implementation

**API Layer:**
- `RecurringTransactionsController.cs` - REST controller with all endpoints
- `RecurringTransactionsControllerTests.cs` - Integration tests

**Client Layer:**
- `RecurringTransactionModel.cs` - Client-side models
- `RecurringInstanceModel.cs` - Model for recurring instances on calendar
- `RecurringInstanceModifyModel.cs` - Model for modifying single instances
- `TransactionListItem.cs` - Unified model for transactions and recurring instances
- `IBudgetApiService.cs` - Extended with recurring transaction methods
- `BudgetApiService.cs` - API client implementation
- `RecurringTransactionForm.razor` - Create form component
- `EditRecurringForm.razor` - Edit form component
- `EditInstanceDialog.razor` - Dialog for editing single instance (parameter-based)
- `Recurring.razor` - Main recurring transactions page
- `CalendarDayModel.cs` - Extended with RecurringInstances
- `Calendar.razor` - Extended with recurring instance display and edit/skip actions
- `CalendarDay.razor` - Extended with recurring indicator
- `DayDetail.razor` - Extended with recurring instance display and action buttons
- `AccountTransactions.razor` - Extended to show recurring alongside regular transactions
- `TransactionTable.razor` - Extended with recurring row support, indicators, edit/skip actions
- `NavMenu.razor` - Added navigation link

---

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

#### AccountTransactionsIntegration
- Recurring transactions appear in account transaction lists alongside regular transactions
- Unified `TransactionListItem` model merges both types for display
- Visual indicators:
  - 🔄 emoji before recurring transaction descriptions
  - Blue left border on recurring rows
  - Subtle blue gradient background on recurring rows
  - "modified" badge on instances with custom values
- Summary shows: "Total: $X.XX | Count: N | 🔄 M recurring"
- Recurring instances have Edit/Skip buttons instead of Edit/Delete
- Date range defaults to 1 month past → 1 month future to show upcoming recurring
- Avoids duplicates by checking if a realized transaction exists on the same date with same description

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

### Phase 1: Domain Layer ✅
1. [x] Create `RecurrenceFrequency` enum
2. [x] Create `RecurrencePattern` value object with validation
3. [x] Create `RecurringTransaction` entity with domain logic
4. [x] Create `ExceptionType` enum
5. [x] Create `RecurringTransactionException` entity
6. [x] Add `IRecurringTransactionRepository` interface
7. [x] ~~Add `IRecurringTransactionExceptionRepository` interface~~ (merged into IRecurringTransactionRepository)
8. [x] Unit tests for recurrence date calculation logic
9. [x] Unit tests for exception application logic (merging exceptions with series)

### Phase 2: Infrastructure Layer ✅
1. [x] Add `RecurringTransactionConfiguration` for EF Core
2. [x] Add `RecurringTransactionExceptionConfiguration` for EF Core
3. [x] Create migration for `RecurringTransactions` table
4. [x] Create migration for `RecurringTransactionExceptions` table
5. [x] Add `RecurringTransactionId` column to `Transactions` table
6. [x] Implement `RecurringTransactionRepository`
7. [x] ~~Implement `RecurringTransactionExceptionRepository`~~ (merged into RecurringTransactionRepository)
8. [x] Integration tests for repositories

### Phase 3: Application Layer ✅
1. [x] Create DTOs (request/response including instance DTOs)
2. [x] ~~Create `IRecurringTransactionService` interface~~ (using concrete class)
3. [x] Implement `RecurringTransactionService`
4. [x] Implement instance projection logic (merge series + exceptions)
5. [x] Implement "edit this and future" logic (series update + exception cleanup)
6. [x] Add mapping extensions
7. [x] Unit tests with mocked repository
8. [x] Unit tests for exception merging scenarios

### Phase 4: API Layer ✅
1. [x] Create `RecurringTransactionsController`
2. [x] Add validation
3. [x] Integration tests for endpoints

### Phase 5: Client Layer ✅
1. [x] ~~Create `IRecurringTransactionApi` interface~~ (extended IBudgetApiService)
2. [x] Implement API client methods in BudgetApiService
3. [x] Create `RecurringTransactionsPage` (Recurring.razor)
4. [x] Create `RecurringTransactionForm` component
5. [x] Create `EditRecurringForm` component
6. [x] Add navigation menu item
7. [x] Integrate with calendar view (projected transactions)
8. [x] Add visual indicators for recurring instances on calendar
9. [x] Wire up instance editing from calendar interactions (EditInstanceDialog, skip confirmation)
10. [x] Integrate with account transaction lists (TransactionListItem, TransactionTable updates)
11. [x] Show recurring transactions in AccountTransactions page with edit/skip support

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
