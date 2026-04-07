# Feature 142: Uncategorized Transactions — Kakeibo Display

> **Status:** Planned

## Prerequisites

- Feature 129b (Feature Flag Implementation) must be merged before implementation begins.
- Feature 131 (KakeiboCategory on BudgetCategory) must be completed.

---

## Feature Flag

**Flag Name:** None  
**Note:** This is informational display only — always useful, no opt-out needed. No feature flag required.

---

## Overview

The `/uncategorized` page allows users to categorize transactions that have no category. When a user selects a category from the dropdown, the page now shows the resulting Kakeibo bucket as confirmation feedback. This provides immediate visual reassurance that the category choice has the expected Kakeibo intent.

For example:
- User selects "Dining" for an uncategorized transaction
- The page displays: "Dining → **Wants** ✓" (with Wants shown in green)
- User confirms the categorization

Optionally (advanced feature), power users can directly override the Kakeibo category during categorization if the default mapping doesn't match their intent.

---

## Domain Model Changes

**None.** This feature is purely display-side. The category assignment logic is existing (`Transaction.Category` assignment). The Kakeibo routing is computed on the fly from `BudgetCategory.KakeiboCategory`.

---

## API Changes

**None.** The categorization endpoint (`PUT /api/v1/transactions/{id}` or similar) remains unchanged. The category is assigned via the existing mechanism.

**Optional Enhancement:** If supporting direct Kakeibo override during categorization, extend the request DTO:

```csharp
public class CategorizationRequest
{
    public Guid CategoryId { get; set; }
    public string? KakeiboOverride { get; set; } // Optional: override the category's default Kakeibo bucket
}
```

But this is **optional** and can be deferred to a follow-up feature.

---

## UI Changes

**Modified Component:** `UncategorizedTransactionsView.razor` (or similar)

**New UI Elements:**
1. **Category Dropdown with Kakeibo Preview**
   - When user hovers over or selects a category from the dropdown, display the Kakeibo badge next to it
   - Format: "Dining **→ Wants**" (with Wants in green)
   - Update the preview in real-time as user hovers/selects

2. **Confirmation Feedback**
   - After user confirms categorization, display a brief confirmation message: "✓ Dining (Wants)" 
   - Show for 1–2 seconds, then fade out
   - Use the Kakeibo color scheme for the badge (green for Wants, etc.)

3. **Optional: Direct Kakeibo Override Button**
   - If the preview shows a Kakeibo bucket the user disagrees with, display a small "Override Kakeibo?" button or link
   - Clicking opens a secondary menu with four buttons (Essentials/Wants/Culture/Unexpected)
   - Selected override is applied to the categorization (both category and override saved together)
   - This is **optional** and can be deferred

**Implementation Details:**
- The category dropdown is populated from `GET /api/v1/categories?type=Expense` (already exists)
- Enrich the category list with `KakeiboCategory` field (no new API call needed — fetch all categories once and cache)
- On hover/selection, show the Kakeibo badge by looking up the `KakeiboCategory` in the cached list
- All logic is client-side; no API changes required for the basic implementation

---

## Acceptance Criteria

- [ ] Category dropdown on `/uncategorized` page shows Kakeibo category next to each category option
- [ ] Kakeibo category is resolved client-side from the category list (no new API calls)
- [ ] Kakeibo badge is displayed with correct color (blue/green/purple/orange)
- [ ] Badge format: "Dining → **Wants**" with Wants in the appropriate color
- [ ] Preview updates in real-time as user hovers/selects categories
- [ ] After categorization confirmation, a feedback message shows (e.g., "✓ Dining (Wants)") for 1–2 seconds
- [ ] Confirmation message uses the Kakeibo color scheme
- [ ] Optional: Direct override button is present and functional (allows user to choose different Kakeibo bucket)
- [ ] Optional: Override saves both category and Kakeibo override to the transaction
- [ ] All existing categorization logic remains unchanged
- [ ] All unit and integration tests pass

---

## Implementation Notes

- **Client-Side Enrichment:** Fetch the full category list (with `KakeiboCategory`) once at component initialization and cache it. This avoids repeated API calls and keeps the UI responsive.
- **Null Handling:** If a category has `KakeiboCategory: null` (Income/Transfer), don't show a preview — these categories aren't Kakeibo-routed.
- **Color Scheme:** Use consistent Kakeibo colors:
  - Essentials: blue
  - Wants: green
  - Culture: purple
  - Unexpected: orange/red
- **Optional Override:** The direct override feature (power user option) is not critical for the base implementation. The confirmation feedback is the main UX improvement. Override can be added in a follow-up if needed.
- **Accessibility:** Badge previews should be announced via screen readers (e.g., "Dining, Wants category"). Keyboard navigation should work smoothly (tab through categories, arrow keys for selection).
- **Performance:** Caching the category list is sufficient — no complex data fetching needed. Keep the component lightweight.

