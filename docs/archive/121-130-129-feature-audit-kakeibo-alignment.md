# Feature 129: Kakeibo Alignment Audit
> **Status:** Done — audit completed; all identified features implemented via 131–144
> **Created:** 2026-04-07
> **Last Updated:** 2026-04-09

## Summary

This document audits every existing feature in BudgetExperiment against the Kakeibo + Kaizen calendar-first philosophy established in Feature 128 (`docs/128-kakeibo-kaizen-calendar-first.md`). Each feature is scored for philosophical alignment and assessed for required modifications to support the mindful budgeting approach. The audit informs implementation priorities, identifies feature flag candidates, and ensures all future work deepens the Kakeibo + Kaizen rhythm rather than diluting it.

## Alignment Legend

🟢 **Aligned** — supports Kakeibo philosophy naturally; requires no or minimal changes  
🟡 **Needs work** — can be aligned with targeted modifications  
🔴 **Tension** — design conflicts with Kakeibo; needs fundamental rethinking  
⚪ **Neutral** — utility feature, philosophy-agnostic

---

## Feature Inventory

### Calendar (Homepage + Month/Week/Day Views)
- **Current state:** The calendar is already the homepage. Displays daily transaction totals, balances, recurring items due. Month grid with week summaries. Day detail modal shows transaction list for a day.
- **Alignment:** 🟢 **Aligned**
- **Kakeibo relevance:** The calendar IS the household ledger. This is the philosophical centerpiece. All Kakeibo enhancements flow through this surface.
- **Required changes:**
  - Add Kakeibo spending heatmap overlay (toggle-able)
  - Month header: savings progress bar (from `MonthlyReflection`)
  - Month-start intention prompt (non-blocking)
  - Week summary: Kakeibo category breakdown (mini-bars for Essentials/Wants/Culture/Unexpected)
  - Day cells: Kakeibo badge showing predominant category
  - Month header: access to end-of-month reflection panel
- **Kakeibo touchpoints:** `MonthlyReflection`, `KakeiboCategory` per transaction, `KaizenGoal` per week
- **Priority:** **Immediate** — this is the core philosophy surface

---

### Transaction Management (Add/Edit/Delete)
- **Current state:** Modals for add/edit/delete transaction and transfer. Fields: date, amount, account, category, note, location. Accessible from calendar day cells, transactions page, accounts page.
- **Alignment:** 🟡 **Needs work**
- **Kakeibo relevance:** Every transaction entry is a mindful recording act. Should prompt Kakeibo categorization at entry time.
- **Required changes:**
  - Add `KakeiboSelector` component (four icons: Essentials/Wants/Culture/Unexpected)
  - Default Kakeibo category derived from selected `BudgetCategory.KakeiboCategory`
  - Allow per-transaction override of Kakeibo category
  - Small educational tooltip on first use: "What kind of spending is this?"
- **Kakeibo touchpoints:** `Transaction.KakeiboOverride`, `BudgetCategory.KakeiboCategory`
- **Priority:** **Immediate** — transaction entry is a primary user interaction

---

### Budget Categories & Goals
- **Current state:** `/categories` page lists all categories (Expense/Income/Transfer). User creates/edits categories. `/budget` page shows monthly goal progress per category. Goals are simple numeric targets.
- **Alignment:** 🟡 **Needs work**
- **Kakeibo relevance:** Categories are the bridge between familiar vocabulary (Groceries, Dining) and Kakeibo philosophy (Essentials, Wants). Each expense category must map to exactly one Kakeibo bucket.
- **Required changes:**
  - Add `KakeiboCategory` field to `BudgetCategory` entity (already in Feature 128 spec)
  - Category edit UI: dropdown for Kakeibo routing (only visible for Expense categories)
  - Migration applies smart defaults (Groceries → Essentials, Dining → Wants, Education → Culture)
  - One-time **Kakeibo Setup Wizard** on first login post-migration: review all Expense categories and confirm/correct their Kakeibo routing
  - Budget goals page: group categories by Kakeibo bucket or show Kakeibo badge per category
- **Kakeibo touchpoints:** `BudgetCategory.KakeiboCategory`
- **Priority:** **Immediate** — must be in place before transaction entry changes go live

---

### Recurring Transactions & Transfers
- **Current state:** `/recurring` and `/recurring-transfers` pages. User creates recurring items with frequency, amount, category. Calendar shows recurring items due each day. Auto-realization service creates instances.
- **Alignment:** 🟢 **Aligned**
- **Kakeibo relevance:** Recurring items are predictable spending — fits naturally into monthly reflection ("Did I honor my recurring commitments?"). No philosophical tension.
- **Required changes:**
  - Recurring transaction/transfer creation: inherit Kakeibo category from selected `BudgetCategory` (no new UI needed)
  - Realized instances use the category's current Kakeibo mapping (retroactive recalculation on category mapping change)
- **Kakeibo touchpoints:** `BudgetCategory.KakeiboCategory` (inherited by instances)
- **Priority:** **Soon** — low risk, passive benefit

---

