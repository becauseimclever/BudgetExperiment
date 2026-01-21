# Feature 026: AI Chat Assistant for Transaction Entry

## Overview

Implement a conversational AI chat interface that allows users to create transactions, transfers, and recurring items using natural language. Users can simply type commands like "Add $50 grocery purchase at Walmart yesterday" or "Create a monthly rent transfer of $1500 from checking to savings on the 1st" and the AI will parse the intent, extract relevant data, and execute the appropriate action. This feature leverages the **local-only AI infrastructure** from Feature 025, ensuring all processing happens on-device with zero data exfiltration.

## Problem Statement

Currently, creating transactions, transfers, and recurring items requires navigating forms and filling in multiple fields manually. This creates friction, especially for quick entries.

### Current State

- Users must navigate to specific pages/forms for each entity type
- Must fill in multiple form fields (amount, date, description, account, category, etc.)
- Transfers require selecting two accounts and specifying direction
- Recurring items have complex scheduling options
- No quick-entry option for power users
- Mobile/quick capture is cumbersome

### Target State

- Users can type natural language commands to create entries
- AI parses intent and extracts structured data
- Preview shows extracted data before confirming
- Supports transactions, transfers, and recurring items
- Handles ambiguity with clarifying questions
- Learns from user patterns for smarter defaults
- Chat history preserved for reference
- All processing remains local

---

## User Stories

### Chat Interface

#### US-026-001: Open Chat Panel
**As a** user  
**I want to** open a chat panel from anywhere in the app  
**So that** I can quickly add entries without navigating away

**Acceptance Criteria:**
- [ ] Chat toggle button visible in bottom-right corner when panel is closed
- [ ] Panel slides in from the **right side** of the screen (VS Code Copilot Chat style)
- [ ] Panel is fixed width (~400px) and full height
- [ ] Main content area shrinks to accommodate panel (not overlay)
- [ ] Keyboard shortcut (Ctrl+K or Cmd+K) toggles chat panel
- [ ] Panel has header with title "AI Assistant", minimize, and close buttons
- [ ] Chat state (open/closed, history) persists across page navigation
- [ ] On mobile/narrow screens, panel becomes full-width overlay with backdrop
- [ ] Smooth slide-in/out animation (200-300ms)

#### US-026-002: Send Natural Language Command
**As a** user  
**I want to** type a natural language command  
**So that** I can create entries quickly

**Acceptance Criteria:**
- [ ] Text input accepts free-form text
- [ ] Enter key sends message
- [ ] Loading indicator while AI processes
- [ ] Response appears in chat thread

#### US-026-003: View Chat History
**As a** user  
**I want to** see my chat history  
**So that** I can reference past commands and entries

**Acceptance Criteria:**
- [ ] Chat messages displayed in chronological order
- [ ] Shows user messages and AI responses
- [ ] Shows action results (success/failure)
- [ ] Can scroll through history
- [ ] History persists across sessions

### Transaction Entry

#### US-026-004: Add Transaction via Chat
**As a** user  
**I want to** add a transaction by typing a description  
**So that** I can quickly log expenses and income

**Acceptance Criteria:**
- [ ] AI parses: amount, description, date, account, category
- [ ] Handles various formats: "$50", "50 dollars", "fifty bucks"
- [ ] Understands relative dates: "yesterday", "last Friday", "Dec 5th"
- [ ] Infers account from context or asks if ambiguous
- [ ] Auto-categorizes using existing rules
- [ ] Shows preview before confirming

**Example Commands:**
- "Add $45.99 dinner at Olive Garden yesterday"
- "Spent 120 on groceries at Costco"
- "Got paid $3500 today"
- "Netflix subscription $15.99 from checking"

#### US-026-005: Add Transfer via Chat
**As a** user  
**I want to** create a transfer by describing it  
**So that** I can move money between accounts quickly

**Acceptance Criteria:**
- [ ] AI parses: amount, source account, destination account, date
- [ ] Understands "from X to Y" patterns
- [ ] Handles account nicknames and partial matches
- [ ] Shows preview with both sides of transfer
- [ ] Creates linked transfer transactions

**Example Commands:**
- "Transfer $500 from checking to savings"
- "Move 1000 from BofA to emergency fund"
- "Paid credit card $2000 from checking"

#### US-026-006: Add Recurring Item via Chat
**As a** user  
**I want to** create recurring transactions or transfers by describing them  
**So that** I can set up scheduled items quickly

**Acceptance Criteria:**
- [ ] AI parses: amount, description, frequency, start date, account(s)
- [ ] Understands frequencies: "monthly", "every 2 weeks", "weekly on Friday"
- [ ] Supports both recurring transactions and recurring transfers
- [ ] Shows preview with schedule summary
- [ ] Can specify end date or occurrence count

