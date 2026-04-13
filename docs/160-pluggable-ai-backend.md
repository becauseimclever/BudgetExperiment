# Feature 160: Pluggable AI Backend

> **Status:** In Progress

## Overview

Currently, the budgeting application's AI features (rule suggestions, transaction analysis, categorization recommendations) are hardcoded to use **Ollama** as the local AI inference engine. Developers and users who prefer **llama.cpp** (or might adopt other local inference backends in the future) cannot easily switch backends without code changes.

This feature introduces a **pluggable AI backend architecture** using the Strategy pattern, allowing operators to switch between Ollama and llama.cpp via configuration, with a clear extension point for adding new backends (e.g., OpenAI, LM Studio) without touching Application or Domain layers.

## Problem Statement

### Current State

- `OllamaAiService` is hardcoded as the sole `IAiService` implementation in `BudgetExperiment.Infrastructure`.
- Configuration (`AiSettingsData`, `AiSettingsDto`) hard-codes `OllamaEndpoint` as the only endpoint property.
- Both Ollama and llama.cpp expose **OpenAI-compatible HTTP APIs** (`/v1/chat/completions`, `/v1/models`, health checks), yet we reinvent the wheel for each backend.
- Adding a new backend requires: creating a new service class, duplicating HTTP logic, and changing DI registration.
- No clear configuration option to specify which backend to use.

### Target State

- `IAiService` abstraction remains unchanged (already clean, used throughout Application and API layers).
- A shared `OpenAiCompatibleAiService` base class encapsulates the OpenAI HTTP protocol logic (message formatting, streaming handling, token counting).
- Configuration includes a `BackendType` enum (Ollama, LlamaCpp) and a generic `EndpointUrl` property.
- DI registration is conditional: based on `AiSettings:BackendType`, register the appropriate `IAiService` implementation.
- New backends are added by extending `OpenAiCompatibleAiService` or implementing `IAiService` directly; no Application/Domain changes.
- Existing Ollama users experience zero-breaking-change default behavior; llama.cpp users configure `BackendType=LlamaCpp` + `EndpointUrl`.

---

## User Stories

### Core Functionality

#### US-160-001: Default Behavior Unchanged (Ollama)
**As a** developer using Ollama  
**I want to** use Ollama as the AI backend without any configuration changes  
**So that** my existing Ollama setup continues to work seamlessly

**Acceptance Criteria:**
- [x] Default `AiSettings:BackendType` is `Ollama`
- [x] Default `AiSettings:EndpointUrl` is `http://localhost:11434`
- [x] Existing code and configurations work without modification
- [ ] AI features (suggestions, analysis) work as before

#### US-160-002: llama.cpp Support
**As a** developer using llama.cpp  
**I want to** switch to llama.cpp via configuration  
**So that** I can use my local llama.cpp instance for AI features

**Acceptance Criteria:**
- [x] `AiSettings:BackendType = LlamaCpp` is a valid configuration option
- [x] `AiSettings:EndpointUrl` defaults to `http://localhost:8080` when `BackendType=LlamaCpp`
- [ ] Setting `BackendType=LlamaCpp` and pointing to a valid llama.cpp server results in working AI features
- [ ] Health check (`/ai/status`) and model listing work identically between backends
- [x] Token counting is compatible with llama.cpp's response format

#### US-160-003: Backend Transparency in API
**As a** client consuming the AI API  
**I want to** know which backend is active  
**So that** I can understand service behavior and troubleshoot issues

**Acceptance Criteria:**
- [x] `GET /api/v1/ai/status` returns the active backend type (e.g., `"Ollama"` or `"LlamaCpp"`)
- [x] `AiSettingsDto` includes a `BackendType` field
- [x] No breaking changes to existing API contracts

#### US-160-004: Extensible Architecture
**As a** future developer  
**I want to** add a new AI backend (e.g., OpenAI, LM Studio) without modifying Application or Domain  
**So that** the codebase scales cleanly and the abstraction holds

**Acceptance Criteria:**
- [ ] Adding a new backend requires only a new enum value, a new service class (inheriting `OpenAiCompatibleAiService` or implementing `IAiService`), and updating DI registration
- [ ] No changes to `IAiService` interface, application services, or domain logic
- [ ] Documentation clearly describes the extension point

---

## Technical Design

### Architecture Changes

