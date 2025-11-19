# BudgetExperiment Architecture & Implementation Plan

## Purpose
Budget tracking & forecasting system that allocates portions of each paycheck to future bills (including irregular cadences) to ensure on‑time payment and surface impending shortfalls early.

## High-Level Goals

- Configurable repeating paychecks (bi-weekly, semi-monthly, monthly, custom interval).
- Bills with varied recurrence (monthly, bi-weekly, quarterly, annual, etc.).
- Automatic sinking/escrow allocation across pay periods.
- Precise per-paycheck set‑aside amounts; detect underfunding before due dates.
- RESTful API + OpenAPI + Scalar explorer.
- Blazor WebAssembly client (FluentUI-Blazor) for projections & alerts.
- Clean Architecture, SOLID, strict TDD.

## Domain Concepts

### Value Objects

- MoneyValue (amount, currency; arithmetic, non-negative guards where applicable, banker's rounding for intermediate calc, cent precision store).
- RecurrencePatternValue (Kind enum: BiWeekly, Weekly, Monthly, SemiMonthly, Quarterly, Annual, CustomInterval; AnchorDate; IntervalLength for custom; validation invariants).
- DateRangeValue (Start, End; Contains, Overlaps, Length).
- DueDateValue (wrap DateOnly; future validation optional).
- PercentageValue (0–100; scaling helper).
- Identifier wrappers: BudgetId, BillId, PaycheckScheduleId, PayPeriodId, AllocationId.

### Entities / Aggregates

- Budget (Id, StartDate, HorizonMonths default, Bills[], PaycheckSchedules[]).
- PaycheckSchedule (Id, RecurrencePattern, NetAmount).
- Bill (Id, Name, Amount, RecurrencePattern, AnchorDueDate, Category?, Priority?).
- PayPeriod (Id, PeriodStart, PeriodEnd, PayDate, NetAmount).
- Allocation (Id, BillId, PayPeriodId, Amount, Status: Planned | Committed | ShortfallFlagged).
- SinkingFundSnapshot (BillId, Date, AccruedAmount, RequiredAmount).
- Alert (Id, BillId?, Severity (Info|Warn|Critical), Reason enum (Shortfall, Late, ScheduleGap), Message, TriggerDate).

### Domain Services

- PayPeriodGeneratorService
- BillOccurrenceService (expand recurrence to concrete due instances)
- AllocationPlannerService (core distribution)
- ShortfallDetectionService
- ProjectionService (orchestrates, returns projection result aggregate)

## Allocation Algorithm (Summary)

1. Determine planning horizon (default = Budget.StartDate + HorizonMonths, ensure at least one full recurrence cycle for each bill; may cap at 12 months initially).
2. Generate pay periods within horizon for each schedule.
3. Expand bills into DueInstances (BillId, DueDate, Amount).
4. For each DueInstance, identify eligible pay periods between previous due (exclusive) and current due (inclusive) whose PayDate <= DueDate.
5. Distribute remaining needed amount across remaining eligible periods: RequiredPerPeriod = RoundUpCents(Remaining / RemainingPeriods). Allocate sequentially; adjust last allocation for rounding drift.
6. Track cumulative sinking fund; flag shortfall if at any point cumulative < pro‑rata expectation when a pay period passes and remaining periods * maximum allocatable < remaining need.
7. After passing DueDate, if fund < Amount → Late alert.

Rounding: Keep internal decimal math, only round at allocation assignment; final sum per bill must equal bill amount for each cycle.

## Edge Cases

- Bill due before first paycheck (instant shortfall alert).
- Annual bill with only one contributing period left (100% allocation that period).
- 3-paycheck month (bi-weekly) – correct distribution, no overfunding.
- Semi-monthly vs monthly interaction; ensure date boundary logic.
- Custom interval not aligning with horizon end.

## Persistence Strategy

Initial: Persist Budgets, Bills, PaycheckSchedules. Compute PayPeriods and Allocations on demand (projection). Later optimization: materialize snapshots for performance/audit.

Tables (initial):

- Budgets(Id, Name, StartDate, HorizonMonths)
- PaycheckSchedules(Id, BudgetId FK, RecurrenceKind, IntervalLength, AnchorDate, NetAmount)
- Bills(Id, BudgetId FK, Name, Amount, RecurrenceKind, IntervalLength, AnchorDueDate, Category, Priority)

## API (Initial Endpoints)

- POST /api/v1/budgets (create budget w/ schedules + bills)
- GET /api/v1/budgets/{id}
- GET /api/v1/budgets/{id}/projection?horizonMonths=12
- (Later) PATCH endpoints for updates, alerts retrieval, etc.

Projection Response Shape (draft):

```json
{
  "payPeriods": [{ "id": "...", "payDate": "2025-01-03", "netAmount": 2500.00, "allocations": [{"billId":"...","amount":125.50}] }],
  "bills": [{ "id":"...", "name":"Rent", "amount":1500.00, "nextDueDate":"2025-02-01", "recurrence":"Monthly"}],
  "sinkingFunds": [{ "billId":"...", "date":"2025-01-03", "accrued":125.50, "required":1500.00 }],
  "alerts": [{ "severity":"Warn", "reason":"Shortfall", "billId":"...", "message":"Rent projected shortfall in 1 period", "triggerDate":"2025-01-17" }]
}
```

## Testing Strategy

Phase 1: Value object & recurrence tests (MoneyValue, RecurrencePattern).
Phase 2: PayPeriodGenerator tests (bi-weekly, monthly, semi-monthly edge cases around month lengths, leap year).
Phase 3: AllocationPlanner tests (monthly + bi-weekly, annual, rounding adjustments, shortfall).
Phase 4: Projection integration test (compose services pure in-memory).
Phase 5: Repository + EF Core tests (Testcontainers Postgres) verifying persistence invariants.
Phase 6: API endpoint tests (WebApplicationFactory) for projection, including validation errors.
Phase 7: Basic UI component tests (bUnit) for projection grid.

## Tooling & Quality Gates

- StyleCop + analyzers (warnings as errors).
- xUnit + Shouldly (no FluentAssertions, no AutoFixture).
- `dotnet format` pre-commit.
- Git branching: `feature/*` with tests included.

## Incremental Delivery Order

1. Scaffold solution + empty projects/tests.
2. Implement MoneyValue + tests.
3. Implement RecurrencePatternValue + tests.
4. PayPeriodGeneratorService + tests.
5. BillOccurrenceService + tests.
6. AllocationPlannerService + tests.
7. ProjectionService + tests.
8. Infrastructure layer (DbContext + migrations + repositories) + integration tests.
9. API + endpoints + OpenAPI/Scalar.
10. Client shell + projection view.
11. Refinements (alerts UI, performance, docs).

## Open Questions (To Revisit)

- Multi-schedule support complexity in allocation ordering (aggregate or treat separately then merge?).
- Currency multi-support (future; keep MoneyValue extensible).
- Persistence of allocations (materialize vs compute ephemeral).

## Decisions Log (Initial)

- Allocation computed on demand first iteration.
- Banker's rounding internal; final cent adjustments on last period.
- StyleCop enforced; no blanket rule suppressions.
- Sinking fund snapshots not persisted initially.

---
Document updated as architecture evolves. Keep lean and actionable.

## CSV Import Endpoints (Implemented)

- POST `/api/v1/csv-import` — legacy immediate import
- POST `/api/v1/csv-import/preview` — multipart (file, bankType) returns preview rows with duplicate flags
- POST `/api/v1/csv-import/commit` — JSON body of edited rows; respects `forceImport` for duplicates

Client performs Preview → Edit → Commit in a dialog hosted by the API. Dedup thresholds configurable via `CsvImportDeduplication` in API appsettings.
