# Archive: Features 161–170

> Scope removal, AI model integration, and security hardening features. Completed work listed below.

---

## Feature 161: BudgetScope Removal

> **Status:** Done  
> **Category:** Architecture / Cleanup

Removed the legacy BudgetScope concept (Personal/Shared split) from the UI and service layer. All accounts and transactions now operate under a unified Shared scope.

**Key Points:**

- `ScopeSwitcher` component removed from navigation
- `ScopeService` locked to Shared; ignores any stored Personal value
- `AccountForm` normalizes scope to Shared on render
- `ScopeMessageHandler` sends only the Shared header to the API
- All scope-related UI removed from NavMenu

---

## Feature 162: Local LLaMA.cpp Model Recommendation

> **Status:** Done  
> **Category:** AI / Documentation

Established a tested model recommendation for the local AI backend using LLaMA.cpp. Documented configuration and hardware guidance for self-hosted deployments.

**Key Points:**

- Primary recommendation: `Qwen/Qwen3-14B-GGUF:Q5_K_M` for 32 GB RAM / 16 GB VRAM class hardware
- Runner-up guidance documented for `Qwen2.5-14B-Instruct`, `Meta-Llama-3.1-8B-Instruct`, and `Mistral-Small-3.1-24B` with explicit tradeoffs
- Quantization and context guidance captured for practical local operation (`Q4_K_M`/`Q5_K_M`/`Q6_K`, 8K-16K default context)

---

## Feature 163: Data Encryption for User Financial Data

> **Status:** Done  
> **Category:** Security

Implemented AES-256-GCM encryption at rest for sensitive financial columns using EF Core value converters. All encrypted fields use a versioned ciphertext format. Plaintext fallback reads support safe rollout for existing rows.

**Key Points:**

- Encrypted fields: `Accounts.Name`, `Transactions.Description`, `Transactions.Amount.Amount`, `ChatMessages.Content`, `MonthlyReflections` (intention, gratitude, improvement), `KaizenGoals.Description`, `CategorizationRules.Pattern`
- Encryption key loaded from `ENCRYPTION_MASTER_KEY` or `Encryption:MasterKey`; file-based `*_FILE` pattern preferred
- Ciphertext uses versioned prefix `enc::v1:`; unknown versions throw `DomainException` explicitly
- Migration `20260426235449_Feature163_EncryptedColumnStorageCompatibility` implemented with rollback guard blocking downgrade when ciphertext rows exist
- Encrypted-path query semantics: all filtering and sorting on encrypted columns is done in-memory after DB materialization (`TransactionRepository`, `AccountRepository`, `CategorizationRuleRepository`)
- Backup/restore scripts include SHA-256 checksum verification and destructive-restore guardrails
- Operational evidence checklist template at `docs/operations/backup-restore-evidence-checklist.md`
- Audit record: `docs/audit/2026-04-28-feature-163-quality-gate.md`

**Deferred to backlog (not blockers):**

- Automated key rotation workflow
- Automated encrypted backup/restore scheduling
- Published performance benchmark report
- GDPR compliance notes update

---

## Feature 164: GitHub Actions Revamp

> **Status:** Done  
> **Category:** DevOps / CI-CD

Completed the workflow-call release chain redesign and validated end-to-end release gating through CI, Docker publish, and release creation.

**Key Points:**

- Release path is now tag push `v*` -> `release.yml` -> `docker-build-publish.yml` -> `ci.yml`
- Docker workflow remains artifact-only and consumes CI `app-publish` output
- CI path filtering skips docs-only and non-code paths while preserving code-change validation
- Action versions are pinned consistently across delivery workflows
- Runtime evidence captured in feature documentation before archiving includes safe-tag chain success and branch-protection/CodeQL checks
- Historical standalone feature file removed from top-level `docs/` after archive capture

---

## Feature 165: NuGet Package Management Simplification

> **Status:** Done  
> **Category:** DevOps / Dependency Governance

