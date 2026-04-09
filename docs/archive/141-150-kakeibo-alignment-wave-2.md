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

## Feature 145: Kakeibo Date-Range Report Service

> **Status:** Done

**What it did:** Introduced `IKakeiboReportService` and `KakeiboReportService` in the Application layer to aggregate expense transactions into daily, weekly (ISO), and monthly bucket totals (Essentials, Wants, Culture, Unexpected) for arbitrary date ranges. Exposed via `GET /api/v1/reports/kakeibo?from={date}&to={date}` gated behind the `Kakeibo:DateRangeReports` feature flag. Closes accuracy gap CAT-8.

**Key decisions:**
- Uses `GetEffectiveKakeiboCategory()` on `Transaction` (existing domain method), respecting `KakeiboOverride` over category default
- Income and Transfer category types excluded from all bucket totals
- Zero-amount buckets are always returned (no silent omission)
- Feature flag checked via `IFeatureFlagService.IsEnabledAsync()` (project's existing pattern, not `[FeatureGate]` attribute)
- DTOs use `decimal` for amounts (not `MoneyValue`) for cleaner JSON serialization
- 24 tests written: 14 unit, 6 API integration, 4 Testcontainers accuracy tests proving INV-8

---

## Feature 146: Transfer Deletion with Orphan Detection

> **Status:** Done

**What it did:** Implemented atomic transfer deletion via `DELETE /api/v1/transfers/{transferId}`. Added `DeleteTransferAsync(Guid, CancellationToken)` to `ITransactionRepository` using `IDbContextTransaction` for all-or-nothing semantics. Introduced `ITransferService` / `TransferService` in the Application layer. Orphaned single-leg transfers are cleaned up gracefully with a logged warning. Feature-flagged via `feature-transfer-atomic-deletion` (seeded `false`). Closes accuracy gap INV-2.

**Key decisions:**
- Extended `ITransactionRepository` (not a new `ITransferRepository`) — both legs are transactions
- Returns `204 NoContent` on success; `404 NotFound` if no legs exist; `403 Forbidden` if feature flag disabled
- Feature flag via `IFeatureFlagService.IsEnabledAsync()` (project pattern, not `[FeatureGate]`)
- Also fixed pre-existing `KakeiboReportControllerTests` failures: `DateOnly? from/to` nullable params + `EnsureFeatureFlag` cache invalidation
- 13 tests: 6 unit (TransferServiceTests), 4 API integration (TransferDeletionControllerTests), 3 Testcontainers accuracy tests (TransferDeletionAccuracyTests)

---

## Features 147–150

> **Status:** Not yet planned — reserved for future work.