**Example Commands:**
- "Add monthly rent payment of $1800 on the 1st"
- "Create weekly transfer of $200 from checking to savings every Friday"
- "Netflix $15.99 monthly recurring from credit card"
- "Paycheck $3500 every two weeks starting next Friday"

### Confirmation & Editing

#### US-026-007: Preview Before Confirm
**As a** user  
**I want to** see a preview of what will be created  
**So that** I can verify the AI understood correctly

**Acceptance Criteria:**
- [ ] Preview card shows all extracted fields
- [ ] Clearly indicates entity type (transaction/transfer/recurring)
- [ ] Highlights any assumptions or defaults
- [ ] Confirm and Cancel buttons
- [ ] Can edit fields in preview before confirming

#### US-026-008: Modify Extracted Data
**As a** user  
**I want to** correct or modify the AI's interpretation  
**So that** I can fix mistakes without starting over

**Acceptance Criteria:**
- [ ] Can click to edit any field in preview
- [ ] Can type corrections: "No, use savings account"
- [ ] AI updates preview based on corrections
- [ ] Remembers corrections for similar future commands

#### US-026-009: Handle Ambiguity
**As a** user  
**I want to** be asked clarifying questions when needed  
**So that** the AI doesn't make wrong assumptions

**Acceptance Criteria:**
- [ ] AI asks when account is ambiguous
- [ ] AI asks when category is unclear
- [ ] AI asks when amount is missing
- [ ] Provides options to choose from
- [ ] Accepts typed responses or button clicks

### Context & Intelligence

#### US-026-010: Use Context from Current View
**As a** user  
**I want to** the AI to use context from what I'm viewing  
**So that** I don't have to repeat information

**Acceptance Criteria:**
- [ ] If on account page, defaults to that account
- [ ] If viewing a date range, uses relevant dates
- [ ] If category is selected, suggests that category
- [ ] Context is shown in chat for transparency

#### US-026-011: Learn from Patterns
**As a** user  
**I want to** the AI to learn my patterns  
**So that** it becomes more accurate over time

**Acceptance Criteria:**
- [ ] Remembers common merchants → category mappings
- [ ] Learns default accounts for certain transaction types
- [ ] Remembers preferred date formats
- [ ] All learning stored locally

---

## Technical Design

### Architecture Changes

- New `IChatService` interface for processing chat messages
- New `INaturalLanguageParser` interface for extracting structured data
- New `ChatMessage` and `ChatSession` entities
- New API endpoints for chat functionality
- New Blazor components for chat interface
- Integration with existing transaction/transfer/recurring services
- Leverage existing `IAiService` from Feature 025

### Domain Model

#### ChatSession Entity

```csharp
public sealed class ChatSession
{
    public Guid Id { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime LastMessageAtUtc { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<ChatMessage> _messages = new();
    public IReadOnlyList<ChatMessage> Messages => _messages.AsReadOnly();

    public static ChatSession Create();
    public ChatMessage AddUserMessage(string content);
    public ChatMessage AddAssistantMessage(string content, ChatAction? action = null);
    public void Close();
}

public sealed class ChatMessage
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public ChatRole Role { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    
    // For assistant messages with actions
    public ChatAction? Action { get; private set; }
    public ChatActionStatus ActionStatus { get; private set; } = ChatActionStatus.None;
    public Guid? CreatedEntityId { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static ChatMessage CreateUserMessage(Guid sessionId, string content);
    public static ChatMessage CreateAssistantMessage(Guid sessionId, string content, ChatAction? action = null);
    public void MarkActionConfirmed(Guid entityId);
    public void MarkActionCancelled();
    public void MarkActionFailed(string error);
}

public enum ChatRole
{
    User,
    Assistant,
    System
}

public enum ChatActionStatus
{
    None,           // No action associated
    Pending,        // Awaiting user confirmation
    Confirmed,      // User confirmed, entity created
    Cancelled,      // User cancelled
    Failed          // Action failed
}
```

#### ChatAction Value Object

