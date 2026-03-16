# Feature 116: Rule Consolidation & Merge Suggestions
> **Status:** Planning

## Overview

Users accumulate rules over time — often multiple rules per category with overlapping or redundant patterns. For example, a user might have separate `Contains` rules for "AMAZON MKTPL", "AMAZON RETA", "AMZN.COM", and "Audible\*" all mapping to Shopping. These could be a single regex rule `AMAZON|AMZN|Audible` without loss of coverage. This feature builds an analysis engine that detects consolidation opportunities, suggests merged rules, and provides a safe workflow for accepting them — reducing rule count, improving evaluation performance, and simplifying rule management.

This is a follow-up to Feature 115 (Rules Listing Redesign) and completes the `RuleConsolidation` suggestion type that currently exists as a skeleton in the codebase.

## Problem Statement

### Current State

- **Multiple rules per category are common** — Users typically create rules one at a time as new transaction patterns appear. A single category like "Groceries" might accumulate 5–10 separate `Contains` rules over months.
- **No merge tooling** — There is no way to detect or suggest that rules could be combined. Users must manually identify overlaps and rewrite rules.
- **Consolidation skeleton exists but throws** — `SuggestionAcceptanceHandler` throws `DomainException("Rule consolidation requires manual review...")` for `SuggestionType.RuleConsolidation`. The domain model supports `RuleSuggestion.CreateConsolidationSuggestion()` and the UI groups these under "Conflicts & Cleanup", but no analysis logic generates them.
- **Performance cost of redundant rules** — Each additional rule adds an evaluation step in the O(N×M) engine loop. 50 rules that could be 15 means ~70% more evaluations per transaction.
- **Regex could replace multiple string rules** — Multiple `Contains` rules for the same category can be combined into a single `Regex` rule using alternation (`|`). This is both faster to evaluate (single regex pass) and easier to manage (one rule instead of many).

### Target State

- **Automated analysis** detects groups of rules targeting the same category that can be merged.
- **Merge suggestions** show the current rules, the proposed consolidated pattern, and a preview of which transactions would match.
- **Safe acceptance workflow** — accepting a consolidation creates the merged rule, deactivates the originals (soft-delete), and allows rollback.
- **Validation before merge** — preview shows any transactions that would be lost or gained by the consolidation, so users can make an informed decision.
- **Integration with AI suggestions** — the existing `RuleConsolidation` suggestion type is populated by the analysis engine and displayed in the AI Suggestions page.

---

## User Stories

### Rule Analysis

#### US-116-001: Detect Consolidation Candidates
**As a** user with many rules  
**I want** the system to identify rules that could be merged  
**So that** I can reduce clutter and improve performance without manually auditing every rule

**Acceptance Criteria:**
- [ ] Analysis groups rules by target category
- [ ] Within each category, identifies rules with the same `MatchType` that can be combined
- [ ] Identifies `Contains` rules that could become a single `Regex` alternation
- [ ] Identifies exact-duplicate patterns (same pattern, same category)
- [ ] Identifies substring-contained rules (e.g., "AMAZON" already covers "AMAZON MKTPL" when both are `Contains`)
- [ ] Minimum 2 rules per group to qualify as a consolidation candidate

#### US-116-002: View Consolidation Suggestions
**As a** user  
**I want to** see suggested merges with clear before/after comparison  
**So that** I can understand what will change before accepting

**Acceptance Criteria:**
- [ ] Each suggestion shows: source rules (names + patterns), proposed merged pattern, target category
- [ ] Match preview: count of transactions that match the merged pattern
- [ ] Coverage delta: any transactions matched by originals but NOT by the merged pattern (loss), and vice versa (gain)
- [ ] Confidence score indicating how safe the merge is (100% = no coverage change)

#### US-116-003: Accept Consolidation
**As a** user  
**I want to** accept a merge suggestion with one click  
**So that** the rules are consolidated without manual editing

