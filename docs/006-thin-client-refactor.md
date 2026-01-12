# 006: Thin Client Refactor - UI Display Logic Only

## Overview

Refactor the Blazor WebAssembly client to contain **only display/presentation logic**, moving all business logic to the API layer. The client should become a "thin client" that renders data received from the API without performing calculations, data transformations, or business rule enforcement.

## Current State Analysis

### Business Logic Currently in Client

1. **Calendar Page (`Calendar.razor`)**
   - `BuildCalendarDays()`: Calculates calendar grid structure (prev/next month days, 6-week grid)
   - Date range calculations for API calls (grid start/end dates)
   - Grouping recurring transactions by date (`recurringByDate` dictionary)
   - Navigation date calculations (previous/next month)

2. **Account Transactions Page (`AccountTransactions.razor`)**
   - `MergeTransactionsAndRecurring()`: Merges actual transactions with recurring instances
   - Duplicate detection logic (checking if recurring instance has realized transaction)
   - `totalAmount` calculation: `allItems.Sum(i => i.Amount.Amount)`
   - `recurringCount` calculation: `allItems.Count(i => i.IsRecurring)`

3. **Client Models with Business Logic**
   - `TransactionListItem.FromTransaction()` / `FromRecurringInstance()`: Data transformation/mapping
   - `CalendarDayModel.RecurringTotal`: Calculated property with filtering logic

4. **API Service (`BudgetApiService.cs`)**
   - Model transformation in `CreateTransactionAsync` (converting client model to API format)

### Problems with Current Approach

1. **Duplicated Logic**: Business rules may need to be implemented in both client and server
2. **Inconsistency Risk**: Different behavior between client and server calculations
3. **Testability**: Client-side logic is harder to unit test reliably
4. **Maintenance**: Changes to business rules require updates in multiple places
5. **Performance**: Client performs calculations that could be pre-computed server-side
6. **Security**: Business logic in client can be bypassed or manipulated

## Target Architecture

### Client Responsibilities (Display Only)

- **Render** data received from API
- **Handle** user interactions (clicks, form inputs)
- **Navigate** between pages
- **Manage** UI state (modals open/closed, loading states, selected items)
- **Format** data for display (date formatting, currency display)
- **Validate** form inputs (basic client-side validation for UX, not security)

### API Responsibilities (All Business Logic)

- **Calculate** derived data (totals, counts, projections)
- **Transform** and aggregate data for specific views
- **Enforce** business rules and validation
- **Build** complex data structures (calendar grids, merged lists)
- **Handle** date/time calculations
- **Manage** data relationships and joins

## Implementation Plan

### Phase 1: New API Endpoints

#### 1.1 Calendar Grid Endpoint

```
GET /api/v1/calendar/grid?year={year}&month={month}&accountId={accountId?}
```

**Response**: Complete calendar grid with all data pre-computed

```json
{
  "year": 2026,
  "month": 1,
  "days": [
    {
      "date": "2025-12-28",
      "isCurrentMonth": false,
      "isToday": false,
      "actualTotal": { "currency": "USD", "amount": -150.00 },
      "projectedTotal": { "currency": "USD", "amount": -75.00 },
      "combinedTotal": { "currency": "USD", "amount": -225.00 },
      "transactionCount": 3,
      "recurringCount": 2,
      "hasRecurring": true
    }
    // ... 42 days total (6 weeks)
  ],
  "monthSummary": {
    "totalIncome": { "currency": "USD", "amount": 5000.00 },
    "totalExpenses": { "currency": "USD", "amount": -3500.00 },
    "netChange": { "currency": "USD", "amount": 1500.00 },
    "projectedIncome": { "currency": "USD", "amount": 500.00 },
    "projectedExpenses": { "currency": "USD", "amount": -200.00 }
  }
}
```

#### 1.2 Day Detail Endpoint

```
GET /api/v1/calendar/day/{date}?accountId={accountId?}
```

**Response**: All transactions and recurring instances for a specific day, pre-merged

