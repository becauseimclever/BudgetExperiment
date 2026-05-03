# Audit: Feature 160 - First Slice Quality Gate (Consolidated)

**Date:** 2026-05-02  
**Auditor:** Dotnet Documentation Steward (consolidation of prior audit rounds)  
**Status:** Consolidated history from initial test audit and final quality-gate planning

## Context

Feature 160 introduced first-slice support for a pluggable AI backend and related test coverage across API and infrastructure test projects.

An additional remediation audit was drafted in the docs root:

- `docs/171-feature-160-slice-quality-gate-remediation.md`

This file consolidates all audit rounds into one dated audit record under `docs/audit/` in line with repository documentation rules.

## Scope

- `tests/BudgetExperiment.Infrastructure.Tests/OpenAiCompatibleAiServiceTests.cs`
- `tests/BudgetExperiment.Infrastructure.Tests/LlamaCppAiServiceTests.cs`
- `tests/BudgetExperiment.Infrastructure.Tests/DependencyInjectionTests.cs`
- `tests/BudgetExperiment.Api.Tests/AiControllerTests.cs`
- `tests/BudgetExperiment.Api.Tests/Feature160OpenApiContractTests.cs`
- `docs/160-pluggable-ai-backend.md`
- `docs/AI.md`
- `README.md`

## Audit History

### Round 1: Test Audit Hardening (Source: 171)

#### Findings Summary

1. High: AI API tests allowed multiple unrelated status codes, which could hide endpoint regressions.
2. Medium: AI API model-list test was weak and environment-dependent.
3. Medium: DI backend-selection test did not prove that only the selected backend was invoked.

#### Acceptance Criteria (Round 1)

- AI controller integration tests are deterministic and assert one expected status per scenario.
- AI controller tests do not rely on ambient AI availability; they use explicit stubs/fakes per scenario.
- Backend-selection tests verify non-selected backend is not called.
- Duplicate or near-duplicate assertions are consolidated to improve test signal quality.

#### Implementation Tasks (Round 1)

- Add dedicated `IAiService` stubs for explicit `OK`, `ServiceUnavailable`, and `GatewayTimeout` scenarios and assert exact endpoint response behavior for each.
- Replace permissive status assertions with precise, scenario-specific assertions.
- Strengthen model-list API tests to assert response shape and expected model payload from controlled test doubles.
- Extend DI backend-selection tests to assert call isolation (selected backend called, non-selected backend not called).
- Remove or rewrite redundant endpoint-accessibility-only tests once deterministic scenario tests exist.

#### Open Questions (Round 1)

- Should `/api/v1/ai/analyze` guarantee one canonical status code for backend unavailability, or preserve both `503` and `504` based on error category?
- Should controller tests always authenticate through the test auth helper for consistency, even when current endpoint policy allows anonymous access in test setup?

#### Assumptions (Round 1)

- Existing test host infrastructure can register scenario-specific `IAiService` test doubles without major fixture changes.
- Feature 160 first slice targets reliable CI regression detection, not only endpoint smoke coverage.

### Round 2: Final Quality-Gate Audit (Source: 172)

#### Problem Statement

After remediation, targeted tests passed locally, but remaining gaps still reduced protection against API contract drift and backend routing regressions.

#### Current State at Time of Audit

- AI controller tests validated status codes for key analyze failure paths.
- Backend-selection tests validated backend routing for status checks.
- Infrastructure adapter tests validated OpenAI-compatible payload and token parsing behavior.
- Targeted remediated test suites passed locally.

#### Target State for Quality-Gate Closure

- Analyze failure-path tests validate RFC 7807 payload shape, not only status codes.
- Backend-selection tests cover complete and model-list operations, not only status checks.
- Test documentation and comments accurately describe tested behavior.

#### User Stories and Acceptance Criteria (Round 2)

1. US-172-001: Validate AI failure contract shape.
2. US-172-002: Cover runtime backend selection beyond status endpoint.
3. US-172-003: Keep test intent documentation accurate.

Acceptance criteria details:

- `503` analyze tests assert `application/problem+json` and stable ProblemDetails fields (`status`, `title`/`type` when set, and `traceId` extension).
- `504` analyze tests assert `application/problem+json` and stable ProblemDetails fields (`status`, `title`/`type` when set, and `traceId` extension).
- Backend selector tests include route verification for `GetAvailableModelsAsync` and `CompleteAsync`.
- Each routing test asserts only the selected backend is called.
- XML summary above unavailable analyze tests describes `503` unavailable behavior accurately.

