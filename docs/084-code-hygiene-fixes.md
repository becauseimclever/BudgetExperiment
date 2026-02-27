# Feature 084: Code Hygiene Fixes
> **Status:** Planning
> **Priority:** Medium (code quality / consistency)
> **Estimated Effort:** Small (< 1 day)
> **Dependencies:** None

## Overview

A codebase audit identified several smaller coding standard violations that individually don't warrant their own feature spec but collectively impact consistency. This document groups them for a single cleanup pass.

## Issues

### 1. Async Method Naming — MerchantMappingsController

**Standard violated:** §5 — "Async methods end with `Async`."

Three public async methods in `MerchantMappingsController.cs` are missing the `Async` suffix:

| Current Name | Line | Proposed Name |
|-------------|------|---------------|
| `GetLearned` | ~43 | `GetLearnedAsync` |
| `Learn` | ~57 | `LearnAsync` |
| `Delete` | ~82 | `DeleteAsync` |

All other ~500+ async methods across the codebase correctly use the `Async` suffix. This is an isolated violation in one controller.

### 2. Exception Handling Middleware Ordering

**Standard violated:** §28 — "Central middleware converting exceptions → ProblemDetails."

In `Program.cs`, `UseMiddleware<ExceptionHandlingMiddleware>()` is registered **after** `MapControllers()` (line ~143). Exception-handling middleware should be registered early in the pipeline (before routing/controllers) to catch all exceptions. The current ordering may cause exceptions in the controller pipeline to bypass the handler.

**Fix:** Move `app.UseMiddleware<ExceptionHandlingMiddleware>()` before `app.UseRouting()` / `app.MapControllers()`.

### 3. Non-Standard HTTP 499 Status Code

**Standard violated:** §9 — Use standard status codes.

`ExceptionHandlingMiddleware.cs` (line ~57) returns HTTP 499 for cancelled requests. This is an nginx convention, not an RFC standard code. Standard alternatives:
- **No response**: Simply don't write a response (connection is closed anyway)
- **408 Request Timeout**: If the request truly timed out
- **Drop**: Log and swallow — client has already disconnected

### 4. Inconsistent Pagination Headers

**Standard violated:** §9 — "Return `X-Pagination-TotalCount` header."

Only `TransactionsController.GetUncategorizedAsync` sets `X-Pagination-TotalCount`. `TransfersController` has pagination parameters (line ~92-93) but does **not** return the header. All paginated endpoints should consistently return this header.

### 5. ExceptionHandlingMiddleware Uses Anonymous Type

**Standard violated:** §28 — Use `ProblemDetails`.

The middleware serializes error responses from an anonymous object instead of `Microsoft.AspNetCore.Mvc.ProblemDetails`. While functional, this misses the `instance` field recommended by RFC 7807 and doesn't integrate with ASP.NET Core's built-in Problem Details machinery.

### 6. Contracts Project Undocumented

**Standard violated:** §3, §21 — Project listing doesn't include Contracts.

The `BudgetExperiment.Contracts` project (56 files, shared DTOs) is not mentioned in the copilot-instructions.md architecture documentation (§2, §3, §21). It should be added to the documented project list and architecture description.

### 7. Missing AddDomain() DI Extension

**Standard violated:** §14 — "Configure per layer registration extension methods (e.g., `AddDomain()`)."

No `AddDomain()` extension method exists. The Domain project is pure models/interfaces with no services to register, but the standard calls for the extension method pattern for all layers. A no-op `AddDomain()` method ensures consistency and provides a hook for future domain service registrations.

---

## Implementation Plan

### Phase 1: Fix Async Naming

**Tasks:**
- [ ] Rename `GetLearned` → `GetLearnedAsync` in `MerchantMappingsController`
- [ ] Rename `Learn` → `LearnAsync`
- [ ] Rename `Delete` → `DeleteAsync`
- [ ] Note: ASP.NET MVC strips `Async` suffix from action names by convention, so route behavior is unaffected

### Phase 2: Fix Middleware Ordering

**Tasks:**
- [ ] Move `UseMiddleware<ExceptionHandlingMiddleware>()` before `MapControllers()` in `Program.cs`
- [ ] Evaluate replacing 499 with standard status code or dropping the response
- [ ] Update middleware to use `ProblemDetails` class instead of anonymous type
- [ ] Add `instance` field per RFC 7807

### Phase 3: Fix Pagination Consistency

**Tasks:**
- [ ] Add `X-Pagination-TotalCount` header to `TransfersController` paginated endpoint
- [ ] Audit other controllers for missing pagination headers
- [ ] Consider creating a helper method for consistent pagination header setting

### Phase 4: Documentation and DI Updates

**Tasks:**
- [ ] Add `BudgetExperiment.Contracts` to copilot-instructions.md §3 and §21
- [ ] Create `AddDomain()` extension method (can be no-op returning `IServiceCollection`)
- [ ] Call `AddDomain()` in `Program.cs` for consistency

**Commit:**
```bash
git commit -m "fix: code hygiene cleanup from codebase audit

- Fix async naming in MerchantMappingsController (3 methods)
- Fix exception middleware ordering (register before MapControllers)
- Replace 499 with standard status handling
- Use ProblemDetails class instead of anonymous type
- Add X-Pagination-TotalCount to TransfersController
- Document Contracts project in copilot-instructions.md
- Add AddDomain() DI extension for consistency

Refs: #084"
```

---

## Testing Strategy

### Unit Tests
- [ ] Middleware ordering verified with integration test
- [ ] Pagination header present in transfer list endpoint

### Integration Tests
- [ ] Exception handling returns proper ProblemDetails format
- [ ] Renamed controller methods still route correctly

### Verification
- [ ] `dotnet build` succeeds
- [ ] All tests pass
- [ ] Manual verification of error responses

---

## Risk Assessment

- **Low risk**: Small, targeted fixes. Each is independent.
- **Middleware ordering**: Most impactful — verify exception handling works in all scenarios after moving.

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
