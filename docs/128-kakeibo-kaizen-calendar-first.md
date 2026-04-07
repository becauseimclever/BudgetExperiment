# Feature 128: Kakeibo + Kaizen — Calendar-First Mindful Budgeting
> **Status:** Planning

## Overview

This feature reorients the application's soul around two complementary Japanese philosophies: **Kakeibo** (家計簿, "household ledger") and **Kaizen** (改善, "continuous improvement"). The calendar is not just the homepage — it becomes the primary interaction surface for every financial decision. Every day on the calendar is a journal entry. Every week is a chapter. Every month is a reflection.

Kakeibo teaches mindful, intentional recording of spending — not just tracking numbers but understanding *why* money moves. Kaizen teaches that small, consistent improvements compound into significant change over time. Together they form a philosophy that transforms budgeting from a chore into a habit of self-awareness.

The application already has strong calendar infrastructure. This feature does not rebuild — it **deepens meaning** into what exists.

---

## Problem Statement

### Current State

The application today is a capable transaction tracker with a calendar view. The calendar is the homepage, but it functions as a *display* — transactions are shown, balances are computed, recurring items are projected. There is no philosophical rhythm guiding how a user interacts with their money over time.

- The calendar shows *what happened* but does not invite reflection
- Budget goals are purely numerical targets with no intention-setting ritual
- The four Kakeibo questions (income? save? spend? improve?) are never asked
- Spending categories have no moral/intentional weight — a dinner is the same as rent
- There is no end-of-week or end-of-month ritual baked into the UX
- Kaizen improvement is invisible — the user cannot see whether this month was better than last month by any measure that matters to *them*
- Reports are separate pages, not part of the daily calendar rhythm

### Target State

The calendar becomes the household ledger (kakeibo). Opening the app opens the calendar. The calendar asks, at the right moments:

- **Start of month:** *What do you want to save this month? What are you grateful for from last month?*
- **End of week:** *Here's how your week breaks down: Essentials, Wants, Culture, Unexpected. How does that feel?*
- **End of month:** *You set a goal to save $X. You saved $Y. What did you do well? What would you change?*
- **Each day:** Transactions are quick to add and categorized not just by account but by Kakeibo intent

The calendar gains a **spending heatmap** — green days are light, amber days are moderate, red days are heavy. This is visual management (a Kaizen principle): see your patterns at a glance without reading a table.

Kaizen shows up as **micro-goals**: small, self-chosen weekly improvements ("spend $10 less on coffee than last week"). Achievements are quiet and non-gamified — a simple checkmark, not confetti.

---

## The Four Kakeibo Questions (Monthly Rhythm)

These four questions structure the monthly experience:

| # | Question | When | Where |
|---|----------|------|-------|
| 1 | How much income did I receive? | Month start / end | Monthly Reflection |
| 2 | How much did I want to save? | Month start | Intention modal |
| 3 | How much did I actually spend? | Month end | Reflection modal |
| 4 | How can I improve next month? | Month end | Reflection text field |

---

## The Four Kakeibo Categories

Every transaction gets a `KakeiboCategory` — a layer of intentional meaning on top of the existing budget category system:

| Category | Japanese | Meaning | Examples |
|----------|----------|---------|---------|
| **Essentials** | 必要 (Hitsuyō) | Things needed to live | Rent, groceries, utilities, transport, medication |
| **Wants** | 欲しい (Hoshii) | Things enjoyed but not essential | Dining out, subscriptions, clothing beyond need |
| **Culture** | 文化 (Bunka) | Things that enrich mind and spirit | Books, courses, concerts, museum, travel |
| **Unexpected** | 予期しない (Yokishinai) | Things that were not planned | Medical copay, car repair, gift, emergency |

`KakeiboCategory` is a classification *overlay* on the existing category system, not a replacement. A `BudgetCategory` like "Restaurants" defaults to **Wants** but the user can override per-transaction.

---

## User Stories

### Monthly Ritual