#### Technical and Implementation Plan (Round 2)

- No production architecture changes; this is test-suite hardening work.
- No endpoint contract changes under `/api/v1/ai`; only deeper validation.

Planned phases:

1. API failure contract assertions for `503` and `504` analyze responses.
2. Backend routing coverage expansion for `GetAvailableModelsAsync` and `CompleteAsync`.
3. Test documentation cleanup for stale XML comments.
4. Validation runs for targeted API and infrastructure test suites with `Category!=Performance`.

#### Risks and Mitigations (Round 2)

- Risk: Strict error payload assertions become brittle.
- Mitigation: Assert stable contract fields (`status`, content type, `traceId`) and avoid overfitting to localized message text.

- Risk: Added routing tests duplicate adapter-level coverage.
- Mitigation: Keep routing tests focused only on dispatch behavior and selected-backend invocation counts.

### Round 3: Slice Quality-Gate Remediation (Source: docs root draft)

#### Findings Summary

1. Medium: Feature documentation had contradictory token-counting verification claims.
2. Medium: Migration guidance mixed "migration exists" and "migration still required" messaging without clear scope boundaries.
3. Medium: OpenAPI contract tests used loose schema matching and limited response-contract assertions, increasing false-positive risk.

#### Acceptance Criteria (Round 3)

- `docs/160-pluggable-ai-backend.md` contains no contradictory checklist claims about token-counting verification.
- `docs/160-pluggable-ai-backend.md` presents one consistent migration story with explicit scope.
- `Feature160OpenApiContractTests` uses deterministic schema identity checks (no suffix-based accidental matches).
- `Feature160OpenApiContractTests` validates at least one response schema shape per Feature 160 endpoint operation.
- `README.md` and `docs/AI.md` remain aligned with validated-status language used in `docs/160-pluggable-ai-backend.md`.
- Targeted `Feature160OpenApiContractTests` pass after updates.

#### Implementation Tasks (Round 3)

- Reconcile token-counting and migration statements in `docs/160-pluggable-ai-backend.md`.
- Harden `Feature160OpenApiContractTests` schema lookup and response-contract coverage.
- Align Feature 160 claim wording in `README.md` and `docs/AI.md`.
- Run targeted `Feature160OpenApiContractTests` and retain any known manual-validation gaps.

#### Out of Scope (Round 3)

- Implementing new AI backend runtime behavior.
- Full dual-backend runtime parity verification against live Ollama and llama.cpp instances.

## Consolidated Outcome

This audit history confirms the progression from broad test hardening to final quality-gate closure planning and a focused remediation pass for documentation and OpenAPI contract evidence quality. Consolidation removes fragmented audit tracking and keeps the audit trail in the required dated `docs/audit/` location.

### Round 4: Guardrail Configuration Audit (Branch feature/160-pluggable-ai-backend)

#### Scope (Round 4)

- `.github/agents/dotnet-auditor-reviewer.agent.md`
- `.github/agents/dotnet-fleet-orchestrator.agent.md`
- `.github/instructions/agent-handoff-coordination.instructions.md`
- Alignment check against `.github/instructions/engineering-guide.instructions.md` section 36.

#### Findings Summary (Round 4)

1. Low: Fleet orchestrator sanitation wording is limited to "numbered audit docs" in docs root, which can miss misplaced non-numbered audit docs and still violate engineering-guide policy.

#### Evidence (Round 4)

- Orchestrator enforces cleanup for numbered audit docs only:
	- `.github/agents/dotnet-fleet-orchestrator.agent.md` lines 23-24 and line 40.
- Engineering guide requires all audit findings to live under docs/audit (not docs root), regardless of numbering:
	- `.github/instructions/engineering-guide.instructions.md` line 290.
- Auditor reviewer guardrails are aligned with dated docs/audit policy and explicitly forbid numbered docs-root audit artifacts:
	- `.github/agents/dotnet-auditor-reviewer.agent.md` lines 25-27 and line 39.
- Handoff coordination requires passing audit artifact policy and consolidation target when delegating the auditor:
	- `.github/instructions/agent-handoff-coordination.instructions.md` line 32.

#### Recommendation (Round 4)

- Tighten orchestrator wording from "numbered audit docs" to "any misplaced audit docs" in docs root so sanitation checks fully match engineering-guide policy.

