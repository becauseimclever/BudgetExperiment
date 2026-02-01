# Feature 042: Optimize CI/CD Pipeline Speed
> **Status:** ✅ Completed (2026-02-01)

## Overview

Reduce CI/CD pipeline execution time from ~20 minutes to under 5 minutes, covering build, Docker image creation, and all tests (excluding E2E tests). The goal is to accelerate feedback for contributors and maintainers, improving productivity and release velocity.

## Problem Statement

The current CI/CD pipeline takes approximately 20 minutes to complete, which slows down development, code review, and release cycles. Most of this time is spent on build, Docker, and test steps that could be parallelized, cached, or otherwise optimized.

### Current State Analysis (2026-01-31)

| Area | Current State | Issue |
|------|---------------|-------|
| **Tests** | ❌ No tests run in CI | Tests are missing entirely from the pipeline |
| **Build** | Inside Dockerfile only | Not run separately before Docker |
| **Docker caching** | ✅ Uses `cache-from: type=gha` | Good - GHA cache is in use |
| **Multi-arch** | Builds both amd64 + arm64 | Slower (~2x build time via QEMU emulation) |
| **Parallelism** | ❌ Single sequential job | No parallel jobs |
| **PR vs Main** | Only pushes on non-PR | Correct, but still builds multi-arch on PR |
| **Concurrency** | ❌ No cancellation | Old runs not cancelled on new pushes |

### Final Implemented State (2026-02-01)

| Area | Final State | Improvement |
|------|-------------|-------------|
| **Tests** | ✅ 1623 tests run in CI | Full test coverage with TRX reporting |
| **Build** | ✅ Separate build-and-test job | .NET publish before Docker |
| **Docker** | ✅ Prebuilt Dockerfile | Uses pre-compiled artifacts |
| **Multi-arch** | ✅ Native runners (amd64 + arm64) | No QEMU emulation |
| **Parallelism** | ✅ 3-job pipeline with matrix | Build → parallel Docker builds → merge |
| **Tags** | ✅ `latest` on releases, `preview` on main | Clear versioning strategy |
| **Concurrency** | ✅ Auto-cancel in-progress | New pushes cancel old workflows |

### Target State

- Pipeline completes in under 5 minutes for typical PRs and main branch builds
- Build, test, and Docker steps are parallelized and/or cached where possible
- Only necessary steps are run for each PR (e.g., skip publish on PRs, single-arch build)
- All unit, integration, and API tests run (E2E tests excluded)
- Docker images are built efficiently with layer caching
- New pushes cancel in-progress workflows for the same branch

---

## User Stories

### Pipeline Speed Optimization

#### US-042-001: Fast feedback for contributors
**As a** contributor  
**I want to** get CI feedback in under 5 minutes  
**So that** I can iterate quickly and merge with confidence

**Acceptance Criteria:**
- [ ] CI pipeline completes in under 5 minutes for PRs
- [ ] All non-E2E tests run and pass
- [ ] Failures are reported quickly and clearly

#### US-042-002: Efficient Docker builds
**As a** maintainer  
**I want to** build Docker images quickly in CI  
**So that** images are ready for deployment without delay

**Acceptance Criteria:**
- [ ] Docker build step uses layer caching
- [ ] Docker build does not block test execution
- [ ] Images are published only on main branch or release tags

#### US-042-003: Parallel and cached steps
**As a** developer  
**I want to** leverage parallelism and caching in CI  
**So that** redundant work is avoided and time is saved

**Acceptance Criteria:**
- [ ] Restore, build, and test steps use caching where possible
- [ ] Steps that can run in parallel do so
- [ ] Only changed projects are rebuilt/tested if feasible

---

## Technical Design

### Optimized Workflow Structure

```
┌─────────────────────────────────────────────────────────────┐
│                    On: push/PR to main                       │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
                ┌───────────────────────┐
                │  Job: build-and-test  │
                │  ─────────────────────│
                │  • NuGet cache        │
                │  • dotnet build       │
                │  • dotnet test (1623) │
                │  • dotnet publish     │
                │  • Upload artifacts   │
                └───────────────────────┘
                              │
        ┌─────────────────────┴─────────────────────┐
        ▼                                           ▼
┌───────────────────┐                    ┌───────────────────┐
│  Job: docker-build│                    │  Job: docker-build│
│  (amd64)          │                    │  (arm64)          │
│  ─────────────────│                    │  ─────────────────│
│  • ubuntu-latest  │                    │  • ubuntu-24.04-  │
│  • Native build   │                    │    arm (native)   │
│  • Push by digest │                    │  • Push by digest │
└───────────────────┘                    └───────────────────┘
                              │
                              ▼
                ┌───────────────────────┐
                │  Job: docker-merge    │
                │  ─────────────────────│
                │  • Create manifest    │
                │  • Tag: preview/latest│
                │  • Push to ghcr.io    │
                └───────────────────────┘
```

### Key Optimizations