**Strategy Pattern Implementation:**
- **Abstraction (unchanged):** `IAiService` remains the public interface in the Application layer
- **Concrete strategies:**
  - `OllamaAiService` — Ollama backend implementation (refactored to leverage shared base class)
  - `LlamaCppAiService` — llama.cpp backend implementation
  - `OpenAiCompatibleAiService` (base class) — Shared HTTP logic for OpenAI-compatible APIs
- **Configuration:**
  - `AiBackendType` enum in `BudgetExperiment.Shared` (or Domain): `Ollama`, `LlamaCpp`
  - `AiSettingsData` gains `BackendType` and refactored `EndpointUrl` (generic, not Ollama-specific)
  - `AiDefaults` updated with defaults per backend type
- **DI Registration (Infrastructure.DependencyInjection):**
  - Factory logic in `AddInfrastructure()` reads `AiSettings:BackendType` from configuration
  - Registers appropriate `IAiService` implementation conditionally
  - HttpClient config is generic (not Ollama-specific)

### Domain Model

**New Enum: `AiBackendType` (BudgetExperiment.Shared)**

```csharp
namespace BudgetExperiment.Shared;

/// <summary>
/// Enumeration of supported AI backend types.
/// </summary>
public enum AiBackendType
{
    /// <summary>
    /// Ollama local AI inference engine.
    /// </summary>
    Ollama = 0,

    /// <summary>
    /// llama.cpp local AI inference engine.
    /// </summary>
    LlamaCpp = 1,
}
```

**Updated: `AiSettingsData` (BudgetExperiment.Application.Settings)**

```csharp
public sealed record AiSettingsData(
    string EndpointUrl,           // Now generic (was OllamaEndpoint)
    string ModelName,
    decimal Temperature,
    int MaxTokens,
    int TimeoutSeconds,
    bool IsEnabled,
    AiBackendType BackendType = AiBackendType.Ollama);  // New property
```

**Updated: `AiDefaults` (BudgetExperiment.Domain.Settings)**

```csharp
public static class AiDefaults
{
    // Default endpoint URLs per backend
    public const string DefaultOllamaUrl = "http://localhost:11434";
    public const string DefaultLlamaCppUrl = "http://localhost:8080";

    // Default backend type
    public const AiBackendType DefaultBackendType = AiBackendType.Ollama;
}
```

### API Endpoints

| Method | Endpoint | Changes |
|--------|----------|---------|
| GET | `/api/v1/ai/status` | Response includes `BackendType` field |
| GET | `/api/v1/ai/settings` | Response includes `BackendType` field |
| PUT | `/api/v1/ai/settings` | Request/response include `BackendType` field |
| GET | `/api/v1/ai/models` | No changes (works identically for all backends) |
| POST | `/api/v1/ai/analyze` | No changes (delegates to pluggable `IAiService`) |

### Database Changes

**None** — All configuration is stored in the `AppSettings` entity's `AiSettings` JSON column (already exists). A new `BackendType` property is serialized/deserialized transparently.

### Shared HTTP Logic: `OpenAiCompatibleAiService` Base Class

