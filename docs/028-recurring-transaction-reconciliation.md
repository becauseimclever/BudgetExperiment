# Feature 028: Recurring Transaction Reconciliation

## Overview

Implement an intelligent reconciliation system that matches imported transactions with expected recurring transaction instances. When users import bank statements (CSV), the system automatically identifies transactions that correspond to scheduled recurring transactions and links them, eliminating duplicate entries and providing a consolidated view of expected vs. actual spending. This closes the loop between planned recurring transactions and real-world bank data.

## Problem Statement

Users define recurring transactions to track expected expenses (rent, subscriptions, utilities) and income (paychecks). When they import actual transactions from their bank, there's no mechanism to correlate these imports with the scheduled recurring instances.

### Current State

- Recurring transactions generate projected instances for forecasting
- CSV import creates new transaction records
- No automatic matching between imports and recurring expectations
- Users may end up with duplicate entries (projected + imported)
- No visibility into whether a recurring transaction actually occurred
- Manual reconciliation required to update recurring transaction status

### Target State

- Imported transactions are automatically matched to pending recurring instances
- Matched imports are linked to their recurring transaction source
- Users can review and confirm/reject suggested matches
- Dashboard shows reconciliation status (matched, unmatched, missing)
- Unmatched recurring instances surface as "missing" for follow-up
- Actual amounts update the recurring transaction's "last known amount" for better forecasting
- Flexible matching tolerances for amounts and dates (bills vary month to month)

---

## User Stories

### Automatic Matching

#### US-028-001: Auto-Match Imports to Recurring Instances
**As a** user  
**I want to** have imported transactions automatically matched to my recurring transactions  
**So that** I don't have to manually reconcile bank data with my budget plan

**Acceptance Criteria:**
- [ ] During import, system evaluates each transaction against pending recurring instances
- [ ] Matching considers: description similarity, amount tolerance, date proximity
- [ ] High-confidence matches are auto-linked
- [ ] Lower-confidence matches are flagged for user review
- [ ] Unmatched imports proceed normally (no recurring link)

#### US-028-002: Configure Matching Tolerances
**As a** user  
**I want to** configure how strictly imports are matched to recurring transactions  
**So that** I can account for variable bills (utilities, credit cards)

**Acceptance Criteria:**
- [ ] Amount tolerance: percentage or absolute value (e.g., ±10% or ±$20)
- [ ] Date tolerance: days before/after scheduled date (e.g., ±5 days)
- [ ] Description match sensitivity (strict, moderate, loose)
- [ ] Tolerances configurable globally or per-recurring transaction
- [ ] Default tolerances provided (sensible out-of-box behavior)

#### US-028-003: Review Suggested Matches
**As a** user  
**I want to** review and approve/reject suggested matches before they're finalized  
**So that** I maintain control over my transaction data

**Acceptance Criteria:**
- [ ] Import preview shows suggested recurring matches
- [ ] Each match shows confidence score and matching criteria
- [ ] User can accept, reject, or modify suggested match
- [ ] Rejected matches import as standalone transactions
- [ ] Bulk accept/reject for multiple matches

### Reconciliation Dashboard

#### US-028-004: View Recurring Reconciliation Status
**As a** user  
**I want to** see which recurring transactions have been reconciled for a period  
**So that** I can identify missing or unexpected transactions

**Acceptance Criteria:**
- [ ] Dashboard shows all recurring instances for selected period (month/week)
- [ ] Status indicators: Matched, Pending, Missing, Skipped
- [ ] Filter by status, category, account
- [ ] Shows expected vs actual amount for matched instances
- [ ] Click through to view linked transaction details

#### US-028-005: Identify Missing Recurring Transactions
**As a** user  
**I want to** be alerted when expected recurring transactions didn't occur  
**So that** I can investigate (missed payment, subscription cancelled, etc.)

**Acceptance Criteria:**
- [ ] "Missing" status when date window passed without match
- [ ] Optional notification for missing recurring transactions
- [ ] Quick actions: Mark as skipped, manually match, or ignore
- [ ] History of missing/skipped instances preserved

#### US-028-006: Manually Match Import to Recurring
**As a** user  
**I want to** manually link an imported transaction to a recurring transaction  
**So that** I can reconcile edge cases the auto-matcher missed

**Acceptance Criteria:**
- [ ] Select any unlinked transaction
- [ ] Show list of unmatched recurring instances (within date range)
- [ ] Confirm link with optional notes
- [ ] Transaction updates with recurring transaction reference

### Variance Tracking

