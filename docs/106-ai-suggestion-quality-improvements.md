# Feature 106: AI Suggestion Quality Improvements

> **Status:** Done
> **Priority:** High
> **Dependencies:** Feature 105 (AI Suggestions UX Redesign — Done)

## Overview

Evaluate and improve the quality of AI-generated rule suggestions. Users report that the suggestions produced by the current Ollama-backed pipeline are not particularly useful — they may be too generic, miss obvious patterns, suggest incorrect categories, or produce low-confidence results that don't merit action. This feature audits the entire suggestion pipeline (prompt design, data context, response parsing, model configuration) and implements targeted improvements.

## Problem Statement

### Current State

The rule suggestion pipeline (`RuleSuggestionService` → `RuleSuggestionPromptBuilder` → `AiPrompts` → `OllamaAiService` → `RuleSuggestionResponseParser`) has several quality-limiting factors:

1. **Insufficient data context in prompts**
   - Only transaction *descriptions* are sent — no amounts, dates, or frequency information
   - The AI cannot distinguish a $5 daily coffee purchase from a $2,000 annual insurance payment, yet both are just "description strings"
   - No transaction count per description — the AI doesn't know which patterns appear 50 times vs. 2 times

2. **Prompt design issues**
   - The system prompt is generic ("financial categorization assistant") with no domain-specific guidance
   - No few-shot examples showing what a *good* suggestion looks like
   - The `FormatDescriptions` helper sends a flat numbered list with no grouping or frequency hints
   - Descriptions may include noise (check numbers, reference codes, trailing digits) that the AI treats as meaningful

3. **No description pre-processing**
   - Raw bank descriptions like `PURCHASE AUTHORIZED ON 03/07 AMAZON.COM AMZN.COM/BILLWA CARD 1234` are sent verbatim
   - Common prefixes (`PURCHASE AUTHORIZED ON`, `RECURRING PAYMENT`, `POS DEBIT`) and suffixes (`CARD 1234`, dates) inflate prompt size without adding value
   - Duplicate/near-duplicate descriptions aren't deduplicated — e.g., 30 variants of "AMAZON" each appear separately

4. **Token/context window limits**
   - `MaxTokens` is 2000 (response limit), but large transaction sets can exceed the model's context window
   - No truncation or sampling strategy for users with thousands of uncategorized transactions
   - Sending 500+ descriptions in one prompt dilutes the AI's attention

5. **Model capability assumptions**
   - Temperature 0.3 and 2000 max tokens are hardcoded in the prompt builder, not tunable per analysis type
   - No guidance on which Ollama models work best (some small models struggle with JSON output)
   - No validation that the configured model can handle structured JSON responses reliably

6. **Response parsing fragility**
   - JSON extraction uses simple `IndexOf('{')`/`LastIndexOf('}')` — fragile with nested JSON or AI preambles
   - No JSON schema validation after extraction
   - Parse failures silently return empty suggestions — user gets no feedback on *why* analysis produced nothing

7. **No feedback loop**
   - User feedback (thumbs up/down) is stored but never used to improve future suggestions
   - Accepted/dismissed suggestion patterns aren't fed back into subsequent analyses
   - The AI makes the same mistakes repeatedly because it has no learning signal

8. **Category suggestions are not AI-powered**
   - `CategorySuggestionService` relies solely on a static `MerchantKnowledgeBase` (~700 hardcoded patterns) and learned merchant mappings
   - For merchants not in the knowledge base, no suggestions are generated at all
   - The AI could suggest new category names and groupings based on spending patterns, but currently doesn't

### Target State

- Suggestions are **actionable and specific** — users accept 50%+ of high-confidence suggestions
- Prompts include **enriched context** (description frequency, amount ranges, temporal patterns)
- Transaction descriptions are **pre-processed** to remove noise and deduplicate
- **Large data sets are sampled** intelligently rather than dumped wholesale
- **User feedback history** influences which types of suggestions the AI generates
- Parse failures produce **diagnostic information** visible to the user
- **Suggestion quality metrics** (acceptance rate, confidence calibration) are tracked

---

## User Stories

### US-106-001: Description Pre-Processing

**As a** system
**I want to** clean and deduplicate transaction descriptions before sending them to the AI
**So that** the AI receives concise, meaningful input and produces more relevant suggestions.

**Acceptance Criteria:**
- [x] Common bank prefixes (e.g., `PURCHASE AUTHORIZED ON`, `POS DEBIT`, `RECURRING PAYMENT`) are stripped before prompt building
- [x] Trailing noise (card numbers, dates, reference codes) is removed or truncated
- [x] Near-duplicate descriptions are grouped and sent with frequency counts (e.g., `AMAZON.COM (×47)`)
- [x] Original (raw) descriptions are preserved in the domain — only cleaned versions go to the prompt
- [x] Unit tests verify cleaning rules against real-world bank description samples

