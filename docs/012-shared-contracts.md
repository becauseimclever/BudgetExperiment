# 012 - Shared Contracts Library

## Overview

This document describes the shared contracts pattern implemented to eliminate duplicate DTO/model definitions between the API and Blazor Client projects.

## Problem Statement

Before this refactor, the codebase had two separate sets of data transfer types:
- `BudgetExperiment.Application.Dtos` - DTOs used by the API layer
- `BudgetExperiment.Client.Models` - Models used by the Blazor client

This duplication caused:
1. **Maintenance overhead**: Changes to API contracts required manual updates to client models
2. **Type mismatches**: Client models could drift from API DTOs, causing runtime errors
3. **Code duplication**: Same data structures defined twice with slight variations
4. **No compile-time safety**: API changes didn't produce client build errors

## Solution

Created a new **BudgetExperiment.Contracts** project that contains shared DTOs used by both the API and Client.

### Project Structure

```
src/
├── BudgetExperiment.Contracts/
│   ├── BudgetExperiment.Contracts.csproj
│   └── Dtos/
│       ├── AccountDto.cs
│       ├── CalendarGridDto.cs
│       ├── CalendarDaySummaryDto.cs
│       ├── CalendarMonthSummaryDto.cs
│       ├── DailyTotalDto.cs
│       ├── DayDetailDto.cs
│       ├── DayDetailItemDto.cs
│       ├── DayDetailSummaryDto.cs
│       ├── MoneyDto.cs
│       ├── RecurringTransactionDto.cs
│       ├── TransactionDto.cs
│       └── TransferDto.cs
```

### Reference Chain

```
Domain → Contracts → Application → Infrastructure → Api
                 ↘             ↗
                   Client
```

Both Api and Client reference Contracts, ensuring type consistency.

### Namespace

All shared DTOs are in `BudgetExperiment.Contracts.Dtos`.

## DTO Categories

### Entity DTOs
- `AccountDto`, `AccountCreateDto` - Account entity representations
- `TransactionDto`, `TransactionCreateDto` - Transaction entity representations
- `RecurringTransactionDto`, `RecurringTransactionCreateDto`, `RecurringTransactionUpdateDto` - Recurring transaction management
- `RecurringInstanceDto`, `RecurringInstanceModifyDto` - Projected recurring instances

### Value Object DTOs
- `MoneyDto` - Currency and amount (replaces separate Amount/Currency properties)

### View Model DTOs
- `CalendarGridDto`, `CalendarDaySummaryDto`, `CalendarMonthSummaryDto` - Calendar display data
- `DayDetailDto`, `DayDetailItemDto`, `DayDetailSummaryDto` - Day detail view data
- `DailyTotalDto` - Daily aggregations

### Request/Response DTOs
- `CreateTransferRequest`, `UpdateTransferRequest` - Transfer operation requests
- `TransferResponse`, `TransferListItemResponse` - Transfer operation responses

## Benefits

1. **Single source of truth**: One definition for each data contract
2. **Compile-time safety**: API changes cause client build errors
3. **Reduced code**: Eliminated ~12 duplicate model files from Client
4. **Consistent naming**: Unified naming convention across layers
5. **Easier refactoring**: Change once, apply everywhere

## Client-Only Models

The Client retains one UI-specific model:

- `TransactionListItem` - Combines actual transactions and recurring instances for unified display in transaction lists. This is a view-level concern that doesn't belong in shared contracts.

## Migration Notes

### For Existing Code

1. Replace `using BudgetExperiment.Application.Dtos` with `using BudgetExperiment.Contracts.Dtos`
2. Replace `using BudgetExperiment.Client.Models` with `using BudgetExperiment.Contracts.Dtos`
3. Update model types:
   - `AccountModel` → `AccountDto`
   - `TransactionModel` → `TransactionDto`
   - `TransactionCreateModel` → `TransactionCreateDto`
   - `RecurringTransactionModel` → `RecurringTransactionDto`
   - `CalendarGridModel` → `CalendarGridDto`
   - `DayDetailModel` → `DayDetailDto`
   - `TransferCreateModel` → `CreateTransferRequest`
   - `TransferUpdateModel` → `UpdateTransferRequest`
   - `TransferModel` → `TransferResponse`

### MoneyDto Pattern

The `MoneyDto` class encapsulates currency and amount together:

```csharp
public sealed class MoneyDto
{
    public string Currency { get; set; } = "USD";
    public decimal Amount { get; set; }
}
```

Forms that previously had separate Amount/Currency fields now use helper properties:

```csharp
private decimal AmountValue
{
    get => Model.Amount.Amount;
    set => Model.Amount = new MoneyDto { Amount = value, Currency = Model.Amount.Currency ?? "USD" };
}
```

## Adding New DTOs

When adding new DTOs:

1. Create in `BudgetExperiment.Contracts/Dtos/`
2. Use namespace `BudgetExperiment.Contracts.Dtos`
3. Follow naming conventions:
   - Entity read: `{Entity}Dto`
   - Entity create: `{Entity}CreateDto`
   - Entity update: `{Entity}UpdateDto`
   - Requests: `{Action}{Entity}Request`
   - Responses: `{Entity}Response` or `{Entity}ListItemResponse`
4. Include XML documentation
5. Use `MoneyDto` for monetary values

## Related Files

- [copilot-instructions.md](../.github/copilot-instructions.md) - Engineering guidelines
- [002-architecture-reset.md](002-architecture-reset.md) - Overall architecture decisions
