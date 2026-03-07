# API Versioning, Concurrency, Constants & Hygiene (081-086) - Consolidated Summary

**Consolidated:** 2026-03-06
**Original Features:** 081 through 086
**Status:** All Completed

---

## Overview

This document consolidates features 081–086, which covered adding runtime API versioning infrastructure, implementing optimistic concurrency with ETags across all mutable aggregates, centralizing ~36+ magic strings into shared constants classes, fixing code hygiene violations (exception middleware hardening, async naming, transfer pagination, DI/docs consistency), renaming Account/Transaction DateTime properties to the `*Utc` suffix convention, and refactoring the SuggestionsController to comply with the application service mediation pattern.

---

## 081: API Versioning Infrastructure

**Completed:** 2026-03-06

Runtime API versioning fully configured with URL segments (`/api/v{version}/{resource}`), `[ApiVersion]` attributes, `api-supported-versions` response headers, and deprecation support.

**Key Outcomes:**
- `Asp.Versioning.Mvc` and `Asp.Versioning.Mvc.ApiExplorer` NuGet packages installed
- `AddApiVersioning()` configured in `Program.cs` with URL segment reader, default version 1.0, and `AssumeDefaultVersionWhenUnspecified = true`
- All 26 controllers decorated with `[ApiVersion("1.0")]` and routes updated to `[Route("api/v{version:apiVersion}/[controller]")]`
- `VersionController` brought under versioned routing
- `SuggestionsController` route normalized to standard pattern
- OpenAPI spec reflects versioning; `api-supported-versions` header automatically included in responses

---

## 082: Optimistic Concurrency with ETags

**Completed:** 2026-03-04

Full optimistic concurrency implementation using PostgreSQL `xmin` system column across all 10 mutable aggregates and 21 PUT/PATCH endpoints, with Blazor client integration.

**Key Outcomes:**
- **PostgreSQL `xmin`** used as concurrency token — no schema migration or domain model changes required
- `DbUpdateConcurrencyException` → `409 Conflict` mapping added in `ExceptionHandlingMiddleware`
- ETag headers returned on all GET responses for mutable aggregates; `If-Match` validated on all PUT/PATCH requests
- During rollout, missing `If-Match` header is accepted (backward compatible); stale header returns 409
- **7 vertical slices** delivered: Foundation + Account → Transaction → BudgetCategory + BudgetGoal → RecurringTransaction → RecurringTransfer → Secondary Aggregates (CategorizationRule, CustomReport, ImportMapping) → Client Integration
- Client stores ETags from GET responses, sends `If-Match` on PUT/PATCH, handles 409 with user-friendly conflict toast and reload-and-retry flow

---

## 083: Centralize Magic Strings and Constants

**Completed:** 2026-03-05

Eliminated ~36+ magic string instances across 15+ files by creating 7 shared constants classes.

**Key Outcomes:**

| Constants Class | Project | Strings Centralized |
|----------------|---------|---------------------|
| `ClaimConstants` | Contracts | `"sub"`, `"preferred_username"`, `"email"`, `"name"`, `"picture"` (15 replacements across 6 files) |
| `ReconciliationStatus` | Contracts | `"Matched"`, `"Pending"`, `"Missing"` (5 replacements across 2 files) |
| `CurrencyDefaults` | Domain | `"USD"` (24 replacements across 3 files) |
| `OidcScopeDefaults` | Contracts | `"openid"`, `"profile"`, `"email"` scope arrays (4 files) |
| `ExportColumns` | Application | 14 export column header strings (17 replacements in ExportController) |
| `AiDefaults` | Domain | `"http://localhost:11434"` Ollama URL (8 replacements across 4 files) |
| `FamilyUserDefaults` | Contracts | Family user ID, name, email (6 replacements across 2 files) |

- Layer dependency rules respected: Contracts types kept as literals where Contracts → Domain dependency not allowed
- Each slice included regression guard tests verifying constant values
- Existing `AuthModeConstants`, `AuthProviderConstants`, `ImportValidationConstants` already centralized — not in scope

---

## 084: Code Hygiene Fixes

**Completed:** 2026-03-06

Four independent slices fixing coding standard violations across the codebase.

**Slice 1 — Exception Handling Middleware Hardening** (Critical):
- Moved `UseMiddleware<ExceptionHandlingMiddleware>()` before `MapControllers()` in `Program.cs` (was registered after, allowing exceptions to bypass handler)
- Replaced anonymous error objects with RFC 7807 `ProblemDetails` (including `Instance = context.Request.Path`)
- Replaced non-standard HTTP 499 for `OperationCanceledException` with standard handling

**Slice 2 — Async Method Naming** (Low):
- Renamed 3 methods in `MerchantMappingsController` to add `Async` suffix: `GetLearned` → `GetLearnedAsync`, `Learn` → `LearnAsync`, `Delete` → `DeleteAsync`
- No route changes (ASP.NET strips `Async` suffix from action names)

**Slice 3 — Transfer Pagination Consistency** (Medium):
- Created `TransferListPageResponse` DTO with `Items`, `TotalCount`, `Page`, `PageSize`, `TotalPages`
- Updated `ITransferService.ListAsync` and `TransferService.ListAsync` to return paged DTO
- `TransfersController.ListAsync` now sets `X-Pagination-TotalCount` header

**Slice 4 — Architecture Documentation & DI Consistency** (Low):
- Documented `BudgetExperiment.Contracts` and `BudgetExperiment.Shared` projects in `copilot-instructions.md` (§2, §3, §21)
- Created `AddDomain()` DI extension method (no-op, completes the per-layer pattern)
- Called `AddDomain()` in `Program.cs`

---

## 085: DateTime Naming Consistency

**Completed:** 2026-03-06

Cross-cutting rename of `Account` and `Transaction` DateTime properties from `CreatedAt`/`UpdatedAt` to `CreatedAtUtc`/`UpdatedAtUtc` per §30.

**Key Outcomes:**
- `Account.CreatedAt` → `Account.CreatedAtUtc`, `Account.UpdatedAt` → `Account.UpdatedAtUtc`
- `Transaction.CreatedAt` → `Transaction.CreatedAtUtc`, `Transaction.UpdatedAt` → `Transaction.UpdatedAtUtc`
- All other entities already followed the `*Utc` convention
- EF migration generated: `Feature085_RenameCreatedAtUpdatedAtToUtcSuffix` (renames 4 database columns)
- Changes propagated across all layers: Domain entities, EF configurations, Contracts DTOs, Application mappers, Client models/components, and tests
- Full test suite verified: 2,917 tests pass (831 Domain, 757 Application, 645 Client, 541 API, 143 Infrastructure)

---

## 086: SuggestionsController Service Layer Compliance

**Completed:** 2026-03-06

Refactored `SuggestionsController` to comply with §13 (application service mediation). It was the only controller (1 of 26) directly injecting repository interfaces.

**Key Outcomes:**
- Removed direct `IBudgetCategoryRepository` and `ICategorizationRuleRepository` injections from `SuggestionsController`
- Category/rule lookup logic moved into `RuleSuggestionService` (application layer)
- Controller now only injects application service interfaces, matching the pattern of all other 25 controllers
- No API endpoint or response shape changes — purely internal refactoring
- Application service tests added for the moved logic; controller tests updated
- Codebase grep confirmed: no remaining `IRepository` injections in any controller