### US-106-002: Enriched Prompt Context

**As a** system
**I want to** include frequency and amount context in AI prompts
**So that** suggestions are prioritized toward high-volume, high-value patterns.

**Acceptance Criteria:**
- [x] Each description group includes: cleaned description, occurrence count, and amount range (min–max)
- [x] Prompt template is updated to instruct the AI to prioritize patterns by frequency and dollar impact
- [x] The AI is told to set higher confidence for patterns appearing 10+ times
- [x] Few-shot examples are added to the prompt showing a good suggestion and a bad suggestion
- [x] Maximum descriptions sent is capped (e.g., top 100 by frequency) to stay within context window

### US-106-003: Improved Response Parsing & Error Reporting

**As a** system
**I want to** robustly parse AI responses and report failures clearly
**So that** users understand when and why the AI produced no suggestions.

**Acceptance Criteria:**
- [x] JSON extraction handles common LLM response wrappers (markdown code blocks, preamble text, trailing commentary)
- [x] Parse failures include a diagnostic message (e.g., "AI response was not valid JSON") surfaced to the UI as an analysis warning
- [x] Suggestions with invalid category names (not matching existing categories) are logged and skipped rather than silently dropped
- [x] Response parser unit tests cover: valid JSON, JSON in markdown block, JSON with preamble, completely invalid response, partial JSON

### US-106-004: Feedback-Informed Suggestions

**As a** system
**I want to** use historical acceptance/dismissal data to improve future suggestions
**So that** the AI stops suggesting patterns the user has already rejected.

**Acceptance Criteria:**
- [x] Previously dismissed suggestion patterns are included in the prompt as "DO NOT suggest these patterns" context
- [x] Previously accepted patterns are included as positive examples of what the user values
- [x] Acceptance rate per suggestion type is tracked (new rule, optimization, conflict, category)
- [x] If acceptance rate for a suggestion type drops below a threshold, that type is de-prioritized in the analysis

### US-106-005: Suggestion Quality Metrics

**As a** user
**I want to** see how well AI suggestions are performing over time
**So that** I can decide whether the AI feature is worth using.

**Acceptance Criteria:**
- [x] Track: total suggestions generated, accepted, dismissed, pending (per type)
- [x] Track: average confidence of accepted vs. dismissed suggestions (for calibration analysis)
- [x] Expose a simple summary on the AI suggestions page (e.g., "12 of 20 suggestions accepted this month")
- [x] Metrics are queryable via API endpoint for potential dashboard use

---

## Technical Design

### Description Pre-Processing Pipeline

```
Raw descriptions from DB
  ↓
TransactionDescriptionCleaner (new static class)
  ├─ StripCommonPrefixes() — regex-based, configurable prefix list
  ├─ StripTrailingNoise() — card numbers, dates, reference codes
  └─ NormalizeWhitespace() — collapse multiple spaces, trim
  ↓
DescriptionAggregator (new static class)
  ├─ GroupByNormalized() — fuzzy grouping (Levenshtein or simple prefix)
  └─ Rank by frequency, annotate with count + amount range
  ↓
Top N groups → prompt builder
```

New types:
```csharp
public record CleanedDescription(string Original, string Cleaned);

public record DescriptionGroup(
    string RepresentativeDescription,
    int Count,
    decimal? MinAmount,
    decimal? MaxAmount);
```

### Prompt Template Changes

Update `AiPrompts.NewRuleSuggestionPrompt` to:
- Accept `DescriptionGroup` items instead of raw strings
- Format as: `1. AMAZON.COM — 47 transactions, $5.99–$249.99`
- Include a "DO NOT SUGGEST" section with dismissed patterns
- Add 1–2 few-shot examples in the prompt

### Response Parser Hardening

- Replace `IndexOf('{')`/`LastIndexOf('}')` with a more robust JSON extraction that:
  - Strips markdown ` ```json ... ``` ` fences
  - Tries `JsonDocument.Parse` on the full response first
  - Falls back to bracket matching only if direct parse fails
  - Returns a `ParseResult<T>` with success/failure + diagnostic message

### Feedback Integration

```
RuleSuggestionPromptBuilder.BuildNewRulePrompt()
  now also accepts:
    - dismissedPatterns: IReadOnlyList<string> (from dismissed RuleSuggestions)
    - acceptedExamples: IReadOnlyList<(string Pattern, string Category)>

Prompt includes:
  PREVIOUSLY DISMISSED (do not re-suggest):
  - PATTERN1
  - PATTERN2

  EXAMPLES OF SUGGESTIONS THE USER FOUND HELPFUL:
  - Pattern "STARBUCKS" → Category "Dining" (accepted)
```

### Quality Metrics

