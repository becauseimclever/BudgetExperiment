# Feature 114: AI-Powered Category Discovery

> **Status:** Planning
> **Priority:** Medium
> **Dependencies:** Feature 105 (AI Suggestions UX Redesign — Done), Feature 106 (AI Suggestion Quality — Done)

## Overview

Extend the category suggestion system to use the AI (Ollama) for discovering **new categories** the user hasn't created yet, based on patterns in their uncategorized transactions. Today, `CategorySuggestionService` only suggests categories that exist in the static `MerchantKnowledgeBase` (~200 hardcoded merchant patterns) or user-learned mappings. Transactions whose descriptions don't match any known pattern produce **zero** suggestions, leaving users with no guidance. This feature closes that gap by sending unmatched transaction descriptions to the AI and asking it to propose meaningful new categories.

## Problem Statement

### Current State

The category suggestion pipeline (`CategorySuggestionService` → `MerchantMappingService` → `MerchantKnowledgeBase`) works as follows:

1. Fetch all uncategorized transactions
2. Match each description against learned mappings + `MerchantKnowledgeBase` defaults
3. Group matched patterns by category name
4. Create `CategorySuggestion` entities for categories not yet in the user's budget

**The limitation:** Any transaction whose description doesn't match a known merchant pattern is silently ignored — no suggestion is generated. This means:

- Niche merchants (local restaurants, specialty shops, regional services) are never suggested
- New spending categories the user hasn't considered (e.g., "Pet Care", "Home Improvement", "Kids Activities") are never surfaced
- Users with many unmatched transactions see few or no category suggestions, even though the transactions clearly cluster into logical groups
- The system can only suggest categories from a fixed vocabulary — it cannot invent new category names that better fit the user's actual spending

Feature 106 explicitly identified this gap: *"CategorySuggestionService relies solely on a static MerchantKnowledgeBase [...] For merchants not in the knowledge base, no suggestions are generated at all. The AI could suggest new category names and groupings based on spending patterns, but currently doesn't."*

### Target State

- After pattern-based matching, unmatched transaction descriptions are sent to the AI for analysis
- The AI groups unmatched descriptions into logical spending categories and proposes **new category names** the user may not have considered
- AI-discovered categories appear alongside pattern-matched suggestions on the unified AI suggestions page (`/ai`)
- Each AI-discovered category includes a name, suggested icon/color, confidence score, reasoning, and the list of transaction descriptions that belong to it
- Users can accept, customize, or dismiss AI-discovered categories using the same flow as existing category suggestions
- The AI considers the user's **existing** categories to avoid suggesting duplicates or near-duplicates

---

## User Stories

### US-114-001: AI-Discovered Category Suggestions

**As a** user with uncategorized transactions that don't match known merchant patterns
**I want** the AI to analyze those transactions and suggest new categories
**So that** I get useful categorization guidance even for niche or unfamiliar merchants.

**Acceptance Criteria:**
- [ ] After pattern-based matching, unmatched descriptions are collected and sent to the AI
- [ ] The AI returns grouped suggestions, each with a proposed category name and matched descriptions
- [ ] AI-discovered suggestions are persisted as `CategorySuggestion` entities with a distinguishing source indicator
- [ ] AI-discovered suggestions appear on the unified AI suggestions page alongside pattern-based suggestions
- [ ] If no AI service is configured/available, the system falls back gracefully to pattern-only suggestions (current behavior)

### US-114-002: Context-Aware Category Naming

**As a** user
**I want** the AI to suggest category names that don't duplicate my existing categories
**So that** I don't end up with redundant or confusingly similar categories.

**Acceptance Criteria:**
- [ ] The AI prompt includes the user's existing category names as context
- [ ] The AI is instructed not to suggest categories that match or closely resemble existing ones
- [ ] If the AI suggests a name too similar to an existing category, the service filters it out before persisting
- [ ] The AI is encouraged to suggest descriptive, user-friendly names (e.g., "Home Improvement" not "HOMEDEPOT_LOWES_GROUP")

### US-114-003: Enriched AI Prompt for Category Discovery

**As a** system
**I want** to send enriched transaction data to the AI (descriptions, frequencies, amount ranges)
**So that** the AI can make informed grouping decisions based on spending patterns, not just merchant names.

**Acceptance Criteria:**
- [ ] Unmatched descriptions are pre-processed using the existing `TransactionDescriptionCleaner` (Feature 106)
- [ ] Descriptions are aggregated with frequency counts and amount ranges (reuse `DescriptionAggregator` from Feature 106)
- [ ] The prompt includes the top N unmatched description groups (capped to stay within context window)
- [ ] The prompt instructs the AI to consider spending frequency and amounts when grouping
- [ ] Few-shot examples show what a good category suggestion looks like

