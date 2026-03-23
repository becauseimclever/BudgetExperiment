# Feature 119: Recurring Charge Detection & Suggestions
> **Status:** Done

## Overview

Automatically detect recurring charges from transaction history and suggest creating `RecurringTransaction` entries. Today, users must manually create recurring transactions. This feature analyzes imported transaction data to identify patterns — such as "Netflix" appearing monthly at $15.99 — and surfaces actionable suggestions the user can accept, dismiss, or tune.

## Problem Statement

### Current State

- Users manually create recurring transactions with a description, amount, account, recurrence pattern, and optional category.
- `ImportPatternValue` on `RecurringTransaction` lets the system auto-link future imports, but only **after** a recurring transaction already exists.
- There is no automated way to discover recurring charges buried in months of transaction history. Users must notice patterns themselves.

### Target State

- The system scans transaction history, groups by normalized merchant/description, and detects regular intervals and consistent amounts.
- Detected patterns are presented as **Recurring Charge Suggestions** with confidence scores.
- Users can accept a suggestion (creating a `RecurringTransaction` with pre-filled fields and import patterns), dismiss it, or adjust parameters before accepting.
- Already-linked transactions (those with a `RecurringTransactionId`) are excluded from detection to avoid duplicates.
- Detection can run on-demand or automatically after a CSV import.

---

## User Stories

### Detection

#### US-119-001: Detect Recurring Charges
**As a** budget user  
**I want** the system to analyze my transaction history and identify charges that recur on a regular schedule  
**So that** I don't have to manually spot and create recurring transactions

**Acceptance Criteria:**
- [x] Transactions are grouped by normalized description (stripping noise like POS/PURCHASE prefixes, trailing reference numbers)
- [x] Groups with ≥ 3 occurrences within the analysis window are evaluated for periodicity
- [x] Supported frequencies detected: Weekly, BiWeekly, Monthly, Quarterly, Yearly
- [x] Amount variance tolerance is configurable (default ±5 %) to handle slight fluctuations
- [x] Each detected pattern receives a confidence score (0.0–1.0) based on interval regularity, amount consistency, and sample size
- [x] Transactions already linked to a `RecurringTransaction` are excluded

#### US-119-002: View Recurring Charge Suggestions
**As a** budget user  
**I want** to see a list of detected recurring charge suggestions sorted by confidence  
**So that** I can quickly review and act on them

**Acceptance Criteria:**
- [x] Suggestions display: merchant/description, detected frequency, average amount, number of matching transactions, confidence score, and last occurrence date
- [x] Suggestions are sorted by confidence descending by default
- [x] User can filter by status (Pending, Accepted, Dismissed)

#### US-119-003: Accept a Recurring Charge Suggestion
**As a** budget user  
**I want** to accept a suggestion and have a `RecurringTransaction` created automatically  
**So that** future imports are tracked and budgeted correctly

**Acceptance Criteria:**
- [x] Accepting creates a `RecurringTransaction` with description, amount, detected recurrence pattern, account, and category (if transactions were categorized)
- [x] An `ImportPatternValue` is auto-generated from the normalized description so future imports link automatically
- [x] Existing matching transactions are retroactively linked to the new `RecurringTransaction` via `RecurringTransactionId`
- [x] Suggestion status changes to Accepted
- [ ] User can edit fields (description, amount, frequency, category) before confirming

#### US-119-004: Dismiss a Recurring Charge Suggestion
**As a** budget user  
**I want** to dismiss a suggestion I don't care about  
**So that** it doesn't clutter my pending list

**Acceptance Criteria:**
- [x] Dismissed suggestions are hidden from the default view
- [x] Dismissed suggestions can be viewed in a separate "Dismissed" list
- [x] Dismissing a suggestion does not delete it; it can be restored

### Post-Import Trigger

#### US-119-005: Auto-Detect After Import
**As a** budget user  
**I want** the system to automatically run recurring charge detection after I import transactions  
**So that** new patterns are surfaced without manual effort

**Acceptance Criteria:**
- [x] After a successful CSV import, detection runs for the affected account(s)
- [x] Only new/changed suggestions are surfaced (no duplicate suggestions for already-pending patterns)
- [x] User is notified of new suggestions (UI indicator or toast)

---

## Technical Design

### Architecture Changes

New components slot into the existing layered architecture:

| Layer | New Component | Responsibility |
|-------|---------------|----------------|
| Domain | `RecurringChargeSuggestion` entity | Persists detected patterns and user decisions |
| Domain | `RecurrenceDetector` (pure logic) | Groups transactions, detects intervals, scores confidence |
| Application | `IRecurringChargeDetectionService` | Orchestrates detection, stores suggestions |
| Application | `RecurringChargeSuggestionAcceptanceHandler` | Creates `RecurringTransaction` + links transactions on accept |
| Contracts | `RecurringChargeSuggestionResponse` | API DTO |
| Infrastructure | `RecurringChargeSuggestionRepository` | EF Core persistence |
| API | `RecurringChargeSuggestionsController` | REST endpoints |
| Client | `RecurringChargeSuggestions` component | UI for reviewing and acting on suggestions |

