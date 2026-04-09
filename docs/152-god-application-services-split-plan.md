# Feature 152: God Application Services — Prioritized Split Plan

> **Status:** Proposed  
> **Severity:** 🟠 High — F-006  
> **Audit Source:** `docs/audit/2026-04-09-full-principle-audit.md`

---

## Overview

Eighteen Application services exceed the 300-line Engineering Guide threshold. Five are particularly severe (450–515 lines). This document defines the split strategy and per-service design for the top five, and establishes a **feature-work-coupled** policy for the remaining thirteen: split them when a feature naturally touches that file, not as standalone refactors.

Splitting application services in isolation is high-risk/low-return if done carelessly — it can fragment cohesive logic, break DI wiring, and produce a PR with no testable behavior changes. The approach here: **each split is scoped, paired with tests, and delivered in its own PR alongside a feature that motivates the change.**

---

## Problem Statement

### Current State

18 Application services exceed 300 lines:

| Service | Lines | Primary Concern Fragmentation |
|---------|-------|-------------------------------|
| `RuleSuggestionResponseParser` | 515 | Parses multiple AI response fields — no shared structure |
| `ImportRowProcessor` | 508 | Field extraction, parsing, validation, business rules all in one class |
| `ChatService` | 487 | Session management + message routing + action dispatch + date parsing |
| `ChatActionParser` | 482 | One parser class handling every action type |
| `CategorySuggestionService` | 453 | Suggestion generation + scoring + ranking + persistence |
| `TransactionListService` | 430 | Query building + pagination + filtering + formatting |
| `CategorizationRuleService` | 411 | Rule CRUD + matching + conflict detection + stats |
| `RuleSuggestionService` | 394 | Suggestion lifecycle + AI orchestration + review workflow |
| `CalendarGridService` | 388 | Grid construction + day summary + event overlays |
| `MerchantKnowledgeBase` | 369 | Merchant lookup + pattern matching + cache |
| `DataHealthService` | 358 | Health checks + scoring + report generation |
| `ImportMappingService` | 339 | Format detection + column mapping + validation |
| `RecurringTransactionService` | 332 | CRUD + scheduling + instance generation |
| `ReportService` | 331 | Multiple report types in one service |
| `RecurringTransferService` | 325 | CRUD + scheduling + transfer pair |
| `AiPrompts` | 324 | All prompt templates in one class |
| `TransferService` | 314 | Transfer CRUD + validation + reconciliation |
| `DayDetailService` | 309 | Day data aggregation across multiple domains |

### Target State

- Top 5 services split into focused collaborating services (detailed below).
- Remaining 13 split opportunistically during feature work.
- Each split delivers its own PR with accompanying unit tests for new classes.
- Engineering Guide §8 threshold (300 lines) respected for all new classes.

---

## Split Strategy

### Policy: Feature-Work-Coupled Splitting

**Rule:** Do not file a PR that only refactors a service with no feature motivation. Instead:
1. When a feature ticket touches a god service, include the split in that PR.
2. The split is always the first commit; the feature work follows on top.
3. Each split PR must include tests for the extracted classes.

This maximizes code review value: reviewers see both the refactor and how it enables the new feature clearly.

**Exception:** The top 5 services (listed below) are large enough to warrant standalone split PRs because the accumulation of technical debt actively impedes onboarding and review. These are approved for standalone refactoring.

---

## Top-5 Service Designs

### 1. RuleSuggestionResponseParser (515 lines → split into field parsers)

**Current:** One class parsing every field from AI suggestion responses (merchant name, category, confidence, match pattern, etc.).

**Proposed split:**
- `RuleSuggestionResponseParser` — coordinator (≤ 100 lines), delegates to field parsers
- `MerchantFieldParser` — extracts and normalizes merchant name from AI response
- `CategoryFieldParser` — extracts category hint and maps to known category IDs
- `ConfidenceFieldParser` — extracts and normalizes confidence level
- `MatchPatternFieldParser` — extracts match pattern type and value

**Interface:** `IRuleSuggestionFieldParser<T>` with `T Parse(string rawResponse)`.

**Tests:** Unit test each field parser in isolation with representative AI response strings (happy path, malformed, empty, ambiguous).

---

### 2. ImportRowProcessor (508 lines → field extraction, parsing, validation)

**Current:** One class that reads a raw CSV/bank row, extracts fields, parses values, applies business rules, and returns a `Transaction` candidate.

**Proposed split:**
- `ImportRowProcessor` — coordinator (≤ 80 lines): owns the pipeline, calls collaborators
- `ImportFieldExtractor` — raw field access from row format
- `ImportFieldParser` — type parsing (date, decimal, description cleaning)
- `ImportRowValidator` — business rule validation (duplicates, required fields, range checks)

**Boundary:** `ImportFieldExtractor` is format-specific (CSV/OFX/QFX); if multiple formats exist, this naturally becomes `ICsvFieldExtractor`, `IOfxFieldExtractor` etc.

**Tests:** Unit test validator in isolation. Unit test parser with edge-case values (negative amounts, ISO vs MM/DD/YYYY dates, Unicode descriptions).

---

### 3. ChatService (487 lines → conversation management + action dispatch)

**Current:** One service handles chat session state, incoming message parsing, action detection, date resolution, category resolution, and executing each action type.

**Proposed split:**
- `ChatService` — conversation orchestrator (≤ 120 lines): session state, message flow, delegates to collaborators
- `ChatActionDispatcher` — receives a parsed `ChatAction` and routes to the appropriate handler
- Individual action handlers (implement `IChatActionHandler`): `AddTransactionActionHandler`, `QueryTransactionActionHandler`, `CategoryActionHandler`, etc.

