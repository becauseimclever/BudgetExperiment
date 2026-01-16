# Feature XXX: [Feature Title]

> **Instructions:** Copy this template for new features. Replace `XXX` with the next feature number. Delete this instruction block and all `<!-- comments -->` after filling in.

## Overview

<!-- 1-2 paragraph summary of what this feature accomplishes and why it matters -->

## Problem Statement

<!-- Describe the current limitation or pain point this feature addresses -->

### Current State

<!-- What exists today? What's missing? -->

### Target State

<!-- What will the application look like after this feature is complete? -->

---

## User Stories

### [Story Group Name]

#### US-XXX-001: [Story Title]
**As a** [user type]  
**I want to** [action]  
**So that** [benefit]

**Acceptance Criteria:**
- [ ] Criterion 1
- [ ] Criterion 2

<!-- Add more user stories as needed -->

---

## Technical Design

### Architecture Changes

<!-- Describe any architectural changes, new components, or structural modifications -->

### Domain Model

<!-- New entities, value objects, or domain changes -->

```csharp
// Example domain model code
```

### API Endpoints

<!-- New or modified endpoints -->

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/resource` | Description |
| POST | `/api/v1/resource` | Description |

### Database Changes

<!-- New tables, columns, migrations -->

### UI Components

<!-- New pages, components, or UI changes -->

---

## Implementation Plan

<!-- 
Each phase should be a logical, testable unit of work.
Follow TDD: write tests first, then implement.
Create a commit after each phase using conventional commit format.
-->

### Phase 1: [Phase Title]

**Objective:** <!-- What this phase accomplishes -->

**Tasks:**
- [ ] Task 1
- [ ] Task 2
- [ ] Write unit tests
- [ ] Implementation

**Commit:**
```bash
git add .
git commit -m "feat(scope): brief description of phase 1

- Detail 1
- Detail 2

Refs: #XXX"
```

---

### Phase 2: [Phase Title]

**Objective:** <!-- What this phase accomplishes -->

**Tasks:**
- [ ] Task 1
- [ ] Task 2
- [ ] Write unit tests
- [ ] Implementation

**Commit:**
```bash
git add .
git commit -m "feat(scope): brief description of phase 2

- Detail 1
- Detail 2

Refs: #XXX"
```

---

### Phase 3: [Phase Title]

**Objective:** <!-- What this phase accomplishes -->

**Tasks:**
- [ ] Task 1
- [ ] Task 2
- [ ] Write integration tests
- [ ] Implementation

**Commit:**
```bash
git add .
git commit -m "feat(scope): brief description of phase 3

- Detail 1
- Detail 2

Refs: #XXX"
```

---

### Phase N: Documentation & Cleanup

**Objective:** Final polish, documentation updates, and cleanup

**Tasks:**
- [ ] Update API documentation / OpenAPI specs
- [ ] Add/update XML comments for public APIs
- [ ] Update README if needed
- [ ] Remove any TODO comments
- [ ] Final code review

**Commit:**
```bash
git add .
git commit -m "docs(scope): add documentation for feature XXX

- XML comments for public API
- Update README
- OpenAPI spec updates

Refs: #XXX"
```

---

## Conventional Commit Reference

Use these commit types to ensure proper changelog generation:

| Type | When to Use | SemVer Impact | Example |
|------|-------------|---------------|---------|
| `feat` | New feature or capability | Minor | `feat(budget): add category management` |
| `fix` | Bug fix | Patch | `fix(api): correct pagination header` |
| `docs` | Documentation only | None | `docs: update API examples` |
| `style` | Formatting, no logic change | None | `style: fix indentation` |
| `refactor` | Code restructure, no feature/fix | None | `refactor(domain): extract value object` |
| `perf` | Performance improvement | Patch | `perf(query): optimize category lookup` |
| `test` | Adding or fixing tests | None | `test(budget): add category validation tests` |
| `chore` | Build, CI, dependencies | None | `chore: update NuGet packages` |

### Breaking Changes

For breaking changes, append `!` after the type:

```bash
git commit -m "feat(api)!: redesign category endpoints

BREAKING CHANGE: Category endpoints now use /api/v2/categories
- Old /api/v1/categories endpoints deprecated
- New request/response format

Refs: #XXX"
```

### Scope Suggestions

Use consistent scopes across the project:

| Scope | Description |
|-------|-------------|
| `domain` | Domain model, entities, value objects |
| `api` | API controllers, endpoints |
| `client` | Blazor UI components |
| `infra` | Infrastructure, database, repositories |
| `app` | Application services |
| `auth` | Authentication/authorization |
| `budget` | Budget-specific features |
| `transaction` | Transaction-specific features |
| `recurring` | Recurring items features |

---

## Testing Strategy

### Unit Tests

<!-- List key unit test scenarios -->

- [ ] Test scenario 1
- [ ] Test scenario 2

### Integration Tests

<!-- List integration test scenarios -->

- [ ] Test scenario 1
- [ ] Test scenario 2

### Manual Testing Checklist

<!-- Steps for manual verification -->

- [ ] Step 1
- [ ] Step 2

---

## Migration Notes

<!-- If this feature requires data migration or special upgrade steps -->

### Database Migration

```bash
dotnet ef migrations add FeatureXXX_Description --project src/BudgetExperiment.Infrastructure --startup-project src/BudgetExperiment.Api
```

### Breaking Changes

<!-- List any breaking changes and migration path -->

---

## Security Considerations

<!-- Security implications of this feature -->

---

## Performance Considerations

<!-- Performance implications and optimizations -->

---

## Future Enhancements

<!-- Out of scope items that could be added later -->

---

## References

<!-- Links to relevant documentation, specs, or discussions -->

- [Related Feature Doc](./0XX-related-feature.md)
- [External Reference](https://example.com)

---

## Changelog

<!-- Track major decisions and changes to this document -->

| Date | Change | Author |
|------|--------|--------|
| YYYY-MM-DD | Initial draft | @username |
