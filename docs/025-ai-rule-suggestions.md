# Feature 025: AI-Powered Rule Suggestions (Local Models Only)

## Overview

Implement an AI-powered assistant that analyzes existing categories, categorized transactions, and categorization rules to suggest optimizations and new rules. This feature uses **exclusively local AI models** (e.g., Ollama with LLaMA, Mistral, or similar) to ensure all data remains on-premise and no information is ever sent to cloud services. The AI assistant helps users discover patterns in their transaction data that could be automated with rules, identifies redundant or conflicting rules, and suggests improvements to existing rules.

## Problem Statement

Users create categorization rules manually based on their knowledge of their transactions. However:

### Current State

- Users must manually identify patterns in transaction descriptions
- No automated way to discover categorization opportunities
- Existing rules may become redundant or suboptimal over time
- Users may miss common patterns that could be automated
- Rule optimization requires manual review of large transaction datasets
- No privacy-respecting AI assistance available

### Target State

- AI assistant analyzes transaction patterns and suggests new rules
- Identifies uncategorized transactions that share common patterns
- Detects redundant, overlapping, or conflicting rules
- Suggests rule consolidation or optimization
- All processing happens locally with zero data exfiltration
- User maintains full control—suggestions require explicit approval
- Works offline without internet connectivity

---

## User Stories

### AI Configuration

#### US-025-001: Configure Local AI Model
**As a** user  
**I want to** configure which local AI model to use  
**So that** I can choose the model that best fits my hardware capabilities