#### US-028-007: Track Amount Variances
**As a** user  
**I want to** see when actual amounts differ from expected recurring amounts  
**So that** I can identify billing changes or errors

**Acceptance Criteria:**
- [ ] Display variance (expected - actual) for matched transactions
- [ ] Highlight significant variances (beyond tolerance threshold)
- [ ] Aggregate variance report per recurring transaction over time
- [ ] Option to update recurring amount based on actual

#### US-028-008: Update Recurring Amount from Actual
**As a** user  
**I want to** update a recurring transaction's amount based on actual imports  
**So that** future projections are more accurate

**Acceptance Criteria:**
- [ ] "Update amount" action on matched transaction
- [ ] Can set new recurring amount to match actual
- [ ] Optionally average last N occurrences for variable bills
- [ ] Change recorded in recurring transaction history

---

## Technical Design

### Architecture Changes

The reconciliation feature introduces a new application service that orchestrates matching logic during import. The matching engine is a domain service that evaluates candidates and returns scored matches.

**New Components:**
- `ReconciliationService` (Application) - Orchestrates matching workflow
- `TransactionMatcher` (Domain) - Core matching logic with scoring
- `ReconciliationResult` (Domain) - Match result with confidence score
- `MatchingTolerances` (Domain) - Configuration value object

### Domain Model

#### ReconciliationMatch

```csharp
/// <summary>
/// Represents a potential match between an imported transaction and a recurring instance.
/// </summary>
public sealed class ReconciliationMatch
{
    public Guid ImportedTransactionId { get; private set; }
    public Guid RecurringTransactionId { get; private set; }
    public DateOnly RecurringInstanceDate { get; private set; }
    public decimal ConfidenceScore { get; private set; }
    public MatchConfidenceLevel ConfidenceLevel { get; private set; }
    public ReconciliationMatchStatus Status { get; private set; }
    public decimal? AmountVariance { get; private set; }
    public int? DateOffsetDays { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
}

public enum MatchConfidenceLevel
{
    High,      // Auto-accept threshold
    Medium,    // Suggest for review
    Low        // Show but don't suggest
}

public enum ReconciliationMatchStatus
{
    Suggested,   // Awaiting user decision
    Accepted,    // User confirmed match
    Rejected,    // User rejected match
    AutoMatched  // System auto-matched (high confidence)
}
```

#### MatchingTolerances

```csharp
/// <summary>
/// Configuration for how strictly to match transactions to recurring instances.
/// </summary>
public sealed class MatchingTolerances
{
    /// <summary>
    /// Maximum days before/after scheduled date to consider a match.
    /// </summary>
    public int DateToleranceDays { get; private set; }

    /// <summary>
    /// Maximum percentage variance in amount to consider a match (0.0 to 1.0).
    /// </summary>
    public decimal AmountTolerancePercent { get; private set; }

    /// <summary>
    /// Maximum absolute amount variance to consider a match.
    /// </summary>
    public decimal AmountToleranceAbsolute { get; private set; }

    /// <summary>
    /// Minimum description similarity score to consider a match (0.0 to 1.0).
    /// </summary>
    public decimal DescriptionSimilarityThreshold { get; private set; }

    /// <summary>
    /// Minimum confidence score for automatic matching without review.
    /// </summary>
    public decimal AutoMatchThreshold { get; private set; }

    public static MatchingTolerances Default => new()
    {
        DateToleranceDays = 7,
        AmountTolerancePercent = 0.10m,
        AmountToleranceAbsolute = 10.00m,
        DescriptionSimilarityThreshold = 0.6m,
        AutoMatchThreshold = 0.85m
    };
}
```

#### Extended Transaction Properties

Add to existing `Transaction` entity (already has `RecurringTransactionId` and `RecurringInstanceDate`):

