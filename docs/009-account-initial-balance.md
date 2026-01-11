# Feature: Account Initial Balance & Edit

## Overview
Enable accounts to have an initial balance set at creation time, and allow users to edit account details (name, type, initial balance) after creation. The initial balance serves as the starting point for all balance calculations displayed on the calendar and account views.

## User Stories

### US-001: Set Initial Balance on Account Creation
**As a** user  
**I want to** enter an initial balance when creating an account  
**So that** my calendar and projections start with the correct amount

### US-002: Edit Account Details
**As a** user  
**I want to** edit an account's name, type, and initial balance after creation  
**So that** I can correct mistakes or update information as needed

### US-003: View Account with Initial Balance
**As a** user  
**I want to** see my account's initial balance reflected in all balance calculations  
**So that** I have accurate running balances on the calendar and transaction views

### US-004: Initial Balance as of Date
**As a** user  
**I want to** specify a date for my initial balance  
**So that** transactions before that date don't affect the starting balance calculation

---

## Domain Model

### Account Entity Changes

```
Account (existing, modified)
├── ... existing properties ...
├── InitialBalance: MoneyValue (new - default 0.00)
└── InitialBalanceDate: DateOnly (new - date the initial balance is as of)
```

The `InitialBalanceDate` establishes when the initial balance was recorded. This is important for:
- Balance calculations before vs after this date
- Importing historical transactions correctly
- Clear audit trail of "balance as of X date"

### Balance Calculation Logic

```
Running Balance = InitialBalance + Sum(transactions where date >= InitialBalanceDate)
```

For calendar/projection purposes:
- Days before `InitialBalanceDate`: Show transactions but no running balance
- Days on or after `InitialBalanceDate`: Show running balance starting from `InitialBalance`

### Validation Rules

1. **Initial Balance**: Any decimal value (positive, negative, or zero)
2. **Initial Balance Date**: Required, defaults to today for new accounts
3. **Name**: Required, non-empty (existing validation)
4. **Type**: Valid AccountType enum value (existing validation)

---

## API Design

### Existing Endpoints Modified

| Method | Endpoint | Changes |
|--------|----------|---------|
| POST | `/api/v1/accounts` | Add `initialBalance` and `initialBalanceDate` to request |
| GET | `/api/v1/accounts` | Response includes `initialBalance` and `initialBalanceDate` |
| GET | `/api/v1/accounts/{id}` | Response includes `initialBalance` and `initialBalanceDate` |

### New Endpoint

| Method | Endpoint | Description |
|--------|----------|-------------|
| PUT | `/api/v1/accounts/{id}` | Update account details |

### Request/Response DTOs

#### AccountCreateDto (modified)
```json
{
  "name": "Checking Account",
  "type": "Checking",
  "initialBalance": 1500.00,
  "initialBalanceDate": "2026-01-01"
}
```

#### AccountUpdateDto (new)
```json
{
  "name": "Primary Checking",
  "type": "Checking",
  "initialBalance": 1500.00,
  "initialBalanceDate": "2026-01-01"
}
```
All fields optional - only provided fields are updated.

#### AccountDto (modified response)
```json
{
  "id": "guid",
  "name": "Checking Account",
  "type": "Checking",
  "initialBalance": 1500.00,
  "initialBalanceDate": "2026-01-01",
  "createdAt": "2026-01-10T10:00:00Z",
  "updatedAt": "2026-01-10T10:00:00Z",
  "transactions": []
}
```

---

## Client UI Design

### Account Creation Dialog (modified)

```
┌─────────────────────────────────────────┐
│ Add Account                             │
├─────────────────────────────────────────┤
│                                         │
│ Account Name:   [_________________]     │
│                                         │
│ Account Type:   [Checking ▼]            │
│                                         │
│ ─── Starting Balance ───────────────── │
│                                         │
│ Initial Balance: [$1,500.00___]         │
│                                         │
│ As of Date:      [01/01/2026]           │
│                                         │
├─────────────────────────────────────────┤
│            [Cancel]    [Create]         │
└─────────────────────────────────────────┘
```