### US-114-004: AI Category Suggestion Confidence & Reasoning

**As a** user
**I want** each AI-suggested category to include a confidence score and brief reasoning
**So that** I can understand why the AI thinks this category makes sense for my spending.

**Acceptance Criteria:**
- [ ] Each AI-discovered suggestion has a confidence score (0.0–1.0) set by the AI based on cluster coherence
- [ ] Each suggestion includes a short reasoning string (1–2 sentences) explaining why these transactions belong together
- [ ] The reasoning is displayed in the suggestion card's expandable details section
- [ ] Low-confidence suggestions (< 0.4) are still shown but visually de-emphasized

### US-114-005: Accept & Customize AI-Discovered Categories

**As a** user
**I want** to accept AI-discovered categories with the option to customize the name, icon, and color
**So that** I have full control over what gets added to my budget.

**Acceptance Criteria:**
- [ ] Accepting an AI-discovered suggestion follows the same flow as existing category suggestions (reuse `AcceptSuggestionAsync`)
- [ ] User can override the suggested name, icon, and color before confirming
- [ ] Accepting creates a `BudgetCategory` and optionally generates categorization rules from the matched patterns
- [ ] Dismissed AI-discovered suggestions are tracked so the AI doesn't re-suggest the same grouping

---

## Technical Design

### Architecture Overview

```
Existing flow (pattern-based):
  AnalyzeTransactionsAsync()
    → MerchantMappingService.FindMatchingPatternsAsync()
    → Group by category → CategorySuggestion entities

New flow (AI-enhanced):
  AnalyzeTransactionsAsync()
    → MerchantMappingService.FindMatchingPatternsAsync()
    → Collect UNMATCHED descriptions
    → TransactionDescriptionCleaner.Clean() (reuse from 106)
    → DescriptionAggregator.Aggregate() (reuse from 106)
    → CategoryDiscoveryPromptBuilder.Build(unmatchedGroups, existingCategories)
    → IAiService.CompleteAsync(prompt)
    → CategoryDiscoveryResponseParser.Parse(response)
    → Filter duplicates / near-matches to existing categories
    → Create CategorySuggestion entities (source = AI)
    → Merge with pattern-based suggestions
```

### Domain Model Changes

Add a `CategorySource` to distinguish how a suggestion was generated:

```csharp
// If not already in BudgetExperiment.Shared
public enum CategorySuggestionSource
{
    PatternMatch,  // From MerchantKnowledgeBase / learned mappings
    AiDiscovered   // From AI analysis of unmatched transactions
}
```

Extend `CategorySuggestion` entity:

```csharp
// New properties on CategorySuggestion
public CategorySuggestionSource Source { get; private set; }
public string? Reasoning { get; private set; }
```

### New Application Components

#### `CategoryDiscoveryPromptBuilder` (static class)

Builds the AI prompt for category discovery:

```csharp
public static class CategoryDiscoveryPromptBuilder
{
    public static AiPrompt Build(
        IReadOnlyList<DescriptionGroup> unmatchedGroups,
        IReadOnlyList<string> existingCategoryNames,
        IReadOnlyList<string>? dismissedCategoryNames = null);
}
```

**Prompt structure:**
- System: "You are a personal finance assistant. Analyze transaction descriptions and suggest logical spending categories."
- Context: User's existing categories (do not duplicate), previously dismissed suggestions (do not re-suggest)
- Data: Unmatched description groups with frequency + amount ranges
- Instructions: Group related transactions, propose descriptive category names, assign confidence, explain reasoning
- Few-shot: 2–3 examples of good category suggestions
- Output format: JSON array with structured schema

**Expected AI response schema:**
```json
[
  {
    "categoryName": "Home Improvement",
    "icon": "🔨",
    "color": "#8B4513",
    "confidence": 0.85,
    "reasoning": "Multiple transactions at hardware stores and home supply retailers suggest a distinct spending category.",
    "matchedDescriptions": [
      "HOME DEPOT",
      "LOWES",
      "ACE HARDWARE"
    ]
  }
]
```

#### `CategoryDiscoveryResponseParser`

Parses AI JSON responses into typed results:

```csharp
public static class CategoryDiscoveryResponseParser
{
    public static IReadOnlyList<DiscoveredCategory> Parse(string aiResponse);
}

public record DiscoveredCategory(
    string CategoryName,
    string? Icon,
    string? Color,
    decimal Confidence,
    string Reasoning,
    IReadOnlyList<string> MatchedDescriptions);
```

### Service Changes

#### `CategorySuggestionService.AnalyzeTransactionsAsync()`

Extended flow:

```
1. Fetch uncategorized transactions
2. Pattern-match via MerchantMappingService (existing)
3. Collect unmatched descriptions (descriptions not in any pattern match result)
4. If AI is available AND unmatched descriptions exist:
   a. Clean & aggregate unmatched descriptions
   b. Build prompt with existing categories + dismissed patterns
   c. Call IAiService.CompleteAsync()
   d. Parse response into DiscoveredCategory list
   e. Filter out categories too similar to existing ones
   f. Convert to CategorySuggestion entities (Source = AiDiscovered)
5. Merge pattern-based + AI-discovered suggestions
6. Persist and return
```

The AI step is **additive** — it cannot remove or alter pattern-based suggestions. If the AI call fails, pattern-based suggestions are still returned (graceful degradation).

### API Changes

No new endpoints required. The existing `POST /api/v1/categorySuggestions/analyze` endpoint returns all suggestions (pattern-based + AI-discovered). The `CategorySuggestionDto` gains:

```csharp
public CategorySuggestionSource Source { get; init; }
public string? Reasoning { get; init; }
```

### Database Changes

- Add `Source` column (`int`, default `0` = PatternMatch) to `CategorySuggestions` table
- Add `Reasoning` column (`text`, nullable) to `CategorySuggestions` table
- EF migration in Infrastructure

### UI Changes

Minimal — the unified AI suggestions page (Feature 105) already displays `CategorySuggestion` cards. Changes:

- Show a "Source" indicator on cards (e.g., small "AI" badge for AI-discovered vs. "Pattern" for knowledge-base matches)
- Show reasoning text in the expandable details section for AI-discovered suggestions
- No new pages or navigation changes

---

## Implementation Plan

### Phase 1: Domain & Contracts

**Objective:** Extend the domain model and contracts to support AI-discovered category suggestions.

**Tasks:**
- [ ] Add `CategorySuggestionSource` enum (or reuse if already in Shared)
- [ ] Add `Source` and `Reasoning` properties to `CategorySuggestion` entity
- [ ] Update `CategorySuggestion.Create()` factory method to accept source and reasoning
- [ ] Update `CategorySuggestionDto` in Contracts with new fields
- [ ] Write unit tests for domain model changes
- [ ] Update mapping between entity and DTO

**Commit:**
```bash
git commit -m "feat(domain): add Source and Reasoning to CategorySuggestion

- Add CategorySuggestionSource enum (PatternMatch, AiDiscovered)
- Extend CategorySuggestion entity with Source and Reasoning
- Update CategorySuggestionDto contract
- Unit tests for new domain properties

Refs: #114"
```

### Phase 2: Prompt Builder & Response Parser

**Objective:** Build the AI prompt construction and response parsing for category discovery.

**Tasks:**
- [ ] Create `CategoryDiscoveryPromptBuilder` static class with `Build()` method
- [ ] Create `DiscoveredCategory` record for parsed results
- [ ] Create `CategoryDiscoveryResponseParser` static class with `Parse()` method
- [ ] Write unit tests for prompt builder (verifies prompt includes existing categories, unmatched descriptions, few-shot examples)
- [ ] Write unit tests for response parser (valid JSON, JSON in markdown block, invalid response, partial JSON, edge cases)

**Commit:**
```bash
git commit -m "feat(application): add CategoryDiscovery prompt builder and response parser

- CategoryDiscoveryPromptBuilder builds enriched prompts for unmatched transactions
- CategoryDiscoveryResponseParser handles AI response extraction
- Comprehensive unit tests for both components

Refs: #114"
```

### Phase 3: Service Integration

**Objective:** Integrate AI category discovery into `CategorySuggestionService.AnalyzeTransactionsAsync()`.

**Tasks:**
- [ ] Inject `IAiService` into `CategorySuggestionService`
- [ ] After pattern matching, collect unmatched descriptions
- [ ] Clean and aggregate unmatched descriptions (reuse Feature 106 components)
- [ ] Call AI service and parse response
- [ ] Filter AI suggestions that duplicate or near-match existing categories
- [ ] Convert `DiscoveredCategory` results to `CategorySuggestion` entities with `Source = AiDiscovered`
- [ ] Merge with pattern-based suggestions
- [ ] Handle AI unavailability gracefully (log warning, return pattern-only results)
- [ ] Write unit tests with mocked `IAiService`
- [ ] Add dismissed category name tracking to prevent re-suggestion

