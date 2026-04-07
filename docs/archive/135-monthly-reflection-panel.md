# Feature 135: Monthly Reflection Panel

> **Status:** Done

## Prerequisites

Feature 129b (Feature Flag Implementation) must be merged before implementation begins.

Feature 131 (Budget Categories — Kakeibo Category Routing) must be merged before implementation begins.

Feature 132 (Transaction Entry — Kakeibo Selector) should be complete before implementation begins.

## Feature Flag

**Name:** `Features:Kakeibo:MonthlyReflectionPrompts`

**Default:** `true`

**Rationale:** Shared flag with Feature 134 (Calendar). Controls whether monthly reflection prompts appear and whether the reflection panel is accessible. When disabled, users don't see reflection UI, but `MonthlyReflection` records are still persisted if created via API.

## Overview

This feature introduces the monthly reflection ritual — the heart of Kakeibo practice — as a dedicated panel accessible from the calendar. Users answer the four Kakeibo questions at month start (intention + savings goal) and month end (gratitude + improvement), transforming abstract financial data into a journaling practice. The reflection panel is part of the calendar UX (accessed from month header) but can also be a standalone page in reports for users who want a dedicated journal view.

## Problem Statement

### Current State

- Budget goals are purely numerical targets with no intention-setting ritual.
- There is no space for users to journal about their spending month-to-month.
- The four Kakeibo questions (income? save? spend? improve?) are never asked or answered.
- Month-end review is absent from the UX — users complete months without closure or learning.
- Reflection history is not preserved — each month is isolated, with no way to see growth over time.

### Target State

- At month start, a non-blocking prompt asks: "What do you want to save this month?" and optionally "What's your intention for this month?"
- At month end (or any time in a past month), users access a **Monthly Reflection Panel** showing:
  - How much income did I receive? (computed from income transactions)
  - How much did I want to save? (from `MonthlyReflection.SavingsGoal`, set at month start)
  - How much did I actually spend? (computed from expense transactions)
  - Implied actual savings: Income - Spending
  - Kakeibo category breakdown (Essentials vs. Wants; Culture and Unexpected highlighted separately)
  - Free-text fields for gratitude ("What went well last month?") and improvement ("What would I do differently?")
- Reflection history is viewable in a **Reflection History** section (timeline of past monthly journals).
- Reflection data is private journal content, separate from financial data.

## Domain Model Changes

### New Entity: `MonthlyReflection`

```csharp
public class MonthlyReflection
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Year and month this reflection covers (e.g., 2026, April).
    /// </summary>
    public int Year { get; set; }
    public int Month { get; set; }
    
    /// <summary>
    /// Savings goal the user set at month start (amount they wanted to save).
    /// </summary>
    public decimal SavingsGoal { get; set; }
    
    /// <summary>
    /// Computed savings at month end (income - expenses).
    /// Calculated, not stored, but can be cached in the reflection record.
    /// </summary>
    public decimal? ActualSavings { get; set; }
    
    /// <summary>
    /// Month-start intention: what the user wanted to focus on (280 chars max).
    /// </summary>
    public string? IntentionText { get; set; }
    
    /// <summary>
    /// Month-end gratitude: what went well (free text, no char limit).
    /// </summary>
    public string? GratitudeText { get; set; }
    
    /// <summary>
    /// Month-end improvement: what they'd do differently (free text, no char limit).
    /// </summary>
    public string? ImprovementText { get; set; }
    
    /// <summary>
    /// Audit fields.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    
    /// <summary>
    /// Foreign key to user (implied; added via fluent config in Infrastructure).
    /// </summary>
    public Guid UserId { get; set; }
    // public virtual User User { get; set; }  // Navigation property (optional)
}
```

### EF Core Configuration

In `MonthlyReflectionConfiguration` (Infrastructure):

