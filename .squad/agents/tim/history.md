# Tim — History

## Project Context

- **Project:** BudgetExperiment
- **User:** Fortinbra
- **Stack:** .NET 10, ASP.NET Core, Blazor WebAssembly, EF Core + Npgsql, xUnit + Shouldly, StyleCop
- **Joined:** 2026-04-18
- **Reason:** Added as a second backend implementer to handle lockout-safe revisions and parallel backend follow-up work.

## Learnings

- Feature 161 removes BudgetScope in phases; Phase 1 is complete, and Phase 2 is scoped to API/contracts/user context only.
- Reviewer lockout is strict: when a backend slice is rejected, the original backend author cannot produce the next revision.
- From clean HEAD, the safe Phase 2 follow-up is to hide scope on `BudgetExperiment.Api.UserContext` via explicit interface implementation while stopping the client header; removing `IUserContext` members would spill into Phase 3 consumers.
