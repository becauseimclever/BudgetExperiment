# Feature 024: Auto-Categorization Rules Engine

## Overview

Implement an intelligent auto-categorization system that automatically assigns categories to transactions based on user-defined rules. The rules engine allows users to create, prioritize, and manage categorization rules that match transaction descriptions against patterns (exact, contains, regex) and assign appropriate budget categories. This feature reduces manual categorization effort and ensures consistent transaction organization.

## Problem Statement

Currently, transactions have a free-form category text field that requires manual entry for each transaction. This presents several challenges:

### Current State

- Users must manually categorize each transaction
- Imported transactions (CSV) come in without categories
- No mechanism exists to recognize recurring merchants/vendors
- Inconsistent categorization leads to inaccurate budget reports
- High friction when processing many transactions (e.g., monthly imports)

### Target State

- Transactions are automatically categorized on creation/import based on matching rules
- Users define rules once, and they apply to all future matching transactions
- Rules can match on description patterns (exact, contains, starts with, regex)
- Rules are prioritized so the most specific rule wins
- Users can bulk-apply rules to existing uncategorized transactions
- Manual categorization still available for edge cases

---

## User Stories

### Rule Management

#### US-024-001: Create Categorization Rule
**As a** user  
**I want to** create a rule that matches transaction descriptions to categories  
**So that** future transactions are automatically categorized

**Acceptance Criteria:**
- [ ] Can specify match type (exact, contains, starts with, ends with, regex)
- [ ] Can specify the pattern to match
- [ ] Can select target category (from existing BudgetCategory list)
- [ ] Can set rule priority (higher priority = evaluated first)
- [ ] Rule is validated before saving (valid regex, category exists)

#### US-024-002: Edit Categorization Rule
**As a** user  
**I want to** edit an existing categorization rule  
**So that** I can refine matching criteria over time

**Acceptance Criteria:**
- [ ] Can modify match type, pattern, category, and priority
- [ ] Changes do not affect already-categorized transactions
- [ ] Validation ensures rule remains valid

#### US-024-003: Delete Categorization Rule
**As a** user  
**I want to** delete rules I no longer need  
**So that** my rule set stays manageable

**Acceptance Criteria:**
- [ ] Rule is soft-deleted (can be restored)
- [ ] Existing transaction categories are not affected

#### US-024-004: View All Rules
**As a** user  
**I want to** see all my categorization rules in one place  
**So that** I can manage and organize them

**Acceptance Criteria:**
- [ ] Rules displayed in priority order
- [ ] Shows match type, pattern, target category
- [ ] Can filter by category
- [ ] Can search rules by pattern

#### US-024-005: Reorder Rule Priority
**As a** user  
**I want to** drag-and-drop rules to change priority  
**So that** more specific rules are evaluated first

**Acceptance Criteria:**
- [ ] Drag-and-drop reordering in UI
- [ ] Priority numbers update automatically
- [ ] Changes persist immediately

### Auto-Categorization

#### US-024-006: Auto-Categorize on Transaction Create
**As a** user  
**I want to** have new transactions automatically categorized  
**So that** I don't need to manually categorize each one

**Acceptance Criteria:**
- [ ] When a transaction is created without a category, rules are evaluated
- [ ] First matching rule (by priority) assigns the category
- [ ] If no rule matches, category remains null
- [ ] Manual category override is preserved (not replaced by rules)

#### US-024-007: Auto-Categorize on Import
**As a** user  
**I want to** have imported transactions automatically categorized  
**So that** bulk imports are immediately organized

**Acceptance Criteria:**
- [ ] CSV import applies categorization rules to all imported transactions
- [ ] Import summary shows how many were auto-categorized

#### US-024-008: Bulk Apply Rules to Existing Transactions
**As a** user  
**I want to** apply rules to existing uncategorized transactions  
**So that** I can categorize historical data

**Acceptance Criteria:**
- [ ] Button to "Apply Rules to Uncategorized"
- [ ] Can optionally include already-categorized transactions (re-categorize)
- [ ] Shows preview of how many will be affected
- [ ] Confirmation before applying
- [ ] Summary of results after completion

#### US-024-009: Create Rule from Transaction
**As a** user  
**I want to** create a rule directly from a transaction  
**So that** similar transactions are auto-categorized in the future

