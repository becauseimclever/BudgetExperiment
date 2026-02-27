# Feature 082: Optimistic Concurrency with ETags
> **Status:** Planning
> **Priority:** High (data integrity)
> **Estimated Effort:** Large (5-7 days)
> **Dependencies:** None

## Overview

The coding standard (§9) requires "Support optimistic concurrency for mutable aggregates (header `If-Match`)." An audit found **zero implementation** of concurrency control — no concurrency tokens on entities, no ETag headers, no `If-Match` validation on PUT/PATCH endpoints. Mutable aggregates like `Account`, `Transaction`, `RecurringTransaction`, `BudgetCategory`, and `BudgetGoal` have no protection against lost updates.

In a multi-user budgeting application (especially with family scope sharing), concurrent edits can silently overwrite each other.

## Problem Statement

### Current State

- **No concurrency tokens** on any entity (`RowVersion`, `xmin`, or similar)
- **No ETag headers** in API responses
- **No `If-Match` validation** on PUT/PATCH endpoints
- **No `409 Conflict` responses** for concurrency violations
- Zero references to `ConcurrencyToken`, `RowVersion`, `IsConcurrencyToken`, `UseXminAsConcurrencyToken`, `ETag`, or `If-Match` in the codebase

### Target State

- Mutable aggregate roots have concurrency tokens
- GET responses include `ETag` header
- PUT/PATCH endpoints require `If-Match` header
- Concurrent modification returns `409 Conflict` with `ProblemDetails`
- PostgreSQL `xmin` system column used as concurrency token (lightweight, no schema change)

---

## User Stories

### US-082-001: Add Concurrency Tokens to Aggregates
**As a** developer
**I want to** aggregate root entities to have concurrency tokens
**So that** EF Core detects concurrent modifications.

**Acceptance Criteria:**
- [ ] `Account` has concurrency token configured via EF Fluent API
- [ ] `Transaction` has concurrency token
- [ ] `RecurringTransaction` has concurrency token
- [ ] `RecurringTransfer` has concurrency token
- [ ] `BudgetCategory` has concurrency token
- [ ] `BudgetGoal` has concurrency token
- [ ] All other mutable aggregates evaluated and protected
- [ ] PostgreSQL `xmin` used (no new columns, no migration needed)

### US-082-002: Add ETag Headers to GET Responses
**As a** developer
**I want to** GET endpoints to return `ETag` headers
**So that** clients can use them for conditional updates.

**Acceptance Criteria:**
- [ ] Single-resource GET endpoints include `ETag` header derived from concurrency token
- [ ] ETag follows HTTP standard format (`"value"` with quotes)
- [ ] Collection endpoints may use composite or skip ETag (evaluate)

### US-082-003: Validate If-Match on PUT/PATCH
**As a** developer
**I want to** PUT/PATCH endpoints to validate `If-Match` header
**So that** stale updates are rejected with `409 Conflict`.

**Acceptance Criteria:**
- [ ] PUT/PATCH endpoints check `If-Match` header
- [ ] Missing `If-Match` returns `428 Precondition Required` or is accepted (choose policy)
- [ ] Mismatched ETag returns `409 Conflict` with ProblemDetails
- [ ] `DbUpdateConcurrencyException` caught and converted to `409`
- [ ] Response body explains the conflict

---

## Technical Design

### PostgreSQL xmin Approach

PostgreSQL has a built-in `xmin` system column (transaction ID that last modified the row). EF Core Npgsql supports this as a concurrency token without schema changes:

```csharp
// In EF Configuration
builder.UseXminAsConcurrencyToken();
```

```csharp
// In entity (shadow property, no domain change needed)
// xmin is configured in Infrastructure only — domain stays clean
```

### ETag Flow

```
Client GET /api/v1/accounts/123
→ Response: 200 OK
  ETag: "12345"
  Body: { ... }

Client PUT /api/v1/accounts/123
  If-Match: "12345"
  Body: { ... }

If xmin matches:
→ Response: 200 OK
  ETag: "12346" (new xmin)

If xmin doesn't match:
→ Response: 409 Conflict
  Body: ProblemDetails { ... }
```

### Middleware vs Controller Approach

**Option A: Per-controller** — Each controller manually reads `If-Match`, passes to service, catches `DbUpdateConcurrencyException`.