### Reports & Analytics (Dashboard + Sub-Reports)
- **Current state:** `/reports/dashboard` landing page. Sub-reports: Monthly Trends (StackedAreaChart), Monthly Categories (Treemap + Donut), Budget Comparison (Waterfall + Radial + Radar), Location Report (map + chart). ComponentShowcase for all chart types.
- **Alignment:** 🟡 **Needs work**
- **Kakeibo relevance:** Reports should reinforce reflection, not replace it. The calendar should be the primary analytical surface. Reports are supplementary deep-dives.
- **Required changes:**
  - Add **Kaizen Dashboard** report: 12-week rolling view of Kakeibo category spending with micro-goal outcomes overlaid (✓/✗ per week)
  - Monthly Categories Report: add Kakeibo grouping toggle — show spending by Kakeibo bucket instead of (or alongside) individual categories
  - Budget Comparison Report: optionally show Kakeibo category variance (Essentials vs Wants trend)
  - Location Report: no Kakeibo relevance (neutral)
  - ReportsDashboard: add Kaizen Dashboard tile
  - Monthly Trends: consider showing Kakeibo trend lines (Essentials flat, Wants declining = good Kaizen)
- **Kakeibo touchpoints:** `KakeiboCategory` aggregations, `KaizenGoal` outcomes
- **Priority:** **Soon** — reports augment the calendar, not replace it

---

### Paycheck Allocation Calculator
- **Current state:** `/paycheck-planner` page. User inputs paycheck frequency and amount. Tool calculates how much of each paycheck goes to recurring bills. Shows which bills are covered by which paycheck periods.
- **Alignment:** ⚪ **Neutral**
- **Kakeibo relevance:** Paycheck planning is a pragmatic utility. Not inherently aligned or opposed to Kakeibo. Could be reframed as "income allocation planning" (one of the four Kakeibo questions: "How much income did I receive?").
- **Required changes:**
  - Optional: Show Kakeibo category breakdown of planned allocations (what % of paycheck goes to Essentials vs Wants)
  - Optional: At month-start, suggest a paycheck-based savings target for `MonthlyReflection.SavingsGoal`
- **Kakeibo touchpoints:** `BudgetCategory.KakeiboCategory` (for allocation breakdown), `MonthlyReflection.SavingsGoal` (suggestion)
- **Priority:** **Low** — nice-to-have enhancement, not critical

---

### AI Chat Assistant
- **Current state:** Floating chat panel (desktop) or mobile sheet. Users ask natural language questions. AI generates transactions, transfers, recurring items, or clarification prompts. Powered by Ollama (local LLM).
- **Alignment:** 🟡 **Needs work**
- **Kakeibo relevance:** AI can accelerate transaction entry but must not bypass mindful categorization. The AI should prompt for Kakeibo category when creating transactions, not auto-assign blindly.
- **Required changes:**
  - When AI creates a transaction, it should ask: "Is this Essentials, Wants, Culture, or Unexpected?" (add `ClarificationNeededAction` for Kakeibo category if not inferrable from description)
  - AI should suggest Kakeibo category based on merchant/description but always allow user override
  - Chat panel could answer Kakeibo-aware queries: "How much did I spend on Wants this week?" → query effective Kakeibo categories
- **Kakeibo touchpoints:** `Transaction.KakeiboOverride`, Kakeibo aggregation queries
- **Priority:** **Soon** — AI must support, not undermine, mindful budgeting

---

### AI Rule Suggestions (Ollama-Powered Categorization)
- **Current state:** `/ai/suggestions` page. AI analyzes uncategorized transactions and suggests categorization rules or category assignments. User reviews and accepts/rejects. Batch apply.
- **Alignment:** 🟡 **Needs work**
- **Kakeibo relevance:** Automated categorization is pragmatic but must remain transparent and user-controlled. Rules should consider Kakeibo implications (e.g., flag when a suggested category has a surprising Kakeibo routing).
- **Required changes:**
  - Show Kakeibo bucket alongside suggested category (e.g., "Dining → Wants")
  - If a suggested category's Kakeibo routing seems off for the merchant, flag for user review
  - AI could suggest Kakeibo overrides for specific transactions (e.g., "This Dining transaction looks like a business meal — Culture instead?")
- **Kakeibo touchpoints:** `BudgetCategory.KakeiboCategory`, `Transaction.KakeiboOverride`
- **Priority:** **Soon** — suggestions must respect philosophy

---

### CSV Import Flow
- **Current state:** `/import` page. Multi-step wizard: upload CSV, detect/map columns, configure date format and indicators, preview, deduplicate, commit. Saved mapping templates. Import history.
- **Alignment:** ⚪ **Neutral**
- **Kakeibo relevance:** CSV import is a data ingestion utility. No direct philosophical impact. Imported transactions inherit category → Kakeibo routing like any other transaction.
- **Required changes:** None required. Imported transactions automatically get Kakeibo categories from their assigned `BudgetCategory`.
- **Kakeibo touchpoints:** None directly; imported transactions resolve Kakeibo category at display time
- **Priority:** **None**

---