```csharp
// Existing properties already support linking:
// public Guid? RecurringTransactionId { get; private set; }
// public DateOnly? RecurringInstanceDate { get; private set; }

// New method to link during reconciliation
public void LinkToRecurringInstance(Guid recurringTransactionId, DateOnly instanceDate)
{
    if (this.RecurringTransactionId.HasValue)
    {
        throw new DomainException("Transaction is already linked to a recurring transaction.");
    }
    
    this.RecurringTransactionId = recurringTransactionId;
    this.RecurringInstanceDate = instanceDate;
    this.UpdatedAt = DateTime.UtcNow;
}
```

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/reconciliation/status?month={month}&year={year}` | Get reconciliation status for period |
| GET | `/api/v1/reconciliation/pending` | Get pending suggested matches |
| POST | `/api/v1/reconciliation/match` | Manually match transaction to recurring |
| POST | `/api/v1/reconciliation/accept/{matchId}` | Accept suggested match |
| POST | `/api/v1/reconciliation/reject/{matchId}` | Reject suggested match |
| POST | `/api/v1/reconciliation/bulk-accept` | Accept multiple matches |
| PUT | `/api/v1/reconciliation/tolerances` | Update matching tolerances |
| GET | `/api/v1/reconciliation/tolerances` | Get current tolerances |
| GET | `/api/v1/reconciliation/variances?recurringId={id}` | Get variance history |
| POST | `/api/v1/recurring-transactions/{id}/update-amount` | Update amount from actual |

### Database Changes

**New Table: `ReconciliationMatches`**

| Column | Type | Description |
|--------|------|-------------|
| Id | uuid | Primary key |
| ImportedTransactionId | uuid | FK to Transactions |
| RecurringTransactionId | uuid | FK to RecurringTransactions |
| RecurringInstanceDate | date | The scheduled instance date |
| ConfidenceScore | decimal(5,4) | Match confidence (0.0000-1.0000) |
| ConfidenceLevel | int | High/Medium/Low enum |
| Status | int | Suggested/Accepted/Rejected/AutoMatched |
| AmountVariance | decimal(18,2) | Expected - Actual amount |
| DateOffsetDays | int | Actual date - Scheduled date |
| CreatedAtUtc | timestamp | When match was suggested |
| ResolvedAtUtc | timestamp | When match was accepted/rejected |
| OwnerUserId | uuid | For personal scope filtering |
| Scope | int | BudgetScope enum |

**New Table: `MatchingToleranceSettings`**

| Column | Type | Description |
|--------|------|-------------|
| Id | uuid | Primary key |
| RecurringTransactionId | uuid | Nullable (null = global default) |
| DateToleranceDays | int | Days tolerance |
| AmountTolerancePercent | decimal(5,4) | Percentage tolerance |
| AmountToleranceAbsolute | decimal(18,2) | Absolute amount tolerance |
| DescriptionSimilarityThreshold | decimal(5,4) | Description match threshold |
| AutoMatchThreshold | decimal(5,4) | Auto-accept threshold |
| OwnerUserId | uuid | User owner |
| Scope | int | BudgetScope enum |

### UI Components

**New Pages:**
- `/reconciliation` - Reconciliation dashboard with status overview
- `/reconciliation/pending` - Review pending matches

**Modified Components:**
- Import preview - Show suggested recurring matches inline
- Transaction detail - Show linked recurring transaction info
- Recurring transaction detail - Show reconciliation history

---

## Implementation Plan

### Phase 1: Domain Model & Matching Logic ✅

**Objective:** Build the core matching engine and domain models

**Tasks:**
- [x] Create `ReconciliationMatch` entity
- [x] Create `MatchingTolerances` value object
- [x] Create `MatchConfidenceLevel` and `ReconciliationMatchStatus` enums
- [x] Create `ITransactionMatcher` interface
- [x] Implement `TransactionMatcher` with scoring algorithm
- [x] Add `LinkToRecurringInstance` method to Transaction
- [x] Write unit tests for matching logic
- [x] Write unit tests for scoring edge cases

**Commit:**
```bash
git add .
git commit -m "feat(domain): add reconciliation matching domain model

- Add ReconciliationMatch entity with confidence scoring
- Add MatchingTolerances value object with defaults
- Implement TransactionMatcher scoring algorithm
- Add LinkToRecurringInstance to Transaction entity
- Unit tests for matching logic

Refs: #028"
```

---

### Phase 2: Infrastructure - Repository & Persistence ✅

**Objective:** Add database support for reconciliation data

**Tasks:**
- [x] Create `IReconciliationMatchRepository` interface
- [x] Create `IMatchingToleranceRepository` interface
- [x] Add EF Core configuration for ReconciliationMatch
- [x] Add EF Core configuration for MatchingToleranceSettings
- [x] Create migration for new tables
- [x] Implement repository classes
- [x] Write integration tests for repositories

**Commit:**
```bash
git add .
git commit -m "feat(infra): add reconciliation persistence layer

- Add ReconciliationMatch EF configuration
- Add MatchingToleranceSettings EF configuration
- Create migration for reconciliation tables
- Implement repositories with scope filtering

