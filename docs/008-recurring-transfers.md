# Feature: Recurring Transfers

## Overview
Enable users to define recurring transfers between accounts (e.g., monthly savings transfer, bi-weekly investment contribution) that automatically schedule paired transactions on a defined schedule. This feature combines the concepts from [004-recurring-transactions.md](004-recurring-transactions.md) and [005-account-transfers.md](005-account-transfers.md).

## User Stories

### US-001: Create Recurring Transfer
**As a** user  
**I want to** define a recurring transfer between two accounts with a schedule  
**So that** I don't have to manually create transfers for predictable money movement

### US-002: View Recurring Transfers
**As a** user  
**I want to** see a list of all my recurring transfers  
**So that** I can manage my scheduled money movements in one place

### US-003: Edit Recurring Transfer
**As a** user  
**I want to** modify an existing recurring transfer (amount, schedule, accounts)  
**So that** I can adjust when things change (e.g., increase savings contribution)

### US-004: Delete Recurring Transfer
**As a** user  
**I want to** remove a recurring transfer  
**So that** I can stop scheduled transfers that are no longer needed

### US-005: Skip/Pause Recurring Transfer
**As a** user  
**I want to** skip the next occurrence or pause a recurring transfer  
**So that** I can handle exceptions (e.g., tight month, skip savings transfer)

### US-006: View Projected Transfers
**As a** user  
**I want to** see projected future transfers on my calendar and account views  
**So that** I can plan ahead and understand future money movement

### US-007: Edit Single Transfer Instance
**As a** user  
**I want to** edit a single occurrence of a recurring transfer (e.g., transfer extra this month)  
**So that** I can handle one-time variations without changing the entire series

### US-008: Skip Single Transfer Instance
**As a** user  
**I want to** skip a single occurrence without affecting the series  
**So that** I can handle one-time exceptions

### US-009: Edit This and Future Transfer Instances
**As a** user  
**I want to** edit this occurrence and all future occurrences  
**So that** I can handle permanent changes (e.g., increased monthly savings starting now)

---

## Domain Model

### New Entity: `RecurringTransfer`

```
RecurringTransfer
├── Id: Guid
├── SourceAccountId: Guid (FK → Account)
├── DestinationAccountId: Guid (FK → Account)
├── Description: string
├── Amount: MoneyValue (always positive, direction determined by accounts)
├── RecurrencePattern: RecurrencePattern (reuse existing value object)
├── StartDate: DateOnly
├── EndDate: DateOnly? (null = indefinite)
├── NextOccurrence: DateOnly
├── IsActive: bool
├── CreatedAtUtc: DateTime
├── UpdatedAtUtc: DateTime
└── LastGeneratedDate: DateOnly? (tracks last auto-generated transfer)
```

### New Entity: `RecurringTransferException`

Stores modifications to individual transfer instances without changing the series definition.

```
RecurringTransferException
├── Id: Guid
├── RecurringTransferId: Guid (FK → RecurringTransfer)
├── OriginalDate: DateOnly (the scheduled date being modified)
├── ExceptionType: ExceptionType (reuse existing enum: Modified, Skipped)
├── ModifiedAmount: MoneyValue? (null = use series amount)
├── ModifiedDescription: string? (null = use series description)
├── ModifiedDate: DateOnly? (null = use original date, allows rescheduling)
├── CreatedAtUtc: DateTime
└── UpdatedAtUtc: DateTime
```

### Transaction Link Extension

When a recurring transfer generates actual transactions, link them back:

```
Transaction (extend existing)
├── ... existing properties ...
├── RecurringTransferId: Guid? (FK → RecurringTransfer, null for non-recurring transfers)
└── RecurringTransferInstanceDate: DateOnly? (the scheduled date this was generated for)
```

This enables:
- Tracking which transfer transactions came from recurring definitions
- Editing a generated transfer updates/creates an exception automatically
- Re-generating doesn't duplicate already-created transfers

### Validation Rules

