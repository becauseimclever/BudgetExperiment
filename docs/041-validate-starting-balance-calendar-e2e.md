# Feature 041: Validate Starting Balance in Calendar (E2E)
> **Status:** 🗒️ Planning

## Overview

Ensure that the starting balance for a given account is accurately reflected in the running balance shown on the calendar view for that account. Add E2E tests (see doc 036) against the demo environment to validate this behavior.

## Problem Statement

Currently, there is no automated test to guarantee that the starting balance is correctly incorporated into the running balance displayed on the calendar. Manual errors or regressions could cause the calendar to show incorrect balances, confusing users and undermining trust.

### Current State

- Starting balance is set per account, but its effect on the calendar running balance is not automatically tested.
- No E2E coverage for this scenario in the demo environment.

### Target State

- The calendar view always reflects the correct running balance, starting from the account's starting balance.
- E2E tests run against the demo environment to validate this for all supported account types.

---

## User Stories

### Calendar Balance Validation

#### US-041-001: Calendar reflects starting balance
**As a** user  
**I want to** see the correct running balance on the calendar, starting from my account's starting balance  
**So that** I can trust the accuracy of my financial overview

**Acceptance Criteria:**
- [ ] The calendar running balance starts with the account's starting balance
- [ ] All transactions are applied in order to the running balance
- [ ] E2E tests confirm this for demo accounts

#### US-041-002: E2E test coverage
**As a** developer  
**I want to** have automated E2E tests for starting balance and running balance on the calendar  
**So that** regressions are caught before release

**Acceptance Criteria:**
- [ ] E2E tests run against the demo environment
- [ ] Tests validate starting balance and running balance for at least one account
- [ ] Failures are reported with clear diagnostics

---

## Technical Design

### Architecture Changes

- No backend changes required unless bugs are found
- Add or update E2E test suite to cover this scenario

### Domain Model

- No changes required

### API Endpoints

- No changes required

### Database Changes

- No changes required

### UI Components

- No changes required; tests will interact with the existing calendar view

---

## Implementation Plan

### Phase 1: E2E test design and implementation

**Objective:** Add E2E tests for starting balance and running balance on the calendar

**Tasks:**
- [ ] Design E2E test scenarios for starting balance and running balance
- [ ] Implement E2E tests using Playwright (see doc 036)
- [ ] Run tests against the demo environment URL
- [ ] Validate results and report failures

**Commit:**
- test(e2e): validate starting balance in calendar

---

### Phase 2: Documentation and cleanup

**Objective:** Document test coverage and update references

**Tasks:**
- [ ] Update E2E test documentation
- [ ] Reference this feature in doc 036
- [ ] Final review and cleanup

**Commit:**
- docs: document starting balance calendar E2E tests

---

## Testing Strategy

### E2E Tests

- [ ] Calendar running balance starts with account starting balance
- [ ] Transactions are applied in order
- [ ] Failures are reported with clear diagnostics

### Manual Testing Checklist

- [ ] Set starting balance for an account
- [ ] Add transactions
- [ ] Verify calendar running balance matches expected values

---

## Migration Notes

- None

---

## Security Considerations

- None

---

## Performance Considerations

- E2E tests should run efficiently and not block CI

---

## Future Enhancements

- Add E2E coverage for multiple accounts and edge cases
- Visual regression tests for calendar view

---

## References

- See doc 036: demo environment E2E tests
- Calendar and account management features

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-26 | Initial draft | @github-copilot |