**Acceptance Criteria:**
- [ ] Accepting creates the new merged rule with the consolidated pattern
- [ ] Original rules are deactivated (not deleted) for rollback safety
- [ ] New rule inherits the lowest priority of the original rules
- [ ] Success toast shows "Merged N rules into 1"
- [ ] Rules list refreshes to show the new consolidated rule

#### US-116-004: Dismiss or Modify Suggestion
**As a** user  
**I want to** dismiss a suggestion or edit the proposed pattern before accepting  
**So that** I maintain control over my rules

**Acceptance Criteria:**
- [ ] Dismiss button marks suggestion as dismissed (not shown again unless rules change)
- [ ] Edit option lets user modify the proposed merged pattern before accepting
- [ ] Pattern test (existing `TestPatternAsync`) available on the edited pattern
- [ ] Dismissed suggestions can be re-surfaced from a "Show dismissed" toggle

#### US-116-005: Rollback Consolidation
**As a** user who accepted a merge  
**I want to** undo the consolidation  
**So that** I can restore my original rules if the merged pattern doesn't work as expected

**Acceptance Criteria:**
- [ ] Deactivated source rules retain a reference to the consolidation event
- [ ] "Undo merge" option available on the new consolidated rule (within session or via rule detail)
- [ ] Undo reactivates the originals and deletes the merged rule

---

## Technical Design

### Analysis Engine

The core of this feature is a `RuleConsolidationAnalyzer` that examines all active rules and produces merge suggestions.

#### Consolidation Strategies

```
Strategy 1: Exact Duplicates
  - Same pattern, same category, same match type → keep one, deactivate rest
  - Confidence: 100%

Strategy 2: Substring Containment
  - Rule A pattern "AMAZON" (Contains) already covers Rule B pattern "AMAZON MKTPL" (Contains)
  - Merged pattern: keep the broader one ("AMAZON")
  - Confidence: 100% (no coverage loss)

Strategy 3: Regex Alternation
  - Multiple Contains rules for same category: "WALMART", "KROGER", "SAFEWAY"
  - Merged pattern: "WALMART|KROGER|SAFEWAY" (Regex, case-insensitive)
  - Confidence: 100% (equivalent coverage)

Strategy 4: Common Prefix/Suffix Extraction
  - "AMAZON MKTPL", "AMAZON RETA", "AMAZON PRIME" → "AMAZON " (Contains)
  - Only suggest if the shorter pattern doesn't over-match
  - Confidence: Variable (requires transaction-level validation)

Strategy 5: AI-Assisted Merge (Future)
  - For complex patterns, ask AI to suggest an optimal regex
  - Pass transaction descriptions for validation
  - Confidence: Based on AI + validation
```

#### Merge Safety Validation

Before presenting a suggestion, the engine tests the proposed pattern against the full transaction set:

```csharp
// Pseudo-code for coverage validation
var originalMatches = transactions.Where(t => originalRules.Any(r => r.Matches(t.Description)));
var mergedMatches = transactions.Where(t => mergedRule.Matches(t.Description));

var lost = originalMatches.Except(mergedMatches);    // Transactions that would lose coverage
var gained = mergedMatches.Except(originalMatches);   // Transactions newly matched (over-match risk)

suggestion.CoverageLost = lost.Count();
suggestion.CoverageGained = gained.Count();
suggestion.Confidence = lost.Count() == 0 && gained.Count() == 0 ? 1.0 : CalculateConfidence(...);
```

### Domain Model Changes