**Option B: Action filter** (Recommended) — Create `ConcurrencyFilter` that:
1. Reads `If-Match` header
2. Passes the expected version to the service layer
3. Catches `DbUpdateConcurrencyException` and returns `409`

### Domain Impact

**None.** The concurrency token is a shadow property configured in Infrastructure's EF Fluent API. Domain entities remain pure.

### ExceptionHandlingMiddleware Update

Add handling for `DbUpdateConcurrencyException`:
```csharp
DbUpdateConcurrencyException => (409, "Conflict", "The resource was modified by another user.")
```

---

## Implementation Plan

### Phase 1: Configure xmin Concurrency Tokens

**Objective:** Add `UseXminAsConcurrencyToken()` to EF configurations for all mutable aggregates.

**Tasks:**
- [ ] Update `AccountConfiguration` with `UseXminAsConcurrencyToken()`
- [ ] Update `TransactionConfiguration`
- [ ] Update `RecurringTransactionConfiguration`
- [ ] Update `RecurringTransferConfiguration`
- [ ] Update `BudgetCategoryConfiguration`
- [ ] Update `BudgetGoalConfiguration`
- [ ] Evaluate and update other aggregate configurations
- [ ] Verify no migration needed (xmin is a system column)
- [ ] Write integration test: concurrent update throws `DbUpdateConcurrencyException`

### Phase 2: Add ETag Support to GET Endpoints

**Objective:** Return ETag headers from single-resource GET endpoints.

**Tasks:**
- [ ] Create helper method or filter to set ETag from xmin value
- [ ] Read xmin shadow property from entity after query
- [ ] Set `ETag` header on response
- [ ] Update key GET endpoints (accounts, transactions, recurring, budget)
- [ ] Write API tests verifying ETag presence

### Phase 3: Add If-Match Validation to PUT/PATCH

**Objective:** Validate concurrent modifications on update endpoints.

**Tasks:**
- [ ] Create `ConcurrencyFilter` or integrate into controllers
- [ ] Read `If-Match` header
- [ ] Set expected OriginalValue on xmin before SaveChanges
- [ ] Handle `DbUpdateConcurrencyException` → 409 response
- [ ] Update `ExceptionHandlingMiddleware` for 409
- [ ] Write API tests for conflict scenarios

### Phase 4: Client Integration

**Objective:** Update Blazor Client to handle ETags.

**Tasks:**
- [ ] Store ETag from GET responses
- [ ] Send `If-Match` header on PUT/PATCH requests
- [ ] Handle 409 responses with user-friendly notification
- [ ] Implement retry/reload strategy for conflicts

**Commit:**
```bash
git commit -m "feat(api): implement optimistic concurrency with ETags

- Configure xmin as concurrency token on all mutable aggregates
- GET endpoints return ETag header
- PUT/PATCH endpoints validate If-Match header
- 409 Conflict with ProblemDetails on concurrent modification
- Client sends/receives ETags for conflict detection

Refs: #082"
```

---

## Testing Strategy

### Unit Tests
- [ ] Concurrency token configuration verified in EF model tests
- [ ] ETag generation/parsing logic tested

### Integration Tests
- [ ] Two concurrent updates: first succeeds, second gets 409
- [ ] Missing If-Match header behavior tested
- [ ] ETag changes after successful update

### API Tests
- [ ] GET returns ETag header
- [ ] PUT with valid If-Match succeeds
- [ ] PUT with stale If-Match returns 409
- [ ] PUT without If-Match handled per policy

---

## Risk Assessment

- **Medium risk**: Requires careful testing of concurrency flows.
- **Client impact**: Existing client code doesn't send If-Match — decide on backward compatibility policy.
- **Performance**: xmin is a system column; no additional queries needed.
- **Migration**: None required — xmin exists automatically on all PostgreSQL tables.

---

## References

- Coding standard §9: "ETags / Concurrency: Support optimistic concurrency for mutable aggregates."
- [PostgreSQL xmin](https://www.postgresql.org/docs/current/ddl-system-columns.html)
- [Npgsql xmin concurrency](https://www.npgsql.org/efcore/modeling/concurrency.html)
- [RFC 7232: Conditional Requests](https://tools.ietf.org/html/rfc7232)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-26 | Initial draft from codebase audit | @copilot |