### Domain Model

```csharp
// New entity: src/BudgetExperiment.Domain/Recurring/RecurringChargeSuggestion.cs
public class RecurringChargeSuggestion
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public string NormalizedDescription { get; private set; }
    public string SampleDescription { get; private set; }          // Original description for display
    public MoneyValue AverageAmount { get; private set; }
    public RecurrenceFrequency DetectedFrequency { get; private set; }
    public int DetectedInterval { get; private set; }              // e.g. 1 for monthly, 2 for every-other-month
    public decimal Confidence { get; private set; }                // 0.0 – 1.0
    public int MatchingTransactionCount { get; private set; }
    public DateOnly FirstOccurrence { get; private set; }
    public DateOnly LastOccurrence { get; private set; }
    public Guid? CategoryId { get; private set; }                  // Most-used category from matched transactions
    public SuggestionStatus Status { get; private set; }           // Pending, Accepted, Dismissed
    public Guid? AcceptedRecurringTransactionId { get; private set; } // Set on accept
    public BudgetScope Scope { get; private set; }
    public Guid? OwnerUserId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
}
```

```csharp
// Pure domain logic: src/BudgetExperiment.Domain/Recurring/RecurrenceDetector.cs
public static class RecurrenceDetector
{
    public static IReadOnlyList<DetectedPattern> Detect(
        IReadOnlyList<Transaction> transactions,
        RecurrenceDetectionOptions options);
}

public record DetectedPattern(
    string NormalizedDescription,
    string SampleDescription,
    MoneyValue AverageAmount,
    RecurrenceFrequency Frequency,
    int Interval,
    decimal Confidence,
    IReadOnlyList<Transaction> MatchingTransactions,
    DateOnly FirstOccurrence,
    DateOnly LastOccurrence,
    Guid? MostUsedCategoryId);

public record RecurrenceDetectionOptions(
    int MinimumOccurrences = 3,
    decimal AmountVarianceTolerance = 0.05m,    // ±5%
    int AnalysisWindowMonths = 12);
```

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST   | `/api/v1/recurring-charge-suggestions/detect` | Trigger detection for an account or all accounts |
| GET    | `/api/v1/recurring-charge-suggestions` | List suggestions (filterable by status, account) |
| GET    | `/api/v1/recurring-charge-suggestions/{id}` | Get suggestion detail with matched transactions |
| POST   | `/api/v1/recurring-charge-suggestions/{id}/accept` | Accept suggestion → create RecurringTransaction |
| POST   | `/api/v1/recurring-charge-suggestions/{id}/dismiss` | Dismiss suggestion |

### Database Changes

- New table `RecurringChargeSuggestions` with columns matching the entity above.
- Index on `(AccountId, Status)` for filtered queries.
- Index on `(NormalizedDescription, AccountId)` for duplicate detection.

### Description Normalization

Reuse the merchant-normalization logic from the existing AI prompt system and `MerchantKnowledgeBase`. The normalizer should:

1. Strip common bank prefixes: `POS`, `PURCHASE`, `DEBIT`, `ACH`, `CHECKCARD`, etc.
2. Strip trailing reference/confirmation numbers (numeric sequences > 4 digits at end).
3. Strip trailing dates in common formats.
4. Trim and collapse whitespace.
5. Case-fold to uppercase for grouping, preserve original for display.

### Confidence Scoring

Confidence is a weighted composite:

| Factor | Weight | Description |
|--------|--------|-------------|
| Interval regularity | 40% | Standard deviation of days between occurrences vs. expected interval |
| Amount consistency | 30% | Coefficient of variation of amounts (lower = higher score) |
| Sample size | 20% | More occurrences = higher confidence (capped at 12) |
| Recency | 10% | Bonus if last occurrence is within one expected interval of today |

Minimum confidence threshold for surfacing: **0.5** (configurable).

### UI Components

- **RecurringChargeSuggestions page** — table/card list of pending suggestions with accept/dismiss actions.
- **Suggestion detail modal** — shows matched transactions, lets user edit fields before accepting.
- **Post-import banner** — "We found N new recurring charge patterns. [Review]" after CSV import.

---

## Implementation Plan

### Phase 1: Domain – Description Normalizer & Recurrence Detector

**Objective:** Build the pure domain logic for normalizing transaction descriptions and detecting recurrence patterns. Fully TDD.

**Tasks:**
- [ ] Create `DescriptionNormalizer` static class with bank-prefix stripping, reference-number removal, whitespace normalization
- [ ] Write unit tests for normalizer edge cases (various bank formats, international characters)
- [ ] Create `RecurrenceDetector` static class with grouping, interval detection, confidence scoring
- [ ] Write unit tests for detection: monthly charges, weekly charges, varying amounts within tolerance, noise rejection
- [ ] Create `RecurringChargeSuggestion` entity with factory method and status transitions
- [ ] Write unit tests for entity invariants

**Commit:**
```bash
git commit -m "feat(domain): add recurrence detection and description normalizer

- DescriptionNormalizer strips bank prefixes and trailing references
- RecurrenceDetector groups transactions and detects periodic patterns
- RecurringChargeSuggestion entity with confidence scoring
- Comprehensive unit tests for all detection scenarios

Refs: #119"
```

