# Feature 017: Running Balance Display

## Overview

Display running account balances throughout the application to give users a clear picture of their financial position over time. This includes:
1. **Calendar**: Running total at the bottom of each day cell
2. **Account Transactions**: Starting and ending balance for each day, plus running balance per transaction

## User Stories

### US-001: Calendar Running Balance
**As a** user viewing the calendar  
**I want to** see the projected account balance at the end of each day  
**So that** I can understand how my balance changes throughout the month

### US-002: Transaction List Daily Balances
**As a** user viewing an account's transactions  
**I want to** see the starting and ending balance for each day  
**So that** I can track daily money movement

### US-003: Per-Transaction Running Balance
**As a** user viewing an account's transactions  
**I want to** see the running balance after each transaction  
**So that** I can pinpoint exactly when my balance changed

### US-004: Multi-Account Calendar Balance
**As a** user viewing the calendar without an account filter  
**I want to** see the combined balance across all accounts  
**So that** I understand my total financial position

---

## Domain Concepts

### Running Balance Calculation

```
Day N Ending Balance = Day N-1 Ending Balance + Day N Transactions + Day N Projected Recurring
```

For the calendar, we need to calculate balances starting from a known point:
- **Starting Point**: Sum of all account initial balances
- **Historical Transactions**: All transactions before the calendar view start date
- **Daily Changes**: Each day's transactions and projected recurring items

### Balance Types

| Type | Description |
|------|-------------|
| **Actual Balance** | Based only on realized transactions |
| **Projected Balance** | Includes unrealized recurring items |
| **Combined Balance** | Actual + Projected (what calendar will show) |

---

## UI Design

### Calendar Day Cell (Enhanced)

```
┌─────────────────────┐
│ 15                  │
│                     │
│ Income    +$2,500   │
│ Expenses    -$450   │
│ Recurring   -$150   │
│─────────────────────│
│ Balance   $12,400   │  ← NEW: Running balance at end of day
└─────────────────────┘
```

For days with negative projected balance:
```
┌─────────────────────┐
│ 28                  │
│                     │
│ Expenses  -$1,200   │
│─────────────────────│
│ Balance     -$350   │  ← Red/warning color when negative
└─────────────────────┘
```

### Account Transactions List (Enhanced)

#### Option A: Day Headers with Balances

```
┌─────────────────────────────────────────────────────────────────────────┐
│ January 15, 2026                     Start: $12,000    End: $14,050    │
├─────────────────────────────────────────────────────────────────────────┤
│ Paycheck                    Income        +$2,500.00         $14,500   │
│ Netflix                     Entertainment    -$15.99         $14,484   │
│ 🔄 Gym (recurring)          Health          -$29.99         $14,454   │
│ Grocery Store               Food           -$125.50         $14,328   │
│ ↔️ Transfer to Savings      Transfer       -$500.00         $13,828   │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│ January 14, 2026                     Start: $12,150    End: $12,000    │
├─────────────────────────────────────────────────────────────────────────┤
│ Coffee Shop                 Food            -$5.50         $12,144    │
│ Gas Station                 Transportation -$45.00         $12,099    │
│ Amazon                      Shopping       -$99.00         $12,000    │
└─────────────────────────────────────────────────────────────────────────┘
```

#### Option B: Running Balance Column

```
┌──────────────────────────────────────────────────────────────────────────┐
│ Date       Description          Category         Amount    Balance      │
├──────────────────────────────────────────────────────────────────────────┤
│ Jan 15     Paycheck             Income        +$2,500.00    $14,500.00  │
│ Jan 15     Netflix              Entertainment    -$15.99    $14,484.01  │
│ Jan 15     🔄 Gym (recurring)   Health          -$29.99    $14,454.02  │
│ Jan 15     Grocery Store        Food           -$125.50    $14,328.52  │
│ Jan 14     Coffee Shop          Food             -$5.50    $12,144.50  │
│ Jan 14     Gas Station          Transportation  -$45.00    $12,099.50  │
│ Jan 14     Amazon               Shopping        -$99.00    $12,000.50  │
└──────────────────────────────────────────────────────────────────────────┘
```

**Recommendation**: Use Option A (day headers) for clarity, with running balance per transaction within each day.

---

## API Design

### Enhanced DTOs

#### CalendarDaySummaryDto (Enhanced)

```csharp
public sealed class CalendarDaySummaryDto
{
    // Existing properties...
    public DateOnly Date { get; set; }
    public bool IsCurrentMonth { get; set; }
    public bool IsToday { get; set; }
    public MoneyDto ActualTotal { get; set; }
    public MoneyDto ProjectedTotal { get; set; }
    public MoneyDto CombinedTotal { get; set; }
    public int TransactionCount { get; set; }
    public int RecurringCount { get; set; }
    public bool HasRecurring { get; set; }

    // NEW: Running balance at end of day
    public MoneyDto EndOfDayBalance { get; set; } = new();
    
    // NEW: Flag for negative balance warning
    public bool IsBalanceNegative { get; set; }
}
```

