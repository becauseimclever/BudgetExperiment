# Feature 149: Extract ICalendarService and IAccountService Interfaces (DIP Fix)

> **Status:** Proposed  
> **Severity:** 🟠 High — F-002 + F-003 (combined)  
> **Audit Source:** `docs/audit/2026-04-09-full-principle-audit.md`  
> **Closes:** Decision #2 (2026-03-22) — remaining concrete-injecting controllers

---

## Overview

Two controllers still inject concrete service types, violating the Dependency Inversion Principle and Decision #2. `CalendarController` accepts `CalendarService` directly; `AccountsController` accepts `AccountService` directly. Neither service has an extracted interface. This feature extracts `ICalendarService` and `IAccountService`, updates both controllers to depend on those abstractions, updates DI registrations, and adds API-layer tests that mock the new interfaces.

This is a pure structural refactor. No behavior changes, no new endpoints, no database changes.

---

## Problem Statement

### Current State

- `CalendarController` constructor: `CalendarService calendarService` (concrete type). No `ICalendarService` exists.
- `AccountsController` constructor: `AccountService service` (concrete type). No `IAccountService` exists.
- Both services are registered in DI as concrete types.
- Neither service can be mocked in API tests — tests must use a real service implementation.
- Decision #2 (2026-03-22) identified this class of violation and mandated interface injection for all controllers. Two controllers were missed.

### Target State

- `ICalendarService` interface extracted in `BudgetExperiment.Application`, covering all public methods consumed by `CalendarController`.
- `IAccountService` interface extracted in `BudgetExperiment.Application`, covering all public methods consumed by `AccountsController`.
- Both controllers updated to inject the interface type.
- DI registrations updated: `services.AddScoped<ICalendarService, CalendarService>()` and `services.AddScoped<IAccountService, AccountService>()`.
- API tests using `WebApplicationFactory` can mock `ICalendarService` and `IAccountService` via test service overrides.

---

## User Stories

### US-149-001: Controller Testability via Mocked Calendar Service

**As a** developer writing API tests for `CalendarController`  
**I want** the controller to depend on `ICalendarService`  
**So that** I can substitute a test double and verify controller behavior in isolation

**Acceptance Criteria:**
- [ ] `ICalendarService` interface exists in `BudgetExperiment.Application`
- [ ] `CalendarController` injects `ICalendarService` (not `CalendarService`)
- [ ] DI wires `ICalendarService → CalendarService`
- [ ] At least two API tests mock `ICalendarService` to control responses

### US-149-002: Controller Testability via Mocked Account Service

**As a** developer writing API tests for `AccountsController`  
**I want** the controller to depend on `IAccountService`  
**So that** I can substitute a test double and verify controller behavior in isolation

**Acceptance Criteria:**
- [ ] `IAccountService` interface exists in `BudgetExperiment.Application`
- [ ] `AccountsController` injects `IAccountService` (not `AccountService`)
- [ ] DI wires `IAccountService → AccountService`
- [ ] At least two API tests mock `IAccountService` to control responses

---

## Technical Design

### ICalendarService Interface

Create `src/BudgetExperiment.Application/Calendar/ICalendarService.cs`.

Inspect `CalendarController` to enumerate all `_calendarService.` call sites — every public method called from the controller belongs on the interface. The interface must be a faithful projection of the controller's usage (ISP: only what the controller uses, not the entire `CalendarService` public surface).

```csharp
// Example shape — confirm against actual CalendarController usage
public interface ICalendarService
{
    Task<CalendarMonthDto> GetMonthAsync(int year, int month, CancellationToken cancellationToken = default);
    // ... add any other methods called in CalendarController
}
```

### IAccountService Interface

Create `src/BudgetExperiment.Application/Accounts/IAccountService.cs`.

Same pattern: enumerate `_service.` call sites in `AccountsController` and surface only those methods.

```csharp
// Example shape — confirm against actual AccountsController usage
public interface IAccountService
{
    Task<IReadOnlyList<AccountDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AccountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AccountDto> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken = default);
    Task<AccountDto> UpdateAsync(Guid id, UpdateAccountRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    // ... add all methods called by AccountsController
}
```

### DI Registration Changes

In `BudgetExperiment.Api/DependencyInjection.cs` (or `Program.cs`):

```csharp
// Before
services.AddScoped<CalendarService>();
services.AddScoped<AccountService>();

// After
services.AddScoped<ICalendarService, CalendarService>();
services.AddScoped<IAccountService, AccountService>();
```

### Controller Changes

```csharp
// CalendarController — before
private readonly CalendarService _calendarService;
public CalendarController(CalendarService calendarService, ...)

// After
private readonly ICalendarService _calendarService;
public CalendarController(ICalendarService calendarService, ...)
```