```csharp
builder.HasKey(r => r.Id);
builder.Property(r => r.Year).IsRequired();
builder.Property(r => r.Month).IsRequired();
builder.Property(r => r.SavingsGoal).HasPrecision(19, 2);
builder.Property(r => r.ActualSavings).HasPrecision(19, 2);
builder.Property(r => r.IntentionText).HasMaxLength(280);
builder.Property(r => r.GratitudeText).HasMaxLength(2000);
builder.Property(r => r.ImprovementText).HasMaxLength(2000);
builder.Property(r => r.CreatedAtUtc).IsRequired();
builder.Property(r => r.UpdatedAtUtc).IsRequired();
builder.Property(r => r.UserId).IsRequired();

// Composite unique key: user + year + month (one reflection per user per month)
builder.HasIndex(r => new { r.UserId, r.Year, r.Month }).IsUnique();
```

### Database Migration

**Schema:**

```sql
CREATE TABLE "MonthlyReflections" (
    "Id" uuid PRIMARY KEY,
    "UserId" uuid NOT NULL,
    "Year" int NOT NULL,
    "Month" int NOT NULL,
    "SavingsGoal" numeric(19, 2) NOT NULL,
    "ActualSavings" numeric(19, 2) NULL,
    "IntentionText" varchar(280) NULL,
    "GratitudeText" varchar(2000) NULL,
    "ImprovementText" varchar(2000) NULL,
    "CreatedAtUtc" timestamp NOT NULL,
    "UpdatedAtUtc" timestamp NOT NULL,
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    UNIQUE ("UserId", "Year", "Month")
);

CREATE INDEX idx_monthly_reflections_user_year_month 
    ON "MonthlyReflections" ("UserId", "Year", "Month");
```

## API Changes

### Create/Update Monthly Reflection

**Endpoints:**

- `POST /api/v1/reflections/month/{year}/{month}` — Create or update a reflection for a month.
- `PUT /api/v1/reflections/{reflectionId}` — Update an existing reflection by ID.

**Request DTO:**

```csharp
public class CreateOrUpdateMonthlyReflectionRequest
{
    /// <summary>
    /// Savings goal for the month (set at month start).
    /// </summary>
    public decimal SavingsGoal { get; set; }
    
    /// <summary>
    /// Optional intention text (month start).
    /// </summary>
    public string? IntentionText { get; set; }
    
    /// <summary>
    /// Optional gratitude text (month end).
    /// </summary>
    public string? GratitudeText { get; set; }
    
    /// <summary>
    /// Optional improvement text (month end).
    /// </summary>
    public string? ImprovementText { get; set; }
}
```

**Validation:**

- `SavingsGoal` must be >= 0.
- `IntentionText` max 280 chars.
- `GratitudeText` and `ImprovementText` max 2000 chars each.
- User can only create/update their own reflections (authorization check).

**Response DTO:**

```csharp
public class MonthlyReflectionResponse
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal SavingsGoal { get; set; }
    public decimal? ActualSavings { get; set; }
    public string? IntentionText { get; set; }
    public string? GratitudeText { get; set; }
    public string? ImprovementText { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
```

### Get Monthly Reflection

**Endpoint:** `GET /api/v1/reflections/month/{year}/{month}`

**Response:** `MonthlyReflectionResponse` (or null if not yet created).

### Get Reflection History

**Endpoint:** `GET /api/v1/reflections?limit=12&offset=0`

**Response:**

```csharp
public class ReflectionHistoryResponse
{
    public List<MonthlyReflectionResponse> Reflections { get; set; }
    public int Total { get; set; }
    public int Offset { get; set; }
    public int Limit { get; set; }
}
```

Returns past reflections in reverse chronological order (most recent first). Useful for timeline/journal view.

### Get Month Financial Summary (for Reflection Panel)

**Endpoint:** `GET /api/v1/calendar/month/{year}/{month}/summary`

**Response:**

```csharp
public class MonthFinancialSummaryResponse
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal ComputedSavings { get; set; }  // Income - Expenses
    public KakeiboBreakdownResponse ExpenseBreakdown { get; set; }
    public MonthlyReflectionResponse? Reflection { get; set; }
}
```

