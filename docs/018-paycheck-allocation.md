# Feature 018: Paycheck Allocation Suggestions

## Overview

Provide users with actionable suggestions for how much to set aside from each paycheck to cover their recurring bills. When a bill's due date doesn't align with paycheck frequency, the system calculates the appropriate amount to save per pay period. Warnings are displayed when bills cannot be reconciled with available income.

## Problem Statement

Many users receive paychecks on a different schedule than their bills are due. For example:
- **Biweekly paycheck** (26 per year) vs **monthly rent** (12 per year)
- **Weekly paycheck** (52 per year) vs **quarterly insurance** (4 per year)
- **Monthly salary** vs **annual property tax**

This mismatch makes budgeting difficult. Users need to know: "How much should I set aside from each paycheck to cover this bill?"

## User Stories

### US-001: View Per-Paycheck Allocation
**As a** user  
**I want to** see how much I need to set aside per paycheck for each recurring bill  
**So that** I can plan my spending and ensure bills are covered

### US-002: Configure Paycheck Frequency
**As a** user  
**I want to** specify my paycheck frequency (weekly, biweekly, monthly, etc.)  
**So that** allocations are calculated correctly for my pay schedule

### US-003: Configure Paycheck Amount
**As a** user  
**I want to** enter my typical paycheck amount  
**So that** the system can warn me if my income is insufficient

### US-004: View Total Allocation Summary
**As a** user  
**I want to** see the total amount I need to set aside per paycheck across all bills  
**So that** I know my overall obligations per pay period

### US-005: See Insufficient Income Warning
**As a** user  
**I want to** be warned when my paycheck cannot cover all bills  
**So that** I can take action before falling behind

### US-006: See Cannot Reconcile Warning
**As a** user  
**I want to** see a clear warning when bills exceed my total annual income  
**So that** I understand the severity of the budget shortfall

### US-007: View Remaining After Bills
**As a** user  
**I want to** see how much remains from each paycheck after covering bills  
**So that** I can plan discretionary spending

### US-008: Filter by Account
**As a** user  
**I want to** view allocations for bills from a specific account  
**So that** I can manage bills paid from different accounts separately

### US-009: Create Recurring Transfer for Allocation
**As a** user  
**I want to** create a recurring transfer to set aside the calculated amount per paycheck  
**So that** I can automatically save for bills in a dedicated account

### US-010: Quick Setup All Allocations
**As a** user  
**I want to** set up a single recurring transfer for the total allocation amount  
**So that** I can quickly automate saving for all my bills at once

---

## Calculation Logic

### Core Formula

All recurring amounts are normalized to an annual basis, then divided by the number of pay periods per year.

**Annual Amount by Frequency:**
| Frequency  | Annual Multiplier |
|------------|-------------------|
| Daily      | 365               |
| Weekly     | 52                |
| BiWeekly   | 26                |
| Monthly    | 12                |
| Quarterly  | 4                 |
| Yearly     | 1                 |

**Pay Periods per Year:**
| Paycheck Frequency | Periods/Year |
|--------------------|--------------|
| Weekly             | 52           |
| BiWeekly           | 26           |
| Monthly            | 12           |

**Per-Paycheck Allocation Formula:**
```
AnnualBillAmount = BillAmount × AnnualMultiplier(BillFrequency)
AllocationPerPaycheck = AnnualBillAmount ÷ PeriodsPerYear(PaycheckFrequency)
```

### Example Calculations

**Example 1: Monthly Rent with Biweekly Paycheck**
- Rent: $1,200/month
- Annual: $1,200 × 12 = $14,400
- Per paycheck: $14,400 ÷ 26 = **$553.85**

**Example 2: Quarterly Insurance with Biweekly Paycheck**
- Insurance: $600/quarter
- Annual: $600 × 4 = $2,400
- Per paycheck: $2,400 ÷ 26 = **$92.31**

**Example 3: Weekly Groceries with Biweekly Paycheck**
- Groceries: $200/week
- Annual: $200 × 52 = $10,400
- Per paycheck: $10,400 ÷ 26 = **$400.00**

