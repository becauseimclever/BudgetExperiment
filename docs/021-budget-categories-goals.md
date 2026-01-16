# Feature 021: Budget Categories & Goals

## Overview

Implement a comprehensive budgeting system that allows users to define spending categories, set monthly budget targets, and track progress against those goals. This feature transforms the application from a transaction tracker into a true budgeting tool.

## Problem Statement

Currently, the application tracks transactions with free-form category text but provides no:
- Standardized category management
- Budget targets or spending limits
- Progress tracking against goals
- Alerts when approaching or exceeding budget

Users need visibility into *where* their money goes and whether spending aligns with their financial goals.

---

## User Stories

### Category Management

#### US-001: Create Budget Category
**As a** user  
**I want to** create custom spending categories  
**So that** I can organize my transactions meaningfully

#### US-002: Edit Budget Category
**As a** user  
**I want to** edit category names and properties  
**So that** I can refine my categorization over time

#### US-003: Delete Budget Category
**As a** user  
**I want to** delete unused categories  
**So that** my category list stays manageable

#### US-004: View All Categories
**As a** user  
**I want to** see all my categories in one place  
**So that** I can manage my budgeting structure

#### US-005: Category Icons/Colors
**As a** user  
**I want to** assign icons and colors to categories  
**So that** I can quickly identify them visually

### Budget Goals

#### US-006: Set Monthly Budget
**As a** user  
**I want to** set a monthly spending limit for each category  
**So that** I can control my spending

#### US-007: View Budget Progress
**As a** user  
**I want to** see how much I've spent vs. budgeted per category  
**So that** I know if I'm on track

#### US-008: Budget Overview Dashboard
**As a** user  
**I want to** see a summary of all category budgets  
**So that** I get a complete picture of my monthly spending

#### US-009: Over-Budget Alert
**As a** user  
**I want to** be alerted when I exceed a category budget  
**So that** I can adjust my spending behavior

#### US-010: Approaching Budget Warning
**As a** user  
**I want to** be warned when I'm approaching my budget limit (e.g., 80%)  
**So that** I can be more careful with remaining funds

### Transaction Integration

#### US-011: Assign Category to Transaction
**As a** user  
**I want to** assign a budget category when creating/editing transactions  
**So that** spending is tracked against the right budget

#### US-012: Categorize Recurring Transactions
**As a** user  
**I want to** assign categories to recurring transactions  
**So that** projected spending includes budget impact

#### US-013: Bulk Categorize Transactions
**As a** user  
**I want to** categorize multiple transactions at once  
**So that** I can quickly organize imported transactions

#### US-014: Category Suggestions
**As a** user  
**I want to** receive category suggestions based on description  
**So that** categorization is faster and more consistent

---

## Domain Model

### BudgetCategory Entity

```csharp
public sealed class BudgetCategory
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Icon { get; private set; }  // Icon identifier (e.g., "shopping", "food", "transport")
    public string? Color { get; private set; } // Hex color code
    public CategoryType Type { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    // Navigation
    public ICollection<BudgetGoal> Goals { get; private set; } = new List<BudgetGoal>();

    public static BudgetCategory Create(string name, CategoryType type, string? icon = null, string? color = null);
    public void Update(string name, string? icon, string? color, int sortOrder);
    public void Deactivate();
    public void Activate();
}

public enum CategoryType
{
    Expense,    // Spending category (negative transactions)
    Income,     // Income category (positive transactions)
    Transfer    // Internal transfers (excluded from budget calculations)
}
```

### BudgetGoal Entity

```csharp
public sealed class BudgetGoal
{
    public Guid Id { get; private set; }
    public Guid CategoryId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public MoneyValue TargetAmount { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    // Navigation
    public BudgetCategory Category { get; private set; } = null!;

    public static BudgetGoal Create(Guid categoryId, int year, int month, MoneyValue targetAmount);
    public void UpdateTarget(MoneyValue newTarget);
}
```

### BudgetProgress Value Object

```csharp
public sealed record BudgetProgress
{
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string? CategoryIcon { get; init; }
    public string? CategoryColor { get; init; }
    public MoneyValue TargetAmount { get; init; }
    public MoneyValue SpentAmount { get; init; }
    public MoneyValue RemainingAmount { get; init; }
    public decimal PercentUsed { get; init; }
    public BudgetStatus Status { get; init; }
    public int TransactionCount { get; init; }
}

public enum BudgetStatus
{
    OnTrack,        // < 80% used
    Warning,        // 80-99% used
    OverBudget,     // >= 100% used
    NoBudgetSet     // No target for this category/month
}
```

### Repository Interfaces

```csharp
public interface IBudgetCategoryRepository : IReadRepository<BudgetCategory>, IWriteRepository<BudgetCategory>
{
    Task<BudgetCategory?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<BudgetCategory>> GetActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BudgetCategory>> GetByTypeAsync(CategoryType type, CancellationToken ct = default);
}

public interface IBudgetGoalRepository : IReadRepository<BudgetGoal>, IWriteRepository<BudgetGoal>
{
    Task<BudgetGoal?> GetByCategoryAndMonthAsync(Guid categoryId, int year, int month, CancellationToken ct = default);
    Task<IReadOnlyList<BudgetGoal>> GetByMonthAsync(int year, int month, CancellationToken ct = default);
    Task<IReadOnlyList<BudgetGoal>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default);
}
```

