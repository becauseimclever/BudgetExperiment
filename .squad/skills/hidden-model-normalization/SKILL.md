---
name: "hidden-model-normalization"
description: "Normalize stale incoming model state when a Blazor form stops exposing a choice in the UI"
domain: "testing"
confidence: "high"
source: "earned"
---

## Context
Use this when a Blazor component removes or hides a user-editable field, but existing DTOs or persisted models may still arrive with old values. The regression risk is hidden state surviving a now-invisible form path and being resubmitted unchanged.

## Patterns
### Normalize in `OnParametersSet()`
- Coerce the incoming parameter model each time parameters are applied, not just in `OnInitialized()`.
- This protects both first render and later parameter swaps/rebinds.

### Pin to the only valid value
- If the UI no longer offers a choice, set the model to the single supported value explicitly.
- Do not rely on “blank defaults” alone; legacy persisted values can be non-blank and still invalid for the new slice.

### Keep a regression test on the hidden path
- Add or preserve a bUnit test that renders the component with a legacy value (for example `"Personal"`) and asserts the model is normalized before submit.
- The test should fail if someone only removes the visible control but forgets the underlying model coercion.

## Examples
- `src/BudgetExperiment.Client/Components/Forms/AccountForm.razor` sets `Model.Scope = "Shared";` in `OnParametersSet()`.
- `tests/BudgetExperiment.Client.Tests/Components/Forms/AccountFormTests.cs` verifies `Render_PersonalScope_DefaultsToShared`.

## Anti-Patterns
- Hiding the UI control while leaving the old DTO value untouched.
- Normalizing only null/blank values and forgetting persisted legacy values.
- Performing the coercion only at submit time, after the component has already rendered misleading state.