**Pattern:** Command + Handler. `ChatActionParser` (see below) produces `ChatAction` objects; `ChatActionDispatcher` routes to `IChatActionHandler` implementations.

**Tests:** Unit test each action handler in isolation with mock repositories. Test dispatcher routing logic.

---

### 4. ChatActionParser (482 lines → action-type parsers)

**Current:** One class identifies and parses every type of chat action from user input.

**Proposed split:**
- `ChatActionParser` — coordinator (≤ 80 lines): tries each action parser in priority order
- Per-action parsers (implement `IChatActionTypeParser`): `AddTransactionParser`, `QueryParser`, `CategoryAssignmentParser`, `DateNavigationParser`, etc.

**Pattern:** Chain of Responsibility — each `IChatActionTypeParser.TryParse(input, out ChatAction? action)` returns `true` if it matched; coordinator walks the chain.

**Tests:** Unit test each action parser with raw user input strings — including ambiguous inputs that should fall through.

---

### 5. CategorySuggestionService (453 lines → suggestion scoring)

**Current:** One service generates suggestions, scores them against the transaction, ranks the ranked list, and saves the result.

**Proposed split:**
- `CategorySuggestionService` — orchestrator (≤ 100 lines): fetches candidates, delegates to scorer, persists results
- `CategorySuggestionScorer` — takes a `Transaction` and a list of `Category` candidates; returns scored + ranked `ScoredSuggestion` list
- `ScoringWeightConfig` — value object holding configurable weights (description match weight, merchant weight, amount proximity weight)

**Tests:** Unit test `CategorySuggestionScorer` with known transaction + category combinations; assert ranking order.

---

## Implementation Plan

### Phase 1: RuleSuggestionResponseParser Split (Standalone PR)

**Tasks:**
- [ ] Audit `RuleSuggestionResponseParser.cs` — list each field extraction block
- [ ] Create `IRuleSuggestionFieldParser<T>` interface in Application
- [ ] Create `MerchantFieldParser`, `CategoryFieldParser`, `ConfidenceFieldParser`, `MatchPatternFieldParser`
- [ ] Reduce `RuleSuggestionResponseParser` to coordinator
- [ ] Update DI registrations
- [ ] Write unit tests for each field parser
- [ ] Run `dotnet test --filter "Category!=Performance"` — green

---

### Phase 2: ImportRowProcessor Split (Standalone PR)

**Tasks:**
- [ ] Audit `ImportRowProcessor.cs` — label each block as extract/parse/validate
- [ ] Create `ImportFieldExtractor`, `ImportFieldParser`, `ImportRowValidator`
- [ ] Reduce `ImportRowProcessor` to coordinator
- [ ] Update DI
- [ ] Write unit tests for each extracted class
- [ ] Run tests — green

---

### Phase 3: ChatService + ChatActionParser Split (Combined PR — natural coupling)

**Tasks:**
- [ ] Define `IChatActionTypeParser` and `IChatActionHandler` interfaces
- [ ] Extract per-action type parsers from `ChatActionParser`
- [ ] Extract per-action handlers from `ChatService`
- [ ] Reduce `ChatService` to orchestrator; reduce `ChatActionParser` to coordinator
- [ ] Register all parsers and handlers in DI (scan or explicit)
- [ ] Update existing chat tests; add new per-handler unit tests
- [ ] Run tests — green

---

### Phase 4: CategorySuggestionService Split (Coupled with next suggestion feature PR)

**Tasks:**
- [ ] Extract `CategorySuggestionScorer` with `ScoringWeightConfig`
- [ ] Reduce `CategorySuggestionService` to orchestrator
- [ ] Unit tests for scorer ranking logic
- [ ] Run tests — green

---

### Phase 5: Remaining 13 Services (Opportunistic)

For each of the 13 remaining services exceeding 300 lines, apply the split when a feature naturally touches the file:

- Before writing new logic in a god service: split first (own commit), then add feature.
- Each split commit message must reference F-006 and Engineering Guide §8.
- No standalone refactor PRs for these until they become blockers.

---

## Testing Strategy

### For Each Split

- New extracted class: at least 3 unit tests (happy path, error path, edge case)
- Coordinator/orchestrator: integration-style unit test asserting it calls collaborators correctly (via mocks)
- No behavior regression: all pre-existing tests pass unchanged

### Naming Convention

```
{ExtractedClass}_{Scenario}_{ExpectedOutcome}
// e.g., MerchantFieldParser_MalformedInput_ReturnsEmpty
// e.g., ImportRowValidator_DuplicateTransaction_ReturnsDuplicateError
// e.g., CategorySuggestionScorer_HighMerchantMatchWeight_RanksMerchantMatchFirst
```

---

## Definition of Done

- [ ] All top-5 splits delivered in standalone PRs with tests
- [ ] Each remaining god service split before or during the next feature PR that touches it
- [ ] No new Application service is merged above 300 lines without architectural review
- [ ] `dotnet test --filter "Category!=Performance"` green after each split

---

## References

- [2026-04-09 Full Principle Audit — F-006](../docs/audit/2026-04-09-full-principle-audit.md#f-006-high--18-application-services-exceed-300-line-limit)
- Engineering Guide §8 (Clean Code — God services forbidden)
- Engineering Guide §24 (Forbidden: God services > ~300 lines)
- Engineering Guide §7 (SOLID — SRP, ISP)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-04-09 | Initial spec from Vic audit F-006 | Alfred (Lead) |
