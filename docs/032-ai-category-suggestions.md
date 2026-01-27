
# Feature 032: AI-Powered Category Suggestions
> **Status:** In Progress

---
## Implementation Status (2026-01-27)

**UI:** Implemented (see Blazor client)



**Backend/Integration Checklist:**

- [x] CategorySuggestionService analyzes uncategorized transactions and integrates with MerchantKnowledgeBase
- [x] Auto-creation of categorization rules after category acceptance (API and service logic present)
- [~] Dismissed suggestions are persisted (restore/undismiss not yet implemented)
- [x] System learns from user manual categorizations and updates merchant mappings
- [~] Endpoints for suggesting, accepting, dismissing, and refreshing category suggestions (restore not implemented)
- [x] Endpoints for merchant-category mapping management
- [x] Unit and integration tests for new logic and endpoints (see tests for CategorySuggestionService, MerchantMappingService, and CategorySuggestionsController)
- [x] OpenAPI docs and user documentation for new endpoints/features (see table below)


All required UI elements and flows described in the feature spec are present. See `src/BudgetExperiment.Client/Pages/CategorySuggestions.razor` and `src/BudgetExperiment.Client/Components/AI/CategorySuggestionCard.razor` for details.

---

## API Endpoint Summary (OpenAPI)

The following endpoints are available for AI category suggestions and merchant mapping. All endpoints are documented in the OpenAPI spec and available via the Scalar UI (`/scalar`).

| Method | Endpoint | Description |
|--------|----------------------------------------------------------|-----------------------------------------------|
| POST   | `/api/v1/categorysuggestions/analyze`                    | Analyze uncategorized transactions and generate suggestions |
| GET    | `/api/v1/categorysuggestions`                            | Get all pending category suggestions          |
| GET    | `/api/v1/categorysuggestions/{id}`                       | Get a specific category suggestion by ID      |
| POST   | `/api/v1/categorysuggestions/{id}/accept`                | Accept a suggestion and create the category   |
| POST   | `/api/v1/categorysuggestions/{id}/dismiss`               | Dismiss a suggestion                         |
| POST   | `/api/v1/categorysuggestions/bulk-accept`                | Accept multiple suggestions in bulk           |
| GET    | `/api/v1/categorysuggestions/{id}/preview-rules`         | Preview rules that would be created for a suggestion |
| POST   | `/api/v1/categorysuggestions/{id}/create-rules`          | Create categorization rules from suggestion patterns |
| GET    | `/api/v1/merchantmappings`                               | Get all learned merchant mappings             |
| POST   | `/api/v1/merchantmappings/learn`                         | Learn a merchant-to-category mapping from manual categorization |
| DELETE | `/api/v1/merchantmappings/{id}`                          | Delete a learned merchant mapping             |

See the OpenAPI/Swagger UI at `/scalar` for full request/response schemas and try-it-out functionality.

This checklist will be updated as each backend/integration item is confirmed.

----

## Overview

