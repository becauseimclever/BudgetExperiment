# Feature 131: Budget Categories — Kakeibo Category Routing

> **Status:** Planned

## Prerequisites

Feature 129b (Feature Flag Implementation) must be merged before implementation begins.

## Overview

This feature establishes the foundational bridge between BudgetExperiment's existing category system and Kakeibo philosophy by adding a `KakeiboCategory` field to the `BudgetCategory` entity. Every expense category is routed to exactly one of the four Kakeibo buckets (Essentials, Wants, Culture, Unexpected), enabling all downstream features (transaction categorization, calendar heatmaps, monthly reflection) to aggregate spending by philosophical intent rather than by literal category name.

This is the single most important foundation piece. Everything else depends on `BudgetCategory.KakeiboCategory` existing.

## Problem Statement

### Current State

- `BudgetCategory` entities exist (Groceries, Dining, Utilities, etc.), but they carry no information about their *nature* or *intent*.
- All expense categories are treated equally in aggregations and reporting — a dollar in Dining is the same numerical weight as a dollar in Housing.
- Users have no way to reflect on spending by whether it was necessary, desired, enriching, or unexpected.
- When new categories are created, there is no guidance about their role in a balanced financial life.

### Target State

- Every expense category has an explicit `KakeiboCategory` field (Essentials, Wants, Culture, Unexpected).
- `BudgetCategory.KakeiboCategory` is `null` for `CategoryType.Income` and `CategoryType.Transfer` (only Expense categories are routed).
- A one-time **Kakeibo Setup Wizard** on first login post-migration asks users to review and confirm Kakeibo routing for all their existing expense categories.
- The migration applies smart defaults based on category names (Groceries → Essentials, Dining → Wants, Education → Culture, anything one-off → Unexpected).
- Users can change a category's Kakeibo routing at any time via the category edit UI; the change is retroactive (all past transactions via that category re-aggregate under the new bucket instantly, with no data mutations).

## Domain Model Changes

### New Enum: `KakeiboCategory`

```csharp
// src/BudgetExperiment.Domain/Kakeibo/KakeiboCategory.cs
public enum KakeiboCategory
{
    Essentials = 1,   // 必要 — things needed to live
    Wants      = 2,   // 欲しい — things enjoyed but not essential
    Culture    = 3,   // 文化 — things that enrich mind and spirit
    Unexpected = 4,   // 予期しない — things that were not planned
}
```

### Domain Entity: `BudgetCategory`

Add a nullable field:

```csharp
public class BudgetCategory
{
    // ... existing fields ...
    
    /// <summary>
    /// The Kakeibo category bucket for this category.
    /// Only set for Expense categories; null for Income and Transfer.
    /// </summary>
    public KakeiboCategory? KakeiboCategory { get; set; }
}
```

### EF Core Configuration

In `BudgetCategoryConfiguration` (Infrastructure):

```csharp
builder.Property(c => c.KakeiboCategory)
    .HasConversion<int?>()
    .IsRequired(false);
```

### Database Migration

**Schema change:**

```sql
ALTER TABLE "BudgetCategories" ADD COLUMN "KakeiboCategory" int NULL;
```

**Seed logic (do NOT use HasData):**

```csharp
// Seed via startup seeder, not migration
// Default: Wants (most neutral)
// Smart defaults from Feature 128 mapping:
// - Essentials: Groceries, Utilities, Gas, Transportation, Healthcare, Insurance, Housing, Rent
// - Wants: Dining, Entertainment, Shopping, Subscriptions, Health & Fitness, Pets, Travel
// - Culture: Education, Charity
// - Unexpected: any category named "Unexpected" or tagged as one-off
// - Fallback: Wants
```

## API Changes

### Category Edit Endpoint

**Existing endpoint:** `PUT /api/v1/categories/{id}`

**Request DTO addition:**

```csharp
public class UpdateBudgetCategoryRequest
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public decimal? MonthlyBudget { get; set; }
    public KakeiboCategory? KakeiboCategory { get; set; }  // NEW
}
```

**Validation:**

- `KakeiboCategory` must be non-null for `CategoryType.Expense`.
- `KakeiboCategory` must be null for `CategoryType.Income` and `CategoryType.Transfer`.