```csharp
// New: Analysis result stored per consolidation suggestion
public record ConsolidationDetail(
    IReadOnlyList<Guid> SourceRuleIds,
    string MergedPattern,
    RuleMatchType MergedMatchType,
    int TransactionsMatched,
    int CoverageLost,
    int CoverageGained,
    double Confidence
);
```

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/categorizationrules/analyze-consolidation` | Run analysis, return consolidation suggestions |
| POST | `/api/v1/categorizationrules/consolidation/{suggestionId}/accept` | Accept a consolidation: create merged rule, deactivate originals |
| POST | `/api/v1/categorizationrules/consolidation/{suggestionId}/dismiss` | Dismiss a suggestion |
| POST | `/api/v1/categorizationrules/consolidation/{suggestionId}/preview` | Preview match results for proposed merged pattern |
| DELETE | `/api/v1/categorizationrules/consolidation/{ruleId}/undo` | Undo: reactivate originals, delete merged rule |

### Service Layer

```csharp
public interface IRuleConsolidationService
{
    Task<IReadOnlyList<ConsolidationSuggestion>> AnalyzeAsync(CancellationToken ct);
    Task<ConsolidationPreview> PreviewAsync(Guid suggestionId, string? overridePattern, CancellationToken ct);
    Task<CategorizationRuleDto> AcceptAsync(Guid suggestionId, string? overridePattern, CancellationToken ct);
    Task DismissAsync(Guid suggestionId, CancellationToken ct);
    Task UndoAsync(Guid mergedRuleId, CancellationToken ct);
}
```

### Integration with Existing Systems

- **`SuggestionAcceptanceHandler`** — Replace the `throw` for `SuggestionType.RuleConsolidation` with actual acceptance logic that delegates to `IRuleConsolidationService`.
- **`RuleSuggestionService`** — Hook `RuleConsolidationAnalyzer` into the suggestion generation pipeline so consolidation suggestions appear alongside AI-generated suggestions.
- **AI Suggestions page** — Consolidation suggestions already group under "Conflicts & Cleanup" in the UI. Enhance the card to show before/after comparison and match preview.

### Performance Considerations

- **Analysis is read-heavy** — Runs against the full rule set and may sample transactions for coverage validation. Should be async with loading indicator.
- **Transaction sampling** — For coverage validation, sample a representative subset (e.g., last 1000 uncategorized + last 1000 categorized) rather than scanning all transactions.
- **Cache analysis results** — Store suggestions in the database (already supported by `RuleSuggestion` entity). Re-analyze only when rules change.
- **Regex alternation performance** — A single regex with `|` alternation is faster than N separate `Contains` calls because the regex engine can optimize the alternation into a trie/state machine (especially with `RegexOptions.Compiled`).

---

## Implementation Plan

Each slice is a vertical cut delivering testable, deployable value from domain through UI.

### Slice 1: Consolidation Analyzer — Exact Duplicates & Substring Containment

**Objective:** Build the core analysis engine that detects the simplest and safest merge opportunities: exact duplicates and substring containment within the same category.

**Tasks:**
- [ ] Create `RuleConsolidationAnalyzer` in Application layer
- [ ] Implement Strategy 1 (exact duplicate detection): same pattern + same category + same match type
- [ ] Implement Strategy 2 (substring containment): pattern A contains pattern B, both `Contains` type, same category
- [ ] Return `ConsolidationSuggestion` list with source rule IDs, merged pattern, and confidence
- [ ] Write unit tests: duplicate detection, substring detection, cross-category rules not merged, mixed match types not merged
- [ ] Write unit tests: edge cases (single rule per category, inactive rules excluded, case-insensitive comparison)

**Commit:**
```bash
git commit -m "feat(rules): consolidation analyzer for duplicates and substrings

- RuleConsolidationAnalyzer detects exact duplicate rules
- Detects substring containment (broader pattern covers narrower)
- Groups suggestions by category with confidence scores
- Unit tests for all detection strategies

Refs: #116"
```

---

### Slice 2: Regex Alternation Merge Strategy

**Objective:** Detect multiple `Contains` rules for the same category and suggest merging them into a single `Regex` alternation pattern.

**Tasks:**
- [ ] Implement Strategy 3 (regex alternation): group `Contains` rules by category, produce `pattern1|pattern2|...` regex
- [ ] Escape regex special characters in source patterns before joining with `|`
- [ ] Validate generated regex compiles and stays within length limit (500 chars)
- [ ] If merged pattern exceeds length limit, split into multiple suggestions
- [ ] Write unit tests: alternation generation, special character escaping, length limit handling
- [ ] Write unit tests: single-rule categories skipped, mixed match types handled

**Commit:**
```bash
git commit -m "feat(rules): regex alternation merge strategy

