# Feature 132: Transaction Entry — Kakeibo Selector

> **Status:** Planned

## Prerequisites

Feature 129b (Feature Flag Implementation) must be merged before implementation begins.

Feature 131 (Budget Categories — Kakeibo Category Routing) must be merged before implementation begins.

## Feature Flag

**Name:** `Features:Kakeibo:TransactionOverride`

**Default value:** `true`

This flag controls whether per-transaction Kakeibo overrides are allowed. When enabled, users can change a transaction's Kakeibo category independently from its `BudgetCategory` assignment. When disabled (for users who want pure category-driven routing), the selector is hidden and the effective Kakeibo category is always derived from the category's routing.

## Overview

This feature adds a `KakeiboSelector` component to the transaction add/edit modal, allowing users to explicitly choose whether a transaction is an Essential, Want, Cultural, or Unexpected expense at the moment of entry. The selector defaults to the `BudgetCategory`'s Kakeibo routing but allows per-transaction override via a nullable `Transaction.KakeiboOverride` field.

This deepens the mindfulness of transaction entry — each entry becomes a small act of categorization and reflection, not just account-keeping.

## Problem Statement

### Current State

- Transactions are entered with a category (e.g., "Dining") but no visibility into their broader *intent* (Wants vs. Culture).
- A birthday dinner (Culture/enrichment) is indistinguishable from a weeknight takeout (Wants/convenience) at entry time — both are categorized as "Dining."
- Category is the only classification layer; there is no space for the user to override or refine the routing on specific outlier transactions.
- The modal is transaction-focused, not reflection-focused — it doesn't invite the user to think about *why* they're spending.

### Target State

- At transaction entry (add/edit modal), after selecting a `BudgetCategory`, a **Kakeibo Selector** appears with four large, clear icons: Essentials, Wants, Culture, Unexpected.
- The default selection is the `BudgetCategory`'s routing (e.g., "Dining" → Wants).
- Clicking a different icon overrides the default and stores the choice in `Transaction.KakeiboOverride`.
- A small tooltip on first use reads: "What kind of spending is this?" — a gentle prompt for mindful reflection.
- The *effective* Kakeibo category for a transaction is computed as: `KakeiboOverride ?? BudgetCategory.KakeiboCategory` — used in all downstream aggregations (calendar, reports, monthly reflection).

## Domain Model Changes

### Domain Entity: `Transaction`

Add a nullable field for per-transaction Kakeibo override:

```csharp
public class Transaction
{
    // ... existing fields ...
    
    /// <summary>
    /// Optional override of the category's Kakeibo routing for this specific transaction.
    /// If null, the effective Kakeibo category is BudgetCategory.KakeiboCategory.
    /// If set, this overrides the category's routing.
    /// </summary>
    public KakeiboCategory? KakeiboOverride { get; set; }
}
```

### EF Core Configuration

In `TransactionConfiguration` (Infrastructure):

```csharp
builder.Property(t => t.KakeiboOverride)
    .HasConversion<int?>()
    .IsRequired(false);
```

### Database Migration

**Schema change:**

```sql
ALTER TABLE "Transactions" ADD COLUMN "KakeiboOverride" int NULL;
```

### Computed Property (Domain Layer)

Add a read-only property to `Transaction` for convenience:

```csharp
public class Transaction
{
    // ... fields ...
    public KakeiboCategory? KakeiboOverride { get; set; }
    
    /// <summary>
    /// Returns the effective Kakeibo category for this transaction.
    /// Uses override if set; otherwise falls back to category's routing.
    /// </summary>
    public KakeiboCategory GetEffectiveKakeiboCategory() => 
        KakeiboOverride ?? BudgetCategory?.KakeiboCategory ?? KakeiboCategory.Wants;
}
```

## API Changes

### Transaction Create/Edit Endpoint

**Existing endpoint:** `POST /api/v1/transactions` (create), `PUT /api/v1/transactions/{id}` (edit)

**Request DTO addition:**

```csharp
public class CreateTransactionRequest
{
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public Guid AccountId { get; set; }
    public Guid BudgetCategoryId { get; set; }
    public string? Note { get; set; }
    public string? Location { get; set; }
    public KakeiboCategory? KakeiboOverride { get; set; }  // NEW
}

public class UpdateTransactionRequest
{
    public decimal Amount { get; set; }
    public Guid BudgetCategoryId { get; set; }
    public string? Note { get; set; }
    public string? Location { get; set; }
    public KakeiboCategory? KakeiboOverride { get; set; }  // NEW
}
```

**Validation:**

- If `KakeiboOverride` is provided, it must be one of the four valid `KakeiboCategory` values.
- `KakeiboOverride` is ignored if feature flag `Features:Kakeibo:TransactionOverride` is disabled.

**Response DTO addition:**

```csharp
public class TransactionResponse
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public Guid AccountId { get; set; }
    public Guid BudgetCategoryId { get; set; }
    public string CategoryName { get; set; }
    public string? Note { get; set; }
    public string? Location { get; set; }
    public KakeiboCategory CategoryKakeiboRouting { get; set; }
    public KakeiboCategory? KakeiboOverride { get; set; }  // NEW
    public KakeiboCategory EffectiveKakeiboCategory { get; set; }  // NEW (computed)
}
```

### Transaction List Endpoint

**Existing endpoint:** `GET /api/v1/transactions`

**Response enhancement:** Include `KakeiboOverride` and `EffectiveKakeiboCategory` in transaction list items (same DTO as detail).

## UI Changes

### KakeiboSelector Component