1. **Source ≠ Destination**: Cannot create recurring transfer to the same account
2. **Positive Amount**: Transfer amount must be > 0
3. **Valid Accounts**: Both accounts must exist
4. **Valid Date Range**: EndDate (if provided) must be >= StartDate
5. **Valid Recurrence**: Pattern must be valid for the frequency type

---

## API Design

### Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/recurring-transfers` | List all recurring transfers |
| GET | `/api/v1/recurring-transfers/{id}` | Get single recurring transfer |
| POST | `/api/v1/recurring-transfers` | Create recurring transfer |
| PUT | `/api/v1/recurring-transfers/{id}` | Update recurring transfer |
| DELETE | `/api/v1/recurring-transfers/{id}` | Delete recurring transfer |
| POST | `/api/v1/recurring-transfers/{id}/skip` | Skip next occurrence |
| POST | `/api/v1/recurring-transfers/{id}/pause` | Pause recurring transfer |
| POST | `/api/v1/recurring-transfers/{id}/resume` | Resume paused transfer |
| GET | `/api/v1/recurring-transfers/projected?from={date}&to={date}` | Get projected transfers |
| GET | `/api/v1/recurring-transfers/{id}/instances?from={date}&to={date}` | Get instances with exceptions |
| PUT | `/api/v1/recurring-transfers/{id}/instances/{date}` | Modify single instance |
| DELETE | `/api/v1/recurring-transfers/{id}/instances/{date}` | Skip/delete single instance |
| PUT | `/api/v1/recurring-transfers/{id}/instances/{date}/future` | Edit this and all future |

### Request/Response DTOs

#### CreateRecurringTransferRequest
```json
{
  "sourceAccountId": "guid",
  "destinationAccountId": "guid",
  "description": "Monthly Savings",
  "amount": 500.00,
  "frequency": "Monthly",
  "interval": 1,
  "dayOfMonth": 1,
  "startDate": "2026-02-01",
  "endDate": null
}
```

#### RecurringTransferResponse
```json
{
  "id": "guid",
  "sourceAccountId": "guid",
  "sourceAccountName": "Checking",
  "destinationAccountId": "guid",
  "destinationAccountName": "Savings",
  "description": "Monthly Savings",
  "amount": 500.00,
  "frequency": "Monthly",
  "interval": 1,
  "dayOfMonth": 1,
  "dayOfWeek": null,
  "startDate": "2026-02-01",
  "endDate": null,
  "nextOccurrence": "2026-02-01",
  "isActive": true,
  "createdAtUtc": "2026-01-10T10:00:00Z"
}
```

#### ModifyTransferInstanceRequest
```json
{
  "amount": 750.00,
  "description": "Monthly Savings (extra this month)",
  "date": "2026-03-01"
}
```
All fields optional - only provided fields override the series defaults.

#### RecurringTransferInstanceResponse
```json
{
  "scheduledDate": "2026-03-01",
  "effectiveDate": "2026-03-01",
  "amount": 750.00,
  "description": "Monthly Savings (extra this month)",
  "sourceAccountId": "guid",
  "sourceAccountName": "Checking",
  "destinationAccountId": "guid",
  "destinationAccountName": "Savings",
  "isModified": true,
  "isSkipped": false,
  "isGenerated": false,
  "sourceTransactionId": null,
  "destinationTransactionId": null
}
```

#### EditFutureTransferInstancesRequest
```json
{
  "amount": 600.00,
  "description": "Monthly Savings (increased)"
}
```
Updates the series definition and clears future exceptions.

### Filtering & Pagination
- `GET /api/v1/recurring-transfers?sourceAccountId={guid}` - Filter by source account
- `GET /api/v1/recurring-transfers?destinationAccountId={guid}` - Filter by destination account
- `GET /api/v1/recurring-transfers?accountId={guid}` - Filter by either account (source or destination)
- `GET /api/v1/recurring-transfers?isActive=true` - Filter by active status
- Standard pagination: `?page=1&pageSize=20`

---

## Client UI Design

### Navigation
- Add "Recurring Transfers" as a sub-item under "Recurring" or as separate menu item
- Or combine with existing recurring page with tabs for "Transactions" and "Transfers"

### Pages/Components

