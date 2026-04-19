# Feature 161: BudgetScope Removal

> **Status:** ✅ COMPLETE — All 4 Phases merged to main (2026-04-19). Migration `RemoveBudgetScopeColumns` ready to apply.

## Overview

In the early project phase, the user made a fundamental architectural decision: **BudgetScope (Personal vs Shared) contradicts the Kakeibo philosophy.** Kakeibo is explicitly a household ledger (家計簿). There is only one scope: the shared family/household ledger. The personal/shared duality must be removed.

Currently, `BudgetScope` enum lives in `BudgetExperiment.Shared` and is referenced in **~80+ source files** across all layers (Domain, Application, Infrastructure, API, Client). This feature plans a phased removal strategy that:

1. Eliminates UI complexity (ScopeSwitcher)
2. Removes API contracts and middleware
3. Purges the concept from Domain and Application layers
4. Migrates the database schema

The deliverable is **architectural clarity** — one scope everywhere, no branching logic for personal vs shared.

---

## Problem Statement

### Current State

- **Enum location:** `BudgetExperiment.Shared/Budgeting/BudgetScope.cs`
  - Values: `Personal = 0`, `Shared = 1`, `All = 2`
- **API layer:** 
  - `BudgetScopeMiddleware` extracts scope from header and injects into user context
  - `UserContext.cs` stores `BudgetScope` property
  - DTOs include scope fields (all transaction, budget, category responses leak scope)
- **Application/Domain layers:**
  - `IUserContext` interface exposes `BudgetScope` property
  - All repositories filter by scope (e.g., `_context.Transactions.Where(t => t.BudgetScope == userScope)`)
  - All entities (Account, Transaction, BudgetCategory, RecurringTransaction, etc.) have `BudgetScope` property
  - Service methods accept scope as parameter or extract from user context
- **Client layer:**
  - `ScopeService` manages the current scope state
  - `ScopeSwitcher.razor` component is a dropdown UI letting users toggle Personal ↔ Shared
  - `ScopeMessageHandler` injects scope header into every API request
  - `UserSettings` persists the last-selected scope
- **Database:**
  - All entities have `BudgetScope` column(s); queries filter by scope
  - No logical separation (scope is not a separate table); it's an entity property

### Target State

- **No BudgetScope enum** — removed from codebase
- **All data is "household"** — no client-side scope switching
- **API simplification** — no scope header, no scope DTOs fields
- **Domain clarity** — entities have no scope property; repositories don't branch on scope
- **Database migration** — drop scope columns from all entity tables
- **UI simplification** — remove ScopeSwitcher component

### Why This Matters

Kakeibo philosophy is rooted in the household (family) ledger as a single source of truth for reflection and intentional spending. A personal scope alongside a shared scope introduces a duality that breaks the mental model:
- **Household members** should all see and reflect on the same ledger
- **Scope switching** (Personal ↔ Shared) introduces "what's mine vs. what's ours" thinking, opposite to the Kakeibo ethos
- **No API/DB support for multi-tenant households** — scope is not per-household; it's per-user, undermining accountability

---

## User Stories

### Core Functionality

#### US-161-001: Hide Scope UI
**As a** user of the budgeting app  
**I want to** not see the ScopeSwitcher dropdown in the navigation  
**So that** I am not confused by a Personal/Shared toggle that doesn't fit Kakeibo

**Acceptance Criteria:**
- [x] ScopeSwitcher component is removed or hidden from Navigation.razor
- [x] Default scope is `Shared` (household) everywhere the user navigates
- [x] No "Personal" scope option is available in the UI
- [x] User is not presented with scope-switching choices
- [x] Application behavior is unchanged (all operations default to household scope)

**Slice 1 progress (Lucius):**
- Removed the navigation scope switcher from the client UI.
- Removed the account form's Personal/Shared choice and replaced it with household-ledger wording.
- Normalized incoming account form models to `Shared` so legacy `Personal` values cannot leak back through the hidden client form path.
- Kept the client compatibility plumbing in place for now, but forced the client default scope to `Shared` so existing API behavior remains stable during Phase 1.