```csharp
/// <summary>
/// Abstract base class for AI services using OpenAI-compatible HTTP APIs.
/// Ollama and llama.cpp both expose /v1/chat/completions.
/// </summary>
public abstract class OpenAiCompatibleAiService : IAiService
{
    // Shared JSON serialization options (snake_case for HTTP APIs)
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    protected readonly HttpClient HttpClient;
    protected readonly IAppSettingsService SettingsService;
    protected readonly ILogger Logger;

    // Abstract methods for backend-specific details
    protected abstract string HealthCheckEndpoint { get; }
    protected abstract Task<IReadOnlyList<AiModelInfo>> ParseModelsResponseAsync(
        string response, CancellationToken cancellationToken);

    // Concrete implementations for shared OpenAI-compatible HTTP protocol
    public virtual async Task<AiServiceStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var settings = await SettingsService.GetAiSettingsAsync(cancellationToken);
        if (!settings.IsEnabled)
            return new AiServiceStatus(false, null, "AI features are disabled.");

        try
        {
            var baseUri = settings.EndpointUrl.TrimEnd('/');
            var response = await HttpClient.GetAsync($"{baseUri}/{HealthCheckEndpoint}", cancellationToken);
            return response.IsSuccessStatusCode
                ? new AiServiceStatus(true, settings.ModelName, null)
                : new AiServiceStatus(false, null, $"Backend returned status {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            Logger.LogWarning(ex, "Failed to connect to AI backend at {Endpoint}", settings.EndpointUrl);
            return new AiServiceStatus(false, null, $"Failed to connect: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            Logger.LogWarning("Connection to AI backend timed out");
            return new AiServiceStatus(false, null, "Connection timed out.");
        }
    }

    public virtual async Task<IReadOnlyList<AiModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await SettingsService.GetAiSettingsAsync(cancellationToken);
        if (!settings.IsEnabled)
            return Array.Empty<AiModelInfo>();

        try
        {
            var baseUri = settings.EndpointUrl.TrimEnd('/');
            var response = await HttpClient.GetAsync($"{baseUri}/v1/models", cancellationToken);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return await ParseModelsResponseAsync(content, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to list models from AI backend");
            return Array.Empty<AiModelInfo>();
        }
    }

    public virtual async Task<AiResponse> CompleteAsync(AiPrompt prompt, CancellationToken cancellationToken = default)
    {
        var settings = await SettingsService.GetAiSettingsAsync(cancellationToken);
        var stopwatch = Stopwatch.StartNew();

        if (!settings.IsEnabled)
            return new AiResponse(false, string.Empty, "AI features are disabled.", 0, stopwatch.Elapsed);

        try
        {
            var request = new OpenAiChatRequest
            {
                Model = settings.ModelName,
                Messages = new[]
                {
                    new OpenAiChatMessage { Role = "system", Content = prompt.SystemPrompt },
                    new OpenAiChatMessage { Role = "user", Content = prompt.UserPrompt },
                },
                Temperature = (float)prompt.Temperature,
                MaxTokens = prompt.MaxTokens,
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));

            var baseUri = settings.EndpointUrl.TrimEnd('/');
            var response = await HttpClient.PostAsJsonAsync(
                $"{baseUri}/v1/chat/completions", request, JsonOptions, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
                Logger.LogWarning("AI backend returned {Status}: {Error}", response.StatusCode, errorContent);
                return new AiResponse(false, string.Empty, 
                    $"Backend returned status {response.StatusCode}: {errorContent}", 0, stopwatch.Elapsed);
            }

            var chatResponse = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(
                JsonOptions, cts.Token);

            stopwatch.Stop();

            if (chatResponse == null || chatResponse.Choices?.FirstOrDefault() is not { } choice)
                return new AiResponse(false, string.Empty, "Failed to parse backend response.", 0, stopwatch.Elapsed);

            var tokensUsed = (chatResponse.Usage?.PromptTokens ?? 0) + (chatResponse.Usage?.CompletionTokens ?? 0);
            return new AiResponse(true, choice.Message?.Content ?? string.Empty, null, tokensUsed, stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("AI request timed out after {Timeout} seconds", settings.TimeoutSeconds);
            return new AiResponse(false, string.Empty, 
                $"AI request timed out after {settings.TimeoutSeconds} seconds.", 0, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to communicate with AI backend");
            return new AiResponse(false, string.Empty, $"Failed to communicate: {ex.Message}", 0, stopwatch.Elapsed);
        }
    }

    // Shared request/response DTOs for OpenAI-compatible API
    protected sealed class OpenAiChatRequest
    {
        public string Model { get; set; } = string.Empty;
        public OpenAiChatMessage[]? Messages { get; set; }
        public float Temperature { get; set; } = 0.7f;
        public int MaxTokens { get; set; } = 2000;
    }

    protected sealed class OpenAiChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    protected sealed class OpenAiChatResponse
    {
        public string? Model { get; set; }
        public OpenAiChoice[]? Choices { get; set; }
        public OpenAiUsage? Usage { get; set; }
    }

    protected sealed class OpenAiChoice
    {
        public int Index { get; set; }
        public OpenAiChatMessage? Message { get; set; }
        public string? FinishReason { get; set; }
    }

    protected sealed class OpenAiUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
```

### Concrete Implementations

**OllamaAiService (Refactored)**

