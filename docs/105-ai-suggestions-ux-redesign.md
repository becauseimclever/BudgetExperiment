# Feature 105: AI Suggestions UX Redesign

> **Status:** Done
> **Priority:** Medium
> **Dependencies:** None (existing AI features functional)

## Overview

Redesign the AI suggestions experience to provide a clear, logical flow that guides users through suggestion discovery, review, and action. The current implementation has two separate pages (`/ai/suggestions` for rule suggestions and `/category-suggestions` for category suggestions) with overlapping concepts, dense card layouts, and no clear user journey. This feature consolidates and simplifies the experience into a single, approachable page.

## Problem Statement

### Current State

The AI suggestions feature is split across **two independent pages** with different navigation entries, different layouts, and different interaction patterns:

1. **Smart Insights** (`/ai/suggestions` — [AiSuggestions.razor](../src/BudgetExperiment.Client/Pages/AiSuggestions.razor))
   - Generates *rule* suggestions (NewRule, Optimization, Consolidation, Conflict, Unused)
   - Uses `SuggestionList` → `SuggestionCard` → `SuggestionDetailDialog` component chain
   - Has `AnalysisProgressDialog` (6-step simulated progress) and `AnalysisSummaryCard`
   - Toolbar with "Run AI Analysis" + "Refresh" + status badge
   - Filter by type + sort dropdown in `SuggestionList`
   - Each card shows: type badge, confidence gauge, title, description, pattern (code block), category tag, transaction count, Accept/Dismiss/Details buttons, thumbs up/down feedback, sample matches
   - Onboarding panel shown when AI is not configured

2. **Category Suggestions** (`/category-suggestions` — [CategorySuggestions.razor](../src/BudgetExperiment.Client/Pages/CategorySuggestions.razor))
   - Generates *category* suggestions (new categories based on uncategorized transactions)
   - Uses `CategorySuggestionCard` directly (no list wrapper)
   - Has Pending/Dismissed tabs, bulk accept, accept dialog with rules opt-in, rules preview modal
   - Each card shows: checkbox, name, status badge, confidence, transaction count, pattern chips, Accept/Dismiss buttons

**UX issues:**
- **Fragmented navigation** — Two nav items ("Smart Insights" and "Category Suggestions") under AI Tools for what users perceive as one concept: "AI help with my budget"
- **Information overload on cards** — SuggestionCard shows 8+ data points simultaneously (type, confidence, title, description, pattern, match type, category, impact, samples, feedback) without visual hierarchy
- **No guided workflow** — Users land on a list of suggestions with no context about *why* they should care or *what order* to act
- **Inconsistent patterns** — Rule suggestions use a detail dialog; category suggestions use inline expand. Accept flows differ (one-click vs. modal with options)
- **Simulated progress** — The 6-step analysis progress dialog uses `Task.Delay(500)` to fake steps, which feels artificial
- **No prioritization signal** — High-confidence, high-impact suggestions look identical to low-value ones. No visual urgency cues
- **No "done" state** — After acting on all suggestions, there's no celebratory or confirmatory state

### Target State

- **Single unified page** at `/ai` (or `/ai/suggestions`) that handles both rule and category suggestions
- **Clear 3-phase flow**: Configure → Analyze → Review & Act
- **Progressive disclosure** — Cards show essential info (title, confidence, impact) with expandable details
- **Visual prioritization** — High-confidence/high-impact suggestions surface first with visual emphasis
- **Consistent action patterns** — Accept and dismiss work the same way for rules and categories
- **Meaningful empty/done states** — Clear guidance when no suggestions exist and celebration when all are handled
- **Honest progress** — Analysis progress reflects real API state rather than simulated steps

---

## User Stories

### US-105-001: Unified Suggestions Page

**As a** user
**I want to** see all AI suggestions (rules and categories) in one place
**So that** I don't have to navigate between two pages to act on AI recommendations.

**Acceptance Criteria:**
- [x] Single page at `/ai` shows both rule suggestions and category suggestions
- [x] Nav menu collapses two AI sub-items into a single "AI Suggestions" link
- [x] Old routes (`/ai/suggestions`, `/category-suggestions`) redirect to `/ai`
- [x] Suggestions are grouped by type with clear section headers (e.g., "New Categories", "Rule Improvements")