#### US-161-002: Remove Scope from API Layer
**As a** API consumer  
**I want to** not see BudgetScope fields in API responses  
**So that** I don't need to understand or handle scope logic

**Acceptance Criteria:**
- [x] `BudgetScopeMiddleware` is removed
- [x] API `UserContext` no longer accepts request-driven scope changes
- [x] All API DTOs (request/response) no longer expose `BudgetScope` field
- [x] API endpoints work correctly without scope header
- [x] Documentation is updated to reflect no scope header requirement
- [x] No breaking changes to endpoints that don't mention scope (transparent removal)
- [x] OpenAPI spec auto-updates (scope fields disappear)

#### US-161-003: Simplify IUserContext Interface
**As a** domain or application developer  
**I want to** not see scope on the user context abstraction  
**So that** services and repositories have a single mental model (household)

**Acceptance Criteria:**
- [ ] `IUserContext.BudgetScope` property is removed
- [ ] No method signatures accept or reference `BudgetScope`
- [ ] All repository filters on scope are removed
- [ ] Domain entities no longer have `BudgetScope` property
- [ ] Repository queries for a given user return household data only (no branching)
- [ ] Tests are updated or rewritten to reflect no-scope assumption

#### US-161-004: Database Schema Migration
**As a** infrastructure team  
**I want to** drop BudgetScope columns from all entity tables  
**So that** the schema reflects the single-scope reality

**Acceptance Criteria:**
- [ ] EF Core migration is created and applied to remove scope columns
- [ ] Migration is backward-compatible (can be rolled back if needed)
- [ ] Seed data or user data migration is handled (scope values converted to household default)
- [ ] `DbContext.OnModelCreating()` no longer configures scope as a discriminator or filter
- [ ] Database queries no longer reference BudgetScope columns

#### US-161-005: Verify Zero Regressions
**As a** QA engineer  
**I want to** confirm all features work after scope removal  
**So that** the application is production-ready

**Acceptance Criteria:**
- [ ] All existing unit tests pass (mocked scope removed)
- [ ] All integration tests pass (DB queries no longer branch on scope)
- [ ] API contract tests pass (scope fields no longer in responses)
- [ ] E2E tests pass (no scope selection in UI, all operations work)
- [ ] Performance is not degraded (no additional queries or logic)
- [ ] No regressions in reports, transactions, budgets, or accounts

---

## Technical Design

### Architecture Changes

#### Remove BudgetScope Enum
- **File:** `BudgetExperiment.Shared/Budgeting/BudgetScope.cs`
- **Action:** Delete file entirely (no replacement)
- **Impact:** All references to `BudgetScope` become compiler errors; refactoring must address each one

#### API Layer Changes

**Remove BudgetScopeMiddleware**
- **File:** `BudgetExperiment.Api/Middleware/BudgetScopeMiddleware.cs`
- **Action:** Delete middleware class and unregister from `Program.cs` (`app.UseMiddleware<BudgetScopeMiddleware>()` line removed)

**Simplify UserContext**
- **File:** `BudgetExperiment.Api/UserContext.cs`
- **Changes:**
  ```csharp
  public class UserContext : IUserContext
  {
      public string UserId { get; }
      public string UserEmail { get; }
      // REMOVED: public BudgetScope BudgetScope { get; }
      // REMOVED: setter for BudgetScope
  }
  ```

**Update DTOs**
- **Files affected:** All DTOs in `BudgetExperiment.Contracts/` (e.g., `TransactionDto`, `BudgetCategoryDto`, `AccountDto`, `RecurringTransactionDto`)
- **Action:** Remove `BudgetScope` property from all DTO classes
- **Example:**
  ```csharp
  public sealed record TransactionDto(
      Guid Id,
      DateTime DateUtc,
      string Description,
      // REMOVED: BudgetScope Scope,
      decimal Amount,
      Guid AccountId,
      Guid CategoryId
  );
  ```

