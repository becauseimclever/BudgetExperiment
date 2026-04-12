# Squad Decisions — Archive

Decisions older than 30 days.

---

### 1. Feature 122: Test Coverage Gaps Verified Complete (2026-01-09)

**Author:** Alfred

**Status:** Feature 122 was marked as "Pending" but upon audit, all required work was already completed in prior sprints.

**Findings:**
- **Phase 1 (Repository Tests):** All four repository test files (`AppSettingsRepositoryTests`, `CustomReportLayoutRepositoryTests`, `RecurringChargeSuggestionRepositoryTests`, `UserSettingsRepositoryTests`) exist with comprehensive coverage using PostgreSQL Testcontainers fixture (219 tests pass).
- **Phase 2 (Controller Tests):** Both `RecurringChargeSuggestionsControllerTests` and `RecurringControllerTests` exist with full endpoint coverage including happy path, 404, and 400/422 validation scenarios.
- **Phase 3 (Vanity Enum Tests):** Already removed in Decision #4 (2026-03-22) — 12 vanity enum test files deleted.

**Test Suite Health:** 5,413 passed, 0 failed, 1 skipped (pre-existing).

**Result:** Feature documentation updated to Status: Done and archived to `docs/archive/121-130-test-coverage-gaps.md`. No code changes required.

---

### Kakeibo + Kaizen Philosophy Embedded in Public Documentation (2026-01-09)

**Author:** Alfred

**Status:** Active

All new features and enhancements must support the **Kakeibo + Kaizen calendar-first philosophy**. This is not a feature — it is the application's identity.

**Kakeibo** (家計簿, "household ledger") — mindful, intentional recording. The app asks the four Kakeibo questions at the right moments: *How much did I receive? How much do I want to save? How much did I spend? How can I improve?*

**Kaizen** (改善, "continuous improvement") — small, consistent changes compound over time. Weekly micro-goals, not grand resolutions. Compare yourself to yourself. Progress is quiet and honest.

**The calendar is the centerpiece** — every financial decision happens on the calendar. Every day is a journal entry. Every week offers Kakeibo breakdowns. Every month closes with reflection.

#### What Changed

**README.md:**
- Opening tagline reframed: leads with Kakeibo + Kaizen philosophy, not feature list
- Purpose section reframed: WHY before HOW, calendar described as centerpiece
- Key Domain Concepts: added `KakeiboCategory`, `MonthlyReflection`, `KaizenGoal` entities; updated `BudgetCategory` and `Transaction` to mention Kakeibo routing
- Development Guidelines: added "Philosophy First" bullet pointing to `docs/128-kakeibo-kaizen-calendar-first.md`

**CONTRIBUTING.md:**
- Added entire "Design Philosophy — Kakeibo + Kaizen" section explaining how philosophy affects contributions: calendar as centerpiece, reflection over data display, no gamification, small/consistent over large/occasional, categorization carries intention

#### Why This Matters

Every contributor — internal or external — must understand that this application is **not** a general-purpose transaction tracker that happens to have a calendar view. It is a **mindful budgeting application** where the calendar is the ledger and Kakeibo + Kaizen philosophy guides every design decision.

Without this framing in README and CONTRIBUTING, contributors default to "add more features" mode. With this framing, they ask: "Does this feature deepen the calendar experience? Does it invite reflection? Does it fit the daily/weekly/monthly rhythm?"

This is architectural guidance at the product level.

#### Constraints

- No gamification (streaks, badges, confetti)
- Calendar remains the primary interaction surface
- Features outside the calendar rhythm require justification
- New expense categories must specify Kakeibo routing

#### References

- Full spec: `docs/128-kakeibo-kaizen-calendar-first.md`
- README.md lines 11-26 (Purpose section), lines 177-186 (Development Guidelines), lines 190-206 (Key Domain Concepts)
- CONTRIBUTING.md new section after intro (Design Philosophy)

---