```json
{
  "date": "2026-01-15",
  "items": [
    {
      "id": "guid",
      "type": "transaction",
      "description": "Grocery Store",
      "amount": { "currency": "USD", "amount": -85.50 },
      "category": "Food",
      "accountName": "Checking",
      "createdAt": "2026-01-15T10:30:00Z"
    },
    {
      "id": "guid",
      "type": "recurring",
      "description": "Netflix",
      "amount": { "currency": "USD", "amount": -15.99 },
      "category": "Entertainment",
      "accountName": "Credit Card",
      "isModified": false,
      "isSkipped": false,
      "recurringTransactionId": "guid"
    }
  ],
  "summary": {
    "totalActual": { "currency": "USD", "amount": -85.50 },
    "totalProjected": { "currency": "USD", "amount": -15.99 },
    "combinedTotal": { "currency": "USD", "amount": -101.49 },
    "itemCount": 2
  }
}
```

#### 1.3 Account Transactions List Endpoint (Enhanced)

```
GET /api/v1/accounts/{accountId}/transaction-list?startDate={date}&endDate={date}&includeRecurring=true
```

**Response**: Pre-merged, pre-sorted list with all calculations done

```json
{
  "accountId": "guid",
  "accountName": "Checking",
  "startDate": "2025-12-01",
  "endDate": "2026-01-31",
  "items": [
    {
      "id": "guid",
      "type": "transaction",
      "date": "2026-01-10",
      "description": "Paycheck",
      "amount": { "currency": "USD", "amount": 2500.00 },
      "category": "Income",
      "isTransfer": false,
      "transferId": null,
      "transferDirection": null
    },
    {
      "id": "guid",
      "type": "recurring",
      "date": "2026-01-15",
      "description": "Rent",
      "amount": { "currency": "USD", "amount": -1500.00 },
      "category": "Housing",
      "isModified": false,
      "recurringTransactionId": "guid"
    }
  ],
  "summary": {
    "totalAmount": { "currency": "USD", "amount": 1000.00 },
    "totalTransactions": 10,
    "totalRecurring": 5,
    "totalIncome": { "currency": "USD", "amount": 2500.00 },
    "totalExpenses": { "currency": "USD", "amount": -1500.00 }
  }
}
```

#### 1.4 Transfer List Endpoint (Enhanced)

```
GET /api/v1/transfers?accountId={accountId?}&startDate={date?}&endDate={date?}
```

**Response**: Already well-structured, but add summary

```json
{
  "items": [ /* existing transfer DTOs */ ],
  "summary": {
    "totalTransfers": 15,
    "totalAmount": { "currency": "USD", "amount": 5000.00 }
  }
}
```

### Phase 2: Application Layer Services

#### 2.1 New/Enhanced Services

| Service | Responsibility |
|---------|---------------|
| `ICalendarGridService` | Build complete calendar grid with all calculations |
| `IDayDetailService` | Aggregate day data with merged items |
| `ITransactionListService` | Merge transactions + recurring, calculate totals |
| `ITransferListService` | Filter and summarize transfers |

#### 2.2 Service Interfaces

```csharp
public interface ICalendarGridService
{
    Task<CalendarGridDto> GetCalendarGridAsync(
        int year, 
        int month, 
        Guid? accountId, 
        CancellationToken cancellationToken = default);
}

public interface IDayDetailService
{
    Task<DayDetailDto> GetDayDetailAsync(
        DateOnly date, 
        Guid? accountId, 
        CancellationToken cancellationToken = default);
}

public interface ITransactionListService
{
    Task<TransactionListDto> GetTransactionListAsync(
        Guid accountId,
        DateOnly startDate,
        DateOnly endDate,
        bool includeRecurring,
        CancellationToken cancellationToken = default);
}
```

### Phase 3: Client Simplification

#### 3.1 Remove from Client

