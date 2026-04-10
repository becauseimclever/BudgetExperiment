# Feature 159: GET /transactions Date-Range Endpoint — Deprecate v1, Add Paginated v2

> **Status:** Done  
> **Severity:** 🟠 High — P-007  
> **Audit Source:** `docs/audit/2026-04-09-performance-review.md`

---

## Decision (Fortinbra, 2026-04-09)

**Chosen approach: Deprecate v1 + create new paginated v2 endpoint.**

- The existing `GET /api/v1/transactions?startDate=&endDate=` endpoint will be **deprecated** (not removed) — existing clients continue to work with `Deprecation` and `Sunset` headers added to responses.
- A new `GET /api/v2/transactions/by-date-range` endpoint will be introduced with proper `page` / `pageSize` parameters and `X-Pagination-TotalCount` header.
- This ensures we never break existing clients while providing a correct, efficient path for new clients.

---

## Overview

`TransactionsController.GetByDateRangeAsync` (lines 57–85) accepts `startDate` and `endDate` but has no `page` / `pageSize` parameters. A query spanning a full year could return thousands of transactions in a single HTTP response — large JSON serialisation on the Raspberry Pi server, large payload over the network, and large Blazor WASM deserialisation on the client. The unified paginated endpoint `GetUnifiedPagedAsync` already exists and implements server-side pagination correctly.

This feature adds pagination to `GetByDateRangeAsync` **and** formally recommends deprecating it in favour of the unified paginated endpoint. The two options are presented as parallel implementation paths; the Product Owner (Fortinbra) should choose which path to pursue.

---

## Problem Statement

### Current State

```
GET /api/v1/transactions?startDate=2025-01-01&endDate=2025-12-31
```

Returns all matching transactions in a single response — no limit. A full year's worth of transactions on an active account could be 3,000–10,000 items.

The paginated endpoint `GetUnifiedPagedAsync` (`GET /api/v1/transactions?page=1&pageSize=50`) already handles this case and returns `X-Pagination-TotalCount` in the response header.

### Target State — Option A (Recommended): Deprecate GetByDateRangeAsync

- Add `Deprecation` and `Sunset` headers to `GetByDateRangeAsync`.
- Recommend all callers migrate to `GetUnifiedPagedAsync` with date-range filter parameters.
- Set a sunset date (e.g., 90 days after deprecation announcement).
- Remove `GetByDateRangeAsync` in a future version (v2 endpoint cleanup).

### Target State — Option B: Add Pagination to GetByDateRangeAsync

- Add optional `page` (default 1) and `pageSize` (default 50, max 100) query parameters.
- Return `X-Pagination-TotalCount` header.
- Update OpenAPI spec.
- Backward compatible — existing callers without pagination parameters receive page 1 with 50 results.

**Alfred's recommendation:** Pursue Option A. The unified endpoint already exists and is superior. Adding pagination to a second endpoint creates duplication and split API surface. Deprecate `GetByDateRangeAsync` and consolidate.

---

## User Stories

### US-159-001 (Option A): Safe Deprecation of Unbounded Endpoint

**As an** API maintainer  
**I want** to deprecate `GetByDateRangeAsync` in favour of the paginated unified endpoint  
**So that** there is a single, well-maintained way to query transactions with date filtering

**Acceptance Criteria:**
- [x] `GetByDateRangeAsync` returns `Deprecation: true` and `Sunset: <date>` HTTP response headers
- [x] OpenAPI spec marks the endpoint as deprecated
- [x] XML doc comment on the action method describes the migration path to `GetUnifiedPagedAsync`
- [x] API Tests assert `Deprecation` header is present on responses from this endpoint

### US-159-002 (Option B): Paginated Date-Range Endpoint

**As a** client consuming the date-range endpoint  
**I want** to paginate the results  
**So that** large date ranges do not cause slow page loads or memory issues