#### US-128-001: Month Intention Setting
**As a** user opening the calendar at the start of a new month  
**I want to** be prompted to set a savings intention for the month  
**So that** I begin the month with conscious financial purpose

**Acceptance Criteria:**
- [ ] On first visit to a month's calendar where no `MonthlyReflection` exists, a non-blocking prompt appears at the top of the calendar
- [ ] Prompt asks: "What do you want to save this month?" (amount input) and "Any intention for this month?" (optional free text, 280 chars max)
- [ ] Dismissing without filling in is allowed — prompt recedes but an "Add intention" link remains in the month header
- [ ] Submitted intention is saved as a `MonthlyReflection` for that year/month
- [ ] The savings target is shown in the calendar month header as a progress bar: "Saved $X of $Y goal"

#### US-128-002: Month-End Reflection
**As a** user viewing a completed (past) month's calendar  
**I want to** see a reflection summary and be prompted to journal about the month  
**So that** I close each month with understanding, not just numbers

**Acceptance Criteria:**
- [ ] For any past month, a "Month Reflection" panel is accessible from the calendar header
- [ ] Panel shows the four Kakeibo answers automatically computed: income, savings goal, actual savings, Kakeibo category breakdown
- [ ] A free-text "How could I improve?" field is shown alongside the computed numbers
- [ ] Reflection text is saved to `MonthlyReflection.ReflectionText`
- [ ] Past reflections are viewable in a dedicated **Reflection History** section (accessible from reports or calendar)

---

### Weekly Review

#### US-128-003: Kakeibo Week Summary
**As a** user viewing the calendar  
**I want to** see each week's spending broken down by Kakeibo category  
**So that** I can spot spending patterns at a weekly cadence without leaving the calendar

**Acceptance Criteria:**
- [ ] The existing `WeekSummary` component gains a Kakeibo breakdown: four mini-bars (Essentials / Wants / Culture / Unexpected) with amounts
- [ ] Clicking a Kakeibo category bar filters the week's day cells to highlight only that category's transactions
- [ ] The weekly Kaizen micro-goal progress (if set) is shown inline in the week summary

---

### Daily Entry

#### US-128-004: Kakeibo Category on Transaction Entry
**As a** user adding a transaction  
**I want to** assign a Kakeibo category (Essentials / Wants / Culture / Unexpected)  
**So that** each transaction carries intentional meaning, not just an account category

**Acceptance Criteria:**
- [ ] The add/edit transaction modal includes a Kakeibo category selector (four clear icons + labels)
- [ ] Default Kakeibo category is derived from the selected `BudgetCategory`'s default mapping (configurable per category)
- [ ] Kakeibo category is displayed as a small badge on day cells that have transactions
- [ ] The calendar day cell colour tinting considers Kakeibo category (optional: Wants = amber tint on high-spend days)

#### US-128-005: Spending Heatmap Overlay
**As a** user viewing the monthly calendar  
**I want to** see each day's spending intensity colour-coded on the calendar grid  
**So that** I can instantly identify heavy-spending days without reading numbers

**Acceptance Criteria:**
- [ ] Each calendar day cell has a subtle background tint: no-spend = neutral, light = pale green, moderate = amber, heavy = soft red (using existing CSS design system, no harsh colours)
- [ ] Thresholds are relative to the user's *own* daily average, not a fixed number (Kaizen: compare yourself to yourself)
- [ ] A toggle in the calendar toolbar enables/disables the heatmap overlay
- [ ] Heatmap overlay state is persisted in user preferences

---

### Kaizen Micro-Goals

#### US-128-006: Weekly Micro-Goal Setting
**As a** user  
**I want to** set a small, concrete weekly improvement goal  
**So that** I make continuous small progress rather than chasing perfection

