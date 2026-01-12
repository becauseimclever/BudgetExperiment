# Feature 015: Realize Recurring Items

## Overview

Enable users to "realize" (confirm) recurring transactions and recurring transfers, converting projected instances into actual transactions. Past-due recurring items should display a visual alert prompting the user to confirm or skip them.

## User Stories

### US-001: Realize Recurring Transaction
**As a** user  
**I want to** confirm a projected recurring transaction instance  
**So that** it becomes an actual transaction in my account

### US-002: Realize Recurring Transfer
**As a** user  
**I want to** confirm a projected recurring transfer instance  
**So that** it becomes actual paired transactions in both accounts

### US-003: Past-Due Alert
**As a** user  
**I want to** see a visual indicator when recurring items are past their scheduled date  
**So that** I know to review and confirm or skip them

### US-004: Bulk Realize Past-Due Items
**As a** user  
**I want to** quickly confirm multiple past-due recurring items  
**So that** I can catch up efficiently

### US-005: Realize with Modifications
**As a** user  
**I want to** realize a recurring item with different values (amount, date, description)  
**So that** I can reflect the actual transaction details

---

## Domain Concepts

### Realization
Converting a projected recurring instance into an actual `Transaction` entity by:
1. Creating transaction(s) with `RecurringTransactionId`/`RecurringTransferId` link
2. Setting `RecurringInstanceDate`/`RecurringTransferInstanceDate` to the scheduled date
3. Existing deduplication logic then hides the projection

### Past-Due
A recurring instance is "past-due" when:
- `ScheduledDate < Today`
- No realized transaction exists for that instance
- The instance is not skipped

---

## API Design

### Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/recurring-transactions/{id}/realize` | Realize a recurring transaction instance |
| POST | `/api/v1/recurring-transfers/{id}/realize` | Realize a recurring transfer instance |
| GET | `/api/v1/recurring/past-due` | Get all past-due recurring items |
| POST | `/api/v1/recurring/realize-batch` | Realize multiple items at once |

### Request/Response DTOs

#### RealizeRecurringTransactionRequest
```json
{
  "instanceDate": "2026-01-15",
  "date": "2026-01-15",           // optional: actual date (defaults to instanceDate)
  "amount": { "currency": "USD", "amount": -50.00 },  // optional: override amount
  "description": "Netflix"        // optional: override description
}
```

#### RealizeRecurringTransferRequest
```json
{
  "instanceDate": "2026-01-15",
  "date": "2026-01-15",           // optional: actual date
  "amount": { "currency": "USD", "amount": 500.00 }  // optional: override amount
}
```

#### PastDueItemDto
```json
{
  "id": "guid",
  "type": "recurring-transaction" | "recurring-transfer",
  "instanceDate": "2026-01-10",
  "daysPastDue": 5,
  "description": "Netflix",
  "amount": { "currency": "USD", "amount": -15.99 },
  "accountId": "guid",
  "accountName": "Checking",
  "sourceAccountName": "Checking",      // for transfers
  "destinationAccountName": "Savings"   // for transfers
}
```

#### PastDueSummaryDto
```json
{
  "items": [ /* PastDueItemDto[] */ ],
  "totalCount": 5,
  "oldestDate": "2026-01-05",
  "totalAmount": { "currency": "USD", "amount": -250.00 }
}
```

---

## UI Design

### Past-Due Alert Banner

Display at top of Calendar and Account Transactions pages when past-due items exist:

```
┌────────────────────────────────────────────────────────────────────┐
│ ⚠️  5 recurring items are past due                    [Review →]  │
└────────────────────────────────────────────────────────────────────┘
```

### Visual Indicators

| State | Icon | Color | Description |
|-------|------|-------|-------------|
| Future | 🔄 | Blue | Normal recurring item |
| Today | 🔄 | Green | Due today |
| Past-Due | 🔄⚠️ | Orange/Red | Past scheduled date, needs attention |

### Day Detail - Realize Action

In the day detail panel, recurring items show a "Confirm" button:

