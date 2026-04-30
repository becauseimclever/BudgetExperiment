# NuGet Package Hygiene Monthly Runbook

Purpose: review package health once a month using the current Dependabot-first workflow and the .NET SDK restore audit.

This runbook replaces the old custom NuGet scripts and workflows. Those files were removed in Feature 165. Do not look for a monthly GitHub Actions hygiene job or any `scripts/operations/invoke-nuget-*.ps1` package policy script.

## 1. What This Runbook Covers

- Dependabot is the default source for NuGet upgrade discovery and PR creation.
- `Directory.Build.props` enables the .NET SDK audit, so `dotnet restore` fails on known vulnerable direct or transitive packages.
- Patch-only Dependabot NuGet PRs can auto-merge after required checks pass.
- Minor, major, and security-related updates still need manual review.

For the wider workflow overview, see `docs/ci-cd-deployment.md`. For step-by-step PR handling, see `.github/prompts/nuget-upgrade.prompt.md`.

## 2. Cadence and Ownership

- Run this review at least once per calendar month.
- Run it again when CI fails at the restore step for a package advisory.
- One owner should complete the review and record the outcome.

## 3. Monthly Review Steps

1. Review open Dependabot NuGet PRs in GitHub.
2. Check whether any package-related CI runs failed at the `Restore dependencies` step.
3. Run the local verification commands from PowerShell:

```powershell
dotnet restore c:\ws\BudgetExperiment\BudgetExperiment.sln
dotnet list c:\ws\BudgetExperiment\BudgetExperiment.sln package --vulnerable --include-transitive
dotnet build c:\ws\BudgetExperiment\BudgetExperiment.sln --configuration Release --no-restore
dotnet test c:\ws\BudgetExperiment\BudgetExperiment.sln --configuration Release --filter "FullyQualifiedName!~E2E&Category!=Performance"
```

1. If you need a manual view of pending updates outside Dependabot, run this optional report:

```powershell
dotnet list c:\ws\BudgetExperiment\BudgetExperiment.sln package --outdated --include-transitive
```

1. Triage what you found:
   - Let eligible patch Dependabot PRs continue through the normal CI and auto-merge path.
   - Review minor, major, and security-related updates manually.
   - If `dotnet restore` fails, treat that as the main vulnerability gate and fix the affected package before merge or release.

## 4. Expected Results

- `dotnet restore` exits with code `0`.
- `dotnet list ... --vulnerable --include-transitive` shows no unresolved vulnerable packages.
- Build succeeds without another restore.
- Non-performance tests pass.

If any of these checks fail, open or update a package fix PR before closing the monthly review.

## 5. Handling Failures

### Restore Fails on a Vulnerable Package

1. Read the package name, version, and advisory from the restore output.
2. Check whether Dependabot already opened a PR for that package or group.
3. If no PR exists, trigger a Dependabot update job from GitHub or create a manual upgrade branch.
4. Re-run the verification commands after the package change.

### A Dependabot PR Needs Manual Review

1. Check the update scope.
2. Read package release notes and breaking changes.
3. Run the local verification commands.
4. Merge only after CI passes.

## 6. Evidence to Record

Keep the record lightweight. A short note in the tracking issue, PR, or operations log is enough.

Record:

- review date
- owner
- whether restore passed
- whether vulnerable packages were found
- open follow-up PRs or issues

There is no required `artifacts/nuget-audit/` evidence bundle in the current model.

## 7. Notes About Current Policy

- The repo currently pins `StyleCop.Analyzers` centrally in `Directory.Build.props`.
- The repo no longer has a separate script or workflow that enforces a pre-release allowlist or checks for the latest StyleCop preview.
- Review any pre-release package change manually in the PR.

## Change Log

- 2026-04-30: Rewrote the runbook for the current Dependabot plus SDK audit model and removed references to deleted scripts, workflows, and artifact requirements.

Last updated: 2026-04-30.
