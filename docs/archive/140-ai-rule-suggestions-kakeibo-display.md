# Feature 140: AI Rule Suggestions — Kakeibo Display

> **Status:** Done

## Prerequisites

- Feature 129b (Feature Flag Implementation) must be merged before implementation begins.
- Feature 131 (KakeiboCategory on BudgetCategory) must be completed.

---

## Feature Flag

**Flag Name:** `Features:AI:RuleSuggestions`  
**Default Value:** `true` (already enabled from Feature 129 audit)  
**Note:** This feature enhances the existing rule suggestions; no new flag is needed. The enhancement is display-only (adding Kakeibo info to suggestion DTOs).

---

## Overview

The AI Rule Suggestions page (`/ai/suggestions`) displays Kakeibo routing alongside suggested transaction categories. Users can now see:
- The suggested category (e.g., "Dining")
- The effective Kakeibo bucket that category maps to (e.g., "Wants")
- A visual badge or label showing the mapping

Additionally, the AI can **flag surprising Kakeibo routings** and even **suggest Kakeibo overrides** when the merchant context suggests a different intent (e.g., a subscription categorized as Wants, but the context suggests it might be Culture or Unexpected).

---

## Domain Model Changes

**None.** The suggestion DTO is a transient value type. No database changes needed.

---

## API Changes

**Modified Endpoint:**
```
GET /api/v1/ai/suggestions
```

**Modified Response DTO:**

```csharp
public class CategorySuggestionDto
{
    public Guid Id { get; set; }
    public string Description { get; set; }
    public string SuggestedCategory { get; set; }
    // NEW FIELD:
    public string? SuggestedKakeiboCategory { get; set; } // e.g., "Wants", null if category is Income/Transfer
    public int ConfidenceScore { get; set; }
    public string Reasoning { get; set; }
}
```

**Optional Enhancement — Kakeibo Override Suggestions:**

The AI service may optionally suggest a Kakeibo override if merchant context indicates a different intent:

```csharp
public class CategorySuggestionDto
{
    // ... existing fields ...
    public string? SuggestedKakeiboCategory { get; set; }
    // NEW OPTIONAL FIELD:
    public string? KakeiboOverrideSuggestion { get; set; } // e.g., "Culture" if the AI believes a subscription might be enrichment rather than pleasure
    public string? KakeiboOverrideReasoning { get; set; } // Explanation of why the override is suggested
}
```

**Implementation Notes:**
- When fetching category suggestions, query the `BudgetCategory` to retrieve the suggested category's `KakeiboCategory`
- If the category is Income/Transfer, `SuggestedKakeiboCategory` is `null`
- The `KakeiboOverrideSuggestion` is optional and only populated if the AI's merchant knowledge or user history suggests a mismatch (e.g., Netflix is usually Wants, but the user also has "Netflix Gift Subscription" in Culture from prior years)
- No API breaking change — new fields are optional and backward-compatible

---

## UI Changes

**Modified Component:** `AISuggestionsView.razor` (or similar)

**New UI Elements:**
1. **Kakeibo Badge in Suggestion Card**
   - Display the `SuggestedKakeiboCategory` as a colored badge next to the category name
   - Format: "Dining → **Wants**" (with Wants displayed in green)
   - Tooltip on hover: "Based on the Dining category"

2. **Kakeibo Override Flag (Optional)**
   - If `KakeiboOverrideSuggestion` is populated, display a secondary badge or callout: "Consider Culture instead?"
   - Include the reasoning: "This looks like a subscription for learning materials."
   - Allow user to apply the override directly via a button (saving both the category AND the Kakeibo override)

3. **Visual Consistency**
   - Use consistent Kakeibo color scheme (blue/green/purple/orange) across all badges
   - Keep the UI informational, not overwhelming — badges are additive, not required to understand the suggestion

**Interaction:**
- When user accepts a suggestion, if a `KakeiboOverrideSuggestion` was shown, ask: "Apply the category + Kakeibo override?"
- Allow user to accept category only, or category + override

---

## Acceptance Criteria

- [ ] `CategorySuggestionDto` includes `SuggestedKakeiboCategory` field (populated from the suggested category's `KakeiboCategory`)
- [ ] `SuggestedKakeiboCategory` is null for Income/Transfer categories
- [ ] API endpoint `GET /api/v1/suggestions` returns the new field correctly
- [ ] UI displays Kakeibo badge next to category name in suggestion cards
- [ ] Kakeibo badge uses correct color (blue/green/purple/orange) and is visually consistent across features
- [ ] Badge tooltip explains the source (e.g., "Wants (from Dining)")
- [ ] Optional: `KakeiboOverrideSuggestion` and `KakeiboOverrideReasoning` are populated when AI detects context-based mismatches
- [ ] Optional: UI displays override callout with reasoning when override suggestion is present
- [ ] Optional: User can apply category + override in a single action
- [ ] All unit and integration tests pass; OpenAPI spec is updated

---

## Implementation Notes

- **Kakeibo Override Suggestions:** This is an optional "nice-to-have" enhancement. The core requirement is simply displaying the category's existing Kakeibo routing. Override suggestions can be added in a follow-up if the AI's merchant knowledge base supports context-based reasoning.
- **Merchant Context:** The AI's `MerchantKnowledgeBase` already groups merchants by category family. Extend this with simple heuristics: if a merchant is tagged "Subscription" and the category is Wants, but user history shows similar merchants in Culture, suggest the override.
- **Color Scheme:** Keep Kakeibo colors consistent:
  - Essentials: blue
  - Wants: green
  - Culture: purple
  - Unexpected: orange/red
- **Backward Compatibility:** Existing clients ignore the new `SuggestedKakeiboCategory` field if they don't know about it. No breaking changes.
- **Performance:** Fetching `BudgetCategory` for each suggestion is negligible (should be cached). If performance becomes a concern, batch-load all suggested categories at once.