### Recurring Charge Detection Suggestions
- **Current state:** `/recurring-charge-suggestions` page. AI analyzes transaction history and suggests patterns that could be set up as recurring transactions. User accepts/rejects.
- **Alignment:** 🟢 **Aligned**
- **Kakeibo relevance:** Detecting recurring patterns supports mindful budgeting by surfacing predictable expenses. Aligns with Kakeibo's emphasis on awareness of spending habits.
- **Required changes:** Show Kakeibo category of suggested recurring item (inherited from matched transaction's category).
- **Kakeibo touchpoints:** `BudgetCategory.KakeiboCategory` (display only)
- **Priority:** **Low** — already aligned, minor display enhancement

---

### Categorization Rules Management
- **Current state:** `/rules` page. User creates/edits/deletes categorization rules (if description matches X, assign category Y). Rules can be applied manually or auto-applied on import. Review mode for bulk editing.
- **Alignment:** ⚪ **Neutral**
- **Kakeibo relevance:** Rules are a pragmatic automation. No philosophical tension as long as rules are transparent and user-controlled (they are).
- **Required changes:** Display Kakeibo bucket when showing/editing a rule's target category. No functional changes needed.
- **Kakeibo touchpoints:** `BudgetCategory.KakeiboCategory` (display only)
- **Priority:** **Low** — informational enhancement only

---

### Reconciliation (Transaction-Based)
- **Current state:** `/reconciliation` page. User marks transactions as cleared/uncleared to track which transactions have posted to bank accounts. Shows cleared balance vs account balance. Configuration: auto-mark past transactions.
- **Alignment:** ⚪ **Neutral**
- **Kakeibo relevance:** Reconciliation is administrative data hygiene. No philosophical impact.
- **Required changes:** None.
- **Kakeibo touchpoints:** None
- **Priority:** **None**

---

### Statement Reconciliation (Bank Statement Upload)
- **Current state:** `/statement-reconciliation` pages. User uploads bank statement PDF or CSV, matches transactions, identifies discrepancies, resolves.
- **Alignment:** ⚪ **Neutral**
- **Kakeibo relevance:** Administrative data validation. No philosophical impact.
- **Required changes:** None.
- **Kakeibo touchpoints:** None
- **Priority:** **None**

---

### Data Health
- **Current state:** `/datahealth` page. Detects duplicates, outliers, date gaps, uncategorized transactions. Provides cleanup actions.
- **Alignment:** ⚪ **Neutral**
- **Kakeibo relevance:** Data hygiene utility. No philosophical impact.
- **Required changes:** None. (Uncategorized transactions already flagged; Kakeibo assignment happens via category.)
- **Kakeibo touchpoints:** None directly
- **Priority:** **None**

---

### Export (CSV/Excel)
- **Current state:** Export controller/service. User can export transactions, accounts, budgets as CSV or Excel. Accessed via toolbar buttons on various pages.
- **Alignment:** ⚪ **Neutral**
- **Kakeibo relevance:** Data export utility. No philosophical impact. Could optionally include Kakeibo category in exported transaction data.
- **Required changes:** Optionally add `EffectiveKakeiboCategory` column to transaction exports.
- **Kakeibo touchpoints:** `Transaction.KakeiboOverride`, `BudgetCategory.KakeiboCategory` (resolved value)
- **Priority:** **Low** — nice-to-have enhancement

---

### Accounts Management
- **Current state:** `/accounts` page. Create/edit/delete accounts. View balance history. Transfer between accounts.
- **Alignment:** 🟢 **Aligned**
- **Kakeibo relevance:** Accounts are the containers for money. No philosophical tension. Account balance awareness supports the Kakeibo question "How much did I want to save?" (net balance trend).
- **Required changes:** None.
- **Kakeibo touchpoints:** None directly (accounts contain transactions which have Kakeibo categories)
- **Priority:** **None**

---

### Transactions List Page
- **Current state:** `/transactions` page. Paginated, filterable, sortable list of all transactions. Bulk actions: categorize, delete. Filter by date, account, category, amount range, uncategorized status.
- **Alignment:** 🟡 **Needs work**
- **Kakeibo relevance:** Transaction list is a utility for bulk management. Calendar should be the primary view. List page should support filtering by Kakeibo category to reinforce the categorization.
- **Required changes:**
  - Add Kakeibo category filter dropdown (Essentials/Wants/Culture/Unexpected/All)
  - Display Kakeibo badge on each transaction row
  - "View on Calendar" link should jump to calendar day (already exists per spec)
- **Kakeibo touchpoints:** `Transaction.KakeiboOverride`, `BudgetCategory.KakeiboCategory`
- **Priority:** **Soon** — list view should support Kakeibo filtering

---

### Transfers List Page
- **Current state:** `/transfers` page. Similar to transactions list but for inter-account transfers. Filters, sorting, pagination.
- **Alignment:** ⚪ **Neutral**
- **Kakeibo relevance:** Transfers are not spending — they move money between accounts. Excluded from all Kakeibo aggregation (per Feature 128 spec).
- **Required changes:** None. Transfers remain Kakeibo-neutral.
- **Kakeibo touchpoints:** None (transfers are explicitly excluded from Kakeibo calculations)
- **Priority:** **None**

---

### Uncategorized Transactions Page
- **Current state:** `/uncategorized` page. Shows transactions without a category. Quick categorization UI.
- **Alignment:** 🟡 **Needs work**
- **Kakeibo relevance:** Uncategorized transactions cannot be routed to Kakeibo buckets. This page supports data hygiene and mindful categorization — strongly aligned.
- **Required changes:**
  - When user assigns a category, show the resulting Kakeibo bucket ("Dining → Wants")
  - Optional: Allow direct Kakeibo override during categorization (advanced feature)
- **Kakeibo touchpoints:** `BudgetCategory.KakeiboCategory`
- **Priority:** **Soon** — supports mindful categorization

---

### Component Showcase
- **Current state:** `/showcase` page. Displays all chart components (BarChart, DonutChart, LineChart, SparkLine, Heatmap, Candlestick, Waterfall, etc.) with sample data. Developer/design reference.
- **Alignment:** ⚪ **Neutral**
- **Kakeibo relevance:** Internal developer tool. No user-facing philosophical impact.
- **Required changes:** None. (Could add a Kakeibo category chart example for consistency.)
- **Kakeibo touchpoints:** None
- **Priority:** **None**

---

### Onboarding Flow
- **Current state:** `/onboarding` page. 4-step wizard: welcome, currency selection, week start day, initial account setup. Runs once on first login. Skippable.
- **Alignment:** 🟡 **Needs work**
- **Kakeibo relevance:** Onboarding is the first impression. Should introduce the Kakeibo philosophy and guide initial category → Kakeibo routing setup.
- **Required changes:**
  - Add a 5th step: **Kakeibo Setup** — show user's existing Expense categories (if imported or seeded) and ask them to confirm/correct Kakeibo routing
  - Brief explanation of the four Kakeibo categories (Essentials/Wants/Culture/Unexpected) with examples
  - Optional: Introduce monthly reflection concept ("At the start of each month, we'll ask what you want to save.")
- **Kakeibo touchpoints:** `BudgetCategory.KakeiboCategory`
- **Priority:** **Immediate** — onboarding must introduce the philosophy

---

### Settings / User Preferences
- **Current state:** `/settings` page. General tab: currency, week start day, decimal places. AI tab: Ollama endpoint, model selection, AI feature toggles.
- **Alignment:** 🟡 **Needs work**
- **Kakeibo relevance:** Settings should include Kakeibo preferences: heatmap toggle default, reflection reminder preferences, Kaizen goal opt-in.
- **Required changes:**
  - Add **Kakeibo/Kaizen Preferences** section:
    - "Show spending heatmap by default" (bool)
    - "Remind me to set monthly savings intention" (bool)
    - "Enable weekly Kaizen micro-goals" (bool, default true but user can opt out)
  - Persist these in `UserSettings` entity
- **Kakeibo touchpoints:** `UserSettings` (new fields)
- **Priority:** **Soon** — user control over Kakeibo/Kaizen features

---

### Authentication (Authentik OIDC)
- **Current state:** `/authentication` page. Login/logout via Authentik OIDC. User identity management.
- **Alignment:** ⚪ **Neutral**
- **Kakeibo relevance:** Infrastructure. No philosophical impact.
- **Required changes:** None.
- **Kakeibo touchpoints:** None
- **Priority:** **None**

---

### Observability (Logs, Traces, Metrics)
- **Current state:** Serilog structured logging. OpenTelemetry traces and metrics. Seq / OTLP export. DebugLogController for in-memory log buffer.
- **Alignment:** ⚪ **Neutral**
- **Kakeibo relevance:** Infrastructure. No philosophical impact.
- **Required changes:** None. (Could add Kakeibo-specific metrics for analysis: "Kakeibo category distribution per user", "Monthly reflection completion rate".)
- **Kakeibo touchpoints:** None (metrics only)
- **Priority:** **None** (metrics are post-launch analysis)

---

### Geocoding / Location Parser
- **Current state:** Location parsing service extracts city/state from transaction descriptions. Geocoding service (stub) for lat/lng. Location report shows spending by location.
- **Alignment:** ⚪ **Neutral**
- **Kakeibo relevance:** Location is metadata. No philosophical impact. Fits into "Where did I spend?" reflection but not core to Kakeibo.
- **Required changes:** None. Location Report could optionally filter by Kakeibo category ("Where did I spend on Wants vs Essentials?").
- **Kakeibo touchpoints:** `BudgetCategory.KakeiboCategory` (for location report filtering)
- **Priority:** **Low** — optional enhancement

---

### Merchant Mappings / Learned Patterns
- **Current state:** Merchant mapping service learns category assignments from user's past categorization decisions. `MerchantKnowledgeBase` has 15 built-in category families. Dismissed suggestions prevent repeat AI prompts.
- **Alignment:** 🟢 **Aligned**
- **Kakeibo relevance:** Learning from user behavior supports mindful categorization. The user's past decisions guide future automation — aligns with Kaizen (continuous improvement through learning).
- **Required changes:** None functionally. Merchant mappings automatically inherit Kakeibo categories from the learned category.
- **Kakeibo touchpoints:** `BudgetCategory.KakeiboCategory` (inherited)
- **Priority:** **None**

---

### Custom Reports Builder
- **Current state:** `/reports/custom-report-builder` page (experimental). User defines custom report layouts by dragging widgets. Save/load report configurations.
- **Alignment:** 🔴 **Tension**
- **Kakeibo relevance:** Custom report builder encourages endless data exploration — the opposite of Kakeibo's "simple, consistent reflection" philosophy. This feature could dilute the calendar-first approach by creating alternative analytical surfaces that bypass reflection.
- **Required changes:**
  - **Feature flag candidate** — disable by default, allow opt-in for power users
  - If kept: enforce Kakeibo-first data model — all custom reports must aggregate by Kakeibo category as a baseline dimension
  - Add educational note: "The calendar is your primary reflection surface. Custom reports are for deep-dive analysis only."
- **Kakeibo touchpoints:** All report data should support Kakeibo grouping
- **Priority:** **Low** — consider feature-flagging this entirely

---

### Licenses Page
- **Current state:** `/licenses` page. Displays third-party open source licenses for transparency (ApexCharts, etc.).
- **Alignment:** ⚪ **Neutral**
- **Kakeibo relevance:** Legal compliance. No philosophical impact.
- **Required changes:** None.
- **Kakeibo touchpoints:** None
- **Priority:** **None**

---

### Account Transactions Drilldown
- **Current state:** `/accounts/{id}/transactions` page. Shows all transactions for a single account. Similar to transactions list but account-scoped.
- **Alignment:** ⚪ **Neutral**
- **Kakeibo relevance:** Account-specific view is utility. No philosophical tension. Could show Kakeibo breakdown for that account's spending.
- **Required changes:** Optionally show Kakeibo category breakdown at top of page (summary card: "This account spent X on Essentials, Y on Wants, etc.").
- **Kakeibo touchpoints:** `BudgetCategory.KakeiboCategory`
- **Priority:** **Low** — nice-to-have summary

---

## Feature Flag Candidates

Based on the audit, the following features are candidates for feature flags:

### 1. **Custom Reports Builder** 🔴
**Reason:** Philosophical tension. Custom report builder encourages data exploration over calendar-first reflection. Should be opt-in for power users who want deep-dive analysis but not the default experience.  
**Flag Name:** `Features:Reports:CustomReportBuilder`  
**Default:** `false` (off)  
**Type:** [experimental]

### 2. **AI Chat Assistant** 🟡
**Reason:** High-value feature but requires Ollama setup (not all users will configure). Also experimental — AI quality varies by model. Should be opt-in.  
**Flag Name:** `Features:AI:ChatAssistant`  
**Default:** `true` (on if Ollama configured), but user can disable in settings  
**Type:** [user-simplification] + [experimental]

### 3. **AI Rule Suggestions (Ollama)** 🟡
**Reason:** Same as Chat Assistant — requires Ollama. Some users may prefer manual categorization for mindfulness. Should be opt-in via settings.  
**Flag Name:** `Features:AI:RuleSuggestions`  
**Default:** `true` (on if Ollama configured), user can disable  
**Type:** [user-simplification]

### 4. **Recurring Charge Detection** 🟢
**Reason:** Useful but not essential. Some users may not want AI-detected patterns. Allow disable.  
**Flag Name:** `Features:AI:RecurringChargeDetection`  
**Default:** `true`  
**Type:** [user-simplification]

### 5. **Spending Heatmap Overlay** 🟢
**Reason:** Core Kakeibo feature but visual preference. Some users may find heatmaps distracting. Toggle in settings.  
**Flag Name:** `Features:Calendar:SpendingHeatmap`  
**Default:** `true`  
**Type:** [user-simplification]

### 6. **Kaizen Micro-Goals** 🟢
**Reason:** Core Kakeibo feature but opt-in by philosophy. Weekly goals may not resonate with all users. Should be user-controllable.  
**Flag Name:** `Features:Kaizen:MicroGoals`  
**Default:** `true`  
**Type:** [user-simplification]

### 7. **Monthly Reflection Prompts** 🟢
**Reason:** Core Kakeibo feature but some users may prefer not to be prompted. Allow dismiss permanently.  
**Flag Name:** `Features:Kakeibo:MonthlyReflectionPrompts`  
**Default:** `true`  
**Type:** [user-simplification]

### 8. **Location Report** ⚪
**Reason:** Niche feature. Not all users care about geographic spending patterns. Allow disable.  
**Flag Name:** `Features:Reports:LocationReport`  
**Default:** `true`  
**Type:** [user-simplification]

### 9. **Paycheck Planner** ⚪
**Reason:** Useful for salaried users but not universal. Some users don't have regular paychecks. Allow hide.  
**Flag Name:** `Features:Paycheck:PaycheckPlanner`  
**Default:** `true`  
**Type:** [user-simplification]

### 10. **Statement Reconciliation (PDF Upload)** ⚪
**Reason:** Advanced feature. Many users use CSV import or manual entry only. Allow hide.  
**Flag Name:** `Features:Reconciliation:StatementReconciliation`  
**Default:** `true`  
**Type:** [user-simplification]

### 11. **Data Health Dashboard** ⚪
**Reason:** Data hygiene tool. Not needed by all users. Allow hide.  
**Flag Name:** `Features:DataHealth:Dashboard`  
**Default:** `true`  
**Type:** [user-simplification]

### 12. **Advanced Charts (Non-Essential)** ⚪
**Reason:** CandlestickChart, BoxPlotChart, SparkLine, LineChart, GroupedBarChart — not used in any active report. Developer showcase only. Could be feature-flagged to reduce bundle size.  
**Flag Name:** `Features:Charts:AdvancedCharts`  
**Default:** `false` (showcase only)  
**Type:** [experimental]

### 13. **Geocoding Service** ⚪
**Reason:** Stub implementation. Not functional yet. Feature-flag until real geocoding provider integrated.  
**Flag Name:** `Features:Location:Geocoding`  
**Default:** `false`  
**Type:** [experimental]

### 14. **Kakeibo Overlay on Calendar (Phase Rollout)** 🟢
**Reason:** During Feature 128 implementation, Kakeibo overlay could be feature-flagged to allow progressive rollout without disrupting existing users.  
**Flag Name:** `Features:Kakeibo:CalendarOverlay`  
**Default:** `false` during development, `true` when shipped  
**Type:** [experimental] → becomes standard

### 15. **Kaizen Dashboard Report** 🟢
**Reason:** New report type. Feature-flag during initial rollout to gather feedback.  
**Flag Name:** `Features:Kaizen:Dashboard`  
**Default:** `false` during development, `true` when shipped  
**Type:** [experimental] → becomes standard

---

## Recommended Implementation Order

Based on alignment priorities and dependencies:

### Phase 1: Foundation (Immediate)
1. **Budget Categories Kakeibo Routing** — add `KakeiboCategory` to `BudgetCategory` entity, migration with defaults
2. **Onboarding Kakeibo Setup** — introduce philosophy and guide initial category routing
3. **Transaction Entry Kakeibo Selector** — add Kakeibo category picker to transaction add/edit modal

### Phase 2: Calendar Kakeibo UI (Immediate)
4. **Calendar Spending Heatmap** — intensity-based tinting on day cells
5. **Month Intention Prompt** — savings goal and intention text at month start
6. **Week Summary Kakeibo Breakdown** — mini-bars for four Kakeibo categories in week summary
7. **Day Cell Kakeibo Badge** — visual indicator of predominant Kakeibo category

### Phase 3: Reflection & Kaizen (Soon)
8. **Monthly Reflection Panel** — end-of-month four-questions panel
9. **Kaizen Micro-Goals** — weekly goal setting and outcome tracking
10. **Kaizen Dashboard Report** — 12-week trend visualization

### Phase 4: Feature Enhancements (Soon)
11. **Transaction List Kakeibo Filter** — filter by Kakeibo category on `/transactions` page
12. **AI Chat Kakeibo Awareness** — AI prompts for Kakeibo category when creating transactions
13. **AI Rule Suggestions Kakeibo Display** — show Kakeibo bucket alongside suggested categories
14. **Settings Kakeibo Preferences** — user control over heatmap, prompts, goals

### Phase 5: Reports & Optional Features (Low Priority)
15. **Reports Kakeibo Grouping** — add Kakeibo grouping to Monthly Categories Report, Budget Comparison
16. **Paycheck Planner Kakeibo Breakdown** — show allocation by Kakeibo category
17. **Location Report Kakeibo Filter** — filter location spending by Kakeibo category
18. **Export Kakeibo Column** — add `EffectiveKakeiboCategory` to transaction exports

### Phase 6: Feature Flags & Cleanup (Low Priority)
19. **Implement Feature Flag System** (see architecture section below)
20. **Feature-Flag Custom Reports Builder** — disable by default, opt-in for power users
21. **Feature-Flag Advanced Charts** — reduce bundle size, showcase-only
22. **User Settings: Feature Toggles** — UI for enabling/disabling flagged features

---

## Feature Flag Architecture

### Purpose

Feature flags serve two primary roles in BudgetExperiment:

1. **User Simplification** — Allow users to hide features they don't use, creating a cleaner, more focused UI. Examples: Location Report, Paycheck Planner, Statement Reconciliation. Users opt out of features that don't fit their workflow.

2. **Phased Rollout of Experimental Features** — Allow new features to be deployed behind flags and enabled progressively. Examples: Kakeibo Calendar Overlay (during Feature 128 implementation), Custom Reports Builder (experimental power-user feature), Advanced Charts (showcase only). Developers control feature visibility per deployment environment.

### Configuration Shape

Feature flags are stored in a **hierarchical `FeatureFlags` section** in `appsettings.json` and overrideable via environment variables in Docker/production.

#### `appsettings.json` Structure

```json
{
  "FeatureFlags": {
    "Calendar": {
      "SpendingHeatmap": true,
      "KakeiboOverlay": true
    },
    "Kakeibo": {
      "MonthlyReflectionPrompts": true,
      "CalendarOverlay": true
    },
    "Kaizen": {
      "MicroGoals": true,
      "Dashboard": true
    },
    "AI": {
      "ChatAssistant": true,
      "RuleSuggestions": true,
      "RecurringChargeDetection": true
    },
    "Reports": {
      "CustomReportBuilder": false,
      "LocationReport": true,
      "CandlestickChart": false
    },
    "Charts": {
      "AdvancedCharts": false
    },
    "Paycheck": {
      "PaycheckPlanner": true
    },
    "Reconciliation": {
      "StatementReconciliation": true
    },
    "DataHealth": {
      "Dashboard": true
    },
    "Location": {
      "Geocoding": false
    }
  }
}
```

#### Environment Variable Overrides (Docker/Production)

Environment variables use double-underscore `__` notation:

```bash
FeatureFlags__Kakeibo__CalendarOverlay=false
FeatureFlags__Reports__CustomReportBuilder=true
FeatureFlags__AI__ChatAssistant=false
```

These override the corresponding `appsettings.json` values without editing config files.

#### User-Level vs Instance-Level

**Decision:** Feature flags are **instance-level** (per-deployment), not per-user.

**Rationale:**
- Simpler architecture — no per-user flag storage in database
- Consistent experience for all users on a deployment instance
- User preferences (e.g., "Hide Paycheck Planner in my nav menu") are handled via `UserSettings` entity, not feature flags
- Feature flags control what's deployed/enabled, not what's visible to individual users

**User-level visibility:** Handled separately via `UserSettings` table. Example:
```csharp
public class UserSettings
{
    // Existing fields...
    public bool ShowPaycheckPlanner { get; set; } = true;
    public bool ShowLocationReport { get; set; } = true;
    public bool ShowSpendingHeatmap { get; set; } = true;
    // ...
}
```

The client UI reads `UserSettings` to show/hide nav menu items and page sections. Feature flags control whether the feature is available at all; user settings control whether the user wants to see it.

### API Surface

#### Server-Side: `FeatureFlagService`

A scoped service in the **Application layer** reads flags from `IConfiguration` and exposes them via a strongly-typed interface.

**Location:** `src/BudgetExperiment.Application/Common/FeatureFlagService.cs`

**Interface:**
```csharp
public interface IFeatureFlagService
{
    bool IsEnabled(string flagPath); // e.g., "Calendar.SpendingHeatmap"
    FeatureFlagsDto GetAllFlags();   // Returns entire flag tree as DTO
}
```

**Implementation:** Reads from `IConfiguration["FeatureFlags:*"]`. Caches flag values per request scope for performance.

**Dependency Injection:** Registered as `Scoped` in `Application/DependencyInjection.cs`.

#### Client-Side: API Endpoint

The Blazor WASM client needs to know which features are enabled. Two options:

1. **Embed flags in `index.html` via server-side rendering** — simpler but requires API restart to change flags
2. **Dedicated API endpoint returning flags** — more flexible, allows dynamic flag changes

**Recommended:** API endpoint.

**Endpoint:**
```
GET /api/v1/config/feature-flags
```

**Response:**
```json
{
  "calendar": {
    "spendingHeatmap": true,
    "kakeiboOverlay": true
  },
  "kakeibo": {
    "monthlyReflectionPrompts": true,
    "calendarOverlay": true
  },
  "kaizen": {
    "microGoals": true,
    "dashboard": true
  },
  "ai": {
    "chatAssistant": true,
    "ruleSuggestions": true,
    "recurringChargeDetection": true
  },
  "reports": {
    "customReportBuilder": false,
    "locationReport": true,
    "candlestickChart": false
  },
  "charts": {
    "advancedCharts": false
  },
  "paycheck": {
    "paycheckPlanner": true
  },
  "reconciliation": {
    "statementReconciliation": true
  },
  "dataHealth": {
    "dashboard": true
  },
  "location": {
    "geocoding": false
  }
}
```

**Controller:** `ConfigController` (already exists for version endpoint). Add `GetFeatureFlags()` action.

**DTO:** `FeatureFlagsDto` with nested structure matching the JSON above. Use `record` types for immutability.

**Client Service:** `FeatureFlagClientService` (scoped) fetches flags once on app load, caches in memory. Injected into components that need flag checks.

### Flag Naming Convention

Hierarchical, self-documenting names:

**Pattern:** `{FeatureArea}:{SubArea?}:{FeatureName}`

**Examples:**
- `Calendar:SpendingHeatmap`
- `Kakeibo:MonthlyReflectionPrompts`
- `Kaizen:MicroGoals`
- `AI:ChatAssistant`
- `Reports:CustomReportBuilder`
- `Charts:AdvancedCharts`

**Rules:**
- PascalCase for all segments
- Use colons `:` as delimiter (maps to double-underscore `__` in env vars)
- Maximum 3 levels deep (`Area:SubArea:Feature`)
- Feature names are nouns or noun phrases (not verbs)

### Clean Architecture Placement

**Domain:** Feature flags are **NOT** domain concerns. Domain entities are business logic and must remain flag-agnostic. Example: `MonthlyReflection` exists regardless of whether the UI prompts for it.

**Application:** Feature flag *service* lives here (`IFeatureFlagService`). Application services may check flags to conditionally enable/disable use cases (e.g., `ReflectionService` always exists, but the API controller may check a flag before exposing the endpoint).

**API:** Controllers and endpoints check flags via `IFeatureFlagService` to conditionally expose routes or return 404 for disabled features. `ConfigController` exposes flags to client.

**Client:** `FeatureFlagClientService` fetches flags from API and caches them. Razor components inject the service and conditionally render UI based on flags.

**Contracts:** `FeatureFlagsDto` lives in Contracts (shared DTO between API and Client).

**Shared:** No flag-specific code in Shared (enums/constants only).

### Blazor Client Pattern

#### Service: `FeatureFlagClientService`

**Location:** `src/BudgetExperiment.Client/Services/FeatureFlagClientService.cs`

**Interface:**
```csharp
public interface IFeatureFlagClientService
{
    Task InitializeAsync(); // Fetch flags from API on app load
    bool IsEnabled(string flagPath); // e.g., "Calendar.SpendingHeatmap"
    FeatureFlagsDto GetAllFlags();
}
```

**Implementation:**
- Injected with `HttpClient` and `IApiService`
- `InitializeAsync()` called in `Program.cs` before rendering root component
- Flags cached in-memory for the session
- Graceful fallback: if API call fails, assume all flags `false` (safe default) OR use client-side defaults embedded in code

**Component Usage:**
```razor
@inject IFeatureFlagClientService FeatureFlags

@if (FeatureFlags.IsEnabled("Calendar.SpendingHeatmap"))
{
    <SpendingHeatmapOverlay ... />
}
```

**Navigation Menu:**
```razor
@if (FeatureFlags.IsEnabled("Reports.LocationReport"))
{
    <NavLink href="/reports/location">Location Report</NavLink>
}
```

### Default-On vs Default-Off Strategy

**Default-On (true):**
- Core Kakeibo/Kaizen features: `MonthlyReflectionPrompts`, `SpendingHeatmap`, `MicroGoals`, `KakeiboOverlay`, `KaizenDashboard`
- Established features: `AI.ChatAssistant`, `AI.RuleSuggestions`, `Paycheck.PaycheckPlanner`, `Reports.LocationReport`, `DataHealth.Dashboard`, `Reconciliation.StatementReconciliation`
- **Rationale:** These are fully-baked features. Users can opt out via `UserSettings` if they don't want them.

**Default-Off (false):**
- Experimental features: `Reports.CustomReportBuilder`, `Charts.AdvancedCharts`, `Location.Geocoding`, `Reports.CandlestickChart`
- In-development features: `Kakeibo.CalendarOverlay` (during Feature 128 implementation — flipped to `true` when shipped)
- **Rationale:** These are incomplete, experimental, or philosophically questionable. Require explicit opt-in.

**Migration Strategy for New Features:**
1. Develop feature behind flag (default `false`)
2. Test in development/staging with flag `true`
3. Deploy to production with flag `false`
4. Gather feedback from beta testers (enable flag via env var for their instance)
5. Flip flag to `true` in `appsettings.json` when ready for general availability
6. Eventually: remove flag and conditional checks once feature is stable (6-12 months post-launch)

### First-Pass Flag List

| Flag Path | Type | Default | Description |
|-----------|------|---------|-------------|
| `Calendar:SpendingHeatmap` | [user-simplification] | `true` | Intensity-based tinting on calendar day cells |
| `Calendar:KakeiboOverlay` | [experimental] | `false` → `true` | Kakeibo category badges and week breakdowns (Feature 128) |
| `Kakeibo:MonthlyReflectionPrompts` | [user-simplification] | `true` | Month-start intention and end-of-month reflection prompts |
| `Kakeibo:CalendarOverlay` | [experimental] | `false` → `true` | (Alias for `Calendar:KakeiboOverlay` during rollout) |
| `Kaizen:MicroGoals` | [user-simplification] | `true` | Weekly improvement goal setting and tracking |
| `Kaizen:Dashboard` | [experimental] | `false` → `true` | 12-week Kaizen trend report (Feature 128) |
| `AI:ChatAssistant` | [user-simplification] + [experimental] | `true` | AI chat panel for natural language transaction entry |
| `AI:RuleSuggestions` | [user-simplification] | `true` | AI-powered categorization rule suggestions |
| `AI:RecurringChargeDetection` | [user-simplification] | `true` | AI-detected recurring transaction patterns |
| `Reports:CustomReportBuilder` | [experimental] | `false` | Drag-and-drop custom report layout builder |
| `Reports:LocationReport` | [user-simplification] | `true` | Geographic spending report with map visualization |
| `Reports:CandlestickChart` | [experimental] | `false` | Candlestick chart (daily balance OHLC) — no current data source |
| `Charts:AdvancedCharts` | [experimental] | `false` | Advanced chart types (SparkLine, LineChart, GroupedBarChart) — showcase only |
| `Paycheck:PaycheckPlanner` | [user-simplification] | `true` | Paycheck allocation calculator for recurring bills |
| `Reconciliation:StatementReconciliation` | [user-simplification] | `true` | Bank statement PDF/CSV upload and matching |
| `DataHealth:Dashboard` | [user-simplification] | `true` | Data quality dashboard (duplicates, outliers, gaps) |
| `Location:Geocoding` | [experimental] | `false` | Geocoding service (stub — not functional yet) |

**Total:** 17 flags

**Implementation Priority:**
- Phase 1 (Immediate): `Calendar:KakeiboOverlay`, `Kakeibo:MonthlyReflectionPrompts`, `Kaizen:MicroGoals` — core Feature 128 flags
- Phase 2 (Soon): `AI:ChatAssistant`, `AI:RuleSuggestions`, `Reports:CustomReportBuilder` — user simplification and experimental features
- Phase 3 (Low Priority): All others — incremental cleanup and bundle size optimization

---

## Summary

This audit identifies **5 immediate-priority features**, **12 soon-priority features**, and **8 low-priority/optional enhancements** to align BudgetExperiment with the Kakeibo + Kaizen calendar-first philosophy. The feature flag architecture provides a clean, scalable way to manage user simplification preferences and phased rollout of experimental features without polluting the domain layer or creating configuration sprawl. Implementation order prioritizes the calendar surface (the philosophical centerpiece) first, followed by transaction entry (the mindful recording act), then reflection/Kaizen tracking, and finally optional enhancements.