Refs: #028"
```

---

### Phase 3: Application Service - Reconciliation Workflow ✅

**Objective:** Orchestration service for matching during import

**Tasks:**
- [x] Create `IReconciliationService` interface
- [x] Implement `ReconciliationService`
- [x] Integrate matching into CSV import flow
- [x] Add batch matching for existing transactions
- [x] Create DTOs for reconciliation data
- [x] Add mapping for reconciliation entities
- [x] Write unit tests for service logic

**Commit:**
```bash
git add .
git commit -m "feat(app): implement reconciliation service

- Add ReconciliationService for matching orchestration
- Integrate with import flow for auto-matching
- Add batch matching for historical transactions
- Create reconciliation DTOs

Refs: #028"
```

---

### Phase 4: API Endpoints ✅

**Objective:** Expose reconciliation functionality via REST API

**Tasks:**
- [x] Create `ReconciliationController`
- [x] Implement status endpoint
- [x] Implement match accept/reject endpoints
- [x] Implement manual match endpoint
- [x] Implement tolerance settings endpoints
- [x] Add variance history endpoint
- [x] Update OpenAPI documentation
- [x] Write API integration tests

**Commit:**
```bash
git add .
git commit -m "feat(api): add reconciliation API endpoints

- Add ReconciliationController with full CRUD
- Implement match review workflow endpoints
- Add tolerance configuration endpoints
- OpenAPI documentation

Refs: #028"
```

---

### Phase 5: Import Integration ✅

**Objective:** Seamlessly integrate matching into CSV import

**Tasks:**
- [x] Modify import preview to show suggested matches
- [x] Add match indicators to import row display
- [x] Allow accept/reject during import preview
- [x] Auto-apply high-confidence matches on import confirm
- [x] Update import service to persist matches
- [x] Write integration tests for import + reconciliation

**Commit:**
```bash
git add .
git commit -m "feat(app): integrate reconciliation with csv import

- Add match suggestions to import preview
- Support match review during import flow
- Auto-apply high-confidence matches
- Persist match decisions with import

Refs: #028"
```

---

### Phase 6: Client - Reconciliation Dashboard ✅

**Objective:** Build the reconciliation dashboard UI

**Tasks:**
- [x] Create ReconciliationPage component
- [x] Create ReconciliationStatusCard component
- [x] Create RecurringInstanceRow component with status
- [x] Add filtering by status, account, category
- [x] Create PendingMatchesList component
- [x] Add variance display and highlighting
- [x] Add period selector (month/week)
- [ ] Write bUnit tests for components

**Commit:**
```bash
git add .
git commit -m "feat(client): add reconciliation dashboard

- Add reconciliation page with status overview
- Display recurring instances with match status
- Add filtering and period selection
- Show pending matches for review

Refs: #028"
```

---

### Phase 7: Client - Match Review & Manual Matching ✅

**Objective:** User interface for reviewing and managing matches

**Tasks:**
- [x] Create MatchReviewModal component
- [x] Create ManualMatchDialog component
- [x] Add bulk accept/reject functionality
- [x] Add match confidence visualization (ConfidenceBadge)
- [ ] Integrate with transaction detail view
- [ ] Integrate with recurring detail view
- [ ] Write bUnit tests for review components

**Commit:**
```bash
git add .
git commit -m "feat(client): add match review ui

- Add modal for reviewing suggested matches
- Add manual match dialog for unlinked transactions
- Implement bulk accept/reject
- Show match details with confidence score

Refs: #028"
```

---

### Phase 8: Import Preview Enhancement

**Objective:** Show match suggestions in import preview UI

**Tasks:**
- [ ] Update ImportPreview component with match column
- [ ] Add RecurringMatchBadge component
- [ ] Add inline accept/reject buttons
- [ ] Show match confidence indicator
- [ ] Handle no-match cases gracefully
- [ ] Write bUnit tests for enhanced preview

**Commit:**
```bash
git add .
git commit -m "feat(client): enhance import preview with match suggestions

- Add recurring match column to import preview
- Show confidence badges for suggested matches
- Add inline match accept/reject controls
- Display variance indicators

Refs: #028"
```

---

### Phase 9: Tolerance Settings UI

**Objective:** Allow users to configure matching behavior

**Tasks:**
- [ ] Create ToleranceSettingsPage component
- [ ] Add global tolerance configuration
- [ ] Add per-recurring tolerance overrides
- [ ] Add preset options (strict, moderate, loose)
- [ ] Validate tolerance values
- [ ] Write bUnit tests for settings

**Commit:**
```bash
git add .
git commit -m "feat(client): add tolerance settings ui