### Warning Conditions

#### Insufficient Income Warning
Triggered when: `TotalAllocationPerPaycheck > PaycheckAmount`

```
Shortfall = TotalAllocationPerPaycheck - PaycheckAmount
```

Message: "Your bills require $X per paycheck, but your paycheck is only $Y. Shortfall: $Z per paycheck."

#### Cannot Reconcile Warning
Triggered when: `TotalAnnualBills > TotalAnnualIncome`

This is a severe condition indicating the user's yearly income cannot cover yearly bills regardless of allocation strategy.

Message: "Your annual bills ($X) exceed your annual income ($Y). Please review your recurring expenses."

---

## Data Model

### Domain Layer

#### BillInfo Value Object
Lightweight representation of a bill for allocation calculation.

```csharp
public sealed record BillInfo
{
    public string Description { get; init; }
    public MoneyValue Amount { get; init; }
    public RecurrenceFrequency Frequency { get; init; }
    public Guid? SourceRecurringTransactionId { get; init; }
    
    public static BillInfo Create(string description, MoneyValue amount, RecurrenceFrequency frequency);
    public static BillInfo FromRecurringTransaction(RecurringTransaction recurring);
}
```

#### PaycheckAllocation Value Object
Result of calculating allocation for a single bill.

```csharp
public sealed record PaycheckAllocation
{
    public BillInfo Bill { get; init; }
    public MoneyValue AmountPerPaycheck { get; init; }
    public MoneyValue AnnualAmount { get; init; }
}
```

#### PaycheckAllocationWarning Value Object
Represents a warning about allocation issues.

```csharp
public sealed record PaycheckAllocationWarning
{
    public AllocationWarningType Type { get; init; }
    public string Message { get; init; }
    public MoneyValue? Amount { get; init; }  // Shortfall amount if applicable
}

public enum AllocationWarningType
{
    InsufficientIncome,
    CannotReconcile,
    NoBillsConfigured,
    NoIncomeConfigured
}
```

#### PaycheckAllocationSummary Value Object
Complete summary of all allocations.

```csharp
public sealed record PaycheckAllocationSummary
{
    public IReadOnlyList<PaycheckAllocation> Allocations { get; init; }
    public MoneyValue TotalPerPaycheck { get; init; }
    public MoneyValue? PaycheckAmount { get; init; }
    public MoneyValue RemainingPerPaycheck { get; init; }
    public MoneyValue Shortfall { get; init; }
    public MoneyValue TotalAnnualBills { get; init; }
    public MoneyValue? TotalAnnualIncome { get; init; }
    public IReadOnlyList<PaycheckAllocationWarning> Warnings { get; init; }
    public bool HasWarnings => Warnings.Count > 0;
    public bool CannotReconcile { get; init; }
    public RecurrenceFrequency PaycheckFrequency { get; init; }
}
```

#### PaycheckAllocationCalculator Domain Service

```csharp
public sealed class PaycheckAllocationCalculator
{
    public PaycheckAllocation CalculateAllocation(BillInfo bill, RecurrenceFrequency paycheckFrequency);
    
    public PaycheckAllocationSummary CalculateAllocationSummary(
        IEnumerable<BillInfo> bills,
        RecurrenceFrequency paycheckFrequency,
        MoneyValue? paycheckAmount = null);
}
```

### Application Layer

#### IPaycheckAllocationService

```csharp
public interface IPaycheckAllocationService
{
    Task<PaycheckAllocationSummaryDto> GetAllocationSummaryAsync(
        RecurrenceFrequency paycheckFrequency,
        decimal? paycheckAmount,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);
}
```

### Contracts Layer

#### PaycheckAllocationDto

```csharp
public sealed class PaycheckAllocationDto
{
    public string Description { get; set; }
    public MoneyDto BillAmount { get; set; }
    public string BillFrequency { get; set; }
    public MoneyDto AmountPerPaycheck { get; set; }
    public MoneyDto AnnualAmount { get; set; }
    public Guid? RecurringTransactionId { get; set; }
}
```