**Update API Endpoints**
- **Files affected:** All controllers in `BudgetExperiment.Api/Controllers/`
- **Action:** Remove scope-related logic (scope extraction from header, scope validation, scope-based filtering before calling service)
- **Example:**
  ```csharp
  // OLD (remove)
  var scope = _userContext.BudgetScope;
  var transactions = await _transactionService.GetByAccountAsync(accountId, scope);
  
  // NEW (scope is not passed; service assumes household)
  var transactions = await _transactionService.GetByAccountAsync(accountId);
  ```

#### Application Layer Changes

**Simplify IUserContext Interface**
- **File:** `BudgetExperiment.Domain/Identity/IUserContext.cs`
- **Changes:**
  ```csharp
  public interface IUserContext
  {
      string UserId { get; }
      string UserEmail { get; }
      // REMOVED: BudgetScope BudgetScope { get; }
  }
  ```

**Update Service Method Signatures**
- **Files affected:** All services in `BudgetExperiment.Application/Services/`
- **Action:** Remove `BudgetScope` parameters from method signatures; assume household scope implicitly
- **Examples:**
  ```csharp
  // OLD
  Task<IReadOnlyList<TransactionDto>> GetByAccountAsync(Guid accountId, BudgetScope scope);
  
  // NEW
  Task<IReadOnlyList<TransactionDto>> GetByAccountAsync(Guid accountId);
  ```

#### Domain Layer Changes

**Remove Scope Property from Entities**
- **Files affected:** All entity classes in `BudgetExperiment.Domain/`
  - `Account.cs`
  - `Transaction.cs`
  - `BudgetCategory.cs`
  - `RecurringTransaction.cs`
  - `RecurringTransfer.cs`
  - `Budget.cs`
  - `ImportBatch.cs`
  - (any other entity with BudgetScope property)
- **Action:** Remove `BudgetScope` property
- **Example:**
  ```csharp
  public class Transaction
  {
      public Guid Id { get; set; }
      public DateTime DateUtc { get; set; }
      // REMOVED: public BudgetScope BudgetScope { get; set; }
      public decimal Amount { get; set; }
      // ... other properties
  }
  ```

#### Infrastructure Layer Changes

**Update EF Core Configuration**
- **File:** `BudgetExperiment.Infrastructure/Persistence/AppDbContext.cs` and related `EntityTypeConfiguration` classes
- **Action:** Remove scope-related fluent configuration (e.g., `.Property(t => t.BudgetScope)`, scope filters, scope discriminators)
- **Example:**
  ```csharp
  // OLD
  modelBuilder.Entity<Transaction>()
      .Property(t => t.BudgetScope)
      .HasConversion<int>();
  
  // NEW (remove entirely)
  ```

**Simplify Repository Implementations**
- **Files affected:** All repository classes in `BudgetExperiment.Infrastructure/Repositories/`
  - `AccountRepository.cs`
  - `TransactionRepository.cs`
  - `BudgetCategoryRepository.cs`
  - etc.
- **Action:** Remove scope-based query filtering; queries return all household data
- **Example:**
  ```csharp
  // OLD
  var transactions = _context.Transactions
      .Where(t => t.UserId == userId && t.BudgetScope == userScope)
      .ToListAsync();
  
  // NEW (scope filter removed)
  var transactions = _context.Transactions
      .Where(t => t.UserId == userId)  // Household data only
      .ToListAsync();
  ```

#### Client Layer Changes

**Remove ScopeSwitcher Component**
- **File:** `BudgetExperiment.Client/Components/Navigation/ScopeSwitcher.razor`
- **Action:** Delete component entirely; remove from Navigation.razor

**Remove ScopeService**
- **File:** `BudgetExperiment.Client/Services/ScopeService.cs`
- **Action:** Delete service class and unregister from `Program.cs`

**Remove ScopeMessageHandler**
- **File:** `BudgetExperiment.Client/Services/ScopeMessageHandler.cs`
- **Action:** Delete handler; remove from HttpClient configuration in `Program.cs`

