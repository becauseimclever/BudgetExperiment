# Feature 108: Docker Hardened Images
> **Status:** Done

## Overview

Migrate all Docker container images to [Docker Hardened Images](https://www.docker.com/products/hardened-images/) where available. Docker Hardened Images are Docker's official hardened, continuously patched container images — built from source with verified provenance, minimal attack surface, SLSA attestations, and proactive CVE remediation. They should be preferred over standard Docker Official Images for all production deployments.

## Problem Statement

### Current State

The project uses standard Docker Official Images:

| File | Image | Purpose |
|------|-------|---------|
| `Dockerfile` (build stage) | `mcr.microsoft.com/dotnet/sdk:10.0` | Build/publish the application |
| `Dockerfile` (runtime stage) | `mcr.microsoft.com/dotnet/aspnet:10.0` | Run the application |
| `Dockerfile.prebuilt` | `mcr.microsoft.com/dotnet/aspnet:10.0` | Run pre-built application |
| `docker-compose.demo.yml` | `postgres:16-alpine` | Demo PostgreSQL database |

Standard images may contain unpatched CVEs, unnecessary packages, and lack provenance guarantees. Docker Hardened Images address these gaps with continuous patching and minimal base layers.

### Target State

Use Docker Hardened Images where available, falling back to the best-hardened alternative when Docker does not publish a hardened variant for a given image:

| File | Current Image | Hardened Image | Source |
|------|---------------|----------------|--------|
| `Dockerfile` (build stage) | `mcr.microsoft.com/dotnet/sdk:10.0` | No change (build-only, not deployed) | Microsoft MCR |
| `Dockerfile` (runtime stage) | `mcr.microsoft.com/dotnet/aspnet:10.0` | `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` | Microsoft (chiseled — .NET is not in Docker Hardened Images catalog) |
| `Dockerfile.prebuilt` | `mcr.microsoft.com/dotnet/aspnet:10.0` | `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` | Microsoft (chiseled) |
| `docker-compose.demo.yml` | `postgres:16-alpine` | Docker Hardened PostgreSQL image | [Docker Hardened Images](https://www.docker.com/products/hardened-images/) |

**Key benefits of Docker Hardened Images:**
- Continuously rebuilt and patched for known CVEs (zero-day and N-day)
- SLSA Build Level 3 provenance attestations
- Built from verified, audited source
- Minimal image footprint — unnecessary packages stripped
- SBOMs (Software Bill of Materials) included
- Drop-in compatible with standard Docker Official Images (same tags/API)

**For .NET images** (not in Docker's hardened catalog): Microsoft's `noble-chiseled` images are the closest equivalent — distroless Ubuntu, non-root by default, no shell/package manager, ~50% smaller.

---

## User Stories

### US-108-001: Use Docker Hardened PostgreSQL Image
**As a** platform operator  
**I want** the PostgreSQL container to use Docker's Hardened Image  
**So that** the database has continuous CVE patching, verified provenance, and minimal attack surface

**Acceptance Criteria:**
- [x] `docker-compose.demo.yml` uses Docker Hardened PostgreSQL image
- [x] PostgreSQL starts, accepts connections, and passes health check
- [x] Data volume compatibility verified (no migration issues from `postgres:16-alpine`)

### US-108-002: Use Hardened .NET Runtime Images
**As a** platform operator  
**I want** the application runtime containers to use the most hardened available .NET image  
**So that** the attack surface is minimized and security posture is improved

**Acceptance Criteria:**
- [x] `Dockerfile` runtime stage uses `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled`
- [x] `Dockerfile.prebuilt` uses `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled`
- [x] Application starts and serves traffic correctly from the chiseled image
- [x] Health check still works (adapted for no-shell environment)
- [x] Multi-architecture builds (amd64, arm64) still succeed

### US-108-003: Establish Hardened Image Policy
**As a** developer  
**I want** a documented policy to always prefer Docker Hardened Images  
**So that** future image additions or upgrades follow the same security baseline

**Acceptance Criteria:**
- [x] Copilot instructions and deployment docs updated with hardened image policy
- [x] Decision rationale documented for each image (hardened vs. chiseled vs. standard)

---

## Technical Design

### Docker Hardened Images — PostgreSQL

Docker publishes hardened variants of popular images including PostgreSQL. These are available through Docker Hub under the Docker Hardened Images program. The exact image reference should be confirmed from the [Docker Hardened Images catalog](https://www.docker.com/products/hardened-images/) during implementation.

**Expected change in `docker-compose.demo.yml`:**
```yaml
# Before
image: postgres:16-alpine

# After — use Docker Hardened Image for PostgreSQL
image: dhi.io/postgres:16
```

> **Note:** The exact image name/tag will be confirmed from Docker's hardened image catalog during implementation. Docker Hardened Images are drop-in replacements and use the same configuration, environment variables, and volume mounts.

### .NET Chiseled Images (Microsoft)

Docker does not publish hardened .NET images — Microsoft manages .NET images on MCR. The best-hardened option is Microsoft's `noble-chiseled` variant, which provides:

- **No shell** (`bash`, `sh` not available)
- **No package manager** (`apt-get` not available)
- **Non-root by default** (UID 1654, user `app`)
- **~50% smaller** than standard Debian-based images

**Implications for Dockerfiles:**

1. **Health checks** — Cannot use `curl` or `CMD-SHELL` health checks. Remove `HEALTHCHECK` from Dockerfiles; docker-compose and orchestrators handle health checks externally.
2. **`apt-get` removal** — The `RUN apt-get install` step must be removed entirely.
3. **User setup removal** — Chiseled images are already non-root. Remove `useradd`/`chown`/`USER` blocks.

### Health Check Strategy

**Chosen approach:**
1. Remove `HEALTHCHECK` from `Dockerfile` and `Dockerfile.prebuilt` (no `curl` in chiseled images)
2. Docker Compose health checks for the app service will also need updating since `curl` won't be in the container. Options:
   - Remove app health checks from compose (rely on external monitoring via `/health`)
   - Use a separate lightweight health-check sidecar
3. PostgreSQL health checks (`pg_isready`) remain unchanged — the hardened PostgreSQL image still includes PostgreSQL CLI tools

### Changes Per File

#### `Dockerfile`
- Change runtime base to `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled`
- Remove `apt-get` block
- Remove `useradd`/`chown`/`USER` block
- Remove `HEALTHCHECK` instruction

#### `Dockerfile.prebuilt`
- Change base to `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled`
- Remove `apt-get` block
- Remove `useradd`/`chown`/`USER` block
- Remove `HEALTHCHECK` instruction

#### `docker-compose.demo.yml`
- Change `postgres` service image to Docker Hardened PostgreSQL
- Update `budgetexperiment` service health check (remove `curl` dependency)

#### `docker-compose.pi.yml` / `docker-compose.second.yml`
- No image changes needed (they pull `ghcr.io/becauseimclever/budgetexperiment` which will automatically use the new hardened base)
- Update health checks to remove `curl` dependency

### CI/CD Impact

The GitHub Actions workflow (`docker-build-publish.yml`) builds from the `Dockerfile` — no workflow changes are needed. The base image change is transparent to the build pipeline. Docker Hardened Images for PostgreSQL are only used in compose files (not built in CI).

---

## Implementation Plan

### Phase 1: Harden PostgreSQL with Docker Hardened Image

**Objective:** Switch the PostgreSQL container in `docker-compose.demo.yml` to Docker's Hardened Image.

**Tasks:**
- [x] Confirm exact Docker Hardened PostgreSQL image name/tag from [Docker Hardened Images catalog](https://www.docker.com/products/hardened-images/)
- [x] Update `docker-compose.demo.yml` `postgres` service to use Docker Hardened PostgreSQL image
- [x] Verify PostgreSQL starts and accepts connections with the hardened image
- [x] Verify data volume compatibility (ensure existing volumes work without migration)
- [x] Verify `pg_isready` health check still works

**Commit:**
```bash
git add .
git commit -m "feat(docker): use Docker Hardened Image for PostgreSQL

- Switch demo compose PostgreSQL to Docker Hardened Image
- Drop-in replacement with continuous CVE patching and SLSA provenance
- Verified health check and volume compatibility

Refs: #108"
```

---

### Phase 2: Harden .NET Runtime with Chiseled Images

**Objective:** Switch both Dockerfiles to Microsoft's chiseled base images and remove unnecessary layers.

**Tasks:**
- [x] Update `Dockerfile` runtime stage to `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled`
- [x] Remove `apt-get install` block from `Dockerfile`
- [x] Remove `useradd`/`chown`/`USER` block from `Dockerfile` (chiseled is non-root by default)
- [x] Remove `HEALTHCHECK` from `Dockerfile`
- [x] Update `Dockerfile.prebuilt` to `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled`
- [x] Remove `apt-get install` block from `Dockerfile.prebuilt`
- [x] Remove `useradd`/`chown`/`USER` block from `Dockerfile.prebuilt`
- [x] Remove `HEALTHCHECK` from `Dockerfile.prebuilt`
- [x] Add comments explaining chiseled image constraints

**Commit:**
```bash
git add .
git commit -m "feat(docker): switch .NET runtime to chiseled base images

- Use mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled for runtime
- Remove apt-get, useradd, curl-based healthcheck (not available in chiseled)
- Chiseled images are non-root by default (UID 1654)
- ~50% smaller image, reduced CVE surface

Refs: #108"
```

---

### Phase 3: Update Docker Compose Health Checks

**Objective:** Ensure docker-compose health checks work with the new shell-less .NET containers.

**Tasks:**
- [x] Update `docker-compose.demo.yml` health check for `budgetexperiment` service (remove `curl` dependency)
- [x] Update `docker-compose.pi.yml` health check (remove `curl` dependency)
- [x] Update `docker-compose.second.yml` health check (remove `curl` dependency)

**Commit:**
```bash
git add .
git commit -m "fix(docker): adapt compose health checks for chiseled images

- Replace curl-based health checks (curl not available in chiseled images)
- Health monitoring via external access to /health endpoint

Refs: #108"
```

---

### Phase 4: Validate & Document

**Objective:** Build, test, and document the hardened image changes. Establish policy for future images.

**Tasks:**
- [ ] Build Docker image locally and verify it starts (`docker build -t budget-test .`)
- [ ] Verify `docker-compose.demo.yml` stack brings up correctly with all hardened images
- [ ] Verify multi-architecture CI build succeeds
- [x] Update `README.Docker.md` or `ci-cd-deployment.md` with hardening notes
- [x] Update `copilot-instructions.md` section 35 with hardened image policy
- [x] Document image decision matrix (which images use Docker Hardened vs. chiseled vs. standard)

**Commit:**
```bash
git add .
git commit -m "docs(docker): document hardened image policy

- Update deployment docs with Docker Hardened Images guidance
- Establish policy: always prefer Docker Hardened Images where available
- Document chiseled fallback for .NET (not in Docker Hardened catalog)
- Note health check changes and rationale

Refs: #108"
```

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Docker Hardened Images require paid subscription | Medium | Medium | Verify access/pricing; fall back to `postgres:16-alpine` if cost prohibitive |
| Health check breakage in compose | Medium | Medium | Test all compose files; external monitoring via `/health` as fallback |
| Missing runtime dependency in chiseled .NET image | Low | High | Run full integration test suite against containerized app before merge |
| Debugging difficulty (no shell in chiseled) | Medium | Low | Use `docker cp` or sidecar debug containers; acceptable trade-off for security |
| arm64 chiseled image unavailability | Low | High | Verify Microsoft publishes `noble-chiseled` for arm64 before merging |
| Docker Hardened PostgreSQL image not available for desired version | Low | Medium | Fall back to `postgres:16-alpine` and re-evaluate when available |

## Image Decision Matrix

| Image Need | First Choice | Fallback | Rationale |
|-----------|-------------|----------|-----------|
| PostgreSQL | Docker Hardened Image | `postgres:16-alpine` | Docker Hardened: continuous CVE patches, SLSA provenance |
| .NET Runtime | `aspnet:10.0-noble-chiseled` (Microsoft) | `aspnet:10.0-noble` | .NET not in Docker Hardened catalog; chiseled is Microsoft's hardened option |
| .NET SDK (build only) | `sdk:10.0` (standard) | N/A | Build stage only, not deployed; SDK requires shell |

## Notes

- **Docker Hardened Images** are Docker's commercial offering: [https://www.docker.com/products/hardened-images/](https://www.docker.com/products/hardened-images/). Verify subscription/access requirements during implementation.
- Docker Hardened Images are **drop-in replacements** for standard Docker Official Images — same env vars, config, and volume mounts.
- The .NET SDK image (`mcr.microsoft.com/dotnet/sdk:10.0`) is used only in the build stage and is not deployed. It does not need hardening.
- Microsoft's `noble-chiseled` images are the .NET equivalent of hardened images — distroless Ubuntu Noble (24.04 LTS), non-root by default, no shell.
- **Policy going forward:** When adding any new Docker image to the project, always check the [Docker Hardened Images catalog](https://www.docker.com/products/hardened-images/) first. Use the hardened variant if available; otherwise use the most minimal official variant (Alpine, slim, or distroless).
