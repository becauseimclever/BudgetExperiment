# Archive: Features 111–120

> Features completed and archived. Listed in completion order.

---

## Feature 114: AI-Powered Category Discovery

> **Status:** Done
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

---

## Feature 112: API Performance Testing

> **Status:** Done

## Overview

Establish a repeatable API performance testing infrastructure for the Budget Experiment application. The goal is to catch performance regressions, establish baseline latency/throughput metrics for critical endpoints, and validate that optimizations (e.g., Feature 111 — `AsNoTracking`, concurrent queries) deliver measurable improvements. This first pass focuses exclusively on HTTP API endpoints — no UI or client-side performance testing.

## Problem Statement

### Current State

- **Zero performance tests.** The project has comprehensive unit and integration tests but no way to measure response time, throughput, or memory behavior under load.
- **No baselines.** Without baselines, there is no way to detect regressions or quantify the impact of architectural changes (e.g., Feature 111 optimizations).
- **Hot-path endpoints are untested under concurrency.** Endpoints like `/api/v1/calendar`, `/api/v1/transactions`, and `/api/v1/budgets` are used on nearly every page load but have never been exercised under concurrent user load.
- **Deployment target is a Raspberry Pi.** The production environment is resource-constrained, making performance awareness essential — a 50ms regression that is invisible on a developer workstation can become a 500ms regression on a Pi.

### Target State

- A dedicated performance test project that can be run locally or in CI.
- Baseline metrics (p50, p95, p99 latency; throughput; error rate) for critical API endpoints.
- Configurable load profiles: smoke, load, stress, and spike.
- HTML/JSON reports for historical comparison.
- Clear documentation so any contributor can run performance tests.

---

## Tool Selection: NBomber

### Why NBomber?