**Update UserSettings**
- **File:** `BudgetExperiment.Client/Services/UserSettingsService.cs` (if it stores scope preference)
- **Action:** Remove `CurrentScope` or equivalent property; no client-side state for scope

**Update Navigation.razor**
- **File:** `BudgetExperiment.Client/Components/Navigation/Navigation.razor`
- **Action:** Remove ScopeSwitcher component reference

### Database Changes

**Migration: Drop BudgetScope Columns**
- **Migration name:** `RemoveBudgetScopeColumns`
- **Action:** Drop `BudgetScope` columns from all entity tables
- **Tables affected:**
  - `transactions`
  - `accounts`
  - `budget_categories`
  - `budgets`
  - `recurring_transactions`
  - `recurring_transfers`
  - `import_batches`
  - (any other tables with BudgetScope column)
- **Example migration code:**
  ```csharp
  protected override void Up(MigrationBuilder migrationBuilder)
  {
      migrationBuilder.DropColumn("BudgetScope", "transactions");
      migrationBuilder.DropColumn("BudgetScope", "accounts");
      migrationBuilder.DropColumn("BudgetScope", "budget_categories");
      // ... other tables
  }
  
  protected override void Down(MigrationBuilder migrationBuilder)
  {
      migrationBuilder.AddColumn<int>("BudgetScope", "transactions", defaultValue: 1);  // 1 = Shared
      migrationBuilder.AddColumn<int>("BudgetScope", "accounts", defaultValue: 1);
      // ... other tables
  }
  ```

### Files to Delete

1. `BudgetExperiment.Shared/Budgeting/BudgetScope.cs`
2. `BudgetExperiment.Api/Middleware/BudgetScopeMiddleware.cs`
3. `BudgetExperiment.Client/Components/Navigation/ScopeSwitcher.razor`
4. `BudgetExperiment.Client/Services/ScopeService.cs`
5. `BudgetExperiment.Client/Services/ScopeMessageHandler.cs`

### Files to Edit (by layer)

**API Layer (BudgetExperiment.Api):**
- `Program.cs` — remove middleware registration, remove ScopeMessageHandler from HttpClient, unregister ScopeService
- `UserContext.cs` — remove BudgetScope property
- All controller files — remove scope extraction and scope-based branching

**Application Layer (BudgetExperiment.Application):**
- All service files — remove BudgetScope parameters, simplify method signatures
- Validators — remove scope validation if any

**Domain Layer (BudgetExperiment.Domain):**
- `Identity/IUserContext.cs` — remove BudgetScope property
- All entity files — remove BudgetScope property

**Infrastructure Layer (BudgetExperiment.Infrastructure):**
- `Persistence/AppDbContext.cs` — remove scope configuration
- All `EntityTypeConfiguration` files — remove scope fluent configuration
- All repository files — remove scope-based query filtering

**Client Layer (BudgetExperiment.Client):**
- `Program.cs` — remove ScopeService registration, remove ScopeMessageHandler from HttpClient config
- `Components/Navigation/Navigation.razor` — remove ScopeSwitcher component reference
- Any component that references scope state — simplify

**Tests (all test projects):**
- Remove or update tests that mock/verify scope behavior
- Update repository tests to not branch on scope
- Update service tests to remove scope parameters
- Update API endpoint tests to not send scope header

---

## Phased Delivery

### Phase 1: Hide UI (Low Risk)
**Estimate:** 2–3 days  
**Status:** ✅ **COMPLETE** (as of 2026-04-18)  
**Goal:** Ship a deployable version with no user-facing scope switching  
**Deliverables:**
- Remove ScopeSwitcher component ✅
- Default all operations to Shared/household scope ✅
- Application behavior unchanged (queries still filter, but always by Shared) ✅
- Scope removal code does not yet touch Domain/Application/Infrastructure ✅
- Unit and integration tests remain unchanged (mocks still return scope-filtered data) ✅