- Add settings page for matching tolerances
- Support global and per-recurring overrides
- Add preset tolerance options
- Input validation

Refs: #028"
```

---

### Phase 10: Documentation & Cleanup

**Objective:** Final polish, documentation updates, and cleanup

**Tasks:**
- [ ] Update API documentation / OpenAPI specs
- [ ] Add/update XML comments for public APIs
- [ ] Update README if needed
- [ ] Remove any TODO comments
- [ ] Final code review
- [ ] Add user-facing help text

**Commit:**
```bash
git add .
git commit -m "docs(reconciliation): add documentation for feature 028

- XML comments for public API
- OpenAPI spec updates
- User guide for reconciliation workflow

Refs: #028"
```

---

## Conventional Commit Reference

Use these commit types to ensure proper changelog generation:

| Type | When to Use | SemVer Impact | Example |
|------|-------------|---------------|---------|
| `feat` | New feature or capability | Minor | `feat(reconciliation): add auto-matching` |
| `fix` | Bug fix | Patch | `fix(reconciliation): correct date tolerance` |
| `docs` | Documentation only | None | `docs: update reconciliation examples` |
| `refactor` | Code restructure, no feature/fix | None | `refactor(domain): extract matcher` |
| `test` | Adding or fixing tests | None | `test(reconciliation): add matcher tests` |

---

## Testing Strategy

### Unit Tests

- [ ] TransactionMatcher.CalculateConfidenceScore with various inputs
- [ ] TransactionMatcher handles null/empty descriptions
- [ ] MatchingTolerances.Default returns valid configuration
- [ ] Amount variance calculation (positive, negative, zero)
- [ ] Date offset calculation with various timezones
- [ ] ConfidenceLevel assignment based on score thresholds
- [ ] Transaction.LinkToRecurringInstance validation
- [ ] Already-linked transaction throws DomainException

### Integration Tests

- [ ] ReconciliationMatchRepository CRUD operations
- [ ] Matching during import creates ReconciliationMatch records
- [ ] Accept/reject updates match status correctly
- [ ] Scope filtering respects user permissions
- [ ] Tolerance settings persist and load correctly
- [ ] High-confidence auto-match applies Transaction link

### Manual Testing Checklist

- [ ] Import CSV and verify match suggestions appear
- [ ] Accept a suggested match, verify transaction links
- [ ] Reject a match, verify transaction imports unlinked
- [ ] View reconciliation dashboard with mixed statuses
- [ ] Manually match a transaction to recurring
- [ ] Configure tolerance settings and verify matching changes
- [ ] View variance history for a recurring transaction
- [ ] Update recurring amount from actual

---

## Migration Notes

### Database Migration

```bash
dotnet ef migrations add Feature028_ReconciliationMatching --project src/BudgetExperiment.Infrastructure --startup-project src/BudgetExperiment.Api
```

### Data Migration

Existing transactions linked to recurring transactions (via `RecurringTransactionId`) should be considered pre-reconciled. No data migration required for existing data.

### Breaking Changes

None. This feature is additive and does not modify existing API contracts.

---

## Security Considerations

- Reconciliation matches respect `BudgetScope` (Personal vs Shared)
- Users can only view/modify matches for transactions they own
- Tolerance settings are per-user (Personal scope supported)

---

## Performance Considerations

- Matching algorithm is O(n*m) where n=imports, m=recurring instances in range
- For typical imports (50-200 rows) and recurring (10-50), performance acceptable
- Index on `RecurringTransactionId + RecurringInstanceDate` for quick lookups
- Consider caching recurring instances during bulk import
- Future: Consider pre-computing pending instances table for large accounts

---

## Future Enhancements

- **AI-Assisted Matching:** Use ML to improve description matching beyond string similarity
- **Learn from User Feedback:** Adjust confidence scores based on accept/reject patterns
- **Notification Integration:** Alert users to missing recurring transactions
- **Mobile-Friendly Review:** Swipe-based accept/reject for mobile reconciliation
- **Batch Reconciliation Wizard:** Guided flow for first-time historical reconciliation

---

## References

- [Feature 024: Auto-Categorization Rules Engine](./024-auto-categorization-rules-engine.md) - Related matching patterns
- [Feature 027: CSV Import](./027-csv-import.md) - Integration point for import flow
- [Archive: Features 011-020](./archive/011-020-ui-recurring-settings.md) - Recurring transaction history

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-19 | Initial draft | @copilot |
| 2026-01-19 | Implemented Phases 1-6 (core feature complete) | @copilot |