Closed NuGet package hygiene simplification by removing custom NuGet audit workflows and scripts, and making restore-time SDK audit checks plus Dependabot the default package governance model.

**Key Points:**

- Global NuGet audit enforcement is set in `Directory.Build.props` (`NuGetAudit=true`, `NuGetAuditMode=all`, `NuGetAuditLevel=low`)
- Legacy workflows and supporting operations scripts for NuGet hygiene were removed from the repository
- Dependabot grouping remains active (`aspnetcore`, `extensions`, `efcore`, `testing`), and NuGet PRs are reviewed and merged manually
- Maintainer guidance exists in `.github/prompts/nuget-upgrade.prompt.md` and `docs/operations/nuget-package-hygiene-monthly-runbook.md`
- Validation evidence includes a historical local `NU1903` vulnerable-package probe plus current workflow-equivalent restore/build/test pass
- Historical standalone feature file removed from top-level `docs/` after archive capture

---

## Feature 166: Feature 165 Final Audit

> **Status:** Done  
> **Category:** Audit / Quality Gate

Final audit for Feature 165 is closed after reconciliation against current repository truth.

**Key Points:**

- Final closeout validated current repository truth for workflows, docs, and governance files
- Workflow-equivalent local validation (`restore`, `build`, `test` with repository filter) passed during final audit
- Final verdict is **Go**, with explicit residual risk only for external GitHub runtime settings not provable from local workspace
- Historical standalone feature file removed from top-level `docs/` after archive capture

---

## Feature 167: NuGet Package Hygiene and Vulnerability Gate

> **Status:** Done  
> **Category:** DevOps / Quality Gate

Established a repeatable, policy-driven package hygiene process for the full solution. Enforces stable package versions, gates on zero-vulnerability restore, and documents operational procedures for monthly audits and safe rollback.

**Key Points:**

- Package policy: all packages stable except `StyleCop.Analyzers`, which tracks latest preview
- Restore authoritative gate: fails on any vulnerability advisory (direct or transitive)
- CI policy validation: pre-release allowlist, `StyleCop.Analyzers` preview version check
- Monthly cadence: documented runbook at `docs/operations/nuget-package-hygiene-monthly-runbook.md` with SLA targets and rollback procedures
- Evidence artifacts: `restore-pass.log`, `restore-failed.log`, `prerelease-policy.log`, `stylecop-latest-preview.log`, `metadata.txt`
- Automation: scheduled CI workflow runs monthly; watches Dependabot PRs; creates upgrade feature docs when needed
- Operational checklist: restore/build/test smoke validation; documented rollback path with known-good SHA recovery

---

## Feature 168: API Test Host Configuration Validation Gap

> **Status:** Done  
> **Category:** Testing / Validation

Closed a validation gap in API integration testing after `Encryption:MasterKey` became mandatory at startup. The work focused on custom API test hosts that isolate configuration and could miss newly required keys.

**Key Points:**

- Reviewed custom `WebApplicationFactory<Program>` usage as a startup-configuration validation surface
- Identified isolated host patterns (`config.Sources.Clear()` and in-memory-only config rebuilds) as risk points for required-key drift
- Standardized expectation that startup-sensitive changes require full `BudgetExperiment.Api.Tests` project validation, with targeted reruns used only for diagnosis
- Captured regression context from release workflow behavior to prevent repeat misses

---

## Feature 169: Dependabot Auto-Merge Security Signal Hardening

> **Status:** Superseded  
> **Category:** DevOps / Dependency Governance

Marked superseded after repository policy shifted away from Dependabot NuGet auto-merge automation to manual review and merge.

**Key Points:**

- NuGet auto-merge workflow is no longer present in the repository
- Dependabot NuGet pull requests are reviewed and merged manually
- CI/CD documentation reflects manual-review policy instead of workflow-level auto-merge hardening
- Historical standalone feature file removed from top-level `docs/` after archive capture