#### Round 4 Verdict

- Safe to keep: Yes, with minor wording hardening recommended to reduce recurrence risk.

### Round 5: Final Feature-Closure Quality Gate (Branch feature/160-pluggable-ai-backend)

#### Scope (Round 5)

- `docs/160-pluggable-ai-backend.md`
- `docs/AI.md`
- `CONTRIBUTING.md`
- `src/BudgetExperiment.Infrastructure/ExternalServices/AI/OpenAiCompatibleAiService.cs`
- `tests/BudgetExperiment.Api.Tests/Feature160OpenApiContractTests.cs`
- `tests/BudgetExperiment.Api.Tests/AiControllerTests.cs`
- `tests/BudgetExperiment.Infrastructure.Tests/OpenAiCompatibleAiServiceTests.cs`
- `tests/BudgetExperiment.Infrastructure.Tests/LlamaCppAiServiceTests.cs`
- `tests/BudgetExperiment.Infrastructure.Tests/DependencyInjectionTests.cs`
- `tests/BudgetExperiment.Infrastructure.Tests/BackendSelectingAiServiceTests.cs`

#### Validation Executed (Round 5)

- Targeted tests executed from scoped files: **37 passed, 0 failed**.

#### Findings (Round 5)

1. **Medium: Feature doc contains implementation-inaccurate claims while marked complete.**
	 - `docs/160-pluggable-ai-backend.md` marks the feature complete (`Status: Complete`) while asserting provider protocol and DI behavior that does not match runtime implementation details.
	 - Evidence:
		 - `docs/160-pluggable-ai-backend.md` line 17 states both Ollama and llama.cpp expose `/v1/chat/completions` and `/v1/models`.
		 - `docs/160-pluggable-ai-backend.md` line 26 states DI conditionally registers one `IAiService` by `AiSettings:BackendType`.
		 - Runtime implementation uses Ollama-specific routes:
			 - `src/BudgetExperiment.Infrastructure/ExternalServices/AI/OllamaAiService.cs` lines 35, 38, 41 (`api/version`, `api/tags`, `api/chat`).
		 - Runtime DI registers a selector with both backends:
			 - `src/BudgetExperiment.Infrastructure/DependencyInjection.cs` line 75 (`AddScoped<IAiService, BackendSelectingAiService>()`).
	 - Impact:
		 - Increases risk of operator/developer misconfiguration and weakens trust in completion checklist claims.
		 - Reduces future maintainability because extension guidance can send contributors to the wrong integration seam.

2. **Low: `docs/AI.md` PowerShell examples do not follow repository fully-qualified-path command policy.**
	 - Evidence:
		 - `docs/AI.md` lines 25-27 and 63-64 use relative project path `--project src/BudgetExperiment.Api` in PowerShell examples.
		 - Policy requires fully qualified paths for PowerShell commands:
			 - `.github/instructions/engineering-guide.instructions.md` line 240.
	 - Impact:
		 - Minor contributor friction and potential reproducibility issues when commands are run outside repository root.

#### Risk Assessment (Round 5)

- **Runtime regression risk:** Low based on scoped test pass (37/37).
- **Documentation and process risk:** Medium due to mismatch between completion claims and actual implementation mechanics.

#### Round 5 Verdict

- **Feature 160 safe to close:** **No (not yet)**.
- Closure should wait for documentation claim correction in `docs/160-pluggable-ai-backend.md` (and policy-aligned command examples in `docs/AI.md`) so the final completion state is accurate and compliant.

### Round 6: Final Close-Out Audit (Branch feature/160-pluggable-ai-backend)

#### Scope (Round 6)

- `docs/archive/151-160-refactoring-performance-quality.md`
- `docs/AI.md`
- `CONTRIBUTING.md`
- `src/BudgetExperiment.Infrastructure/ExternalServices/AI/OpenAiCompatibleAiService.cs`
- `tests/BudgetExperiment.Api.Tests/Feature160OpenApiContractTests.cs`
- `tests/BudgetExperiment.Api.Tests/AiControllerTests.cs`
- `tests/BudgetExperiment.Infrastructure.Tests/OpenAiCompatibleAiServiceTests.cs`
- `tests/BudgetExperiment.Infrastructure.Tests/LlamaCppAiServiceTests.cs`
- `tests/BudgetExperiment.Infrastructure.Tests/DependencyInjectionTests.cs`
- `tests/BudgetExperiment.Infrastructure.Tests/BackendSelectingAiServiceTests.cs`
- Deletion state of Feature 160 docs-root feature spec and archive closure compliance.