#### PaycheckAllocationWarningDto

```csharp
public sealed class PaycheckAllocationWarningDto
{
    public string Type { get; set; }
    public string Message { get; set; }
    public MoneyDto? Amount { get; set; }
}
```

#### PaycheckAllocationSummaryDto

```csharp
public sealed class PaycheckAllocationSummaryDto
{
    public List<PaycheckAllocationDto> Allocations { get; set; }
    public MoneyDto TotalPerPaycheck { get; set; }
    public MoneyDto? PaycheckAmount { get; set; }
    public MoneyDto RemainingPerPaycheck { get; set; }
    public MoneyDto Shortfall { get; set; }
    public MoneyDto TotalAnnualBills { get; set; }
    public MoneyDto? TotalAnnualIncome { get; set; }
    public List<PaycheckAllocationWarningDto> Warnings { get; set; }
    public bool HasWarnings { get; set; }
    public bool CannotReconcile { get; set; }
    public string PaycheckFrequency { get; set; }
}
```

---

## API Design

### Endpoint

```
GET /api/v1/allocations/paycheck?frequency={frequency}&amount={amount}&accountId={accountId}
```

**Query Parameters:**
| Parameter   | Type    | Required | Description |
|-------------|---------|----------|-------------|
| frequency   | string  | Yes      | Paycheck frequency: Weekly, BiWeekly, Monthly |
| amount      | decimal | No       | Paycheck amount (for warning calculations) |
| accountId   | guid    | No       | Filter to bills from specific account |

**Response:** `200 OK` with `PaycheckAllocationSummaryDto`

**Example Request:**
```
GET /api/v1/allocations/paycheck?frequency=BiWeekly&amount=2000
```

**Example Response:**
```json
{
  "allocations": [
    {
      "description": "Rent",
      "billAmount": { "currency": "USD", "amount": 1200.00 },
      "billFrequency": "Monthly",
      "amountPerPaycheck": { "currency": "USD", "amount": 553.85 },
      "annualAmount": { "currency": "USD", "amount": 14400.00 },
      "recurringTransactionId": "..."
    },
    {
      "description": "Car Insurance",
      "billAmount": { "currency": "USD", "amount": 600.00 },
      "billFrequency": "Quarterly",
      "amountPerPaycheck": { "currency": "USD", "amount": 92.31 },
      "annualAmount": { "currency": "USD", "amount": 2400.00 },
      "recurringTransactionId": "..."
    }
  ],
  "totalPerPaycheck": { "currency": "USD", "amount": 646.16 },
  "paycheckAmount": { "currency": "USD", "amount": 2000.00 },
  "remainingPerPaycheck": { "currency": "USD", "amount": 1353.84 },
  "shortfall": { "currency": "USD", "amount": 0.00 },
  "totalAnnualBills": { "currency": "USD", "amount": 16800.00 },
  "totalAnnualIncome": { "currency": "USD", "amount": 52000.00 },
  "warnings": [],
  "hasWarnings": false,
  "cannotReconcile": false,
  "paycheckFrequency": "BiWeekly"
}
```

---

## UI Design

### Navigation

Add "Paycheck Planner" link to the navigation menu under the recurring transactions section.

### Page Layout

