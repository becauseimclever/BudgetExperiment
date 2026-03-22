# Feature 123: Backend Code Quality Cleanup
> **Status:** Pending

## Overview

A code quality review on `feature/code-quality-review` surfaced four categories of backend quality issues: an exception handler that uses fragile string matching instead of the existing `DomainException.ExceptionType` enum, three redundant dual-registration DI entries, six methods with excessive nesting depth, and a `DateTime.Now` usage in a Blazor component that violates the project's UTC-everywhere rule. None are blockers, but together they accumulate maintenance debt and introduce subtle risk. This work addresses all four categories.

## Problem Statement

### Current State

1. **`ExceptionHandlingMiddleware`** maps `DomainException` to HTTP status codes by calling `.Message.Contains("not found")`. The domain already has a `DomainException.ExceptionType` enum. String matching is fragile: it breaks on message rewordings, is case-sensitive by default, and obscures intent.

2. **Three redundant DI registrations** register each service twice — once as an interface and once as a concrete type — with a comment referencing "backward compatibility". No consumer in the solution references the concrete registration. Both entries resolve to the same implementation, so the duplicate is pure noise that misleads future readers about the DI contract.

3. **Six methods with 3+ nesting levels** are harder to read, test, and modify safely:
   - `TransactionListService` — recurring instance builder methods
   - `ImportExecuteRequestValidator`
   - `RuleSuggestionResponseParser`
   - `ImportRowProcessor.DetermineCategory`
   - `LocationParserService`

4. **`Reconciliation.razor`** uses `DateTime.Now` instead of `DateTime.UtcNow`. All `DateTime` values in this application must be UTC (engineering guidelines §30).

### Target State

- `ExceptionHandlingMiddleware` switches on `DomainException.ExceptionType` — zero string matching.
- Redundant DI registrations are removed; each service is registered once, by interface only.
- The six identified methods are refactored to ≤ 2 nesting levels using guard clauses, extracted private methods, or helper types.
- `Reconciliation.razor` uses `DateTime.UtcNow`.
- All existing tests pass; no behavioural changes are introduced.

---

## Acceptance Criteria

### Exception Handler

- [ ] `ExceptionHandlingMiddleware` contains no `.Message.Contains(...)` calls.
- [ ] HTTP status code mapping is driven exclusively by `DomainException.ExceptionType` (or equivalent typed exception switch) .
- [ ] All existing `DomainException` types map to the same status codes they did before.
- [ ] Unit tests for `ExceptionHandlingMiddleware` cover every `ExceptionType` case.

### DI Cleanup

- [ ] The three duplicate service registrations are identified and the concrete-type registrations are removed.
- [ ] Existing consumers (if any exist) continue to resolve their dependencies correctly.
- [ ] A grep of the solution confirms no remaining `// backward compatibility` DI comments.

### Method Nesting

- [ ] `TransactionListService` recurring instance methods: max nesting ≤ 2.
- [ ] `ImportExecuteRequestValidator`: max nesting ≤ 2.
- [ ] `RuleSuggestionResponseParser`: max nesting ≤ 2.
- [ ] `ImportRowProcessor.DetermineCategory`: max nesting ≤ 2.
- [ ] `LocationParserService` (identified method(s)): max nesting ≤ 2.
- [ ] Refactored methods preserve all existing unit test coverage and pass without modification.

### DateTime UTC

- [ ] `Reconciliation.razor` uses `DateTime.UtcNow` in every location previously using `DateTime.Now`.
- [ ] A grep of the Client project for `DateTime.Now` (excluding comments and string literals) returns zero results after this change.

---

## Technical Design

### Exception Handler Refactor

Replace the current pattern:

```csharp
// Before — fragile string matching
if (ex is DomainException domEx && domEx.Message.Contains("not found"))
    return StatusCodes.Status404NotFound;
```

With a switch on the enum:

```csharp
// After — typed dispatch
if (ex is DomainException domEx)
{
    return domEx.ExceptionType switch
    {
        DomainExceptionType.NotFound       => StatusCodes.Status404NotFound,
        DomainExceptionType.Conflict       => StatusCodes.Status409Conflict,
        DomainExceptionType.Validation     => StatusCodes.Status422UnprocessableEntity,
        _                                  => StatusCodes.Status400BadRequest,
    };
}
```

