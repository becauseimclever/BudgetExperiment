# Feature 138: Transactions List — Kakeibo Filter and Badge

> **Status:** Done

## Prerequisites

- Feature 129b (Feature Flag Implementation) must be merged before implementation begins.
- Feature 131 (KakeiboCategory on BudgetCategory) must be completed.
- Feature 132 (KakeiboOverride on Transaction) must be completed.

---

## Feature Flag

**Flag Name:** `Kakeibo:TransactionFilter`  
**Default Value:** `true` (visible by default; controls whether filter dropdown and badges appear)  
**When Enabled:** Kakeibo filter dropdown and category badges are displayed on the transactions list page

---

## Overview

The transactions list (`/transactions`) gains a Kakeibo category filter and visual badges showing the effective Kakeibo category for each transaction. Users can:
- Filter transactions by Kakeibo bucket (All / Essentials / Wants / Culture / Unexpected)
- See at a glance which Kakeibo bucket each transaction belongs to via a colored badge

The **effective Kakeibo category** is resolved server-side:
- If `Transaction.KakeiboOverride` is set, use that
- Otherwise, use `BudgetCategory.KakeiboCategory`
- For Income and Transfer categories (which have no Kakeibo mapping), the field is `null` and no badge is shown

---

## Domain Model Changes

**None.** The `Transaction.KakeiboOverride` field and `BudgetCategory.KakeiboCategory` field are already defined in Features 131 and 132. This feature consumes them.

---

## API Changes

**Modified Endpoint:**
```
GET /api/v1/transactions?kakeiboCategory=Wants
```

**Query Parameters:**
- `kakeiboCategory: string?` (optional) — filter by Kakeibo bucket name (Essentials / Wants / Culture / Unexpected). If `null` or omitted, all transactions are returned.

**Modified Response DTO:**
```csharp
public class TransactionDto
{
    public string? EffectiveKakeiboCategory { get; set; } // null for Income/Transfer
}
```

**Implementation Notes:**
- Server resolves `EffectiveKakeiboCategory` from `Transaction.KakeiboOverride ?? BudgetCategory.KakeiboCategory`
- When filtering by `kakeiboCategory`, use the effective Kakeibo category (not just the category's default)
- Filtering logic: if `kakeiboCategory` is provided, exclude transactions with `EffectiveKakeiboCategory != kakeiboCategory`
- For Income/Transfer transactions, `EffectiveKakeiboCategory` is always `null` and they never match a Kakeibo filter

---

## UI Changes

**Modified Component:** `TransactionsList.razor`

**New UI Elements:**
1. **Kakeibo Filter Dropdown** (above transaction table)
   - Label: "Kakeibo Category"
   - Options: "All", "Essentials", "Wants", "Culture", "Unexpected"
   - Default selected: "All"
   - Triggers a new API request when changed
   - Controlled by feature flag `Features:Kakeibo:TransactionFilter` — hidden if disabled

2. **Kakeibo Badge** (in each transaction row)
   - Small colored icon/badge showing the transaction's effective Kakeibo category
   - Color scheme:
     - Essentials: blue
     - Wants: green
     - Culture: purple
     - Unexpected: orange/red
   - Tooltip on hover: "Essentials (from Groceries)" or "Wants (override)"
   - Only shown for Expense transactions; Income/Transfer rows have no badge
   - Controlled by feature flag — hidden if disabled

**Filter State Management:**
- Store selected `kakeiboCategory` in component state
- Pass as query parameter to API on filter change
- Persist filter selection to the query string (so filter state survives page reload)

---

## Acceptance Criteria

- [x] Feature flag `Kakeibo:TransactionFilter` is defined and controls badge/filter visibility
- [x] API endpoint `GET /api/v1/transactions?kakeiboCategory=Wants` correctly filters transactions by effective Kakeibo category
- [x] `TransactionDto` includes `EffectiveKakeiboCategory` field (resolved server-side)
- [x] Effective Kakeibo category is resolved correctly: override takes precedence over category default
- [x] Income and Transfer transactions have `null` EffectiveKakeiboCategory and are excluded from Kakeibo filters
- [x] Kakeibo filter dropdown appears above transaction table with options: All / Essentials / Wants / Culture / Unexpected
- [x] Kakeibo badge (colored icon) displays on each Expense transaction row
- [x] Badge tooltip shows category source (e.g., "Wants (from Dining)" or "Culture (override)")
- [x] Filter state persists to the query string across page reloads
- [x] When filter is changed, page fetches new results via API and updates display
- [x] Feature flag hides dropdown and badges when disabled
- [x] All unit and integration tests pass; OpenAPI spec is updated

---

## Implementation Notes

- **Badge Appearance:** Keep badges small and unobtrusive — they are informational, not interactive. Use a 16×16px icon or a small colored pill (e.g., "Wants" in green text on light background).
- **Filter Performance:** The filter is client-side state management triggering API queries. No caching needed at the client level (rely on API caching if expensive).
- **Null Handling:** Ensure the DTO serializer handles `null` for `EffectiveKakeiboCategory` gracefully (JSON should omit or set to `null`, not error).
- **Consistency:** Use the same color scheme for Kakeibo badges across all features (137, 143, etc.) to build visual coherence.
- **Accessibility:** Badge tooltips should be keyboard-accessible (not just on hover). Use ARIA labels if appropriate.