```csharp
/// <summary>
/// AI service implementation for Ollama.
/// </summary>
public sealed class OllamaAiService : OpenAiCompatibleAiService
{
    public OllamaAiService(HttpClient httpClient, IAppSettingsService settingsService, ILogger<OllamaAiService> logger)
        : base(httpClient, settingsService, logger)
    {
    }

    protected override string HealthCheckEndpoint => "api/version";

    protected override async Task<IReadOnlyList<AiModelInfo>> ParseModelsResponseAsync(
        string response, CancellationToken cancellationToken)
    {
        var tagsResponse = JsonSerializer.Deserialize<OllamaTagsResponse>(response, JsonOptions);
        return tagsResponse?.Models?
            .Select(m => new AiModelInfo(m.Name ?? string.Empty, m.ModifiedAt, m.Size))
            .ToList() ?? Array.Empty<AiModelInfo>();
    }

    private sealed class OllamaTagsResponse
    {
        public List<OllamaModelInfo>? Models { get; set; }
    }

    private sealed class OllamaModelInfo
    {
        public string? Name { get; set; }
        public DateTime ModifiedAt { get; set; }
        public long Size { get; set; }
    }
}
```

**LlamaCppAiService (New)**

```csharp
/// <summary>
/// AI service implementation for llama.cpp.
/// </summary>
public sealed class LlamaCppAiService : OpenAiCompatibleAiService
{
    public LlamaCppAiService(HttpClient httpClient, IAppSettingsService settingsService, ILogger<LlamaCppAiService> logger)
        : base(httpClient, settingsService, logger)
    {
    }

    protected override string HealthCheckEndpoint => "health";

    protected override async Task<IReadOnlyList<AiModelInfo>> ParseModelsResponseAsync(
        string response, CancellationToken cancellationToken)
    {
        var modelsResponse = JsonSerializer.Deserialize<LlamaCppModelsResponse>(response, JsonOptions);
        return modelsResponse?.Data?
            .Select(m => new AiModelInfo(
                m.Id ?? string.Empty,
                DateTime.UtcNow, // llama.cpp doesn't return modification time
                0)) // llama.cpp doesn't return size
            .ToList() ?? Array.Empty<AiModelInfo>();
    }

    private sealed class LlamaCppModelsResponse
    {
        public List<LlamaCppModel>? Data { get; set; }
    }

    private sealed class LlamaCppModel
    {
        public string? Id { get; set; }
    }
}
```

---

## Implementation Plan

### Phase 1: Introduce Shared Enum & Update DTOs

**Objective:** Add `AiBackendType` enum to Shared, update configuration DTOs and application settings

**Tasks:**
- [x] Create `AiBackendType` enum in `BudgetExperiment.Shared`
- [x] Update `AiSettingsData` record to include `BackendType` and rename `OllamaEndpoint` → `EndpointUrl`
- [x] Update `AiSettingsDto` (Contracts) with `BackendType` field
- [x] Update `AiStatusDto` with `BackendType` field
- [x] Update `AiDefaults` with new defaults and backend-specific URLs
- [x] Update `AiController` to map `BackendType` in GET/PUT settings endpoints
- [x] Write unit tests for DTO serialization/deserialization

**Commit:**
```bash
git add .
git commit -m "feat(shared): add AiBackendType enum and update AI settings DTOs

- Add AiBackendType enum (Ollama, LlamaCpp) to BudgetExperiment.Shared
- Refactor AiSettingsData: OllamaEndpoint → EndpointUrl, add BackendType
- Extend AiSettingsDto and AiStatusDto with BackendType field
- Add backend-specific endpoint defaults in AiDefaults
- Update AiController status and settings endpoints
- Unit tests for DTO mapping

Refs: #160"
```

### Phase 2: Create OpenAiCompatibleAiService Base Class

**Objective:** Extract shared HTTP protocol logic from OllamaAiService into a reusable base class

**Tasks:**
- [x] Create abstract `OpenAiCompatibleAiService` in `Infrastructure.ExternalServices.AI`
- [x] Move JSON options, timeout handling, and core HTTP logic to base class
- [x] Define abstract methods: `HealthCheckEndpoint`, `ParseModelsResponseAsync`
- [x] Move shared request/response DTOs (OpenAiChatRequest, OpenAiChatResponse, etc.) into base class
- [x] Write unit tests for base class core logic (mocking HTTP responses)
- [x] Ensure interface compatibility with `IAiService`

**Commit:**
```bash
git add .
git commit -m "refactor(infra): extract OpenAiCompatibleAiService base class

- Create abstract base class for OpenAI-compatible AI backends
- Move shared HTTP protocol logic, JSON options, timeout handling
- Define abstract methods for backend-specific health check and model parsing
- Move OpenAI request/response DTOs to base class
- Unit tests for shared protocol handling with mocks

Refs: #160"
```

### Phase 3: Refactor OllamaAiService & Add LlamaCppAiService