This endpoint provides all data needed to render the reflection panel without multiple calls.

## UI Changes

### Monthly Reflection Panel Component

**New component:** `src/BudgetExperiment.Client/Components/Reflection/MonthlyReflectionPanel.razor`

**Structure:**

1. **Header:**
   - Display month/year (e.g., "April 2026")
   - Show status badges: "In Progress" (current month), "Completed" (past month with reflection), "Not Reflected" (past month without reflection)

2. **Financial Summary Section (Read-Only):**
   - Three metrics displayed clearly:
     - "Total Income": amount from income transactions for the month
     - "Total Spending": amount from expense transactions for the month
     - "Computed Savings": Income - Spending (shown in green if positive, red if negative)
   - Below: Kakeibo category breakdown bar (same as in Feature 134):
     - Essentials, Wants, Culture, Unexpected with amounts and % of total

3. **Savings Goal Section (Month-Start & Month-End):**
   - Field: "Savings Goal" (number input, editable)
   - Subtext: "How much did you want to save this month?"
   - Progress indicator (month-end only): "Saved $X of $Y goal" (colour coded: green if on track, amber if slightly behind, soft red if significantly behind)
   - Compare to computed savings: if goal is $500 and actual savings is $450, show "8% away from goal"

4. **Intention Section (Month-Start):**
   - Label: "What's your intention for this month?"
   - Text input (280 chars max, with counter)
   - Placeholder: "e.g., save for a vacation, reduce dining out, focus on essentials"
   - Only editable in month-start phase (current month)

5. **Gratitude Section (Month-End, Editable in Current/Past Months):**
   - Label: "What went well last month?" or "What went well this month?" (depending on current month vs. past)
   - Rich text area (2000 chars max, with counter)
   - Placeholder: "e.g., I stuck to my grocery budget, I enjoyed a great meal with friends without guilt"
   - Optional; can be left blank

6. **Improvement Section (Month-End, Editable in Current/Past Months):**
   - Label: "What would you do differently next month?"
   - Rich text area (2000 chars max, with counter)
   - Placeholder: "e.g., plan better for unexpected expenses, save more from dining"
   - Optional; can be left blank

7. **Action Buttons:**
   - "Save Changes" button (saves all editable fields; enabled only if changes made)
   - "Close" or back link
   - Optional: "Delete Reflection" link (for admins or users who want to clear the journal; soft delete or just clear text fields)

**Rendering Logic:**

- For **current month** (year/month == today):
  - Show all sections
  - Intention, Gratitude, Improvement fields are all editable
  - Savings Goal is editable
  
- For **past months** (year/month < today):
  - Show all sections
  - Gratitude and Improvement are editable (allows users to add/update journal entries)
  - Savings Goal can be viewed but ideally not edited (or edit-disabled with note "This was the goal set at month start")
  - Intention is read-only

- For **future months** (if accessible):
  - Show only Savings Goal and Intention sections
  - Other fields disabled with note "Complete the month first"

**Feature flag gating:** Entire panel is only visible if `Features:Kakeibo:MonthlyReflectionPrompts` is enabled.

### Integration with Calendar

**Calendar month header (Feature 134):**

- Add a "Reflection" link for past months that opens this panel (modal or navigate to `/reflections/month/{year}/{month}`).
- For current month, show "Set Intention" or "Review Progress" CTA.

**Month-start intention prompt (Feature 134):**

- Can reuse this panel's Intention & Savings Goal sections, or have a lightweight modal that opens the full panel after submission.

### Reflection History Page (Optional)

**New page:** `/reflections` or `/journal`

**Features:**

- Timeline view of all monthly reflections (list or timeline layout).
- Show month/year, savings goal, and first line of intention text.
- Click to open detailed reflection panel.
- Filter/sort: by year, by savings goal achievement, etc.
- Export: save reflections as PDF journal (future enhancement).

**Entry point:** Link from reports landing page or account settings menu.