**Acceptance Criteria:**
- [x] `page` and `pageSize` query parameters added (defaults: `page=1`, `pageSize=50`)
- [x] `pageSize` capped at 100 — requests for larger sizes return HTTP 400
- [x] `X-Pagination-TotalCount` header returned with every paginated response
- [x] Existing callers without pagination parameters automatically receive page 1 of 50 results (backward compatible)
- [x] OpenAPI spec updated with new parameters and `X-Pagination-TotalCount` response header documentation
- [x] API integration tests cover: default pagination, explicit page/size, max size enforcement, count header accuracy

---

## Technical Design

### Option A — Deprecation Path

#### Controller Change

```csharp
// TransactionsController.cs
[HttpGet("by-date-range")]
[Obsolete("Use GET /api/v1/transactions with page/pageSize and date filter parameters instead.")]
public async Task<IActionResult> GetByDateRangeAsync(
    [FromQuery] DateOnly startDate,
    [FromQuery] DateOnly endDate,
    CancellationToken cancellationToken)
{
    Response.Headers.Append("Deprecation", "true");
    Response.Headers.Append("Sunset", "2026-07-09");  // 90 days from deprecation
    Response.Headers.Append("Link",
        "</api/v1/transactions>; rel=\"successor-version\"");

    // existing implementation unchanged
    ...
}
```

#### OpenAPI Annotation

```csharp
[ApiExplorerSettings(IgnoreApi = false)]  // keep visible so clients can see deprecation notice
// Add [Obsolete] XML summary describing migration path
```

In the OpenAPI spec (via `WithOpenApi()` or XML comments), mark operation as `deprecated: true`.

---

### Option B — Pagination Addition

#### Controller Change

```csharp
[HttpGet("by-date-range")]
public async Task<IActionResult> GetByDateRangeAsync(
    [FromQuery] DateOnly startDate,
    [FromQuery] DateOnly endDate,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50,
    CancellationToken cancellationToken)
{
    if (pageSize > 100) return BadRequest("pageSize cannot exceed 100.");
    if (page < 1) return BadRequest("page must be at least 1.");

    var (items, totalCount) = await _transactionService
        .GetByDateRangePagedAsync(startDate, endDate, page, pageSize, cancellationToken);

    Response.Headers.Append("X-Pagination-TotalCount", totalCount.ToString());
    return Ok(items);
}
```

#### Service / Repository Changes

- Add `GetByDateRangePagedAsync(DateOnly start, DateOnly end, int page, int pageSize)` to `ITransactionService` and `ITransactionRepository` (or reuse `GetUnifiedPagedAsync` with date filters).
- Return `(IReadOnlyList<TransactionDto> Items, int TotalCount)`.

#### Affected Files (Option B)

| File | Layer | Change |
|------|-------|--------|
| `src/BudgetExperiment.Api/Controllers/TransactionsController.cs` | API | Add pagination parameters, `X-Pagination-TotalCount` header |
| `src/BudgetExperiment.Application/Transactions/ITransactionService.cs` | Application | Add `GetByDateRangePagedAsync` |
| `src/BudgetExperiment.Application/Transactions/TransactionService.cs` | Application | Implement `GetByDateRangePagedAsync` |
| `src/BudgetExperiment.Infrastructure/Persistence/Repositories/TransactionRepository.cs` | Infrastructure | Add paginated date-range query |
| `tests/BudgetExperiment.Api.Tests/Controllers/TransactionsControllerTests.cs` | Tests | Pagination tests |

---

## Implementation Plan

### Phase 0: Decision

- [x] **Fortinbra chose: deprecate v1 + create paginated v2 endpoint** (2026-04-09). Do not modify the existing v1 endpoint's behaviour — add deprecation headers only. New v2 endpoint carries proper pagination.

The phases below cover **Option A** as the primary path; Option B tasks are noted in brackets.

---

### Phase 1: Tests (RED)

**Option A tasks:**
- [ ] Add API test `GetByDateRange_Returns_DeprecationHeader`
- [ ] Add API test `GetByDateRange_Returns_SunsetHeader`
- [ ] Add API test `GetByDateRange_OpenApiSpec_MarksEndpointAsDeprecated`
- [ ] Run tests — expect **RED**