- Combine multiple Contains rules into single Regex alternation
- Escape special characters in source patterns
- Respect 500-char pattern length limit
- Split large groups into multiple suggestions

Refs: #116"
```

---

### Slice 3: Coverage Validation & Preview

**Objective:** Before suggesting a merge, validate coverage against actual transactions. Expose a preview endpoint.

**Tasks:**
- [ ] Add `PreviewConsolidation` method to analyzer — test merged pattern against transaction sample
- [ ] Calculate coverage delta: transactions lost, transactions gained, confidence score
- [ ] Add `POST /api/v1/categorizationrules/consolidation/preview` endpoint (accepts source rule IDs + proposed pattern)
- [ ] Create `ConsolidationPreview` DTO (matched count, lost count, gained count, sample descriptions)
- [ ] Add to Contracts: `ConsolidationPreviewRequest`, `ConsolidationPreviewResponse`
- [ ] Write unit tests: coverage calculation, confidence scoring
- [ ] Write integration test: preview endpoint returns correct match data

**Commit:**
```bash
git commit -m "feat(rules): consolidation coverage validation and preview

- Validate merged pattern against transaction sample
- Calculate coverage delta (lost/gained transactions)
- Preview endpoint for proposed merges
- Confidence score based on coverage analysis

Refs: #116"
```

---

### Slice 4: Analysis API & Suggestion Storage

**Objective:** Wire the analyzer into the API and store suggestions in the database via the existing `RuleSuggestion` entity.

**Tasks:**
- [ ] Create `IRuleConsolidationService` interface and implementation
- [ ] Wire `RuleConsolidationAnalyzer` into `RuleSuggestionService` suggestion pipeline
- [ ] Add `POST /api/v1/categorizationrules/analyze-consolidation` endpoint
- [ ] Store results as `RuleSuggestion` entities with `SuggestionType.RuleConsolidation`
- [ ] Populate `ConflictingRuleIds` and `OptimizedPattern` fields on suggestion entity
- [ ] Update `IBudgetApiService` client with analysis API call
- [ ] Write unit tests: service orchestration, suggestion persistence
- [ ] Write API integration test: analyze endpoint returns suggestions

**Commit:**
```bash
git commit -m "feat(rules): consolidation analysis API and suggestion storage

- IRuleConsolidationService with analyze endpoint
- Hooks into existing RuleSuggestion pipeline
- Stores consolidation suggestions in database
- Client API service updated

Refs: #116"
```

---

### Slice 5: Accept & Dismiss Workflow

**Objective:** Implement the acceptance handler that creates the merged rule and deactivates originals. Replace the existing `throw` in `SuggestionAcceptanceHandler`.

**Tasks:**
- [ ] Implement `AcceptAsync` in `RuleConsolidationService`: create merged rule, deactivate source rules
- [ ] New merged rule inherits lowest priority from source rules
- [ ] Tag deactivated rules with a consolidation reference (for undo)
- [ ] Replace `throw DomainException(...)` in `SuggestionAcceptanceHandler` with delegation to consolidation service
- [ ] Implement `DismissAsync`: mark suggestion as dismissed with feedback
- [ ] Add `POST .../consolidation/{id}/accept` and `POST .../consolidation/{id}/dismiss` endpoints
- [ ] Write unit tests: accept creates rule + deactivates originals, dismiss marks suggestion
- [ ] Write integration tests: accept + dismiss endpoints

**Commit:**
```bash
git commit -m "feat(rules): consolidation accept and dismiss workflow

- Accept creates merged rule, deactivates originals
- Merged rule inherits lowest priority
- SuggestionAcceptanceHandler no longer throws for consolidation
- Dismiss with feedback support

Refs: #116"
```

---

### Slice 6: Undo Consolidation

**Objective:** Allow users to reverse a consolidation by reactivating original rules and removing the merged rule.

**Tasks:**
- [ ] Add consolidation tracking: store source rule IDs on merged rule (metadata or separate table)
- [ ] Implement `UndoAsync`: reactivate original rules, delete merged rule
- [ ] Add `DELETE .../consolidation/{ruleId}/undo` endpoint
- [ ] Validate undo is only available for rules created via consolidation
- [ ] Write unit tests: undo reactivates originals, undo fails for non-consolidated rules
- [ ] Write integration test: full accept → undo round-trip

**Commit:**
```bash
git commit -m "feat(rules): undo consolidation support