---

## API Design

### Category Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/categories` | Get all categories |
| GET | `/api/v1/categories/{id}` | Get category by ID |
| POST | `/api/v1/categories` | Create new category |
| PUT | `/api/v1/categories/{id}` | Update category |
| DELETE | `/api/v1/categories/{id}` | Delete/deactivate category |

### Budget Goal Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/budgets?year={year}&month={month}` | Get all budget goals for month |
| GET | `/api/v1/budgets/{categoryId}?year={year}&month={month}` | Get specific category budget |
| PUT | `/api/v1/budgets/{categoryId}` | Set/update budget goal |
| DELETE | `/api/v1/budgets/{categoryId}?year={year}&month={month}` | Remove budget goal |

### Budget Progress Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/budgets/progress?year={year}&month={month}` | Get budget progress for all categories |
| GET | `/api/v1/budgets/progress/{categoryId}?year={year}&month={month}` | Get progress for specific category |
| GET | `/api/v1/budgets/summary?year={year}&month={month}` | Get overall budget summary |

### DTOs

```csharp
// Category DTOs
public sealed class BudgetCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public string Type { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public sealed class BudgetCategoryCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public string Type { get; set; } = "Expense";
}

public sealed class BudgetCategoryUpdateDto
{
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public int? SortOrder { get; set; }
}

// Budget Goal DTOs
public sealed class BudgetGoalDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public MoneyDto TargetAmount { get; set; } = new();
}

public sealed class BudgetGoalSetDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public MoneyDto TargetAmount { get; set; } = new();
}

// Progress DTOs
public sealed class BudgetProgressDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryIcon { get; set; }
    public string? CategoryColor { get; set; }
    public MoneyDto TargetAmount { get; set; } = new();
    public MoneyDto SpentAmount { get; set; } = new();
    public MoneyDto RemainingAmount { get; set; } = new();
    public decimal PercentUsed { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
}

public sealed class BudgetSummaryDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public MoneyDto TotalBudgeted { get; set; } = new();
    public MoneyDto TotalSpent { get; set; } = new();
    public MoneyDto TotalRemaining { get; set; } = new();
    public decimal OverallPercentUsed { get; set; }
    public int CategoriesOnTrack { get; set; }
    public int CategoriesWarning { get; set; }
    public int CategoriesOverBudget { get; set; }
    public IReadOnlyList<BudgetProgressDto> CategoryProgress { get; set; } = [];
}
```

---

## UI Design

### Categories Page (`/categories`)

```
┌─────────────────────────────────────────────────────────────────┐
│ Budget Categories                              [+ Add Category] │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ EXPENSE CATEGORIES                                              │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 🛒 Groceries           $500/month      [Edit] [Delete]      │ │
│ │ 🏠 Housing             $1,500/month    [Edit] [Delete]      │ │
│ │ 🚗 Transportation      $300/month      [Edit] [Delete]      │ │
│ │ 🎬 Entertainment       $200/month      [Edit] [Delete]      │ │
│ │ 💡 Utilities           $150/month      [Edit] [Delete]      │ │
│ │ 🍽️  Dining Out          $250/month      [Edit] [Delete]      │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ INCOME CATEGORIES                                               │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 💰 Salary              (no budget)     [Edit] [Delete]      │ │
│ │ 📈 Investments         (no budget)     [Edit] [Delete]      │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Budget Overview Page (`/budget`)

```
┌─────────────────────────────────────────────────────────────────┐
│ Budget Overview                    [◀ Dec 2025] January 2026 [▶]│
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ SUMMARY                                                         │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │  Total Budgeted    Total Spent    Remaining                 │ │
│ │    $2,900.00       $1,847.50      $1,052.50                 │ │
│ │                                                             │ │
│ │  ████████████████████░░░░░░░░░░░░░░░  64% used             │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ BY CATEGORY                                                     │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 🛒 Groceries        $423/$500                               │ │
│ │    ████████████████░░░░  85% ⚠️                             │ │
│ │                                                             │ │
│ │ 🏠 Housing          $1,500/$1,500                           │ │
│ │    ████████████████████  100% ✓                            │ │
│ │                                                             │ │
│ │ 🚗 Transportation   $187/$300                               │ │
│ │    ████████████░░░░░░░░  62%                                │ │
│ │                                                             │ │
│ │ 🎬 Entertainment    $245/$200                               │ │
│ │    ████████████████████████  123% 🔴                        │ │
│ │                                                             │ │
│ │ 💡 Utilities        $0/$150                                 │ │
│ │    ░░░░░░░░░░░░░░░░░░░░  0%                                 │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Category Selector (Transaction Forms)

Replace free-form category text input with dropdown:

```
┌─────────────────────────────────────────────────────────────────┐
│ Category                                                        │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 🛒 Groceries                                            ▼  │ │
│ └─────────────────────────────────────────────────────────────┘ │
│   ┌───────────────────────────────────────────────────────────┐ │
│   │ 🛒 Groceries                                              │ │
│   │ 🏠 Housing                                                │ │
│   │ 🚗 Transportation                                         │ │
│   │ 🎬 Entertainment                                          │ │
│   │ 💡 Utilities                                              │ │
│   │ 🍽️  Dining Out                                             │ │
│   │ ──────────────────────                                    │ │
│   │ + Create new category...                                  │ │
│   └───────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

---

## Implementation Plan

### Phase 1: Domain & Infrastructure
- [x] Create `BudgetCategory` entity
- [x] Create `BudgetGoal` entity
- [x] Create `BudgetProgress` value object
- [x] Create repository interfaces
- [x] Create EF Core configurations
- [x] Add database migration
- [x] Implement repositories
- [x] Write unit tests for domain logic (50 tests)

### Phase 2: Application Services
- [ ] Create `IBudgetCategoryService` interface
- [ ] Implement `BudgetCategoryService`
- [ ] Create `IBudgetGoalService` interface
- [ ] Implement `BudgetGoalService`
- [ ] Create `IBudgetProgressService` interface
- [ ] Implement `BudgetProgressService`
- [ ] Write service unit tests

### Phase 3: API Layer
- [ ] Create `CategoriesController`
- [ ] Create `BudgetsController`
- [ ] Add DTOs to Contracts project
- [ ] Add validation
- [ ] Write API integration tests

### Phase 4: Client - Categories
- [ ] Add category endpoints to `IBudgetApiService`
- [ ] Create Categories page (`/categories`)
- [ ] Create CategoryForm component
- [ ] Create CategoryList component
- [ ] Add category CRUD functionality

### Phase 5: Client - Budget Overview
- [ ] Add budget endpoints to `IBudgetApiService`
- [ ] Create Budget page (`/budget`)
- [ ] Create BudgetSummary component
- [ ] Create BudgetProgressBar component
- [ ] Create CategoryBudgetCard component

### Phase 6: Transaction Integration
- [ ] Update transaction forms with category dropdown
- [ ] Update recurring transaction forms
- [ ] Migrate existing free-text categories to `CategoryId` reference
- [ ] Add "Uncategorized" handling

### Phase 7: Navigation & Polish
- [ ] Add Categories link to navigation
- [ ] Add Budget link to navigation
- [ ] Add budget status indicators to calendar
- [ ] Add over-budget alerts

---

## Migration Strategy

### Existing Category Data

Current transactions have a free-form `Category` string field. Migration approach:

1. **Extract unique categories** from existing transactions
2. **Create BudgetCategory records** for each unique value
3. **Add `CategoryId` column** to Transaction table
4. **Populate `CategoryId`** based on string matching
5. **Keep `Category` string** temporarily for rollback safety
6. **Remove `Category` string** in future cleanup migration

### Default Categories

Seed common categories on first run:

```csharp
var defaultCategories = new[]
{
    ("Groceries", CategoryType.Expense, "cart", "#4CAF50"),
    ("Housing", CategoryType.Expense, "home", "#2196F3"),
    ("Transportation", CategoryType.Expense, "car", "#FF9800"),
    ("Utilities", CategoryType.Expense, "bolt", "#9C27B0"),
    ("Entertainment", CategoryType.Expense, "film", "#E91E63"),
    ("Dining Out", CategoryType.Expense, "utensils", "#FF5722"),
    ("Healthcare", CategoryType.Expense, "heart", "#F44336"),
    ("Shopping", CategoryType.Expense, "shopping-bag", "#673AB7"),
    ("Salary", CategoryType.Income, "briefcase", "#4CAF50"),
    ("Investments", CategoryType.Income, "trending-up", "#2196F3"),
    ("Other Income", CategoryType.Income, "plus-circle", "#9E9E9E"),
    ("Transfer", CategoryType.Transfer, "repeat", "#607D8B"),
};
```

---

## Success Criteria

1. Users can create, edit, and delete budget categories
2. Users can set monthly budget targets per category
3. Budget progress is calculated and displayed accurately
4. Transactions can be assigned to categories via dropdown
5. Over-budget and warning states are clearly indicated
6. Calendar optionally shows budget status per day/month
7. All existing tests pass; new features have 80%+ coverage

---

## Future Enhancements

- **Rollover budgets**: Unused budget carries to next month
- **Annual budgets**: Yearly targets (insurance, subscriptions)
- **Budget templates**: Copy last month's budget to new month
- **Category rules**: Auto-categorize based on description patterns
- **Spending insights**: AI-powered spending analysis
- **Budget sharing**: Share budgets between users (after auth)

---

## Related Documents

- [015-realize-recurring-items.md](015-realize-recurring-items.md) - Recurring items affect budget projections
- [017-running-balance-display.md](017-running-balance-display.md) - Balance calculations
- [018-paycheck-allocation.md](018-paycheck-allocation.md) - Paycheck vs. bills (related budgeting concept)