```
┌─────────────────────────────────────────────────────────┐
│ January 10, 2026                                        │
├─────────────────────────────────────────────────────────┤
│ 🔄⚠️ Netflix           -$15.99    [Confirm] [Skip] [Edit] │
│ 🔄⚠️ Gym Membership    -$29.99    [Confirm] [Skip] [Edit] │
│ ✓  Grocery Store      -$85.50    [Edit] [Delete]        │
└─────────────────────────────────────────────────────────┘
```

### Account Transactions - Realize Action

Similar to day detail, but in table row actions.

### Past-Due Review Modal

Accessed from the alert banner:

```
┌─────────────────────────────────────────────────────────┐
│ Review Past-Due Items                              [X]  │
├─────────────────────────────────────────────────────────┤
│ ☑️ Jan 5  Netflix          -$15.99   Checking          │
│ ☑️ Jan 5  Savings Transfer  $500.00  Checking→Savings  │
│ ☑️ Jan 10 Gym Membership   -$29.99   Checking          │
│ ☐ Jan 10 Electric Bill    -$120.00  Checking          │
├─────────────────────────────────────────────────────────┤
│ Selected: 3 items, Total: -$545.98                      │
│                                                         │
│              [Skip Selected]  [Confirm Selected]        │
└─────────────────────────────────────────────────────────┘
```

---

## Implementation Plan

### Phase 1: Service Layer
1. Add `RealizeInstanceAsync` to `RecurringTransactionService`
2. Add `RealizeInstanceAsync` to `RecurringTransferService`
3. Add `GetPastDueItemsAsync` to new `PastDueService`

### Phase 2: API Endpoints
1. Add realize endpoints to existing controllers
2. Add new `/api/v1/recurring/past-due` endpoint
3. Add batch realize endpoint

### Phase 3: Client API Service
1. Add `RealizeRecurringTransactionAsync` method
2. Add `RealizeRecurringTransferAsync` method
3. Add `GetPastDueItemsAsync` method

### Phase 4: UI Components
1. Add "Confirm" button to `DayDetail` component
2. Add "Confirm" button to `TransactionTable` component
3. Create `PastDueAlert` component
4. Create `PastDueReviewModal` component

### Phase 5: Integration
1. Add alert banner to Calendar page
2. Add alert banner to Account Transactions page
3. Wire up confirm actions

---

## Technical Details

### Realize Recurring Transaction

```csharp
public async Task<TransactionDto> RealizeInstanceAsync(
    Guid recurringTransactionId,
    DateOnly instanceDate,
    RealizeRecurringTransactionRequest request,
    CancellationToken ct = default)
{
    var recurring = await _recurringRepository.GetByIdAsync(recurringTransactionId, ct);
    if (recurring is null)
        throw new NotFoundException("Recurring transaction not found");

    // Check if already realized
    var existing = await _transactionRepository.GetByRecurringInstanceAsync(
        recurringTransactionId, instanceDate, ct);
    if (existing != null)
        throw new ConflictException("This instance has already been realized");

    // Apply any exception modifications
    var exception = await _recurringRepository.GetExceptionAsync(
        recurringTransactionId, instanceDate, ct);

    var actualDate = request.Date ?? exception?.ModifiedDate ?? instanceDate;
    var actualAmount = request.Amount ?? exception?.ModifiedAmount ?? recurring.Amount;
    var actualDescription = request.Description ?? exception?.ModifiedDescription ?? recurring.Description;

    var transaction = Transaction.CreateFromRecurring(
        recurring.AccountId,
        actualAmount,
        actualDate,
        actualDescription,
        recurringTransactionId,
        instanceDate,
        recurring.Category);

    await _transactionRepository.AddAsync(transaction, ct);
    await _unitOfWork.SaveChangesAsync(ct);

    return DomainToDtoMapper.ToDto(transaction);
}
```

### Realize Recurring Transfer

