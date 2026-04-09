# Archive: Features 141–150

> Kakeibo alignment wave 2. Implemented features listed below; remaining numbers reserved for future work.

---

## Feature 141: Settings — Kakeibo/Kaizen Preferences

> **Status:** Done

**What it did:** Added a "Kakeibo & Kaizen Preferences" section to `/settings`. Extended `UserSettings` entity with four boolean fields (`ShowSpendingHeatmap`, `ShowMonthlyReflectionPrompts`, `EnableKaizenMicroGoals`, `ShowKakeiboCalendarBadges`). User-level toggles that control UI visibility independently of the backend feature flag system.

**Key decisions:**
- Settings page itself is not feature-flagged; server-side flags control whether features exist at all, user settings control per-user visibility within those bounds

---

## Feature 142: Uncategorized Transactions — Kakeibo Display

> **Status:** Done

**What it did:** The `/uncategorized` page now shows a Kakeibo bucket badge beside the selected category when a user categorizes a transaction (e.g., "Dining → **Wants**"), providing immediate visual confirmation of Kakeibo intent. Gated behind the shared `Kakeibo:TransactionFilter` feature flag.

---

## Feature 143: Reports — Kakeibo Grouping

> **Status:** Done

**What it did:** Added optional Kakeibo grouping toggles to existing reports (Monthly Categories Report, Budget Comparison Report). When enabled, spending is aggregated by Kakeibo bucket (Essentials/Wants/Culture/Unexpected) instead of individual categories.

**Key decisions:**
- No new feature flag required; toggles are UI controls on the report pages themselves, using existing report feature flags

---

## Feature 144: Custom Reports Builder — Feature Flag

> **Status:** Done

**What it did:** Feature-flagged the Custom Reports Builder (`/reports/custom-builder`) off by default (`Reports:CustomReportBuilder = false`). When disabled, the route returns a redirect and the nav item is hidden. When enabled, the page shows an educational note reminding users the calendar is the primary reflection surface.

**Key decisions:**
- Balances power-user demand for custom reporting with the Kakeibo calendar-first philosophy (Feature 128)

---

## Features 145–150

> **Status:** Not yet planned — reserved for future work.
