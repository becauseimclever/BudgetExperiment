# Feature 118: Upgrade PostgreSQL to Version 18
> **Status:** Done

## Overview

Upgrade the PostgreSQL database from version 16 to version 18 across all Docker Compose files, documentation, and deployment references. PostgreSQL 18 is the latest major release, offering performance improvements, new SQL features, and continued security enhancements.

## Problem Statement

### Current State

The project uses PostgreSQL 16 throughout:

| File | Current Image | Purpose |
|------|---------------|---------|
| `docker-compose.demo.yml` | `dhi.io/postgres:16` | Demo deployment database |
| `docker-compose.pi.yml` | External PostgreSQL 16 (user-managed) | Production database |

Documentation (`DEPLOY-QUICKSTART.md`, `copilot-instructions.md`, `ci-cd-deployment.md`) all reference PostgreSQL 16.

### Target State

All PostgreSQL image references updated to version 18. Documentation reflects the new version. Data migration path documented for existing deployments.

| File | Target Image |
|------|-------------|
| `docker-compose.demo.yml` | `dhi.io/postgres:18` |

---

## User Stories

### US-118-001: Upgrade PostgreSQL Image to 18
**As a** platform operator  
**I want** the PostgreSQL container to run version 18  
**So that** I benefit from the latest performance improvements, SQL features, and security fixes

**Acceptance Criteria:**
- [x] `docker-compose.demo.yml` uses PostgreSQL 18 image
- [x] PostgreSQL 18 starts, accepts connections, and passes health check
- [x] EF Core migrations run successfully against PostgreSQL 18
- [x] Application reads and writes work correctly

### US-118-002: Document Migration Path for Existing Deployments
**As a** platform operator with an existing PostgreSQL 16 database  
**I want** a clear migration guide  
**So that** I can upgrade without data loss

**Acceptance Criteria:**
- [x] Migration/upgrade steps documented for existing deployments
- [x] Breaking changes (if any) between PostgreSQL 16 and 18 identified and addressed
- [x] Rollback guidance provided

### US-118-003: Update All Documentation References
**As a** developer  
**I want** all documentation to reference PostgreSQL 18  
**So that** docs are consistent and accurate

**Acceptance Criteria:**
- [x] `DEPLOY-QUICKSTART.md` references PostgreSQL 18
- [x] `copilot-instructions.md` references PostgreSQL 18
- [x] `ci-cd-deployment.md` references PostgreSQL 18
- [x] Feature doc 108 references updated where relevant

---

## Technical Design

### Compatibility

**Npgsql / EF Core:** The project uses `Npgsql.EntityFrameworkCore.PostgreSQL` version 10.0.0. Npgsql supports PostgreSQL 13–18, so no driver or ORM changes are required.

**Docker Hardened Image:** The `dhi.io` registry publishes PostgreSQL 18 images (confirmed via `docker pull dhi.io/postgres:18-alpine3.22-dev`). The production tag to use is `dhi.io/postgres:18`.

**Breaking Changes (PostgreSQL 16 → 18):**
PostgreSQL major version upgrades require a data directory migration — the on-disk format is not backward-compatible. This affects existing deployments with persistent volumes.

### Data Migration Strategy

For existing deployments with data in PostgreSQL 16 volumes:

1. **Demo deployments** (`docker-compose.demo.yml`): These are ephemeral. Simply `docker compose down -v` to remove the old volume and start fresh with PostgreSQL 18.
2. **Production deployments** (Raspberry Pi): Use `pg_dumpall` to export from PostgreSQL 16, then restore into PostgreSQL 18. Detailed steps in Implementation Plan below.

### Changes Per File

#### `docker-compose.demo.yml`
- Change image from `dhi.io/postgres:16` to `dhi.io/postgres:18`
- Update comments referencing PostgreSQL 16

#### Documentation Updates
- `DEPLOY-QUICKSTART.md` — Update PostgreSQL version reference
- `.github/copilot-instructions.md` — Update hardened image policy reference
- `docs/ci-cd-deployment.md` — Update container security reference

---

## Implementation Plan

### Phase 1: Update Docker Compose Image

**Objective:** Upgrade the PostgreSQL image in `docker-compose.demo.yml` to version 18.

**Tasks:**
- [x] Update `docker-compose.demo.yml` image from `dhi.io/postgres:16` to `dhi.io/postgres:18`
- [x] Update comments in `docker-compose.demo.yml` referencing PostgreSQL 16
- [x] Verify PostgreSQL 18 starts and passes `pg_isready` health check
- [x] Verify EF Core migrations apply cleanly against PostgreSQL 18
- [x] Verify application CRUD operations work correctly

**Commit:**
```bash
git add .
git commit -m "feat(docker): upgrade PostgreSQL to version 18

- Update demo compose to use dhi.io/postgres:18
- PostgreSQL 18 supported by Npgsql 10.0.0 (no driver changes needed)
- Existing demo volumes must be recreated (docker compose down -v)

Refs: #118"
```

---

### Phase 2: Update Documentation

**Objective:** Update all documentation to reference PostgreSQL 18.

**Tasks:**
- [x] Update `DEPLOY-QUICKSTART.md` PostgreSQL version reference
- [x] Update `.github/copilot-instructions.md` hardened image policy
- [x] Update `docs/ci-cd-deployment.md` container security section
- [x] Add migration notes for existing production deployments

**Commit:**
```bash
git add .
git commit -m "docs: update PostgreSQL references from 16 to 18

- Update DEPLOY-QUICKSTART, copilot-instructions, and ci-cd-deployment docs
- Add migration guidance for existing PostgreSQL 16 deployments

Refs: #118"
```

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Data volume incompatibility (major version upgrade) | High | High | Document `pg_dumpall`/restore procedure; demo deployments use `down -v` |
| EF Core / Npgsql incompatibility | Low | High | Npgsql 10.0.0 supports PostgreSQL 13–18; verify with integration tests |
| PostgreSQL 18 behavioral changes break queries | Low | Medium | Run full test suite against PostgreSQL 18 before merging |
| `dhi.io/postgres:18` image not yet available as stable tag | Low | Medium | Fall back to `dhi.io/postgres:17` or standard `postgres:18-alpine` |

## Migration Guide (Existing PostgreSQL 16 Deployments)

### Demo Deployments
```bash
# Remove old volume and start fresh
docker compose -f docker-compose.demo.yml down -v
docker compose -f docker-compose.demo.yml up -d
```

### Production Deployments (Raspberry Pi)
```bash
# 1. Export data from PostgreSQL 16
docker exec budgetexperiment-db pg_dumpall -U budget > backup.sql

# 2. Stop the stack
docker compose -f docker-compose.pi.yml down

# 3. Remove the old PostgreSQL volume (AFTER confirming backup)
docker volume rm <volume_name>

# 4. Update docker-compose to use PostgreSQL 18 (if applicable)
# 5. Start the new stack
docker compose -f docker-compose.pi.yml up -d

# 6. Restore data
docker exec -i budgetexperiment-db psql -U budget < backup.sql
```

## Notes

- PostgreSQL major version upgrades (16 → 18) require data directory migration. The on-disk format changes between major versions.
- `pg_upgrade` is an alternative to dump/restore but requires both PostgreSQL versions installed in the container, which is not practical with hardened/chiseled images.
- The Npgsql driver (`Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.0) supports PostgreSQL versions 13 through 18 — no driver update is needed.
- PostgreSQL 17 was skipped intentionally to go directly to the latest release.
