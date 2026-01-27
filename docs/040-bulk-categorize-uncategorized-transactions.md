# Feature 040: Bulk Categorize Uncategorized Transactions
> **Status:** 🗒️ Planning

## Overview

Provide a UI and API to view all uncategorized transactions, filter them, and bulk assign categories. This helps users efficiently categorize transactions when auto-categorization fails or is incomplete.

## Problem Statement

Currently, users must categorize uncategorized transactions one at a time, which is slow and tedious when auto-categorization misses many items. There is no way to filter or bulk assign categories to speed up this process.

### Current State

- Uncategorized transactions are not easily discoverable in bulk.
- No filtering or bulk assignment UI exists.
- Users must edit each transaction individually.

### Target State

- Users can view all uncategorized transactions in a single view.
- Filtering by date, amount, description, or other fields is supported.
- Users can select multiple transactions and assign a category in bulk.
- Bulk actions are validated and auditable.

---

## User Stories

### Bulk Categorization

#### US-040-001: View uncategorized transactions
**As a** user  
**I want to** see all uncategorized transactions in one place  
**So that** I can quickly identify and categorize them

**Acceptance Criteria:**
- [ ] There is a view listing all uncategorized transactions
- [ ] The view supports paging and sorting

#### US-040-002: Filter uncategorized transactions
**As a** user  
**I want to** filter uncategorized transactions by date, amount, or description  
**So that** I can find related transactions to categorize together

**Acceptance Criteria:**
- [ ] Filtering by date range, amount, and description is supported
- [ ] Filters can be combined

#### US-040-003: Bulk assign categories
**As a** user  
**I want to** select multiple uncategorized transactions and assign a category in one action  
**So that** I can efficiently categorize many transactions at once

**Acceptance Criteria:**
- [ ] Multiple transactions can be selected
- [ ] A category can be assigned to all selected transactions in one action
- [ ] Bulk assignment is validated and errors are surfaced

---

## Technical Design

### Architecture Changes

- Add API endpoint to list uncategorized transactions with filtering and paging
- Add API endpoint for bulk category assignment
- Update client to provide uncategorized view, filtering, and bulk actions

### Domain Model

- No changes required; use existing transaction and category models

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/v1/transactions/uncategorized | List uncategorized transactions with filters |
| POST | /api/v1/transactions/bulk-categorize | Bulk assign category to transactions |

### Database Changes

- Ensure index on category_id for efficient uncategorized queries

### UI Components

- New page/view for uncategorized transactions
- Filtering controls (date, amount, description)
- Bulk select and assign category UI

---

## Implementation Plan

### Phase 1: API endpoints and tests

**Objective:** List and bulk categorize uncategorized transactions

**Tasks:**
- [ ] Add GET endpoint for uncategorized transactions with filters
- [ ] Add POST endpoint for bulk category assignment
- [ ] Write unit and integration tests
- [ ] Implement endpoints

**Commit:**
- feat(api): uncategorized transaction listing and bulk categorize

---

### Phase 2: UI implementation

**Objective:** Provide uncategorized view and bulk actions

**Tasks:**
- [ ] Add uncategorized transactions page/view
- [ ] Add filtering controls
- [ ] Add bulk select and assign UI
- [ ] Write client tests if needed
- [ ] Implement UI

**Commit:**
- feat(client): uncategorized view and bulk categorize

---

### Phase 3: Documentation and cleanup

**Objective:** Update docs and verify behavior

**Tasks:**
- [ ] Update API documentation and OpenAPI examples
- [ ] Add release notes if needed
- [ ] Final review and cleanup

**Commit:**
- docs: document bulk categorize uncategorized transactions

---

## Testing Strategy

### Unit Tests

- [ ] Filtering logic for uncategorized transactions
- [ ] Bulk assignment validation

### Integration Tests

- [ ] List uncategorized transactions with filters
- [ ] Bulk assign category and verify update

### Manual Testing Checklist

- [ ] View uncategorized transactions
- [ ] Filter and select transactions
- [ ] Bulk assign category and verify

---

## Migration Notes

- Ensure index on category_id for performance

---

## Security Considerations

- Validate user permissions for bulk actions
- Ensure only user’s own transactions are affected

---

## Performance Considerations

- Indexes for fast uncategorized queries and updates
- Paging for large result sets

---

## Future Enhancements

- Suggest categories for bulk selection
- Undo/rollback for bulk actions

---

## References

- Related: auto-categorization, transaction management

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-26 | Initial draft | @github-copilot |