### Account Edit Dialog (new)

```
┌─────────────────────────────────────────┐
│ Edit Account                            │
├─────────────────────────────────────────┤
│                                         │
│ Account Name:   [Primary Checking__]    │
│                                         │
│ Account Type:   [Checking ▼]            │
│                                         │
│ ─── Starting Balance ───────────────── │
│                                         │
│ Initial Balance: [$1,500.00___]         │
│                                         │
│ As of Date:      [01/01/2026]           │
│                                         │
│ ⚠️ Changing initial balance will affect │
│    all calculated running balances.     │
│                                         │
├─────────────────────────────────────────┤
│            [Cancel]    [Save]           │
└─────────────────────────────────────────┘
```

### Account Card Updates

```
┌─────────────────────────────────────────┐
│ Checking Account                        │
│ Type: Checking                          │
│ Initial Balance: $1,500.00 (as of 1/1)  │
├─────────────────────────────────────────┤
│ [Edit] [Transfer] [Transactions] [Delete]│
└─────────────────────────────────────────┘
```

### Calendar Integration

- Starting from `InitialBalanceDate`, show running balance for each day
- Running balance = `InitialBalance` + cumulative transactions
- Days before `InitialBalanceDate`: Show "—" or "N/A" for balance (transactions still shown)

### Account Transactions Page

- Show initial balance prominently at top of transaction list
- Running balance column starts from initial balance
- Summary includes: "Starting Balance: $X.XX | Current Balance: $Y.YY"

---

## Implementation Plan (TDD Order)

### Phase 1: Domain Layer
1. [ ] Add `InitialBalance` (MoneyValue) property to `Account` entity
2. [ ] Add `InitialBalanceDate` (DateOnly) property to `Account` entity
3. [ ] Update `Account.Create()` factory method with new parameters (with defaults)
4. [ ] Add `Account.Update()` method or individual update methods
5. [ ] Add `Account.UpdateInitialBalance()` method with validation
6. [ ] Unit tests for entity creation with initial balance
7. [ ] Unit tests for entity update methods

### Phase 2: Infrastructure Layer
1. [ ] Update `AccountConfiguration` with new column mappings
2. [ ] Create migration adding `InitialBalance` and `InitialBalanceDate` columns
3. [ ] Add data migration to set default values for existing accounts
4. [ ] Integration tests for repository with new fields

### Phase 3: Application Layer
1. [ ] Update `AccountCreateDto` with `InitialBalance` and `InitialBalanceDate`
2. [ ] Create `AccountUpdateDto` for edit operations
3. [ ] Update `AccountDto` response with new fields
4. [ ] Update `AccountService.CreateAsync()` to handle initial balance
5. [ ] Add `AccountService.UpdateAsync()` method
6. [ ] Update mapping logic
7. [ ] Unit tests with mocked repository

### Phase 4: API Layer
1. [ ] Update `AccountsController.CreateAsync()` to accept new fields
2. [ ] Add `AccountsController.UpdateAsync()` endpoint (PUT)
3. [ ] Update OpenAPI documentation
4. [ ] Integration tests for create with initial balance
5. [ ] Integration tests for update endpoint

### Phase 5: Client Layer
1. [ ] Update `AccountCreateModel` with `InitialBalance` and `InitialBalanceDate`
2. [ ] Create `AccountUpdateModel` for edit operations
3. [ ] Update `AccountModel` with new fields
4. [ ] Update `AccountForm.razor` to include initial balance fields
5. [ ] Create `AccountEditForm.razor` or extend existing form
6. [ ] Add edit button and dialog to `Accounts.razor` page
7. [ ] Extend `IBudgetApiService` with `UpdateAccountAsync()` method
8. [ ] Implement API client method
9. [ ] Update calendar balance calculations to use initial balance
10. [ ] Update account transaction view to show initial balance
11. [ ] Component tests where appropriate

