# Gordon — History

## Project Context

- **Project:** BudgetExperiment
- **User:** Fortinbra
- **Stack:** .NET 10, ASP.NET Core, Blazor WebAssembly, EF Core + Npgsql, xUnit + Shouldly, StyleCop
- **Joined:** 2026-04-18
- **Reason:** Added as a fourth backend implementer to produce a lockout-safe rollback revision for Feature 161 Phase 2.

## Learnings

- Feature 161 Phase 2 must stay at the API/contracts/user-context layer; Application and Infrastructure drift belongs to Phase 3.
- Multiple rejected revision authors remain locked out of the artifact; the next revision must come from a different backend implementer again.
- Feature 161 Phase 3 Client cleanup: `ScopeService` was registered as a singleton; all 8 ViewModels injected it. Deleting the service requires removing the DI param from constructors, dropping `IDisposable` entirely where it was the only disposable resource, and removing `ViewModel.Dispose()` calls in Razor page `@code` blocks.
- Three ViewModels (Accounts, Budget, Transactions) had `IDisposable` solely for the scope unsubscription — removing IDisposable also required updating Razor pages that called `ViewModel.Dispose()` directly.
- `ScopeMessageHandler` had a stub constructor taking `ScopeService` (already discarded with `_ = scopeService`); removing ScopeService required dropping that constructor entirely — the handler still strips the `X-Budget-Scope` header defensively.
- `BudgetScope` in ViewModel files without an explicit `using BudgetExperiment.Shared.Budgeting;` was covered by `GlobalUsings.cs`; files with the explicit using needed it removed.
- Razor pages in `Pages/Reports/` had `@implements IDisposable` solely for scope event unsubscription — all four report pages had their `Dispose()` method and `@implements IDisposable` removed entirely.
- Calendar.razor kept `@implements IDisposable` because `ChatContext.ClearContext()` remains in `Dispose()`.
- Phase 2 CI workflow integration required 5 specific fixes: (1) state file caching via GitHub Actions cache/restore, (2) explicit Cobertura.xml existence check, (3) `-StatePath` parameter passed to validation script, (4) state save on push events (success OR failure), (5) `shell: pwsh` directive for PowerShell context.
- Barbara identified Infrastructure module missing from coverage report — NOT a CI workflow issue; upstream collection/runsettings problem owned by Tim or Lucius.
- State file cache strategy: primary key = branch+SHA, fallback #1 = same branch any commit, fallback #2 = main branch history (for new feature branches).
- PR comment posting made conditional on validation output existing (`steps.module_coverage.outputs.result != ''`) to avoid errors when validation step crashes.