```csharp
public async Task<TransferDto> RealizeInstanceAsync(
    Guid recurringTransferId,
    DateOnly instanceDate,
    RealizeRecurringTransferRequest request,
    CancellationToken ct = default)
{
    var recurring = await _recurringTransferRepository.GetByIdAsync(recurringTransferId, ct);
    if (recurring is null)
        throw new NotFoundException("Recurring transfer not found");

    // Check if already realized
    var existing = await _transactionRepository.GetByRecurringTransferInstanceAsync(
        recurringTransferId, instanceDate, ct);
    if (existing.Any())
        throw new ConflictException("This instance has already been realized");

    // Apply any exception modifications
    var exception = await _recurringTransferRepository.GetExceptionAsync(
        recurringTransferId, instanceDate, ct);

    var actualDate = request.Date ?? exception?.ModifiedDate ?? instanceDate;
    var actualAmount = request.Amount ?? exception?.ModifiedAmount ?? recurring.Amount;

    var transferId = Guid.NewGuid();

    // Create source transaction (negative)
    var sourceTransaction = Transaction.CreateFromRecurringTransfer(
        recurring.SourceAccountId,
        MoneyValue.Create(actualAmount.Currency, -actualAmount.Amount),
        actualDate,
        recurring.Description,
        transferId,
        TransferDirection.Source,
        recurringTransferId,
        instanceDate);

    // Create destination transaction (positive)
    var destTransaction = Transaction.CreateFromRecurringTransfer(
        recurring.DestinationAccountId,
        actualAmount,
        actualDate,
        recurring.Description,
        transferId,
        TransferDirection.Destination,
        recurringTransferId,
        instanceDate);

    await _transactionRepository.AddAsync(sourceTransaction, ct);
    await _transactionRepository.AddAsync(destTransaction, ct);
    await _unitOfWork.SaveChangesAsync(ct);

    return /* build TransferDto */;
}
```

### Calculate Past-Due Items

```csharp
public async Task<PastDueSummaryDto> GetPastDueItemsAsync(
    Guid? accountId = null,
    CancellationToken ct = default)
{
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var lookbackDate = today.AddDays(-30); // Configurable lookback

    var items = new List<PastDueItemDto>();

    // Get recurring transactions
    var recurringTransactions = accountId.HasValue
        ? await _recurringRepository.GetByAccountIdAsync(accountId.Value, ct)
        : await _recurringRepository.GetActiveAsync(ct);

    foreach (var recurring in recurringTransactions)
    {
        var occurrences = recurring.GetOccurrencesBetween(lookbackDate, today.AddDays(-1));
        foreach (var date in occurrences)
        {
            // Check if skipped
            var exception = await _recurringRepository.GetExceptionAsync(recurring.Id, date, ct);
            if (exception?.ExceptionType == ExceptionType.Skipped)
                continue;

            // Check if realized
            var realized = await _transactionRepository.GetByRecurringInstanceAsync(
                recurring.Id, date, ct);
            if (realized != null)
                continue;

            items.Add(new PastDueItemDto { /* ... */ });
        }
    }

    // Similar loop for recurring transfers...

    return new PastDueSummaryDto
    {
        Items = items.OrderBy(i => i.InstanceDate).ToList(),
        TotalCount = items.Count,
        OldestDate = items.MinBy(i => i.InstanceDate)?.InstanceDate,
        TotalAmount = /* sum amounts */
    };
}
```

---

## Files to Create/Modify

### New Files
- `src/BudgetExperiment.Contracts/Dtos/RealizeRecurringTransactionRequest.cs`
- `src/BudgetExperiment.Contracts/Dtos/RealizeRecurringTransferRequest.cs`
- `src/BudgetExperiment.Contracts/Dtos/PastDueItemDto.cs`
- `src/BudgetExperiment.Contracts/Dtos/PastDueSummaryDto.cs`
- `src/BudgetExperiment.Application/Services/IPastDueService.cs`
- `src/BudgetExperiment.Application/Services/PastDueService.cs`
- `src/BudgetExperiment.Client/Components/Display/PastDueAlert.razor`
- `src/BudgetExperiment.Client/Components/Modals/PastDueReviewModal.razor`