**Objective:** Refactor Ollama to use base class and implement llama.cpp backend

**Tasks:**
- [x] Refactor `OllamaAiService` to extend `OpenAiCompatibleAiService`
- [x] Move Ollama-specific model parsing to `ParseModelsResponseAsync` override
- [x] Keep Ollama-specific DTOs (OllamaTagsResponse) internal to OllamaAiService
- [x] Implement `LlamaCppAiService` extending `OpenAiCompatibleAiService`
- [x] Add llama.cpp-specific health check (`/health`) and model parsing logic
- [x] Write unit tests for both services with mock HTTP handlers
- [x] Verify backward compatibility: Ollama tests still pass without changes

**Commit:**
```bash
git add .
git commit -m "feat(infra): implement pluggable AI backends (Ollama, llama.cpp)

- Refactor OllamaAiService to extend OpenAiCompatibleAiService
- Implement new LlamaCppAiService extending same base
- Backend-specific health check endpoints and model parsing
- Unit tests for both backends using mock HTTP handlers
- Maintain backward compatibility with existing Ollama tests

Refs: #160"
```

### Phase 4: Update DI Registration & Configuration

**Objective:** Wire conditional registration based on `AiSettings:BackendType` configuration

**Tasks:**
- [x] Update `Infrastructure.DependencyInjection.AddInfrastructure()` to read `AiSettings:BackendType` from config
- [x] Implement factory/conditional registration logic:
  - If `BackendType == Ollama` → register `OllamaAiService`
  - If `BackendType == LlamaCpp` → register `LlamaCppAiService`
  - Default to Ollama if not specified
- [x] Update HttpClient registration to use generic endpoint URL (not Ollama-specific)
- [x] Write integration tests for DI registration with different configurations
- [x] Test default behavior (Ollama) and explicit llama.cpp configuration

**Commit:**
```bash
git add .
git commit -m "feat(infra): conditional AI backend registration via DI

- Update AddInfrastructure() with BackendType-based DI registration
- Default to Ollama if BackendType not specified
- Wire HttpClient with generic endpoint URL
- Integration tests for DI with different backend configurations
- Verify default Ollama behavior unchanged

Refs: #160"
```

### Phase 5: Update AppSettingsService & Migration Notes

**Objective:** Ensure AppSettingsService correctly reads/writes the new `BackendType` property

**Migration note:** `20260412102000_AddAiBackendTypeToAppSettings` adds `AiBackendType` with database default `0` (`Ollama`), so existing persisted settings rows backfill safely without a separate data migration.

**Tasks:**
- [x] Review `AppSettingsService.GetAiSettingsAsync()` and `UpdateAiSettingsAsync()` to ensure `BackendType` is properly serialized/deserialized
- [ ] If using JSON column in database (likely), verify JSON mapping is correct
- [x] Add migration notes: existing `AiSettings` records without `BackendType` default to `Ollama`
- [ ] Create a simple db migration or schema update if needed (likely none, since JSON columns are schema-agnostic)
- [x] Write a test that reads/writes settings with `BackendType` to ensure persistence

**Commit:**
```bash
git add .
git commit -m "feat(app): ensure AiSettingsService handles BackendType persistence

- Verify JSON serialization of BackendType in AppSettingsService
- Add migration note: defaults to Ollama if not specified
- Test persistence: read/write settings with BackendType
- Update AppSettings integration tests

Refs: #160"
```

### Phase 6: API & UI Integration

**Objective:** Update API responses and UI to surface the active backend and allow configuration

**Tasks:**
- [x] Update `AiController.GetStatusAsync()` to include `BackendType` in response
- [x] Verify `AiController.GetSettingsAsync()` and `UpdateSettingsAsync()` handle `BackendType` field
- [ ] Update OpenAPI documentation (auto-generated or XML comments)
- [x] Client-side (Blazor): update AI Settings panel to include `BackendType` dropdown and `EndpointUrl` field
- [x] Test GET/PUT settings endpoints with both Ollama and llama.cpp configurations
- [ ] Verify OpenAPI spec accurately reflects new fields

**Client behavior note:** The settings form now exposes a backend selector plus a generic `EndpointUrl` field. Switching backends updates the endpoint to the new backend default only when the user is still using the previous default; custom endpoints are preserved.

