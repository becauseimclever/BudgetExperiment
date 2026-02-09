# Feature 051: AI Assistant Calendar Context
> **Status:** 🗒️ Planning  
> **Priority:** Medium  
> **Dependencies:** Chat/AI Assistant (Complete), Calendar Page (Complete)

## Overview

Ensure the AI Assistant uses the currently selected calendar date or range as context for all suggestions and actions. When a user is viewing a specific date on the calendar and asks the AI to "add a transaction," the AI should pre-fill the transaction date with the selected calendar date rather than defaulting to today.

This feature makes AI-driven workflows more relevant, time-aware, and reduces friction by eliminating the need for users to specify dates already visible in the UI context.

## Problem Statement

Currently, the AI Assistant does not know what date or month the user is viewing on the calendar. When a user selects a day on the calendar and then asks the AI to "add $50 for groceries," the AI defaults to today's date rather than the selected date. This creates a disconnect between what the user sees and what the AI assumes.

### Current State

**What Exists:**
- `ChatContextService` on the client tracks current account (ID + name), category (ID + name), and page type
- `ChatPageContext` class has properties for `CurrentAccountId`, `CurrentAccountName`, `CurrentCategoryId`, `CurrentCategoryName`, `PageType`, and a `GetContextSummary()` method
- `ChatContext` record in the Application layer already has `CurrentDate` (DateOnly?), `CurrentAccountId`, `CurrentAccountName`, `CurrentCategoryId`, `CurrentCategoryName`, `CurrentPage`
- `NaturalLanguageParser.FormatContext` already handles `CurrentDate` in prompt building (emits "Viewing date: {date}") — but no caller populates it
- `IChatService.SendMessageAsync` already accepts an optional `ChatContext?` parameter — controller passes `null`
- Calendar page tracks `selectedDate` (DateOnly?), `currentDate` (DateOnly, viewed month), `selectedAccountId`, and `filterAccountId` internally
- `ChatPanel.razor` injects `IChatContextService` and subscribes to `ContextChanged`, displays `GetContextSummary()`
- AI suggestions work but are date-unaware

**Current Gaps:**
1. `ChatPageContext` has no calendar date properties (`CalendarViewedYear`, `CalendarViewedMonth`, `SelectedDate`)
2. Calendar page doesn't inject `IChatContextService` or update it when dates change
3. `SendMessageRequest` DTO has only `Content` — no context data
4. `ChatContextDto` does not exist in Contracts
5. API controller explicitly passes `null` for context to `ChatService`
6. `ChatApiService.SendMessageAsync` only accepts `(Guid sessionId, string content)` — no context parameter

### Target State

- Calendar page updates `ChatContextService` with selected date and viewed month/year
- Chat panel displays context hint showing current calendar date
- AI suggestions pre-fill dates based on calendar context
- When user says "add a transaction" while viewing Jan 15th, the AI uses Jan 15th as the date
- Context flows: Calendar → ChatContextService → API → AI Parser

---

## User Stories

### Calendar Date Context

#### US-051-001: AI uses selected calendar date for transactions
**As a** user viewing a specific day on the calendar  
**I want** the AI to use that date when I add a transaction  
**So that** I don't have to specify the date manually

**Acceptance Criteria:**
- [ ] When a day is selected on the calendar, `ChatContextService` is updated with that date
- [ ] AI transaction creation uses the selected date instead of today
- [ ] If no date is selected, AI defaults to today (current behavior)
- [ ] Context is cleared when navigating away from calendar

#### US-051-002: AI aware of viewed calendar month
**As a** user viewing a specific month on the calendar  
**I want** the AI to understand what time period I'm looking at  
**So that** suggestions are relevant to that period

**Acceptance Criteria:**
- [ ] `ChatContextService` includes the currently viewed year/month
- [ ] AI prompt includes "User is viewing [Month Year]" context
- [ ] Works when navigating between months

