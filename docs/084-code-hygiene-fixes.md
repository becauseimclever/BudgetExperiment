# Feature 084: Code Hygiene Fixes

> **Status:** Complete
> **Priority:** Medium (code quality / consistency)
> **Dependencies:** None

## Overview

A codebase audit identified several coding standard violations. This document organizes them as independently deliverable **vertical slices** — each slice crosses layers, includes its own tests, and can be merged on its own.

---

## Slice 1 — Exception Handling Middleware Hardening

> **Priority:** Critical — middleware ordering bug means exceptions may bypass the handler entirely.
> **Standards:** §9 (status codes), §28 (ProblemDetails)

### Problem

Three related issues in `ExceptionHandlingMiddleware` and its registration:

| # | Issue | Detail |
|---|-------|--------|
| A | **Wrong pipeline position** | `UseMiddleware<ExceptionHandlingMiddleware>()` is registered *after* `MapControllers()` in `Program.cs`. Exceptions thrown inside the controller pipeline can bypass the handler. |
| B | **Non-standard 499 status** | `OperationCanceledException` returns HTTP 499 (nginx convention). Not an RFC standard code. |
| C | **Anonymous type response** | Error responses are built from an anonymous object instead of `Microsoft.AspNetCore.Mvc.ProblemDetails`, missing `instance` field and built-in framework integration. |

### Implementation

**Files touched:**

- `src/BudgetExperiment.Api/Program.cs` — move middleware registration before `MapControllers()`
- `src/BudgetExperiment.Api/Middleware/ExceptionHandlingMiddleware.cs`:
  - Replace anonymous object with `ProblemDetails` (include `Instance = context.Request.Path`)
  - Replace 499 → log + abort without writing response (client already disconnected), or use `StatusCodes.Status408RequestTimeout` if response not yet started

**Tasks:**

- [x] RED: Write unit test — middleware returns RFC 7807 `ProblemDetails` for unhandled exception
- [x] RED: Write unit test — middleware handles `OperationCanceledException` without 499
- [x] GREEN: Refactor middleware to use `ProblemDetails` class
- [x] GREEN: Replace 499 with standard handling
- [x] REFACTOR: Move `UseMiddleware<ExceptionHandlingMiddleware>()` before `MapControllers()` in `Program.cs`
- [x] Verify all existing tests pass

**Commit:** `fix(api): harden exception middleware — ordering, ProblemDetails, drop 499`

---

## Slice 2 — Async Method Naming (MerchantMappingsController)

> **Priority:** Low — naming-only change, no behavior impact.
> **Standard:** §5 — "Async methods end with `Async`."

### Problem

Three public async methods in `MerchantMappingsController` are missing the `Async` suffix. All other ~500+ async methods across the codebase follow the convention.

| Current Name | Proposed Name |
|-------------|---------------|
| `GetLearned` | `GetLearnedAsync` |
| `Learn` | `LearnAsync` |
| `Delete` | `DeleteAsync` |

ASP.NET MVC strips the `Async` suffix from action names by convention, so **routes and OpenAPI docs are unaffected**.

### Implementation

**Files touched:**

- `src/BudgetExperiment.Api/Controllers/MerchantMappingsController.cs` — rename 3 methods

**Tasks:**

- [x] Rename methods
- [x] Verify `dotnet build` succeeds
- [x] Verify existing tests pass (no test changes needed — routes unchanged)

**Commit:** `refactor(api): add Async suffix to MerchantMappingsController methods`

---

## Slice 3 — Transfer Pagination Consistency

> **Priority:** Medium — clients cannot paginate transfers properly without `TotalCount`.
> **Standard:** §9 — "Return `X-Pagination-TotalCount` header."

### Problem

`TransfersController.ListAsync` accepts `page` and `pageSize` but does **not** return:
- `X-Pagination-TotalCount` response header
- Total count in the response body

The service layer (`ITransferService.ListAsync`) returns `IReadOnlyList<TransferListItemResponse>` — a bare list with no pagination metadata. By comparison, `UncategorizedTransactionService.GetPagedAsync` returns `UncategorizedTransactionPageDto` with `TotalCount`, `Page`, `PageSize`, and `TotalPages`.

### Implementation

This slice crosses **Application → API** layers:

