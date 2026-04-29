# Feature 163: NuGet Upgrade Cycle 2026-04

> **Status:** Planning

## Overview

This feature tracks the NuGet upgrade cycle for 2026-04. It is generated automatically from CI when the monthly schedule runs or when Dependabot pull request activity is detected.

## Trigger Metadata

- Upgrade Cycle: 2026-04
- Trigger Source: dependabot-pr
- Generated On (UTC): 2026-04-29 04:24:09

## Policy Context (Feature 167)

- Stable package versions are required across the solution.
- StyleCop.Analyzers is the only allowed pre-release package and must stay on the latest preview.
- Vulnerability checks must include direct and transitive dependencies.

## Upgrade Tasks

- [ ] Review restore vulnerability gate output.
- [ ] Review vulnerable package report (dotnet list package --vulnerable --include-transitive).
- [ ] Review outdated package report (dotnet list package --outdated --include-transitive).
- [ ] Prepare update plan that keeps stable-only policy compliance.
- [ ] Confirm StyleCop.Analyzers remains on latest preview.
- [ ] Capture rollback notes and required follow-up actions.

## Definition of Done

- [ ] Audit artifacts are attached to the workflow run.
- [ ] Upgrade plan is documented and linked to implementation work.
- [ ] Feature 167 package policy compliance is confirmed.
