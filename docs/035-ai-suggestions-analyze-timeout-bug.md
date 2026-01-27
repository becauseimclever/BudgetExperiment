# Feature 035: AI Suggestions Analyze Timeout Bug
> **Status:** ✅ Complete

> **Status:** ✅ Complete  
> **Type:** Bug Fix  
> **Severity:** High  
> **Started:** 2026-01-23  
> **Completed:** 2026-01-23

## Overview

Fix the "Run AI Analysis" button on the Smart Insights page (`/ai/suggestions`) which fails after approximately 2 minutes with a connection error when calling the Ollama AI service.

## Problem Statement

### Current Behavior (Bug)

When a user navigates to the Smart Insights page (`/ai/suggestions`) and clicks "Run AI Analysis":

1. **Client sends POST request** to `/api/v1/ai/analyze`
2. **Server calls Ollama** via `_suggestionService.AnalyzeAllAsync()` which internally invokes `OllamaAiService.CompleteAsync()`
3. **Request times out** after approximately 120 seconds (configured `AiTimeoutSeconds`)
4. **Connection is lost** - browser receives `net::ERR_CONNECTION_REFUSED`
5. **Error displayed in UI**: "AnalysisError" dialog appears with message "Please check your AI settings and try again."

### Console/Network Evidence

```
POST http://localhost:5099/api/v1/ai/analyze [failed - net::ERR_CONNECTION_REFUSED]
```

The request shows as pending for ~2 minutes before failing with a connection refused error, suggesting the server-side request to Ollama times out and potentially causes issues with the API response.

### Server Terminal Evidence

The server shows the request being received and the Ollama call being initiated, but after the timeout period, the request fails to complete properly.

### Root Cause Analysis

The issue appears to be a combination of:

1. **Ollama Response Time**: The AI model takes longer than the configured 120-second timeout to generate a response, especially when analyzing many uncategorized transactions.

2. **Timeout Configuration**: The `OllamaAiService` creates a linked cancellation token with `CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds))`, but the timeout may not be sufficient for complex analyses.

3. **Error Propagation**: When the timeout occurs, the `OperationCanceledException` is caught and returns an `AiResponse` with `Success = false`, but the error may not be properly propagated to the client as a meaningful HTTP response.

4. **Connection Loss**: The `net::ERR_CONNECTION_REFUSED` suggests the server connection may be dropping during the long-running operation.

### Expected Behavior

1. User clicks "Run AI Analysis" on Smart Insights page
2. POST request to `/api/v1/ai/analyze` initiates analysis
3. If Ollama takes longer than timeout:
   - Server returns a proper error response (e.g., 504 Gateway Timeout or 503 with meaningful message)
   - UI shows a user-friendly timeout error with option to retry or increase timeout in settings
4. On success:
   - Response contains analysis results with suggestion counts
   - UI displays the analysis summary and generated suggestions

---

## Technical Investigation

### Relevant Code Paths

