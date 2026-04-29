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
- Recommended model identified and validated for local inference
- Configuration guidance added to `docs/162-local-llamacpp-model-recommendation.md`
- Hardware requirements documented for Raspberry Pi and x86 targets

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