```csharp
public abstract record ChatAction
{
    public abstract ChatActionType Type { get; }
    public abstract string GetPreviewSummary();
}

public sealed record CreateTransactionAction : ChatAction
{
    public override ChatActionType Type => ChatActionType.CreateTransaction;
    public Guid AccountId { get; init; }
    public string AccountName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateOnly Date { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? Category { get; init; }
    public Guid? CategoryId { get; init; }

    public override string GetPreviewSummary() =>
        $"Transaction: {Amount:C} - {Description} on {Date:d} ({AccountName})";
}

public sealed record CreateTransferAction : ChatAction
{
    public override ChatActionType Type => ChatActionType.CreateTransfer;
    public Guid FromAccountId { get; init; }
    public string FromAccountName { get; init; } = string.Empty;
    public Guid ToAccountId { get; init; }
    public string ToAccountName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateOnly Date { get; init; }
    public string? Description { get; init; }

    public override string GetPreviewSummary() =>
        $"Transfer: {Amount:C} from {FromAccountName} to {ToAccountName} on {Date:d}";
}

public sealed record CreateRecurringTransactionAction : ChatAction
{
    public override ChatActionType Type => ChatActionType.CreateRecurringTransaction;
    public Guid AccountId { get; init; }
    public string AccountName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? Category { get; init; }
    public RecurrencePattern Recurrence { get; init; } = null!;
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }

    public override string GetPreviewSummary() =>
        $"Recurring: {Amount:C} - {Description} ({Recurrence.GetDescription()})";
}

public sealed record CreateRecurringTransferAction : ChatAction
{
    public override ChatActionType Type => ChatActionType.CreateRecurringTransfer;
    public Guid FromAccountId { get; init; }
    public string FromAccountName { get; init; } = string.Empty;
    public Guid ToAccountId { get; init; }
    public string ToAccountName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? Description { get; init; }
    public RecurrencePattern Recurrence { get; init; } = null!;
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }

    public override string GetPreviewSummary() =>
        $"Recurring Transfer: {Amount:C} from {FromAccountName} to {ToAccountName} ({Recurrence.GetDescription()})";
}

public sealed record ClarificationNeededAction : ChatAction
{
    public override ChatActionType Type => ChatActionType.ClarificationNeeded;
    public string Question { get; init; } = string.Empty;
    public IReadOnlyList<ClarificationOption> Options { get; init; } = Array.Empty<ClarificationOption>();
    public string FieldName { get; init; } = string.Empty;  // Which field needs clarification

    public override string GetPreviewSummary() => Question;
}

public sealed record ClarificationOption
{
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public Guid? EntityId { get; init; }  // For account/category selection
}

public enum ChatActionType
{
    CreateTransaction,
    CreateTransfer,
    CreateRecurringTransaction,
    CreateRecurringTransfer,
    ClarificationNeeded
}
```

#### Recurrence Pattern (if not already exists)

```csharp
public sealed record RecurrencePattern
{
    public RecurrenceFrequency Frequency { get; init; }
    public int Interval { get; init; } = 1;  // Every N periods
    public DayOfWeek? DayOfWeek { get; init; }  // For weekly
    public int? DayOfMonth { get; init; }  // For monthly (1-31)

    public string GetDescription() => Frequency switch
    {
        RecurrenceFrequency.Daily when Interval == 1 => "Daily",
        RecurrenceFrequency.Daily => $"Every {Interval} days",
        RecurrenceFrequency.Weekly when Interval == 1 => DayOfWeek.HasValue ? $"Weekly on {DayOfWeek}" : "Weekly",
        RecurrenceFrequency.Weekly => $"Every {Interval} weeks",
        RecurrenceFrequency.Biweekly => "Every 2 weeks",
        RecurrenceFrequency.Monthly when Interval == 1 => DayOfMonth.HasValue ? $"Monthly on the {DayOfMonth.Value.Ordinal()}" : "Monthly",
        RecurrenceFrequency.Monthly => $"Every {Interval} months",
        RecurrenceFrequency.Yearly => "Yearly",
        _ => "Custom"
    };
}

public enum RecurrenceFrequency
{
    Daily,
    Weekly,
    Biweekly,
    Monthly,
    Yearly
}
```

### Service Interfaces