#### RecurringTransfersPage
- List view of all recurring transfers
- Columns: Description, From Account, To Account, Amount, Frequency, Next Due, Status, Actions
- Add button to create new recurring transfer
- Row actions: Edit, Skip, Pause/Resume, Delete

#### RecurringTransferDialog
```
┌─────────────────────────────────────────┐
│ Create Recurring Transfer               │
├─────────────────────────────────────────┤
│                                         │
│ From Account:  [Checking ▼]             │
│                                         │
│ To Account:    [Savings ▼]              │
│                                         │
│ Amount:        [$500.00___]             │
│                                         │
│ Description:   [Monthly Savings____]    │
│                                         │
│ ─── Schedule ───────────────────────── │
│                                         │
│ Frequency:     [Monthly ▼]              │
│                                         │
│ Day of Month:  [1 ▼]                    │
│                                         │
│ Start Date:    [02/01/2026]             │
│                                         │
│ End Date:      [________] (optional)    │
│                                         │
├─────────────────────────────────────────┤
│            [Cancel]    [Create]         │
└─────────────────────────────────────────┘
```

#### EditTransferInstanceDialog
- Shown when user edits a projected or generated recurring transfer instance
- Prompt: "Edit this occurrence, this and future, or all?"
- Options:
  - **This occurrence only** → Creates/updates exception for this date
  - **This and future occurrences** → Updates series definition, clears future exceptions
  - **All occurrences** → Edits the series definition
- Fields (pre-filled with current/series values):
  - Amount (currency input)
  - Description (text)
  - Date (date picker, for rescheduling this instance)

### Calendar Integration
- Show recurring transfers as projected entries in calendar view
- Distinguish visually from one-time transfers (e.g., dashed border, recurring icon)
- Show both source (outgoing) and destination (incoming) in respective account views
- Visual indicator: ↔️🔄 combining transfer and recurring icons

### Account Transactions Integration
- Recurring transfer instances appear alongside regular transactions in account views
- Visual indicators:
  - Transfer icon (↔️) + recurring icon (🔄)
  - Shows "Recurring transfer to/from [Account Name]"
  - Different styling for projected vs generated instances

### Transfer Quick Actions
- From existing Transfer dialog, add "Make this recurring" checkbox
- From recurring transfer list, add "Execute now" action to generate immediate instance

---

## Transfer Generation Logic

When a recurring transfer is due (or projected):

```csharp
public TransferPair GenerateTransferPair(
    RecurringTransfer recurringTransfer,
    DateOnly instanceDate,
    RecurringTransferException? exception = null)
{
    var transferId = Guid.NewGuid();
    var effectiveAmount = exception?.ModifiedAmount ?? recurringTransfer.Amount;
    var effectiveDescription = exception?.ModifiedDescription ?? recurringTransfer.Description;
    var effectiveDate = exception?.ModifiedDate ?? instanceDate;
    
    // Source transaction (money leaving source account)
    var sourceTransaction = Transaction.Create(
        accountId: recurringTransfer.SourceAccountId,
        amount: new MoneyValue(-effectiveAmount.Value),
        date: effectiveDate,
        description: $"Transfer to {destinationAccountName}: {effectiveDescription}",
        transferId: transferId,
        transferDirection: TransferDirection.Source,
        recurringTransferId: recurringTransfer.Id,
        recurringTransferInstanceDate: instanceDate
    );
    
    // Destination transaction (money entering destination account)
    var destinationTransaction = Transaction.Create(
        accountId: recurringTransfer.DestinationAccountId,
        amount: effectiveAmount,
        date: effectiveDate,
        description: $"Transfer from {sourceAccountName}: {effectiveDescription}",
        transferId: transferId,
        transferDirection: TransferDirection.Destination,
        recurringTransferId: recurringTransfer.Id,
        recurringTransferInstanceDate: instanceDate
    );
    
    return new TransferPair(sourceTransaction, destinationTransaction);
}
```

---

## Implementation Plan (TDD Order)