- Track source rule IDs on merged rules
- Undo reactivates originals and deletes merged rule
- Validates rule was created via consolidation

Refs: #116"
```

---

### Slice 7: Client — Consolidation Suggestions UI

**Objective:** Enhance the AI Suggestions page to display consolidation suggestions with before/after comparison and accept/dismiss/edit controls.

**Tasks:**
- [ ] Create `ConsolidationSuggestionCard` component (shows source rules, merged pattern, confidence, coverage delta)
- [ ] Add "Preview" button that calls preview endpoint and shows matching transactions
- [ ] Add "Accept" button with confirmation (shows what will be deactivated)
- [ ] Add "Edit Pattern" option to modify proposed pattern before accepting
- [ ] Wire pattern editor to `TestPatternAsync` for live testing
- [ ] Add "Dismiss" button with optional feedback
- [ ] Update `AiSuggestionsViewModel` to handle consolidation suggestion lifecycle
- [ ] Write ViewModel tests: accept, dismiss, edit, preview flows
- [ ] Write bUnit tests: card renders source rules, delta display

**Commit:**
```bash
git commit -m "feat(client): consolidation suggestion UI

- ConsolidationSuggestionCard with before/after comparison
- Coverage preview with transaction match display
- Accept, dismiss, edit pattern workflows
- Pattern testing integration

Refs: #116"
```

---

### Slice 8: Rules Page Integration & Polish

**Objective:** Surface consolidation opportunities on the Rules page itself (not just AI Suggestions) and final polish.

**Tasks:**
- [ ] Add "Optimize Rules" button to Rules page toolbar (runs analysis, shows count of suggestions)
- [ ] Badge/indicator on rules that are part of a consolidation group
- [ ] Link from Rules page to AI Suggestions page filtered to consolidation suggestions
- [ ] Add "Undo merge" option to rule detail/edit view for consolidated rules
- [ ] Update OpenAPI spec documentation for new endpoints
- [ ] Add XML comments for public API surface
- [ ] Manual testing with 50+ rule dataset

**Commit:**
```bash
git commit -m "feat(rules): rules page consolidation integration and polish

- Optimize Rules button with suggestion count
- Consolidation group indicators on rule cards/rows
- Undo merge option on consolidated rules
- OpenAPI docs and XML comments

Refs: #116"
```

---

## Open Questions

1. **Common prefix extraction (Strategy 4)** — Extracting "AMAZON" from "AMAZON MKTPL", "AMAZON RETA", "AMAZON PRIME" is powerful but risks over-matching (e.g., "AMAZON RETURNS" going to wrong category). Should this require explicit coverage validation before suggesting? Recommend yes — only suggest if coverage delta is zero.
2. **Minimum rule count for alternation** — Should we require 3+ rules before suggesting a regex alternation, or is 2 sufficient? A merge of 2 rules is still a net reduction. Recommend 2 as the minimum.
3. **Case sensitivity handling** — If source rules have mixed case sensitivity, should the merged regex be case-insensitive (broader) or case-sensitive (conservative)? Recommend case-insensitive with a note to the user.
4. **Cross-match-type merging** — Can a `StartsWith("ATT*")` rule and a `Contains("AT&T")` rule be merged? These have different semantics. Recommend only merging rules with the same match type initially, except for the Contains→Regex promotion (Strategy 3).
5. **Automatic consolidation** — Should the system auto-consolidate without user approval? Recommend no — always require user confirmation, at least initially. Auto-consolidation could be a future preference toggle.
6. **AI-assisted merge (Strategy 5)** — Should we ask the AI to suggest optimal merge patterns for complex cases? Useful but adds latency and cost. Recommend deferring to a later iteration unless users request it.