**Files touched:**

- `src/BudgetExperiment.Contracts/Transfers/TransferListPageResponse.cs` — new paged DTO (matches `UncategorizedTransactionPageDto` pattern: `Items`, `TotalCount`, `Page`, `PageSize`, `TotalPages`)
- `src/BudgetExperiment.Application/Transfers/ITransferService.cs` — change `ListAsync` return type to `TransferListPageResponse`
- `src/BudgetExperiment.Application/Transfers/TransferService.cs` — query total count before pagination, return paged DTO
- `src/BudgetExperiment.Api/Controllers/TransfersController.cs` — set `X-Pagination-TotalCount` header

**Tasks:**

- [x] RED: Write unit test — `TransferService.ListAsync` returns `TransferListPageResponse` with correct `TotalCount`
- [x] RED: Write integration test — `TransfersController.ListAsync` response includes `X-Pagination-TotalCount` header
- [x] GREEN: Create `TransferListPageResponse` DTO
- [x] GREEN: Update `ITransferService` and `TransferService`
- [x] GREEN: Update `TransfersController` to set header
- [x] REFACTOR: Audit other paginated endpoints for same issue
- [x] Verify all existing tests pass

**Commit:** `feat(api): add pagination metadata to transfer list endpoint`

---

## Slice 4 — Architecture Documentation & DI Consistency

> **Priority:** Low — documentation and structural consistency only.
> **Standards:** §3 (project listing), §14 (DI extensions), §21 (folder layout)

### Problem

| # | Issue |
|---|-------|
| A | `BudgetExperiment.Contracts` (56+ shared DTOs) is not documented in `copilot-instructions.md` §2, §3, or §21. |
| B | `BudgetExperiment.Shared` (shared enums: `BudgetScope`, `CategorySource`, `DescriptionMatchMode`) is also undocumented. |
| C | No `AddDomain()` DI extension exists. `AddApplication()` and `AddInfrastructure()` follow the pattern, but Domain is skipped. |

### Implementation

**Files touched:**

- `.github/copilot-instructions.md` — add Contracts and Shared to §2, §3, §21
- `src/BudgetExperiment.Domain/DependencyInjection.cs` — new file, no-op `AddDomain()` extension
- `src/BudgetExperiment.Api/Program.cs` — call `builder.Services.AddDomain()` before `AddApplication()`

**Tasks:**

- [x] Update `copilot-instructions.md` §2 (architecture description)
- [x] Update `copilot-instructions.md` §3 (project list)
- [x] Update `copilot-instructions.md` §21 (folder layout)
- [x] Create `AddDomain()` extension method (no-op, returns `IServiceCollection`)
- [x] Call `AddDomain()` in `Program.cs`
- [x] Verify `dotnet build` succeeds

**Commit:** `docs: document Contracts/Shared projects, add AddDomain() DI extension`

---

## Slice Dependency & Ordering

```
Slice 1 (Exception Middleware)  ──┐
Slice 2 (Async Naming)          ──┤── all independent, any order
Slice 3 (Transfer Pagination)   ──┤
Slice 4 (Docs & DI)             ──┘
```

All four slices are **independent** — no slice depends on another. Recommended order is by priority: 1 → 3 → 2 → 4.

---

## Risk Assessment

| Slice | Risk | Mitigation |
|-------|------|------------|
| 1 — Exception Middleware | Medium — pipeline reordering affects all requests | Unit test middleware in isolation; run full test suite |
| 2 — Async Naming | Minimal — ASP.NET strips suffix; routes unchanged | Build verification only |
| 3 — Transfer Pagination | Low — additive change (new DTO, new header) | Unit + integration tests |
| 4 — Docs & DI | Minimal — docs + no-op method | Build verification only |

---

## References

- Coding standard §5: Async naming
- Coding standard §9: REST API design, status codes, pagination
- Coding standard §14: DI extension methods
- Coding standard §28: Error handling with ProblemDetails
- [RFC 7807: Problem Details](https://tools.ietf.org/html/rfc7807)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-26 | Initial draft from codebase audit | @copilot |
| 2026-03-06 | Restructured into vertical slices; added Shared project to Slice 4; detailed pagination layer changes in Slice 3 | @copilot |