### Phase 1: Domain Layer ✅ COMPLETED (2026-01-11)
1. [x] Create `RecurringTransfer` entity with factory method and validation
2. [x] Create `RecurringTransferException` entity
3. [x] Create `IRecurringTransferRepository` interface
4. [x] Add `RecurringTransferId` and `RecurringTransferInstanceDate` to `Transaction` entity
5. [x] Add `CreateFromRecurringTransfer()` factory method to `Transaction`
6. [x] Unit tests for entity creation and validation (24 + 15 + 4 = 43 new tests)

**Files Created/Modified:**
- `src/BudgetExperiment.Domain/RecurringTransfer.cs` (new)
- `src/BudgetExperiment.Domain/RecurringTransferException.cs` (new)
- `src/BudgetExperiment.Domain/IRecurringTransferRepository.cs` (new)
- `src/BudgetExperiment.Domain/Transaction.cs` (extended with RecurringTransferId properties)
- `tests/BudgetExperiment.Domain.Tests/RecurringTransferTests.cs` (new - 24 tests)
- `tests/BudgetExperiment.Domain.Tests/RecurringTransferExceptionTests.cs` (new - 15 tests)
- `tests/BudgetExperiment.Domain.Tests/TransactionTests.cs` (extended - 4 new tests)

**All 184 domain tests passing.**

### Phase 2: Infrastructure Layer ⬅️ START HERE
1. [ ] Create `RecurringTransferConfiguration` EF Core config
2. [ ] Create `RecurringTransferExceptionConfiguration` EF Core config
3. [ ] Update `TransactionConfiguration` for new FK columns
4. [ ] Create migration for new tables and columns
5. [ ] Implement `RecurringTransferRepository`
6. [ ] Integration tests for repository

### Phase 3: Application Layer
1. [ ] Create DTOs (CreateRecurringTransferRequest, RecurringTransferResponse, etc.)
2. [ ] Create `IRecurringTransferService` interface
3. [ ] Implement `RecurringTransferService` with CRUD operations
4. [ ] Implement instance projection logic with exception handling
5. [ ] Implement skip/pause/resume functionality
6. [ ] Implement instance modification (single, future, all)
7. [ ] Add mapping extensions
8. [ ] Unit tests with mocked repository

### Phase 4: API Layer
1. [ ] Create `RecurringTransfersController`
2. [ ] Add request validation
3. [ ] Integration tests for all endpoints

### Phase 5: Client Layer
1. [ ] Create `RecurringTransferModel` client-side model
2. [ ] Create `RecurringTransferInstanceModel` for calendar/list display
3. [ ] Extend `IBudgetApiService` with recurring transfer methods
4. [ ] Implement API client methods in `BudgetApiService`
5. [ ] Create `RecurringTransferForm.razor` component
6. [ ] Create `EditRecurringTransferForm.razor` component
7. [ ] Create `EditTransferInstanceDialog.razor` component
8. [ ] Create `RecurringTransfers.razor` page (or tab on existing Recurring page)
9. [ ] Extend calendar components to show recurring transfer instances
10. [ ] Extend account transaction views to show recurring transfer instances
11. [ ] Add navigation link
12. [ ] Component tests where appropriate

---

## Database Changes

### Migration: Add Recurring Transfers

