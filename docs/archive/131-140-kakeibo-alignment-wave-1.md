# Archive: Features 131–140

> Kakeibo alignment wave 1. All features completed and archived.

---

## Feature 131: Budget Categories — Kakeibo Category Routing

> **Status:** Done

**What it did:** Added `KakeiboCategory` field to `BudgetCategory` entity, routing every expense category to one of the four Kakeibo buckets (Essentials / Wants / Culture / Unexpected). Included a Kakeibo Setup Wizard triggered on first login post-migration so existing users confirm or correct routing for all their expense categories. Smart migration defaults applied by category name pattern.

**Key decisions:**
- Foundation piece for all downstream features — all Kakeibo aggregations, calendar heatmaps, and monthly reflection depend on this field
- `KakeiboCategory` is `null` for Income and Transfer categories; only Expense categories are routed

---

## Feature 132: Transaction Entry — Kakeibo Selector

> **Status:** Done

**What it did:** Added `KakeiboSelector` component to the transaction add/edit modal with a nullable `Transaction.KakeiboOverride` field. Defaults to the category's Kakeibo routing but allows per-transaction override, deepening the mindfulness of each entry.

**Key decisions:**
- Feature-flagged (`Features:Kakeibo:TransactionOverride`); users who prefer pure category-driven routing can disable the selector
- Override is per-transaction and non-destructive — category routing remains unchanged

---

## Feature 133: Onboarding — Kakeibo Setup Step

> **Status:** Done

**What it did:** Extended the 4-step onboarding wizard to 5 steps by inserting a dedicated Kakeibo Setup step. Introduces the four Kakeibo categories with explanations and examples, then asks users to confirm or correct Kakeibo routing for their expense categories. Also triggered on first login after the Feature 131 migration for pre-existing users.

---

## Feature 134: Calendar — Kakeibo Enhancements

> **Status:** Done

**What it did:** Added spending intensity heatmap overlay (green/amber/red day-cell tinting), Kakeibo badges on individual day cells showing the predominant bucket, weekly summary bars for Kakeibo category breakdowns, and a month-start intention prompt linking to the Monthly Reflection panel.

**Key decisions:**
- Three feature flags (`SpendingHeatmap`, `CalendarOverlay`, `MonthlyReflectionPrompts`) allow granular rollout of each enhancement independently

---

## Feature 135: Monthly Reflection Panel

> **Status:** Done

**What it did:** Introduced the monthly reflection ritual as a dedicated panel accessible from the calendar. Users answer four Kakeibo questions at month-start (intention + savings goal) and month-end (gratitude + improvement), transforming spending data into a journaling practice. `MonthlyReflection` records stored in DB.

---

## Feature 136: Kaizen Micro-Goals

> **Status:** Done

**What it did:** Added weekly micro-goals — small, self-chosen weekly improvements scoped to a Kakeibo category. `KaizenGoal` entity tracks whether each week's goal was achieved. A 12-week rolling view in reports shows the compound effect of small consistent improvements.

**Key decisions:**
- Feature-flagged (`Features:Kaizen:MicroGoals`), default on; entirely optional — users who prefer strict number tracking can disable it
- Non-gamified and non-judgmental by design: quiet checkmark on success, no confetti or streaks

---

## Feature 137: Kaizen Dashboard Report

> **Status:** Done

**What it did:** Added a 12-week rolling report at `/reports/kaizen-dashboard`. Displays four stacked area lines (one per Kakeibo category) showing weekly spending aggregations, visual month-boundary dividers, and weekly micro-goal outcome indicators (✓/✗ badges per week column).

**Key decisions:**
- Feature-flagged (`Kaizen:Dashboard`), default false during development, true when shipped

---

## Feature 138: Transactions List — Kakeibo Filter and Badge

> **Status:** Done

**What it did:** Added a Kakeibo category filter dropdown and colored badges to the `/transactions` page. The effective Kakeibo category is resolved server-side: `Transaction.KakeiboOverride` takes precedence over `BudgetCategory.KakeiboCategory`; Income/Transfer categories show no badge.

---

## Feature 139: AI Chat — Kakeibo Awareness

> **Status:** Done

**What it did:** Made the AI Chat Assistant Kakeibo-aware: it confirms the Kakeibo bucket in transaction creation messages, asks for clarification when the category is ambiguous (`AskKakeiboCategory` action), includes Kakeibo intent in its reasoning, and supports Kakeibo aggregate queries ("How much did I spend on Wants this week?").

---

## Feature 140: AI Rule Suggestions — Kakeibo Display

> **Status:** Done

**What it did:** AI Rule Suggestions page (`/ai/suggestions`) now shows the Kakeibo bucket alongside each suggested category via a colored badge in `UnifiedSuggestionCard.razor`. The AI also flags surprising Kakeibo routings and can suggest `KakeiboOverride` values when merchant context implies a different intent than the default category mapping.