- `BuildCalendarDays()` method and related calculations
- `MergeTransactionsAndRecurring()` method
- `CalendarDayModel` (replaced by API response)
- `TransactionListItem.FromTransaction()` / `FromRecurringInstance()` factory methods
- Calculated properties like `RecurringTotal`, `totalAmount`, `recurringCount`
- Date range calculations for grid boundaries

#### 3.2 Simplified Client Models

```csharp
// Client just holds data from API - no business logic
public sealed class CalendarGridModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public IReadOnlyList<CalendarDayModel> Days { get; set; } = [];
    public CalendarSummaryModel Summary { get; set; } = new();
}

public sealed class CalendarDayModel
{
    // All properties come directly from API - no calculated properties
    public DateOnly Date { get; set; }
    public bool IsCurrentMonth { get; set; }
    public bool IsToday { get; set; }
    public MoneyModel ActualTotal { get; set; } = new();
    public MoneyModel ProjectedTotal { get; set; } = new();
    public MoneyModel CombinedTotal { get; set; } = new();
    public int TransactionCount { get; set; }
    public int RecurringCount { get; set; }
    public bool HasRecurring { get; set; }
}
```

#### 3.3 Simplified Page Code

```csharp
// Calendar.razor - AFTER refactor
@code {
    private CalendarGridModel? calendarGrid;
    private DayDetailModel? selectedDayDetail;
    private bool isLoading = true;

    // UI state only
    private DateOnly? selectedDate;
    private bool showAddTransaction = false;
    private bool isSubmitting = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadCalendarGrid();
    }

    private async Task LoadCalendarGrid()
    {
        isLoading = true;
        calendarGrid = await ApiService.GetCalendarGridAsync(Year, Month, filterAccountId);
        isLoading = false;
    }

    private async Task SelectDate(DateOnly date)
    {
        selectedDate = date;
        selectedDayDetail = await ApiService.GetDayDetailAsync(date, filterAccountId);
    }

    // Navigation is still client responsibility
    private void PreviousMonth() => Navigation.NavigateTo($"/{Year - (Month == 1 ? 1 : 0)}/{(Month == 1 ? 12 : Month - 1)}");
    private void NextMonth() => Navigation.NavigateTo($"/{Year + (Month == 12 ? 1 : 0)}/{(Month == 12 ? 1 : Month + 1)}");
}
```

### Phase 4: API Service Updates

#### 4.1 Updated IBudgetApiService

```csharp
public interface IBudgetApiService
{
    // Calendar - single call returns everything needed
    Task<CalendarGridModel> GetCalendarGridAsync(int year, int month, Guid? accountId = null);
    Task<DayDetailModel> GetDayDetailAsync(DateOnly date, Guid? accountId = null);

    // Account Transactions - pre-merged list
    Task<TransactionListModel> GetAccountTransactionListAsync(
        Guid accountId, 
        DateOnly startDate, 
        DateOnly endDate);

    // Transfers - with summary
    Task<TransferListModel> GetTransfersAsync(
        Guid? accountId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null);

    // CRUD operations remain similar
    Task<AccountModel?> CreateAccountAsync(AccountCreateModel model);
    Task<TransactionModel?> CreateTransactionAsync(TransactionCreateModel model);
    // ... etc
}
```

## Migration Strategy

### Step 1: Add New Endpoints (Non-Breaking)
- Add new "view-optimized" endpoints alongside existing ones
- Existing endpoints continue to work

### Step 2: Update Client to Use New Endpoints
- One page at a time, switch to new endpoints
- Remove client-side business logic as each page is migrated

### Step 3: Deprecate Old Endpoints
- Mark old endpoints as deprecated
- Monitor usage, remove when safe

### Step 4: Clean Up
- Remove unused client models and logic
- Remove deprecated API endpoints

## File Changes Summary

### New Files

**API Layer:**
- `src/BudgetExperiment.Api/Controllers/CalendarController.cs` (new endpoints)
- `src/BudgetExperiment.Api/Dtos/CalendarGridDto.cs`
- `src/BudgetExperiment.Api/Dtos/DayDetailDto.cs`
- `src/BudgetExperiment.Api/Dtos/TransactionListDto.cs`