#### CalendarGridDto (Enhanced)

```csharp
public sealed class CalendarGridDto
{
    // Existing properties...
    public int Year { get; set; }
    public int Month { get; set; }
    public IReadOnlyList<CalendarDaySummaryDto> Days { get; set; }
    public CalendarSummaryDto MonthSummary { get; set; }

    // NEW: Starting balance for the grid (balance at start of first day)
    public MoneyDto StartingBalance { get; set; } = new();
}
```

#### TransactionListDto (Enhanced)

```csharp
public sealed class TransactionListDto
{
    // Existing properties...
    public Guid AccountId { get; set; }
    public string AccountName { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public MoneyDto InitialBalance { get; set; }
    public DateOnly InitialBalanceDate { get; set; }
    public IReadOnlyList<TransactionListItemDto> Items { get; set; }
    public TransactionListSummaryDto Summary { get; set; }

    // NEW: Daily summaries with start/end balances
    public IReadOnlyList<DailyBalanceSummaryDto> DailyBalances { get; set; } = [];
}
```

#### NEW: DailyBalanceSummaryDto

```csharp
public sealed class DailyBalanceSummaryDto
{
    public DateOnly Date { get; set; }
    public MoneyDto StartingBalance { get; set; } = new();
    public MoneyDto EndingBalance { get; set; } = new();
    public MoneyDto DayTotal { get; set; } = new();
    public int TransactionCount { get; set; }
}
```

#### TransactionListItemDto (Enhanced)

```csharp
public sealed class TransactionListItemDto
{
    // Existing properties...
    public Guid Id { get; set; }
    public string Type { get; set; }
    public DateOnly Date { get; set; }
    public string Description { get; set; }
    public MoneyDto Amount { get; set; }
    public string? Category { get; set; }
    // ... other existing properties

    // NEW: Running balance after this transaction
    public MoneyDto RunningBalance { get; set; } = new();
}
```

---

## Implementation Plan

### Phase 1: Calculate Account Starting Balance
1. Add method to calculate total balance up to a date
2. Account for initial balances across all accounts
3. Sum all transactions before the start date

### Phase 2: Calendar Running Balances
1. Calculate starting balance for calendar grid
2. Iterate through days, accumulating daily totals
3. Set `EndOfDayBalance` and `IsBalanceNegative` for each day
4. Update calendar UI to display balance

### Phase 3: Transaction List Balances
1. Calculate starting balance for date range
2. Group transactions by day
3. Calculate daily start/end balances
4. Calculate running balance for each transaction
5. Update transaction list UI

### Phase 4: Multi-Account Support
1. When no account filter: sum all account balances
2. Handle currency (assume single currency for now)

---

## Technical Details

### Balance Calculation Service

```csharp
public interface IBalanceCalculationService
{
    /// <summary>
    /// Gets the total balance across all accounts (or single account) up to but not including the specified date.
    /// </summary>
    Task<MoneyValue> GetBalanceBeforeDateAsync(
        DateOnly date, 
        Guid? accountId = null, 
        CancellationToken ct = default);
}

public sealed class BalanceCalculationService : IBalanceCalculationService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;

    public async Task<MoneyValue> GetBalanceBeforeDateAsync(
        DateOnly date, 
        Guid? accountId = null, 
        CancellationToken ct = default)
    {
        // Get accounts
        var accounts = accountId.HasValue
            ? new[] { await _accountRepository.GetByIdAsync(accountId.Value, ct) }
            : (await _accountRepository.GetAllAsync(ct)).ToArray();

        accounts = accounts.Where(a => a != null).ToArray()!;

        // Sum initial balances (only for balances dated before the target date)
        var initialBalanceSum = accounts
            .Where(a => a.InitialBalanceDate < date)
            .Sum(a => a.InitialBalance.Amount);

        // Sum all transactions before the date
        var transactionSum = 0m;
        foreach (var account in accounts)
        {
            var transactions = await _transactionRepository.GetByDateRangeAsync(
                account.InitialBalanceDate,
                date.AddDays(-1),
                account.Id,
                ct);
            
            transactionSum += transactions.Sum(t => t.Amount.Amount);
        }

        return MoneyValue.Create("USD", initialBalanceSum + transactionSum);
    }
}
```

### Enhanced CalendarGridService