```csharp
// AccountsController — before
private readonly AccountService _service;
public AccountsController(AccountService service)

// After
private readonly IAccountService _service;
public AccountsController(IAccountService service)
```

---

## Implementation Plan

### Phase 1: Extract ICalendarService

**Tasks:**
- [ ] Read `CalendarController.cs` — list all `_calendarService.*` call sites
- [ ] Create `src/BudgetExperiment.Application/Calendar/ICalendarService.cs` with all required method signatures
- [ ] Add XML doc comments to interface public methods
- [ ] Make `CalendarService` implement `ICalendarService` (add `: ICalendarService` to class declaration)
- [ ] Update `CalendarController` constructor: replace `CalendarService` with `ICalendarService`
- [ ] Update DI registration in application/API layer
- [ ] `dotnet build` — zero errors

**Commit:**
```
refactor(app): extract ICalendarService, update CalendarController to inject abstraction

CalendarController no longer depends on concrete CalendarService.
DI: ICalendarService → CalendarService.

Closes F-002 (2026-04-09 audit), Decision #2 (2026-03-22)
Refs: §7 Engineering Guide (DIP)
```

---

### Phase 2: Extract IAccountService

**Tasks:**
- [ ] Read `AccountsController.cs` — list all `_service.*` call sites
- [ ] Create `src/BudgetExperiment.Application/Accounts/IAccountService.cs` with all required method signatures
- [ ] Add XML doc comments
- [ ] Make `AccountService` implement `IAccountService`
- [ ] Update `AccountsController` constructor
- [ ] Update DI registration
- [ ] `dotnet build` — zero errors

**Commit:**
```
refactor(app): extract IAccountService, update AccountsController to inject abstraction

AccountsController no longer depends on concrete AccountService.
DI: IAccountService → AccountService.

Closes F-003 (2026-04-09 audit), Decision #2 (2026-03-22)
Refs: §7 Engineering Guide (DIP)
```

---

### Phase 3: API Tests Using Mocked Interfaces

**Tasks:**
- [ ] In `BudgetExperiment.Api.Tests`, add `CalendarControllerTests.cs`:
  - Use `WebApplicationFactory` with `services.AddScoped<ICalendarService>(_ => mockCalendarService)`
  - `CalendarController_GetMonth_ValidYearMonth_Returns200WithDto`
  - `CalendarController_GetMonth_InvalidMonth_Returns400`
- [ ] In `BudgetExperiment.Api.Tests`, add or extend `AccountsControllerTests.cs`:
  - Use `WebApplicationFactory` with `services.AddScoped<IAccountService>(_ => mockAccountService)`
  - `AccountsController_GetAll_Returns200WithList`
  - `AccountsController_GetById_NotFound_Returns404`
- [ ] Run all API tests: `dotnet test tests/BudgetExperiment.Api.Tests/ --filter "Category!=Performance"` — green

**Commit:**
```
test(api): add controller tests using mocked ICalendarService and IAccountService

API layer tests can now substitute test doubles for both interfaces.

Refs: F-002, F-003, §15 Engineering Guide
```

---

### Phase 4: Verification

**Tasks:**
- [ ] Run full suite: `dotnet test --filter "Category!=Performance"` — all green
- [ ] Run `dotnet format` — no style issues
- [ ] Confirm `decisions.md` Decision #2 is now fully resolved (all controllers use interface injection)

---

## Testing Strategy

### API Tests (WebApplicationFactory)

- `CalendarController_GetMonth_ValidYearMonth_Returns200WithDto`
- `CalendarController_GetMonth_InvalidMonth_Returns400`
- `AccountsController_GetAll_Returns200WithList`
- `AccountsController_GetById_ValidId_Returns200WithDto`
- `AccountsController_GetById_NotFoundId_Returns404`
- `AccountsController_Create_ValidRequest_Returns201WithLocation`

### No Domain or Infrastructure Tests

This refactor touches only Application interfaces and API controllers. Domain and Infrastructure layers are unchanged.

---

## Migration Notes

No database changes. No API contract changes. Existing API consumers are unaffected.

---

## References

- [2026-04-09 Full Principle Audit — F-002](../docs/audit/2026-04-09-full-principle-audit.md#f-002-high--dip-violation-calendarcontroller-injects-concrete-calendarservice)
- [2026-04-09 Full Principle Audit — F-003](../docs/audit/2026-04-09-full-principle-audit.md#f-003-high--dip-violation-accountscontroller-injects-concrete-accountservice)
- Engineering Guide §7 (DIP — "Higher layers depend on abstractions")
- [Squad Decisions — Decision #2 (2026-03-22)](../.squad/decisions.md)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-04-09 | Initial spec from Vic audit F-002 + F-003, closes Decision #2 | Alfred (Lead) |