**Commit:**
```bash
git commit -m "feat(application): integrate AI category discovery into analysis pipeline

- CategorySuggestionService calls AI for unmatched descriptions
- Graceful fallback when AI unavailable
- Duplicate/near-match filtering against existing categories
- Dismissed pattern tracking prevents re-suggestion
- Unit tests with mocked AI service

Refs: #114"
```

### Phase 4: Infrastructure & Persistence

**Objective:** Add database support for new fields and ensure proper persistence.

**Tasks:**
- [ ] Add EF migration for `Source` and `Reasoning` columns on `CategorySuggestions` table
- [ ] Update EF entity configuration in Infrastructure
- [ ] Verify repository operations handle new fields correctly
- [ ] Integration tests for persistence round-trip

**Commit:**
```bash
git commit -m "feat(infrastructure): add migration for CategorySuggestion Source and Reasoning

- New Source (int) and Reasoning (text) columns
- EF configuration update
- Integration tests for persistence

Refs: #114"
```

### Phase 5: UI Enhancements

**Objective:** Surface AI-discovered suggestions in the unified AI suggestions page with source indicator and reasoning.

**Tasks:**
- [ ] Add "AI" badge/indicator on suggestion cards for AI-discovered suggestions
- [ ] Show reasoning text in expandable details section
- [ ] Verify existing accept/dismiss/customize flow works with AI-discovered suggestions
- [ ] bUnit tests for new UI elements (if applicable)

**Commit:**
```bash
git commit -m "feat(client): display AI-discovered category suggestions with source badge

- AI badge on suggestion cards for AI-discovered categories
- Reasoning text in expandable details
- Existing accept/dismiss flow unchanged

Refs: #114"
```

### Phase 6: Documentation & Cleanup

**Objective:** Final polish and documentation.

**Tasks:**
- [ ] Update OpenAPI spec annotations if needed
- [ ] Add XML comments for new public APIs
- [ ] Move this document to `docs/archive/` when complete
- [ ] Remove any TODO comments

**Commit:**
```bash
git commit -m "docs: add documentation for AI-powered category discovery

- XML comments for new public APIs
- OpenAPI spec updates
- Archive feature document

Refs: #114"
```

---

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Domain | `CategorySuggestion` with new `Source`/`Reasoning` fields | xUnit + Shouldly |
| Application | `CategoryDiscoveryPromptBuilder` output structure | xUnit, verify prompt content |
| Application | `CategoryDiscoveryResponseParser` edge cases | xUnit, multiple response formats |
| Application | `CategorySuggestionService` AI integration path | xUnit, mock `IAiService` |
| Application | Graceful degradation when AI unavailable | xUnit, mock returns failure |
| Application | Duplicate/near-match filtering | xUnit, verify filtered results |
| Infrastructure | Migration applies cleanly | Integration test |
| Infrastructure | Round-trip persistence of new fields | Integration test with test DB |
| API | Analyze endpoint returns AI-discovered suggestions | `WebApplicationFactory` test |
| Client | Source badge renders for AI-discovered cards | bUnit (if applicable) |

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| AI suggests nonsensical category names | Validate names (length, characters); show confidence prominently so users can dismiss low-quality suggestions |
| AI response doesn't follow expected JSON schema | Robust parser with fallback; log parse errors; return pattern-only suggestions on failure |
| AI call adds latency to analysis | Run AI call concurrently with pattern-based suggestion persistence; show pattern results immediately, AI results when ready (future enhancement) |
| AI suggests categories too similar to existing ones | Fuzzy string comparison (e.g., Levenshtein distance or case-insensitive contains) to filter near-duplicates |
| Large number of unmatched descriptions exceeds context window | Cap at top N by frequency (reuse Feature 106 sampling strategy) |
| Model quality varies across Ollama models | Document recommended models; test with 2–3 common models (llama3, mistral, gemma) |

## Out of Scope

- **Automatic category creation** — AI suggestions always require user approval
- **Real-time / streaming suggestions** — Analysis is batch-triggered, not on every transaction import
- **Multi-language support** — Category names and reasoning in English only (for now)
- **AI-powered category *merging*  or *renaming*** — This feature only suggests *new* categories, not modifications to existing ones
- **Cross-user learning** — Suggestions are per-user; no shared intelligence across accounts