```
┌─────────────────────────────────────────────────────────────┐
│  Paycheck Planner                                           │
├─────────────────────────────────────────────────────────────┤
│  Configure Your Paycheck                                    │
│  ┌─────────────────┐  ┌─────────────────────┐              │
│  │ Frequency: [▼]  │  │ Amount: [$_______]  │  [Calculate] │
│  │ BiWeekly        │  │                     │              │
│  └─────────────────┘  └─────────────────────┘              │
├─────────────────────────────────────────────────────────────┤
│  ⚠️ WARNING: Your bills require $1,153.85 per paycheck...   │  (if applicable)
├─────────────────────────────────────────────────────────────┤
│  Summary                                                    │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Total per Paycheck:     $646.16                     │  │
│  │  Your Paycheck:          $2,000.00                   │  │
│  │  Remaining:              $1,353.84                   │  │
│  │  Annual Bills:           $16,800.00                  │  │
│  └──────────────────────────────────────────────────────┘  │
├─────────────────────────────────────────────────────────────┤
│  Bill Breakdown                                             │
│  ┌─────────────────────────────────────────────────────────┐│
│  │ Bill          │ Amount    │ Frequency │ Per Paycheck   ││
│  ├───────────────┼───────────┼───────────┼────────────────┤│
│  │ Rent          │ $1,200.00 │ Monthly   │ $553.85   [⟳]  ││
│  │ Car Insurance │ $600.00   │ Quarterly │ $92.31    [⟳]  ││
│  │ Groceries     │ $200.00   │ Weekly    │ $400.00   [⟳]  ││
│  └─────────────────────────────────────────────────────────┘│
├─────────────────────────────────────────────────────────────┤
│  Set Aside Money                                            │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  From Account: [Checking ▼]  To Account: [Bills ▼]   │  │
│  │                                                      │  │
│  │  [Set Aside Total ($646.16/paycheck)]                │  │
│  │                                                      │  │
│  │  This will create a BiWeekly recurring transfer      │  │
│  │  to automatically save for your bills.               │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### Action: Create Recurring Transfer

When the user clicks "Set Aside Total" or an individual allocation's "Set Aside" button:

1. **Modal/Form appears** with pre-filled values:
   - Amount: The allocation amount (total or individual)
   - Frequency: Matches the configured paycheck frequency
   - From Account: User selects source (checking) account
   - To Account: User selects destination (bills/savings) account
   - Description: Auto-generated (e.g., "Bills fund - Rent" or "Bills fund - Total")
   - Start Date: Next paycheck date (user can adjust)

2. **On confirm**: Creates a `RecurringTransfer` via existing API

3. **Success feedback**: Shows confirmation and optionally navigates to Recurring Transfers page

### Warning Display

Warnings should be prominently displayed:

- **InsufficientIncome**: Yellow warning banner with shortfall amount
- **CannotReconcile**: Red error banner indicating serious budget issue
- **NoBillsConfigured**: Info message prompting user to add recurring transactions
- **NoIncomeConfigured**: Info message explaining that warnings require paycheck amount

### Responsive Behavior

- On mobile, summary cards stack vertically
- Bill breakdown table becomes a card list on small screens

---

## Implementation Plan

### Phase 1: Domain Layer (TDD) ✅ COMPLETED
1. ✅ Write tests for `BillInfo` value object
2. ✅ Implement `BillInfo`
3. ✅ Write tests for `PaycheckAllocation` value object
4. ✅ Implement `PaycheckAllocation`
5. ✅ Write tests for `PaycheckAllocationWarning` value object
6. ✅ Implement `PaycheckAllocationWarning`
7. ✅ Write tests for `PaycheckAllocationSummary` value object
8. ✅ Implement `PaycheckAllocationSummary`
9. ✅ Write tests for `PaycheckAllocationCalculator`
10. ✅ Implement `PaycheckAllocationCalculator`
11. ✅ Create `AllocationWarningType` enum

**Files Created:**
- `src/BudgetExperiment.Domain/BillInfo.cs`
- `src/BudgetExperiment.Domain/AllocationWarningType.cs`
- `src/BudgetExperiment.Domain/PaycheckAllocation.cs`
- `src/BudgetExperiment.Domain/PaycheckAllocationWarning.cs`
- `src/BudgetExperiment.Domain/PaycheckAllocationSummary.cs`
- `src/BudgetExperiment.Domain/PaycheckAllocationCalculator.cs`
- `tests/BudgetExperiment.Domain.Tests/BillInfoTests.cs`
- `tests/BudgetExperiment.Domain.Tests/PaycheckAllocationTests.cs`
- `tests/BudgetExperiment.Domain.Tests/PaycheckAllocationWarningTests.cs`
- `tests/BudgetExperiment.Domain.Tests/PaycheckAllocationSummaryTests.cs`
- `tests/BudgetExperiment.Domain.Tests/PaycheckAllocationCalculatorTests.cs`

### Phase 2: Application Layer ✅ COMPLETED
1. ✅ Create DTOs in Contracts project
2. ✅ Add mapping methods to `DomainToDtoMapper`
3. ✅ Write tests for `PaycheckAllocationService`
4. ✅ Implement `PaycheckAllocationService`
5. ✅ Add DI registration

**Files Created:**
- `src/BudgetExperiment.Contracts/Dtos/PaycheckAllocationDto.cs`
- `src/BudgetExperiment.Contracts/Dtos/PaycheckAllocationWarningDto.cs`
- `src/BudgetExperiment.Contracts/Dtos/PaycheckAllocationSummaryDto.cs`
- `src/BudgetExperiment.Application/Services/IPaycheckAllocationService.cs`
- `src/BudgetExperiment.Application/Services/PaycheckAllocationService.cs`
- `tests/BudgetExperiment.Application.Tests/PaycheckAllocationServiceTests.cs`

**Files Modified:**
- `src/BudgetExperiment.Application/Mapping/DomainToDtoMapper.cs` (added ToDto methods)
- `src/BudgetExperiment.Application/DependencyInjection.cs` (registered service)

### Phase 3: API
1. Create `AllocationsController`
2. Write API integration tests

### Phase 4: Client
1. Create `PaycheckPlanner.razor` page
2. Add navigation link
3. Implement configuration form
4. Implement summary display
5. Implement warning display
6. Implement bill breakdown table
7. Implement "Set Aside" action buttons
8. Implement recurring transfer creation modal/form
9. Wire up to existing recurring transfer API

### Phase 5: Polish
1. Add account filter dropdown
2. Add CSS styling consistent with design system
3. Add client-side validation
4. Add loading states

---

## Future Enhancements

### Persist Paycheck Configuration
Store user's paycheck frequency and amount in AppSettings (from Feature 016) so they don't have to re-enter each time.

### Income from Recurring Transactions
Auto-detect income by looking at positive recurring transactions and use those as paycheck sources.

### Multiple Income Sources
Support users with multiple jobs/income sources with different frequencies.

### Projection View
Show a calendar-style view of when bills are due relative to paychecks, highlighting potential cash flow issues.

### Export/Print
Allow users to export or print their paycheck allocation plan.

---

## Testing Strategy

### Unit Tests (Domain)
- `BillInfo` creation and validation
- `PaycheckAllocationCalculator` with various frequency combinations
- Warning generation logic
- Edge cases (zero amounts, single bill, many bills)

### Unit Tests (Application)
- Service correctly fetches recurring transactions
- Service correctly filters by account
- Service correctly maps to DTOs

### Integration Tests (API)
- Endpoint returns correct status codes
- Query parameters are validated
- Response format matches contract

### Component Tests (Client)
- Form validation
- Warning display
- Empty state handling
- Set aside button enables recurring transfer creation
- Modal pre-fills correct amount and frequency

---

## Dependencies

- Feature 004: Recurring Transactions (must exist to have bills)
- Feature 008: Recurring Transfers (required for "Set Aside" functionality)
- Feature 016: User Settings (optional, for persisting paycheck config)

---

## Design Decisions

1. **Should we include recurring transfers as "bills"?** **No.** Since recurring transfers are the mechanism for setting aside allocation amounts, including them as bills would create a feedback loop. Only recurring transactions (actual expenses) are considered bills.

2. **How to handle mixed currencies?** **Deferred.** Currently assume single currency (USD). Multi-currency support will be addressed in a future iteration.

3. **Should there be a "recommended" paycheck frequency?** **Yes.** When the user has recurring income transactions (positive amounts), auto-suggest a paycheck frequency based on detected income patterns.

4. **Include one-time upcoming bills?** **No.** Strictly recurring transactions only for now. One-time bills may be added in a future enhancement.