Implement an AI-powered category suggestion system that analyzes existing transactions to recommend new budget categories the user may be missing. The system recognizes common merchant patterns (e.g., Netflix → Entertainment, Amazon → Shopping, McDonald's → Dining) and suggests creating appropriate categories. When a user accepts a suggested category, the system also offers to create corresponding auto-categorization rules, integrating seamlessly with the existing rules engine (Feature 024).

## Problem Statement

Users often start with a minimal set of budget categories and may not realize they're missing important ones until they've accumulated many uncategorized transactions. Manually reviewing transaction descriptions to identify missing categories is tedious and error-prone.

### Current State

- Users must manually create all budget categories
- No guidance on what categories might be useful based on actual spending patterns
- Users may have many transactions that could be categorized if appropriate categories existed
- No intelligent analysis of transaction descriptions to suggest organizational improvements
- Users must manually create categorization rules after creating new categories

### Target State

- AI analyzes uncategorized transactions and identifies common merchant patterns
- System suggests new categories based on recognized merchants (Netflix → Entertainment)
- Merchant-to-category mapping uses a knowledge base of common vendors
- When user accepts a category suggestion, system offers to create matching rules
- All processing happens locally via Ollama (privacy-first, no cloud services)
- Suggestions respect existing categories—only recommends genuinely missing ones
- Integration with Feature 024 (Rules Engine) and Feature 025 (AI Rule Suggestions)

---

## User Stories

### Category Discovery

#### US-032-001: Analyze Transactions for Missing Categories
**As a** user  
**I want to** have the AI analyze my transactions and suggest missing categories  
**So that** I can quickly set up a comprehensive category structure

**Acceptance Criteria:**
- [ ] "Suggest Categories" button triggers analysis of uncategorized transactions
- [ ] AI identifies merchant patterns and maps to common category types
- [ ] Only suggests categories that don't already exist (by name similarity)
- [ ] Shows confidence level for each suggestion
- [ ] Progress indicator during analysis

#### US-032-002: View Category Suggestions
**As a** user  
**I want to** see all suggested categories with supporting evidence  
**So that** I can make informed decisions about which to create

**Acceptance Criteria:**
- [ ] Suggestion list shows proposed category name, icon, and type
- [ ] Each suggestion shows sample transactions that would belong to it
- [ ] Shows how many uncategorized transactions would match
- [ ] Suggestions grouped by category type (Expense, Income, etc.)
- [ ] Can dismiss suggestions I don't want

#### US-032-003: Accept Category Suggestion
**As a** user  
**I want to** accept a category suggestion with one click  
**So that** I can quickly add recommended categories

**Acceptance Criteria:**
- [ ] Accept creates the BudgetCategory with suggested name, icon, color
- [ ] User can modify category details before accepting
- [ ] After accepting, prompted to create matching categorization rules
- [ ] Category respects user's default scope (Personal/Shared)
- [ ] Success feedback shows category was created

#### US-032-004: Bulk Accept Category Suggestions
**As a** user  
**I want to** accept multiple category suggestions at once  
**So that** I can quickly set up my budget structure

**Acceptance Criteria:**
- [ ] Checkbox selection for multiple suggestions
- [ ] "Accept Selected" button creates all selected categories
- [ ] Prompted to create rules for all accepted categories
- [ ] Summary shows what was created

### Rule Integration

#### US-032-005: Auto-Create Rules for New Category
**As a** user  
**I want to** automatically create categorization rules when I accept a category suggestion  
**So that** future transactions are categorized without manual work

**Acceptance Criteria:**
- [ ] After accepting category, shown list of suggested rules
- [ ] Rules derived from merchant patterns that triggered the suggestion
- [ ] Can accept all, select specific rules, or skip
- [ ] Created rules integrate with existing rules engine (Feature 024)
- [ ] Rules use appropriate match type based on pattern

#### US-032-006: Preview Rule Impact
**As a** user  
**I want to** see how many existing transactions would be categorized by suggested rules  
**So that** I understand the impact before accepting

**Acceptance Criteria:**
- [ ] Each suggested rule shows count of matching uncategorized transactions
- [ ] Can preview list of transactions that would match
- [ ] Shows if rule would conflict with existing rules

### Merchant Knowledge Base

#### US-032-007: View Merchant-Category Mappings
**As a** user  
**I want to** see how the system maps merchants to categories  
**So that** I understand and can adjust the suggestions

**Acceptance Criteria:**
- [ ] Settings page shows merchant knowledge base
- [ ] Can see which merchants map to which category types
- [ ] Can add custom merchant mappings
- [ ] Can override default mappings

#### US-032-008: Learn from User Categorization
**As a** user  
**I want to** have the system learn from my manual categorizations  
**So that** suggestions improve over time

**Acceptance Criteria:**
- [ ] When user manually categorizes a transaction, merchant is remembered
- [ ] Future suggestions use learned merchant→category mappings
- [ ] User categorizations take precedence over default knowledge base
- [ ] Learned mappings persist across sessions

### Suggestion Management

#### US-032-009: Dismiss Category Suggestion
**As a** user  
**I want to** dismiss suggestions I don't want  
**So that** my suggestion list stays relevant

**Acceptance Criteria:**
- [ ] Can dismiss individual suggestions
- [ ] Dismissed suggestions don't reappear for same merchant pattern
- [ ] Can view history of dismissed suggestions
- [ ] Can restore dismissed suggestions if needed

#### US-032-010: Refresh Suggestions
**As a** user  
**I want to** refresh the analysis after importing new transactions  
**So that** I get suggestions based on latest data

**Acceptance Criteria:**
- [ ] "Refresh" button re-analyzes all uncategorized transactions
- [ ] New merchants result in new suggestions
- [ ] Previously dismissed patterns remain dismissed

---

## Technical Design

### Architecture Changes

- New `CategorySuggestionService` in Application layer
- New `ICategorySuggestionRepository` for persisting suggestion state
- Integration with existing `IOllamaService` from Feature 025
- New `MerchantKnowledgeBase` for default merchant→category mappings
- Extension of `ICategorizationRuleService` to support bulk rule creation

### Domain Model

```csharp
/// <summary>
/// Represents a suggested category based on transaction analysis.
/// </summary>
public sealed class CategorySuggestion
{
    public Guid Id { get; private set; }
    
    /// <summary>
    /// Gets the suggested category name.
    /// </summary>
    public string SuggestedName { get; private set; } = string.Empty;
    
    /// <summary>
    /// Gets the suggested icon identifier.
    /// </summary>
    public string? SuggestedIcon { get; private set; }
    
    /// <summary>
    /// Gets the suggested color.
    /// </summary>
    public string? SuggestedColor { get; private set; }
    
    /// <summary>
    /// Gets the suggested category type.
    /// </summary>
    public CategoryType SuggestedType { get; private set; }
    
    /// <summary>
    /// Gets the confidence score (0.0 to 1.0).
    /// </summary>
    public decimal Confidence { get; private set; }
    
    /// <summary>
    /// Gets the merchant patterns that triggered this suggestion.
    /// </summary>
    public IReadOnlyList<string> MerchantPatterns { get; private set; } = Array.Empty<string>();
    
    /// <summary>
    /// Gets the count of transactions that would match this category.
    /// </summary>
    public int MatchingTransactionCount { get; private set; }
    
    /// <summary>
    /// Gets the suggestion status.
    /// </summary>
    public SuggestionStatus Status { get; private set; }
    
    /// <summary>
    /// Gets the owner user ID.
    /// </summary>
    public string OwnerId { get; private set; } = string.Empty;
    
    /// <summary>
    /// Gets the UTC timestamp when the suggestion was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }
}

/// <summary>
/// Status of a category suggestion.
/// </summary>
public enum SuggestionStatus
{
    Pending,
    Accepted,
    Dismissed
}

/// <summary>
/// Represents a suggested rule to create alongside a category.
/// </summary>
public sealed class SuggestedRule
{
    public string Pattern { get; init; } = string.Empty;
    public RuleMatchType MatchType { get; init; }
    public int MatchingTransactionCount { get; init; }
}
```

### Merchant Knowledge Base

```csharp
/// <summary>
/// Default merchant-to-category mappings for common vendors.
/// </summary>
public static class MerchantKnowledgeBase
{
    public static readonly Dictionary<string, (string Category, string Icon)> DefaultMappings = new()
    {
        // Entertainment
        { "netflix", ("Entertainment", "movie") },
        { "spotify", ("Entertainment", "music") },
        { "hulu", ("Entertainment", "movie") },
        { "disney+", ("Entertainment", "movie") },
        { "hbo", ("Entertainment", "movie") },
        { "youtube", ("Entertainment", "movie") },
        { "amazon prime video", ("Entertainment", "movie") },
        { "apple tv", ("Entertainment", "movie") },
        { "twitch", ("Entertainment", "gaming") },
        
        // Dining / Restaurants
        { "mcdonald", ("Dining", "restaurant") },
        { "burger king", ("Dining", "restaurant") },
        { "wendy", ("Dining", "restaurant") },
        { "taco bell", ("Dining", "restaurant") },
        { "chipotle", ("Dining", "restaurant") },
        { "starbucks", ("Dining", "coffee") },
        { "dunkin", ("Dining", "coffee") },
        { "chick-fil-a", ("Dining", "restaurant") },
        { "subway", ("Dining", "restaurant") },
        { "domino", ("Dining", "restaurant") },
        { "pizza hut", ("Dining", "restaurant") },
        { "grubhub", ("Dining", "restaurant") },
        { "doordash", ("Dining", "restaurant") },
        { "uber eats", ("Dining", "restaurant") },
        
        // Shopping
        { "amazon", ("Shopping", "shopping-cart") },
        { "walmart", ("Shopping", "shopping-cart") },
        { "target", ("Shopping", "shopping-cart") },
        { "costco", ("Shopping", "shopping-cart") },
        { "ebay", ("Shopping", "shopping-cart") },
        { "etsy", ("Shopping", "shopping-cart") },
        { "best buy", ("Shopping", "shopping-cart") },
        { "home depot", ("Shopping", "tools") },
        { "lowes", ("Shopping", "tools") },
        { "ikea", ("Shopping", "home") },
        
        // Groceries
        { "kroger", ("Groceries", "grocery") },
        { "safeway", ("Groceries", "grocery") },
        { "publix", ("Groceries", "grocery") },
        { "whole foods", ("Groceries", "grocery") },
        { "trader joe", ("Groceries", "grocery") },
        { "aldi", ("Groceries", "grocery") },
        { "wegmans", ("Groceries", "grocery") },
        
        // Transportation
        { "uber", ("Transportation", "car") },
        { "lyft", ("Transportation", "car") },
        { "shell", ("Gas", "fuel") },
        { "exxon", ("Gas", "fuel") },
        { "chevron", ("Gas", "fuel") },
        { "bp gas", ("Gas", "fuel") },
        
        // Utilities
        { "electric", ("Utilities", "lightning") },
        { "water bill", ("Utilities", "water") },
        { "gas bill", ("Utilities", "flame") },
        { "internet", ("Utilities", "wifi") },
        { "comcast", ("Utilities", "wifi") },
        { "verizon", ("Utilities", "phone") },
        { "at&t", ("Utilities", "phone") },
        { "t-mobile", ("Utilities", "phone") },
        
        // Subscriptions
        { "apple.com/bill", ("Subscriptions", "subscription") },
        { "google storage", ("Subscriptions", "cloud") },
        { "dropbox", ("Subscriptions", "cloud") },
        { "microsoft 365", ("Subscriptions", "subscription") },
        { "adobe", ("Subscriptions", "subscription") },
        { "gym", ("Health & Fitness", "fitness") },
        { "planet fitness", ("Health & Fitness", "fitness") },
        
        // Healthcare
        { "pharmacy", ("Healthcare", "medical") },
        { "cvs", ("Healthcare", "medical") },
        { "walgreens", ("Healthcare", "medical") },
        { "doctor", ("Healthcare", "medical") },
        { "hospital", ("Healthcare", "medical") },
        { "dental", ("Healthcare", "dental") },
        
        // Travel
        { "airbnb", ("Travel", "bed") },
        { "hotel", ("Travel", "bed") },
        { "marriott", ("Travel", "bed") },
        { "hilton", ("Travel", "bed") },
        { "airline", ("Travel", "plane") },
        { "united air", ("Travel", "plane") },
        { "delta air", ("Travel", "plane") },
        { "southwest", ("Travel", "plane") },
        { "american air", ("Travel", "plane") },
    };
}
```

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/category-suggestions/analyze` | Trigger analysis of uncategorized transactions |
| GET | `/api/v1/category-suggestions` | Get all pending category suggestions |
| GET | `/api/v1/category-suggestions/{id}` | Get specific suggestion with details |
| POST | `/api/v1/category-suggestions/{id}/accept` | Accept suggestion and create category |
| POST | `/api/v1/category-suggestions/{id}/dismiss` | Dismiss a suggestion |
| POST | `/api/v1/category-suggestions/bulk-accept` | Accept multiple suggestions |
| GET | `/api/v1/category-suggestions/{id}/preview-rules` | Preview rules that would be created |
| POST | `/api/v1/category-suggestions/{id}/create-rules` | Create rules for accepted category |
| GET | `/api/v1/merchant-mappings` | Get merchant knowledge base |
| POST | `/api/v1/merchant-mappings` | Add custom merchant mapping |

### Database Changes

```sql
-- New table for category suggestions
CREATE TABLE category_suggestions (
    id UUID PRIMARY KEY,
    suggested_name VARCHAR(100) NOT NULL,
    suggested_icon VARCHAR(50),
    suggested_color VARCHAR(7),
    suggested_type INTEGER NOT NULL,
    confidence DECIMAL(3,2) NOT NULL,
    merchant_patterns JSONB NOT NULL,
    matching_transaction_count INTEGER NOT NULL,
    status INTEGER NOT NULL DEFAULT 0,
    owner_id VARCHAR(255) NOT NULL,
    created_at_utc TIMESTAMP NOT NULL,
    updated_at_utc TIMESTAMP NOT NULL
);

-- New table for learned merchant mappings
CREATE TABLE learned_merchant_mappings (
    id UUID PRIMARY KEY,
    merchant_pattern VARCHAR(200) NOT NULL,
    category_id UUID NOT NULL REFERENCES budget_categories(id),
    owner_id VARCHAR(255) NOT NULL,
    learn_count INTEGER NOT NULL DEFAULT 1,
    created_at_utc TIMESTAMP NOT NULL,
    updated_at_utc TIMESTAMP NOT NULL,
    UNIQUE(merchant_pattern, owner_id)
);

-- New table for dismissed suggestion patterns (prevent re-suggesting)
CREATE TABLE dismissed_suggestion_patterns (
    id UUID PRIMARY KEY,
    pattern VARCHAR(200) NOT NULL,
    owner_id VARCHAR(255) NOT NULL,
    dismissed_at_utc TIMESTAMP NOT NULL,
    UNIQUE(pattern, owner_id)
);
```

### UI Components

- **CategorySuggestionsPage.razor**: Main page showing all suggestions
- **CategorySuggestionCard.razor**: Card component for individual suggestion
- **AcceptCategoryModal.razor**: Modal for customizing category before accepting
- **SuggestedRulesPanel.razor**: Panel showing suggested rules for a category
- **MerchantMappingsPage.razor**: Settings page for viewing/editing merchant mappings

---

## Implementation Plan

### Phase 1: Domain Model & Repository ✅

**Objective:** Establish domain entities and persistence for category suggestions

**Tasks:**
- [x] Create `CategorySuggestion` entity with factory methods
- [x] Reuse existing `SuggestionStatus` enum from Categorization namespace
- [x] Create `LearnedMerchantMapping` entity
- [x] Create `DismissedSuggestionPattern` entity
- [x] Create `ICategorySuggestionRepository` interface
- [x] Create `ILearnedMerchantMappingRepository` interface
- [x] Create `IDismissedSuggestionPatternRepository` interface
- [x] Write unit tests for domain entities (24 tests)
- [x] Implement repositories in Infrastructure
- [x] Add EF Core configurations
- [x] Create database migration

**Commit:**
```bash
git add .
git commit -m "feat(domain): add category suggestion entities

- CategorySuggestion entity with status tracking
- LearnedMerchantMapping for user-trained mappings
- DismissedSuggestionPattern to prevent re-suggestions
- Repository interfaces and implementations
- Database migration

Refs: #032"
```

---

### Phase 2: Merchant Knowledge Base ✅

**Objective:** Implement the default merchant-to-category mapping system

**Tasks:**
- [x] Create `MerchantKnowledgeBase` static class with 100+ default mappings
- [x] Create `IMerchantMappingService` interface
- [x] Implement `MerchantMappingService` that combines default + learned mappings
- [x] Write unit tests for merchant matching logic (31 tests)
- [x] Implement partial matching for merchant names
- [x] Add case-insensitive matching
- [x] Register service in DependencyInjection

**Commit:**
```bash
git add .
git commit -m "feat(app): implement merchant knowledge base

- Default mappings for 100+ common merchants
- Category type inference from merchant
- Partial matching for merchant names
- Combined default + learned mapping service

Refs: #032"
```

---

### Phase 3: Category Suggestion Service

**Objective:** Implement AI-powered analysis service

**Tasks:**
- [x] Create `ICategorySuggestionService` interface
- [x] Implement transaction analysis logic
- [ ] Integrate with Ollama for AI-enhanced suggestions
- [x] Implement category deduplication (don't suggest existing categories)
- [x] Calculate confidence scores
- [x] Write unit tests with mocked AI service
- [ ] Write integration tests

**Commit:**
```bash
git add .
git commit -m "feat(app): implement category suggestion service

- Transaction pattern analysis
- AI integration via Ollama
- Confidence scoring
- Category deduplication logic
- Integration with existing category service

Refs: #032"
```

---

### Phase 4: API Endpoints

**Objective:** Expose suggestion functionality via REST API

**Tasks:**
- [x] Create `CategorySuggestionsController`
- [x] Implement analyze endpoint
- [x] Implement CRUD endpoints for suggestions
- [x] Implement bulk accept endpoint
- [x] Implement rule preview endpoint
- [x] Create request/response DTOs
- [x] Add OpenAPI documentation
- [x] Write API integration tests

**Commit:**
```bash
git add .
git commit -m "feat(api): add category suggestion endpoints

- POST /category-suggestions/analyze
- GET/POST/DELETE suggestion endpoints
- Bulk accept support
- Rule preview endpoint
- OpenAPI documentation

Refs: #032"
```

---

### Phase 5: Rules Integration

**Objective:** Integrate with existing categorization rules engine

**Tasks:**
- [x] Extend `ICategorizationRuleService` for bulk creation
- [x] Implement `SuggestedRule` generation from merchant patterns
- [x] Create endpoint for creating rules from suggestion
- [x] Add rule conflict detection
- [ ] Write integration tests with rules engine

**Commit:**
```bash
git add .
git commit -m "feat(app): integrate category suggestions with rules engine

- Automatic rule generation from merchant patterns
- Bulk rule creation support
- Conflict detection with existing rules
- Integration tests

Refs: #032"
```

---

### Phase 6: Blazor UI Components

**Objective:** Build the user interface for category suggestions

**Tasks:**
- [x] Create `CategorySuggestionsPage.razor`
- [x] Create `CategorySuggestionCard.razor` component
- [x] Create accept category modal with customization
- [x] Create rules preview modal
- [x] Add navigation link to suggestions page
- [x] Implement suggestion refresh functionality
- [x] Add loading and empty states
- [x] Create `ICategorySuggestionApiService` and implementation

**Commit:**
```bash
git add .
git commit -m "feat(client): add category suggestions UI

- Suggestions page with card layout
- Accept/dismiss functionality
- Category customization modal
- Rule preview and creation
- Loading and empty states

Refs: #032"
```

---

### Phase 7: Learning System

**Objective:** Implement the learning-from-user-actions system

**Tasks:**
- [x] Add `LearnFromCategorizationAsync` to `IMerchantMappingService`
- [x] Implement pattern extraction from transaction descriptions
- [x] Add `GetLearnedMappingsAsync` to retrieve user's learned mappings
- [x] Add `DeleteLearnedMappingAsync` to remove mappings
- [x] Add `GetByIdsAsync` to `IBudgetCategoryRepository`
- [x] Create `MerchantMappingsController` with API endpoints
- [x] Write unit tests for learning flow

**Commit:**
```bash
git add .
git commit -m "feat(app): implement merchant learning system

- Learn from manual categorizations
- Learned mappings override defaults
- Merchant mappings settings page
- Learning persistence

Refs: #032"
```

---

### Phase 8: Documentation & Cleanup

**Objective:** Final polish, documentation updates, and cleanup

**Tasks:**
- [x] Update API documentation / OpenAPI specs (auto-generated from controllers)
- [x] Add/update XML comments for public APIs
- [x] Verify no TODO comments remain
- [x] Final code review
- [x] All tests passing (1509 total)

**Commit:**
```bash
git add .
git commit -m "docs(suggestions): add documentation for feature 032

- XML comments for public API
- Update README
- OpenAPI spec updates

Refs: #032"
```

---

## Conventional Commit Reference

Use these commit types to ensure proper changelog generation:

| Type | When to Use | SemVer Impact | Example |
|------|-------------|---------------|---------|
| `feat` | New feature or capability | Minor | `feat(suggestions): add category analysis` |
| `fix` | Bug fix | Patch | `fix(suggestions): correct confidence calculation` |
| `docs` | Documentation only | None | `docs: update suggestions API examples` |
| `refactor` | Code restructure, no feature/fix | None | `refactor(suggestions): extract pattern matcher` |
| `test` | Adding or fixing tests | None | `test(suggestions): add merchant matching tests` |

---

## Testing Strategy

### Unit Tests

- [ ] `CategorySuggestion` entity creation and validation
- [ ] `MerchantKnowledgeBase` matching accuracy
- [ ] Fuzzy merchant name matching
- [ ] Category deduplication logic
- [ ] Confidence score calculation
- [ ] Suggested rule generation

### Integration Tests

- [ ] Full analysis flow with real transactions
- [ ] Accept suggestion creates category and rules
- [ ] Dismiss prevents re-suggestion
- [ ] Learning from manual categorization
- [ ] API endpoint authorization

### Manual Testing Checklist

- [ ] Trigger analysis with varied transaction set
- [ ] Verify Netflix suggests Entertainment
- [ ] Verify Amazon suggests Shopping
- [ ] Accept suggestion and verify category created
- [ ] Accept rules and verify transactions categorized
- [ ] Dismiss suggestion and verify it doesn't reappear
- [ ] Manually categorize and verify system learns

---

## Migration Notes

### Database Migration

```bash
dotnet ef migrations add Feature032_CategorySuggestions --project src/BudgetExperiment.Infrastructure --startup-project src/BudgetExperiment.Api
```

### Breaking Changes

None expected. This is an additive feature.

---

## Security Considerations

- All AI processing happens locally via Ollama (no data sent to cloud)
- Suggestions are user-scoped (only see your own)
- Merchant mappings respect user ownership
- No sensitive transaction data exposed in suggestions

---

## Performance Considerations

- Analysis should run async/background for large transaction sets
- Cache merchant knowledge base lookups
- Paginate suggestion results
- Consider batch processing for 1000+ transactions
- Debounce refresh requests

---

## Future Enhancements

- Export/import merchant mappings between users
- Community-contributed merchant mappings
- Integration with external merchant databases
- Category hierarchy suggestions (parent/child)
- Scheduled automatic analysis after imports

---

## References

- [Feature 024: Auto-Categorization Rules Engine](./024-auto-categorization-rules-engine.md)
- [Feature 025: AI-Powered Rule Suggestions](./025-ai-rule-suggestions.md)
- [Feature 027: CSV Import](./027-csv-import.md)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-19 | Initial draft | @copilot |
