# Feature 039: Reconciliation label matching

## Overview

Use the existing transaction description to link an actual transaction to its automatic counterpart. When the description matches (along with the date), reconciliation should succeed even if the amounts differ.

## Problem Statement

Reconciliation currently relies on matching amounts, which fails when an actual transaction differs from its automatically generated counterpart. Users need a reliable way to reconcile by intent (using the description) rather than amount.

### Current State

- Automatic and actual transactions reconcile by amount and date.
- If the amount differs, reconciliation fails and users must manually resolve it.
- There is no stable identifier that links the two records across edits or imports.

### Target State

- Users can edit the transaction description on both automatic and actual transactions.
- Reconciliation matches by date and description; amount may differ.
- If the description does not match, current matching behavior remains unchanged.

---

## User Stories



### Manual Reconciliation Flag

#### US-039-004: Manually flag for reconciliation
**As a** user  
**I want to** manually flag a transaction or recurring item for reconciliation  
**So that** I can force a match when automatic matching fails

**Acceptance Criteria:**
- [ ] User can manually flag a transaction or recurring item for reconciliation
- [ ] Flagged items are surfaced in the UI for manual review
- [ ] Manual flag overrides auto-matching logic for the flagged pair
- [ ] Audit trail records who/when the manual flag was set

#### US-039-001: Use description for reconciliation
**As a** user  
**I want to** use the transaction description to link an automatic transaction and its actual counterpart  
**So that** they reconcile even if the amounts differ

**Acceptance Criteria:**
- [ ] The transaction description can be edited by the user.
- [ ] Reconciliation logic uses date and description to match, ignoring amount if both match.
- [ ] Descriptions are scoped to the user’s data set and do not leak between users.

#### US-039-002: Reconcile by date and description
**As a** user  
**I want to** reconcile transactions by matching date and description  
**So that** differing amounts do not block reconciliation

**Acceptance Criteria:**
- [ ] When the description matches on both sides, reconciliation uses date and description, ignoring amount.
- [ ] When description does not match, existing matching logic remains.
- [ ] A mismatch in date or description prevents reconciliation.

#### US-039-003: Prevent ambiguous matches
**As a** user  
**I want to** avoid ambiguous reconciliation matches  
**So that** the system is predictable

**Acceptance Criteria:**
- [ ] If multiple candidates share the same date and description, reconciliation fails with a clear error.
- [ ] The UI surfaces the ambiguity and prompts the user to resolve it.

---

## Technical Design


- Add a manual reconciliation flag property to transactions and recurring items (nullable, default false)
- Update reconciliation workflow to allow manual override when flag is set

- No new value object needed; use the existing transaction description field.
- Update application reconciliation workflow to match by date and description when present.


- Add optional `IsManuallyFlaggedForReconciliation` property to transaction and recurring item entities

- Use the existing `Description` property on the transaction entity.
- Validation: non-empty when provided, case-insensitive comparison, normalized storage (trimmed, max length as currently enforced).


| PATCH | /api/v1/transactions/{id}/flag-reconcile | Manually flag a transaction for reconciliation |
| PATCH | /api/v1/recurring/{id}/flag-reconcile | Manually flag a recurring item for reconciliation |

| Method | Endpoint | Description |
|--------|----------|-------------|
| PATCH | /api/v1/transactions/{id} | Edit transaction description |
| POST | /api/v1/transactions/reconcile | Reconcile by amount or by date+description depending on availability |


- Add nullable boolean `is_manually_flagged_for_reconciliation` column to transactions and recurring items tables

- No schema change required; use the existing `description` column.
- Add index on `(user_id, transaction_date, description)` if not already present.


- Add manual flag toggle to transaction and recurring item edit panels
- Show flagged items in reconciliation UI for manual review

- Transaction edit panel: ensure description is editable.
- Reconciliation UI: display description and match status; show ambiguity warnings.

---

## Implementation Plan


- [ ] Add `IsManuallyFlaggedForReconciliation` to domain entities
- [ ] Write unit tests for manual flag logic

**Objective:** Ensure description normalization and validation

**Tasks:**
- [ ] Confirm description normalization and validation rules
- [ ] Write unit tests for description-based matching
- [ ] Implement any needed domain changes

**Commit:**
- feat(domain): support reconciliation by description

---


- [ ] Update reconciliation service to allow manual override when flag is set
- [ ] Write application tests for manual flag scenarios

**Objective:** Match by date and description when present

**Tasks:**
- [ ] Update reconciliation service to prefer description match
- [ ] Add ambiguity detection and error handling
- [ ] Write application tests for description-based reconciliation
- [ ] Implement application changes

**Commit:**
- feat(app): reconcile by date and description

---


- [ ] Add PATCH endpoints for manual flag
- [ ] Add migration for new column(s)

**Objective:** Expose description updates and persist the field

**Tasks:**
- [ ] Ensure PATCH endpoint allows editing description
- [ ] Add index if needed
- [ ] Update DTOs and mapping for description if required
- [ ] Write API tests for description update and reconcile behaviors
- [ ] Implement API changes

**Commit:**
- feat(api): support reconciliation by description

---


- [ ] Add manual flag toggle to UI
- [ ] Show flagged items in reconciliation views

**Objective:** Enable description editing and display

**Tasks:**
- [ ] Ensure description is editable in transaction edit UI
- [ ] Show description in reconciliation views
- [ ] Add client tests if needed
- [ ] Implement UI changes

**Commit:**
- feat(client): show reconciliation description

---


- [ ] Document manual flag feature

**Objective:** Update docs and verify behavior

**Tasks:**
- [ ] Update API documentation and OpenAPI examples
- [ ] Add release notes if needed
- [ ] Final review and cleanup

**Commit:**
- docs: document reconciliation by description

---

## Testing Strategy

### Unit Tests

- [ ] Description normalization and validation rules
- [ ] Matching logic: description+date wins over amount
- [ ] Manual flag overrides auto-matching

### Integration Tests

- [ ] Reconcile with differing amounts and matching description/date
- [ ] Ambiguous description+date returns a conflict/validation error
- [ ] Manual flag forces reconciliation

### Manual Testing Checklist

- [ ] Edit description on automatic transaction
- [ ] Edit description on actual transaction with different amount
- [ ] Reconcile and verify match
- [ ] Manually flag transaction/recurring item and verify forced match

---

## Migration Notes

### Database Migration

- Add nullable boolean column for manual flag; add index on `(user_id, transaction_date, description, is_manually_flagged_for_reconciliation)` if needed

### Breaking Changes

- None expected

---

## Security Considerations

- Validate description input length and allowed characters.
- Ensure description and manual flag lookups are scoped by user ownership.

---

## Performance Considerations

- Add composite index to keep description/manual flag-based lookups fast.
- Description/manual flag matching should not increase query count.

---

## Future Enhancements

- Suggest descriptions based on recurring patterns.
- Bulk edit descriptions for imported transactions.
- Allow bulk manual flagging for imported or selected transactions.

---

## References

- Related doc: 038 reconciliation status endpoint mismatch

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-26 | Initial draft | @github-copilot |