---

### Phase 2: Infrastructure – Persistence & Repository

**Objective:** Add EF Core configuration and repository for `RecurringChargeSuggestion`.

**Tasks:**
- [ ] Add `RecurringChargeSuggestion` to `BudgetDbContext`
- [ ] Create EF Core entity configuration (table, indexes, value conversions)
- [ ] Add migration
- [ ] Create `IRecurringChargeSuggestionRepository` interface in Domain
- [ ] Implement repository in Infrastructure
- [ ] Write integration tests with test database

**Commit:**
```bash
git commit -m "feat(infra): add RecurringChargeSuggestion persistence

- EF Core configuration with indexes on AccountId+Status
- Migration for RecurringChargeSuggestions table
- Repository implementation with filtered queries

Refs: #119"
```

---

### Phase 3: Application – Detection Service & Acceptance Handler

**Objective:** Orchestration layer that ties detection to persistence and handles accept/dismiss workflows.

**Tasks:**
- [ ] Create `IRecurringChargeDetectionService` with `DetectAsync(Guid? accountId)` and suggestion CRUD
- [ ] Implement service: load transactions, run detector, upsert suggestions (avoid duplicates)
- [ ] Create `RecurringChargeSuggestionAcceptanceHandler`: on accept, create `RecurringTransaction`, generate `ImportPatternValue`, link existing transactions
- [ ] Write unit tests with faked repositories
- [ ] Add post-import hook: call detection after `ImportService` completes

**Commit:**
```bash
git commit -m "feat(app): add recurring charge detection service

- DetectAsync analyzes transactions and persists suggestions
- AcceptanceHandler creates RecurringTransaction from suggestion
- Post-import trigger for automatic detection
- Unit tests with faked repositories

Refs: #119"
```

---

### Phase 4: Contracts & API Endpoints

**Objective:** Expose recurring charge suggestions via REST API.

**Tasks:**
- [ ] Add `RecurringChargeSuggestionResponse`, `DetectRecurringChargesRequest`, `AcceptRecurringChargeSuggestionRequest` DTOs to Contracts
- [ ] Create `RecurringChargeSuggestionsController` with endpoints per design table
- [ ] Add mapping between domain and contracts
- [ ] Write API integration tests (happy path + validation + 404)
- [ ] Verify OpenAPI spec generation

**Commit:**
```bash
git commit -m "feat(api): add recurring charge suggestion endpoints

- POST detect, GET list/detail, POST accept/dismiss
- Request validation and Problem Details error responses
- Integration tests with WebApplicationFactory

Refs: #119"
```

---

### Phase 5: Client UI

**Objective:** Blazor WebAssembly UI for reviewing and acting on recurring charge suggestions.

**Tasks:**
- [ ] Create `RecurringChargeSuggestions.razor` page with sortable/filterable table
- [ ] Create `RecurringChargeSuggestionDetail.razor` modal with editable fields and matched transaction list
- [ ] Add post-import notification banner to import page
- [ ] Add navigation link in sidebar
- [ ] Write bUnit tests for component logic

**Commit:**
```bash
git commit -m "feat(client): add recurring charge suggestions UI

- Suggestions page with confidence-sorted table
- Detail modal with editable accept flow
- Post-import notification banner
- Navigation integration

Refs: #119"
```

---

### Phase 6: Documentation & Cleanup

**Objective:** Final polish, documentation updates, and cleanup.

**Tasks:**
- [ ] Update API documentation / OpenAPI specs
- [ ] Add XML comments for public APIs
- [ ] Remove any TODO comments
- [ ] Final code review

**Commit:**
```bash
git commit -m "docs(recurring): add documentation for feature 119

- XML comments for public API surface
- OpenAPI spec updates

Refs: #119"
```

---

## Design Decisions & Notes

1. **Pure domain detection** — `RecurrenceDetector` is a static, pure function with no dependencies. This keeps it fast, testable, and free of infrastructure concerns. The application service handles I/O.

2. **Separate entity from `CategorySuggestion`** — Although both are "suggestion" concepts, recurring charge suggestions have different lifecycle (they create `RecurringTransaction` + link transactions, not categories/rules). A shared `SuggestionStatus` enum is reused.

3. **No AI required** — Detection is algorithmic (interval math + statistical scoring), not AI-driven. This keeps it fast, deterministic, and works without an AI provider configured. AI could enhance normalization in a future iteration.

4. **Duplicate avoidance** — When detection re-runs, existing pending suggestions for the same `(NormalizedDescription, AccountId)` are updated rather than duplicated.

5. **Amount tolerance** — Fixed-amount subscriptions get high confidence. Variable charges (e.g., utility bills) still match if within the configured tolerance, but with lower confidence.

## Conventional Commit Reference

| Type | When to Use | SemVer Impact |
|------|-------------|---------------|
| `feat` | New feature or capability | Minor |
| `fix` | Bug fix | Patch |
| `test` | Adding or fixing tests | None |
| `docs` | Documentation only | None |
| `refactor` | Code restructure, no feature/fix | None |