| File | Purpose |
|------|---------|
| [AiController.cs](../src/BudgetExperiment.Api/Controllers/AiController.cs#L127-L162) | `/api/v1/ai/analyze` endpoint |
| [OllamaAiService.cs](../src/BudgetExperiment.Infrastructure/ExternalServices/AI/OllamaAiService.cs#L110-L220) | Ollama HTTP client with timeout handling |
| [RuleSuggestionService.cs](../src/BudgetExperiment.Application/Categorization/RuleSuggestionService.cs) | `AnalyzeAllAsync()` orchestration |
| [AiSuggestions.razor](../src/BudgetExperiment.Client/Pages/AiSuggestions.razor) | Client page with analysis dialog |

### Current Timeout Configuration

```csharp
// OllamaAiService.cs - Line 151-152
using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
cts.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds)); // Default: 120 seconds
```

### Potential Issues

1. **No server-side timeout for long operations**: The Kestrel server may have its own connection timeout that's shorter than the Ollama timeout.

2. **Missing error response on timeout**: When `OperationCanceledException` is caught, the service returns an `AiResponse` with error details, but this may not translate to a proper HTTP error response.

3. **Client connection may be dropped**: The browser or proxy may drop the connection before the server responds.

---

## User Stories

### US-035-001: Handle AI Analysis Timeout Gracefully
**As a** user  
**I want to** see a clear error message when AI analysis times out  
**So that** I know to either wait longer or adjust my timeout settings

**Acceptance Criteria:**
- [ ] Timeout returns proper HTTP 504 or 503 status code
- [ ] Error message explains timeout occurred
- [ ] UI suggests increasing timeout in settings
- [ ] Retry button works correctly

### US-035-002: Keep Connection Alive During Long Analysis
**As a** user  
**I want to** the analysis to complete even if it takes several minutes  
**So that** I can analyze large transaction sets without connection errors

**Acceptance Criteria:**
- [ ] Server maintains connection during long-running Ollama requests
- [ ] Progress updates are sent to keep connection alive (if feasible)
- [ ] Configurable timeout in Settings page works correctly

---

## Technical Design

### Potential Solutions

#### Option 1: Increase Default Timeout and Add Keep-Alive
- Increase default `AiTimeoutSeconds` from 120 to 300 seconds
- Configure Kestrel request timeout to match
- Add background job pattern for very long operations

#### Option 2: Add Streaming/SSE for Progress Updates
- Implement Server-Sent Events for analysis progress
- Send periodic progress updates to keep connection alive
- More complex but better UX

#### Option 3: Async Job Pattern
- POST to analyze returns immediately with job ID
- Client polls for job status
- Better for very long operations, more complex implementation

### Recommended Approach

Start with **Option 1** as it's the simplest fix:

1. Ensure proper error propagation from `OllamaAiService` to `AiController`
2. Return appropriate HTTP status code on timeout (504 or 503)
3. Configure Kestrel/server timeouts to allow longer requests
4. Update client error handling to show timeout-specific message

---

## Implementation Plan

### Phase 1: Diagnosis & Root Cause Confirmation

**Objective:** Confirm exact failure point

**Tasks:**
- [ ] Add detailed logging in `AiController.AnalyzeAsync()` 
- [ ] Add detailed logging in `OllamaAiService.CompleteAsync()`
- [ ] Verify Kestrel request timeout configuration
- [ ] Test with a smaller dataset to confirm Ollama connectivity
- [ ] Capture exact error from server logs

**Commit:**
```bash
git add .
git commit -m "chore(ai): add diagnostic logging for analyze timeout

- Add logging to AiController.AnalyzeAsync
- Add logging to OllamaAiService.CompleteAsync
- Capture timing and error details

Refs: #035"
```

---

### Phase 2: Fix Timeout Handling

**Objective:** Ensure timeout errors are properly returned to client

**Tasks:**
- [ ] Update `AiController.AnalyzeAsync()` to catch and handle timeout exceptions
- [ ] Return proper HTTP 504 status for timeout scenarios
- [ ] Include meaningful error message in response body
- [ ] Write unit tests for timeout scenarios

**Commit:**
```bash
git add .
git commit -m "fix(ai): return proper HTTP status on analyze timeout

- Catch OperationCanceledException in AiController
- Return 504 Gateway Timeout with descriptive message
- Add unit tests for timeout handling

Refs: #035"
```

---

### Phase 3: Configure Server Timeouts

**Objective:** Ensure server allows long-running AI requests

**Tasks:**
- [ ] Review and configure Kestrel request timeout
- [ ] Configure HTTP client timeout for Ollama calls
- [ ] Document timeout configuration in copilot-instructions.md
- [ ] Consider adding configurable endpoint-specific timeouts

**Commit:**
```bash
git add .
git commit -m "fix(api): configure server timeouts for AI endpoints

- Set Kestrel request timeout for long AI operations
- Ensure HTTP client timeout matches AI settings
- Document configuration

Refs: #035"
```

---

### Phase 4: Update Client Error Handling

**Objective:** Improve user experience on timeout

**Tasks:**
- [ ] Update `AiSuggestions.razor` to detect timeout errors
- [ ] Show specific timeout message with guidance
- [ ] Link to Settings page to adjust timeout
- [ ] Test error scenarios end-to-end

**Commit:**
```bash
git add .
git commit -m "fix(client): improve AI analysis timeout error handling

- Detect 504 timeout responses
- Show user-friendly timeout message
- Add link to Settings for timeout adjustment

Refs: #035"
```

---

### Phase 5: Documentation & Cleanup

**Objective:** Final polish and documentation

**Tasks:**
- [ ] Update AI documentation
- [ ] Remove diagnostic logging (or move to Debug level)
- [ ] Add troubleshooting guide for AI timeout issues
- [ ] Final code review

**Commit:**
```bash
git add .
git commit -m "docs(ai): add timeout troubleshooting documentation

- Document AI timeout configuration
- Add troubleshooting steps
- Update feature doc status to complete

Refs: #035"
```

---

## Testing Strategy

### Unit Tests

- [ ] `AiController.AnalyzeAsync` returns 504 on `OperationCanceledException`
- [ ] `OllamaAiService.CompleteAsync` returns error response on timeout
- [ ] Timeout error message includes useful details

### Integration Tests

- [ ] End-to-end analyze request with mocked slow Ollama response
- [ ] Verify HTTP status codes for various timeout scenarios

### Manual Testing Checklist

- [ ] Navigate to `/ai/suggestions`
- [ ] Click "Run AI Analysis" with Ollama running
- [ ] Wait for analysis to complete or timeout
- [ ] Verify error message is displayed correctly on timeout
- [ ] Verify retry button works
- [ ] Test with different timeout settings in Settings page

---

## Security Considerations

None - this is a bug fix that doesn't change security posture.

---

## Performance Considerations

- Consider implementing request batching if analyzing very large transaction sets
- Consider background job pattern for analyses exceeding reasonable timeout limits

---

## References

- [Feature 032: AI-Powered Category Suggestions](./032-ai-category-suggestions.md)
- [Feature 034: Category Suggestions Analyze 405 Fix](./034-category-suggestions-analyze-405-fix.md)
- [OllamaAiService Implementation](../src/BudgetExperiment.Infrastructure/ExternalServices/AI/OllamaAiService.cs)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-23 | Initial bug report created | @copilot |
| 2026-01-23 | Fixed: Added timeout handling, request timeout middleware, client error handling | @copilot |