```csharp
public interface IChatService
{
    /// <summary>
    /// Gets or creates the active chat session.
    /// </summary>
    Task<ChatSession> GetOrCreateSessionAsync(CancellationToken ct = default);

    /// <summary>
    /// Processes a user message and returns the AI response.
    /// </summary>
    Task<ChatMessage> ProcessMessageAsync(
        Guid sessionId,
        string userMessage,
        ChatContext? context = null,
        CancellationToken ct = default);

    /// <summary>
    /// Confirms a pending action and executes it.
    /// </summary>
    Task<ChatMessage> ConfirmActionAsync(
        Guid messageId,
        CancellationToken ct = default);

    /// <summary>
    /// Cancels a pending action.
    /// </summary>
    Task<ChatMessage> CancelActionAsync(
        Guid messageId,
        CancellationToken ct = default);

    /// <summary>
    /// Provides a clarification response.
    /// </summary>
    Task<ChatMessage> ProvideClarificationAsync(
        Guid messageId,
        string fieldName,
        string value,
        CancellationToken ct = default);

    /// <summary>
    /// Gets chat history for a session.
    /// </summary>
    Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(
        Guid sessionId,
        int limit = 50,
        CancellationToken ct = default);
}

/// <summary>
/// Context from the current UI state to inform AI responses.
/// </summary>
public sealed record ChatContext
{
    public Guid? CurrentAccountId { get; init; }
    public string? CurrentAccountName { get; init; }
    public Guid? CurrentCategoryId { get; init; }
    public string? CurrentCategoryName { get; init; }
    public DateOnly? CurrentDate { get; init; }
    public string? CurrentPage { get; init; }
}

public interface INaturalLanguageParser
{
    /// <summary>
    /// Parses a natural language command into a structured action.
    /// </summary>
    Task<ParseResult> ParseCommandAsync(
        string input,
        IReadOnlyList<AccountInfo> accounts,
        IReadOnlyList<CategoryInfo> categories,
        ChatContext? context = null,
        CancellationToken ct = default);
}

public sealed record ParseResult
{
    public bool Success { get; init; }
    public ChatAction? Action { get; init; }
    public string? ErrorMessage { get; init; }
    public decimal Confidence { get; init; }
}

public sealed record AccountInfo(Guid Id, string Name, AccountType Type);
public sealed record CategoryInfo(Guid Id, string Name);
```

### Prompt Engineering

