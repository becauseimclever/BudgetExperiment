# Feature 039: Manual Reconciliation Linking
> **Status:** 🗒️ Planning

## Overview

Allow users to manually link an imported transaction to a specific recurring transaction instance when automatic matching fails or produces incorrect results. This provides an escape hatch for edge cases the weighted matching algorithm cannot handle.

## Problem Statement

### Current State

The existing `TransactionMatcher` uses a weighted confidence scoring system:
- **Description similarity: 50%** - Fuzzy matching between transaction descriptions
- **Amount match: 30%** - Tolerance-based amount comparison  
- **Date proximity: 20%** - How close the actual date is to the expected date

This handles most cases well, but fails when:
- Two recurring items have similar descriptions (e.g., "Electric Bill" and "Electric Car Payment")
- A transaction description changes significantly from what was expected
- The amount and date are both outside tolerance but the user knows it's the right match
- Auto-matching picks the wrong candidate from multiple possibilities

### Target State

- Users can manually link any imported transaction to any recurring transaction instance
- Manual links override automatic matching results
- Users can unlink incorrectly matched transactions
- Audit trail records manual linking actions

---

## User Stories

### US-039-001: Manually Link Transaction to Recurring Instance
**As a** user  
**I want to** manually link an imported transaction to a specific recurring transaction instance  
**So that** I can reconcile transactions when automatic matching fails

**Acceptance Criteria:**
- [ ] User can select an unmatched transaction and choose a recurring instance to link it to
- [ ] User can see a list of potential recurring instances (not limited by matching thresholds)
- [ ] Link is persisted and reflected in reconciliation status
- [ ] Transaction shows as "Manually Linked" in the UI

### US-039-002: Unlink Incorrectly Matched Transaction
**As a** user  
**I want to** unlink a transaction that was incorrectly matched (auto or manual)  
**So that** I can correct matching errors

**Acceptance Criteria:**
- [ ] User can unlink any matched transaction
- [ ] Unlinking returns both the transaction and recurring instance to "unmatched" state
- [ ] The match record is updated to "Rejected" status with reason

### US-039-003: View Linkable Recurring Instances
**As a** user  
**I want to** see all recurring instances within a reasonable date range when manually linking  
**So that** I can find the correct match even if it's outside normal tolerances

**Acceptance Criteria:**
- [ ] List shows recurring instances within ±30 days of transaction date
- [ ] List includes description, expected amount, and scheduled date
- [ ] List indicates which instances are already matched
- [ ] User can filter/search the list

### US-039-004: Audit Trail for Manual Links
**As a** user  
**I want to** see when a match was manually created or modified  
**So that** I have visibility into manual reconciliation actions

**Acceptance Criteria:**
- [ ] Manual matches show "Manual" indicator in match details
- [ ] Match record stores timestamp and source (Auto/Manual)
- [ ] Unlink actions are recorded

### US-039-005: Configure Import Description Pattern for Recurring Item
**As a** user  
**I want to** specify the expected import description pattern for a recurring item  
**So that** the system can automatically match imports even when the bank description differs from my recurring item name

**Acceptance Criteria:**
- [ ] User can add one or more "import patterns" to a recurring item
- [ ] Patterns can be exact text or contain wildcards (e.g., `*ACME CORP*`)
- [ ] When importing transactions, the matcher uses these patterns for description comparison
- [ ] Multiple recurring items cannot have overlapping patterns (validation error)
- [ ] Patterns are case-insensitive

**Example:**
- Recurring item: "Paycheck"
- Import patterns: `*ACME CORP PAYROLL*`, `ACH DEPOSIT ACME*`
- Imported description: "DIRECT DEP ACME CORP PAYROLL 01/15" → matches "Paycheck"

### US-039-006: Learn Pattern from Manual Link
**As a** user  
**I want to** optionally save the import description as a pattern when manually linking  
**So that** future imports with the same description auto-match

**Acceptance Criteria:**
- [ ] When manually linking, user sees option: "Remember this description for future imports"
- [ ] If selected, the imported transaction's description is added as a pattern to the recurring item
- [ ] Pattern is normalized (trimmed, optional wildcard prefix/suffix)

---

## Technical Design

### Domain Changes

- Add `MatchSource` enum: `Auto`, `Manual`
- Add `MatchSource` property to `ReconciliationMatch` entity
- Add `ManualLink()` method to create manual matches
- Add `Unlink()` method to reject/remove matches
- Add `ImportPatterns` collection to `RecurringTransaction` entity (list of pattern strings)
- Add pattern matching logic to `TransactionMatcher` to check import patterns first

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/v1/reconciliation/link | Manually link transaction to recurring instance |
| DELETE | /api/v1/reconciliation/matches/{matchId} | Unlink/reject a match |
| GET | /api/v1/reconciliation/linkable-instances?transactionId={id} | Get recurring instances available for manual linking |
| PUT | /api/v1/recurring/{id}/import-patterns | Update import patterns for a recurring item |
| GET | /api/v1/recurring/{id}/import-patterns | Get import patterns for a recurring item |

### Request/Response DTOs

