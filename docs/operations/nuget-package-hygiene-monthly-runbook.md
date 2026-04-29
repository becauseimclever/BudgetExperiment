# NuGet Package Hygiene Monthly Runbook

**Purpose:** Define the minimum monthly operating procedure for NuGet package hygiene in BudgetExperiment.

**Applies to:** Monthly scheduled run and any manual or Dependabot-triggered hygiene run that needs triage, remediation, or rollback.

---

## 1. Cadence

- Minimum cadence: once per calendar month.
- Scheduled workflow baseline: first day of month.
- Additional runs are required after major dependency merges, failed audit gates, or critical vulnerability advisories.

---

## 2. Ownership and Accountability

| Role | Accountability |
|------|----------------|
| Release Operations Owner | Accountable for monthly execution, triage completion, and runbook evidence quality. |
| Operations Reviewer (secondary approver) | Reviews rollback decisions and confirms smoke validation evidence before closure. |
| Feature Owner for active upgrade cycle | Delivers package update PRs and tracks remediation tasks to closure. |

**Single-thread accountability rule:** One named Release Operations Owner must be assigned for each monthly cycle before triage begins.

---

## 3. SLA Targets for Vulnerability Remediation

| Severity | Target Remediation SLA |
|----------|-------------------------|
| Critical (NU1904 / high-impact advisory) | Begin remediation same day; merged fix or approved rollback within 24 hours. |
| High (NU1903) | Merged fix or approved rollback within 3 calendar days. |
| Moderate (NU1902) | Merged fix or approved rollback within 7 calendar days. |
| Low (NU1901) | Merged fix or approved rollback within 30 calendar days. |

If an SLA cannot be met, record the exception and compensating controls in release and operations notes before the SLA window closes.

---

## 4. Command Contract (Windows, Full Paths)

Run these commands exactly from PowerShell:

```powershell
dotnet restore c:\ws\BudgetExperiment\BudgetExperiment.sln -p:NuGetAudit=true -p:NuGetAuditMode=all -p:NuGetAuditLevel=low "-p:WarningsAsErrors=NU1901;NU1902;NU1903;NU1904"

dotnet list c:\ws\BudgetExperiment\BudgetExperiment.sln package --vulnerable --include-transitive

dotnet list c:\ws\BudgetExperiment\BudgetExperiment.sln package --outdated --include-transitive

pwsh -NoLogo -NoProfile -File c:\ws\BudgetExperiment\scripts\operations\invoke-nuget-package-policy-gates.ps1 -RepositoryRoot c:\ws\BudgetExperiment -ArtifactDirectory c:\ws\BudgetExperiment\artifacts\nuget-audit -StyleCopRegistrationUrl https://api.nuget.org/v3/registration5-semver2/stylecop.analyzers/index.json

dotnet build c:\ws\BudgetExperiment\BudgetExperiment.sln --no-restore
```

Expected contract:
- `dotnet restore` is the authoritative vulnerability gate and must return exit code 0.
- Vulnerable/outdated reports must produce readable logs for triage.
- Policy gate script must return a pass state for prerelease policy and latest StyleCop preview checks.

---

## 5. Artifact Retention Expectation

- Retain package hygiene artifacts for at least 30 days.
- Required artifact set:
  - restore audit log
  - vulnerable package report
  - outdated package report
  - policy gate output
  - metadata summary (trigger source, commit SHA, timestamp, exit codes)
- If a run fails, do not delete artifacts early.

---

## 6. Rollback Procedure to Prior Known-Good Package Set

Use this when package updates cause build, test, or runtime regression.

1. Identify the last known-good commit SHA that passed package hygiene checks and smoke validation.
2. Create a rollback branch from the active integration branch.
3. Restore package definitions from known-good commit:

```powershell
dotnet list c:\ws\BudgetExperiment\BudgetExperiment.sln package > c:\ws\BudgetExperiment\artifacts\nuget-audit\rollback-package-baseline.txt

git -C c:\ws\BudgetExperiment diff --name-only <known-good-sha> HEAD -- "*.csproj" "Directory.Build.props" > c:\ws\BudgetExperiment\artifacts\nuget-audit\rollback-package-files.txt

Get-Content c:\ws\BudgetExperiment\artifacts\nuget-audit\rollback-package-files.txt | ForEach-Object { git -C c:\ws\BudgetExperiment checkout <known-good-sha> -- $_ }
```

4. Review rollback scope with `git -C c:\ws\BudgetExperiment status --short` and keep only package-version rollback deltas.
5. Commit with a rollback message that includes reason, affected advisories or regressions, and known-good SHA.

---

## 7. Post-Rollback Smoke Validation

Run the following in sequence and capture output in operations notes:

```powershell
dotnet restore c:\ws\BudgetExperiment\BudgetExperiment.sln -p:NuGetAudit=true -p:NuGetAuditMode=all -p:NuGetAuditLevel=low "-p:WarningsAsErrors=NU1901;NU1902;NU1903;NU1904"

dotnet build c:\ws\BudgetExperiment\BudgetExperiment.sln --no-restore

dotnet test c:\ws\BudgetExperiment\BudgetExperiment.sln --filter "Category!=Performance"
```

Smoke pass criteria:
- Restore gate passes with zero vulnerability audit errors.
- Build succeeds with no restore step.
- Non-performance tests pass.

---

## 8. Evidence Logging Pattern

For audit and compliance traceability, capture and log evidence artifacts consistently across all monthly cycles and rollback events:

**Evidence Logging Structure:**
```
artifacts/nuget-audit/
  ├─ restore-pass.log          (restore audit passed)
  ├─ restore-failed.log        (restore audit failed, if applicable)
  ├─ prerelease-policy.log     (pre-release policy validation output)
  ├─ stylecop-latest-preview.log (StyleCop preview version check)
  └─ metadata.txt              (cycle month, commit SHA, timestamp, exit codes)
```

**Naming Convention**: Use ISO 8601 format for cycle dates in `metadata.txt` (e.g., `2026-04-28`).

**Mandatory Capture**: Every monthly cycle must record restore pass/fail state, pre-release policy validation, StyleCop preview check, and metadata. If a rollback occurs, capture both failed and post-rollback pass evidence with clear artifact naming for traceability.

This pattern ensures future cycles can easily locate and reference evidence without ambiguity.

---

## 9. Release and Operations Notes Tracking

For each monthly cycle and rollback event:

1. Add an operations note entry to the active feature document in docs with:
   - cycle month
   - owner name
   - audit result summary
   - remediation or rollback decision
   - links to artifact names (use evidence logging pattern from Section 8)
2. Add a release note entry in CHANGELOG.md when remediation or rollback changes shipped behavior or dependency risk posture.
3. If a rollback occurred, include:
   - known-good SHA
   - root cause summary
   - smoke validation result summary
   - follow-up task reference for re-upgrade planning
   - references to rollback-evidence-failed-* and rollback-evidence-pass-* artifact folders for traceability

---

## Change Log

| Date | Change | Author |
|------|--------|--------|
| 2026-04-28 | Corrected restore command contract to PowerShell-safe quoting for the `WarningsAsErrors` semicolon list, aligned with Feature 167 testing guidance. | GitHub Copilot |

*Last updated: 2026-04-28.*