**Response DTO addition:**

```csharp
public class BudgetCategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public CategoryType Type { get; set; }
    public decimal? MonthlyBudget { get; set; }
    public KakeiboCategory? KakeiboCategory { get; set; }  // NEW
}
```

### New Endpoint: Get All Categories with Kakeibo

**GET /api/v1/categories?includeKakeibo=true**

Returns all categories including their Kakeibo routing. Used by onboarding/setup wizard.

## UI Changes

### Category Edit Form

- For **Expense** categories: add a dropdown field "Kakeibo Category" with options: Essentials, Wants, Culture, Unexpected.
- For **Income** and **Transfer** categories: field is hidden (read-only note: "This category type is not part of Kakeibo tracking").
- Tooltip: "Kakeibo is a Japanese budgeting philosophy. Choose what kind of spending this is: Essentials (needs), Wants (desires), Culture (enrichment), or Unexpected (surprises)."

### Kakeibo Setup Wizard (One-Time)

**Trigger:** First login after migration, or via `UserSettings.HasCompletedKakeiboSetup` flag.

**Flow:**

1. Welcome screen: "Let's set up Kakeibo categories for your spending."
2. Display all existing **Expense** categories grouped by their assigned Kakeibo bucket (using migration defaults).
3. For each category, show a small card with the category name and a dropdown to change its Kakeibo routing.
4. Provide a help panel on the right explaining each bucket with examples:
   - **Essentials:** Groceries, Utilities, Housing, Transportation, Healthcare
   - **Wants:** Dining, Entertainment, Shopping, Subscriptions, Pets
   - **Culture:** Education, Books, Charity, Museums
   - **Unexpected:** Emergency repairs, surprise medical bills
5. Confirmation step: "Review your assignments" with all categories and their buckets listed.
6. Submit button: saves all changes, sets `UserSettings.HasCompletedKakeiboSetup = true`.
7. Dismissal is allowed at any point (though encouraged to complete); incomplete setup can be resumed from category settings page or account settings.

## Feature Flag

**None.** This is a foundation feature — it must always be on once Feature 128 ships. There is no toggle.

## Acceptance Criteria

- [ ] `KakeiboCategory` enum exists with four values (Essentials, Wants, Culture, Unexpected).
- [ ] `BudgetCategory.KakeiboCategory` field is added and nullable.
- [ ] Database migration creates the schema change with correct data type (int null).
- [ ] Seed/startup logic applies smart defaults from Feature 128 mapping table (name-based routing).
- [ ] Migration default for unrecognized category names is `Wants`.
- [ ] Category edit endpoint validates: non-null for Expense, null for Income/Transfer.
- [ ] Category edit form shows Kakeibo dropdown (Expense only) with helpful tooltip.
- [ ] Kakeibo Setup Wizard displays on first login post-migration (controlled by `HasCompletedKakeiboSetup` flag).
- [ ] Wizard shows all Expense categories grouped by their assigned Kakeibo bucket.
- [ ] Wizard allows user to change any category's routing before final confirmation.
- [ ] Wizard sets `HasCompletedKakeiboSetup = true` on successful completion.
- [ ] Category routing changes are retroactive — no per-transaction data changes; aggregations computed from current category routing.
- [ ] All existing tests pass; no breaking changes to category entity or API.

## Implementation Order

1. **Create domain enum and update `BudgetCategory` entity** (Domain layer, no external deps).
2. **Create database migration** with schema and startup seed logic.
3. **Update category DTOs and validation** (API contracts layer).
4. **Enhance category edit endpoint** with Kakeibo routing validation and response.
5. **Create Kakeibo Setup Wizard component** (Blazor UI) and integration in onboarding flow.
6. **Update category edit form** (Blazor UI) to show Kakeibo dropdown.
7. **Add tests** for enum, entity, seeding, API validation, and UI component interactions.

**Critical dependency:** This feature must be complete and merged before Feature 132 (Transaction Entry — Kakeibo Selector), Feature 133 (Onboarding), Feature 134 (Calendar), Feature 135 (Monthly Reflection), and Feature 136 (Kaizen Goals) begin implementation.