**Commit:**
```bash
git add .
git commit -m "feat(api,client): surface AI backend selection and configuration

- Add BackendType to AiStatusDto and update AiController endpoints
- Update AI Settings Razor component with BackendType dropdown
- Generic EndpointUrl field (replaces Ollama-specific field)
- Update OpenAPI documentation for AI endpoints
- Manual test: verify settings panel works for both backends

Refs: #160"
```

### Phase 7: Documentation & Cleanup

**Objective:** Document the new architecture, update README, and ensure examples are clear

**Tasks:**
- [ ] Update README or `docs/architecture.md` with new pluggable backend approach
- [ ] Add configuration examples for both Ollama and llama.cpp (appsettings.json, user-secrets)
- [ ] Update `CONTRIBUTING.md` with instructions for adding new AI backends
- [ ] Add inline XML comments to `OpenAiCompatibleAiService` explaining extension points
- [ ] Update feature doc status to `Complete`
- [x] Review all code for stray Ollama-specific comments or naming in the touched client surface
- [ ] Final code review and cleanup

**Commit:**
```bash
git add .
git commit -m "docs: document pluggable AI backend architecture and examples

- Update README with backend switching instructions
- Configuration examples for Ollama and llama.cpp
- Developer guide: adding new AI backends (extend base class + update DI)
- XML comments for extension points
- Cleanup: remove stale Ollama-specific references
- Feature doc 160 complete

Refs: #160"
```

---

## Testing Strategy

### Unit Tests

- **`OpenAiCompatibleAiService` base class:**
  - [ ] Health check success/failure (mocked HTTP responses)
  - [x] Model listing parsing (mock backend response)
  - [x] Chat completion with various prompts and parameters
  - [x] Timeout handling
  - [ ] JSON serialization/deserialization of requests/responses
  - [ ] Token counting

- **`OllamaAiService` (refactored):**
  - [x] Ollama-specific model parsing (OllamaTagsResponse structure)
  - [x] Health check endpoint (`/api/version`)
  - [x] Backward compatibility: existing tests pass unchanged

- **`LlamaCppAiService` (new):**
  - [x] llama.cpp-specific model parsing (models array structure)
  - [x] Health check endpoint (`/health`)
  - [x] Chat completion with llama.cpp response format
  - [ ] Error handling (same as Ollama)

- **DI Registration:**
  - [x] Ollama backend registered by default
  - [x] Ollama backend registered when explicitly configured
  - [x] LlamaCpp backend registered when configured
  - [ ] Correct HttpClient endpoint URL passed to each service

### Integration Tests

- **AppSettingsService persistence:**
  - [x] Create settings with `BackendType=Ollama`
  - [x] Update settings to `BackendType=LlamaCpp`
  - [x] Read back and verify persistence
  - [x] Default behavior when BackendType is not specified

- **AiController endpoints (with mocked backends):**
  - [x] `GET /api/v1/ai/status` returns correct `BackendType` and endpoint
  - [x] `GET /api/v1/ai/settings` includes `BackendType`
  - [x] `PUT /api/v1/ai/settings` with new `BackendType` value persists and is reflected in next GET
  - [ ] `GET /api/v1/ai/models` works with mocked Ollama and llama.cpp responses

### Manual Testing Checklist

- [ ] **Ollama (default):** Start Ollama, run app with no backend config changes, verify AI features work
- [ ] **Ollama (explicit):** Set `AiSettings:BackendType=Ollama` in appsettings.json, verify same behavior
- [ ] **llama.cpp:** Start llama.cpp server at `http://localhost:8080`, set `AiSettings:BackendType=LlamaCpp`, verify AI features work
- [ ] **Dynamic switching:** Change backend via PUT `/api/v1/ai/settings`, verify new backend is used immediately
- [ ] **Status endpoint:** Call `GET /api/v1/ai/status` and verify `BackendType` field matches configured value
- [ ] **Model listing:** Call `GET /api/v1/ai/models` with both Ollama and llama.cpp, verify correct model list returned
- [ ] **AI analysis:** Run suggestion analysis with both backends, verify results are consistent

---

## Migration Notes

### Existing Users (Ollama)

**Zero-breaking change.** Existing deployments and configurations continue to work:
- If `AiSettings:BackendType` is not specified, it defaults to `Ollama`.
- `AiSettings:OllamaEndpoint` is transparently renamed to `AiSettings:EndpointUrl` in the internal record, but DTO mapping handles the distinction.
- No database migration required; JSON column schema is backward compatible.

### Upgrading to Dual-Backend Support

