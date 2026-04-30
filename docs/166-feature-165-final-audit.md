# Feature 166: Feature 165 Final Audit

> **Status:** Review
> **Reviewed On:** 2026-04-30
> **Scope:** Final audit of Feature 165 on the current branch against `docs/165-nuget-package-management.md`

## Audit Scope

- Confirm implementation alignment with Feature 165 acceptance criteria.
- Review changed files for standards compliance and repository instruction alignment.
- Identify defects, regressions, policy violations, and missing validation.
- Inspect workflow acceptance scope across `.github/workflows/ci.yml`, `.github/workflows/docker-build-publish.yml`, `.github/workflows/release.yml`, and the new `.github/workflows/dependabot-automerge.yml`.

## Findings

### High

1. **Stable-only / latest StyleCop preview policy enforcement was removed without a replacement control.**
   - Evidence:
     - `Directory.Build.props` still pins `StyleCop.Analyzers` to a preview version.
     - `engineering-guide.instructions.md` still requires all packages to be stable except `StyleCop.Analyzers`, and still requires `StyleCop.Analyzers` to track the highest preview from nuget.org.
     - The deleted `scripts/operations/invoke-nuget-package-policy-gates.ps1` was the only concrete enforcement path for those rules in the reviewed change set.
     - No remaining workflow in the inspected workflow acceptance scope enforces pre-release allowlist compliance or verifies that `StyleCop.Analyzers` is pinned to the latest preview.
   - Impact:
     - A future PR can introduce unauthorized pre-release packages or leave `StyleCop.Analyzers` behind the required preview version without any CI failure.
     - This weakens an active repository policy while the updated instructions still claim that policy remains in force.
   - Files:
     - `Directory.Build.props`
     - `.github/instructions/engineering-guide.instructions.md`
     - `.github/workflows/ci.yml`
     - `scripts/operations/invoke-nuget-package-policy-gates.ps1` (deleted)

### Medium

1. **Operational documentation still instructs maintainers to run a deleted script and to retain artifacts that this feature removed.**
   - Evidence:
     - `docs/operations/nuget-package-hygiene-monthly-runbook.md` still tells maintainers to run `scripts/operations/invoke-nuget-package-policy-gates.ps1`.
     - The same runbook still requires “policy gate output” artifacts and states that the policy gate script must pass for prerelease and StyleCop preview checks.
   - Impact:
     - The documented operating procedure is now broken.
     - A maintainer following the runbook after this feature lands will hit a missing-file failure and will receive outdated compliance guidance.
   - Files:
     - `docs/operations/nuget-package-hygiene-monthly-runbook.md`

2. **The branch does not provide the workflow-equivalent evidence required to show the new restore gate was validated with a known vulnerable package.**
   - Evidence:
     - Feature 165 explicitly requires validation that `ci.yml` restore fails and reports the vulnerable package when a known-vulnerable package version is pinned, and that local `dotnet restore` reports the advisory clearly.
     - The reviewed branch contains no new test, no audit note, and no committed evidence showing that validation occurred.
   - Impact:
     - The main behavior change of the feature is plausible from configuration, but not fully demonstrated.
     - This remains a release risk because the acceptance criterion depends on actual restore behavior, not only on `Directory.Build.props` content.
   - Files:
     - `docs/165-nuget-package-management.md`
     - `Directory.Build.props`
     - `.github/workflows/ci.yml`

### Low

1. **`docs/ci-cd-deployment.md` currently fails markdown lint checks.**
   - Evidence:
     - Markdown diagnostics report `MD031` and `MD032` issues in the updated document.
   - Impact:
     - This is not a runtime defect, but it is a standards-compliance issue in a changed file.
   - Files:
     - `docs/ci-cd-deployment.md`

## Acceptance Criteria Coverage