### US-105-002: Simplified Suggestion Cards

**As a** user
**I want to** quickly scan suggestions and understand their value
**So that** I can decide which to accept without reading dense technical details.

**Acceptance Criteria:**
- [x] Card shows only: title/name, confidence indicator (visual bar/dot, not percentage), impact summary (e.g., "affects 42 transactions"), and primary action
- [x] Technical details (pattern, match type, reasoning, sample descriptions) are hidden behind an expandable "Details" section
- [x] High-confidence suggestions (≥80%) have a subtle visual highlight
- [x] Cards have consistent layout for both rule and category suggestion types

### US-105-003: Guided Review Flow

**As a** user
**I want to** be guided through suggestions in a logical order
**So that** I can efficiently review and act on the most impactful ones first.

**Acceptance Criteria:**
- [x] Suggestions default-sort by a composite score (confidence × impact)
- [x] An optional "Review Mode" lets users step through suggestions one at a time (Next/Skip/Accept/Dismiss)
- [x] After all suggestions are handled, a completion state is shown ("All caught up!")
- [x] Batch actions are available: "Accept All High-Confidence", "Dismiss All"

### US-105-004: Honest Analysis Progress

**As a** user
**I want to** see real progress during AI analysis
**So that** I know the system is working and roughly how long it will take.

**Acceptance Criteria:**
- [x] Progress indicator shows a single honest state: "Analyzing..." with elapsed time
- [x] Remove simulated 6-step progression (`SimulateProgressSteps` with `Task.Delay`)
- [x] Result summary appears inline after analysis completes (not in a separate dialog)
- [x] Analysis errors show inline with retry option

### US-105-005: Setup & Empty State

**As a** user
**I want to** understand what AI suggestions are and how to get started
**So that** I can configure AI and generate my first suggestions.

**Acceptance Criteria:**
- [x] When AI is not configured, show a clear setup prompt with link to Settings
- [x] When AI is configured but no suggestions exist, show "Run your first analysis" CTA
- [x] Empty state explains value proposition in 1-2 sentences, not a 5-feature list

---

## Technical Design

### Page Consolidation

```
Before:
  NavMenu
    └── AI Tools (expandable)
        ├── Smart Insights → /ai/suggestions → AiSuggestions.razor
        └── Category Suggestions → /category-suggestions → CategorySuggestions.razor

After:
  NavMenu
    └── AI Suggestions → /ai → AiSuggestions.razor (unified)

  /ai/suggestions → redirect to /ai (backward compat)
  /category-suggestions → redirect to /ai (backward compat)
```

### Component Restructure

```
Before (9 AI components):
  AiOnboardingPanel.razor        — setup guidance (5 features, 4 steps)
  AiSettingsForm.razor           — settings (stays in Settings page)
  AiStatusBadge.razor            — status indicator
  AnalysisProgressDialog.razor   — 6-step fake progress modal
  AnalysisSummaryCard.razor      — post-analysis stats card
  CategorySuggestionCard.razor   — category suggestion card
  SuggestionCard.razor           — rule suggestion card
  SuggestionDetailDialog.razor   — rule detail modal
  SuggestionList.razor           — rule list with filters

After (simplified):
  AiSetupBanner.razor            — streamlined setup/empty state (replaces AiOnboardingPanel)
  AiStatusBadge.razor            — status indicator (keep as-is)
  AiSettingsForm.razor           — settings (stays in Settings page, unchanged)
  SuggestionCard.razor           — unified card for both types (simplified, expandable)
  SuggestionDetailPanel.razor    — inline expandable detail (replaces modal dialog)
  SuggestionGroup.razor          — grouped list with section header + batch actions
  AnalysisInlineProgress.razor   — honest inline progress (replaces dialog)
```

### Unified SuggestionCard Design