## Service Layer

### ReflectionService (Application)

```csharp
public interface IReflectionService
{
    Task<MonthlyReflectionResponse> GetOrCreateAsync(int year, int month, Guid userId);
    Task<MonthlyReflectionResponse> UpdateAsync(Guid reflectionId, CreateOrUpdateMonthlyReflectionRequest request, Guid userId);
    Task<List<MonthlyReflectionResponse>> GetHistoryAsync(Guid userId, int limit = 12, int offset = 0);
    Task<MonthFinancialSummaryResponse> GetMonthSummaryAsync(int year, int month, Guid userId);
    Task DeleteAsync(Guid reflectionId, Guid userId);
}
```

Responsibilities:

- CRUD operations for `MonthlyReflection`.
- Compute `ActualSavings` from transaction data (cached in record for read access).
- Authorization checks (user can only read/write their own reflections).

## Feature Flag

**Name:** `Features:Kakeibo:MonthlyReflectionPrompts`

**Default:** `true`

**Scope:** Shared with Feature 134 (Calendar). Controls both calendar prompts and reflection panel visibility.

## Acceptance Criteria

- [ ] `MonthlyReflection` entity created with all required fields.
- [ ] EF Core configuration includes composite unique key (UserId, Year, Month).
- [ ] Database migration creates schema with correct column types and indexes.
- [ ] Create/Update endpoint POST `/api/v1/reflections/month/{year}/{month}` works.
- [ ] Get endpoint `GET /api/v1/reflections/month/{year}/{month}` returns reflection or null.
- [ ] History endpoint `GET /api/v1/reflections` supports pagination (limit, offset).
- [ ] Summary endpoint `GET /api/v1/calendar/month/{year}/{month}/summary` returns complete financial data.
- [ ] `MonthlyReflectionPanel.razor` component renders correctly for current and past months.
- [ ] Financial summary section displays income, expenses, computed savings.
- [ ] Kakeibo breakdown bar is rendered in the panel.
- [ ] Intention, Gratitude, and Improvement fields are editable (with char limits and counters).
- [ ] Savings Goal field is editable for current month; read-only for past months (or disabled).
- [ ] "Save Changes" button persists all editable fields via PUT endpoint.
- [ ] Month-start prompt triggers for current month without reflection.
- [ ] Reflection link appears in calendar month header for past months.
- [ ] Reflection panel is only visible if feature flag `Features:Kakeibo:MonthlyReflectionPrompts` is enabled.
- [ ] All text fields validate max character limits (IntentionText: 280, Gratitude/Improvement: 2000).
- [ ] Authorization: users can only access their own reflections (no cross-user data exposure).
- [ ] All existing tests pass; new tests for service, API endpoints, component rendering, and authorization.
- [ ] Accessibility: labels, ARIA, keyboard navigation in text areas.

## Implementation Order

1. **Create `MonthlyReflection` entity** (Domain).
2. **Create EF Core configuration** with unique constraint (Infrastructure).
3. **Create database migration** with schema.
4. **Create `ReflectionService`** (Application) with CRUD and financial summary methods.
5. **Create API controller/endpoints** for reflection CRUD and history.
6. **Create `MonthlyReflectionPanel.razor` component** (Blazor UI).
7. **Integrate panel into calendar month header** (Feature 134 integration).
8. **Implement month-start intention prompt** (light modal or panel integration).
9. **Add feature flag gating** to all UI and API.
10. **Create reflection history page** (optional, can defer).
11. **Add tests** for service, endpoints, authorization, component, and end-to-end flows.

**Dependencies:**
- Feature 131 (KakeiboCategory routing).
- Feature 132 (Transaction KakeiboOverride for breakdown aggregation).
- Feature 134 (Calendar enhancements; coordinate savings progress bar display).
- Feature 129b (feature flag infrastructure).

**Related:** Feature 136 (Kaizen Micro-Goals) can reference monthly reflections for trend analysis; coordinate service data fetching.