| Acceptance Criterion | Status | Notes |
| --- | --- | --- |
| `Directory.Build.props` includes `NuGetAudit=true`, `NuGetAuditMode=all`, `NuGetAuditLevel=low` | Satisfied | Properties are present globally. |
| `ci.yml` restore step fails on vulnerable packages | Partial | Restore step exists; no branch evidence demonstrates the vulnerable-package failure case. |
| Local `dotnet restore` reports vulnerable package and advisory clearly | Partial | Configuration suggests this should happen, but no reviewed evidence proves it. |
| `TreatWarningsAsErrors` escalates `NU1901`-`NU1904` | Satisfied | `TreatWarningsAsErrors=true` remains global. |
| Delete `.github/workflows/nuget-package-hygiene.yml` | Satisfied | File is deleted. |
| Delete `.github/workflows/nuget-upgrade-cycle-audit.yml` | Satisfied | File is deleted. |
| Delete `scripts/operations/invoke-nuget-restore-vulnerability-gate.ps1` | Satisfied | File is deleted. |
| Delete `scripts/operations/invoke-nuget-package-policy-gates.ps1` | Satisfied | File is deleted. |
| Delete `scripts/operations/new-nuget-upgrade-feature-doc.ps1` | Satisfied | File is deleted. |
| No other workflow or script references deleted files | Satisfied | No remaining workflow or script references were found in the reviewed tree. |
| CI remains green after removal | Partial | Local workflow-equivalent validation was not provided in this audit. |
| Dependabot grouping retained for `aspnetcore`, `extensions`, `efcore`, `testing` | Satisfied | `.github/dependabot.yml` still defines these groups. |
| Dependabot PRs trigger CI automatically | Satisfied | `.github/workflows/ci.yml` runs on `pull_request`. |
| No custom workflow replaces `dotnet list --outdated` | Satisfied | No replacement workflow was introduced. |
| `dependabot-automerge.yml` enables patch-only NuGet auto-merge after checks | Satisfied | Workflow exists and gates on Dependabot metadata patch updates. |
| Minor and major version bumps are never auto-merged | Satisfied | Workflow only enables auto-merge for `version-update:semver-patch`. |
| Auto-merge behavior documented in `docs/ci-cd-deployment.md` | Satisfied | Documented in the new NuGet update section. |
| New `.github/prompts/nuget-upgrade.prompt.md` exists | Satisfied | Prompt exists. |
| Prompt covers Dependabot PR review, CI vulnerability failure, and manual upgrade scenarios | Satisfied | All three scenarios are included. |
| Prompt includes `dotnet restore`, `dotnet build`, `dotnet test` verification commands | Satisfied | Commands are present. |
| Prompt describes manual Dependabot update trigger path | Satisfied | Included in Scenario 2. |
| Prompt references Dependabot group names | Satisfied | Groups are listed. |
| `dotnet-devops-specialist.agent.md` notes Dependabot + audit property ownership | Satisfied | Scope updated as required. |
| `engineering-guide.instructions.md` notes global audit-property policy | Satisfied | Added. |
| `workflow-test-validation.instructions.md` notes restore-step vulnerability enforcement in `ci.yml` | Satisfied | Added. |

## Workflow Acceptance Scope Review

Inspected workflows:

- `.github/workflows/ci.yml`
- `.github/workflows/docker-build-publish.yml`
- `.github/workflows/release.yml`
- `.github/workflows/dependabot-automerge.yml`

Observed workflow acceptance scope after this feature:

- `ci.yml` remains the merge-gating path for restore, build, tests, and coverage.
- `docker-build-publish.yml` depends on `ci.yml` via `workflow_call` before image publish work begins.
- `release.yml` depends on `docker-build-publish.yml`, so release flow still inherits `ci.yml` validation.
- `dependabot-automerge.yml` only enables GitHub auto-merge; it does not replace CI.

Gap:

- The reviewed workflow set no longer contains any enforcement for the repository's stable-only package rule or for the “latest StyleCop preview” requirement.

## Residual Risks

- Local reproduction of the vulnerable-package restore failure was not run as part of this audit, so the key acceptance check remains unproven in this review.
- GitHub branch protection and required-check wiring cannot be verified locally from the workspace alone, so actual auto-merge behavior after CI is still partly dependent on repository settings.
- Security-update exclusion in `dependabot-automerge.yml` relies on PR title and label heuristics rather than an audited advisory-specific signal; if repository labeling conventions drift, manual-review protection may be weaker than intended.

## Recommended Remediation Tasks

- [ ] Reintroduce a lightweight enforcement mechanism for the stable-only rule and the “latest `StyleCop.Analyzers` preview” rule, or explicitly retire those repository policies from the instruction set and runbooks.
- [ ] Update `docs/operations/nuget-package-hygiene-monthly-runbook.md` so every command and artifact expectation matches the new Dependabot-first model.
- [ ] Capture workflow-equivalent validation evidence for the vulnerable-package failure path required by Feature 165.
- [ ] Fix markdown lint violations in `docs/ci-cd-deployment.md`.

## Recommendation

**No-go** for final sign-off in the current state.

The core vulnerability-gating configuration is in place and most acceptance criteria are satisfied, but the branch leaves an active package-governance policy unenforced, ships stale operational guidance, and lacks evidence for the feature's most important validation scenario.