**Application Layer:**
- `src/BudgetExperiment.Application/Services/ICalendarGridService.cs`
- `src/BudgetExperiment.Application/Services/CalendarGridService.cs`
- `src/BudgetExperiment.Application/Services/IDayDetailService.cs`
- `src/BudgetExperiment.Application/Services/DayDetailService.cs`
- `src/BudgetExperiment.Application/Services/ITransactionListService.cs`
- `src/BudgetExperiment.Application/Services/TransactionListService.cs`
- `src/BudgetExperiment.Application/Dtos/CalendarGridDto.cs`
- `src/BudgetExperiment.Application/Dtos/DayDetailDto.cs`
- `src/BudgetExperiment.Application/Dtos/TransactionListResultDto.cs`

### Modified Files

**Client Layer:**
- `src/BudgetExperiment.Client/Pages/Calendar.razor` - Remove business logic
- `src/BudgetExperiment.Client/Pages/AccountTransactions.razor` - Remove business logic
- `src/BudgetExperiment.Client/Pages/Transfers.razor` - Use enhanced endpoint
- `src/BudgetExperiment.Client/Services/IBudgetApiService.cs` - New methods
- `src/BudgetExperiment.Client/Services/BudgetApiService.cs` - Implement new methods
- `src/BudgetExperiment.Client/Models/CalendarDayModel.cs` - Remove calculated properties

### Deleted Files (After Migration)

- `src/BudgetExperiment.Client/Models/TransactionListItem.cs` - Logic moved to API

## Testing Strategy

### Unit Tests

1. **Application Services** (Priority: High)
   - `CalendarGridServiceTests` - Grid building, date calculations
   - `DayDetailServiceTests` - Merging logic, totals
   - `TransactionListServiceTests` - Merge, dedup, sort, calculate

2. **API Controllers** (Priority: High)
   - Integration tests for new endpoints
   - Verify response shapes match expected DTOs

### Integration Tests

1. **End-to-End** (Priority: Medium)
   - Calendar grid with various month/year combinations
   - Day detail with mixed transactions and recurring
   - Account transaction list with overlapping dates

### Client Tests (Optional)

1. **Component Tests** (Priority: Low)
   - Verify components render API data correctly
   - No business logic to test

## Success Criteria

1. **Zero business logic in client** - Only UI state management and rendering
2. **Single API call per view** - No multiple round-trips for data aggregation
3. **All calculations server-side** - Totals, counts, projections
4. **Consistent behavior** - Same results regardless of client
5. **Improved testability** - All business logic has unit tests
6. **No breaking changes** - Existing functionality preserved during migration

## Risks and Mitigations

| Risk | Mitigation |
|------|-----------|
| Increased API response size | Use pagination, lazy loading for day details |
| More complex API logic | Comprehensive unit tests, clear service boundaries |
| Migration disruption | Phased approach, feature flags if needed |
| Performance regression | Profile endpoints, optimize queries, caching |

## Future Considerations

1. **Caching**: Server-side caching of computed views
2. **Real-time Updates**: SignalR for live calendar updates
3. **Offline Support**: Service worker caching of grid data
4. **Mobile API**: Same endpoints work for future mobile clients

---

## Appendix: Client Code Comparison

### Before (Business Logic in Client)

```csharp
// Calendar.razor - current state
private void BuildCalendarDays()
{
    calendarDays.Clear();
    var firstOfMonth = new DateOnly(currentDate.Year, currentDate.Month, 1);
    var daysInMonth = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
    var startDayOfWeek = (int)firstOfMonth.DayOfWeek;
    var today = DateOnly.FromDateTime(DateTime.Today);

    // Add days from previous month
    var prevMonth = firstOfMonth.AddMonths(-1);
    var daysInPrevMonth = DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
    for (int i = startDayOfWeek - 1; i >= 0; i--)
    {
        var date = new DateOnly(prevMonth.Year, prevMonth.Month, daysInPrevMonth - i);
        calendarDays.Add(new CalendarDayModel
        {
            Date = date,
            IsCurrentMonth = false,
            IsToday = date == today,
            DailyTotal = dailyTotals.GetValueOrDefault(date),
            RecurringInstances = recurringByDate.GetValueOrDefault(date) ?? new()
        });
    }
    // ... more logic
}
```