### Modified Files
- `src/BudgetExperiment.Application/Services/RecurringTransactionService.cs` - Add `RealizeInstanceAsync`
- `src/BudgetExperiment.Application/Services/RecurringTransferService.cs` - Add `RealizeInstanceAsync`
- `src/BudgetExperiment.Api/Controllers/RecurringTransactionsController.cs` - Add realize endpoint
- `src/BudgetExperiment.Api/Controllers/RecurringTransfersController.cs` - Add realize endpoint
- `src/BudgetExperiment.Client/Services/IBudgetApiService.cs` - Add realize methods
- `src/BudgetExperiment.Client/Services/BudgetApiService.cs` - Implement methods
- `src/BudgetExperiment.Client/Pages/Calendar.razor` - Add past-due alert
- `src/BudgetExperiment.Client/Pages/AccountTransactions.razor` - Add past-due alert
- `src/BudgetExperiment.Client/Components/Display/DayDetail.razor` - Add Confirm button
- `src/BudgetExperiment.Client/Components/Display/TransactionTable.razor` - Add Confirm button

---

## Testing Strategy

### Unit Tests
1. `RealizeInstanceAsync` creates transaction with correct links
2. `RealizeInstanceAsync` applies exception modifications
3. `RealizeInstanceAsync` throws if already realized
4. `GetPastDueItemsAsync` returns only unrealized, non-skipped items
5. `GetPastDueItemsAsync` excludes future items
6. Past-due calculation respects lookback window

### Integration Tests
1. Realize endpoint creates transaction
2. Realize endpoint returns 409 if already realized
3. Past-due endpoint returns correct items
4. Batch realize processes multiple items

---

## Success Criteria

1. ✅ Users can realize recurring transaction instances
2. ✅ Users can realize recurring transfer instances
3. ✅ Past-due items display visual alert
4. ✅ Users can bulk-realize past-due items
5. ✅ Realized items support modifications
6. ✅ Deduplication correctly hides realized projections
7. ✅ All tests pass

---

## Implementation Status

### Completed ✅

**Phase 1: Service Layer**
- ✅ `RecurringTransactionService.RealizeInstanceAsync` - 6 unit tests
- ✅ `RecurringTransferService.RealizeInstanceAsync` - 5 unit tests
- ✅ Repository methods: `GetByRecurringInstanceAsync`, `GetByRecurringTransferInstanceAsync`
- ✅ `IPastDueService` interface and `PastDueService` implementation - 9 unit tests

**Phase 2: API Endpoints**
- ✅ `POST /api/v1/recurring-transactions/{id}/realize`
- ✅ `POST /api/v1/recurring-transfers/{id}/realize`
- ✅ `GET /api/v1/recurring/past-due`
- ✅ `POST /api/v1/recurring/realize-batch`

**Phase 3: Client API Service**
- ✅ `RealizeRecurringTransactionAsync` method
- ✅ `RealizeRecurringTransferAsync` method
- ✅ `GetPastDueItemsAsync` method
- ✅ `RealizeBatchAsync` method

**Phase 4: UI Components**
- ✅ "Confirm" button added to `DayDetail` component (Calendar page)
- ✅ "Confirm" button added to `TransactionTable` component
- ✅ Wired up in `Calendar.razor` and `AccountTransactions.razor`
- ✅ `PastDueAlert` component (banner at top of pages)
- ✅ `PastDueReviewModal` component (bulk confirm modal)
- ✅ CSS styles for past-due alerts

**DTOs Created**
- ✅ `RealizeRecurringTransactionRequest.cs`
- ✅ `RealizeRecurringTransferRequest.cs`
- ✅ `PastDueItemDto.cs`
- ✅ `PastDueSummaryDto.cs`
- ✅ `BatchRealizeRequest.cs`
- ✅ `BatchRealizeResultDto.cs`

---

## Edge Cases

1. **Realize on different date**: User confirms Jan 10 Netflix on Jan 12. Store both dates (`RecurringInstanceDate` = Jan 10, `Date` = Jan 12).

2. **Modified then realized**: If an exception exists with modifications, apply those to the realized transaction.

3. **Skip after modify**: If user modified an instance then later wants to skip, create skip exception and don't realize.

4. **Double realize**: Return 409 Conflict if trying to realize an already-realized instance.

5. **Realize old items**: Allow realizing items from any past date (no cutoff).

6. **Timezone considerations**: Past-due calculation uses server UTC date; display converts to user's timezone.

---

**Document Version**: 1.2  
**Created**: 2026-01-11  
**Updated**: 2026-01-11  
**Status**: ✅ Complete  
**Author**: Engineering Team