---

## Database Changes

### Migration: Add Initial Balance to Accounts

```sql
-- Add columns
ALTER TABLE "Accounts"
ADD COLUMN "InitialBalance" numeric NOT NULL DEFAULT 0,
ADD COLUMN "InitialBalanceDate" date NOT NULL DEFAULT CURRENT_DATE;

-- For existing accounts, set InitialBalanceDate to their CreatedAt date
UPDATE "Accounts"
SET "InitialBalanceDate" = DATE("CreatedAt")
WHERE "InitialBalanceDate" = CURRENT_DATE;
```

### Column Details

| Column | Type | Nullable | Default | Notes |
|--------|------|----------|---------|-------|
| InitialBalance | decimal | No | 0 | Stored as numeric for precision |
| InitialBalanceDate | date | No | CURRENT_DATE | Date the balance was recorded |

---

## Edge Cases

1. **Negative Initial Balance**: Allow for accounts starting in debt (e.g., credit card balance)

2. **Zero Initial Balance**: Valid default; account starts at $0

3. **Changing Initial Balance Date**:
   - If moved forward: Fewer transactions included in running balance
   - If moved backward: More transactions included
   - UI should warn about impact on balance calculations

4. **Transactions Before Initial Balance Date**:
   - Still stored and displayed
   - Not included in running balance calculation
   - Consider showing warning: "Transaction before initial balance date"

5. **Concurrent Edits**: Use `UpdatedAt` for optimistic concurrency if needed

6. **Import Scenarios**: When importing transactions:
   - User sets initial balance to bank statement balance
   - Sets date to statement date
   - Imports transactions after that date

---

## Balance Calculation Examples

### Example 1: New Account
```
Initial Balance: $1,000 (as of 2026-01-01)
Transactions:
  2026-01-05: -$50 (groceries)
  2026-01-10: +$2,000 (paycheck)
  2026-01-15: -$100 (utilities)

Running Balance by Date:
  2026-01-01: $1,000 (initial)
  2026-01-05: $950
  2026-01-10: $2,950
  2026-01-15: $2,850
```

### Example 2: Transactions Before Initial Date
```
Initial Balance: $500 (as of 2026-01-10)
Transactions:
  2026-01-05: -$50 (groceries) ← Before initial date
  2026-01-10: +$100 (deposit)
  2026-01-15: -$25 (coffee)

Running Balance by Date:
  2026-01-05: — (no balance, before initial date)
  2026-01-10: $600 ($500 + $100)
  2026-01-15: $575
```

### Example 3: Credit Card (Negative Balance)
```
Initial Balance: -$2,500 (as of 2026-01-01)
Transactions:
  2026-01-05: -$100 (purchase, increases debt)
  2026-01-15: +$500 (payment, reduces debt)

Running Balance by Date:
  2026-01-01: -$2,500 (initial debt)
  2026-01-05: -$2,600
  2026-01-15: -$2,100
```

---

## UI/UX Considerations

### Discoverability
- Initial balance fields visible but collapsible in create form
- Default initial balance to $0 and date to today
- "Edit" button prominently visible on account cards

### Validation Feedback
- Real-time validation on initial balance (numeric format)
- Date picker prevents future dates? (optional, may want for projections)
- Clear error messages for invalid input

### Warning Messages
- Warn when editing initial balance: "This will recalculate all running balances"
- Warn when changing initial balance date with existing transactions

### Defaults
- Initial balance: `0.00`
- Initial balance date: Today's date
- Makes form simple for users who don't need initial balance

---

## Future Enhancements

- [ ] Balance reconciliation: Compare calculated vs actual balance
- [ ] Multiple initial balance "checkpoints" for periodic reconciliation
- [ ] Import initial balance from bank connection
- [ ] Historical balance tracking (snapshots over time)
- [ ] Balance alerts: Notify when balance falls below threshold
- [ ] Balance goals: Set target balance for savings accounts