```csharp
public record ManualLinkRequest(
    Guid TransactionId,
    Guid RecurringTransactionId,
    DateOnly RecurringInstanceDate,
    bool RememberPattern = false);

public record LinkableInstanceDto(
    Guid RecurringTransactionId,
    string Description,
    decimal ExpectedAmount,
    DateOnly InstanceDate,
    bool IsAlreadyMatched);

public record ImportPatternsDto(
    IReadOnlyList<string> Patterns);
```

### Database Changes

- Add `match_source` column to `reconciliation_matches` table (varchar, default 'Auto')
- Add `import_patterns` table: `recurring_transaction_id`, `pattern` (varchar), `created_at_utc`
- Or: Add `import_patterns` JSONB column to `recurring_transactions` table

### UI Changes

- Add "Link Manually" button on unmatched transactions
- Add "Unlink" button on matched transactions
- Create modal/dialog showing linkable recurring instances
- Show "Manual" badge on manually linked matches

---

## Implementation Plan

### Phase 1: Domain & Application
**Objective:** Add manual linking capability to domain and application layers

**Tasks:**
- [ ] Add `MatchSource` enum to domain
- [ ] Update `ReconciliationMatch` entity with `MatchSource` property
- [ ] Add `ManualLink()` and `Unlink()` methods
- [ ] Add `ImportPatterns` collection to `RecurringTransaction`
- [ ] Add pattern matching logic to `TransactionMatcher`
- [ ] Write unit tests for manual linking behavior
- [ ] Write unit tests for import pattern matching
- [ ] Update `ReconciliationService` with manual link/unlink methods
- [ ] Write application layer tests

**Commit:** `feat(domain): add manual reconciliation linking and import patterns`

---

### Phase 2: API & Infrastructure
**Objective:** Expose manual linking via REST API

**Tasks:**
- [ ] Add database migration for `match_source` column
- [ ] Add database migration for import patterns
- [ ] Create DTOs for manual linking
- [ ] Add POST endpoint for manual linking
- [ ] Add DELETE endpoint for unlinking
- [ ] Add GET endpoint for linkable instances
- [ ] Add PUT/GET endpoints for import patterns
- [ ] Write API integration tests

**Commit:** `feat(api): add manual reconciliation linking endpoints`

---

### Phase 3: Client UI
**Objective:** Enable manual linking from the reconciliation UI

**Tasks:**
- [ ] Add "Link Manually" button to unmatched transactions
- [ ] Create linkable instances modal/dialog
- [ ] Add "Unlink" button to matched transactions
- [ ] Show "Manual" badge on manually linked items
- [ ] Add "Remember this description" checkbox in link dialog
- [ ] Add import patterns management UI on recurring item edit screen
- [ ] Add client-side tests

**Commit:** `feat(client): add manual reconciliation linking UI`

---

## Testing Strategy

### Unit Tests
- [ ] `ManualLink()` creates match with `MatchSource.Manual`
- [ ] `Unlink()` sets match status to `Rejected`
- [ ] Cannot manually link already-matched transaction (must unlink first)
- [ ] Import pattern matching: exact match works
- [ ] Import pattern matching: wildcard prefix/suffix works
- [ ] Import pattern matching: case-insensitive
- [ ] Overlapping patterns across recurring items detected

### Integration Tests
- [ ] POST /reconciliation/link creates manual match
- [ ] POST /reconciliation/link with `RememberPattern=true` adds pattern
- [ ] DELETE /reconciliation/matches/{id} rejects match
- [ ] GET /linkable-instances returns instances within date range
- [ ] PUT /recurring/{id}/import-patterns saves patterns
- [ ] Import with matching pattern auto-matches to correct recurring item

### Manual Testing Checklist
- [ ] Manually link an unmatched transaction
- [ ] Verify "Manual" badge appears
- [ ] Use "Remember this description" option
- [ ] Import new transaction with same description - verify auto-match
- [ ] Add import patterns via recurring item edit screen
- [ ] Unlink a matched transaction
- [ ] Verify both sides return to unmatched state

---

## Migration Notes

### Database Migration
- Add `match_source VARCHAR(10) NOT NULL DEFAULT 'Auto'` to `reconciliation_matches`
- Add `import_patterns` table or JSONB column to `recurring_transactions`
- Update existing records to 'Auto' (no-op since default)

### Breaking Changes
- None expected

### Matching Priority
When matching imports, the system will check in order:
1. **Import patterns** - If a recurring item has patterns configured, check those first (high confidence if matched)
2. **Description similarity** - Fall back to existing fuzzy matching algorithm
3. **Amount and date** - As before

---

## Security Considerations
- Validate user owns both the transaction and recurring item before linking
- Scope queries by user ownership

---

## Performance Considerations
- Linkable instances query should use existing indexes
- Limit date range to prevent excessive data retrieval

---

## Future Enhancements
- Bulk manual linking for imported transactions
- Suggested links based on historical patterns
- Configurable matching tolerances per recurring item

---

## References
- Related: Feature 038 (Reconciliation Status Endpoint)
- Current implementation: [TransactionMatcher.cs](../src/BudgetExperiment.Domain/Reconciliation/TransactionMatcher.cs)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-31 | Revised to focus on manual linking; removed redundant description matching | @github-copilot |
| 2026-01-26 | Initial draft | @github-copilot |
