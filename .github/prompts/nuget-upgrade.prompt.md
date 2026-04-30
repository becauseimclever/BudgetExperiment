---
description: "Review a Dependabot NuGet PR, respond to a restore vulnerability failure, or run a manual package upgrade with local verification"
name: "NuGet Upgrade"
argument-hint: "Scenario: dependabot-pr, ci-vulnerability, or manual-upgrade. Optional package name and target version."
agent: "agent"
---
Handle NuGet package upgrades in this repository using the current Dependabot-first workflow.

Repository rules:
- Dependabot is the default source for NuGet upgrade discovery and pull requests.
- NuGet groups in `.github/dependabot.yml`: `aspnetcore`, `extensions`, `efcore`, `testing`.
- `Directory.Build.props` enables global SDK audit checks, so `dotnet restore` fails when a direct or transitive package has a known vulnerability.
- Patch-only Dependabot NuGet PRs can be set to auto-merge after required checks pass. Minor, major, and security-related updates still need human review.

Use these local verification commands:

```powershell
dotnet restore c:\ws\BudgetExperiment\BudgetExperiment.sln
dotnet build c:\ws\BudgetExperiment\BudgetExperiment.sln --configuration Release
dotnet test c:\ws\BudgetExperiment\BudgetExperiment.sln --configuration Release --filter "FullyQualifiedName!~E2E&Category!=Performance"
```

## Scenario 1: Dependabot PR review flow

Goal: review a Dependabot NuGet PR and decide whether it can merge.

1. Confirm the PR is a NuGet update from `dependabot[bot]`.
2. Check whether the PR matches one of the repository's NuGet groups: `aspnetcore`, `extensions`, `efcore`, or `testing`.
3. Check the update scope:
   - Patch updates can be eligible for auto-merge after CI passes.
   - Minor and major updates always need manual review.
   - Security-related PRs should be reviewed by a human even if the version bump is small.
4. Run the local verification commands.
5. Review CI results, package release notes, and any breaking-change notes.
6. If everything is green:
   - Approve and merge the PR when manual review is required.
   - Or leave auto-merge enabled for eligible patch-only PRs.

## Scenario 2: CI failure due to a vulnerable package

Goal: clear a restore failure caused by a known package advisory.

1. Open the failing CI run and read the `Restore dependencies` step in `.github/workflows/ci.yml`.
2. Identify the package name, version, and advisory reported by `dotnet restore`.
3. Check whether Dependabot already opened a NuGet PR for that package or group.
4. If no PR exists yet, trigger a manual Dependabot update from GitHub:
   1. Open the repository on GitHub.
   2. Go to `Insights` > `Dependency graph` > `Dependabot`.
   3. Find the NuGet ecosystem entry.
   4. Use the UI action to check for updates or request a new update job.
5. If Dependabot cannot resolve it quickly enough, run a manual upgrade on a branch.
6. Run the local verification commands.
7. Open or update the PR with the package fix and confirm CI passes.

## Scenario 3: Manual package upgrade flow

Goal: upgrade a specific package directly when Dependabot is not enough.

1. Create or switch to a branch for the upgrade.
2. Update the package with the `dotnet` CLI. Example:

```powershell
dotnet add c:\ws\BudgetExperiment\src\BudgetExperiment.Api\BudgetExperiment.Api.csproj package Microsoft.Extensions.DependencyInjection --version 10.0.1
```

3. Keep versions explicit. Do not hand-edit `.csproj` package references unless you are resolving a merge conflict.
4. Run the local verification commands.
5. Summarize the upgrade, affected projects, and any follow-up work in the PR.

Expected output:
- Identify the scenario being handled.
- List the package or Dependabot group involved.
- Show the verification commands that were run or still need to be run.
- Call out whether the change is patch-only or needs manual review.
- Report any blocker, including unresolved vulnerabilities or failing tests.