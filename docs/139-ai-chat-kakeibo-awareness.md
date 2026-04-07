# Feature 139: AI Chat — Kakeibo Awareness

> **Status:** Planned

## Prerequisites

- Feature 129b (Feature Flag Implementation) must be merged before implementation begins.
- Feature 131 (KakeiboCategory on BudgetCategory) must be completed.
- Feature 132 (KakeiboOverride on Transaction) must be completed.
- Feature 138 (Transactions List Kakeibo Filter) should be completed for query support.

---

## Feature Flag

**Flag Name:** `Features:AI:ChatAssistant`  
**Default Value:** `true` (already enabled from Feature 129 audit)  
**Note:** This feature enhances the existing chat assistant; no new flag is needed. The enhancements are behavioral (Kakeibo awareness in transaction suggestions and clarification logic).

---

## Overview

The AI Chat Assistant becomes Kakeibo-aware. When the AI creates or suggests a transaction, it:
1. **Confirms Kakeibo Category** — If the matched category's `KakeiboCategory` is deterministic (not the fallback Wants), includes it in the confirmation message to set user expectations.
2. **Asks for Clarification** — If the AI cannot confidently determine a category, or if the user hasn't confirmed the category, it may request Kakeibo clarification: *"Is this Essentials, Wants, Culture, or Unexpected?"* via a `ClarificationNeededAction` with type `AskKakeiboCategory`.
3. **Suggests Kakeibo in Reasoning** — The AI's explanation includes Kakeibo intent when creating transactions, e.g., *"Dinner at Olive Garden — I'll categorize as Dining (Wants). Does that sound right?"*
4. **Supports Kakeibo Queries** — Users can ask Kakeibo-aware questions like *"How much did I spend on Wants this week?"* and the backend supports aggregated Kakeibo queries via the filter mechanism (Feature 138).

---

## Domain Model Changes

**Entity:** `ClarificationNeededAction`  
Extend the existing action type to include a new case:

```csharp
public abstract class ClarificationNeededAction
{
    public class AskCategory : ClarificationNeededAction
    {
        // Existing: Ask user to confirm the suggested category
    }

    // NEW:
    public class AskKakeiboCategory : ClarificationNeededAction
    {
        public string Description { get; set; } // Transaction description
        public List<string> SuggestedCategories { get; set; } // Up to 2 suggested Kakeibo buckets
    }
}
```

**No database changes.** `ClarificationNeededAction` is a transient DTO/event type used within chat session logic.

---

## API Changes

**Modified Endpoint:**
```
POST /api/v1/ai/chat
```

**Response Change — ChatActionDto:**

Extend the existing action union to include the new clarification type. Example response:

```json
{
  "message": "I found a transaction for you...",
  "action": {
    "type": "ClarificationNeededAskKakeiboCategory",
    "description": "Dinner at Olive Garden",
    "suggestedCategories": ["Wants", "Culture"]
  }
}
```

or (successful case with Kakeibo confirmation):

```json
{
  "message": "I'm creating: Dinner at Olive Garden, $35, Dining category (Wants). Confirm?",
  "action": {
    "type": "ConfirmTransaction",
    "transactionDraft": {
      "date": "2026-04-15",
      "amount": 35.00,
      "category": "Dining",
      "kakeiboCategory": "Wants"
    }
  }
}
```

**Implementation Notes:**
- Existing endpoints remain unchanged; responses are extended with new fields
- When AI suggests a category, query the `BudgetCategory` to retrieve its `KakeiboCategory`
- If `KakeiboCategory` is null (Income/Transfer), omit it from the response
- If `KakeiboCategory` is the fallback Wants (default), the AI may ask for confirmation or proceed with caution

---

## UI Changes

**Modified Component:** `AIChatWindow.razor`

**New Behaviors:**
1. **Display Kakeibo Category in Confirmation Message**
   - When AI suggests a transaction and the category has a deterministic Kakeibo bucket, show it in the message preview: *"Dining (Wants)"*
   - Color the Kakeibo category label with the corresponding badge color (green for Wants, etc.)

2. **Handle `AskKakeiboCategory` Clarification Action**
   - Display a new clarification UI: *"Is this Essentials, Wants, Culture, or Unexpected?"*
   - Show four buttons (one per bucket) or a segmented control
   - On user selection, send the choice back to the API (extend chat message with `userKakeiboChoice`)
   - Proceed to transaction confirmation after Kakeibo choice

3. **Kakeibo Query Support**
   - When user asks a natural language question like *"How much on Wants?"*, the chat should recognize the intent and query the API
   - Use the feature flag `Features:Kakeibo:TransactionFilter` (Feature 138) to enable `/transactions?kakeiboCategory=Wants` queries
   - Return aggregated results in the chat response: *"You spent $387.50 on Wants this week."*

---

## Acceptance Criteria

- [ ] AI confirmation messages include Kakeibo category when deterministic (e.g., "Dining (Wants)")
- [ ] New `ClarificationNeededAction.AskKakeiboCategory` type is defined and handled in chat logic
- [ ] When AI cannot determine Kakeibo category (or user input is ambiguous), the chat requests clarification with four-button/segmented control UI
- [ ] User can select Kakeibo category directly in clarification UI and proceed
- [ ] Chat response DTO supports the new clarification action type (serializes/deserializes correctly)
- [ ] AI suggests Kakeibo intent in its reasoning text (e.g., "Dinner at Olive Garden — I'll categorize as Dining (Wants)")
- [ ] Chat recognizes natural language Kakeibo queries ("How much on Wants?", "Culture spending this month?")
- [ ] Kakeibo queries are answered via aggregation using the Kakeibo filter API (Feature 138)
- [ ] Kakeibo badges in confirmation messages use correct colors (blue/green/purple/orange)
- [ ] All unit and integration tests pass; API spec is updated

---

## Implementation Notes

- **AI Intent Detection:** Extend the AI action builder to detect Kakeibo category determinism. If the suggested category's `KakeiboCategory` is null or the fallback bucket, request clarification.
- **Natural Language Processing:** The AI should recognize patterns like "How much did I spend on [bucket]?" where [bucket] ∈ {Essentials, Wants, Culture, Unexpected}. Map user language to the four buckets.
- **Fallback Strategy:** If the user never confirms a Kakeibo category, default to the category's mapped bucket. Don't block transaction creation on Kakeibo clarification — it's helpful but optional.
- **Query Integration:** Reuse the existing `GET /api/v1/transactions?kakeiboCategory=X` endpoint (Feature 138) to answer user questions. The AI service calls this endpoint, aggregates the results, and formats a natural response.
- **Color Consistency:** Use the same Kakeibo color scheme (blue/green/purple/orange) across all chat UI elements for visual continuity.