**Acceptance Criteria:**
- [ ] Right-click or action menu on transaction
- [ ] Pre-fills pattern from transaction description
- [ ] Pre-fills category from transaction (if set)
- [ ] Can adjust before saving

### Rule Testing

#### US-024-010: Test Rule Before Saving
**As a** user  
**I want to** test a rule against my transactions  
**So that** I know it will match what I expect

**Acceptance Criteria:**
- [ ] Test button shows which existing transactions would match
- [ ] Shows count and sample descriptions
- [ ] Helps validate regex patterns before saving

---

## Technical Design

### Architecture Changes

- New `CategorizationRule` entity in Domain
- New `ICategorizationRuleRepository` interface
- New `ICategorizationEngine` service interface and implementation
- Integration with Transaction creation flow
- Integration with CSV import service
- New API endpoints for rule CRUD and categorization actions
- New Blazor components for rule management UI

### Domain Model

#### CategorizationRule Entity

```csharp
public sealed class CategorizationRule
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public RuleMatchType MatchType { get; private set; }
    public string Pattern { get; private set; } = string.Empty;
    public Guid CategoryId { get; private set; }
    public int Priority { get; private set; }  // Lower number = higher priority (evaluated first)
    public bool IsActive { get; private set; } = true;
    public bool CaseSensitive { get; private set; } = false;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    // Navigation (for queries)
    public BudgetCategory Category { get; private set; } = null!;

    public static CategorizationRule Create(
        string name,
        RuleMatchType matchType,
        string pattern,
        Guid categoryId,
        int priority = 100,
        bool caseSensitive = false);
    
    public void Update(
        string name,
        RuleMatchType matchType,
        string pattern,
        Guid categoryId,
        bool caseSensitive);
    
    public void SetPriority(int priority);
    public void Deactivate();
    public void Activate();
    public bool Matches(string description);
}

public enum RuleMatchType
{
    Exact,       // Description exactly matches pattern
    Contains,    // Description contains pattern
    StartsWith,  // Description starts with pattern
    EndsWith,    // Description ends with pattern
    Regex        // Pattern is a regular expression
}
```

#### CategorizationEngine Service Interface

```csharp
public interface ICategorizationEngine
{
    /// <summary>
    /// Finds the best matching category for a transaction description.
    /// </summary>
    /// <param name="description">The transaction description to match.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The category ID if a rule matches, null otherwise.</returns>
    Task<Guid?> FindMatchingCategoryAsync(string description, CancellationToken ct = default);

    /// <summary>
    /// Applies categorization rules to multiple transactions.
    /// </summary>
    /// <param name="transactionIds">The transactions to categorize.</param>
    /// <param name="overwriteExisting">If true, replaces existing categories.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result with counts of categorized transactions.</returns>
    Task<CategorizationResult> ApplyRulesAsync(
        IEnumerable<Guid> transactionIds,
        bool overwriteExisting = false,
        CancellationToken ct = default);

    /// <summary>
    /// Tests a rule pattern against existing transactions without applying.
    /// </summary>
    /// <param name="matchType">The match type to test.</param>
    /// <param name="pattern">The pattern to test.</param>
    /// <param name="caseSensitive">Whether matching is case-sensitive.</param>
    /// <param name="limit">Maximum transactions to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of matching transaction descriptions.</returns>
    Task<IReadOnlyList<string>> TestPatternAsync(
        RuleMatchType matchType,
        string pattern,
        bool caseSensitive,
        int limit = 10,
        CancellationToken ct = default);
}

public sealed record CategorizationResult
{
    public int TotalProcessed { get; init; }
    public int Categorized { get; init; }
    public int Skipped { get; init; }
    public int Errors { get; init; }
    public IReadOnlyList<string> ErrorMessages { get; init; } = Array.Empty<string>();
}
```

#### Repository Interface