**Acceptance Criteria:**
- [ ] Can specify Ollama endpoint URL (default: http://localhost:11434)
- [ ] Can select from available models on the Ollama instance
- [ ] Can test connection and model availability
- [ ] Settings persist across sessions
- [ ] Graceful handling when AI is unavailable

#### US-025-002: View AI Status
**As a** user  
**I want to** see the status of the local AI service  
**So that** I know if suggestions are available

**Acceptance Criteria:**
- [ ] Status indicator shows: Connected, Disconnected, or Not Configured
- [ ] Shows currently selected model name
- [ ] Can refresh status manually

### Rule Suggestions

#### US-025-003: Suggest Rules for Uncategorized Transactions
**As a** user  
**I want to** get AI suggestions for rules to categorize uncategorized transactions  
**So that** I can quickly create rules for common patterns

**Acceptance Criteria:**
- [ ] AI analyzes uncategorized transaction descriptions
- [ ] Groups similar descriptions and suggests patterns
- [ ] Recommends appropriate category based on similar categorized transactions
- [ ] Shows confidence level for each suggestion
- [ ] Can accept, modify, or dismiss each suggestion

#### US-025-004: Suggest Rule Optimizations
**As a** user  
**I want to** get AI suggestions for improving existing rules  
**So that** my rules are more effective and maintainable

**Acceptance Criteria:**
- [ ] Identifies rules that never match any transactions
- [ ] Identifies rules with overlapping patterns
- [ ] Suggests pattern simplification (e.g., "AMAZON" instead of "AMAZON.COM" + "AMAZON PRIME" + "AMZN")
- [ ] Suggests regex conversion for multiple similar rules
- [ ] Shows reasoning for each suggestion

#### US-025-005: Detect Rule Conflicts
**As a** user  
**I want to** be alerted to conflicting or redundant rules  
**So that** I can resolve ambiguities in my rule set

**Acceptance Criteria:**
- [ ] Identifies rules where same description could match multiple rules
- [ ] Shows which rule would win based on priority
- [ ] Suggests priority adjustments or rule modifications
- [ ] Identifies completely redundant rules (same pattern, same category)

#### US-025-006: Bulk Suggestion Generation
**As a** user  
**I want to** generate suggestions for all my transaction data at once  
**So that** I can do a comprehensive rule review

**Acceptance Criteria:**
- [ ] "Analyze All" button triggers comprehensive analysis
- [ ] Progress indicator during analysis (may take time for large datasets)
- [ ] Results organized by suggestion type (new rules, optimizations, conflicts)
- [ ] Can export suggestions for later review

### Suggestion Management

#### US-025-007: Review Suggestion Queue
**As a** user  
**I want to** review all pending suggestions in one place  
**So that** I can efficiently process AI recommendations

**Acceptance Criteria:**
- [ ] Suggestion queue shows all pending suggestions
- [ ] Can filter by type (new rule, optimization, conflict)
- [ ] Can sort by confidence, date, or category
- [ ] Batch accept/dismiss multiple suggestions

#### US-025-008: Accept Suggestion
**As a** user  
**I want to** accept an AI suggestion with one click  
**So that** I can quickly implement good recommendations

**Acceptance Criteria:**
- [ ] Accept creates/updates rule automatically
- [ ] Can modify suggestion before accepting
- [ ] Confirmation shows what will change
- [ ] Undo available for recent acceptances

#### US-025-009: Dismiss Suggestion
**As a** user  
**I want to** dismiss suggestions I don't want  
**So that** they don't clutter my queue

**Acceptance Criteria:**
- [ ] Can dismiss individual suggestions
- [ ] Can optionally provide reason (helps improve future suggestions)
- [ ] Dismissed suggestions don't reappear for same pattern
- [ ] Can view dismissed suggestions history

#### US-025-010: Provide Feedback on Suggestions
**As a** user  
**I want to** rate suggestion quality  
**So that** the system can learn my preferences

**Acceptance Criteria:**
- [ ] Thumbs up/down on each suggestion
- [ ] Feedback stored locally for future prompt tuning
- [ ] Can view feedback history

---

## Technical Design

### Architecture Changes

- New `IAiService` interface for local AI integration
- New `OllamaAiService` implementation using Ollama HTTP API
- New `IRuleSuggestionService` for generating and managing suggestions
- New `RuleSuggestion` entity for persisting suggestions
- New API endpoints for AI configuration and suggestions
- New Blazor components for suggestion management UI
- Settings extension for AI configuration

### Domain Model

#### RuleSuggestion Entity

```csharp
public sealed class RuleSuggestion
{
    public Guid Id { get; private set; }
    public SuggestionType Type { get; private set; }
    public SuggestionStatus Status { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Reasoning { get; private set; } = string.Empty;
    public decimal Confidence { get; private set; }  // 0.0 to 1.0
    
    // For new rule suggestions
    public string? SuggestedPattern { get; private set; }
    public RuleMatchType? SuggestedMatchType { get; private set; }
    public Guid? SuggestedCategoryId { get; private set; }
    
    // For optimization suggestions
    public Guid? TargetRuleId { get; private set; }
    public string? OptimizedPattern { get; private set; }
    
    // For conflict detection
    public IReadOnlyList<Guid> ConflictingRuleIds { get; private set; } = Array.Empty<Guid>();
    
    // Metadata
    public int AffectedTransactionCount { get; private set; }
    public IReadOnlyList<string> SampleDescriptions { get; private set; } = Array.Empty<string>();
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public string? DismissalReason { get; private set; }
    public bool? UserFeedbackPositive { get; private set; }

    public static RuleSuggestion CreateNewRuleSuggestion(
        string title,
        string description,
        string reasoning,
        decimal confidence,
        string suggestedPattern,
        RuleMatchType suggestedMatchType,
        Guid suggestedCategoryId,
        int affectedTransactionCount,
        IReadOnlyList<string> sampleDescriptions);

    public static RuleSuggestion CreateOptimizationSuggestion(
        string title,
        string description,
        string reasoning,
        decimal confidence,
        Guid targetRuleId,
        string optimizedPattern);

    public static RuleSuggestion CreateConflictSuggestion(
        string title,
        string description,
        string reasoning,
        IReadOnlyList<Guid> conflictingRuleIds);

    public void Accept();
    public void Dismiss(string? reason = null);
    public void ProvideFeedback(bool positive);
}

public enum SuggestionType
{
    NewRule,           // Suggest creating a new rule
    PatternOptimization,  // Suggest improving an existing rule's pattern
    RuleConsolidation,    // Suggest merging multiple rules
    RuleConflict,         // Alert about conflicting rules
    UnusedRule            // Alert about rule that never matches
}

public enum SuggestionStatus
{
    Pending,
    Accepted,
    Dismissed
}
```

#### AI Service Interfaces

```csharp
public interface IAiService
{
    /// <summary>
    /// Checks if the AI service is available and configured.
    /// </summary>
    Task<AiServiceStatus> GetStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets available models from the AI service.
    /// </summary>
    Task<IReadOnlyList<AiModelInfo>> GetAvailableModelsAsync(CancellationToken ct = default);

    /// <summary>
    /// Sends a prompt to the AI and returns the response.
    /// </summary>
    Task<AiResponse> CompleteAsync(AiPrompt prompt, CancellationToken ct = default);
}

public sealed record AiServiceStatus
{
    public bool IsAvailable { get; init; }
    public string? CurrentModel { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed record AiModelInfo
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
}

public sealed record AiPrompt
{
    public string SystemPrompt { get; init; } = string.Empty;
    public string UserPrompt { get; init; } = string.Empty;
    public decimal Temperature { get; init; } = 0.3m;  // Lower for more deterministic
    public int MaxTokens { get; init; } = 2000;
}

public sealed record AiResponse
{
    public bool Success { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public int TokensUsed { get; init; }
    public TimeSpan Duration { get; init; }
}
```

#### Rule Suggestion Service

```csharp
public interface IRuleSuggestionService
{
    /// <summary>
    /// Analyzes uncategorized transactions and suggests new rules.
    /// </summary>
    Task<IReadOnlyList<RuleSuggestion>> SuggestNewRulesAsync(
        int maxSuggestions = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Analyzes existing rules and suggests optimizations.
    /// </summary>
    Task<IReadOnlyList<RuleSuggestion>> SuggestOptimizationsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Detects conflicts and redundancies in existing rules.
    /// </summary>
    Task<IReadOnlyList<RuleSuggestion>> DetectConflictsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Runs comprehensive analysis and returns all suggestions.
    /// </summary>
    Task<RuleSuggestionAnalysis> AnalyzeAllAsync(
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets pending suggestions.
    /// </summary>
    Task<IReadOnlyList<RuleSuggestion>> GetPendingSuggestionsAsync(
        SuggestionType? typeFilter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Accepts a suggestion and creates/updates the rule.
    /// </summary>
    Task<CategorizationRule> AcceptSuggestionAsync(
        Guid suggestionId,
        CancellationToken ct = default);

    /// <summary>
    /// Dismisses a suggestion.
    /// </summary>
    Task DismissSuggestionAsync(
        Guid suggestionId,
        string? reason = null,
        CancellationToken ct = default);
}

public sealed record RuleSuggestionAnalysis
{
    public IReadOnlyList<RuleSuggestion> NewRuleSuggestions { get; init; } = Array.Empty<RuleSuggestion>();
    public IReadOnlyList<RuleSuggestion> OptimizationSuggestions { get; init; } = Array.Empty<RuleSuggestion>();
    public IReadOnlyList<RuleSuggestion> ConflictSuggestions { get; init; } = Array.Empty<RuleSuggestion>();
    public int UncategorizedTransactionsAnalyzed { get; init; }
    public int RulesAnalyzed { get; init; }
    public TimeSpan AnalysisDuration { get; init; }
}

public sealed record AnalysisProgress
{
    public string CurrentStep { get; init; } = string.Empty;
    public int PercentComplete { get; init; }
}
```

### AI Configuration Settings

```csharp
// Extension to existing AppSettings or new AiSettings entity
public sealed class AiSettings
{
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";
    public string ModelName { get; set; } = "llama3.2";
    public decimal Temperature { get; set; } = 0.3m;
    public int MaxTokens { get; set; } = 2000;
    public int TimeoutSeconds { get; set; } = 120;
    public bool IsEnabled { get; set; } = true;
}
```

### Prompt Engineering

The system will use carefully crafted prompts to analyze transaction data:

```csharp
public static class AiPrompts
{
    public const string SystemPrompt = @"
You are a financial categorization assistant. Your job is to analyze transaction descriptions 
and suggest categorization rules. You must:
1. Identify common patterns in transaction descriptions
2. Suggest specific, accurate matching patterns
3. Recommend appropriate categories based on similar transactions
4. Explain your reasoning clearly
5. Never hallucinate or make up transaction data

Respond ONLY with valid JSON in the specified format. Do not include any other text.
";

    public const string NewRuleSuggestionPrompt = @"
Analyze these uncategorized transaction descriptions and suggest categorization rules.

Existing Categories:
{categories}

Existing Rules:
{existingRules}

Uncategorized Transaction Descriptions:
{descriptions}

For each pattern you identify, respond with JSON:
{
  ""suggestions"": [
    {
      ""pattern"": ""the pattern to match"",
      ""matchType"": ""Contains|StartsWith|Exact|Regex"",
      ""categoryName"": ""suggested category"",
      ""confidence"": 0.0-1.0,
      ""reasoning"": ""why this pattern and category"",
      ""sampleMatches"": [""descriptions that would match""]
    }
  ]
}
";

    public const string OptimizationPrompt = @"
Analyze these categorization rules and suggest optimizations.

Current Rules:
{rules}

Transaction Match Statistics:
{matchStats}

Look for:
1. Rules that could be simplified
2. Multiple rules that could be consolidated into one
3. Rules that never match any transactions
4. Patterns that could be more specific or general

Respond with JSON:
{
  ""suggestions"": [
    {
      ""type"": ""simplify|consolidate|remove|broaden|narrow"",
      ""targetRuleIds"": [""rule-ids""],
      ""suggestedPattern"": ""new pattern if applicable"",
      ""reasoning"": ""explanation""
    }
  ]
}
";
}
```

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/ai/status` | Get AI service status |
| GET | `/api/v1/ai/models` | List available models |
| PUT | `/api/v1/ai/settings` | Update AI settings |
| POST | `/api/v1/ai/suggestions/generate` | Generate new suggestions |
| GET | `/api/v1/ai/suggestions` | Get pending suggestions |
| GET | `/api/v1/ai/suggestions/{id}` | Get suggestion details |
| POST | `/api/v1/ai/suggestions/{id}/accept` | Accept a suggestion |
| POST | `/api/v1/ai/suggestions/{id}/dismiss` | Dismiss a suggestion |
| POST | `/api/v1/ai/suggestions/{id}/feedback` | Provide feedback |
| POST | `/api/v1/ai/analyze` | Run comprehensive analysis |

### Request/Response DTOs

```csharp
public sealed record AiStatusResponse
{
    public bool IsAvailable { get; init; }
    public bool IsEnabled { get; init; }
    public string? CurrentModel { get; init; }
    public string Endpoint { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
}

public sealed record AiSettingsRequest
{
    public string OllamaEndpoint { get; init; } = string.Empty;
    public string ModelName { get; init; } = string.Empty;
    public decimal Temperature { get; init; }
    public int MaxTokens { get; init; }
    public int TimeoutSeconds { get; init; }
    public bool IsEnabled { get; init; }
}

public sealed record RuleSuggestionDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Reasoning { get; init; } = string.Empty;
    public decimal Confidence { get; init; }
    public string? SuggestedPattern { get; init; }
    public string? SuggestedMatchType { get; init; }
    public Guid? SuggestedCategoryId { get; init; }
    public string? SuggestedCategoryName { get; init; }
    public Guid? TargetRuleId { get; init; }
    public string? TargetRuleName { get; init; }
    public IReadOnlyList<Guid> ConflictingRuleIds { get; init; } = Array.Empty<Guid>();
    public int AffectedTransactionCount { get; init; }
    public IReadOnlyList<string> SampleDescriptions { get; init; } = Array.Empty<string>();
    public DateTime CreatedAtUtc { get; init; }
}

public sealed record GenerateSuggestionsRequest
{
    public string? SuggestionType { get; init; }  // NewRule, Optimization, Conflict, or null for all
    public int MaxSuggestions { get; init; } = 10;
}

public sealed record AnalysisResponse
{
    public int NewRuleSuggestions { get; init; }
    public int OptimizationSuggestions { get; init; }
    public int ConflictSuggestions { get; init; }
    public int UncategorizedTransactionsAnalyzed { get; init; }
    public int RulesAnalyzed { get; init; }
    public double AnalysisDurationSeconds { get; init; }
}

public sealed record DismissSuggestionRequest
{
    public string? Reason { get; init; }
}

public sealed record FeedbackRequest
{
    public bool IsPositive { get; init; }
}
```

### Database Changes

New table: `RuleSuggestions`

| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| Type | int | NOT NULL |
| Status | int | NOT NULL |
| Title | varchar(200) | NOT NULL |
| Description | text | NOT NULL |
| Reasoning | text | NOT NULL |
| Confidence | decimal(3,2) | NOT NULL |
| SuggestedPattern | varchar(500) | NULL |
| SuggestedMatchType | int | NULL |
| SuggestedCategoryId | uuid | NULL, FK |
| TargetRuleId | uuid | NULL, FK |
| OptimizedPattern | varchar(500) | NULL |
| ConflictingRuleIds | jsonb | NULL |
| AffectedTransactionCount | int | NOT NULL |
| SampleDescriptions | jsonb | NOT NULL |
| CreatedAtUtc | timestamp | NOT NULL |
| ReviewedAtUtc | timestamp | NULL |
| DismissalReason | varchar(500) | NULL |
| UserFeedbackPositive | boolean | NULL |

Index: `IX_RuleSuggestions_Status` (Status, CreatedAtUtc)
Index: `IX_RuleSuggestions_Type` (Type, Status)

New table: `AiSettings` (or extend existing settings)

| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| OllamaEndpoint | varchar(500) | NOT NULL |
| ModelName | varchar(100) | NOT NULL |
| Temperature | decimal(3,2) | NOT NULL |
| MaxTokens | int | NOT NULL |
| TimeoutSeconds | int | NOT NULL |
| IsEnabled | boolean | NOT NULL |

### UI Components

#### New Pages

- `/ai/settings` - AI configuration page
- `/ai/suggestions` - Suggestion queue management

#### New Components

- `AiStatusBadge.razor` - Shows AI connection status in header
- `SuggestionList.razor` - Displays pending suggestions
- `SuggestionCard.razor` - Individual suggestion with accept/dismiss actions
- `SuggestionDetail.razor` - Detailed view with reasoning and samples
- `AiSettingsForm.razor` - Configure AI endpoint and model
- `AnalysisProgressDialog.razor` - Shows progress during comprehensive analysis
- `FeedbackButtons.razor` - Thumbs up/down component

#### Modified Components

- `RuleList.razor` - Add "Get AI Suggestions" button
- `Layout/MainLayout.razor` - Add AI status indicator

---

## Implementation Plan

### Phase 1: Domain Model ✅

**Objective:** Establish the RuleSuggestion entity and enums

**Tasks:**
- [x] Write unit tests for `RuleSuggestion` factory methods
- [x] Write unit tests for status transitions (Accept, Dismiss)
- [x] Implement `SuggestionType` and `SuggestionStatus` enums
- [x] Implement `RuleSuggestion` entity with factory methods
- [x] Add `IRuleSuggestionRepository` interface

**Commit:**
```bash
git add .
git commit -m "feat(domain): add RuleSuggestion entity for AI suggestions

- SuggestionType enum (NewRule, Optimization, Consolidation, Conflict, Unused)
- SuggestionStatus enum (Pending, Accepted, Dismissed)
- RuleSuggestion entity with factory methods
- IRuleSuggestionRepository interface

Refs: #025"
```

---

### Phase 2: AI Service Infrastructure ✅

**Objective:** Implement Ollama integration

**Tasks:**
- [x] Create `IAiService` interface
- [x] Implement `OllamaAiService` with HTTP client
- [x] Add connection testing and model listing
- [x] Add AI settings to configuration
- [x] Write integration tests (requires running Ollama)
- [x] Handle timeouts and errors gracefully

**Commit:**
```bash
git add .
git commit -m "feat(infra): add Ollama AI service integration

- IAiService interface
- OllamaAiService HTTP client implementation
- AiSettings configuration
- Connection testing and model listing
- Timeout and error handling

Refs: #025"
```

---

### Phase 3: Infrastructure - Repository & Migrations ✅

**Objective:** Implement database persistence for suggestions

**Tasks:**
- [x] Create EF Core configuration for `RuleSuggestion`
- [x] Add migration for `RuleSuggestions` table
- [x] Add migration for AI settings storage (AI settings in appsettings.json instead)
- [x] Implement `RuleSuggestionRepository`
- [x] Write integration tests for repository operations

**Commit:**
```bash
git add .
git commit -m "feat(infra): add RuleSuggestion persistence

- EF Core entity configuration
- Database migration for RuleSuggestions table
- AiSettings storage migration
- RuleSuggestionRepository implementation

Refs: #025"
```

---

### Phase 4: Application Service - Rule Suggestion Service ✅

**Objective:** Implement the suggestion generation logic

**Tasks:**
- [x] Define prompt templates for each suggestion type
- [x] Implement `IRuleSuggestionService` interface
- [x] Implement `RuleSuggestionService` with prompt construction
- [x] Parse AI responses into structured suggestions
- [x] Implement `SuggestNewRulesAsync` method
- [x] Write unit tests with mocked AI service

**Commit:**
```bash
git add .
git commit -m "feat(app): implement RuleSuggestionService for new rule suggestions

- IRuleSuggestionService interface
- Prompt template construction
- AI response parsing to RuleSuggestion entities
- SuggestNewRulesAsync implementation
- Unit tests with mocked IAiService

Refs: #025"
```

---

### Phase 5: Application Service - Optimization & Conflict Detection ✅

**Objective:** Implement optimization analysis and conflict detection

**Tasks:**
- [x] Implement `SuggestOptimizationsAsync` method
- [x] Implement `DetectConflictsAsync` method
- [x] Implement `AnalyzeAllAsync` with progress reporting
- [x] Implement suggestion acceptance (creates/updates rules)
- [x] Write unit tests for all analysis paths

**Commit:**
```bash
git add .
git commit -m "feat(app): add rule optimization and conflict detection

- SuggestOptimizationsAsync for rule improvements
- DetectConflictsAsync for finding rule conflicts
- AnalyzeAllAsync with progress reporting
- AcceptSuggestionAsync creates rules from suggestions
- Unit tests for optimization logic

Refs: #025"
```

---

### Phase 6: API Endpoints ✅

**Objective:** Expose AI features via REST API

**Tasks:**
- [x] Add DTOs to Contracts project
- [x] Implement `AiController` for status and settings
- [x] Implement `SuggestionsController` for suggestion management
- [x] Add endpoint for comprehensive analysis
- [x] Write API integration tests

**Commit:**
```bash
git add .
git commit -m "feat(api): add AI suggestion endpoints

- GET /ai/status for service status
- PUT /ai/settings for configuration
- POST /ai/suggestions/generate for creating suggestions
- GET/POST endpoints for suggestion management
- POST /ai/analyze for comprehensive analysis

Refs: #025"
```

---

### Phase 7: Client - AI Settings UI

**Objective:** Build AI configuration interface

**Tasks:**
- [x] Create `AiSettingsForm` component
- [x] Create AI settings page
- [x] Implement connection testing UI
- [x] Add model selection dropdown
- [x] Create `AiStatusBadge` component for header

**Commit:**
```bash
git add .
git commit -m "feat(client): add AI settings configuration UI

- AiSettingsForm component
- AI settings page at /ai/settings
- Connection test functionality
- Model selection dropdown
- AiStatusBadge in header

Refs: #025"
```

---

### Phase 8: Client - Suggestion Management UI

**Objective:** Build suggestion queue and management interface

**Tasks:**
- [x] Create `SuggestionList` component
- [x] Create `SuggestionCard` component with actions
- [x] Create `SuggestionDetail` dialog
- [x] Implement accept/dismiss flows
- [x] Add feedback buttons
- [x] Add "Get AI Suggestions" button to rules page

**Commit:**
```bash
git add .
git commit -m "feat(client): add AI suggestion management UI

- SuggestionList component with filtering
- SuggestionCard with accept/dismiss actions
- SuggestionDetail dialog with reasoning
- Feedback thumbs up/down
- Integration with rules page

Refs: #025"
```

---

### Phase 9: Client - Analysis Progress & Polish

**Objective:** Implement comprehensive analysis UI and polish

**Tasks:**
- [x] Create `AnalysisProgressDialog` component
- [x] Implement real-time progress updates
- [x] Add analysis summary view
- [x] Polish loading states and error handling
- [x] Add empty states and onboarding hints

**Commit:**
```bash
git add .
git commit -m "feat(client): add comprehensive analysis UI

- AnalysisProgressDialog with real-time updates
- Analysis summary view
- Loading states and error handling
- Empty states and onboarding

Refs: #025"
```

---

### Phase 10: Documentation & Cleanup

**Objective:** Final polish, documentation updates, and cleanup

**Tasks:**
- [x] Update API documentation / OpenAPI specs
- [x] Add XML comments for public APIs
- [x] Document AI setup requirements (Ollama installation)
- [x] Update README with AI feature information
- [x] Remove any TODO comments
- [x] Final code review

**Commit:**
```bash
git add .
git commit -m "docs(ai): add documentation for AI rule suggestions feature

- XML comments for public API
- OpenAPI spec updates
- Ollama setup documentation
- README updates

Refs: #025"
```

---

## Testing Strategy

### Unit Tests

- [x] `RuleSuggestion.CreateNewRuleSuggestion()` validation
- [x] `RuleSuggestion.Accept()` and `Dismiss()` state transitions
- [x] `OllamaAiService` response parsing (mocked HTTP)
- [x] `RuleSuggestionService.SuggestNewRulesAsync()` prompt construction
- [x] `RuleSuggestionService.SuggestOptimizationsAsync()` analysis logic
- [x] `RuleSuggestionService.DetectConflictsAsync()` conflict identification
- [x] AI response JSON parsing edge cases
- [x] Handling of invalid/malformed AI responses

### Integration Tests

- [x] Repository CRUD operations for suggestions
- [ ] AI service connection and model listing (requires Ollama)
- [ ] Full flow: analyze → generate suggestion → accept → verify rule created
- [x] API endpoint authorization
- [x] API endpoint validation

### Manual Testing Checklist

- [ ] Install and configure Ollama with a model
- [ ] Configure AI settings in the application
- [ ] Generate suggestions for uncategorized transactions
- [ ] Review suggestion queue and accept a suggestion
- [ ] Verify rule is created correctly
- [ ] Test optimization suggestions with existing rules
- [ ] Test conflict detection with overlapping rules
- [ ] Test with AI service unavailable (graceful degradation)
- [ ] Test with large transaction dataset
- [ ] Verify no data is sent to external services (network monitoring)

---

## Migration Notes

### Database Migration

```bash
dotnet ef migrations add Feature025_AiRuleSuggestions --project src/BudgetExperiment.Infrastructure --startup-project src/BudgetExperiment.Api
```

### Ollama Setup Requirements

Users must install Ollama separately:

```bash
# Windows (via winget)
winget install Ollama.Ollama

# Or download from https://ollama.ai

# Pull a model
ollama pull llama3.2

# Verify it's running
curl http://localhost:11434/api/tags
```

### Breaking Changes

None - this is a new feature that does not modify existing behavior.

---

## Security Considerations

- **Data Privacy**: All data stays local. Never send transaction data to external services.
- **Network Isolation**: AI service only communicates with localhost by default
- **Input Validation**: Sanitize AI responses before using in database operations
- **Prompt Injection**: AI prompts constructed server-side only; no user input in prompts
- **Resource Limits**: Timeout and token limits prevent resource exhaustion
- **Model Trust**: Users choose which model to run; application doesn't auto-download

---

## Performance Considerations

- AI inference can be slow (10-60+ seconds depending on model and hardware)
- Use async/await throughout to avoid blocking
- Implement progress reporting for long-running analysis
- Consider batching large transaction sets for analysis
- Cache AI service status to avoid repeated checks
- Suggestion generation should be user-initiated, not automatic
- Consider background job for comprehensive analysis

### Hardware Recommendations

For acceptable AI inference performance:
- Minimum: 8GB RAM, CPU-only inference (slow but works)
- Recommended: 16GB+ RAM with GPU acceleration
- Optimal: 32GB+ RAM with dedicated GPU (NVIDIA with CUDA)

---

## Future Enhancements

- Support for additional local AI backends (llama.cpp, LocalAI, text-generation-webui)
- Fine-tuning prompts based on user feedback
- Learning user preferences from accepted/dismissed suggestions
- Scheduled automatic analysis (weekly rule review)
- Export/import suggestion history
- Multi-language transaction description support
- Amount-aware suggestions (different categories for different amounts)
- Seasonal pattern detection (holiday spending, etc.)

---

## References

- [Feature 024: Auto-Categorization Rules Engine](./024-auto-categorization-rules-engine.md) - Rules system this feature enhances
- [Feature 021: Budget Categories & Goals](./021-budget-categories-goals.md) - Category system
- [Ollama Documentation](https://ollama.ai/docs) - Local AI runtime
- [Feature Template](./FEATURE-TEMPLATE.md)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-15 | Initial draft | @copilot |