After evaluating the tools recommended by [Microsoft's ASP.NET Core load testing documentation](https://learn.microsoft.com/aspnet/core/test/load-tests), **NBomber** is the best fit for this project:

| Criterion | NBomber | k6 | JMeter | Bombardier |
|---|---|---|---|---|
| **Language** | C# (native .NET) | JavaScript | Java/XML | Go CLI |
| **Integration with existing test infra** | Excellent — NuGet package, xUnit compatible, can use `WebApplicationFactory` for in-process testing | Separate JS scripts, no .NET integration | Heavy XML config, separate JVM runtime | CLI only, no programmatic control |
| **Scenario modeling** | Rich scenario/step API with think time, data feeds, assertions | Good scripting but separate ecosystem | GUI-based, complex for code-first teams | Single URL bombardment only |
| **Reporting** | Built-in HTML, JSON, CSV, console reports with percentile breakdowns | CLI + cloud dashboard | GUI reports | CLI output only |
| **CI/CD friendly** | Yes — runs as a dotnet test, exit code on threshold failures | Yes — CLI based | Heavyweight, needs JVM | Yes but limited assertions |
| **Learning curve for .NET devs** | Minimal — write C#, use familiar patterns | Moderate — learn k6 JS API | High — XML, GUI, JVM | Low but limited |
| **In-process testing** | Yes — can create `HttpClient` from `WebApplicationFactory` for zero-network-overhead benchmarks | No — must hit a running server | No | No |

**Decision:** Use **NBomber** as the primary performance testing tool. It is pure .NET, integrates with our existing xUnit infrastructure, supports both in-process (`WebApplicationFactory`) and out-of-process (running API) testing, and generates rich reports.

### NBomber Concepts

- **Scenario**: A named workload definition (e.g., "get_transactions"). Contains one or more steps.
- **Step**: A single operation within a scenario (e.g., send GET request, parse response).
- **Load Simulation**: Controls how virtual users are injected — constant, ramp-up, spike, etc.
- **Assertion / Threshold**: Pass/fail criteria (e.g., p99 < 500ms, error rate < 1%).
- **Report**: Auto-generated HTML/JSON with latency percentiles, throughput, data transfer, and error breakdowns.

---

## Critical Endpoints to Test

Prioritized by traffic frequency and computational complexity:

| Priority | Endpoint | Method | Why |
|----------|----------|--------|-----|
| **P0** | `/api/v1/transactions` | GET | Highest traffic — loaded on every account view. Pagination, filtering. |
| **P0** | `/api/v1/calendar` | GET | Hot path — 9 sequential DB queries (see Feature 111). Most complex read endpoint. |
| **P0** | `/api/v1/accounts` | GET | Loaded on nearly every page. Lightweight but high frequency. |
| **P1** | `/api/v1/budgets` | GET | Budget dashboard — moderate complexity, multiple joins. |
| **P1** | `/api/v1/transactions` | POST | Primary write path. Important for import workflows. |
| **P1** | `/api/v1/recurring-transactions` | GET | Calendar and dashboard dependency. |
| **P2** | `/api/v1/reports` | GET | Aggregation-heavy; potential slow path with large datasets. |
| **P2** | `/api/v1/import` | POST | Bulk operation — relevant for stress testing (large CSV uploads). |
| **P2** | `/api/v1/suggestions` | GET | AI-coupled — variable latency depending on backend. |
| **P3** | `/health` | GET | Baseline smoke test — should always be < 10ms. |

---

## Load Profiles

### Smoke Test
- **Purpose:** Verify the system works under minimal load. Sanity check.
- **Load:** 1 virtual user, 10 seconds duration.
- **Thresholds:** All requests succeed (0% error rate), p99 < 1000ms.

### Load Test
- **Purpose:** Simulate expected production traffic. Establish baselines.
- **Load:** 10–20 concurrent users, 60 seconds duration, 5-second ramp-up.
- **Thresholds:** p95 < 500ms, p99 < 1000ms, error rate < 1%.

### Stress Test
- **Purpose:** Find the breaking point. How many concurrent users before degradation?
- **Load:** Ramp from 10 to 100 users over 120 seconds.
- **Thresholds:** Observe degradation curve — no hard pass/fail, but log where p99 exceeds 2000ms.

### Spike Test
- **Purpose:** Simulate sudden traffic bursts (e.g., all family members open the app simultaneously).
- **Load:** 5 users baseline → spike to 50 users for 10 seconds → back to 5 users.
- **Thresholds:** Recovery time < 10 seconds after spike. Error rate during spike < 5%.

---

## Technical Design

### Project Structure

```
tests/
  BudgetExperiment.Performance.Tests/
    BudgetExperiment.Performance.Tests.csproj
    Scenarios/
      HealthCheckScenario.cs         ← P3: Baseline smoke
      AccountsScenario.cs            ← P0: GET /accounts
      TransactionsScenario.cs        ← P0: GET & POST /transactions
      CalendarScenario.cs            ← P0: GET /calendar
      BudgetsScenario.cs             ← P1: GET /budgets
    Infrastructure/
      PerformanceTestBase.cs         ← Shared setup: HttpClient, auth, config
      TestDataSeeder.cs              ← Seed realistic data volumes for testing
    Profiles/
      SmokeProfile.cs                ← Smoke test load simulation config
      LoadProfile.cs                 ← Standard load test config
      StressProfile.cs               ← Stress test config
      SpikeProfile.cs                ← Spike test config
    nbomber-config.json              ← Optional external config overrides
    README.md                        ← How to run performance tests
```

### NuGet Dependencies

```xml
<PackageReference Include="NBomber" Version="6.*" />
<PackageReference Include="NBomber.Http" Version="6.*" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.*" />
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="xunit.runner.visualstudio" Version="3.*" />
```

### Test Modes

#### 1. In-Process (WebApplicationFactory)

Uses `WebApplicationFactory` to spin up the API in-process. No actual HTTP port needed — `HttpClient` talks directly to the test server. Best for CI, development machines, and isolating API performance from network noise.

```csharp
public class PerformanceTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly HttpClient Client;

    public PerformanceTestBase(CustomWebApplicationFactory factory)
    {
        Client = factory.CreateApiClient();
    }
}
```

**Pros:** Fast, no network overhead, deterministic, works in CI without port binding.  
**Cons:** Doesn't test real HTTP stack (Kestrel, middleware pipeline fully), uses SQLite instead of PostgreSQL.

#### 2. Out-of-Process (Running API)

Hits a real running API instance (local or remote). Tests the full stack including Kestrel, PostgreSQL, and middleware.

```csharp
var baseUrl = Environment.GetEnvironmentVariable("PERF_TEST_URL")
    ?? "http://localhost:5099";
var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
```

**Pros:** Tests the real stack. Can target the Raspberry Pi deployment.  
**Cons:** Requires a running server, results vary with hardware and network.

**Recommendation:** Start with in-process for CI and baseline regression detection. Add out-of-process as a secondary mode for deployment validation.

### Example Scenario: Transactions GET

```csharp
public class TransactionsScenario
{
    public static ScenarioProps Create(HttpClient client)
    {
        return Scenario.Create("get_transactions", async context =>
        {
            var request = Http.CreateRequest("GET", "/api/v1/transactions")
                .WithHeader("Authorization", "Bearer test-token");

            var response = await Http.Send(client, request);

            return response;
        })
        .WithLoadSimulations(
            Simulation.Inject(
                rate: 10,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(60)));
    }
}
```

### Example Test Runner

```csharp
[Trait("Category", "Performance")]
public class LoadTests : PerformanceTestBase
{
    public LoadTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact(Skip = "Run manually — not part of CI by default")]
    public void Transactions_Load_Test()
    {
        var scenario = TransactionsScenario.Create(Client);

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Json)
            .Run();

        // Assert baseline thresholds
        var stats = result.ScenarioStats[0];
        Assert.True(stats.Ok.Request.Percent >= 99, 
            $"Success rate {stats.Ok.Request.Percent}% is below 99% threshold");
        Assert.True(stats.Ok.Latency.Percent95 < 500, 
            $"p95 latency {stats.Ok.Latency.Percent95}ms exceeds 500ms threshold");
    }
}
```

### Authentication in Tests

The existing `CustomWebApplicationFactory` already provides auto-authentication with a test user. Performance tests reuse this infrastructure — no real OIDC tokens needed for in-process testing. For out-of-process tests against a real deployment, a service account token or test user credential would be injected via environment variable.

### Data Seeding

Performance tests need realistic data volumes to be meaningful. A `TestDataSeeder` will create:
- 3–5 accounts
- 500–1000 transactions spread across 6 months
- 10–15 recurring transactions
- 5–10 budget categories with goals
- 3–5 categorization rules

This mirrors a real household's data volume and ensures query performance reflects actual usage patterns.

---

## Implementation Plan

### Phase 1: Project Scaffolding

**Objective:** Create the performance test project, add NBomber dependencies, establish base infrastructure.

**Tasks:**
- [x] Create `tests/BudgetExperiment.Performance.Tests/` project
- [x] Add NuGet references: `NBomber`, `NBomber.Http`, `Microsoft.AspNetCore.Mvc.Testing`
- [x] Add project reference to `BudgetExperiment.Api` (for `WebApplicationFactory`)
- [x] Create `PerformanceTestBase` class reusing `CustomWebApplicationFactory`
- [x] Create `TestDataSeeder` for realistic data volumes
- [x] Add project to solution
- [x] Verify project builds and NBomber initializes

**Commit:**
```
feat(perf): scaffold performance test project with NBomber

- Add BudgetExperiment.Performance.Tests project
- Configure NBomber and NBomber.Http dependencies
- Create PerformanceTestBase with WebApplicationFactory integration
- Add TestDataSeeder for realistic data volumes

Refs: #112
```

### Phase 2: Smoke & Health Check Scenarios

**Objective:** Implement the simplest scenario to validate the pipeline end-to-end.

**Tasks:**
- [x] Create `HealthCheckScenario` targeting `GET /health`
- [x] Create `SmokeProfile` (1 user, 10 seconds)
- [x] Create smoke test runner with basic assertions
- [x] Verify HTML report generation
- [x] Document how to run in README.md

**Commit:**
```
feat(perf): add health check smoke test scenario

- Implement HealthCheckScenario with NBomber
- Add SmokeProfile load simulation
- Verify report generation pipeline
- Add performance test README

Refs: #112
```

### Phase 3: P0 Read Endpoint Scenarios

**Objective:** Cover the three highest-traffic read endpoints.

**Tasks:**
- [x] Create `AccountsScenario` — `GET /api/v1/accounts`
- [x] Create `TransactionsScenario` — `GET /api/v1/transactions`
- [x] Create `CalendarScenario` — `GET /api/v1/calendar`
- [x] Create `LoadProfile` (10–20 users, 60 seconds)
- [x] Add threshold assertions (p95 < 500ms, error rate < 1%)
- [x] Run and capture initial baselines

**Commit:**
```
feat(perf): add P0 read endpoint load test scenarios

- Add Accounts, Transactions, Calendar scenarios
- Configure LoadProfile with 10-20 concurrent users
- Establish p95/p99 latency thresholds
- Capture initial baseline metrics

Refs: #112
```

### Phase 4: Write Path & Stress Scenarios

**Objective:** Test write endpoints and push the system to find breaking points.

**Tasks:**
- [x] Create `TransactionsScenario` write variant — `POST /api/v1/transactions`
- [x] Create `BudgetsScenario` — `GET /api/v1/budgets`
- [x] Create `StressProfile` (ramp 10→100 users over 120 seconds)
- [x] Create `SpikeProfile` (baseline 5, spike to 50, recover)
- [x] Run stress tests, document degradation thresholds

**Commit:**
```
feat(perf): add write path and stress test scenarios

- Add transaction creation load scenario
- Add budgets read scenario
- Implement stress and spike load profiles
- Document degradation thresholds

Refs: #112
```

### Phase 5: CI Workflow & Reporting

**Objective:** Run performance tests automatically on a schedule and on every PR, with reports delivered as artifacts and PR comments.

**Tasks:**
- [x] Add `[Trait("Category", "Performance")]` to all tests
- [x] Ensure performance tests are excluded from default `dotnet test` runs (the existing CI filter already excludes non-unit tests)
- [x] Create dedicated GitHub Actions workflow `.github/workflows/performance.yml` (see design below)
- [x] Configure **scheduled runs** (weekly cron) for full load profile
- [x] Configure **PR-triggered runs** for smoke profile (lightweight gate)
- [x] Upload NBomber HTML/JSON reports as workflow artifacts
- [x] Post a performance summary as a sticky PR comment (latency percentiles + throughput)
- [x] Update project README with CI performance testing instructions

**Commit:**
```
feat(perf): add GitHub Actions performance workflow

- Create performance.yml with scheduled and PR triggers
- Run smoke profile on PRs, full load profile on schedule
- Upload HTML/JSON reports as artifacts
- Post performance summary as sticky PR comment

Refs: #112
```

### Phase 6: Baseline Tracking & Quality Gate

**Objective:** Establish a committed performance baseline and fail PRs that regress beyond acceptable thresholds.

**Tasks:**
- [x] Define baseline JSON schema (scenario name, p50/p95/p99 latency, throughput, error rate, timestamp)
- [x] Create `BaselineComparer` tool (simple .NET console app or script) that reads the NBomber JSON report and the baseline file, compares metrics, and outputs a Markdown summary with pass/fail
- [ ] Create initial baseline by running scheduled workflow and committing the JSON output
- [x] Add regression thresholds to a config file (`perf-thresholds.json`)
- [x] Fail the PR workflow step if any metric regresses beyond the configured threshold
- [x] Document the baseline update process

**Commit:**
```
feat(perf): add baseline tracking and quality gate

- Define baseline JSON schema for performance metrics
- Create BaselineComparer tool for regression detection
- Add perf-thresholds.json with configurable regression limits
- Fail PR checks when performance regresses beyond threshold

Refs: #112
```

---

## Running Performance Tests

### Quick Start (In-Process)

```powershell
# Run smoke tests only
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.Performance.Tests\BudgetExperiment.Performance.Tests.csproj --filter "Category=Performance&Category=Smoke"

# Run full load test suite
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.Performance.Tests\BudgetExperiment.Performance.Tests.csproj --filter "Category=Performance"
```

### Against a Running API (Out-of-Process)

```powershell
# Start the API
dotnet run --project c:\ws\BudgetExperiment\src\BudgetExperiment.Api\BudgetExperiment.Api.csproj --configuration Release

# In another terminal, run performance tests against it
$env:PERF_TEST_URL = "http://localhost:5099"
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.Performance.Tests\BudgetExperiment.Performance.Tests.csproj --filter "Category=Performance"
```

### Against Raspberry Pi Deployment

```powershell
$env:PERF_TEST_URL = "https://budget.becauseimclever.com"
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.Performance.Tests\BudgetExperiment.Performance.Tests.csproj --filter "Category=Performance&Category=Smoke"
```

---

## Feature 118: Upgrade PostgreSQL to Version 18

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

---

## Feature 119: Recurring Charge Detection & Suggestions

> **Status:** Done

## Overview

Automatically detect recurring charges from transaction history and suggest creating `RecurringTransaction` entries. Today, users must manually create recurring transactions. This feature analyzes imported transaction data to identify patterns — such as "Netflix" appearing monthly at $15.99 — and surfaces actionable suggestions the user can accept, dismiss, or tune.

## Problem Statement

### Current State

- Users manually create recurring transactions with a description, amount, account, recurrence pattern, and optional category.
- `ImportPatternValue` on `RecurringTransaction` lets the system auto-link future imports, but only **after** a recurring transaction already exists.
- There is no automated way to discover recurring charges buried in months of transaction history. Users must notice patterns themselves.

### Target State

- The system scans transaction history, groups by normalized merchant/description, and detects regular intervals and consistent amounts.
- Detected patterns are presented as **Recurring Charge Suggestions** with confidence scores.
- Users can accept a suggestion (creating a `RecurringTransaction` with pre-filled fields and import patterns), dismiss it, or adjust parameters before accepting.
- Already-linked transactions (those with a `RecurringTransactionId`) are excluded from detection to avoid duplicates.
- Detection can run on-demand or automatically after a CSV import.

---

## User Stories

### Detection

#### US-119-001: Detect Recurring Charges
**As a** budget user  
**I want** the system to analyze my transaction history and identify charges that recur on a regular schedule  
**So that** I don't have to manually spot and create recurring transactions

**Acceptance Criteria:**
- [x] Transactions are grouped by normalized description (stripping noise like POS/PURCHASE prefixes, trailing reference numbers)
- [x] Groups with ≥ 3 occurrences within the analysis window are evaluated for periodicity
- [x] Supported frequencies detected: Weekly, BiWeekly, Monthly, Quarterly, Yearly
- [x] Amount variance tolerance is configurable (default ±5 %) to handle slight fluctuations
- [x] Each detected pattern receives a confidence score (0.0–1.0) based on interval regularity, amount consistency, and sample size
- [x] Transactions already linked to a `RecurringTransaction` are excluded

#### US-119-002: View Recurring Charge Suggestions
**As a** budget user  
**I want** to see a list of detected recurring charge suggestions sorted by confidence  
**So that** I can quickly review and act on them

**Acceptance Criteria:**
- [x] Suggestions display: merchant/description, detected frequency, average amount, number of matching transactions, confidence score, and last occurrence date
- [x] Suggestions are sorted by confidence descending by default
- [x] User can filter by status (Pending, Accepted, Dismissed)

#### US-119-003: Accept a Recurring Charge Suggestion
**As a** budget user  
**I want** to accept a suggestion and have a `RecurringTransaction` created automatically  
**So that** future imports are tracked and budgeted correctly

**Acceptance Criteria:**
- [x] Accepting creates a `RecurringTransaction` with description, amount, detected recurrence pattern, account, and category (if transactions were categorized)
- [x] An `ImportPatternValue` is auto-generated from the normalized description so future imports link automatically
- [x] Existing matching transactions are retroactively linked to the new `RecurringTransaction` via `RecurringTransactionId`
- [x] Suggestion status changes to Accepted
- [ ] User can edit fields (description, amount, frequency, category) before confirming

#### US-119-004: Dismiss a Recurring Charge Suggestion
**As a** budget user  
**I want** to dismiss a suggestion I don't care about  
**So that** it doesn't clutter my pending list

**Acceptance Criteria:**
- [x] Dismissed suggestions are hidden from the default view
- [x] Dismissed suggestions can be viewed in a separate "Dismissed" list
- [x] Dismissing a suggestion does not delete it; it can be restored

### Post-Import Trigger

#### US-119-005: Auto-Detect After Import
**As a** budget user  
**I want** the system to automatically run recurring charge detection after I import transactions  
**So that** new patterns are surfaced without manual effort

**Acceptance Criteria:**
- [x] After a successful CSV import, detection runs for the affected account(s)
- [x] Only new/changed suggestions are surfaced (no duplicate suggestions for already-pending patterns)
- [x] User is notified of new suggestions (UI indicator or toast)

---

## Technical Design

### Architecture Changes

New components slot into the existing layered architecture:

| Layer | New Component | Responsibility |
|-------|---------------|----------------|
| Domain | `RecurringChargeSuggestion` entity | Persists detected patterns and user decisions |
| Domain | `RecurrenceDetector` (pure logic) | Groups transactions, detects intervals, scores confidence |
| Application | `IRecurringChargeDetectionService` | Orchestrates detection, stores suggestions |
| Application | `RecurringChargeSuggestionAcceptanceHandler` | Creates `RecurringTransaction` + links transactions on accept |
| Contracts | `RecurringChargeSuggestionResponse` | API DTO |
| Infrastructure | `RecurringChargeSuggestionRepository` | EF Core persistence |
| API | `RecurringChargeSuggestionsController` | REST endpoints |
| Client | `RecurringChargeSuggestions` component | UI for reviewing and acting on suggestions |

### Domain Model

```csharp
// New entity: src/BudgetExperiment.Domain/Recurring/RecurringChargeSuggestion.cs
public class RecurringChargeSuggestion
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public string NormalizedDescription { get; private set; }
    public string SampleDescription { get; private set; }          // Original description for display
    public MoneyValue AverageAmount { get; private set; }
    public RecurrenceFrequency DetectedFrequency { get; private set; }
    public int DetectedInterval { get; private set; }              // e.g. 1 for monthly, 2 for every-other-month
    public decimal Confidence { get; private set; }                // 0.0 – 1.0
    public int MatchingTransactionCount { get; private set; }
    public DateOnly FirstOccurrence { get; private set; }
    public DateOnly LastOccurrence { get; private set; }
    public Guid? CategoryId { get; private set; }                  // Most-used category from matched transactions
    public SuggestionStatus Status { get; private set; }           // Pending, Accepted, Dismissed
    public Guid? AcceptedRecurringTransactionId { get; private set; } // Set on accept
    public BudgetScope Scope { get; private set; }
    public Guid? OwnerUserId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
}
```

```csharp
// Pure domain logic: src/BudgetExperiment.Domain/Recurring/RecurrenceDetector.cs
public static class RecurrenceDetector
{
    public static IReadOnlyList<DetectedPattern> Detect(
        IReadOnlyList<Transaction> transactions,
        RecurrenceDetectionOptions options);
}

public record DetectedPattern(
    string NormalizedDescription,
    string SampleDescription,
    MoneyValue AverageAmount,
    RecurrenceFrequency Frequency,
    int Interval,
    decimal Confidence,
    IReadOnlyList<Transaction> MatchingTransactions,
    DateOnly FirstOccurrence,
    DateOnly LastOccurrence,
    Guid? MostUsedCategoryId);

public record RecurrenceDetectionOptions(
    int MinimumOccurrences = 3,
    decimal AmountVarianceTolerance = 0.05m,    // ±5%
    int AnalysisWindowMonths = 12);
```

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST   | `/api/v1/recurring-charge-suggestions/detect` | Trigger detection for an account or all accounts |
| GET    | `/api/v1/recurring-charge-suggestions` | List suggestions (filterable by status, account) |
| GET    | `/api/v1/recurring-charge-suggestions/{id}` | Get suggestion detail with matched transactions |
| POST   | `/api/v1/recurring-charge-suggestions/{id}/accept` | Accept suggestion → create RecurringTransaction |
| POST   | `/api/v1/recurring-charge-suggestions/{id}/dismiss` | Dismiss suggestion |

### Database Changes

- New table `RecurringChargeSuggestions` with columns matching the entity above.
- Index on `(AccountId, Status)` for filtered queries.
- Index on `(NormalizedDescription, AccountId)` for duplicate detection.

### Description Normalization

Reuse the merchant-normalization logic from the existing AI prompt system and `MerchantKnowledgeBase`. The normalizer should:

1. Strip common bank prefixes: `POS`, `PURCHASE`, `DEBIT`, `ACH`, `CHECKCARD`, etc.
2. Strip trailing reference/confirmation numbers (numeric sequences > 4 digits at end).
3. Strip trailing dates in common formats.
4. Trim and collapse whitespace.
5. Case-fold to uppercase for grouping, preserve original for display.

### Confidence Scoring

Confidence is a weighted composite:

| Factor | Weight | Description |
|--------|--------|-------------|
| Interval regularity | 40% | Standard deviation of days between occurrences vs. expected interval |
| Amount consistency | 30% | Coefficient of variation of amounts (lower = higher score) |
| Sample size | 20% | More occurrences = higher confidence (capped at 12) |
| Recency | 10% | Bonus if last occurrence is within one expected interval of today |

Minimum confidence threshold for surfacing: **0.5** (configurable).

### UI Components

- **RecurringChargeSuggestions page** — table/card list of pending suggestions with accept/dismiss actions.
- **Suggestion detail modal** — shows matched transactions, lets user edit fields before accepting.
- **Post-import banner** — "We found N new recurring charge patterns. [Review]" after CSV import.

---

## Implementation Plan

### Phase 1: Domain – Description Normalizer & Recurrence Detector

**Objective:** Build the pure domain logic for normalizing transaction descriptions and detecting recurrence patterns. Fully TDD.

**Tasks:**
- [ ] Create `DescriptionNormalizer` static class with bank-prefix stripping, reference-number removal, whitespace normalization
- [ ] Write unit tests for normalizer edge cases (various bank formats, international characters)
- [ ] Create `RecurrenceDetector` static class with grouping, interval detection, confidence scoring
- [ ] Write unit tests for detection: monthly charges, weekly charges, varying amounts within tolerance, noise rejection
- [ ] Create `RecurringChargeSuggestion` entity with factory method and status transitions
- [ ] Write unit tests for entity invariants

**Commit:**
```bash
git commit -m "feat(domain): add recurrence detection and description normalizer

- DescriptionNormalizer strips bank prefixes and trailing references
- RecurrenceDetector groups transactions and detects periodic patterns
- RecurringChargeSuggestion entity with confidence scoring
- Comprehensive unit tests for all detection scenarios

Refs: #119"
```

---

### Phase 2: Infrastructure – Persistence & Repository

**Objective:** Add EF Core configuration and repository for `RecurringChargeSuggestion`.

**Tasks:**
- [ ] Add `RecurringChargeSuggestion` to `BudgetDbContext`
- [ ] Create EF Core entity configuration (table, indexes, value conversions)
- [ ] Add migration
- [ ] Create `IRecurringChargeSuggestionRepository` interface in Domain
- [ ] Implement repository in Infrastructure
- [ ] Write integration tests with test database

**Commit:**
```bash
git commit -m "feat(infra): add RecurringChargeSuggestion persistence

- EF Core configuration with indexes on AccountId+Status
- Migration for RecurringChargeSuggestions table
- Repository implementation with filtered queries

Refs: #119"
```

---

### Phase 3: Application – Detection Service & Acceptance Handler

**Objective:** Orchestration layer that ties detection to persistence and handles accept/dismiss workflows.

**Tasks:**
- [ ] Create `IRecurringChargeDetectionService` with `DetectAsync(Guid? accountId)` and suggestion CRUD
- [ ] Implement service: load transactions, run detector, upsert suggestions (avoid duplicates)
- [ ] Create `RecurringChargeSuggestionAcceptanceHandler`: on accept, create `RecurringTransaction`, generate `ImportPatternValue`, link existing transactions
- [ ] Write unit tests with faked repositories
- [ ] Add post-import hook: call detection after `ImportService` completes

**Commit:**
```bash
git commit -m "feat(app): add recurring charge detection service

- DetectAsync analyzes transactions and persists suggestions
- AcceptanceHandler creates RecurringTransaction from suggestion
- Post-import trigger for automatic detection
- Unit tests with faked repositories

Refs: #119"
```

---

### Phase 4: Contracts & API Endpoints

**Objective:** Expose recurring charge suggestions via REST API.

**Tasks:**
- [ ] Add `RecurringChargeSuggestionResponse`, `DetectRecurringChargesRequest`, `AcceptRecurringChargeSuggestionRequest` DTOs to Contracts
- [ ] Create `RecurringChargeSuggestionsController` with endpoints per design table
- [ ] Add mapping between domain and contracts
- [ ] Write API integration tests (happy path + validation + 404)
- [ ] Verify OpenAPI spec generation

**Commit:**
```bash
git commit -m "feat(api): add recurring charge suggestion endpoints

- POST detect, GET list/detail, POST accept/dismiss
- Request validation and Problem Details error responses
- Integration tests with WebApplicationFactory

Refs: #119"
```

---

### Phase 5: Client UI

**Objective:** Blazor WebAssembly UI for reviewing and acting on recurring charge suggestions.

**Tasks:**
- [ ] Create `RecurringChargeSuggestions.razor` page with sortable/filterable table
- [ ] Create `RecurringChargeSuggestionDetail.razor` modal with editable fields and matched transaction list
- [ ] Add post-import notification banner to import page
- [ ] Add navigation link in sidebar
- [ ] Write bUnit tests for component logic

**Commit:**
```bash
git commit -m "feat(client): add recurring charge suggestions UI

- Suggestions page with confidence-sorted table
- Detail modal with editable accept flow
- Post-import notification banner
- Navigation integration

Refs: #119"
```

---

### Phase 6: Documentation & Cleanup

**Objective:** Final polish, documentation updates, and cleanup.

**Tasks:**
- [ ] Update API documentation / OpenAPI specs
- [ ] Add XML comments for public APIs
- [ ] Remove any TODO comments
- [ ] Final code review

**Commit:**
```bash
git commit -m "docs(recurring): add documentation for feature 119

- XML comments for public API surface
- OpenAPI spec updates

Refs: #119"
```

---

## Design Decisions & Notes

1. **Pure domain detection** — `RecurrenceDetector` is a static, pure function with no dependencies. This keeps it fast, testable, and free of infrastructure concerns. The application service handles I/O.

2. **Separate entity from `CategorySuggestion`** — Although both are "suggestion" concepts, recurring charge suggestions have different lifecycle (they create `RecurringTransaction` + link transactions, not categories/rules). A shared `SuggestionStatus` enum is reused.

3. **No AI required** — Detection is algorithmic (interval math + statistical scoring), not AI-driven. This keeps it fast, deterministic, and works without an AI provider configured. AI could enhance normalization in a future iteration.

4. **Duplicate avoidance** — When detection re-runs, existing pending suggestions for the same `(NormalizedDescription, AccountId)` are updated rather than duplicated.

5. **Amount tolerance** — Fixed-amount subscriptions get high confidence. Variable charges (e.g., utility bills) still match if within the configured tolerance, but with lower confidence.

## Conventional Commit Reference

| Type | When to Use | SemVer Impact |
|------|-------------|---------------|
| `feat` | New feature or capability | Minor |
| `fix` | Bug fix | Patch |
| `test` | Adding or fixing tests | None |
| `docs` | Documentation only | None |
| `refactor` | Code restructure, no feature/fix | None |

---

## Feature 115: Rules Listing Redesign

> **Status:** Done

## Overview

The categorization rules page currently renders every rule as a full card in a single unbounded list. As users accumulate rules (50+, 100+, or more), the page becomes an unmanageable scroll-fest that is difficult to navigate, slow to render, and painful to maintain. This feature redesigns the rules listing for scalability — both in UI ergonomics and backend performance.

## Problem Statement

### Current State

- **All rules loaded at once** — `GetAllAsync` calls `ListAsync(0, int.MaxValue)`, pulling every rule from the database in a single query with no pagination.
- **Flat, unbounded list** — Rules render as individual `RuleCard` components in a `foreach` loop. With 100+ rules, this produces a wall of cards with no grouping, filtering, or search.
- **No client-side filtering or search** — Users must visually scan the entire list to find a specific rule.
- **No grouping** — Rules targeting the same category are scattered across the list, sorted only by priority. Understanding "what rules feed into Groceries?" requires mentally filtering the whole list.
- **Performance concerns** — Loading hundreds of rules with their category navigation properties in one query, then rendering hundreds of Blazor components, will degrade both API response time and client-side rendering.
- **Card layout is space-inefficient** — Each rule card takes significant vertical space (name, pattern, match type badges, target category, action buttons). A compact table/row layout would show 3–5× more rules per viewport.

### Target State

- **Server-side pagination** — Rules API returns paginated results (default 25 per page) with total count.
- **Filtering & search** — Users can filter by category, active/inactive status, and search by rule name or pattern text.
- **Grouping by category** — A "group by category" toggle collapses rules into expandable category sections, making it easy to see all rules targeting a given category.
- **Compact list view** — A dense table/row layout is the default, showing rule name, pattern, match type, category, priority, and status in a single row. The full card view remains available as an optional toggle.
- **Bulk operations** — Select multiple rules for bulk delete, bulk activate/deactivate, or bulk category reassignment.
- **Rule count indicators** — Header shows total rule count, active count, and filtered count.
- **Responsive performance** — Page loads quickly even with 500+ rules via pagination and virtualization.

---

## User Stories

### Efficient Rule Browsing

#### US-115-001: Paginated Rule Listing
**As a** budget user with many rules  
**I want to** see rules in paginated pages  
**So that** the page loads quickly and I can navigate through rules without endless scrolling

**Acceptance Criteria:**
- [x] Rules are displayed in pages of 25 (configurable: 10, 25, 50, 100)
- [x] Pagination controls show current page, total pages, and total rule count
- [x] Page size preference persists across sessions (local storage) *(Slice 7)*
- [x] API returns paginated results with `X-Pagination-TotalCount` header

#### US-115-002: Search Rules
**As a** user looking for a specific rule  
**I want to** search rules by name or pattern text  
**So that** I can quickly find and edit a rule without scrolling

**Acceptance Criteria:**
- [x] Search input with debounced (300ms) server-side filtering
- [x] Searches against rule name and pattern text (case-insensitive)
- [x] Search results show match count
- [x] Clear search button resets to full list

#### US-115-003: Filter by Category
**As a** user managing rules for a specific category  
**I want to** filter rules to show only those targeting a specific category  
**So that** I can review and manage related rules together

**Acceptance Criteria:**
- [x] Category dropdown filter showing all categories with rule counts
- [x] "All Categories" option to reset filter
- [x] Filter combines with search (AND logic)

#### US-115-004: Filter by Status
**As a** user  
**I want to** filter rules by active/inactive status  
**So that** I can focus on active rules or review deactivated ones

**Acceptance Criteria:**
- [x] Status filter: All, Active Only, Inactive Only
- [x] Filter combines with category filter and search

### Compact Display

#### US-115-005: Table/Row View
**As a** user with many rules  
**I want to** see rules in a compact table layout  
**So that** I can see more rules at a glance without scrolling

**Acceptance Criteria:**
- [x] Table view with columns: Priority, Name, Pattern, Match Type, Category, Status, Actions
- [x] Sortable columns (click header to sort by that column)
- [x] Row actions (edit, activate/deactivate, delete) in a compact action column
- [x] Table view is the default for lists > 10 rules
- [x] View toggle (table/card) persists in local storage *(Slice 7)*

#### US-115-006: Group by Category
**As a** user  
**I want to** group rules by their target category  
**So that** I can see all rules feeding into each category in one place

**Acceptance Criteria:**
- [x] "Group by Category" toggle in the toolbar
- [x] Each category is a collapsible section header showing category name and rule count
- [x] Rules within each group are sorted by priority
- [x] Collapsed state persists during the session

### Bulk Operations

#### US-115-007: Bulk Selection and Actions
**As a** user managing many rules  
**I want to** select multiple rules and perform actions on them  
**So that** I can efficiently manage rules in bulk instead of one at a time

**Acceptance Criteria:**
- [x] Checkbox selection on each row/card
- [x] "Select All" on current page
- [x] Bulk action toolbar appears when items are selected: Delete, Activate, Deactivate
- [x] Confirmation dialog for bulk delete showing count
- [x] Success toast showing number of affected rules

---

## Technical Design

### Architecture Changes

No new projects. Changes span API, Application, Infrastructure, Client, and Contracts layers.

### API Endpoint Changes

| Method | Endpoint | Change |
|--------|----------|--------|
| GET | `/api/v1/categorizationrules` | Add query params: `page`, `pageSize`, `search`, `categoryId`, `status`, `sortBy`, `sortDirection`. Return paginated response with `X-Pagination-TotalCount` header. |
| DELETE | `/api/v1/categorizationrules/bulk` | **New.** Accept `{ ids: Guid[] }` for bulk delete. |
| POST | `/api/v1/categorizationrules/bulk/activate` | **New.** Accept `{ ids: Guid[] }` for bulk activation. |
| POST | `/api/v1/categorizationrules/bulk/deactivate` | **New.** Accept `{ ids: Guid[] }` for bulk deactivation. |

### Contracts (New/Modified DTOs)

```csharp
// New: Paginated request for rules listing
public record CategorizationRuleListRequest(
    int Page = 1,
    int PageSize = 25,
    string? Search = null,
    Guid? CategoryId = null,
    string? Status = null,       // "active", "inactive", or null for all
    string? SortBy = null,       // "priority", "name", "category", "createdAt"
    string? SortDirection = null  // "asc" or "desc"
);

// New: Paginated response
public record CategorizationRulePageResponse(
    IReadOnlyList<CategorizationRuleDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

// New: Bulk action request
public record BulkRuleActionRequest(IReadOnlyList<Guid> Ids);
```

### Repository Changes

```csharp
// New method on ICategorizationRuleRepository
Task<(IReadOnlyList<CategorizationRule> Items, int TotalCount)> ListPagedAsync(
    int page,
    int pageSize,
    string? search,
    Guid? categoryId,
    bool? isActive,
    string? sortBy,
    string? sortDirection,
    CancellationToken cancellationToken = default);
```

### Service Changes

- `GetAllAsync` is preserved for backward compatibility (used by Apply Rules and AI features) but a new `ListPagedAsync` method is added for the listing UI.
- New `BulkDeleteAsync`, `BulkActivateAsync`, `BulkDeactivateAsync` methods.

### UI Components

- **RulesToolbar** — New component: search input, category filter dropdown, status filter, view toggle (table/card), group-by toggle.
- **RulesTable** — New component: compact table view of rules with sortable headers and inline actions.
- **RulesPagination** — New component: page navigation, page size selector, total count display.
- **BulkActionBar** — New component: floating/sticky bar that appears when items are selected, showing selected count and bulk action buttons.
- **Rules.razor** — Refactored to compose the new components, defaulting to table view.

### Performance Considerations

#### Listing Performance
- Server-side pagination prevents loading all rules at once.
- Server-side search and filtering push work to the database (indexed queries) instead of client-side scanning.
- EF Core query uses `Skip`/`Take` for efficient pagination.
- Consider adding a database index on `CategorizationRule.Name` for search performance.
- Bulk operations use a single round-trip per action instead of N individual calls.

#### Rule Application Performance (ApplyRules / Categorization Engine)

The current `CategorizationEngine.ApplyRulesAsync` has an **O(N × M)** evaluation pattern: every transaction (N) is tested against every active rule (M) until a match is found. This has several scaling concerns:

| Aspect | Current Behavior | Risk at Scale |
|--------|-----------------|---------------|
| **Rule evaluation** | N×M: each transaction tested against all rules until first match | 10K transactions × 100 rules = up to 1M evaluations |
| **Regex compilation** | Pre-compiled per entity instance via `_compiledRegex` field | **Lost on every request** — rules are re-loaded from DB each time, so compiled regex is rebuilt per request |
| **Regex timeout** | 1-second timeout per match (`RegexMatchTimeoutException` → returns `false`) | A single pathological regex can block for 1 second × N transactions |
| **Rule loading** | Fresh `GetActiveByPriorityAsync()` query per apply request, no cache | Moderate — OK for infrequent apply, wasteful if called repeatedly |
| **Transaction fetch (by IDs)** | Individual `GetByIdAsync` in a loop — N+1 problem | **Critical**: 1000 IDs = 1000 DB round-trips |
| **String methods** | `Contains`, `StartsWith`, `EndsWith` use native `StringComparison` | Fast — O(description length), negligible overhead |

**Mitigation strategies (in priority order):**

1. **Batch transaction loading** — Replace the per-ID loop in `ApplyRulesAsync` with a single `GetByIdsAsync(IReadOnlyList<Guid>)` batch query. Eliminates N+1 problem.

2. **In-memory rule cache** — Cache active rules (with compiled regexes) using `IMemoryCache` with short TTL (e.g., 30 seconds) or invalidation on rule CRUD. Avoids rebuilding compiled regexes per request and eliminates redundant DB queries.

3. **Separate non-regex from regex rules** — Partition active rules into two groups during evaluation:
   - **String rules** (Contains/StartsWith/EndsWith/Exact): Very fast, evaluate first.
   - **Regex rules**: More expensive, evaluate only if no string rule matched. Most categorization patterns are simple `Contains` matches ("WALMART" → Groceries). Only evaluate regex rules for transactions that didn't match any string rule.

4. **Compiled regex with `RegexOptions.Compiled`** — The current `BuildRegex` does NOT use `RegexOptions.Compiled` (which JIT-compiles to IL). Adding this flag trades startup time for significantly faster matching on repeated evaluations. Ideal when rules are cached.

5. **Regex complexity validation** — At rule creation time, validate that regex patterns are not pathologically complex (e.g., reject nested quantifiers like `(a+)+`). The 1-second timeout is a safety net, not a solution.

6. **Database-side matching for simple patterns** — For `Contains`/`StartsWith`/`EndsWith` rules, consider pushing matching to SQL (`LIKE`, `ILIKE` on PostgreSQL) to avoid loading transactions into memory. This is a bigger architectural change but would be the ultimate performance win for non-regex rules.

**Recommendation:** Items 1–3 are low-risk, high-impact improvements that should be included in this feature. Items 4–6 are optimizations to consider if profiling shows bottlenecks at higher scale.

---

## Implementation Plan

Each slice is a vertical cut delivering testable, deployable value from API through client. Every slice includes its own tests and can be merged independently.

### Slice 1: Paginated Rules Listing (End-to-End)

**Objective:** Replace the unbounded rule list with a paginated table view — API through UI in one slice.

**Tasks:**
- [x] Add `CategorizationRuleListRequest` and `CategorizationRulePageResponse` to Contracts
- [x] Add `ListPagedAsync` to `ICategorizationRuleRepository` interface
- [x] Implement `ListPagedAsync` in `CategorizationRuleRepository` (EF Core `Skip`/`Take`, ordered by priority)
- [x] Add `ListPagedAsync` to `ICategorizationRuleService` and implement in `CategorizationRuleService`
- [x] Update `CategorizationRulesController` GET endpoint to accept `page` and `pageSize` params, return `X-Pagination-TotalCount` header
- [x] Update `IBudgetApiService` client to call paginated endpoint
- [x] Create `RulesPagination` component (page nav, page size selector, total count)
- [x] Create `RulesTable` component (compact table: Priority, Name, Pattern, Match Type, Category, Status, Actions)
- [x] Update `RulesViewModel` with pagination state (`CurrentPage`, `PageSize`, `TotalCount`)
- [x] Refactor `Rules.razor` to use `RulesTable` + `RulesPagination` as default view
- [x] Write unit tests: service pagination logic, ViewModel pagination state
- [x] Write bUnit tests: `RulesTable` renders columns, `RulesPagination` emits page changes
- [x] Write API integration test: paginated GET returns correct page/count

**Commit:**
```bash
git commit -m "feat(rules): paginated table view for rules listing

- Server-side pagination with page/pageSize params
- Compact table layout replaces card list as default
- RulesPagination component with page size selector
- X-Pagination-TotalCount response header
- Unit, bUnit, and integration tests

Refs: #115"
```

---

### Slice 2: Search & Filter

**Objective:** Add search by name/pattern and filter by category/status — full vertical slice from DB query to UI toolbar.

**Tasks:**
- [x] Extend `ListPagedAsync` repository to support `search`, `categoryId`, `isActive` filter params
- [x] Add database index on `Name` column for search performance
- [x] Extend `ListPagedAsync` service to pass through filter params
- [x] Update controller GET endpoint to accept `search`, `categoryId`, `status` query params
- [x] Update `IBudgetApiService` client to pass filter params
- [x] Create `RulesToolbar` component (search input with debounce, category dropdown, status filter)
- [x] Update `RulesViewModel` with filter state (`SearchText`, `FilterCategoryId`, `FilterStatus`)
- [x] Wire toolbar changes to reload paginated data (reset to page 1 on filter change)
- [x] Write unit tests: repository filtering queries, service filter pass-through
- [x] Write bUnit tests: toolbar emits filter events, debounced search
- [x] Write ViewModel tests: filter changes reset page, trigger reload

**Commit:**
```bash
git commit -m "feat(rules): search and filter for rules listing

- Search by rule name or pattern text (debounced, server-side)
- Filter by category and active/inactive status
- RulesToolbar component with search, category dropdown, status filter
- Database index on Name for search performance

Refs: #115"
```

---

### Slice 3: Sortable Columns

**Objective:** Add server-side sorting by clicking table column headers.

**Tasks:**
- [x] Extend `ListPagedAsync` repository to support `sortBy` and `sortDirection` params
- [x] Extend service and controller to pass through sort params
- [x] Update `RulesTable` headers to be clickable with sort direction indicators
- [x] Update `RulesViewModel` with sort state (`SortBy`, `SortDirection`)
- [x] Write unit tests: repository sort queries, ViewModel sort state toggle
- [x] Write bUnit test: clicking header emits sort event

**Commit:**
```bash
git commit -m "feat(rules): sortable columns in rules table

- Click column headers to sort by priority, name, category, createdAt
- Toggle ascending/descending with sort direction indicators
- Server-side sorting via repository query

Refs: #115"
```

---

### Slice 4: Group by Category

**Objective:** Add a toggle to view rules grouped under collapsible category headers.

**Tasks:**
- [x] Add group-by-category toggle to `RulesToolbar`
- [x] Create grouped view rendering in `Rules.razor` (category section headers with rule count, collapsible)
- [x] Update `RulesViewModel` with `IsGroupedByCategory` state and grouping logic
- [x] When grouped, sort within each category group by priority
- [x] Write ViewModel tests: grouping logic, collapse state
- [x] Write bUnit test: grouped sections render with correct counts

**Commit:**
```bash
git commit -m "feat(rules): group by category view

- Toggle to group rules under collapsible category headers
- Each group shows category name and rule count
- Rules within groups sorted by priority

Refs: #115"
```

---

### Slice 5: Bulk Operations

**Objective:** Add multi-select and bulk delete/activate/deactivate — full vertical slice from API endpoints to selection UI.

**Tasks:**
- [x] Add `BulkRuleActionRequest` to Contracts
- [x] Add `BulkDeleteAsync`, `BulkActivateAsync`, `BulkDeactivateAsync` to repository interface and implementation
- [x] Add bulk methods to service interface and implementation
- [x] Add bulk endpoints to controller (`DELETE bulk`, `POST bulk/activate`, `POST bulk/deactivate`)
- [x] Add checkbox column to `RulesTable` with "Select All" on current page
- [x] Create `BulkActionBar` component (sticky bar with selected count and action buttons)
- [x] Update `RulesViewModel` with selection state and bulk action methods
- [x] Add confirmation dialog for bulk delete
- [x] Update `IBudgetApiService` with bulk API calls
- [x] Write unit tests: service bulk operations, ViewModel selection logic
- [x] Write bUnit tests: checkbox selection, bulk action bar visibility
- [x] Write API integration tests: bulk endpoints

**Commit:**
```bash
git commit -m "feat(rules): bulk rule operations with multi-select

- Checkbox selection with Select All on current page
- Bulk delete, activate, deactivate endpoints
- BulkActionBar appears when items selected
- Confirmation dialog for destructive bulk actions

Refs: #115"
```

---

### Slice 6: Rule Application Performance

**Objective:** Fix the critical performance issues in the categorization engine for applying rules at scale.

**Tasks:**
- [x] Add `GetByIdsAsync(IReadOnlyList<Guid>)` to `ITransactionRepository` and implement — replace N+1 loop in `ApplyRulesAsync`
- [x] Add `IMemoryCache`-based caching for active rules in `CategorizationEngine` (short TTL, invalidated on rule CRUD)
- [x] Add `RegexOptions.Compiled` to `BuildRegex` for cached rule instances
- [x] Partition rule evaluation: evaluate string rules first, then regex rules only for unmatched transactions
- [x] Add regex complexity validation at rule creation (reject nested quantifiers)
- [x] Write unit tests: batch transaction fetch, cache hit/miss, evaluation ordering
- [x] Write performance regression test: apply 100 rules against 1000 transactions within threshold

**Commit:**
```bash
git commit -m "perf(rules): optimize rule application for large datasets

- Batch transaction loading (eliminates N+1 queries)
- In-memory cache for compiled rules
- RegexOptions.Compiled for cached regex instances
- String rules evaluated before regex rules
- Regex complexity validation at creation time

Refs: #115"
```

---

### Slice 7: View Toggle & Preference Persistence

**Objective:** Allow switching between table and card views, persist user preferences.

**Tasks:**
- [x] Add view toggle (table/card) to `RulesToolbar`
- [x] Preserve existing `RuleCard` component for card view mode
- [x] Persist view preference and page size in local storage via JS interop
- [x] Add rule count indicators to page header (total, active, filtered)
- [x] Write ViewModel test: view toggle state
- [x] Write bUnit test: correct view renders based on toggle

**Commit:**
```bash
git commit -m "feat(rules): view toggle and preference persistence

- Table/card view toggle with local storage persistence
- Page size preference persists across sessions
- Rule count indicators in page header

Refs: #115"
```

---

### Slice 8: Polish & Accessibility

**Objective:** Final polish, accessibility audit, and documentation.

**Tasks:**
- [x] Verify keyboard navigation in table (tab through rows, Enter to edit)
- [x] Add ARIA labels to table, sort indicators, pagination controls
- [x] Update OpenAPI spec documentation for new/modified endpoints
- [x] Add XML comments for new public APIs
- [x] Manual testing with 200+ rules dataset
- [x] Review and clean up any TODO comments

**Commit:**
```bash
git commit -m "docs(rules): polish, accessibility, and documentation

- Keyboard navigation and ARIA labels
- OpenAPI spec updates for pagination and bulk endpoints
- XML comments for public API surface

Refs: #115"
```

---

## Open Questions

1. **Virtual scrolling vs. pagination?** — Pagination is simpler and works better with server-side filtering. Virtual scrolling could be considered later if users prefer a continuous scroll experience.
2. **Should we consolidate duplicate/overlapping rules?** — A future feature could detect and suggest merging rules with identical patterns or categories. Out of scope for this feature but worth noting.
3. **Rule priority reflow** — When filtering by category, should priority numbers be the global priority or category-local? Recommend keeping global priorities visible but sorting within group by priority.
4. **Cache invalidation strategy** — For the rule cache (Slice 6), should we use TTL-based expiry, event-based invalidation on CRUD, or both? TTL is simpler; event-based is more precise. Recommend TTL with manual invalidation on write operations.
5. **DB-side pattern matching** — Pushing `Contains`/`StartsWith`/`EndsWith` matching to PostgreSQL `ILIKE` queries would eliminate the need to load transactions into memory for simple rules. This is a significant architectural change — worth a separate feature if profiling warrants it.

---

## Feature 116: Rule Consolidation and Merge Suggestions

> **Status:** Done

(Content merged from standalone file 116-rule-consolidation-merge-suggestions.md — details preserved in this consolidated archive document.)

---

## Feature 117: Initial Page Load Performance

> **Status:** Done

## Overview

The initial page load experience involves multiple sequential delays that compound into a frustrating wait. A user opening the app sees a loading spinner for several seconds while the Blazor WASM framework downloads and initializes, then gets redirected to Authentik for authentication, then waits again on redirect back while the WASM runtime re-initializes and checks auth state, and finally waits a third time while the landing page fetches its data from the API. Each phase is individually tolerable; stacked together they create a perception that the app is sluggish.

This feature attacks all four phases: WASM payload size, authentication round-trip overhead, post-auth framework re-initialization, and initial data loading.

## Problem Statement

### Current State

The page load has four sequential bottleneck phases:

**Phase A — WASM Download & Parse (~2–4s on first load):**
- The Blazor WebAssembly client has no publish-time optimizations configured. No IL trimming (`PublishTrimmed`), no AOT compilation, no globalization invariance, no lazy-loading of assemblies.
- The `_framework/` directory ships the full .NET runtime plus all referenced assemblies — approximately 15–25 MB uncompressed. Gzip-compressed `.wasm.gz` variants exist (served by ASP.NET Core's static web assets middleware), but Brotli pre-compression is not explicitly enabled for static serving.
- The nginx reverse proxy caches `_framework/` files for only 1 day (`max-age=86400`). These fingerprinted, immutable files could be cached for a year.

**Phase B — Auth Redirect (~1–3s):**
- On first visit (no token cached), the client boots the WASM runtime, fetches `api/v1/config` to get OIDC settings, determines the user is unauthenticated, then triggers a full-page `forceLoad: true` redirect to Authentik. Authentik renders its login page (or auto-redirects if session exists). After login, Authentik redirects back to the app.
- This redirect back triggers another full WASM download + parse cycle — the browser may serve from cache, but the framework still needs to initialize, re-fetch config, check auth state, and resolve the user.
- The `preconnect` hint to Authentik is present in `index.html`, which helps, but the OIDC metadata discovery (`/.well-known/openid-configuration`) and JWKS fetch are still sequential blocking operations.

**Phase C — Post-Auth Bootstrap (~1–2s):**
- After the auth redirect returns, `AuthInitializer.razor` checks `AuthenticationStateProvider`, waits for `Task<AuthenticationState>`, and only then hides the loading overlay and renders the Router.
- The `Program.cs` startup itself is sequential: create `HttpClient` → fetch `api/v1/config` → configure OIDC → build service provider → render root component. No parallelism.

**Phase D — Data Loading (~0.5–2s):**
- Once the landing page component renders, it calls its ViewModel's `OnInitializedAsync`, which makes one or more API calls to load data (e.g., budget summary, accounts, transactions).
- These API calls go through `TokenRefreshHandler` (adds bearer token) and `ScopeHandler` (adds scope header) before hitting the server. The server then runs sequential database queries (see Feature 111 for that concern).
- No data prefetching occurs during phases A–C. The first API call doesn't start until the page component mounts.

**Total perceived wait: 4–10 seconds** from URL entry to seeing usable data, depending on network conditions and cache state.

### Target State

- **First paint with content** under 1.5 seconds (cached) / 3 seconds (cold).
- **Auth redirect** is a one-time cost that feels instant on subsequent visits (token caching, silent renew).
- **Post-auth data** appears within 500ms of the app becoming interactive (prefetching during bootstrap).
- **Return visits** feel near-instant — service worker serves cached framework files, auth token is still valid, data API responses are pre-cached or eagerly fetched.

---

## Optimization Areas

### Area 1: Reduce WASM Payload Size (High Impact)

**1a. Enable IL Trimming for Published Builds**

The Client `.csproj` has zero publish optimizations. Adding trimming removes unused code paths from the shipped assemblies.

```xml
<!-- BudgetExperiment.Client.csproj (publish-only settings) -->
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>partial</TrimMode>
</PropertyGroup>
```

`TrimMode=partial` is conservative — only trims assemblies explicitly marked as trimmable (most BCL assemblies are). This avoids breaking reflection-heavy code while still removing significant dead weight. Expected savings: 20–40% of framework DLL size.

**1b. Globalization Invariant Mode (If Acceptable)**

If the app doesn't need full ICU globalization data (locale-specific date/number formatting beyond what's handled client-side via JS), enabling invariant mode removes the ~2 MB ICU data files:

```xml
<InvariantGlobalization>true</InvariantGlobalization>
```

**Caution:** This breaks `CultureInfo` formatting. Since the client already detects timezone via JS interop and formats dates/currencies in the ViewModel layer, this may be viable — but requires validation that no Blazor component relies on `ToString("C")` or similar culture-dependent calls. A safer alternative is `BlazorWebAssemblyLoadAllGlobalizationData=false` combined with `HybridGlobalization=true` to ship only the needed locale.

**1c. Lazy-Load Non-Critical Assemblies**

Assemblies for features the user won't access on first load (AI chat, import, reconciliation, reporting) can be deferred:

```xml
<ItemGroup>
    <BlazorWebAssemblyLazyLoad Include="BudgetExperiment.Contracts.wasm" />
    <!-- Other non-landing-page assemblies -->
</ItemGroup>
```

This requires identifying which assemblies are needed for the landing page vs. deferred pages, and adding `OnNavigateAsync` in the Router to load them on demand.

---

### Area 2: Aggressive Caching (High Impact, Zero Risk)

**2a. Immutable Cache Headers for Fingerprinted Assets**

ASP.NET Core's static web assets already fingerprint `_framework/` files (e.g., `BudgetExperiment.Client.29s0yetj7k.wasm`) and set `Cache-Control: max-age=31536000, immutable`. This is correct.

However, the nginx reverse proxy overrides this with `max-age=86400` (1 day) for `_framework/` files. This is unnecessarily conservative for fingerprinted, immutable content.

**Fix in nginx config:**
```nginx
location /_framework/ {
    proxy_pass http://127.0.0.1:5099;
    # ...existing proxy headers...
    
    # Fingerprinted files are immutable - cache aggressively
    proxy_cache_valid 200 365d;
    expires 365d;
    add_header Cache-Control "public, max-age=31536000, immutable";
}
```

**2b. Service Worker for Offline Cache**

A Blazor WASM service worker (`service-worker.published.js`) can cache all framework files on first load so subsequent visits are served entirely from the local cache — zero network requests for the WASM runtime.

```xml
<!-- BudgetExperiment.Client.csproj -->
<ServiceWorkerAssetsManifest>service-worker-assets.js</ServiceWorkerAssetsManifest>

<ItemGroup>
    <ServiceWorker Include="wwwroot/service-worker.js" PublishedContent="wwwroot/service-worker.published.js" />
</ItemGroup>
```

This is the single biggest improvement for return visits. The framework files download once and are served from `CacheStorage` on every subsequent visit, eliminating Phase A entirely.

**2c. Cache OIDC Discovery Metadata**

The OIDC `/.well-known/openid-configuration` and JWKS endpoints are fetched on every app initialization. These change extremely rarely. Caching them in `sessionStorage` with a short TTL (5–15 minutes) avoids a network round-trip on most visits.

---

### Area 3: Optimize Auth Flow (Medium Impact)

**3a. Silent Token Renewal**

The OIDC library (`Microsoft.AspNetCore.Components.WebAssembly.Authentication`) supports silent renewal via hidden iframe. When a user returns to the app with an expired access token but a valid Authentik session cookie, silent renewal can obtain a new token without a full redirect cycle.

Ensure `AuthenticationService.js` is configured with:
- `automaticSilentRenew: true`
- Appropriate `silentRedirectUri` pointing to a lightweight HTML page

This eliminates Phase B entirely for users whose Authentik session is still alive.

**3b. Faster First-Visit Auth Redirect**

Currently the client boots the entire WASM runtime before discovering the user is unauthenticated and redirecting. For a first-time visitor, this means downloading 15+ MB of WASM just to be told "go to Authentik."

**Option: Early auth check via JavaScript.** Before `blazor.webassembly.js` loads, a small inline script can check for an existing auth token in storage. If none exists, redirect to Authentik immediately — skipping the entire WASM download.

```html
<script>
    // Fast-path: if no auth token cached, redirect before loading WASM
    (function() {
        const oidcKey = Object.keys(sessionStorage).find(k => k.startsWith('oidc.'));
        if (!oidcKey && !window.location.pathname.startsWith('/authentication')) {
            // No token cached - will need to redirect anyway
            // Defer to WASM to handle redirect (needs OIDC config)
            // But we can start preloading Authentik assets
            const link = document.createElement('link');
            link.rel = 'prefetch';
            link.href = 'https://authentik.becauseimclever.com/application/o/authorize/?...';
            document.head.appendChild(link);
        }
    })();
</script>
```

A more aggressive approach: embed the OIDC authority URL directly in `index.html` and redirect via JS without waiting for WASM, but this duplicates config and introduces a maintenance burden.

**3c. Preload Config Endpoint**

The `api/v1/config` fetch during `Program.cs` startup is sequential with everything else. Issuing it as a `<link rel="preload">` or `fetch()` in the HTML `<head>` allows the browser to start the request while the WASM runtime downloads:

```html
<link rel="preload" href="api/v1/config" as="fetch" crossorigin="anonymous" />
```

Then in `Program.cs`, check if the preloaded response is available before making a new request.

---

### Area 4: Optimize Post-Auth Bootstrap (Medium Impact)

**4a. Parallel Service Initialization**

In `Program.cs`, the `api/v1/config` fetch is awaited before configuring services. If the OIDC settings were embedded in `index.html` (via server-side templating or a `<script>` tag with JSON), the config fetch could be eliminated entirely:

```html
<!-- In index.html, rendered by the API's fallback handler -->
<script id="app-config" type="application/json">
    {"authentication":{"mode":"oidc","authority":"...","clientId":"..."}}
</script>
```

The client reads this from the DOM — zero network requests for config.

**4b. Prefetch Landing Page Data During Auth Resolution**

While `AuthInitializer` waits for `AuthenticationStateProvider` to resolve, the app is idle. If we know the user will land on the budget page, we can start prefetching that data in parallel with auth resolution:

```csharp
// In AuthInitializer - fire and forget the data prefetch
var prefetchTask = PrefetchLandingPageDataAsync(ct);
var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();

if (authState.User.Identity?.IsAuthenticated == true)
{
    await prefetchTask; // Ensure data is ready
    // ...proceed to render
}
```

This requires careful handling — the prefetch needs a valid token, so it can only start after auth is confirmed. But if the token is already cached (return visit), both can proceed in parallel.

**4c. Skeleton Screens Instead of Spinner**

Replace the loading spinner with skeleton screens that match the layout of the landing page. This doesn't reduce actual load time but significantly improves perceived performance — the user sees a page "forming" rather than staring at a spinner.

---

### Area 5: Server-Side Response Compression (Low-Medium Impact)

**5a. Enable Brotli Response Compression in ASP.NET Core**

Currently no `UseResponseCompression` middleware is configured. While ASP.NET Core serves pre-compressed `.gz` files for static assets, API JSON responses are not compressed:

```csharp
// In Program.cs
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json" });
});

// Early in the pipeline
app.UseResponseCompression();
```

This compresses API responses (JSON payloads) on the fly. Budget summary, transaction lists, and other API responses can be 60–80% smaller with Brotli.

**5b. Enable Brotli Pre-Compression for Static Assets**

Ensure the published output includes `.br` files alongside `.gz` for all `_framework/` assets. Brotli typically achieves 15–25% better compression than gzip for WASM and DLL files.

**5c. Nginx Brotli Module**

If the nginx reverse proxy supports `ngx_brotli`, enable it:

```nginx
brotli on;
brotli_types application/wasm application/javascript text/css application/json;
brotli_comp_level 6;
```

---

### Area 6: HTTP/2 Server Push / Early Hints (Low Impact, Nice-to-Have)

**6a. 103 Early Hints**

ASP.NET Core supports 103 Early Hints, which tells the browser to start fetching critical subresources before the main response arrives:

```csharp
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Headers.Link = 
            "</_framework/blazor.webassembly.js>; rel=preload; as=script, " +
            "</css/app.css>; rel=preload; as=style";
        await context.Response.WriteAsync("", context.RequestAborted); // 103
    }
    await next();
});
```

This is marginal but essentially free to implement.

---

## Implementation Plan

### Phase 1: Quick Wins — Caching & Compression
1. [x] Fix nginx `_framework/` cache headers (immutable, 1-year)
2. [x] Add `UseResponseCompression` with Brotli to API
3. [x] Preload `api/v1/config` via `<link rel="preload">` in `index.html`
4. [x] Embed client config as inline JSON in `index.html` (eliminate config fetch)

### Phase 2: Payload Reduction
5. [x] Enable IL trimming (`PublishTrimmed`, `TrimMode=partial`) for Release builds
6. [x] Enable hybrid globalization (`HybridGlobalization=true`)
7. [ ] ~~Identify and configure lazy-loaded assemblies~~ — Deferred: requires splitting Client into separate Razor Class Libraries (all pages compile into one DLL; Contracts/Shared are lightweight)
8. [x] Verify no trimming regressions — Release publish succeeds; all 5,119 unit/integration tests pass

### Phase 3: Service Worker & Offline Cache
9. [x] Add Blazor PWA service worker for framework caching
10. [x] Configure service worker cache versioning strategy
11. [x] Offline-first verified — service worker caches all `_framework/` assets via manifest; API/index.html excluded from cache

### Phase 4: Auth Flow Optimization
12. [x] Verify silent token renewal is working — Already implemented via `TokenRefreshHandler` (automatic 401 → token refresh → retry)
13. [x] Add skeleton screens for post-auth page loading
14. [x] Prefetch landing page data during auth resolution — Parallelized Calendar's 5 sequential API calls (`LoadAccounts`, `LoadCategories`, `LoadCalendarData`, `LoadPastDueItems`, `LoadBudgetSummary`) via `Task.WhenAll`
15. [ ] ~~Explore early JS-based auth redirect~~ — Explored, deferred: sessionStorage is per-tab (empty on new tabs); constructing OIDC authorize URL with PKCE in plain JS duplicates fragile logic; service worker already eliminates WASM download on return visits

### Phase 5: Measurement & Validation
16. [ ] Add Lighthouse CI or manual Lighthouse audit to track Core Web Vitals (LCP, FID, CLS) — deferred to post-deploy
17. [ ] Measure before/after on throttled connections — deferred to post-deploy
18. [x] Document final payload sizes (see Measured Results below)

---

## Measured Results (Release Publish, 2026-03-17)

### Payload Sizes

| Metric | Before (no trimming) | After (trimmed) | Improvement |
|--------|---------------------|-----------------|-------------|
| `_framework/` total (all files) | 37.92 MB (642 files) | 19.02 MB (195 files) | **50% reduction** |
| Brotli transfer size | 6.68 MB | 3.32 MB | **50.3% reduction** |
| Largest files (Brotli) | — | dotnet.native 954 KB, CoreLib 541 KB, Client.dll 365 KB | — |
| ICU data (HybridGlobalization) | ~2 MB single file | 3 shards totaling ~600 KB | **70% reduction** |

### Optimizations Applied

| Optimization | Impact |
|-------------|--------|
| IL Trimming (`TrimMode=partial`) | 50% payload reduction |
| HybridGlobalization | ~1.4 MB ICU savings |
| Response Compression (Brotli) | 60–80% API response reduction |
| nginx immutable caching | Zero revalidation on return visits |
| Service Worker | Zero network for `_framework/` on cached visits |
| Inline config embedding | Eliminated `/api/v1/config` round-trip |
| Calendar parallel loading | 5 API calls concurrent vs sequential |
| Skeleton loading screen | Perceived instant first paint |

### Success Metrics

| Metric | Original Estimate | Target | Measured |
|--------|-------------------|--------|----------|
| WASM payload (Brotli) | ~8–12 MB | < 5 MB | **3.32 MB** ✅ |
| First Contentful Paint (cold) | 3–5s | < 2s | Pending Lighthouse |
| Time to Interactive (cold) | 5–10s | < 3.5s | Pending Lighthouse |
| Time to Interactive (cached/return) | 3–5s | < 1.5s | Expected near-instant (SW) |
| Post-auth data render | 1–2s | < 500ms | Expected ~max(5 calls) |
| Lighthouse Performance score | ~40–60 | > 75 | Pending |

---

## Risks & Considerations

- **IL Trimming** can break reflection-based code. `TrimMode=partial` is conservative but still requires testing. Run full E2E suite after enabling.
- **Invariant Globalization** will break `ToString("C")` and similar culture-dependent formatting. Needs audit of all Blazor components.
- **Service Worker** cache invalidation must be correct — a stale service worker serving an old framework version against a new API is a common PWA bug. Blazor's built-in service worker handles this via cache versioning.
- **Embedded Config** in `index.html` — Implemented via `MapFallback` handler in `Program.cs` that reads `index.html`, injects config JSON as `<script id="app-config" type="application/json">`, and serves dynamically. A fetch interceptor in `index.html` transparently provides this to Blazor's `HttpClient`.
- **Silent Token Renewal** requires Authentik to set appropriate CORS and cookie policies for the iframe-based flow. If Authentik doesn't cooperate, this falls back to the full redirect.
- **Brotli on Raspberry Pi** — encoding is CPU-intensive. Use pre-compressed files rather than on-the-fly compression for static assets. For API responses, `BrotliCompressionLevel.Fastest` keeps CPU usage reasonable.

---

## References

- Feature 111: Pragmatic Performance Optimizations (server-side query performance)
- [Blazor WASM Performance Best Practices (Microsoft Docs)](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance)
- [ASP.NET Core Response Compression](https://learn.microsoft.com/en-us/aspnet/core/performance/response-compression)
- [Blazor PWA / Service Worker](https://learn.microsoft.com/en-us/aspnet/core/blazor/progressive-web-app)

---

## Feature 111: Pragmatic Performance Optimizations

> **Status:** Done

**What it did:** Identified and resolved concrete performance overhead in the clean architecture: added `AsNoTracking()` on all read-only repository queries, parallelized 9–10 sequential DB round trips in hot-path endpoints (`CalendarGridService`, `TransactionListService`) via `Task.WhenAll`, consolidated 73 scoped DI registrations (removed duplicate backward-compat entries), and fixed unbounded eager loading in `AccountRepository.GetByIdWithTransactionsAsync`.

**Key decisions:**
- Retained full clean layering for multi-repo orchestration and domain logic; only removed abstraction overhead in commodity read-only CRUD paths
- Repository interfaces with a single EF Core implementation and no realistic polymorphism use case were consolidated (but not removed wholesale)

---

## Feature 113: External Integration Feature

> **Status:** On Hold — external dependencies, no timeline

**What it does:** Requires external service integration. Paused indefinitely pending third-party availability.

---

## Feature 120: Plugin System

> **Status:** Cancelled — on hold indefinitely; no value under Kakeibo/Kaizen pivot

**What it did:** Planned a plugin architecture (`BudgetExperiment.Plugin.Abstractions` SDK + `BudgetExperiment.Plugin.Hosting`) allowing third-party DLL drop-in or NuGet-distributed extensions to add API endpoints, Blazor pages, sidebar navigation items, and domain event subscribers. Cancelled in favour of Kakeibo/Kaizen philosophy pivot.

**Key decisions:**
- Domain event scaffolding on `Transaction` (`_domainEvents`) would have been the extension point; never dispatched prior to cancellation