**Option B tasks (if chosen):**
- [ ] Add API test `GetByDateRange_WithDefaultPagination_Returns50Items`
- [ ] Add API test `GetByDateRange_WithPageSizeOver100_Returns400`
- [ ] Add API test `GetByDateRange_ReturnsPaginationTotalCountHeader`
- [ ] Run tests — expect **RED**

**Commit:**
```
test(api): failing tests for transactions date-range pagination/deprecation

Refs: Feature 159, P-007
```

---

### Phase 2: Implementation (GREEN)

**Option A:**
- [ ] Add `Deprecation`, `Sunset`, and `Link` response headers to `GetByDateRangeAsync`
- [ ] Add `[Obsolete]` attribute and XML doc comment with migration path
- [ ] Mark operation as deprecated in OpenAPI configuration
- [ ] Run tests — expect **GREEN**

**Option B:**
- [ ] Add `page` / `pageSize` parameters with defaults and validation
- [ ] Implement `GetByDateRangePagedAsync` in service and repository layers
- [ ] Return `X-Pagination-TotalCount` header
- [ ] Update OpenAPI spec to document parameters and response header
- [ ] Run tests — expect **GREEN**

**Commit (Option A):**
```
feat(api): deprecate GetByDateRange endpoint; add Deprecation + Sunset headers

Marks endpoint deprecated in favour of GET /api/v1/transactions with pagination.
Sunset date: 2026-07-09.

Fixes: P-007 (2026-04-09 performance audit)
Refs: Feature 159
```

**Commit (Option B):**
```
feat(api): add pagination to GetByDateRange endpoint

Adds page/pageSize parameters (default 50, max 100) and
X-Pagination-TotalCount response header.

Fixes: P-007 (2026-04-09 performance audit)
Refs: Feature 159
```

---

### Phase 3: Verification

**Tasks:**
- [ ] Run full test suite: `dotnet test --filter "Category!=Performance"` — all green
- [ ] Run `dotnet format` — no style violations
- [ ] Verify updated OpenAPI spec at `/scalar` in development
- [ ] Test endpoint manually with `curl` or Scalar UI; confirm headers present

---

## Testing Strategy

### API Integration Tests

**Option A:**
- `GetByDateRange_Returns_DeprecationHeader_True`
- `GetByDateRange_Returns_SunsetHeader_WithCorrectDate`
- `GetByDateRange_Returns_LinkHeader_PointingToUnifiedEndpoint`
- `OpenApiSpec_GetByDateRange_MarkedDeprecated`

**Option B:**
- `GetByDateRange_WithNoPageParams_Returns50ItemsPage1`
- `GetByDateRange_WithPage2_ReturnsCorrectItems`
- `GetByDateRange_WithPageSizeOver100_Returns400`
- `GetByDateRange_WithPage0_Returns400`
- `GetByDateRange_ReturnsPaginationTotalCountHeaderMatchingFilteredCount`

---

## Security Considerations

Pagination parameters are validated (page ≥ 1, pageSize ≤ 100) before use. No authorisation surface changes — existing scope filtering in the service/repository layer applies to all paginated results.

---

## Performance Considerations

- **Hardware target:** Raspberry Pi ARM64. Large JSON serialisation on the server and WASM deserialisation on the client are both costly.
- **Option A improvement:** Deprecating the endpoint eliminates the unbounded response path entirely as callers migrate to the already-optimised unified endpoint.
- **Option B improvement:** Default page size of 50 reduces response payload from potentially thousands of items to a bounded 50; `X-Pagination-TotalCount` allows clients to implement progressive loading.

---

## References

- [2026-04-09 Performance Audit — P-007](../docs/audit/2026-04-09-performance-review.md#p-007-high--transactionscontroller-get-endpoint-has-no-pagination)
- `src/BudgetExperiment.Api/Controllers/TransactionsController.cs:57-85`
- Engineering Guide §9 (REST — pagination, `X-Pagination-TotalCount`, deprecation headers), §20 (Versioning & Deprecation)
- RFC 8594 — `Sunset` HTTP header

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-04-09 | Initial spec from Vic audit P-007; recommends Option A (deprecate) | Alfred (Lead) |