Adjust the `DomainExceptionType` enum if any required cases are missing.

### DI Cleanup

Locate registrations of the form:

```csharp
services.AddScoped<IMyService, MyService>();
services.AddScoped<MyService>(); // backward compatibility
```

Remove the second line. Verify with a DI resolution test or manual inspection that no constructor requests the concrete type directly.

### Nesting Reduction Techniques

Use these standard approaches (choose per context):

- **Guard clauses** — invert conditional and return/continue early to avoid one nesting level.
- **Extract method** — move an inner block to a well-named private method.
- **LINQ / pattern decomposition** — replace nested `if`/`foreach` with composable expressions where it improves clarity.

Do not reduce nesting in ways that obscure intent — prefer clarity over cleverness.

### DateTime Fix

```razor
@* Before *@
var today = DateTime.Now;

@* After *@
var today = DateTime.UtcNow;
```

If the date is displayed to the user, convert to local time via `CultureService.CurrentTimeZone` (per engineering guidelines §30 and §38).

---

## Implementation Plan

### Phase 1: Exception Handler

**Objective:** Replace string-matching exception mapping with type-safe enum dispatch.

**Tasks:**
- [ ] Audit `DomainExceptionType` enum — add any missing cases needed for current status code mapping
- [ ] Rewrite `ExceptionHandlingMiddleware` status code logic to switch on `ExceptionType`
- [ ] Write/update unit tests for the middleware covering each `ExceptionType` case
- [ ] Run tests — confirm green

**Commit:**
```bash
git commit -m "refactor(api): replace string matching in ExceptionHandlingMiddleware with ExceptionType switch

- Remove .Message.Contains() calls
- Switch on DomainException.ExceptionType for status code mapping
- Unit tests cover all ExceptionType cases

Refs: #123"
```

---

### Phase 2: DI Cleanup

**Objective:** Remove the three redundant concrete-type service registrations.

**Tasks:**
- [ ] Locate all `// backward compatibility` DI comments in the solution
- [ ] Confirm no constructor or service locator references the concrete type directly
- [ ] Remove the redundant `AddScoped<ConcreteType>()` registrations
- [ ] Run full test suite — confirm no resolution failures

**Commit:**
```bash
git commit -m "refactor(api): remove redundant DI registrations

- Three services were registered as both interface and concrete type
- Concrete-type registrations removed; interface registrations remain
- No consumers reference concrete types

Refs: #123"
```

---

### Phase 3: Nesting Reduction

**Objective:** Refactor the six methods with 3+ nesting levels to ≤ 2 levels.

**Tasks:**
- [ ] `TransactionListService` recurring methods — apply guard clauses / extract helper
- [ ] `ImportExecuteRequestValidator` — extract validation branches into named private methods
- [ ] `RuleSuggestionResponseParser` — flatten nested parsing logic
- [ ] `ImportRowProcessor.DetermineCategory` — guard-clause early returns for each condition
- [ ] `LocationParserService` — extract inner logic blocks
- [ ] Confirm all existing unit tests pass unchanged after each refactor

**Commit:**
```bash
git commit -m "refactor(app): reduce method nesting depth in five service classes

- TransactionListService, ImportExecuteRequestValidator, RuleSuggestionResponseParser,
  ImportRowProcessor, LocationParserService all now <= 2 nesting levels
- Guard clauses and extracted private methods; no behaviour changes
- All unit tests pass

Refs: #123"
```

---

### Phase 4: DateTime UTC Fix

**Objective:** Replace `DateTime.Now` with `DateTime.UtcNow` in `Reconciliation.razor`.

**Tasks:**
- [ ] Find all `DateTime.Now` uses in `Reconciliation.razor`
- [ ] Replace with `DateTime.UtcNow`
- [ ] If displayed to user, ensure conversion to local via `CultureService` (check existing pattern in other components)
- [ ] Grep Client project for remaining `DateTime.Now` references and fix any others found
- [ ] Run Client tests — confirm green

**Commit:**
```bash
git commit -m "fix(client): replace DateTime.Now with DateTime.UtcNow in Reconciliation

- All DateTime values must be UTC per project conventions
- Convert to local time for display using CultureService

Refs: #123"
```