```
┌─────────────────────────────────────────────────────┐
│ ● High Confidence          affects 42 transactions  │
│                                                     │
│ Create rule for "AMAZON" purchases                  │
│ Categorize matching transactions as "Shopping"      │
│                                                     │
│  [✓ Accept]  [✗ Dismiss]  [▾ Details]               │
│                                                     │
│ ┌─ Details (collapsed by default) ────────────────┐ │
│ │ Pattern: AMAZON*  (Contains)                    │ │
│ │ Reasoning: Found 42 transactions matching...    │ │
│ │ Sample matches: AMAZON.COM, AMAZON PRIME, ...   │ │
│ │ Feedback: 👍 👎                                 │ │
│ └─────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
```

### Suggestion Grouping

Suggestions are displayed in priority-ordered groups:

1. **New Categories** — Category suggestions (highest value for new users)
2. **New Rules** — Rule suggestions for uncategorized patterns
3. **Optimizations** — Pattern improvements for existing rules
4. **Conflicts & Cleanup** — Conflicts, consolidations, unused rules

Each group has a header with count and batch action ("Accept All" for high-confidence items).

---

## Implementation Plan

### Phase 1: Unified Page & Simplified Cards

**Tasks:**
- [x] Create unified `AiSuggestions.razor` that loads both rule and category suggestions
- [x] Create new `SuggestionCard.razor` with progressive disclosure (collapsed details)
- [x] Create `SuggestionGroup.razor` for grouped display with section headers
- [x] Add composite scoring (confidence × impact) for default sort
- [x] Add redirect routes for backward compatibility
- [x] Update NavMenu to single "AI Suggestions" link
- [x] Remove `CategorySuggestions.razor` page
- [x] Remove `SuggestionList.razor` (replaced by grouping in page)
- [x] Remove `SuggestionDetailDialog.razor` (replaced by inline expand)
- [x] ViewModel extraction: create `AiSuggestionsViewModel` following established pattern

### Phase 2: Honest Progress & Empty States

**Tasks:**
- [x] Replace `AnalysisProgressDialog` with `AnalysisInlineProgress` (inline, no fake steps)
- [x] Remove `SimulateProgressSteps` and `Task.Delay` calls
- [x] Replace `AiOnboardingPanel` with streamlined `AiSetupBanner`
- [x] Add "All caught up!" completion state
- [x] Remove `AnalysisSummaryCard` (integrate summary into inline progress result)

### Phase 3: Review Mode & Batch Actions

**Tasks:**
- [x] Add optional "Review Mode" (step-through one at a time)
- [x] Add batch actions per group ("Accept All High-Confidence")
- [x] Add "Dismiss All" with confirmation
- [x] Handle dismissed suggestions (restore capability in a collapsible section or filter)

### Phase 4: Tests & Cleanup

**Tasks:**
- [x] Create `AiSuggestionsViewModelTests` with comprehensive coverage
- [x] Update existing bUnit tests for renamed/restructured components
- [x] Remove orphaned component files and CSS
- [x] Verify all existing E2E tests still pass
- [x] Update this document status to Done

---

## Testing Strategy

### Unit Tests (ViewModel)

- `AiSuggestionsViewModelTests` — composite scoring, grouping logic, accept/dismiss/feedback, analysis lifecycle, empty/done states

### Component Tests (bUnit)

- `SuggestionCardTests` — render, expand/collapse details, accept/dismiss callbacks, confidence highlighting
- `SuggestionGroupTests` — section header, count, batch actions
- `AiSetupBannerTests` — states (not configured, no suggestions, configured)
- `AnalysisInlineProgressTests` — analyzing state, elapsed time, completion, error

### Integration Tests (Existing)

- Existing bUnit page tests adapted for unified page
- E2E tests verified for non-regression

---

## Migration Notes

- **Breaking nav change**: `/category-suggestions` route removed (redirect added)
- **Component removal**: `AnalysisProgressDialog`, `AnalysisSummaryCard`, `AiOnboardingPanel`, `CategorySuggestionCard`, `SuggestionDetailDialog`, old `SuggestionList` — all replaced
- **Kept unchanged**: `AiSettingsForm` (lives in Settings page), `AiStatusBadge` (reused)
- **CategorySuggestionApiService** and **IAiApiService** interfaces unchanged — only the UI layer is redesigned

---

## References

- Feature 061: Restore dismissed category suggestions (archived)
- Feature 070: Fix AI suggestions JSON extraction (archived)
- Feature 097: ViewModel extraction pattern (established)