**Acceptance Criteria Phase 1:**
- [x] UI no longer shows ScopeSwitcher
- [x] All user operations use Shared scope implicitly
- [x] No API changes; client compatibility plumbing remains in place for now
- [x] All existing tests pass
- [x] Can be deployed without breaking existing clients

**Risk:** Minimal — UI-only change, backend behavior preserved

---

### Phase 2: Remove from API Layer (Medium Risk)
**Estimate:** 3–4 days  
**Status:** ✅ **COMPLETE** (2026-04-18, merged to main)  
**Goal:** Clean up API contracts; no more scope header or scope DTO fields  
**Deliverables:**
- ✅ Remove BudgetScopeMiddleware
- ✅ Stop using request-scoped BudgetScope in UserContext
- ✅ Remove BudgetScope from all DTOs
- ✅ Update all controllers to not expect or use scope header
- ✅ Update OpenAPI spec (scope fields auto-removed)
- ✅ Service and repository scope internals remain for now; API simply defaults to household/shared behavior

**Acceptance Criteria Phase 2:**
- [x] No BudgetScope in API request/response contracts
- [x] Middleware no longer runs
- [x] API UserContext no longer accepts request-driven scope changes
- [x] All API endpoints work without scope header
- [x] OpenAPI spec is clean (no scope references)
- [x] API integration tests pass (5,876 tests all passing)
- [x] Scope is still used in repositories (Phase 3 removes it)

**Implementation Details:**
- BudgetScopeMiddleware deleted; middleware registration removed from Program.cs
- UserContext.BudgetScope hardcoded to `Shared`; UserContext.SetScope() is a no-op
- All DTOs (AccountDto, TransactionDto, etc.) have no BudgetScope field
- Controllers do not extract or validate scope from requests
- ScopeMessageHandler no longer sends scope header to API
- New integration tests (Feature161Phase2ApiContractTests) prove endpoints work without scope header
- Commit `abd55c8`: "feat(161): Phase 2 complete - remove BudgetScope from API layer"

**Risk:** Medium — API contracts change; clients must adapt (but default to no scope) — **✅ RESOLVED**

---

### Phase 3: Remove from Application/Domain (High Risk)
**Estimate:** 4–5 days  
**Goal:** Purge scope concept from service methods, repositories, and domain entities  
**Deliverables:**
- Remove BudgetScope property from all entities
- Update IUserContext to not expose BudgetScope
- Simplify all service method signatures (no scope parameter)
- Update all repository queries to remove scope filtering
- Update all tests to remove scope mocking/verification
- Database migration created (not yet applied)

**Acceptance Criteria Phase 3:**
- [ ] No BudgetScope property on any entity
- [ ] IUserContext has no scope reference
- [ ] Service methods no longer accept scope parameter
- [ ] Repository queries filter by user only (not scope)
- [ ] All unit and integration tests pass
- [ ] Database migration is created and tested (rollback works)
- [ ] No compiler errors; code compiles and runs locally

**Risk:** High — fundamental refactoring; extensive test updates; high surface area

---

### Phase 4: Database Migration (Data-Critical)
**Estimate:** 1–2 days (post-Phase-3 validation)  
**Goal:** Apply migration; clean up schema  
**Deliverables:**
- Deploy Phase 3 code + migration to staging
- Run migration (drops scope columns)
- Verify all queries and reports work
- Promote to production

**Acceptance Criteria Phase 4:**
- [ ] Migration applied to staging database successfully
- [ ] All queries return correct data (no scope column needed)
- [ ] Reports, exports, and analytics work
- [ ] No query errors in logs
- [ ] Rollback tested and verified
- [ ] Production deployment executed
- [ ] Post-deployment monitoring confirms no errors

**Risk:** Data-critical — requires backup, tested rollback, production coordination

---

## Implementation Strategy (TDD)

For each file modified:

1. **Write test(s)** that document the expected behavior without scope
   - Service method returns correct data for user (no scope branching)
   - Repository query includes user filter only (no scope)
   - DTO does not serialize scope
   - Entity class has no scope property