#### US-051-003: Chat panel shows date context hint
**As a** user using the AI assistant  
**I want** to see what date context the AI is using  
**So that** I understand why suggestions use certain dates

**Acceptance Criteria:**
- [ ] Chat welcome screen shows context hint (e.g., "Viewing January 2026, selected: Jan 15")
- [ ] Context hint updates when calendar selection changes
- [ ] Hint is visually subtle but readable

### Account Filter Context

#### US-051-004: AI uses selected account filter
**As a** user filtering the calendar by account  
**I want** the AI to use that account as the default  
**So that** transactions go to the right account

**Acceptance Criteria:**
- [ ] When account filter is selected, AI uses it as default for transactions
- [ ] User can still override by specifying a different account
- [ ] Works in combination with date context

---

## Technical Design

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Calendar.razor                                                             │
│  ├── selectedDate: DateOnly?                                               │
│  ├── currentDate: DateOnly (viewed month)                                  │
│  └── selectedAccountId: Guid?                                              │
│                    │                                                        │
│                    ▼                                                        │
│         ChatContextService.SetCalendarContext(...)                          │
└─────────────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  ChatContextService (Scoped)                                                │
│  ├── ChatPageContext.CalendarViewedYear                                    │
│  ├── ChatPageContext.CalendarViewedMonth                                   │
│  ├── ChatPageContext.SelectedDate                                          │
│  └── ChatPageContext.CurrentAccountId (existing)                           │
└─────────────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  ChatPanel.razor                                                            │
│  └── Reads context from ChatContextService                                 │
│      └── Includes in SendMessageRequest                                    │
└─────────────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  API: POST /api/v1/chat/sessions/{id}/messages                             │
│  └── SendMessageRequest now includes ChatContextDto                        │
│      └── ChatController passes context to ChatService                      │
└─────────────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  NaturalLanguageParser                                                      │
│  └── Builds prompt with context: "User is viewing January 2026,            │
│       selected date: 2026-01-15, account: Checking"                        │
│  └── AI uses context to populate TransactionDate in parsed action          │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Files to Modify

#### Client - Services

| File | Status | Description |
|------|--------|-------------|
| `Services/ChatContextService.cs` | Modified | Add calendar date properties and `SetCalendarContext` method |

#### Client - Pages

| File | Status | Description |
|------|--------|-------------|
| `Pages/Calendar.razor` | Modified | Inject `IChatContextService`, call `SetCalendarContext` on date changes |

#### Client - Components

| File | Status | Description |
|------|--------|-------------|
| `Components/Chat/ChatPanel.razor` | Modified | Pass context to API when sending messages |

#### Client - API Services

| File | Status | Description |
|------|--------|-------------|
| `Services/IChatApiService.cs` | Modified | Add context parameter to `SendMessageAsync` |
| `Services/ChatApiService.cs` | Modified | Include context in HTTP request body |

#### Contracts - DTOs

| File | Status | Description |
|------|--------|-------------|
| `Contracts/Dtos/ChatDtos.cs` | Modified | Add `ChatContextDto` and update `SendMessageRequest` |

#### API - Controllers

| File | Status | Description |
|------|--------|-------------|
| `Api/Controllers/ChatController.cs` | Modified | Extract context from request and pass to service |

#### Application - Services

| File | Status | Description |
|------|--------|-------------|
| `Application/Chat/ChatService.cs` | No change | Already accepts `ChatContext?` parameter (currently receives `null`) |
| `Application/Chat/NaturalLanguageParser.cs` | Minor tweak | `FormatContext` already handles `CurrentDate`; improve prompt wording |

### DTO Changes

#### New: ChatContextDto