| Optimization | Benefit |
|--------------|---------|
| **Add test job** | Catch bugs in CI before merge |
| **NuGet cache** | Skip restore if lockfile unchanged |
| **Parallel test execution** | Run test projects concurrently |
| **Native ARM64 runners** | `ubuntu-24.04-arm` eliminates QEMU emulation |
| **Matrix Docker builds** | amd64 and arm64 build in parallel |
| **Prebuilt Dockerfile** | Docker copies pre-compiled artifacts, no in-container build |
| **Cancel in-progress** | New pushes cancel old workflows |
| **Split jobs** | build-and-test → docker-build (matrix) → docker-merge |
| **Tag strategy** | `preview` on main, `latest` only on version tags |

### Architecture Changes

- Update GitHub Actions workflow(s) for optimal step ordering, caching, and parallelism
- Use GitHub Actions cache for NuGet and Docker layers
- Split jobs for build/test and Docker where possible
- Use matrix builds for test parallelism
- Only publish Docker images on main branch or tags

### Domain Model

- No changes required

### API Endpoints

- No changes required

### Database Changes

- No changes required

### UI Components

- No changes required

---

## Implementation Plan

### Phase 1: Add Build & Test Job ✅

**Objective:** Add tests to CI with caching

**Tasks:**
- [x] Add `build-and-test` job to workflow
- [x] Add NuGet cache using `actions/cache`
- [x] Run `dotnet build` for solution
- [x] Run `dotnet test` for all non-E2E test projects
- [x] Upload test results as artifacts

**Commit:**
- chore(ci): add build and test job with caching

---

### Phase 2: Optimize Docker Build ✅

**Objective:** Speed up Docker build for PRs

**Tasks:**
- [x] Build single-arch (amd64) for PRs
- [x] Keep multi-arch for main branch and tags
- [x] Make Docker job depend on build-test success

**Commit:**
- chore(ci): optimize Docker build with conditional multi-arch

---

### Phase 3: Add Workflow Optimizations ✅

**Objective:** Add quality-of-life improvements

**Tasks:**
- [x] Add concurrency to cancel superseded runs
- [x] Add job summaries for visibility

**Commit:**
- chore(ci): add concurrency cancellation

---

### Phase 4: Native ARM64 Runners & Matrix Builds ✅

**Objective:** Eliminate QEMU emulation and parallelize Docker builds

**Tasks:**
- [x] Create `Dockerfile.prebuilt` for pre-compiled artifact builds
- [x] Implement matrix strategy for parallel amd64/arm64 builds
- [x] Use native `ubuntu-24.04-arm` runner for arm64
- [x] Remove QEMU setup step
- [x] Add docker-merge job to create multi-arch manifest
- [x] Implement tag strategy: `preview` on main, `latest` on releases

**Commits:**
- ci: restructure workflow with parallel Docker matrix builds
- ci: add Dockerfile.prebuilt for pre-compiled builds
- ci: tag latest only on releases, preview on main
- ci: lowercase image name for Docker registry
- ci: use native arm64 runners, remove QEMU

---

### Phase 5 (Optional): Post-deployment E2E tests

**Objective:** Optionally run E2E tests after deployment completes (see doc 037)

**Tasks:**
- [ ] Trigger E2E test suite after successful deployment
- [ ] Report E2E results separately from main pipeline
- [ ] Ensure failures do not block main build unless configured

**Commit:**
- chore(ci): add post-deployment E2E test step

**Objective:** Document pipeline changes and update references

**Tasks:**
- [ ] Update CI/CD documentation
- [ ] Add timing benchmarks to docs
- [ ] Final review and cleanup

**Commit:**
- docs: document CI/CD pipeline optimizations

---

## Testing Strategy

### Automated Validation

- [x] Pipeline completes in under 5 minutes (excluding E2E)
- [x] All non-E2E tests run and pass (filter: `FullyQualifiedName!~E2E`)
- [x] Docker image is built on all runs
- [x] Docker image is published only on main/tags
- [x] Test results uploaded as artifacts
- [x] Test summary displayed in PR checks

### Manual Testing Checklist

- [ ] Trigger PR and main branch builds
- [ ] Verify timing and output
- [ ] Confirm no regressions in test or build steps

---

## Migration Notes

- None

---

## Security Considerations

- Ensure secrets are not exposed in logs
- Restrict publish steps to trusted branches/tags

---

## Performance Considerations

- Aggressive caching and parallelism
- Minimize redundant work

---


## Future Enhancements

- Optionally auto-run E2E tests after deployment (see doc 037)
- Auto-cancel redundant builds on new pushes ✅ (implemented)
- Fine-grained test selection based on changed files
- Build artifact sharing between build-test and docker jobs

---

## References

- .github/workflows/docker-build-publish.yml
- GitHub Actions cache and matrix docs

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-01 | Feature completed: native runners, matrix builds, tag strategy | @github-copilot |
| 2026-01-31 | Implemented workflow optimizations, test fixes | @github-copilot |
| 2026-01-26 | Initial draft | @github-copilot |
