# Feature 165: NuGet Package Management Simplification

> **Status:** Archived (Done)

Final closeout: Feature 165 is complete based on repository truth for configuration, workflow, script removal, and documentation updates.

What is verified locally:

- `Directory.Build.props` sets `NuGetAudit=true`, `NuGetAuditMode=all`, and `NuGetAuditLevel=low`.
- `.github/workflows/dependabot-automerge.yml` exists for eligible patch-level Dependabot NuGet PRs.
- `.github/workflows/nuget-package-hygiene.yml` and `.github/workflows/nuget-upgrade-cycle-audit.yml` are absent.
- `scripts/operations/*.ps1` NuGet hygiene scripts referenced by this feature are absent.
- Maintainer guidance exists in `.github/prompts/nuget-upgrade.prompt.md` and `docs/operations/nuget-package-hygiene-monthly-runbook.md`.

Pending external-runtime evidence (not fully verifiable from local workspace alone):

- Live branch protection enforcement behavior.
- Runtime auto-merge execution in GitHub after required checks pass.

Canonical summary: `docs/archive/161-170-scope-ai-encryption.md` (Feature 165).

---

## Final Evidence Summary

### Repository Configuration and Workflow Evidence

- `Directory.Build.props` contains global audit enforcement:
  - `<NuGetAudit>true</NuGetAudit>`
  - `<NuGetAuditMode>all</NuGetAuditMode>`
  - `<NuGetAuditLevel>low</NuGetAuditLevel>`
  - `TreatWarningsAsErrors=true` remains enabled.
- `.github/workflows/ci.yml` restore gate is present as `dotnet restore BudgetExperiment.sln`.
- `.github/workflows/dependabot-automerge.yml` exists and only enables auto-merge for Dependabot NuGet patch updates.
- `.github/workflows/nuget-package-hygiene.yml` and `.github/workflows/nuget-upgrade-cycle-audit.yml` are absent.
- `scripts/operations` no longer contains NuGet hygiene scripts; only encryption backup/restore scripts remain.
- `docs/operations/nuget-package-hygiene-monthly-runbook.md` and `.github/prompts/nuget-upgrade.prompt.md` both match the Dependabot-first model.

### Validation Evidence

- Historical vulnerability probe evidence (2026-04-30):
  - Temporary package pin `Newtonsoft.Json 9.0.1` caused restore failure with `NU1903` (`GHSA-5crp-9r3c-p9vr`).
  - After reverting the probe, solution restore passed.
- Fresh workflow-equivalent local validation (2026-05-01):
  - `dotnet restore c:\ws\BudgetExperiment\BudgetExperiment.sln` -> pass.
  - `dotnet build c:\ws\BudgetExperiment\BudgetExperiment.sln --configuration Release --no-restore` -> pass.
  - `dotnet test c:\ws\BudgetExperiment\BudgetExperiment.sln --configuration Release --no-build --filter "FullyQualifiedName!~E2E&Category!=Performance"` -> pass.

## Acceptance Closure

- [x] Global NuGet audit properties are enforced in `Directory.Build.props`.
- [x] CI restore gate exists and aligns with workflow acceptance scope.
- [x] Legacy NuGet workflows and scripts are removed.
- [x] Dependabot grouping remains active (`aspnetcore`, `extensions`, `efcore`, `testing`).
- [x] Patch-only NuGet auto-merge workflow exists with manual-review boundaries for security/minor/major updates.
- [x] Maintainer guidance is present and updated.

## Explicit Verdict

Feature 165 is complete.

Known external-runtime checks that cannot be fully proven from local workspace alone remain explicitly out of scope for local proof:

- Live branch protection enforcement in GitHub settings.
- Runtime auto-merge execution behavior after required checks pass.