### After (Display Logic Only)

```csharp
// Calendar.razor - after refactor
private async Task LoadCalendarGrid()
{
    isLoading = true;
    StateHasChanged();
    
    calendarGrid = await ApiService.GetCalendarGridAsync(Year, Month, filterAccountId);
    
    isLoading = false;
    StateHasChanged();
}

// calendarGrid.Days is ready to render - no processing needed
```

---

## Implementation Status

### ✅ Completed

#### Phase 1: Calendar Grid (Completed 2026-01)
- **CalendarController** with `/api/v1/calendar/grid` endpoint
- **ICalendarGridService** and **CalendarGridService** implementation
- Calendar.razor refactored to thin client
- All date calculations, totals, and grid building moved server-side
- DTOs: `CalendarGridDto`, `CalendarDayDto`, `CalendarSummaryDto`

#### Phase 2: Day Detail (Completed 2026-01)
- Day detail data included in calendar grid response
- Click-to-view day detail uses pre-computed data

#### Phase 3: Account Transaction List (Completed 2026-01)
- **GET /api/v1/calendar/accounts/{accountId}/transactions** endpoint added
- `GetAccountTransactionListAsync` method in `ICalendarGridService`
- DTOs: `TransactionListDto`, `TransactionListItemDto`, `TransactionListSummaryDto` in Contracts
- **AccountTransactions.razor** refactored to thin client:
  - Removed `MergeTransactionsAndRecurring()` method
  - Removed client-side duplicate detection
  - Removed client-side total/count calculations
  - Single API call returns pre-merged, pre-calculated data
  - Client only renders data and handles UI state

### Files Created/Modified

**Contracts (New DTOs):**
- `src/BudgetExperiment.Contracts/Dtos/TransactionListDto.cs`
- `src/BudgetExperiment.Contracts/Dtos/TransactionListItemDto.cs`
- `src/BudgetExperiment.Contracts/Dtos/TransactionListSummaryDto.cs`

**Application Layer:**
- `src/BudgetExperiment.Application/Services/ICalendarGridService.cs` - Added `GetAccountTransactionListAsync`
- `src/BudgetExperiment.Application/Services/CalendarGridService.cs` - ~150 lines of merge/calculate logic

**API Layer:**
- `src/BudgetExperiment.Api/Controllers/CalendarController.cs` - New endpoint

**Client Layer:**
- `src/BudgetExperiment.Client/Services/IBudgetApiService.cs` - Added method
- `src/BudgetExperiment.Client/Services/BudgetApiService.cs` - Implemented method
- `src/BudgetExperiment.Client/Pages/AccountTransactions.razor` - Refactored to thin client

**Tests:**
- `tests/BudgetExperiment.Application.Tests/CalendarGridServiceTests.cs` - 4 new tests

### Test Coverage

Unit tests for `GetAccountTransactionListAsync`:
1. `GetAccountTransactionListAsync_Returns_Pre_Merged_Transaction_And_Recurring_List`
2. `GetAccountTransactionListAsync_Excludes_Recurring_When_IncludeRecurring_False`
3. `GetAccountTransactionListAsync_Throws_When_Account_Not_Found`
4. `GetAccountTransactionListAsync_Calculates_Current_Balance_From_Initial_Plus_Transactions`

All 342 tests passing.

---

**Document Version**: 2.0  
**Created**: 2026-01-10  
**Last Updated**: 2026-01-11  
**Status**: ✅ Complete  
**Author**: Engineering Team