```csharp
/// <summary>
/// Context from the client UI to inform AI responses.
/// </summary>
public sealed class ChatContextDto
{
    /// <summary>
    /// Gets or sets the currently selected account ID.
    /// </summary>
    public Guid? CurrentAccountId { get; set; }

    /// <summary>
    /// Gets or sets the currently selected account name.
    /// </summary>
    public string? CurrentAccountName { get; set; }

    /// <summary>
    /// Gets or sets the currently selected category ID.
    /// </summary>
    public Guid? CurrentCategoryId { get; set; }

    /// <summary>
    /// Gets or sets the currently selected category name.
    /// </summary>
    public string? CurrentCategoryName { get; set; }

    /// <summary>
    /// Gets or sets the year being viewed on the calendar.
    /// </summary>
    public int? CalendarViewedYear { get; set; }

    /// <summary>
    /// Gets or sets the month being viewed on the calendar (1-12).
    /// </summary>
    public int? CalendarViewedMonth { get; set; }

    /// <summary>
    /// Gets or sets the selected date on the calendar (if any).
    /// </summary>
    public DateOnly? SelectedDate { get; set; }

    /// <summary>
    /// Gets or sets the current page type (e.g., "calendar", "transactions").
    /// </summary>
    public string? PageType { get; set; }
}
```

#### Updated: SendMessageRequest

```csharp
public sealed class SendMessageRequest
{
    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional context from the client UI.
    /// </summary>
    public ChatContextDto? Context { get; set; }
}
```

### Client Service Changes

#### ChatPageContext Updates

```csharp
public class ChatPageContext
{
    // Existing properties...
    public Guid? CurrentAccountId { get; set; }
    public string? CurrentAccountName { get; set; }
    public Guid? CurrentCategoryId { get; set; }
    public string? CurrentCategoryName { get; set; }
    public string? PageType { get; set; }

    // New calendar properties
    public int? CalendarViewedYear { get; set; }
    public int? CalendarViewedMonth { get; set; }
    public DateOnly? SelectedDate { get; set; }

    public string GetContextSummary()
    {
        var parts = new List<string>();

        if (CalendarViewedYear.HasValue && CalendarViewedMonth.HasValue)
        {
            var monthName = new DateOnly(CalendarViewedYear.Value, CalendarViewedMonth.Value, 1)
                .ToString("MMMM yyyy");
            parts.Add($"Viewing {monthName}");
        }

        if (SelectedDate.HasValue)
        {
            parts.Add($"Selected: {SelectedDate.Value:MMM d}");
        }

        if (!string.IsNullOrEmpty(CurrentAccountName))
        {
            parts.Add($"Account: {CurrentAccountName}");
        }

        // ... existing logic
        return parts.Count > 0 ? string.Join(" • ", parts) : string.Empty;
    }
}
```

#### IChatContextService Updates

```csharp
public interface IChatContextService
{
    // Existing methods...

    /// <summary>
    /// Sets the calendar context (viewed month and selected date).
    /// </summary>
    void SetCalendarContext(int year, int month, DateOnly? selectedDate, Guid? accountId = null, string? accountName = null);

    /// <summary>
    /// Converts the current context to a DTO for API transmission.
    /// </summary>
    ChatContextDto ToDto();
}
```

### Calendar.razor Integration

```csharp
@inject IChatContextService ChatContext

// In OnInitializedAsync or when month changes:
ChatContext.SetCalendarContext(
    currentDate.Year,
    currentDate.Month,
    selectedDate,
    selectedAccountId.HasValue ? Guid.Parse(selectedAccountId) : null,
    selectedAccountName);

// In SelectDate method:
private async Task SelectDate(DateOnly date)
{
    selectedDate = date;
    ChatContext.SetCalendarContext(currentDate.Year, currentDate.Month, date, ...);
    // ... existing logic
}

// On dispose:
public void Dispose()
{
    ChatContext.ClearContext();
    // ... existing cleanup
}
```

### AI Prompt Enhancement

Update `NaturalLanguageParser.FormatContext`:

```csharp
private static string FormatContext(ChatContext? context)
{
    if (context is null)
    {
        return string.Empty;
    }

    var parts = new List<string>();

    // Calendar context
    if (context.CurrentDate.HasValue)
    {
        parts.Add($"The user has selected {context.CurrentDate.Value:MMMM d, yyyy} on the calendar. " +
                  "Use this date for any transactions unless they specify otherwise.");
    }

    // Account context
    if (!string.IsNullOrEmpty(context.CurrentAccountName))
    {
        parts.Add($"The user is viewing the '{context.CurrentAccountName}' account. " +
                  "Use this account as the default unless they specify otherwise.");
    }

    // Page context
    if (!string.IsNullOrEmpty(context.CurrentPage))
    {
        parts.Add($"The user is on the {context.CurrentPage} page.");
    }

    return parts.Count > 0
        ? "\n\nContext from the user's current view:\n" + string.Join("\n", parts)
        : string.Empty;
}
```

---

## Implementation Plan

### Phase 1: Extend ChatContextService with Calendar Support
> **Commit:** `feat(client): add calendar context support to ChatContextService`

**Objective:** Add calendar date tracking to the client-side context service.

**Tasks:**
- [ ] Add `CalendarViewedYear`, `CalendarViewedMonth`, `SelectedDate` to `ChatPageContext`
- [ ] Add `SetCalendarContext` method to `IChatContextService`
- [ ] Implement `SetCalendarContext` in `ChatContextService`
- [ ] Update `GetContextSummary` to include calendar context
- [ ] Add `ToDto()` method to convert context to DTO
- [ ] Write unit tests for new context methods

**Validation:**
- Context service correctly stores calendar dates
- Context summary displays calendar information

---

### Phase 2: Add ChatContextDto to Contracts
> **Commit:** `feat(contracts): add ChatContextDto for AI context transmission`

**Objective:** Create the DTO for transmitting context from client to API.

**Tasks:**
- [ ] Create `ChatContextDto` class in `Contracts/Dtos/ChatDtos.cs`
- [ ] Add `Context` property to `SendMessageRequest`
- [ ] Ensure XML documentation is complete

**Validation:**
- DTOs compile and serialize correctly
- Backward compatible (Context is optional)

---

### Phase 3: Wire Calendar Page to Context Service
> **Commit:** `feat(client): connect Calendar page to ChatContextService`

**Objective:** Calendar page updates context when dates change.

**Tasks:**
- [ ] Inject `IChatContextService` into `Calendar.razor`
- [ ] Call `SetCalendarContext` in `OnParametersSetAsync` (when month changes)
- [ ] Call `SetCalendarContext` in `SelectDate` (when day is selected)
- [ ] Call `SetCalendarContext` when account filter changes
- [ ] Clear context in `Dispose`
- [ ] Test that context updates propagate to ChatPanel

**Validation:**
- Opening chat panel shows current calendar context
- Changing months updates context
- Selecting days updates context

---

### Phase 4: Pass Context Through ChatPanel to API
> **Commit:** `feat(client): include context in chat API requests`

**Objective:** ChatPanel sends context with each message.

**Tasks:**
- [ ] Update `IChatApiService.SendMessageAsync` to accept optional `ChatContextDto`
- [ ] Update `ChatApiService.SendMessageAsync` to include context in request body
- [ ] Update `ChatPanel.HandleSendMessage` to pass context from `ChatContextService`
- [ ] Write integration tests for context transmission

**Validation:**
- Network requests include context payload
- Context is correctly serialized

---

### Phase 5: API Controller and Service Integration
> **Commit:** `feat(api): process calendar context in chat endpoint`

**Objective:** API extracts context and passes to chat service.

**Tasks:**
- [ ] Update `ChatController.SendMessageAsync` to extract context from request
- [ ] Map `ChatContextDto` to domain `ChatContext` record
- [ ] Pass context to `_chatService.SendMessageAsync`
- [ ] Write API integration tests verifying context handling

**Validation:**
- API accepts and processes context
- Null context doesn't break existing behavior

---

### Phase 6: Enhance AI Prompt with Calendar Context (Mostly Exists)
> **Commit:** `feat(app): enhance AI prompt with calendar date context`

**Objective:** AI uses calendar context for smarter suggestions.

