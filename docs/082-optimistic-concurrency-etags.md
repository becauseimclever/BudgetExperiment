# Feature 082: Optimistic Concurrency with ETags

> **Status:** In Progress (Slices 1–2 complete)
> **Priority:** High (data integrity)
> **Dependencies:** None

## Problem

The coding standard (§9) requires optimistic concurrency for mutable aggregates via `If-Match` headers. A codebase audit found **zero implementation**: no concurrency tokens, no ETag headers, no `If-Match` validation, and no `409 Conflict` responses across 10 mutable aggregates and 21 PUT/PATCH endpoints. Concurrent edits silently overwrite each other.

## Approach

- **PostgreSQL `xmin`** system column as concurrency token — no migration, no domain changes.
- **`ExceptionHandlingMiddleware`** catches `DbUpdateConcurrencyException` → `409 Conflict`.
- **Per-controller** ETag read/write — explicit, testable, no magic filters.
- **If-Match optional during rollout** — missing header is accepted (backward compatible), stale header returns 409.
- Each vertical slice delivers one aggregate end-to-end: EF config → service plumbing → controller ETag/If-Match → API tests.

---

## Vertical Slices

### Slice 1: Foundation + Account

First slice establishes shared infrastructure and proves the pattern on `Account` (1 PUT endpoint — simplest aggregate).

**Tasks:**
- [x] Add `DbUpdateConcurrencyException` → 409 mapping in `ExceptionHandlingMiddleware`
- [x] Add `UseXminAsConcurrencyToken()` to `AccountConfiguration`
- [x] Surface `xmin` value through repository/service so controller can read it
- [x] `GET /accounts/{id}` returns `ETag` header from xmin
- [x] `PUT /accounts/{id}` reads `If-Match`, sets xmin original value before save
- [x] API test: GET returns ETag header
- [x] API test: PUT with valid If-Match succeeds, returns new ETag
- [x] API test: PUT with stale If-Match returns 409 Conflict
- [x] API test: PUT without If-Match still succeeds (backward compatible)

### Slice 2: Transaction

`Transaction` has 1 PUT + 1 PATCH endpoint.

**Tasks:**
- [x] Add `UseXminAsConcurrencyToken()` to `TransactionConfiguration`
- [x] `GET /transactions/{id}` returns ETag
- [x] `PUT /transactions/{id}` validates If-Match
- [x] `PATCH /transactions/{id}/location` validates If-Match
- [x] API tests: ETag returned, valid/stale/missing If-Match scenarios

### Slice 3: BudgetCategory + BudgetGoal

Both budgeting aggregates in one slice (1 PUT each).

**Tasks:**
- [ ] Add `UseXminAsConcurrencyToken()` to `BudgetCategoryConfiguration`
- [ ] Add `UseXminAsConcurrencyToken()` to `BudgetGoalConfiguration`
- [ ] `GET /budgets/{categoryId}` returns ETag
- [ ] `PUT /budgets/{categoryId}` validates If-Match
- [ ] Budget goal endpoints: ETag + If-Match where applicable
- [ ] API tests for both aggregates

### Slice 4: RecurringTransaction

4 PUT endpoints — instance and future-instance edits need concurrency protection too.

**Tasks:**
- [ ] Add `UseXminAsConcurrencyToken()` to `RecurringTransactionConfiguration`
- [ ] `GET /recurring/{id}` returns ETag
- [ ] `PUT /recurring/{id}` validates If-Match
- [ ] `PUT /recurring/{id}/instances/{date}` validates If-Match
- [ ] `PUT /recurring/{id}/instances/{date}/future` validates If-Match
- [ ] API tests for each endpoint

### Slice 5: RecurringTransfer

3 PUT endpoints, same pattern as Slice 4.

**Tasks:**
- [ ] Add `UseXminAsConcurrencyToken()` to `RecurringTransferConfiguration`
- [ ] `GET /recurring-transfers/{id}` returns ETag
- [ ] `PUT /recurring-transfers/{id}` validates If-Match
- [ ] `PUT /recurring-transfers/{id}/instances/{date}` validates If-Match
- [ ] `PUT /recurring-transfers/{id}/instances/{date}/future` validates If-Match
- [ ] API tests for each endpoint

### Slice 6: Secondary Aggregates

Lower-risk aggregates with single update endpoints each.

**Tasks:**
- [ ] `CategorizationRule`: xmin config + ETag + If-Match on `PUT /rules/{id}`
- [ ] `CustomReport`: xmin config + ETag + If-Match on `PUT /reports/{id}`
- [ ] `ImportMapping`: xmin config + ETag + If-Match on `PUT /import/mappings/{id}`
- [ ] API tests for each

### Slice 7: Client Integration

Update Blazor WASM client to participate in the concurrency protocol.

**Tasks:**
- [ ] Store ETag from GET responses in state/service
- [ ] Send `If-Match` header on PUT/PATCH requests
- [ ] Handle 409 response: show user-friendly conflict notification
- [ ] Offer reload-and-retry flow for conflicts

---

## Technical Notes

### xmin Configuration (Infrastructure only, no domain impact)

```csharp
// In each entity's IEntityTypeConfiguration
builder.UseXminAsConcurrencyToken();
```

### ETag Flow

```
GET /api/v1/accounts/123
→ 200 OK
  ETag: "12345"
  Body: { ... }

PUT /api/v1/accounts/123
  If-Match: "12345"
  Body: { ... }

→ 200 OK (xmin matched)     or     → 409 Conflict (xmin changed)
  ETag: "12346"                       ProblemDetails { ... }
```

### If-Match Policy

During rollout, missing `If-Match` is accepted (no `428`). This keeps backward compatibility while the client integration (Slice 7) is developed. Once the client sends ETags, the policy can be tightened.

### ExceptionHandlingMiddleware Addition

```csharp
DbUpdateConcurrencyException => (409, "Conflict", "The resource was modified by another user. Reload and try again.")
```

---

## References

- Coding standard §9: optimistic concurrency for mutable aggregates
- [PostgreSQL xmin](https://www.postgresql.org/docs/current/ddl-system-columns.html)
- [Npgsql xmin concurrency](https://www.npgsql.org/efcore/modeling/concurrency.html)
- [RFC 7232: Conditional Requests](https://tools.ietf.org/html/rfc7232)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-03-04 | Slice 2 implemented: Transaction (xmin, ETag, If-Match on PUT + PATCH/location) | @copilot |
| 2026-03-04 | Slice 1 implemented: Foundation + Account (xmin, ETag, If-Match, 409 middleware) | @copilot |
| 2026-03-04 | Rewrite as vertical slices; confirmed zero existing implementation | @copilot |
| 2026-02-26 | Initial draft from codebase audit | @copilot |