**Acceptance Criteria:**
- [ ] Each week, the user can set one `KaizenGoal`: a category (or Kakeibo category), a target direction (less than / same as), and a reference amount (last week's spend in that category, auto-filled)
- [ ] The week summary shows live progress toward the goal
- [ ] At week end, the goal outcome is computed and shown: ✓ met / ✗ missed, with no blame language — just "You spent $X vs. your goal of $Y"
- [ ] Goals are optional. If not set, no UI pressure is shown

#### US-128-007: Improvement Trend (Kaizen View)
**As a** user  
**I want to** see whether my spending habits are improving over time  
**So that** I can see the compound effect of small weekly improvements

**Acceptance Criteria:**
- [ ] A **Kaizen Dashboard** section in Reports shows a rolling 12-week view of Kakeibo category spending
- [ ] Each week is a column; each Kakeibo category is a coloured segment
- [ ] A trend line shows total "Wants + Unexpected" spend over time (the improvable categories)
- [ ] Micro-goal outcomes are annotated on the chart (✓ weeks vs ✗ weeks)

---

### Calendar-First Navigation

#### US-128-008: Calendar as Entry Point for All Workflows
**As a** user  
**I want to** reach any financial workflow from the calendar without going to a separate page  
**So that** the calendar is the hub, not just the home

**Acceptance Criteria:**
- [ ] From a calendar day cell, I can: add transaction, add transfer, view recurring items due that day, mark a recurring item cleared
- [ ] From a calendar month header, I can: navigate to that month's budget overview, start the month reflection, view the Kaizen trend for that month
- [ ] From a calendar week summary, I can: set a Kaizen micro-goal for that week, drill into transactions for that week (currently filtered list view)
- [ ] The transaction list page retains a "View on Calendar" link that jumps to the correct day

---

## Technical Design

### Architecture Changes

This feature adds a thin **Reflection layer** above the existing Calendar + Budget layers. No major architectural changes — the existing `CalendarGridService`, `DayDetailService`, and balance chain remain untouched. New services are additive.

```
Calendar (existing)
├── CalendarGridService     (unchanged)
├── DayDetailService        (unchanged)
└── [NEW] KakeiboService    ← Kakeibo category aggregation per week/month

Budget (existing)
├── BudgetGoalService       (unchanged)
└── [NEW] ReflectionService ← Monthly intention + reflection CRUD

Reports (existing)
├── ReportService           (unchanged)
└── [NEW] KaizenGoalService ← Weekly micro-goal CRUD + outcome computation
```

### Domain Model

#### New Value Object: `KakeiboCategory` (enum)
```csharp
// src/BudgetExperiment.Domain/Kakeibo/KakeiboCategory.cs
public enum KakeiboCategory
{
    Essentials = 1,   // 必要 — needs
    Wants      = 2,   // 欲しい — desires
    Culture    = 3,   // 文化 — enrichment
    Unexpected = 4,   // 予期しない — surprises
}
```

#### Domain Change: `BudgetCategory` — add default Kakeibo mapping
```csharp
// Add to BudgetCategory entity
public KakeiboCategory DefaultKakeiboCategory { get; private set; }
    = KakeiboCategory.Wants; // sensible default

public void SetDefaultKakeiboCategory(KakeiboCategory category)
    => DefaultKakeiboCategory = category;
```

#### Domain Change: `Transaction` — add Kakeibo override
```csharp
// Add to Transaction entity
public KakeiboCategory? KakeiboCategory { get; private set; } // null = use category default

public void SetKakeiboCategory(KakeiboCategory? category)
    => KakeiboCategory = category;
```

#### New Entity: `MonthlyReflection`
```csharp
// src/BudgetExperiment.Domain/Kakeibo/MonthlyReflection.cs
public sealed class MonthlyReflection
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }

    // Set at start of month
    public decimal? SavingsGoal { get; private set; }
    public string? Intention { get; private set; }  // 280 char max

    // Set at end of month
    public string? ReflectionText { get; private set; }  // free text
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
}
```

#### New Entity: `KaizenGoal`
```csharp
// src/BudgetExperiment.Domain/Kakeibo/KaizenGoal.cs
public sealed class KaizenGoal
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateOnly WeekStartDate { get; private set; }   // Monday of the goal week

    // Goal definition
    public KakeiboCategory TargetCategory { get; private set; }
    public decimal ReferenceAmount { get; private set; }  // last week's spend (auto-filled)
    public decimal GoalAmount { get; private set; }       // user's target

    // Outcome (computed post-week)
    public decimal? ActualAmount { get; private set; }
    public bool? WasMet { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
```

### API Endpoints

#### Reflection Endpoints
| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/v1/reflections/{year}/{month}` | Get reflection for a month (null if not started) |
| `PUT` | `/api/v1/reflections/{year}/{month}/intention` | Set savings goal + intention text |
| `PUT` | `/api/v1/reflections/{year}/{month}/reflection` | Set end-of-month reflection text |

#### Kakeibo Summary Endpoints
| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/v1/kakeibo/{year}/{month}/summary` | Kakeibo category totals for a month |
| `GET` | `/api/v1/kakeibo/{year}/{month}/weeks` | Kakeibo breakdown by ISO week within the month |

#### Kaizen Goal Endpoints
| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/v1/kaizen/goals/{weekStartDate}` | Get goal for a specific week |
| `PUT` | `/api/v1/kaizen/goals/{weekStartDate}` | Create or update a week's goal |
| `GET` | `/api/v1/kaizen/goals?months=3` | Rolling history for Kaizen dashboard |

#### Transaction Extension
Existing `PUT /api/v1/transactions/{id}` gains `KakeiboCategory` field in the request body. `GET` responses return the effective Kakeibo category (override ?? category default).

### Database Changes

```sql
-- BudgetCategories: add default Kakeibo classification
ALTER TABLE "BudgetCategories" ADD COLUMN "DefaultKakeiboCategory" integer NOT NULL DEFAULT 2;

-- Transactions: add optional Kakeibo override
ALTER TABLE "Transactions" ADD COLUMN "KakeiboCategory" integer NULL;

-- New table: MonthlyReflections
CREATE TABLE "MonthlyReflections" (
    "Id"             uuid NOT NULL PRIMARY KEY,
    "UserId"         uuid NOT NULL,
    "Year"           integer NOT NULL,
    "Month"          integer NOT NULL,
    "SavingsGoal"    numeric(18,2) NULL,
    "Intention"      text NULL,
    "ReflectionText" text NULL,
    "CreatedAtUtc"   timestamp with time zone NOT NULL,
    "UpdatedAtUtc"   timestamp with time zone NOT NULL,
    UNIQUE ("UserId", "Year", "Month")
);

-- New table: KaizenGoals
CREATE TABLE "KaizenGoals" (
    "Id"              uuid NOT NULL PRIMARY KEY,
    "UserId"          uuid NOT NULL,
    "WeekStartDate"   date NOT NULL,
    "TargetCategory"  integer NOT NULL,
    "ReferenceAmount" numeric(18,2) NOT NULL,
    "GoalAmount"      numeric(18,2) NOT NULL,
    "ActualAmount"    numeric(18,2) NULL,
    "WasMet"          boolean NULL,
    "CreatedAtUtc"    timestamp with time zone NOT NULL,
    UNIQUE ("UserId", "WeekStartDate")
);
```

### UI Components

#### New Components
| Component | Location | Purpose |
|-----------|----------|---------|
| `KakeiboIntentionModal.razor` | `Pages/Calendar/` | Month-start intention prompt |
| `KakeiboReflectionPanel.razor` | `Pages/Calendar/` | Month-end four-questions panel |
| `KakeiboWeekBreakdown.razor` | `Components/Calendar/` | Kakeibo bars in WeekSummary |
| `KaizenGoalWidget.razor` | `Components/Calendar/` | Inline micro-goal progress in week summary |
| `KaizenDashboard.razor` | `Pages/Reports/` | 12-week Kaizen trend page |
| `KakeiboSelector.razor` | `Components/Transactions/` | Four-option category picker (icons + labels) |
| `SpendingHeatmapOverlay.razor` | `Components/Calendar/` | Tint layer on CalendarGrid cells |

#### Modified Components
| Component | Change |
|-----------|--------|
| `CalendarGrid.razor` | Accept heatmap intensity data; apply tints to day cells |
| `CalendarDay.razor` | Show Kakeibo badge on days with transactions |
| `WeekSummary.razor` | Embed `KakeiboWeekBreakdown` + `KaizenGoalWidget` |
| `Calendar.razor` (page) | Month header: savings progress bar; reflection prompt; heatmap toggle |
| Transaction add/edit modal | Add `KakeiboSelector` |
| `NavMenu.razor` | Add "Reflections" link under Reports; remove redundant Budget link if absorbed |

#### New DTOs (Contracts)
```csharp
// MonthlyReflectionDto — GET/PUT reflection
public sealed record MonthlyReflectionDto(
    int Year, int Month,
    decimal? SavingsGoal, string? Intention, string? ReflectionText,
    // Computed fields in GET response:
    decimal TotalIncome, decimal TotalSpend, decimal ActualSavings
);

// KakeiboWeekSummaryDto — per-week Kakeibo breakdown
public sealed record KakeiboWeekSummaryDto(
    DateOnly WeekStart, DateOnly WeekEnd,
    decimal Essentials, decimal Wants, decimal Culture, decimal Unexpected
);

// KaizenGoalDto — goal for a week
public sealed record KaizenGoalDto(
    DateOnly WeekStartDate,
    KakeiboCategory TargetCategory,
    decimal ReferenceAmount, decimal GoalAmount,
    decimal? ActualAmount, bool? WasMet
);
```

---

## Implementation Plan

### Phase 1: Kakeibo Classification Layer (Domain + DB)
**Objective:** Add `KakeiboCategory` to the domain. Every transaction has an effective Kakeibo category. No UI changes yet.

**Tasks:**
- [ ] Add `KakeiboCategory` enum to `BudgetExperiment.Domain`
- [ ] Add `DefaultKakeiboCategory` to `BudgetCategory` entity
- [ ] Add `KakeiboCategory?` override to `Transaction` entity
- [ ] EF Core fluent config for both new fields
- [ ] Migration: `AddKakeiboCategory`
- [ ] Unit tests: domain rules (default resolution logic)

**Commit:**
```bash
git commit -m "feat(domain): add KakeiboCategory — Essentials/Wants/Culture/Unexpected

- KakeiboCategory enum in Domain
- BudgetCategory.DefaultKakeiboCategory (default: Wants)
- Transaction.KakeiboCategory nullable override
- EF config + migration

Refs: #128"
```

---

### Phase 2: Monthly Reflection (Domain + API)
**Objective:** Users can record a monthly intention and end-of-month reflection via API.

**Tasks:**
- [ ] `MonthlyReflection` entity + repository interface
- [ ] EF config + migration: `AddMonthlyReflections`
- [ ] `ReflectionService` + `IReflectionService` in Application
- [ ] `ReflectionsController` with GET/PUT intention + reflection endpoints
- [ ] Unit tests: service logic (idempotent upsert, 280-char cap)
- [ ] Integration tests: controller + DB round-trip

**Commit:**
```bash
git commit -m "feat(app,api): monthly reflection — Kakeibo intention + end-of-month journal

- MonthlyReflection entity, repo, migration
- ReflectionService with intention + reflection upsert
- ReflectionsController GET/PUT endpoints

Refs: #128"
```

---

### Phase 3: Kaizen Micro-Goals (Domain + API)
**Objective:** Users can set a weekly improvement goal and see outcomes computed automatically.

**Tasks:**
- [ ] `KaizenGoal` entity + repository interface
- [ ] Migration: `AddKaizenGoals`
- [ ] `KaizenGoalService` — CRUD + outcome computation (compare actual vs. goal post-week)
- [ ] `KaizenController` endpoints
- [ ] Unit tests: outcome computation rules, edge cases (week not yet ended)

**Commit:**
```bash
git commit -m "feat(app,api): Kaizen micro-goals — weekly improvement targets with outcomes

- KaizenGoal entity, repo, migration
- KaizenGoalService computes outcome after week end
- KaizenController CRUD endpoints

Refs: #128"
```

---

### Phase 4: Kakeibo Aggregation Service (Application + API)
**Objective:** The API can return Kakeibo category breakdowns per week and per month.

**Tasks:**
- [ ] `KakeiboService` in Application — aggregates transactions by effective Kakeibo category
- [ ] `GET /api/v1/kakeibo/{year}/{month}/summary` endpoint
- [ ] `GET /api/v1/kakeibo/{year}/{month}/weeks` endpoint
- [ ] Extend `CalendarGridDto` or add `KakeiboWeekSummaryDto[]` alongside it
- [ ] `IBudgetApiService` client methods for new endpoints
- [ ] Unit tests: aggregation logic, null-override resolution

**Commit:**
```bash
git commit -m "feat(app,api): Kakeibo aggregation — monthly + weekly category breakdowns

- KakeiboService aggregates by effective category
- /api/v1/kakeibo/{year}/{month}/summary|weeks
- Client IBudgetApiService methods

Refs: #128"
```

---

### Phase 5: Calendar UI — Intention, Heatmap, Week Breakdown
**Objective:** The calendar surface reflects Kakeibo philosophy visually.

**Tasks:**
- [ ] `KakeiboIntentionModal.razor` — month-start prompt, savings goal input
- [ ] Savings progress bar in calendar month header
- [ ] `SpendingHeatmapOverlay` — intensity tinting on `CalendarDay.razor`
- [ ] Heatmap toggle in calendar toolbar; persist preference
- [ ] `KakeiboWeekBreakdown.razor` embedded in `WeekSummary`
- [ ] Kakeibo badge on day cells
- [ ] bUnit tests for new components

**Commit:**
```bash
git commit -m "feat(client): Kakeibo calendar UI — heatmap, intention prompt, week breakdown

- KakeiboIntentionModal at month start
- Savings progress in month header
- SpendingHeatmapOverlay on CalendarDay
- KakeiboWeekBreakdown in WeekSummary
- Kakeibo badge on day cells

Refs: #128"
```

---

### Phase 6: Month-End Reflection UI + Kaizen Dashboard
**Objective:** Complete the monthly ritual loop and add the Kaizen trend report.

**Tasks:**
- [ ] `KakeiboReflectionPanel.razor` — four questions panel for past months
- [ ] `KaizenGoalWidget.razor` embedded in week summary
- [ ] `KaizenDashboard.razor` — 12-week trend page in Reports
- [ ] Nav link: "Reflections" under Reports section
- [ ] bUnit tests for panels

**Commit:**
```bash
git commit -m "feat(client): month reflection panel + Kaizen dashboard

- KakeiboReflectionPanel for past months (4 questions)
- KaizenGoalWidget in week summary
- KaizenDashboard 12-week trend in Reports

Refs: #128"
```

---

### Phase 7: Transaction Entry — Kakeibo Category Selector
**Objective:** Every transaction can be assigned a Kakeibo category at entry time.

**Tasks:**
- [ ] `KakeiboSelector.razor` component — four icons, accessible
- [ ] Embed in transaction add/edit modal
- [ ] Category → Kakeibo default wiring in the UI
- [ ] Category management page: default Kakeibo mapping per category
- [ ] bUnit tests

**Commit:**
```bash
git commit -m "feat(client): Kakeibo category selector in transaction entry

- KakeiboSelector component (icon + label, accessible)
- Default derived from BudgetCategory mapping
- Category settings: configure default Kakeibo per category

Refs: #128"
```

---

### Phase 8: Documentation & Cleanup
**Objective:** Polish, XML docs, OpenAPI, and README update.

**Tasks:**
- [ ] XML docs on all new public API surface
- [ ] OpenAPI spec describes new endpoints and DTOs
- [ ] Update README with Kakeibo/Kaizen philosophy section
- [ ] Remove any TODO comments
- [ ] Add `kakeibo` and `kaizen` scope to conventional commit reference

---

## Testing Strategy

### Unit Tests
- [ ] `KakeiboCategory` enum default resolution (Transaction override takes priority over Category default)
- [ ] `MonthlyReflection` — savings goal must be positive; intention truncated at 280 chars
- [ ] `KaizenGoal` — outcome computation: met when actual ≤ goal (for "spend less" type)
- [ ] `KakeiboService` — aggregation correctly sums by effective category; handles missing overrides
- [ ] `ReflectionService` — idempotent upsert for intention and reflection text
- [ ] Heatmap threshold computation — relative to user's own average, not fixed

### Integration Tests
- [ ] `ReflectionsController` — GET returns null for month with no reflection; PUT creates then GET returns it
- [ ] `KaizenController` — goal creation and outcome computed after week passes
- [ ] `KakeiboController` — week summary totals match transaction breakdown

### Manual Testing Checklist
- [ ] Open app on 1st of month — intention prompt appears
- [ ] Set savings goal → progress bar shows in header
- [ ] Add transactions with different Kakeibo categories — day badges show
- [ ] Enable heatmap toggle — high-spend days tint correctly
- [ ] View WeekSummary — Kakeibo bars reflect actual transaction distribution
- [ ] View a past month — reflection panel is accessible and answers are pre-populated
- [ ] Set a Kaizen goal — widget shows live progress mid-week
- [ ] Week ends — goal widget shows outcome (met/missed) without blame language

---

## Migration Notes

```bash
# Phase 1
dotnet ef migrations add Feature128_KakeiboCategory \
  --project src/BudgetExperiment.Infrastructure \
  --startup-project src/BudgetExperiment.Api

# Phase 2
dotnet ef migrations add Feature128_MonthlyReflections \
  --project src/BudgetExperiment.Infrastructure \
  --startup-project src/BudgetExperiment.Api

# Phase 3
dotnet ef migrations add Feature128_KaizenGoals \
  --project src/BudgetExperiment.Infrastructure \
  --startup-project src/BudgetExperiment.Api
```

### Data Seeding Note
Existing `BudgetCategory` records will receive `DefaultKakeiboCategory = Wants` as the migration default. After deploying, users should review their categories and reassign Essentials categories (rent, utilities, groceries) to `KakeiboCategory.Essentials`. A one-time "Kakeibo Setup" prompt can guide this on first run.

---

## Security Considerations

- `MonthlyReflection` and `KaizenGoal` are user-scoped — all queries must filter by the authenticated user's ID
- Reflection text is free-form but stored as plain text (no HTML); sanitise on display
- Savings goal and amounts must be non-negative

---

## Performance Considerations

- `KakeiboService` aggregation runs on the same data already loaded for `CalendarGridService` — consider computing Kakeibo totals in the same query pass rather than a second DB round-trip
- Heatmap intensity thresholds are computed client-side from the existing `CalendarGridDto` daily totals — no additional API call needed
- `KaizenGoal` outcome computation runs at query time (not a background job) — outcome is derived from existing transaction data already available

---

## Future Enhancements

- **Kakeibo Setup Wizard** — guided first-run experience to classify all existing categories
- **Shared Household Kakeibo** — two users, one joint reflection, each visible to the other
- **Reflection Export** — export monthly reflections as a PDF journal / CSV
- **AI Reflection Prompts** — contextual prompt suggestions based on actual spending patterns ("You spent 40% more on Wants this month than last — what drove that?")
- **Annual Kakeibo Summary** — end-of-year four questions across all 12 months
- **Fiscal Year Support** — allow custom year start (e.g., April) for non-calendar fiscal years

---

## References

- [Kakeibo — Wikipedia](https://en.wikipedia.org/wiki/Kakeibo)
- [Kaizen — Wikipedia](https://en.wikipedia.org/wiki/Kaizen)
- Fumiko Chiba, *Kakeibo: The Japanese Art of Saving Money* (2017)
- Existing calendar audit: see `.squad` agent history (2026-04-07)
- `docs/COMPONENT-STANDARDS.md` — Blazor component patterns to follow
- `docs/THEMING.md` — design system for heatmap colour choices

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-04-07 | Initial draft — post-pivot from plugin system | Fortinbra + Copilot |