**New Blazor component:** `src/BudgetExperiment.Client/Components/Transaction/KakeiboSelector.razor`

**Features:**

- Four large, equal-width buttons/icons representing the four Kakeibo categories.
- Each button displays:
  - A distinct icon (e.g., grocery bag for Essentials, gift for Wants, book for Culture, lightning bolt for Unexpected).
  - Category label beneath the icon.
  - Visual state: selected (filled/highlighted), unselected (outlined).
- **Visibility control:** Only rendered if feature flag `Features:Kakeibo:TransactionOverride` is true.
- **Default selection:** Highlighted button reflects the `BudgetCategory.KakeiboCategory` value passed in.
- **Tooltip:** On first render (checked via `UserSettings.HasSeenKakeiboSelectorTooltip`), displays a tooltip: "What kind of spending is this?" and marks the flag as true after 5 seconds or on interaction.
- **Callback:** Emits `SelectedKakeiboCategory` parameter back to parent (the add/edit modal).

**Styling:**

- Use existing design system colors/tokens for consistency (no new CSS).
- Ensures accessibility: keyboard navigation (arrow keys), ARIA labels, sufficient contrast.

### Transaction Add/Edit Modal

**Existing modal component:** `TransactionModal.razor`

**Integration:**

1. After the "Category" field, insert the `KakeiboSelector` component.
2. Pass the selected `BudgetCategory.KakeiboCategory` as initial value.
3. Capture the user's selection and bind to a local variable `selectedKakeiboOverride`.
4. Before submit: if `selectedKakeiboOverride != BudgetCategory.KakeiboCategory`, set the request's `KakeiboOverride` to the selected value.
5. If `selectedKakeiboOverride == BudgetCategory.KakeiboCategory`, send `KakeiboOverride = null` (no override needed).

**Example integration:**

```razor
<div class="form-group">
    <label>Category</label>
    <Select @bind-Value="form.BudgetCategoryId">
        <!-- existing options -->
    </Select>
</div>

@if (FeatureFlagService.IsEnabled("Features:Kakeibo:TransactionOverride"))
{
    <KakeiboSelector 
        InitialValue="SelectedCategory?.KakeiboCategory" 
        OnSelected="HandleKakeiboSelected" />
}

@code {
    private KakeiboCategory? selectedKakeiboOverride;
    
    private void HandleKakeiboSelected(KakeiboCategory selected)
    {
        if (selected != SelectedCategory?.KakeiboCategory)
            selectedKakeiboOverride = selected;
        else
            selectedKakeiboOverride = null;
    }
    
    private async Task Submit()
    {
        var request = new CreateTransactionRequest
        {
            // ... other fields ...
            KakeiboOverride = selectedKakeiboOverride
        };
        // submit
    }
}
```

### User Preferences

Add a flag to `UserSettings`:

```csharp
public bool HasSeenKakeiboSelectorTooltip { get; set; } = false;
```

Used to show the tooltip only on first encounter.

## Feature Flag

**Name:** `Features:Kakeibo:TransactionOverride`

**Default:** `true` (override capability is enabled by default)

**Rationale:** Basic Kakeibo selector is always visible and functional. This flag controls whether users can change the override — when disabled, the selector shows but is read-only (displays the category's routing; no override storage).

**For future:** Users who prefer strict category-driven routing can disable this flag in settings to hide the selector entirely.

## Acceptance Criteria

- [ ] `KakeiboCategory? KakeiboOverride` field added to `Transaction` entity.
- [ ] Database migration creates `KakeiboOverride` column (int null, default null).
- [ ] `GetEffectiveKakeiboCategory()` computed property returns `KakeiboOverride ?? BudgetCategory.KakeiboCategory ?? Wants`.
- [ ] Create/Edit transaction endpoints accept `KakeiboOverride` in request DTO.
- [ ] Create/Edit endpoints return `KakeiboOverride` and `EffectiveKakeiboCategory` in response.
- [ ] `KakeiboSelector` component renders four clear category buttons with icons and labels.
- [ ] Selector defaults to `BudgetCategory.KakeiboCategory` on initial render.
- [ ] Selector is only visible/functional if feature flag `Features:Kakeibo:TransactionOverride` is enabled.
- [ ] Tooltip "What kind of spending is this?" displays on first use (checked via `HasSeenKakeiboSelectorTooltip`).
- [ ] Modal integration: captures selector value and sends override only if different from category routing.
- [ ] If override equals category routing, request sends `KakeiboOverride = null`.
- [ ] Transaction list endpoint returns `EffectiveKakeiboCategory` for all items.
- [ ] All existing tests pass; new unit tests for effective category computation.
- [ ] Accessibility: keyboard navigation, ARIA labels, sufficient contrast on selector buttons.

## Implementation Order

1. **Add `KakeiboOverride` field to `Transaction` entity** (Domain).
2. **Create database migration** with schema change.
3. **Implement `GetEffectiveKakeiboCategory()` method** on `Transaction`.
4. **Update transaction DTOs** (Create/Update request, response).
5. **Update Create/Edit transaction endpoints** to handle override in request and return in response.
6. **Create `KakeiboSelector.razor` component** (Blazor UI).
7. **Add `HasSeenKakeiboSelectorTooltip` to `UserSettings`** (Domain/DB).
8. **Integrate `KakeiboSelector` into transaction modal** (Blazor UI).
9. **Implement feature flag gating** in modal and service layer.
10. **Add tests** for effective category computation, endpoint validation, component interaction, and feature flag behavior.

**Dependencies:** Feature 131 must be complete; Feature 129b must be merged (feature flag infrastructure).
