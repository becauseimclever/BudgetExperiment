---
name: "explicit-interface-surface-hiding"
description: "Hide legacy members from a public class surface during phased refactors by moving them to explicit interface implementation"
domain: "backend"
confidence: "high"
source: "earned"
---

## Context
Use this when a phased migration needs the public/API surface cleaned up now, but dependent inner layers still temporarily require the legacy interface members to keep compiling.

## Patterns
### Move legacy members to explicit interface implementation
- Keep the interface contract for dependent layers.
- Remove the member from the concrete class public surface by implementing it explicitly.

### Freeze behavior to the safe default
- If the old value should no longer be user-controlled, return the single supported default from the explicit implementation.
- Ignore or no-op the setter/mutator so old entry points cannot change behavior indirectly.

### Test both surfaces
- Add reflection-based tests for the concrete class to prove the public members disappeared.
- Keep interface-level tests to prove dependent layers still get the expected fallback behavior.

## Examples
- `src/BudgetExperiment.Api/UserContext.cs` keeps `IUserContext.CurrentScope`/`SetScope` only as explicit members.
- `tests/BudgetExperiment.Api.Tests/Feature161Phase2ApiContractTests.cs` verifies `UserContext` no longer exposes public scope members.

## Anti-Patterns
- Leaving the legacy member public "just for now" and calling the phase complete.
- Removing the interface member too early and forcing unrelated downstream refactors into the same slice.
- Preserving mutability when the migration goal is to eliminate user control over the legacy concept.
