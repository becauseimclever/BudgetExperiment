# Feature 020: SOLID Principles Refactoring

📋 **Status**: In Progress (Phase 2 Complete)

## Overview

Several classes in the codebase have grown large and accumulated multiple responsibilities, violating SOLID principles (particularly SRP - Single Responsibility Principle). This feature documents a refactoring effort to decompose these classes into smaller, focused components while maintaining existing functionality.

## Target Classes

### 1. CalendarGridService (801 lines) - **Critical Priority**

**Location**: [src/BudgetExperiment.Application/Services/CalendarGridService.cs](../src/BudgetExperiment.Application/Services/CalendarGridService.cs)

**Current Responsibilities** (SRP violations):
1. Building calendar grid views (`GetCalendarGridAsync`)
2. Building day detail views (`GetDayDetailAsync`)
3. Building account transaction lists (`GetAccountTransactionListAsync`)
4. Projecting recurring transaction instances
5. Projecting recurring transfer instances
6. Auto-realizing past-due recurring items
7. Managing exception lookups for recurring items
8. Computing running balances

**Dependencies** (6 repositories - potential ISP concern):
- `ITransactionRepository`
- `IRecurringTransactionRepository`
- `IRecurringTransferRepository`
- `IAccountRepository`
- `IAppSettingsRepository`
- `IUnitOfWork`

**Proposed Decomposition**:

| New Service | Responsibility | Methods to Extract |
|-------------|----------------|-------------------|
| `IRecurringInstanceProjector` | Project recurring transaction instances for date ranges | `GetRecurringInstancesByDateAsync`, `GetRecurringInstancesForDateAsync` |
| `IRecurringTransferInstanceProjector` | Project recurring transfer instances for date ranges | `GetRecurringTransferInstancesByDateAsync`, `GetRecurringTransferInstancesForDateAsync` |
| `IAutoRealizeService` | Auto-realize past-due recurring items | `AutoRealizePastDueItemsIfEnabledAsync`, `AutoRealizeRecurringTransactionsAsync`, `AutoRealizeRecurringTransfersAsync` |
| `IRunningBalanceCalculator` | Calculate running balances for transaction lists | Balance computation logic |
| `CalendarGridService` (refactored) | Orchestrate calendar grid building | `GetCalendarGridAsync` (slim) |
| `DayDetailService` | Build day detail views | `GetDayDetailAsync` |
| `TransactionListService` | Build account transaction lists | `GetAccountTransactionListAsync` |

---

### 2. RecurringTransferService (527 lines) - **High Priority**

**Location**: [src/BudgetExperiment.Application/Services/RecurringTransferService.cs](../src/BudgetExperiment.Application/Services/RecurringTransferService.cs)

**Current Responsibilities** (SRP violations):
1. CRUD operations for recurring transfers
2. Lifecycle management (pause/resume/skip)
3. Instance projection
4. Instance modification (exceptions)
5. Instance realization (creating actual transfers)
6. Account name resolution

**Proposed Decomposition**:

| New Service | Responsibility | Methods to Extract |
|-------------|----------------|-------------------|
| `RecurringTransferService` (refactored) | CRUD + lifecycle | `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `PauseAsync`, `ResumeAsync`, `SkipNextAsync` |
| `IRecurringTransferInstanceService` | Instance projection and modification | `GetInstancesAsync`, `ModifyInstanceAsync`, `SkipInstanceAsync`, `GetProjectedInstancesAsync` |
| `IRecurringTransferRealizationService` | Realize instances into actual transfers | `RealizeInstanceAsync` |

---

### 3. RecurringTransactionService (448 lines) - **High Priority**

**Location**: [src/BudgetExperiment.Application/Services/RecurringTransactionService.cs](../src/BudgetExperiment.Application/Services/RecurringTransactionService.cs)

**Current Responsibilities** (similar pattern to RecurringTransferService):
1. CRUD operations
2. Lifecycle management
3. Instance projection
4. Instance modification
5. Instance realization

**Proposed Decomposition**: Mirror the RecurringTransferService refactoring pattern.

---

### 4. BudgetApiService (478 lines) - **Medium Priority**

**Location**: [src/BudgetExperiment.Client/Services/BudgetApiService.cs](../src/BudgetExperiment.Client/Services/BudgetApiService.cs)

**Current Responsibilities**:
- HTTP client wrapper for ALL API endpoints (accounts, transactions, recurring transactions, recurring transfers, transfers, calendar, settings)

**Note**: This is a thin client façade that follows the API structure. While large, it's primarily pass-through methods with consistent patterns. Consider splitting only if the file becomes unmanageable or if certain API domains need specialized handling.

**Proposed Decomposition** (if pursued):

| New Service | Responsibility |
|-------------|----------------|
| `IAccountApiService` | Account CRUD operations |
| `ITransactionApiService` | Transaction CRUD operations |
| `IRecurringTransactionApiService` | Recurring transaction operations |
| `IRecurringTransferApiService` | Recurring transfer operations |
| `ITransferApiService` | Transfer operations |
| `ICalendarApiService` | Calendar grid and day detail |
| `ISettingsApiService` | App settings operations |

---

### 5. IBudgetApiService Interface (323 lines) - **Medium Priority**

**Location**: [src/BudgetExperiment.Client/Services/IBudgetApiService.cs](../src/BudgetExperiment.Client/Services/IBudgetApiService.cs)

**ISP Violation**: Single interface with 40+ methods. Components depending on this interface depend on methods they don't use.

**Proposed Decomposition**: Split into focused interfaces matching the service decomposition above, then have `IBudgetApiService` inherit from all (or remove entirely).

---

## Implementation Phases

### Phase 1: Extract Recurring Instance Projectors ✅
- [x] Create `IRecurringInstanceProjector` interface in Domain
- [x] Create `RecurringInstanceProjector` implementation in Application
- [x] Create `IRecurringTransferInstanceProjector` interface in Domain
- [x] Create `RecurringTransferInstanceProjector` implementation in Application
- [x] Update `CalendarGridService` to use the new projectors
- [x] Update DI registration
- [x] Write unit tests for projectors (existing tests updated to mock projectors)

### Phase 2: Extract Auto-Realize Service ✅
- [x] Create `IAutoRealizeService` interface in Domain
- [x] Create `AutoRealizeService` implementation in Application
- [x] Move auto-realize logic from `CalendarGridService`
- [x] Update DI registration
- [x] Write unit tests (AutoRealizeServiceTests.cs created)

### Phase 3: Split Calendar Services
- [ ] Create `IDayDetailService` interface
- [ ] Create `DayDetailService` implementation
- [ ] Create `ITransactionListService` interface  
- [ ] Create `TransactionListService` implementation
- [ ] Slim down `CalendarGridService` to orchestration only
- [ ] Update controllers/API endpoints
- [ ] Update DI registration
- [ ] Write unit tests

### Phase 4: Refactor Recurring Transfer/Transaction Services
- [ ] Create `IRecurringTransferInstanceService` interface
- [ ] Create `IRecurringTransferRealizationService` interface
- [ ] Create implementations and move methods
- [ ] Repeat for `RecurringTransactionService`
- [ ] Update DI registration
- [ ] Write unit tests

### Phase 5: Client API Service Split (Optional)
- [ ] Evaluate if split is warranted based on complexity
- [ ] If yes, create focused API service interfaces
- [ ] Create implementations
- [ ] Update DI registration in Client

---

## SOLID Principles Reference

### Single Responsibility Principle (SRP)
> A class should have only one reason to change.

**Target**: Each service should have ONE primary responsibility. If you can describe what a class does with "AND", it likely violates SRP.

### Open/Closed Principle (OCP)
> Software entities should be open for extension but closed for modification.

**Application**: New projector implementations can be added without modifying existing code. Use strategy/decorator patterns where appropriate.

### Liskov Substitution Principle (LSP)
> Objects of a superclass should be replaceable with objects of its subclasses without breaking the application.

**Application**: All interface implementations must fulfill the contract completely. No `NotImplementedException` in production code.

### Interface Segregation Principle (ISP)
> Clients should not be forced to depend on interfaces they do not use.

**Target**: Split `IBudgetApiService` and create focused repository interfaces. Components should depend on minimal interfaces.

### Dependency Inversion Principle (DIP)
> High-level modules should not depend on low-level modules. Both should depend on abstractions.

**Current State**: ✅ Already following - Application layer depends on Domain interfaces, Infrastructure provides implementations.

---

## Success Criteria

1. **No service exceeds 300 lines** (per copilot-instructions.md guideline)
2. **Each service has a single, clear responsibility**
3. **All existing tests continue to pass**
4. **New services have dedicated unit tests**
5. **No functionality regression**
6. **Constructor injection doesn't exceed 4-5 dependencies per service**

---

## Risk Mitigation

- **Incremental refactoring**: Each phase is independently deployable
- **Test coverage**: Write tests for extracted services before removing original code
- **Interface-first**: Define interfaces in Domain before implementation
- **Feature flags**: Consider flags if phased rollout needed (unlikely for internal refactoring)

---

## Notes

- Migration-generated files (e.g., `*.Designer.cs`, `ModelSnapshot.cs`) are auto-generated and excluded from refactoring
- Domain entities (`RecurringTransfer.cs`, `Transaction.cs`, etc.) at 200-300 lines are acceptable given they contain business logic and validation
- Focus refactoring effort on Application layer services first, as these have the most impact on maintainability