```csharp
public async Task<CalendarGridDto> GetCalendarGridAsync(
    int year,
    int month,
    Guid? accountId = null,
    CancellationToken ct = default)
{
    // ... existing code to build days ...

    // Calculate running balances
    var startingBalance = await _balanceService.GetBalanceBeforeDateAsync(
        gridStartDate, 
        accountId, 
        ct);

    var runningBalance = startingBalance.Amount;

    foreach (var day in days)
    {
        // Add day's actual + projected to running balance
        runningBalance += day.CombinedTotal.Amount;
        
        day.EndOfDayBalance = new MoneyDto 
        { 
            Currency = "USD", 
            Amount = runningBalance 
        };
        day.IsBalanceNegative = runningBalance < 0;
    }

    return new CalendarGridDto
    {
        Year = year,
        Month = month,
        Days = days,
        MonthSummary = monthSummary,
        StartingBalance = new MoneyDto 
        { 
            Currency = startingBalance.Currency, 
            Amount = startingBalance.Amount 
        }
    };
}
```

### Enhanced Transaction List

```csharp
public async Task<TransactionListDto> GetAccountTransactionListAsync(
    Guid accountId,
    DateOnly startDate,
    DateOnly endDate,
    bool includeRecurring = true,
    CancellationToken ct = default)
{
    // ... existing code to get items ...

    // Calculate starting balance
    var startingBalance = await _balanceService.GetBalanceBeforeDateAsync(
        startDate, 
        accountId, 
        ct);

    // Sort items by date ascending for running balance calculation
    var sortedForBalance = items.OrderBy(i => i.Date).ThenBy(i => i.CreatedAt).ToList();

    // Calculate running balance for each item
    var runningBalance = startingBalance.Amount;
    foreach (var item in sortedForBalance)
    {
        runningBalance += item.Amount.Amount;
        item.RunningBalance = new MoneyDto { Currency = "USD", Amount = runningBalance };
    }

    // Group by day for daily summaries
    var dailyBalances = new List<DailyBalanceSummaryDto>();
    var dayBalance = startingBalance.Amount;
    
    foreach (var dayGroup in sortedForBalance.GroupBy(i => i.Date).OrderBy(g => g.Key))
    {
        var dayStart = dayBalance;
        var dayTotal = dayGroup.Sum(i => i.Amount.Amount);
        dayBalance += dayTotal;

        dailyBalances.Add(new DailyBalanceSummaryDto
        {
            Date = dayGroup.Key,
            StartingBalance = new MoneyDto { Currency = "USD", Amount = dayStart },
            EndingBalance = new MoneyDto { Currency = "USD", Amount = dayBalance },
            DayTotal = new MoneyDto { Currency = "USD", Amount = dayTotal },
            TransactionCount = dayGroup.Count()
        });
    }

    // Resort items for display (descending)
    var sortedItems = items
        .OrderByDescending(i => i.Date)
        .ThenByDescending(i => i.CreatedAt ?? DateTime.MinValue)
        .ToList();

    return new TransactionListDto
    {
        // ... existing properties ...
        DailyBalances = dailyBalances.OrderByDescending(d => d.Date).ToList()
    };
}
```

---

## UI Components

### Calendar Day Cell Update

```razor
<!-- In CalendarGrid.razor or CalendarDayCell.razor -->
<div class="calendar-day @(day.IsBalanceNegative ? "balance-negative" : "")">
    <div class="day-header">
        <span class="day-number">@day.Date.Day</span>
    </div>
    
    <div class="day-content">
        @if (day.TransactionCount > 0 || day.RecurringCount > 0)
        {
            <div class="day-totals">
                @if (day.ActualTotal.Amount != 0)
                {
                    <span class="actual">@day.ActualTotal.Amount.ToString("C")</span>
                }
                @if (day.ProjectedTotal.Amount != 0)
                {
                    <span class="projected">@day.ProjectedTotal.Amount.ToString("C")</span>
                }
            </div>
        }
    </div>
    
    <div class="day-footer">
        <span class="end-of-day-balance @(day.IsBalanceNegative ? "negative" : "")">
            @day.EndOfDayBalance.Amount.ToString("C")
        </span>
    </div>
</div>
```

### Transaction List Day Header

```razor
<!-- In AccountTransactions.razor -->
@foreach (var dailyBalance in transactionList.DailyBalances)
{
    <div class="day-header-row">
        <span class="day-date">@dailyBalance.Date.ToString("MMMM d, yyyy")</span>
        <span class="day-balances">
            <span class="balance-label">Start:</span>
            <MoneyDisplay Amount="@dailyBalance.StartingBalance.Amount" />
            <span class="balance-separator">→</span>
            <span class="balance-label">End:</span>
            <MoneyDisplay Amount="@dailyBalance.EndingBalance.Amount" />
        </span>
    </div>
    
    @foreach (var item in GetItemsForDay(dailyBalance.Date))
    {
        <TransactionRow Item="@item" ShowRunningBalance="true" />
    }
}
```