New domain entity or value object:
```csharp
public record SuggestionMetrics(
    int TotalGenerated,
    int Accepted,
    int Dismissed,
    int Pending,
    decimal AverageAcceptedConfidence,
    decimal AverageDismissedConfidence);
```

Computed from existing `RuleSuggestion` and `CategorySuggestion` tables — no new database tables required initially. API endpoint: `GET /api/v1/suggestions/metrics`.

---

## Implementation Plan

### Phase 1: Description Pre-Processing

**Tasks:**
- [x] Create `TransactionDescriptionCleaner` with prefix/suffix stripping and normalization
- [x] Create `DescriptionAggregator` for frequency-based grouping with amount ranges
- [x] Unit tests for cleaner and aggregator using real bank description samples from `sample data/`
- [x] Integrate into `RuleSuggestionPromptBuilder.BuildNewRulePrompt()`

### Phase 2: Prompt Improvements

**Tasks:**
- [x] Update `AiPrompts.NewRuleSuggestionPrompt` to use enriched format with frequency/amounts
- [x] Add few-shot examples to the prompt (1 good suggestion, 1 bad suggestion to avoid)
- [x] Add cap on descriptions sent (top 100 by frequency)
- [x] Update system prompt with domain-specific guidance (merchant patterns, common bank prefixes)
- [x] Update prompt builder tests

### Phase 3: Response Parsing & Error Reporting

**Tasks:**
- [x] Harden `RuleSuggestionResponseParser.ExtractJson()` with markdown fence stripping and fallback strategy
- [x] Add `ParseResult<T>` return type with diagnostic messages
- [x] Surface parse failure diagnostics through the service layer to the UI
- [x] Add parser unit tests for edge cases (markdown blocks, preambles, partial JSON, empty response)

### Phase 4: Feedback Loop

**Tasks:**
- [x] Query dismissed suggestions for "DO NOT suggest" patterns in prompt builder
- [x] Query accepted suggestions for positive examples in prompt builder
- [x] Update `RuleSuggestionPromptBuilder.BuildNewRulePrompt()` signature and implementation
- [x] Unit tests for feedback-enriched prompts

### Phase 5: Quality Metrics

**Tasks:**
- [x] Create `SuggestionMetricsService` computing acceptance/dismissal rates from existing data
- [x] Add `GET /api/v1/suggestions/metrics` endpoint
- [x] Add simple metrics summary to AI suggestions page (ViewModel + UI)
- [x] Unit and integration tests for metrics calculation

---

## Testing Strategy

### Unit Tests

- `TransactionDescriptionCleanerTests` — prefix stripping, suffix removal, normalization, edge cases (empty, already clean, non-English)
- `DescriptionAggregatorTests` — grouping, frequency counting, amount range calculation, cap enforcement
- `RuleSuggestionPromptBuilderTests` — enriched format output, few-shot inclusion, dismissed/accepted pattern inclusion, description cap
- `RuleSuggestionResponseParserTests` — JSON extraction edge cases, diagnostic messages, invalid category handling
- `SuggestionMetricsServiceTests` — rate calculations, empty data, mixed states

### Integration Tests

- End-to-end prompt building with real sample data from `sample data/*.csv`
- Response parsing with actual Ollama response samples (captured and saved as test fixtures)

---

## Sample Data Reference

The repository includes real bank CSV files in `sample data/` that should be used for testing:
- `boa.csv` — Bank of America transaction descriptions
- `capone.csv` — Capital One transaction descriptions  
- `uhcu.csv` — UHCU (credit union) transaction descriptions

These contain real-world description formats with noise (dates, card numbers, reference codes) that the pre-processing pipeline must handle.

---

## Success Criteria

- Users accept ≥50% of high-confidence (≥0.8) suggestions (measured via metrics endpoint)
- Parse failure rate drops below 5% of analysis runs
- Prompt token usage reduced by ≥30% through description deduplication and cleaning
- Zero re-suggestions of previously dismissed patterns

---

## References

- Feature 105: AI Suggestions UX Redesign (UI layer — Done)
- [AiPrompts.cs](../src/BudgetExperiment.Application/Ai/AiPrompts.cs) — Current prompt templates
- [RuleSuggestionService.cs](../src/BudgetExperiment.Application/Categorization/RuleSuggestionService.cs) — Orchestration service
- [RuleSuggestionPromptBuilder.cs](../src/BudgetExperiment.Application/Categorization/RuleSuggestionPromptBuilder.cs) — Prompt construction
- [RuleSuggestionResponseParser.cs](../src/BudgetExperiment.Application/Categorization/RuleSuggestionResponseParser.cs) — Response parsing
- [OllamaAiService.cs](../src/BudgetExperiment.Infrastructure/ExternalServices/AI/OllamaAiService.cs) — Ollama HTTP client
- [MerchantKnowledgeBase.cs](../src/BudgetExperiment.Application/Categorization/MerchantKnowledgeBase.cs) — Static merchant patterns