```csharp
public interface ICategorizationRuleRepository : IReadRepository<CategorizationRule>, IWriteRepository<CategorizationRule>
{
    Task<IReadOnlyList<CategorizationRule>> GetActiveByPriorityAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CategorizationRule>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default);
    Task<int> GetNextPriorityAsync(CancellationToken ct = default);
    Task ReorderPrioritiesAsync(IEnumerable<(Guid RuleId, int NewPriority)> priorities, CancellationToken ct = default);
}
```

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/categorization-rules` | Get all rules (filtered, paginated) |
| GET | `/api/v1/categorization-rules/{id}` | Get single rule by ID |
| POST | `/api/v1/categorization-rules` | Create a new rule |
| PUT | `/api/v1/categorization-rules/{id}` | Update an existing rule |
| DELETE | `/api/v1/categorization-rules/{id}` | Soft-delete a rule |
| POST | `/api/v1/categorization-rules/{id}/activate` | Activate a rule |
| POST | `/api/v1/categorization-rules/{id}/deactivate` | Deactivate a rule |
| PUT | `/api/v1/categorization-rules/reorder` | Bulk update priorities |
| POST | `/api/v1/categorization-rules/test` | Test a pattern against transactions |
| POST | `/api/v1/transactions/apply-rules` | Apply rules to transactions |

### Request/Response DTOs

```csharp
public sealed record CategorizationRuleDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string MatchType { get; init; } = string.Empty;
    public string Pattern { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public int Priority { get; init; }
    public bool IsActive { get; init; }
    public bool CaseSensitive { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}

public sealed record CreateCategorizationRuleRequest
{
    public string Name { get; init; } = string.Empty;
    public string MatchType { get; init; } = string.Empty;  // Exact, Contains, StartsWith, EndsWith, Regex
    public string Pattern { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public int? Priority { get; init; }  // Optional, auto-assigned if null
    public bool CaseSensitive { get; init; } = false;
}

public sealed record UpdateCategorizationRuleRequest
{
    public string Name { get; init; } = string.Empty;
    public string MatchType { get; init; } = string.Empty;
    public string Pattern { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public bool CaseSensitive { get; init; }
}

public sealed record ReorderRulesRequest
{
    public IReadOnlyList<RulePriorityItem> Rules { get; init; } = Array.Empty<RulePriorityItem>();
}

public sealed record RulePriorityItem
{
    public Guid RuleId { get; init; }
    public int Priority { get; init; }
}

public sealed record TestPatternRequest
{
    public string MatchType { get; init; } = string.Empty;
    public string Pattern { get; init; } = string.Empty;
    public bool CaseSensitive { get; init; }
    public int Limit { get; init; } = 10;
}

public sealed record TestPatternResponse
{
    public int MatchCount { get; init; }
    public IReadOnlyList<string> SampleDescriptions { get; init; } = Array.Empty<string>();
}

public sealed record ApplyRulesRequest
{
    public IReadOnlyList<Guid>? TransactionIds { get; init; }  // Null = all uncategorized
    public bool OverwriteExisting { get; init; } = false;
}

public sealed record ApplyRulesResponse
{
    public int TotalProcessed { get; init; }
    public int Categorized { get; init; }
    public int Skipped { get; init; }
}
```

### Database Changes

New table: `CategorizationRules`

| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| Name | varchar(200) | NOT NULL |
| MatchType | int | NOT NULL |
| Pattern | varchar(500) | NOT NULL |
| CategoryId | uuid | FK to BudgetCategories |
| Priority | int | NOT NULL |
| IsActive | boolean | NOT NULL DEFAULT true |
| CaseSensitive | boolean | NOT NULL DEFAULT false |
| CreatedAtUtc | timestamp | NOT NULL |
| UpdatedAtUtc | timestamp | NOT NULL |

Index: `IX_CategorizationRules_Priority` (IsActive, Priority)
Index: `IX_CategorizationRules_CategoryId` (CategoryId)

### UI Components

#### New Pages

- `/rules` - Categorization Rules management page

#### New Components

- `RuleList.razor` - Displays all rules with drag-drop reordering
- `RuleEditor.razor` - Create/edit rule dialog
- `RuleTestPanel.razor` - Test pattern matching
- `ApplyRulesDialog.razor` - Bulk apply rules confirmation

#### Modified Components

- `TransactionRow.razor` - Add "Create Rule" context menu item
- `TransactionList.razor` - Add "Apply Rules" bulk action
- `ImportTransactions.razor` - Show auto-categorization results

---

## Implementation Plan

### Phase 1: Domain Model ✅

**Objective:** Establish the CategorizationRule entity and core domain logic

**Tasks:**
- [x] Write unit tests for `RuleMatchType` enum
- [x] Write unit tests for `CategorizationRule.Create()` validation
- [x] Write unit tests for `CategorizationRule.Matches()` method (all match types)
- [x] Implement `RuleMatchType` enum
- [x] Implement `CategorizationRule` entity with factory methods
- [x] Implement pattern matching logic for each match type
- [x] Add `ICategorizationRuleRepository` interface

**Commit:**
```bash
git add .
git commit -m "feat(domain): add CategorizationRule entity and matching logic

- RuleMatchType enum (Exact, Contains, StartsWith, EndsWith, Regex)
- CategorizationRule entity with Create, Update, Matches methods
- ICategorizationRuleRepository interface
- Unit tests for all match type scenarios

Refs: #024"
```

---

### Phase 2: Infrastructure - Repository & Migrations ✅

**Objective:** Implement database persistence for categorization rules

**Tasks:**
- [x] Create EF Core configuration for `CategorizationRule`
- [x] Add migration for `CategorizationRules` table
- [x] Implement `CategorizationRuleRepository`
- [x] Write integration tests for repository operations

**Commit:**
```bash
git add .
git commit -m "feat(infra): add CategorizationRule persistence

- EF Core entity configuration
- Database migration for CategorizationRules table
- CategorizationRuleRepository implementation
- Integration tests for CRUD operations

Refs: #024"
```

---

### Phase 3: Application Service - Categorization Engine ✅ COMPLETE

**Objective:** Implement the core categorization engine service

**Tasks:**
- [x] Write unit tests for `CategorizationEngine` with mocked repository
- [x] Implement `ICategorizationEngine` interface
- [x] Implement `CategorizationEngine` service
- [x] Test pattern matching with priority ordering
- [x] Test bulk categorization logic

**Commit:**
```bash
git add .
git commit -m "feat(app): implement CategorizationEngine service

- ICategorizationEngine interface
- FindMatchingCategoryAsync for single descriptions
- ApplyRulesAsync for bulk categorization
- TestPatternAsync for rule preview
- Unit tests with mocked dependencies

Refs: #024"
```

---

### Phase 4: Integration with Transaction Creation ✅ COMPLETE

**Objective:** Auto-categorize transactions when created

**Tasks:**
- [x] Modify `TransactionService.CreateAsync` to invoke categorization engine
- [x] Update tests for transaction creation with auto-categorization
- [x] Ensure manual category is not overwritten
- [x] Update import service to apply rules after import (N/A - no import service exists yet)

**Commit:**
```bash
git add .
git commit -m "feat(app): integrate auto-categorization into transaction creation

- TransactionService calls CategorizationEngine on create
- Import service applies rules post-import
- Manual categories preserved
- Updated unit tests

Refs: #024"
```

---

### Phase 5: API Endpoints ✅

**Objective:** Expose categorization rules via REST API

**Tasks:**
- [x] Add DTOs to Contracts project
- [x] Implement `CategorizationRulesController`
- [x] Add endpoint for testing patterns
- [x] Add endpoint for bulk applying rules
- [x] Write API integration tests

**Commit:**
```bash
git add .
git commit -m "feat(api): add categorization rules endpoints

- GET/POST/PUT/DELETE for rules CRUD
- POST /test for pattern testing
- POST /transactions/apply-rules for bulk categorization
- PUT /reorder for priority management
- API integration tests

Refs: #024"
```

---

### Phase 6: Client - Rule Management UI ✅

**Objective:** Build the Blazor UI for managing rules

**Tasks:**
- [x] Create `RuleList` component with data table
- [x] Create `RuleEditor` dialog component
- [x] Implement drag-drop reordering
- [x] Add rules page to navigation
- [x] Wire up to API client service

**Commit:**
```bash
git add .
git commit -m "feat(client): add categorization rules management UI

- RuleList component with DataGrid
- RuleEditor dialog for create/edit
- Drag-drop priority reordering
- Navigation link to /rules page

Refs: #024"
```

---

### Phase 7: Client - Rule Testing & Bulk Actions ✅

**Objective:** Implement pattern testing and bulk categorization UI

**Tasks:**
- [x] Create `RuleTestPanel` component
- [x] Create `ApplyRulesDialog` component
- [x] Add "Create Rule" action to transaction context menu
- [x] Add "Apply Rules" bulk action to transaction list
- [x] Update import summary to show categorization results

**Commit:**
```bash
git add .
git commit -m "feat(client): add rule testing and bulk categorization UI

- RuleTestPanel for pattern preview
- ApplyRulesDialog with confirmation
- Create Rule from transaction context menu
- Apply Rules bulk action
- Import summary shows categorization stats

Refs: #024"
```

---

### Phase 8: Documentation & Cleanup ✅

**Objective:** Final polish, documentation updates, and cleanup

**Tasks:**
- [x] Update API documentation / OpenAPI specs
- [x] Add XML comments for public APIs
- [x] Update README if needed (N/A - existing README covers new features)
- [x] Remove any TODO comments (none found in feature code)
- [x] Final code review

**Commit:**
```bash
git add .
git commit -m "docs(rules): add documentation for categorization rules feature

- XML comments for public API
- OpenAPI spec updates
- README updates

Refs: #024"
```

---

## Conventional Commit Reference

| Type | When to Use | Example |
|------|-------------|---------|
| `feat` | New feature | `feat(domain): add CategorizationRule entity` |
| `fix` | Bug fix | `fix(rules): correct regex pattern escaping` |
| `test` | Adding tests | `test(rules): add edge case tests for pattern matching` |
| `refactor` | Code cleanup | `refactor(rules): extract pattern matcher strategy` |

---

## Testing Strategy

### Unit Tests

- [ ] `CategorizationRule.Create()` validation (name required, pattern required, valid regex)
- [ ] `CategorizationRule.Matches()` for each `RuleMatchType`
- [ ] `CategorizationRule.Matches()` case sensitivity
- [ ] `CategorizationEngine.FindMatchingCategoryAsync()` priority ordering
- [ ] `CategorizationEngine.FindMatchingCategoryAsync()` no match returns null
- [ ] `CategorizationEngine.ApplyRulesAsync()` respects `overwriteExisting` flag
- [ ] `CategorizationEngine.TestPatternAsync()` returns correct matches

### Integration Tests

- [ ] Repository CRUD operations
- [ ] Repository priority ordering queries
- [ ] API endpoint authorization
- [ ] API endpoint validation (invalid regex, missing category)
- [ ] Full flow: create rule → create transaction → verify category assigned

### Manual Testing Checklist

- [ ] Create rule with each match type and verify matching works
- [ ] Create regex rule with complex pattern
- [ ] Reorder rules via drag-drop
- [ ] Test pattern preview shows correct matches
- [ ] Bulk apply rules to uncategorized transactions
- [ ] Create rule from transaction context menu
- [ ] Import CSV and verify auto-categorization
- [ ] Verify manual category is not overwritten

---

## Migration Notes

### Database Migration

```bash
dotnet ef migrations add Feature024_CategorizationRules --project src/BudgetExperiment.Infrastructure --startup-project src/BudgetExperiment.Api
```

### Breaking Changes

None - this is a new feature that does not modify existing behavior.

---

## Security Considerations

- Rule patterns are user input and must be validated
- Regex patterns should have timeout limits to prevent ReDoS attacks
- Bulk operations should have reasonable limits to prevent resource exhaustion
- Rule management should follow existing authorization patterns

---

## Performance Considerations

- Cache active rules in memory (rules change infrequently)
- Evaluate rules in priority order and stop on first match
- Regex patterns should be compiled and cached
- Bulk categorization should process in batches
- Consider indexing transaction descriptions if pattern matching becomes slow

---

## Future Enhancements

- Machine learning-based category suggestions (analyze past categorizations)
- Rule templates / preset rules for common merchants
- Import rules from other users (shared rule packs)
- Rule statistics (how many transactions matched each rule)
- Time-based rules (e.g., different category for weekend transactions)
- Amount-based rules (e.g., >$500 goes to "Large Purchases")
- Account-specific rules (e.g., credit card vs checking)

---

## References

- [Feature 021: Budget Categories & Goals](./021-budget-categories-goals.md) - Category system this feature integrates with
- [Feature Template](./FEATURE-TEMPLATE.md)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-15 | Initial draft | @copilot |
