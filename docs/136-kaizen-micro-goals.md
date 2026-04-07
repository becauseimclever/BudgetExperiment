# Feature 136: Kaizen Micro-Goals

> **Status:** Planned

## Prerequisites

Feature 129b (Feature Flag Implementation) must be merged before implementation begins.

Feature 131 (Budget Categories — Kakeibo Category Routing) should be complete before implementation begins.

Feature 134 (Calendar — Kakeibo Enhancements) should be complete before implementation begins (week summary UI).

## Feature Flag

**Name:** `Features:Kaizen:MicroGoals`

**Default:** `true`

**Rationale:** Micro-goals are central to Kaizen philosophy but are entirely optional. Users who prefer strict budget tracking without self-improvement rituals can disable this feature. When disabled, the UI for goal-setting and tracking is hidden, but stored `KaizenGoal` records remain accessible via API (for data export/migration).

## Overview

This feature introduces **Kaizen micro-goals** — small, self-chosen weekly improvements chosen by the user at the start of each week. A goal is concrete ("Spend $10 less on coffee than last week") and scoped to a Kakeibo category or specific category. Goals are non-gamified, non-judgmental, and entirely optional. At week end, the user marks whether the goal was achieved (a quiet checkmark, no confetti). Over time, a 12-week rolling view in reports shows the compound effect of small weekly improvements.

This is the final piece of the Kakeibo + Kaizen philosophy, embedding the Kaizen principle of continuous small improvements directly into the weekly calendar rhythm.

## Problem Statement

### Current State

- Budget goals are monthly aggregate targets with no weekly cadence.
- There is no space for users to reflect on *small* improvements week-to-week.
- Kaizen philosophy (continuous improvement) is not operationalized in the UX.
- The calendar week view has no goal/intention setting ritual.

### Target State

- At the start of each week (or from the week summary row), users can set one **micro-goal**: a small, specific improvement for that week.
- Goal examples:
  - "Spend $10 less on coffee than last week" (amount-based).
  - "No dining out on weekdays" (behaviour-based).
  - "Stay under $200 on Wants" (category-based).
- At week end, a non-blocking reminder asks: "Did you achieve your goal?" — the user marks it as achieved or not (no guilt, just tracking).
- Week summary shows the goal and its status (✓ achieved, ✗ not yet, — not set).
- A **Kaizen Dashboard** report (Feature 134 mentions this; coordinated here) shows a rolling 12-week view of Kakeibo category spending with micro-goal outcomes overlaid.
- Goals are non-gamified and non-social — they're for the user's own learning, not external validation.

## Domain Model Changes

### New Entity: `KaizenGoal`

```csharp
public class KaizenGoal
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// The Monday of the ISO week this goal applies to.
    /// Used as the week identifier (ISO 8601).
    /// </summary>
    public DateOnly WeekStartDate { get; set; }
    
    /// <summary>
    /// Free-text description of the goal.
    /// Examples: "Spend $10 less on coffee", "No dining out on weekdays", "Stay under $200 on Wants"
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Optional numeric target (e.g., $10 savings, $200 limit).
    /// If provided, IsAchieved can be computed from actual spend.
    /// If null, IsAchieved must be set manually by user.
    /// </summary>
    public decimal? TargetAmount { get; set; }
    
    /// <summary>
    /// Optional Kakeibo category this goal applies to.
    /// If null, goal is for total spending or a specific BudgetCategory.
    /// </summary>
    public KakeiboCategory? KakeiboCategory { get; set; }
    
    /// <summary>
    /// Whether this goal was achieved.
    /// Set by user at week end (manual) or computed from actual spend (if TargetAmount provided).
    /// </summary>
    public bool IsAchieved { get; set; } = false;
    
    /// <summary>
    /// Audit fields.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    
    /// <summary>
    /// Foreign key to user.
    /// </summary>
    public Guid UserId { get; set; }
    // public virtual User User { get; set; }
}
```

### EF Core Configuration

In `KaizenGoalConfiguration` (Infrastructure):

```csharp
builder.HasKey(g => g.Id);
builder.Property(g => g.WeekStartDate).IsRequired();
builder.Property(g => g.Description).IsRequired().HasMaxLength(500);
builder.Property(g => g.TargetAmount).HasPrecision(19, 2);
builder.Property(g => g.KakeiboCategory).HasConversion<int?>();
builder.Property(g => g.IsAchieved).IsRequired();
builder.Property(g => g.CreatedAtUtc).IsRequired();
builder.Property(g => g.UpdatedAtUtc).IsRequired();
builder.Property(g => g.UserId).IsRequired();

// Composite unique key: one goal per user per week
builder.HasIndex(g => new { g.UserId, g.WeekStartDate }).IsUnique();
```

### Database Migration

**Schema:**