**Note:** `NaturalLanguageParser.FormatContext` already handles `CurrentDate`, `CurrentAccountName`, `CurrentCategoryName`, and `CurrentPage`. The existing prompt text is basic ("Viewing date: {date}"). This phase focuses on verifying/improving the prompt phrasing and testing the end-to-end flow.

**Tasks:**
- [ ] Review and improve `FormatContext` prompt wording (e.g., instruct AI to pre-fill transaction date)
- [ ] Verify `CurrentDate` is used for transaction date when present
- [ ] Write unit tests verifying date is extracted from context
- [ ] Test with various prompts ("add transaction", "spent $50 on groceries")

**Validation:**
- AI uses selected date instead of today
- Date is correctly parsed in transaction actions

---

### Phase 7: Testing and Polish
> **Commit:** `test(chat): add E2E tests for calendar context flow`

**Objective:** Comprehensive testing and edge case handling.

**Tasks:**
- [ ] Add E2E test: select date → open chat → add transaction → verify date
- [ ] Add E2E test: change month → verify context updates
- [ ] Test with no date selected (should default to today)
- [ ] Test context clearing on navigation
- [ ] Verify mobile experience
- [ ] Run accessibility audit on context hint display

**Validation:**
- All tests pass
- No regressions in existing chat functionality

---

## Testing Strategy

### Unit Tests (Client)

- [ ] `ChatContextService.SetCalendarContext` updates all properties
- [ ] `ChatPageContext.GetContextSummary` includes calendar info
- [ ] `ChatPageContext.ToDto` correctly maps all properties
- [ ] Context cleared properly on `ClearContext`

### Unit Tests (Application)

- [ ] `NaturalLanguageParser.FormatContext` includes date context in prompt
- [ ] Parser extracts transaction date from context when not specified in input
- [ ] Parser prefers explicit date in input over context date

### API Integration Tests

- [ ] `POST /api/v1/chat/sessions/{id}/messages` accepts context payload
- [ ] Null context doesn't cause errors (backward compatible)
- [ ] Context is passed through to service layer

### E2E Tests (Playwright)

- [ ] Select date on calendar → open AI panel → "add $50 groceries" → transaction has selected date
- [ ] Navigate to different month → open AI panel → context hint shows new month
- [ ] Filter by account → AI uses that account as default
- [ ] Navigate away from calendar → context is cleared

---

## Security Considerations

- Context data only includes IDs and names already known to the user
- Context is validated on API side (dates must be reasonable)
- No sensitive data exposure

---

## Performance Considerations

- Context updates are lightweight (no API calls)
- Context is included in existing message request (no extra round-trips)
- ChatContextService is scoped, so no memory accumulation

---

## Accessibility Considerations

- Context hint in chat panel has proper ARIA label
- Screen readers can announce current context
- Context hint has sufficient color contrast

---

## Future Enhancements

- **Date Range Context**: Support selected date ranges for reports integration
- **Transaction Context**: When viewing a transaction, pre-fill edit suggestions
- **Smart Suggestions**: AI proactively suggests based on context ("You usually buy groceries on Sundays")
- **Voice Input**: Date context helps disambiguate spoken commands

---

## References

- [Feature 032 - AI Category Suggestions](./archive/032-ai-category-suggestions.md) - AI infrastructure reference
- [Feature 043 - Consolidate AI Features](./archive/043-consolidate-ai-features-ui.md) - Chat panel implementation
- [ChatContextService.cs](../src/BudgetExperiment.Client/Services/ChatContextService.cs) - Current implementation
- [NaturalLanguageParser.cs](../src/BudgetExperiment.Application/Chat/NaturalLanguageParser.cs) - AI prompt building

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-26 | Initial draft | @becauseimclever |
| 2026-02-02 | Full technical design, implementation phases, user stories | @github-copilot |
| 2026-02-09 | Codebase audit: updated current state, fixed broken 043 link, noted Application layer already supports dates | @github-copilot |