```csharp
public static class ChatPrompts
{
    public const string SystemPrompt = @"
You are a financial assistant that helps users add transactions, transfers, and recurring items to their budget tracking app.

Your job is to:
1. Parse user commands to extract structured financial data
2. Ask clarifying questions when information is ambiguous or missing
3. Confirm actions before executing them
4. Be conversational but concise

Available accounts:
{accounts}

Available categories:
{categories}

Current context:
{context}

When parsing commands, extract:
- For transactions: amount, description, date, account, category
- For transfers: amount, from_account, to_account, date, description
- For recurring: amount, description, frequency, start_date, end_date, account(s)

Respond ONLY with valid JSON in this format:
{
  ""intent"": ""transaction|transfer|recurring_transaction|recurring_transfer|clarification|unknown"",
  ""confidence"": 0.0-1.0,
  ""data"": {
    // Extracted fields based on intent
  },
  ""clarification"": {
    ""needed"": true|false,
    ""field"": ""field_name"",
    ""question"": ""question to ask"",
    ""options"": [{""label"": ""..."", ""value"": ""...""}]
  },
  ""response"": ""Natural language response to user""
}
";

    public const string TransactionExamples = @"
Examples of transaction commands:
- ""Add $50 for groceries at Walmart"" -> transaction, $50, ""groceries at Walmart"", today
- ""Spent 120 on gas yesterday"" -> transaction, $-120, ""gas"", yesterday
- ""Got paid 3500"" -> transaction, $3500, ""paycheck"", today
- ""Netflix 15.99 from checking"" -> transaction, $-15.99, ""Netflix"", today, checking account
";

    public const string TransferExamples = @"
Examples of transfer commands:
- ""Transfer 500 from checking to savings"" -> transfer, $500, checking, savings, today
- ""Move 1000 to emergency fund"" -> transfer (needs clarification on source)
- ""Pay off credit card 2000 from checking"" -> transfer, $2000, checking, credit card
";

    public const string RecurringExamples = @"
Examples of recurring commands:
- ""Monthly rent 1800 on the 1st"" -> recurring_transaction, $-1800, ""rent"", monthly day 1
- ""Weekly savings 200 every Friday"" -> recurring_transfer, $200, weekly Friday
- ""Paycheck 3500 every two weeks"" -> recurring_transaction, $3500, biweekly
- ""Netflix 15.99 monthly"" -> recurring_transaction, $-15.99, ""Netflix"", monthly
";
}
```

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/chat/session` | Get or create active session |
| GET | `/api/v1/chat/session/{id}/messages` | Get chat history |
| POST | `/api/v1/chat/session/{id}/messages` | Send a message |
| POST | `/api/v1/chat/messages/{id}/confirm` | Confirm pending action |
| POST | `/api/v1/chat/messages/{id}/cancel` | Cancel pending action |
| POST | `/api/v1/chat/messages/{id}/clarify` | Provide clarification |
| DELETE | `/api/v1/chat/session/{id}` | Close/delete session |

### Request/Response DTOs

```csharp
public sealed record ChatSessionDto
{
    public Guid Id { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime LastMessageAtUtc { get; init; }
    public bool IsActive { get; init; }
}

public sealed record ChatMessageDto
{
    public Guid Id { get; init; }
    public Guid SessionId { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public ChatActionDto? Action { get; init; }
    public string ActionStatus { get; init; } = string.Empty;
    public Guid? CreatedEntityId { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed record ChatActionDto
{
    public string Type { get; init; } = string.Empty;
    public string PreviewSummary { get; init; } = string.Empty;
    
    // Transaction fields
    public Guid? AccountId { get; init; }
    public string? AccountName { get; init; }
    public decimal? Amount { get; init; }
    public DateOnly? Date { get; init; }
    public string? Description { get; init; }
    public string? Category { get; init; }
    public Guid? CategoryId { get; init; }
    
    // Transfer fields
    public Guid? FromAccountId { get; init; }
    public string? FromAccountName { get; init; }
    public Guid? ToAccountId { get; init; }
    public string? ToAccountName { get; init; }
    
    // Recurring fields
    public RecurrencePatternDto? Recurrence { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    
    // Clarification fields
    public string? Question { get; init; }
    public IReadOnlyList<ClarificationOptionDto>? Options { get; init; }
    public string? FieldName { get; init; }
}

public sealed record RecurrencePatternDto
{
    public string Frequency { get; init; } = string.Empty;
    public int Interval { get; init; }
    public string? DayOfWeek { get; init; }
    public int? DayOfMonth { get; init; }
    public string Description { get; init; } = string.Empty;
}

public sealed record ClarificationOptionDto
{
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public Guid? EntityId { get; init; }
}

public sealed record SendMessageRequest
{
    public string Content { get; init; } = string.Empty;
    public ChatContextDto? Context { get; init; }
}

public sealed record ChatContextDto
{
    public Guid? CurrentAccountId { get; init; }
    public string? CurrentAccountName { get; init; }
    public Guid? CurrentCategoryId { get; init; }
    public string? CurrentCategoryName { get; init; }
    public DateOnly? CurrentDate { get; init; }
    public string? CurrentPage { get; init; }
}

public sealed record ClarifyRequest
{
    public string FieldName { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}
```

### Database Changes

New table: `ChatSessions`

| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| CreatedAtUtc | timestamp | NOT NULL |
| LastMessageAtUtc | timestamp | NOT NULL |
| IsActive | boolean | NOT NULL |

New table: `ChatMessages`

| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| SessionId | uuid | FK, NOT NULL |
| Role | int | NOT NULL |
| Content | text | NOT NULL |
| CreatedAtUtc | timestamp | NOT NULL |
| ActionJson | jsonb | NULL |
| ActionStatus | int | NOT NULL |
| CreatedEntityId | uuid | NULL |
| ErrorMessage | varchar(500) | NULL |

Index: `IX_ChatMessages_SessionId` (SessionId, CreatedAtUtc)
Index: `IX_ChatMessages_ActionStatus` (ActionStatus) WHERE ActionStatus = Pending

### UI Components

#### Panel Layout Design (VS Code Copilot Chat Style)

The chat panel is designed to match the look and feel of GitHub Copilot Chat in VS Code:

**Position & Behavior:**
- Fixed to the **right side** of the screen
- Slides in/out horizontally from the right edge
- Default width: `400px` (adjustable via drag handle)
- Full height of the viewport (below the header/navbar)
- Does NOT overlay the main content - main content area shrinks when panel opens
- Panel state (open/closed) persists across page navigation and sessions

**Visual Design:**
```
┌─────────────────────────────────────────────┬────────────────────────┐
│                                             │ 💬 AI Assistant    ─ × │
│                                             ├────────────────────────┤
│                                             │ ┌──────────────────┐   │
│                                             │ │ 🤖 How can I     │   │
│                                             │ │ help you today?  │   │
│          Main Application Content           │ └──────────────────┘   │
│          (shrinks when panel open)          │                        │
│                                             │ ┌──────────────────┐   │
│                                             │ │ 👤 Add $50 at   │   │
│                                             │ │ Walmart          │   │
│                                             │ └──────────────────┘   │
│                                             │                        │
│                                             │ ┌──────────────────┐   │
│                                             │ │ 🤖 Preview:      │   │
│                                             │ │ ┌──────────────┐ │   │
│                                             │ │ │ Transaction  │ │   │
│                                             │ │ │ $50.00       │ │   │
│                                             │ │ │ Walmart      │ │   │
│                                             │ │ │ Today        │ │   │
│                                             │ │ └──────────────┘ │   │
│                                             │ │ [Confirm] [Edit] │   │
│                                             │ └──────────────────┘   │
│                                             ├────────────────────────┤
│                                             │ ┌──────────────────┐   │
│                                             │ │ Type a message...│ ⏎ │
│                                             │ └──────────────────┘   │
└─────────────────────────────────────────────┴────────────────────────┘
```

**Panel Header:**
- Title: "AI Assistant" with chat icon (💬)
- Minimize button (─) - collapses to thin strip with just the toggle icon
- Close button (×) - hides panel completely
- Subtle border/shadow separating from main content

**Message Area:**
- Scrollable conversation history
- Messages aligned: user messages right, AI messages left
- User messages: subtle background (e.g., `var(--accent-light)`)
- AI messages: no background or very light card style
- Timestamps shown subtly (hover or inline)
- Avatar/icon for each role (🤖 AI, 👤 User)

**Action Preview Cards:**
- Inline within AI messages when action is proposed
- Card-style with subtle border and shadow
- Shows extracted fields in clear layout
- Confirmation buttons at bottom of card
- Editable fields (click to modify)

**Input Area:**
- Fixed at bottom of panel
- Full-width text input
- Placeholder: "Type a message..." or "Ask me to add a transaction..."
- Send button (or Enter to send)
- Subtle top border separating from messages

**Animation & Transitions:**
- Smooth slide-in/out animation (200-300ms ease)
- Content area width transitions smoothly
- No jarring reflows

**Responsive Behavior:**
- On narrow screens (<768px): panel becomes full-width overlay with backdrop
- On very wide screens (>1600px): panel can be wider (up to 500px)
- Touch-friendly on tablets

#### New Components

| Component | Description |
|-----------|-------------|
| `ChatPanel.razor` | Main chat interface - right-side panel with header, messages, and input |
| `ChatPanelHeader.razor` | Panel header with title, minimize, and close buttons |
| `ChatToggleButton.razor` | Floating button (bottom-right) to open chat when closed |
| `ChatMessageList.razor` | Scrollable container for chat message history |
| `ChatMessageBubble.razor` | Individual message display with role-based styling |
| `ChatActionPreview.razor` | Preview card for pending actions (embedded in AI message) |
| `ChatActionFields.razor` | Editable field display within action preview |
| `ClarificationOptions.razor` | Button group for clarification choices |
| `ChatInput.razor` | Text input with send button, fixed at panel bottom |

#### Layout Integration

The main layout (`MainLayout.razor`) must be updated to accommodate the chat panel:

```razor
@* MainLayout.razor structure *@
<div class="app-container @(IsChatOpen ? "chat-open" : "")">
    <header class="app-header">
        @* Header content *@
    </header>
    
    <div class="app-body">
        <nav class="app-sidebar">
            @* Navigation menu *@
        </nav>
        
        <main class="app-main">
            @Body
        </main>
        
        @if (IsChatOpen)
        {
            <aside class="chat-panel">
                <ChatPanel OnClose="CloseChat" />
            </aside>
        }
    </div>
    
    @if (!IsChatOpen)
    {
        <ChatToggleButton OnClick="OpenChat" />
    }
</div>
```

```css
/* Layout CSS */
.app-body {
    display: flex;
    flex: 1;
    overflow: hidden;
}

.app-main {
    flex: 1;
    overflow: auto;
    transition: margin-right 0.25s ease;
}

.chat-open .app-main {
    margin-right: 0; /* Panel is part of flex, not overlay */
}

.chat-panel {
    width: 400px;
    min-width: 320px;
    max-width: 500px;
    border-left: 1px solid var(--border-color);
    display: flex;
    flex-direction: column;
    background: var(--surface-color);
    animation: slideInRight 0.25s ease;
}

@keyframes slideInRight {
    from { transform: translateX(100%); opacity: 0; }
    to { transform: translateX(0); opacity: 1; }
}

/* Responsive: overlay on mobile */
@media (max-width: 768px) {
    .chat-panel {
        position: fixed;
        top: 0;
        right: 0;
        bottom: 0;
        width: 100%;
        max-width: 100%;
        z-index: 1000;
    }
    
    .chat-panel::before {
        content: '';
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background: rgba(0, 0, 0, 0.5);
        z-index: -1;
    }
}
```

#### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+K` / `Cmd+K` | Toggle chat panel open/closed |
| `Escape` | Close chat panel (when focused) |
| `Enter` | Send message |
| `Shift+Enter` | New line in message input |
| `Up Arrow` | Edit last message (when input empty) |

#### Accessibility

- Panel announced to screen readers when opened
- Focus trapped within panel when open
- Keyboard navigation through messages
- ARIA labels for all interactive elements
- High contrast support for message bubbles

---

## Implementation Plan

### Phase 1: Domain Model ✅

**Objective:** Establish chat entities and action types

**Tasks:**
- [x] Write unit tests for `ChatSession` and `ChatMessage` creation
- [x] Write unit tests for action status transitions
- [x] Implement `ChatSession` entity
- [x] Implement `ChatMessage` entity
- [x] Implement `ChatAction` hierarchy (all action types)
- [x] Add `IChatSessionRepository` and `IChatMessageRepository` interfaces

**Tests:** 43 unit tests

**Commit:** `feat(domain): add ChatSession and ChatMessage entities`

---

### Phase 2: Infrastructure - Repository & Migrations ✅

**Objective:** Implement database persistence for chat

**Tasks:**
- [x] Create EF Core configuration for chat entities
- [x] Configure JSON column for ChatAction storage with polymorphic `$type` discriminator
- [x] Add `RecurrencePatternJsonConverter` for nested RecurrencePattern serialization
- [x] Add migration for chat tables (`ChatSessions`, `ChatMessages`)
- [x] Implement `ChatSessionRepository` and `ChatMessageRepository`
- [x] Write integration tests

**Tests:** 16 integration tests

**Commit:** `feat(infrastructure): add chat persistence with EF Core`

---

### Phase 3: Natural Language Parser ✅

**Objective:** Implement AI-powered command parsing

**Tasks:**
- [x] Create `INaturalLanguageParser` interface
- [x] Implement `NaturalLanguageParser` using `IAiService` from Feature 025
- [x] Define `ChatAiPrompts` with structured prompt templates
- [x] Implement response parsing from AI JSON
- [x] Handle date parsing (relative and absolute)
- [x] Handle amount parsing (various formats)
- [x] Write unit tests with mocked AI responses

**Tests:** 21 unit tests

**Commit:** `feat(application): add NaturalLanguageParser for chat`

---

### Phase 4: Chat Service ✅

**Objective:** Implement main chat processing and action execution

**Tasks:**
- [x] Implement `IChatService` interface
- [x] Implement `SendMessageAsync` flow with AI parsing
- [x] Gather accounts and categories for AI context
- [x] Handle clarification requests
- [x] Store messages and actions via repositories
- [x] Implement `ConfirmActionAsync` for transactions, transfers, recurring items
- [x] Implement `CancelActionAsync`
- [x] Integration with existing services (`ITransactionService`, `IRecurringTransactionService`)
- [x] Write unit tests

**Tests:** 16 unit tests

**Commit:** `feat(application): add ChatService for orchestration`

---

### Phase 5: API Endpoints ✅

**Objective:** Expose chat functionality via REST API

**Tasks:**
- [x] Add `ChatDtos` to Api project (session, message, action DTOs)
- [x] Implement `ChatController` with REST endpoints
- [x] Add mapper extensions (`DomainToDtoMapper`)
- [x] Write API integration tests

**Endpoints:**
- `GET /api/v1/chat/sessions` - List user's sessions
- `GET /api/v1/chat/sessions/{id}` - Get session with messages
- `POST /api/v1/chat/sessions` - Create new session
- `POST /api/v1/chat/sessions/{sessionId}/messages` - Send message
- `POST /api/v1/chat/messages/{messageId}/confirm` - Confirm action
- `POST /api/v1/chat/messages/{messageId}/cancel` - Cancel action

**Tests:** 12 API tests

**Commit:** `feat(api): add ChatController REST API endpoints`

---

### Phase 6: Blazor UI ✅

**Objective:** Build complete chat interface with action preview

**Tasks:**
- [x] Create `IChatApiService` interface and `ChatApiService` HTTP client
- [x] Create `ChatPanel` component (floating panel, bottom-right toggle)
- [x] Create `ChatMessageBubble` component with action previews
- [x] Create `ChatInput` component with Enter-to-send
- [x] Implement confirm/cancel buttons in action cards
- [x] Add loading states and typing indicator
- [x] Add welcome message for new sessions
- [x] Wire up to API

**Components:**
- `ChatPanel.razor` - Main floating panel container
- `ChatMessageBubble.razor` - Message display with action cards
- `ChatInput.razor` - Text input with send button

**Commit:** `feat(client): add Blazor chat UI components`

---

### Phase 7: Integration ✅

**Objective:** Integrate chat panel into main layout

**Tasks:**
- [x] Add `ChatPanel` to `MainLayout.razor`
- [x] Verify all tests pass (1216 tests)

**Commit:** `feat(client): integrate ChatPanel into MainLayout`

---

### Phase 8: Side Panel & Context Awareness ✅

**Objective:** Convert to VS Code-style side panel and add page context

**Tasks:**
- [x] Convert `ChatPanel` from floating bottom panel to right side panel
- [x] Create `IChatContextService` to track current page context
- [x] Add chat toggle button in header bar
- [x] Update `MainLayout` to shrink main content when chat is open
- [x] Update `AccountTransactions`, `Transfers`, `Recurring`, `Categories` pages to set context
- [x] Show context hint in chat welcome message

**Components:**
- `ChatContextService.cs` - Tracks current account, category, and page type
- Updated `MainLayout.razor` - Chat toggle button, CSS class for open state
- Updated `ChatPanel.razor.css` - Full-height side panel with 380px width

**Commit:** `feat(client): add VS Code-style side panel and page context awareness`

---

### Future Enhancements (Not Implemented)

The following features from the original plan were deferred for future iterations:

- **Keyboard shortcut (Ctrl+K)** - Toggle chat panel via keyboard
- **Inline field editing** - Edit parsed values before confirming
- **Clarification response handling** - Click-to-select for ambiguous items
- **SignalR real-time updates** - Streaming AI responses

---

## Testing Strategy

### Unit Tests (108 total)

- [x] `ChatSession.Create()` and `AddUserMessage()`
- [x] `ChatMessage` status transitions
- [x] `ChatAction` hierarchy and properties
- [x] `NaturalLanguageParser` date extraction
- [x] `NaturalLanguageParser` amount parsing
- [x] `NaturalLanguageParser` account matching
- [x] `ChatService.SendMessageAsync()` happy path
- [x] `ChatService.ConfirmActionAsync()` creates correct entity
- [x] `ChatService.CancelActionAsync()` updates status
- [x] AI response JSON parsing

### Integration Tests

- [x] Repository CRUD operations (ChatSession, ChatMessage)
- [x] API endpoint tests (12 tests)
- [ ] Full flow: send message → get action → confirm → verify entity (E2E)
- [ ] Clarification flow (E2E)

### Manual Testing Checklist

- [ ] Open chat panel (click toggle button)
- [ ] Add transaction: "Add $50 for lunch at Chipotle"
- [ ] Verify preview shows correct data
- [ ] Confirm and verify transaction created
- [ ] Add transfer: "Transfer 500 from checking to savings"
- [ ] Handle ambiguous account (clarification)
- [ ] Add recurring: "Monthly rent $1800 on the 1st"
- [ ] Cancel a pending action
- [ ] Test with AI unavailable
- [ ] Test chat history across sessions

---

## Migration Notes

### Database Migration

```bash
dotnet ef migrations add Feature026_ChatAssistant --project src/BudgetExperiment.Infrastructure --startup-project src/BudgetExperiment.Api
```

### Breaking Changes

None - this is a new feature that does not modify existing behavior.

---

## Security Considerations

- **Data Privacy**: All processing is local; no data leaves the device
- **Input Validation**: Sanitize all user input before AI processing
- **Action Confirmation**: Never auto-execute; always require user confirmation
- **Amount Limits**: Consider validating amounts are reasonable
- **Session Isolation**: Chat sessions should be user-specific (when auth is implemented)
- **Rate Limiting**: Consider limiting message frequency to prevent abuse

---

## Performance Considerations

- AI inference latency (same as Feature 025: 10-60+ seconds)
- Show typing indicator during AI processing
- Consider streaming responses if Ollama supports it
- Cache account/category lists for prompt construction
- Limit chat history to recent messages (50 default)
- Consider background session cleanup for old sessions

---

## Future Enhancements

- Voice input for hands-free entry
- Batch commands: "Add these transactions: ..."
- Undo last action via chat
- Query capabilities: "How much did I spend on groceries last month?"
- Smart suggestions: "You usually buy gas on Fridays, want me to add it?"
- Multi-turn conversation memory for complex entries
- Templates: "Add the usual coffee purchase"
- Integration with receipt scanning
- Export chat history

---

## References

- [Feature 025: AI-Powered Rule Suggestions](./025-ai-rule-suggestions.md) - AI infrastructure this feature uses
- [Feature 024: Auto-Categorization Rules Engine](./024-auto-categorization-rules-engine.md) - Category rules for auto-categorization
- [Ollama Documentation](https://ollama.ai/docs) - Local AI runtime
- [Feature Template](./FEATURE-TEMPLATE.md)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-19 | Add side panel layout and page context awareness (Phase 8) | @copilot |
| 2026-01-19 | Feature implementation complete (Phases 1-7) | @copilot |
| 2026-01-15 | Initial draft | @copilot |
