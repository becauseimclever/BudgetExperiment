# Feature: Account Transfers

## Overview
Enable users to easily transfer money between accounts with a single action, automatically creating the corresponding debit and credit transactions as a linked pair.

## User Stories

### US-001: Create Transfer Between Accounts
**As a** user  
**I want to** transfer money from one account to another with a single action  
**So that** I don't have to manually create two separate transactions

### US-002: View Transfer History
**As a** user  
**I want to** see a history of transfers between my accounts  
**So that** I can track money movement between accounts

### US-003: Edit Transfer
**As a** user  
**I want to** edit a transfer (amount, date, description)  
**So that** I can correct mistakes without deleting and recreating

### US-004: Delete Transfer
**As a** user  
**I want to** delete a transfer  
**So that** both the debit and credit transactions are removed together

### US-005: Identify Linked Transactions
**As a** user  
**I want to** see that two transactions are part of a transfer  
**So that** I understand they're linked and won't double-count them

---

## Domain Model

### Approach: Transfer as Linked Transactions
Add a `TransferId` to existing `Transaction` entity to link paired transactions. This is simpler than a separate `Transfer` entity and maintains existing transaction query patterns.

```
Transaction (existing, modified)
├── ... existing properties ...
├── TransferId: Guid? (null for non-transfer transactions)
└── TransferDirection: TransferDirection? (Source/Destination)
```

The `TransferId` links the pair, and operations ensure both are updated/deleted together.

### New Enum: `TransferDirection`
```csharp
public enum TransferDirection
{
    Source,      // Money leaving this account (negative amount)
    Destination  // Money entering this account (positive amount)
}
```

---

## API Design

### Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/transfers` | Create a transfer (creates 2 linked transactions) |
| GET | `/api/v1/transfers` | List all transfers |
| GET | `/api/v1/transfers/{transferId}` | Get transfer details |
| PUT | `/api/v1/transfers/{transferId}` | Update transfer (updates both transactions) |
| DELETE | `/api/v1/transfers/{transferId}` | Delete transfer (deletes both transactions) |

### Request/Response DTOs

#### CreateTransferRequest
```json
{
  "sourceAccountId": "guid",
  "destinationAccountId": "guid",
  "amount": 500.00,
  "date": "2026-01-15",
  "description": "Move to savings"
}
```

#### TransferResponse
```json
{
  "transferId": "guid",
  "sourceAccountId": "guid",
  "sourceAccountName": "Checking",
  "destinationAccountId": "guid",
  "destinationAccountName": "Savings",
  "amount": 500.00,
  "date": "2026-01-15",
  "description": "Move to savings",
  "sourceTransactionId": "guid",
  "destinationTransactionId": "guid",
  "createdAtUtc": "2026-01-15T10:30:00Z"
}
```

#### TransferListItemResponse
```json
{
  "transferId": "guid",
  "sourceAccountName": "Checking",
  "destinationAccountName": "Savings",
  "amount": 500.00,
  "date": "2026-01-15",
  "description": "Move to savings"
}
```

### Filtering & Pagination
- `GET /api/v1/transfers?accountId={guid}` - Transfers involving specific account
- `GET /api/v1/transfers?from={date}&to={date}` - Date range filter
- Standard pagination: `?page=1&pageSize=20`

---

## Client UI Design

### Quick Transfer Action
- Add "Transfer" button to account cards/list
- Floating action button (FAB) for quick transfer from any page

### Transfer Dialog Component
```
┌─────────────────────────────────────────┐
│ Transfer Between Accounts               │
├─────────────────────────────────────────┤
│                                         │
│ From Account:  [Checking ▼]             │
│                                         │
│ To Account:    [Savings ▼]              │
│                                         │
│ Amount:        [$_______]               │
│                                         │
│ Date:          [01/15/2026]             │
│                                         │
│ Description:   [Move to savings____]    │
│                (optional)               │
│                                         │
├─────────────────────────────────────────┤
│            [Cancel]  [Transfer]         │
└─────────────────────────────────────────┘
```

### Transaction List Integration
- Show transfer indicator icon on transactions that are part of a transfer
- Tooltip or expandable detail showing linked account
- Click on transfer icon navigates to transfer details or highlights paired transaction

### Transfer History Page (Optional)
- Dedicated page showing all transfers
- Columns: Date, From, To, Amount, Description, Actions
- Filter by account, date range
- Row actions: Edit, Delete

### Visual Indicators
- Use distinct icon (e.g., ↔️ or 🔄) for transfer transactions
- Color coding: transfers shown in neutral color (not red/green)
- Badge on transaction showing "Transfer to/from [Account Name]"

---

## Transaction Generation Logic

When creating a transfer:

```csharp
public async Task<TransferResponse> CreateTransferAsync(CreateTransferRequest request)
{
    var transferId = Guid.NewGuid();
    
    // Source transaction (money leaving)
    var sourceTransaction = new Transaction(
        id: Guid.NewGuid(),
        accountId: request.SourceAccountId,
        date: request.Date,
        description: $"Transfer to {destinationAccount.Name}: {request.Description}",
        amount: new MoneyValue(-request.Amount),
        transferId: transferId,
        transferDirection: TransferDirection.Source
    );
    
    // Destination transaction (money entering)
    var destinationTransaction = new Transaction(
        id: Guid.NewGuid(),
        accountId: request.DestinationAccountId,
        date: request.Date,
        description: $"Transfer from {sourceAccount.Name}: {request.Description}",
        amount: new MoneyValue(request.Amount),
        transferId: transferId,
        transferDirection: TransferDirection.Destination
    );
    
    // Save both in same unit of work
    await _transactionRepository.AddAsync(sourceTransaction);
    await _transactionRepository.AddAsync(destinationTransaction);
    await _unitOfWork.SaveChangesAsync();
    
    return MapToResponse(transferId, sourceTransaction, destinationTransaction);
}
```

---

## Implementation Plan (TDD Order)

### Phase 1: Domain Layer
1. [ ] Add `TransferDirection` enum
2. [ ] Extend `Transaction` entity with `TransferId` and `TransferDirection` properties
3. [ ] Add domain validation (source ≠ destination, amount > 0)
4. [ ] Add `GetByTransferIdAsync` to `ITransactionRepository`
5. [ ] Unit tests for transfer validation logic

### Phase 2: Infrastructure Layer
1. [ ] Update `TransactionConfiguration` for new columns
2. [ ] Create migration adding `TransferId` and `TransferDirection` columns
3. [ ] Add index on `TransferId` for efficient paired lookups
4. [ ] Implement repository query for transfers
5. [ ] Integration tests for transfer queries

### Phase 3: Application Layer
1. [ ] Create transfer DTOs (CreateTransferRequest, TransferResponse, etc.)
2. [ ] Create `ITransferService` interface
3. [ ] Implement `TransferService` with create/read/update/delete logic
4. [ ] Ensure atomic operations (both transactions succeed or fail together)
5. [ ] Unit tests with mocked repository

### Phase 4: API Layer
1. [ ] Create `TransfersController`
2. [ ] Add request validation (FluentValidation or data annotations)
3. [ ] Integration tests for all endpoints

### Phase 5: Client Layer
1. [ ] Create `ITransferApi` interface
2. [ ] Implement API client
3. [ ] Create `TransferDialog` component
4. [ ] Add transfer button to account UI
5. [ ] Update transaction list to show transfer indicators
6. [ ] Optional: Create transfers history page

---

## Database Changes

### Migration: Add Transfer Columns to Transactions

```sql
ALTER TABLE "Transactions" 
ADD COLUMN "TransferId" uuid NULL,
ADD COLUMN "TransferDirection" int NULL;

CREATE INDEX "IX_Transactions_TransferId" ON "Transactions"("TransferId");
```

### Constraints
- `TransferDirection` must be NOT NULL when `TransferId` is NOT NULL
- No foreign key on `TransferId` (it's a logical grouping, not a reference)

---

## Validation Rules

1. **Source ≠ Destination**: Cannot transfer to the same account
2. **Positive Amount**: Transfer amount must be > 0
3. **Valid Accounts**: Both accounts must exist and be active
4. **Date Required**: Transfer date is mandatory
5. **Edit Constraints**: When editing, both transactions must be updated atomically
6. **Delete Constraints**: Deleting one transaction of a pair deletes both

---

## Edge Cases

1. **Account Deletion**: When an account is deleted, transfers involving that account should:
   - Option A: Cascade delete both transactions (current behavior for regular transactions)
   - Option B: Convert to regular transactions (orphan the remaining transaction)
   - **Recommendation**: Option A for consistency

2. **Currency Mismatch**: For future multi-currency support, handle exchange rates

3. **Same-Day Multiple Transfers**: Allow multiple transfers between same accounts on same day

4. **Import Reconciliation**: When importing bank data, detect potential transfers:
   - Same amount (opposite signs) on same date between two accounts
   - Offer to link as transfer

---

## UI/UX Considerations

### Discoverability
- Show "Transfer" as a primary action alongside "Add Transaction"
- Context menu on account row: "Transfer from this account..."
- Keyboard shortcut: Ctrl+T for quick transfer dialog

### Amount Entry
- Always positive (direction determined by From/To selection)
- Currency formatting with locale support
- Optional: Quick amount buttons ($50, $100, $500, etc.)

### Account Selection
- Default "From" to most recently used account
- Remember last used account pair for quick repeat transfers
- Show current balance next to account name

### Confirmation
- Show clear summary before executing: "Transfer $500 from Checking to Savings?"
- Success notification with undo option (30 second window)

---

## Future Enhancements

- [ ] Recurring transfers (integrate with recurring transactions feature)
- [ ] Transfer templates (e.g., "Monthly savings transfer")
- [ ] Multi-currency transfers with exchange rate
- [ ] Scheduled future transfers
- [ ] Transfer approval workflow (for shared accounts)
- [ ] Import detection: Auto-identify transfers from bank imports
- [ ] Transfer analytics: Track movement patterns between accounts