2. **Make changes** to pass the test
3. **Refactor** for clarity and SOLID compliance
4. **Run full test suite** (unit + integration); verify no regressions

**Critical test updates:**
- All unit tests mocking `IUserContext` — remove scope setup
- All repository integration tests — verify queries don't branch on scope
- All service tests — simplify to not pass scope
- All API endpoint tests — don't send scope header, verify no scope in response

---

## Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| **High surface area (~80+ files)** | Phase delivery; each phase independently deployable and testable |
| **Test regression (scope mocks everywhere)** | Systematic test update during each phase; CI must pass before proceeding |
| **Data loss if migration goes wrong** | Backup production DB; test migration on staging first; rollback procedure documented |
| **Client breakage if API changes mid-phase** | Phase 2 change is breaking (no scope header); clients must adapt (simple removal) |
| **Scope logic hiding in unexpected places** | Grep for `BudgetScope`, `scope`, and `Scope` before each phase; code review checklist includes scope audit |
| **Performance regression (missing index)** | Run performance tests; verify query plans after migration; no new indexes needed (user filter is primary key) |

---

## Breaking Changes

### Phase 2: API Contract
- Clients must stop sending `X-BudgetScope` header (optional, but if sent, will be ignored)
- Responses no longer include `scope` field in DTOs
- Old clients expecting scope field will need update (graceful degradation via default or null coalescing)

### Phase 4: Database
- Rollback to pre-migration version requires dropping migration and re-creating scope columns with default value

---

## Non-Breaking Aspects

- Domain logic unaffected for users not working at the infrastructure layer
- Reports and analytics continue to work (they query by user, not scope)
- Authentication and authorization unchanged
- Roles and permissions unaffected
- Household ledger model strengthened (no personal scope to lose)

---

## Success Metrics

- [ ] **Scope enum removed** from codebase (no references in compiler)
- [ ] **Test coverage maintained** (unit + integration tests >85%)
- [ ] **API simplification** (DTOs smaller, fewer fields, cleaner schema)
- [ ] **Zero regressions** (all reports, transactions, budgets work as before)
- [ ] **Database schema clean** (no scope columns, migrations applied)
- [ ] **Code maintainability improved** (less branching logic, single mental model)
- [ ] **UI clarity** (no scope toggle; household ledger is the default and only option)
- [ ] **Kakeibo philosophy reinforced** (single household ledger is the only concept users see)

---

## Dependencies

- **Vic's architecture audit feedback** — scope removal aligns with Kakeibo philosophy decision (already captured in `.squad/decisions.md`)
- **No blocking external dependencies** — scope removal is self-contained
- **Suggested timing:** Complete before next major feature rollout to avoid scope-aware logic creeping in elsewhere

---

## References

- User directive (2026-04-06): "Budget scopes (Personal vs Shared) do not fit the Kakeibo household-ledger model."
- Kakeibo philosophy guide: `docs/FEATURE-TEMPLATE.md` (Kakeibo section)
- Current BudgetScope enum: `BudgetExperiment.Shared/Budgeting/BudgetScope.cs`
- Middleware: `BudgetExperiment.Api/Middleware/BudgetScopeMiddleware.cs`
- Client scope service: `BudgetExperiment.Client/Services/ScopeService.cs`
- Scope UI: `BudgetExperiment.Client/Components/Navigation/ScopeSwitcher.razor`

---

## Next Steps

1. **Phase 1 (UI):** Assign to Lucius; 2–3 days; low risk
2. **Phase 2 (API):** Follow Phase 1; 3–4 days; medium risk; breaking change communication
3. **Phase 3 (Domain/Application):** Follow Phase 2; 4–5 days; high risk; extensive refactoring
4. **Phase 4 (Database):** Follow Phase 3; 1–2 days; data-critical; staging validation required
5. **Documentation update:** After all phases; update API docs, architecture guide, and deployment runbooks

---

**Feature Owner:** User (Fortinbra)  
**Proposed By:** Alfred (Technical Writer)  
**Date:** 2026-04-10