---

## Files to Create/Modify

### New Files
- `src/BudgetExperiment.Application/Services/IBalanceCalculationService.cs`
- `src/BudgetExperiment.Application/Services/BalanceCalculationService.cs`
- `src/BudgetExperiment.Contracts/Dtos/DailyBalanceSummaryDto.cs`

### Modified Files (Contracts)
- `src/BudgetExperiment.Contracts/Dtos/CalendarDaySummaryDto.cs` - Add `EndOfDayBalance`, `IsBalanceNegative`
- `src/BudgetExperiment.Contracts/Dtos/CalendarGridDto.cs` - Add `StartingBalance`
- `src/BudgetExperiment.Contracts/Dtos/TransactionListDto.cs` - Add `DailyBalances`
- `src/BudgetExperiment.Contracts/Dtos/TransactionListItemDto.cs` - Add `RunningBalance`

### Modified Files (Application)
- `src/BudgetExperiment.Application/Services/CalendarGridService.cs` - Calculate running balances
- `src/BudgetExperiment.Application/DependencyInjection.cs` - Register balance service

### Modified Files (Client)
- `src/BudgetExperiment.Client/Components/Display/CalendarGrid.razor` - Show end-of-day balance
- `src/BudgetExperiment.Client/Pages/AccountTransactions.razor` - Show daily balances
- `src/BudgetExperiment.Client/Components/Display/TransactionTable.razor` - Show running balance column
- `src/BudgetExperiment.Client/wwwroot/css/app.css` - Balance display styles

---

## CSS Styling

```css
/* Calendar day balance */
.day-footer {
    margin-top: auto;
    padding-top: var(--space-1);
    border-top: 1px solid var(--border-color);
    font-size: var(--font-size-sm);
}

.end-of-day-balance {
    font-weight: 500;
    color: var(--text-secondary);
}

.end-of-day-balance.negative {
    color: var(--color-danger);
}

.calendar-day.balance-negative {
    background-color: var(--color-danger-light);
}

/* Transaction list day headers */
.day-header-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: var(--space-2) var(--space-3);
    background-color: var(--surface-secondary);
    border-bottom: 1px solid var(--border-color);
    font-weight: 500;
}

.day-balances {
    display: flex;
    gap: var(--space-2);
    align-items: center;
    font-size: var(--font-size-sm);
}

.balance-label {
    color: var(--text-secondary);
}

.balance-separator {
    color: var(--text-muted);
}

/* Running balance column */
.running-balance {
    text-align: right;
    font-family: var(--font-mono);
    white-space: nowrap;
}

.running-balance.negative {
    color: var(--color-danger);
}
```

---

## Testing Strategy

### Unit Tests
1. `GetBalanceBeforeDateAsync` calculates correct starting balance
2. `GetBalanceBeforeDateAsync` handles single account filter
3. `GetBalanceBeforeDateAsync` includes initial balances correctly
4. Running balance accumulates correctly across days
5. Daily balance summaries group correctly
6. Negative balance flag sets correctly

### Integration Tests
1. Calendar grid includes running balances
2. Transaction list includes daily balances
3. Running balances match expected values
4. Balance calculations handle empty date ranges

---

## Edge Cases

1. **No transactions**: Balance equals sum of initial balances
2. **Future dates**: Include projected recurring in balance
3. **Account filter**: Only include filtered account's balance
4. **Negative balances**: Clearly highlight with visual warning
5. **Date range before initial balance**: Handle gracefully (may be zero or negative)
6. **Same-day ordering**: Within a day, order by `CreatedAt` for consistent running balance

---

## Performance Considerations

1. **Cache balance calculations**: Starting balance for a month rarely changes
2. **Batch queries**: Get all transactions in one query, not per-day
3. **Limit lookback**: For calendar, only calculate balances for visible grid (42 days)

---

## Success Criteria

1. ✅ Calendar shows end-of-day balance for each day
2. ✅ Negative balances are visually highlighted
3. ✅ Transaction list shows daily start/end balances
4. ✅ Each transaction shows running balance
5. ✅ Balances include projected recurring items
6. ✅ Multi-account view shows combined balance
7. ✅ Performance is acceptable (< 500ms for calendar load)

---

## Dependencies

- **Feature 009**: Account Initial Balance - Required for correct starting balance calculation

---

**Document Version**: 1.0  
**Created**: 2026-01-11  
**Status**: 📋 Planning  
**Author**: Engineering Team
