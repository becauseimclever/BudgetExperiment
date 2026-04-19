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