```sql
CREATE TABLE "KaizenGoals" (
    "Id" uuid PRIMARY KEY,
    "UserId" uuid NOT NULL,
    "WeekStartDate" date NOT NULL,
    "Description" varchar(500) NOT NULL,
    "TargetAmount" numeric(19, 2) NULL,
    "KakeiboCategory" int NULL,
    "IsAchieved" boolean NOT NULL DEFAULT false,
    "CreatedAtUtc" timestamp NOT NULL,
    "UpdatedAtUtc" timestamp NOT NULL,
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    UNIQUE ("UserId", "WeekStartDate")
);

CREATE INDEX idx_kaizen_goals_user_week 
    ON "KaizenGoals" ("UserId", "WeekStartDate");
```

## API Changes

### Create/Update Kaizen Goal

**Endpoints:**

- `POST /api/v1/goals/kaizen/week/{weekStart}` — Create a goal for a week (weekStart is ISO Monday, format: YYYY-MM-DD).
- `PUT /api/v1/goals/kaizen/{goalId}` — Update an existing goal.

**Request DTO:**

```csharp
public class CreateKaizenGoalRequest
{
    /// <summary>
    /// Goal description (e.g., "Spend $10 less on coffee").
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Optional numeric target.
    /// </summary>
    public decimal? TargetAmount { get; set; }
    
    /// <summary>
    /// Optional Kakeibo category scope.
    /// </summary>
    public KakeiboCategory? KakeiboCategory { get; set; }
}

public class UpdateKaizenGoalRequest
{
    public string Description { get; set; }
    public decimal? TargetAmount { get; set; }
    public KakeiboCategory? KakeiboCategory { get; set; }
    public bool IsAchieved { get; set; }  // User can mark as achieved/not
}
```

**Validation:**

- `Description` max 500 chars, required.
- `TargetAmount` >= 0 if provided.
- `KakeiboCategory` must be one of the four valid values if provided.
- User can only create/update their own goals (authorization).
- One goal per user per week (unique constraint enforced).

**Response DTO:**

```csharp
public class KaizenGoalResponse
{
    public Guid Id { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public string Description { get; set; }
    public decimal? TargetAmount { get; set; }
    public KakeiboCategory? KakeiboCategory { get; set; }
    public bool IsAchieved { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
```

### Get Kaizen Goal

**Endpoint:** `GET /api/v1/goals/kaizen/week/{weekStart}`

**Response:** `KaizenGoalResponse` (or null if not set).

### Get Kaizen Goals (Range)

**Endpoint:** `GET /api/v1/goals/kaizen?from={weekStart}&to={weekEnd}`

**Response:**

```csharp
public class KaizenGoalListResponse
{
    public List<KaizenGoalResponse> Goals { get; set; }
    public int Count { get; set; }
}
```

Used to fetch goals for a 12-week rolling window (for Kaizen Dashboard in Feature 134 reports).

### Delete Kaizen Goal

**Endpoint:** `DELETE /api/v1/goals/kaizen/{goalId}`

Allows users to remove a goal if they change their mind.

## UI Changes

### 1. Week Summary Goal Card