#### Validation Executed (Round 6)

- Targeted tests for scoped files: **37 passed, 0 failed**.
- Archive closure check: Feature 160 appears in archive with `Status: Done`.
- Deletion check: Feature 160 docs-root feature spec is currently in deleted state in git index/worktree.

#### Findings (Round 6)

1. **Medium: AI analyze error responses are not RFC 7807 ProblemDetails for 503/504 paths.**
	 - Evidence:
		 - Engineering standard requires ProblemDetails for API errors: `.github/instructions/engineering-guide.instructions.md` line 84.
		 - `AiController.AnalyzeAsync` advertises 503/504 but returns anonymous JSON payloads for these paths:
			 - `src/BudgetExperiment.Api/Controllers/AiController.cs` lines 151-156.
			 - `src/BudgetExperiment.Api/Controllers/AiController.cs` lines 194-200.
			 - `src/BudgetExperiment.Api/Controllers/AiController.cs` lines 205-210.
		 - API tests assert only status codes for 503/504 and do not validate `application/problem+json` contract on those paths:
			 - `tests/BudgetExperiment.Api.Tests/AiControllerTests.cs` line 223.
			 - `tests/BudgetExperiment.Api.Tests/AiControllerTests.cs` line 239.
			 - By contrast, only the 500 path asserts ProblemDetails content type and shape (`application/problem+json`): `tests/BudgetExperiment.Api.Tests/AiControllerTests.cs` line 269.
	 - Impact:
		 - Error contract inconsistency for clients integrating `/api/v1/ai/analyze`.
		 - Reduced compatibility with documented API error-shape standard and weaker regression protection.

#### Risk Assessment (Round 6)

- **Runtime regression risk:** Low for covered happy/error status routing in scoped tests (37/37 pass).
- **API contract risk:** Medium due to non-ProblemDetails payloads on 503/504 analyze paths and missing test assertions for those contracts.
- **Documentation/deletion closure risk:** Low; archive closure and docs-root feature-doc deletion are both in compliant state.

#### Round 6 Verdict

- **Feature 160 safe to close:** **No (not yet)**.
- Closure should wait for `/api/v1/ai/analyze` 503/504 error-shape alignment to RFC 7807 plus targeted contract assertions in API tests.

### Round 7: Final Re-Audit After ProblemDetails Remediation (Branch feature/160-pluggable-ai-backend)

#### Scope (Round 7)

- `src/BudgetExperiment.Api/Controllers/AiController.cs`
- `tests/BudgetExperiment.Api.Tests/AiControllerTests.cs`
- `docs/archive/151-160-refactoring-performance-quality.md`
- `docs/AI.md`
- `CONTRIBUTING.md`
- Deletion state of Feature 160 docs-root feature spec.

#### Validation Executed (Round 7)

- Targeted API tests: `tests/BudgetExperiment.Api.Tests/AiControllerTests.cs` -> **11 passed, 0 failed**.
- Workflow acceptance context reviewed: `.github/workflows/ci.yml`, `.github/workflows/docker-build-publish.yml`, `.github/workflows/release.yml`.
- Deletion check confirmed: Feature 160 docs-root spec remains deleted in git state.
- Archive closure check confirmed: Feature 160 remains listed as `Status: Done` in archive.

#### Findings (Round 7)

- **No new findings in audited scope.**

#### Prior Finding Resolution Check (Round 6 -> Round 7)

- **Resolved:** Prior medium finding on non-ProblemDetails analyze 503/504 payloads is closed.
	- `AiController.AnalyzeAsync` now routes 503/504 paths through `CreateAnalyzeProblem(...)` and emits RFC 7807 payloads with `application/problem+json` and `traceId` extension.
	- `AiControllerTests` now asserts 503 and 504 content type plus ProblemDetails contract fields (`type`, `title`, `status`, `detail`, `instance`, `traceId`).

#### Risk Assessment (Round 7)

- **Runtime risk in audited scope:** Low.
- **API error-contract risk in audited scope:** Low after ProblemDetails remediation and targeted assertion coverage.
- **Residual validation risk:** Medium for full branch-wide CI equivalence because only targeted local tests were rerun for this re-audit (not full solution CI matrix).

#### Round 7 Verdict

- **Feature 160 safe to close:** **Yes (for scoped close-out).**