```sql
-- RecurringTransfers table
CREATE TABLE "RecurringTransfers" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "SourceAccountId" uuid NOT NULL,
    "DestinationAccountId" uuid NOT NULL,
    "Description" character varying(500) NOT NULL,
    "Amount" numeric NOT NULL,
    "Frequency" integer NOT NULL,
    "Interval" integer NOT NULL,
    "DayOfMonth" integer NULL,
    "DayOfWeek" integer NULL,
    "MonthOfYear" integer NULL,
    "StartDate" date NOT NULL,
    "EndDate" date NULL,
    "NextOccurrence" date NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "LastGeneratedDate" date NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "FK_RecurringTransfers_Accounts_Source" FOREIGN KEY ("SourceAccountId") REFERENCES "Accounts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_RecurringTransfers_Accounts_Destination" FOREIGN KEY ("DestinationAccountId") REFERENCES "Accounts" ("Id") ON DELETE CASCADE
);

-- RecurringTransferExceptions table
CREATE TABLE "RecurringTransferExceptions" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "RecurringTransferId" uuid NOT NULL,
    "OriginalDate" date NOT NULL,
    "ExceptionType" integer NOT NULL,
    "ModifiedAmount" numeric NULL,
    "ModifiedDescription" character varying(500) NULL,
    "ModifiedDate" date NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "FK_RecurringTransferExceptions_RecurringTransfers" FOREIGN KEY ("RecurringTransferId") REFERENCES "RecurringTransfers" ("Id") ON DELETE CASCADE
);

-- Extend Transactions table
ALTER TABLE "Transactions"
ADD COLUMN "RecurringTransferId" uuid NULL,
ADD COLUMN "RecurringTransferInstanceDate" date NULL;

-- Indexes
CREATE INDEX "IX_RecurringTransfers_SourceAccountId" ON "RecurringTransfers"("SourceAccountId");
CREATE INDEX "IX_RecurringTransfers_DestinationAccountId" ON "RecurringTransfers"("DestinationAccountId");
CREATE INDEX "IX_RecurringTransfers_NextOccurrence" ON "RecurringTransfers"("NextOccurrence");
CREATE INDEX "IX_RecurringTransferExceptions_RecurringTransferId_OriginalDate" 
    ON "RecurringTransferExceptions"("RecurringTransferId", "OriginalDate");
CREATE INDEX "IX_Transactions_RecurringTransferId" ON "Transactions"("RecurringTransferId");

-- Foreign key for Transactions
ALTER TABLE "Transactions"
ADD CONSTRAINT "FK_Transactions_RecurringTransfers" 
    FOREIGN KEY ("RecurringTransferId") REFERENCES "RecurringTransfers" ("Id") ON DELETE SET NULL;
```

---

## Edge Cases

1. **Account Deletion**: When an account involved in a recurring transfer is deleted:
   - Cascade delete the recurring transfer (cannot have orphaned recurring transfer)
   - Generated transactions follow existing cascade behavior

2. **Same Account Validation**: Prevent source = destination at creation and update

3. **Multiple Recurring Transfers Same Accounts**: Allow multiple recurring transfers between same account pair (e.g., weekly + monthly savings)

4. **Instance Already Generated**: When editing an instance that has already generated transactions:
   - Update the generated transactions
   - Mark exception as "applied to generated"

5. **Pause/Resume Timing**: When resuming:
   - Recalculate next occurrence from current date, not from when paused
   - Don't generate "missed" instances during pause period

6. **Date Change Conflicts**: When modifying an instance's date to a date that already has an instance:
   - Prevent the change
   - Or allow and handle display appropriately

7. **Transfer Deletion with Generated Transactions**: When deleting a recurring transfer:
   - Keep generated transactions (they're real transactions that happened)
   - Clear the `RecurringTransferId` link (orphan them as regular transfers)

---

## UI/UX Considerations

### Discoverability
- Show "Make Recurring" toggle in transfer dialog
- Quick action on transfer list: "Set up recurring"
- Suggest recurring when same transfer created multiple times

### Visual Consistency
- Use same recurrence icons/colors as recurring transactions
- Combine transfer icon with recurring indicator
- Consistent date/frequency pickers with recurring transactions

### Amount Display
- Always show positive amount (direction is From→To)
- Show account names with arrow: "Checking → Savings"
- Include frequency in summary: "$500/month"

### Confirmation
- Confirm before deleting recurring transfer
- Show impact: "This will stop future scheduled transfers. X generated transfers will be kept."
- Confirm pause with next occurrence info

---

## Future Enhancements

- [ ] Transfer templates (quick setup from common patterns)
- [ ] Smart suggestions based on transfer history
- [ ] Notifications before scheduled transfers
- [ ] Balance threshold triggers (transfer when account exceeds $X)
- [ ] Variable amounts (percentage of balance, or formula)
- [ ] Multi-currency recurring transfers with exchange rate handling
- [ ] Transfer approval workflow for shared accounts
- [ ] Analytics: Monthly/yearly transfer totals between accounts