1. **Apply code changes** (all 7 phases).
2. **No explicit migration needed** — existing `AiSettings` JSON records automatically get `BackendType: "Ollama"` when deserialized (C# defaults).
3. **If switching to llama.cpp:**
   - Set `AiSettings:BackendType=LlamaCpp` in `appsettings.json` or user-secrets.
   - Set `AiSettings:EndpointUrl=http://localhost:8080` (or appropriate llama.cpp server URL).
   - Restart the application.

### Configuration Examples

**appsettings.json (Ollama, default):**
```json
{
  "AiSettings": {
    "IsEnabled": true,
    "BackendType": "Ollama",
    "EndpointUrl": "http://localhost:11434",
    "ModelName": "llama3.2",
    "Temperature": 0.3,
    "MaxTokens": 2000,
    "TimeoutSeconds": 120
  }
}
```

**appsettings.json (llama.cpp):**
```json
{
  "AiSettings": {
    "IsEnabled": true,
    "BackendType": "LlamaCpp",
    "EndpointUrl": "http://localhost:8080",
    "ModelName": "Meta-Llama-3.1-8B-Instruct",
    "Temperature": 0.3,
    "MaxTokens": 2000,
    "TimeoutSeconds": 120
  }
}
```

**User-secrets (development override for llama.cpp):**
```bash
dotnet user-secrets set "AiSettings:BackendType" "LlamaCpp" --project src/BudgetExperiment.Api
dotnet user-secrets set "AiSettings:EndpointUrl" "http://localhost:8080" --project src/BudgetExperiment.Api
```

---

## Security Considerations

- **Endpoint URL validation:** Both `OllamaAiService` and `LlamaCppAiService` use `TrimEnd('/')` to normalize URLs, preventing injection via malformed endpoints. Consider adding explicit URI validation (e.g., `Uri.IsWellFormedUriString`) if endpoints can be user-configured in a web UI.
- **HTTP vs. HTTPS:** Local backends (Ollama, llama.cpp) typically run on HTTP. Production deployments may want to enforce HTTPS or mTLS; this is a deployment concern (reverse proxy, firewall) rather than code-level.
- **API key/auth:** Ollama and llama.cpp servers are typically not exposed to the internet and don't use API keys. If a backend requires authentication (e.g., future OpenAI integration), extend `IAiService` or add auth headers to the HttpClient factory.
- **Prompt injection:** No changes. The existing `AiPrompt` structure (SystemPrompt + UserPrompt) is backend-agnostic; attack surface remains unchanged.

---

## Performance Considerations

- **No regression:** Refactoring to a base class reduces code duplication (~150 lines of shared HTTP logic) but introduces one extra virtual method call per operation. Negligible impact (<1ms per request).
- **Backend selection (DI):** Conditional registration happens once at startup; zero runtime overhead.
- **Model listing cache:** If model lists are fetched frequently, consider caching in `OpenAiCompatibleAiService.GetAvailableModelsAsync()`. Out of scope for this feature but noted for optimization phase.
- **Timeout behavior:** Timeout logic is unchanged; both backends use the same `TimeoutSeconds` setting.

---

## Future Enhancements

- **OpenAI API integration:** Add `OpenAI` enum value, implement `OpenAiAiService(ApiKey, Model)`, update DI.
- **LM Studio support:** Similar to llama.cpp; add enum value and service class.
- **Model selection UI:** Extend `AiController` to expose `/models` endpoint and allow UI to select model at runtime.
- **Streaming responses:** Refactor `CompleteAsync` to support streaming (line-delimited JSON) for longer responses.
- **Model download management:** Add endpoints to download/delete models (Ollama has `/api/pull`; llama.cpp model management is simpler).
- **Backend health monitoring:** Add metrics/health check polling in the background.

---

## References

- **Ollama API Docs:** https://github.com/ollama/ollama/blob/main/docs/api.md
- **llama.cpp Server:** https://github.com/ggerganov/llama.cpp/blob/master/examples/server/README.md
- **OpenAI Chat Completions API:** https://platform.openai.com/docs/api-reference/chat/create
- **Strategy Pattern:** GoF Design Patterns (Gamma et al.)
- **Related Features:**
  - Feature 151: Extract TransactionFactory
  - Feature 152: God Application Services Split Plan
  - Feature 153: God Controllers Split Strategy

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-04-XX | Feature doc 160 created: Pluggable AI Backend design | Alfred (Lead) |

