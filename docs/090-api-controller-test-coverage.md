# Feature 090: Test Coverage Gaps — API Controllers

> **Status:** Planning
> **Priority:** Low (2 controllers untested — 93% coverage already)
> **Dependencies:** None

## Overview

A test coverage audit found 2 API controllers without test files. All other 25 controllers have comprehensive test coverage.

## Problem Statement

### Current State

**Untested Controllers:**
1. `MerchantMappingsController` — CRUD for learned merchant-to-category mappings
2. `VersionController` — Returns application version info

### Target State

All 27 controllers have corresponding test files using `WebApplicationFactory`.

---

## User Stories

### US-090-001: Test MerchantMappingsController
**As a** developer  
**I want** MerchantMappingsController to have integration tests  
**So that** merchant mapping CRUD endpoints are verified.

**Acceptance Criteria:**
- [ ] `MerchantMappingsControllerTests.cs` created
- [ ] Tests cover all HTTP methods (GET, POST, PUT, DELETE as applicable)
- [ ] Tests cover happy path and failure cases (404, 400)
- [ ] All tests pass

### US-090-002: Test VersionController
**As a** developer  
**I want** VersionController to have integration tests  
**So that** the version endpoint is verified.

**Acceptance Criteria:**
- [ ] `VersionControllerTests.cs` created
- [ ] Tests verify response shape and status code
- [ ] All tests pass

---

## Implementation Plan

### Phase 1: MerchantMappingsController Tests

**Tasks:**
- [ ] Create `MerchantMappingsControllerTests.cs`
- [ ] Test GET (list), GET (by id), POST (create), PUT (update), DELETE (remove)
- [ ] Test 404 for non-existent mappings
- [ ] All tests pass

### Phase 2: VersionController Tests

**Tasks:**
- [ ] Create `VersionControllerTests.cs`
- [ ] Test GET returns 200 with version info
- [ ] All tests pass