**Component affected:** Week summary row (Feature 134's `WeekSummary.razor`)

**Changes:**

- Below the Kakeibo breakdown bar, add a **goal card** showing:
  - If goal exists: "Goal: [description]" + status badge (✓ achieved, ✗ not yet, — ongoing)
  - If no goal: "Set a goal for this week" CTA button
- **Interaction:**
  - Clicking "Set a goal..." or the goal card opens a lightweight **goal-setting modal**.
  - Clicking the status badge (if week is complete) toggles achievement status (✓ ↔ ✗).

**Visibility:** Only shown if feature flag `Features:Kaizen:MicroGoals` is enabled.

### 2. Goal-Setting Modal

**New component:** `src/BudgetExperiment.Client/Components/Goals/KaizenGoalModal.razor`

**Structure:**

1. **Header:** "Set a goal for the week of [Monday–Sunday]"

2. **Goal Description Field:**
   - Text input (500 chars max, with counter)
   - Placeholder: "e.g., Spend $10 less on coffee, No dining out on weekdays, Stay under $200 on Wants"
   - Required field

3. **Target Amount (Optional):**
   - Number input (currency)
   - Label: "Target amount (optional)"
   - Placeholder: "$0.00"
   - Used to auto-compute achievement if provided

4. **Category Scope (Optional):**
   - Radio buttons or dropdown: "Any", "Essentials", "Wants", "Culture", "Unexpected"
   - Label: "Category this goal applies to (optional)"
   - Defaults to "Any"

5. **Help Text:**
   - Below the form: "A micro-goal is a small improvement you choose for this week. It doesn't need to be ambitious — consistency matters more than size. You'll review it at week end."

6. **Action Buttons:**
   - "Set Goal" (saves and closes modal)
   - "Cancel" (closes without saving)
   - "Clear Goal" link (if editing existing goal; deletes the goal)

**Form validation:**

- Description is required and non-empty.
- TargetAmount >= 0 if provided.
- All inputs validated before submit.

### 3. Week-End Goal Achievement Reminder

**Component:** Small inline reminder/prompt in the week summary (post-week-end)

**Trigger:**

- If current week has passed (week ending date is in the past) and a goal exists, show a non-blocking prompt:
  - "How did it go? Did you achieve your goal?" with two buttons: "✓ Yes" and "✗ No"
  - Clicking either button updates `IsAchieved` and dismisses the prompt.
  - If no goal is set, just show "This week passed without a goal" (no action).

**UX note:** This reminder is gentle and non-judgmental. Whether achieved or not, the emphasis is on learning, not perfection.

**Visibility:** Only shown if feature flag `Features:Kaizen:MicroGoals` is enabled.

### 4. Kaizen Dashboard Report (Feature 134 Enhancement)

This feature integrates with the reports mentioned in Feature 134. A new **Kaizen Dashboard** shows:

- **12-week rolling view** (horizontal axis: weeks, vertical axis: spend by Kakeibo category).
- **Stacked area chart** showing Essentials (bottom), Wants, Culture, Unexpected (top).
- **Micro-goal outcomes overlaid:** Each week shows a ✓ (achieved) or ✗ (not achieved) badge at the top of the chart.
- **Trend line:** Optional second chart showing "improvable spend" (Wants + Unexpected) over the 12 weeks.
- **Summary stats:** Average weekly spend by category, goal achievement rate (X% of weeks met goals).

**Entry point:** Link from reports dashboard or calendar.

## Service Layer

### KaizenGoalService (Application)

```csharp
public interface IKaizenGoalService
{
    Task<KaizenGoalResponse> GetOrCreateAsync(DateOnly weekStart, Guid userId);
    Task<KaizenGoalResponse> UpdateAsync(Guid goalId, UpdateKaizenGoalRequest request, Guid userId);
    Task<List<KaizenGoalResponse>> GetRangeAsync(DateOnly fromWeek, DateOnly toWeek, Guid userId);
    Task DeleteAsync(Guid goalId, Guid userId);
    Task<bool> IsGoalAchievedAsync(Guid goalId, Guid userId);  // Optionally compute from actual spend
}
```

Responsibilities:

- CRUD for `KaizenGoal`.
- Optional: compute goal achievement from actual spend (if `TargetAmount` is set and applies to a specific category).
- Authorization checks.

## Feature Flag

**Name:** `Features:Kaizen:MicroGoals`

**Default:** `true`

## Acceptance Criteria

- [ ] `KaizenGoal` entity created with all required fields.
- [ ] EF Core configuration includes composite unique key (UserId, WeekStartDate).
- [ ] Database migration creates schema with correct column types and indexes.
- [ ] Create endpoint `POST /api/v1/goals/kaizen/week/{weekStart}` works.
- [ ] Get endpoint `GET /api/v1/goals/kaizen/week/{weekStart}` returns goal or null.
- [ ] Update endpoint `PUT /api/v1/goals/kaizen/{goalId}` allows marking achievement status.
- [ ] Delete endpoint `DELETE /api/v1/goals/kaizen/{goalId}` removes goal.
- [ ] Range endpoint `GET /api/v1/goals/kaizen?from=...&to=...` supports 12-week queries (for Kaizen Dashboard).
- [ ] `KaizenGoalModal.razor` component renders with description, target amount, and category fields.
- [ ] Modal validates description (required, max 500 chars) and target amount (>= 0 if provided).
- [ ] Week summary card displays goal and status badge (✓/✗) if goal exists.
- [ ] Week summary card shows "Set a goal..." CTA if no goal.
- [ ] Clicking "Set a goal..." or card opens goal-setting modal.
- [ ] Goal-end reminder prompt appears for weeks past their end date.
- [ ] Reminder allows user to mark goal as achieved/not achieved.
- [ ] Feature flag `Features:Kaizen:MicroGoals` gates all UI (hidden when disabled).
- [ ] API endpoints validate authorization (users can only access their own goals).
- [ ] All text fields validate char limits (Description: 500 max).
- [ ] Kaizen Dashboard report shows 12-week rolling view with goal outcomes overlaid.
- [ ] All existing tests pass; new tests for service, endpoints, authorization, components, and goal achievement logic.
- [ ] Accessibility: labels, ARIA, keyboard navigation in modal and week summary.

## Implementation Order

1. **Create `KaizenGoal` entity** (Domain).
2. **Create EF Core configuration** with unique constraint (Infrastructure).
3. **Create database migration** (Infrastructure).
4. **Create `KaizenGoalService`** (Application).
5. **Create API controller/endpoints** for CRUD and range queries.
6. **Create `KaizenGoalModal.razor` component** (Blazor UI).
7. **Enhance week summary card** to show goal and "Set a goal..." CTA (Feature 134 integration).
8. **Implement week-end reminder prompt** (conditional rendering in week summary).
9. **Create Kaizen Dashboard report component** (Feature 134 reports integration).
10. **Implement feature flag gating** in all UI and API.
11. **Add tests** for service, endpoints, authorization, components, and end-to-end flows.

**Dependencies:**
- Feature 131 (KakeiboCategory for goal scoping).
- Feature 134 (Calendar week summary UI and reports dashboard).
- Feature 129b (feature flag infrastructure).

**Related:** Feature 135 (Monthly Reflection Panel) can reference micro-goal outcomes for contextual insights (e.g., "You achieved 3 of 4 weeks' goals this month").
