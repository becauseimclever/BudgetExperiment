# Feature 110: Debug Log Export for Issue Reporting
> **Status:** In Progress  
> **Depends On:** [Feature 109: Production Logging & Observability](109-production-logging-observability.md)

## Overview

When an error occurs in the application, allow the user to export a sanitized debug log bundle that they can attach to a GitHub issue. The bundle contains the exception details, relevant request/response context, and recent log entries leading up to the error — but **all personally identifiable information (PII) is stripped or redacted** before the export is assembled. The user stays in control: they can review the contents before submitting, and the feature is only available when structured logging (Feature 109) is active.

## Problem Statement

### Current State

- When users encounter an error, the `ErrorAlert` component shows a short message and an optional retry button — but provides no diagnostic data the user can share.
- The developer (project maintainer) must ask the user to describe what happened, guess at the cause, or request access to their logs — which most homelab users either don't retain or are uncomfortable sharing because they contain financial data, account names, and transaction descriptions.
- There is no mechanism to collect relevant log context around an error, strip sensitive data, and package it for submission.

### Target State

- When an error occurs, the `ErrorAlert` component gains a **"Download Debug Log"** button.
- Clicking it downloads a JSON file containing:
  - The error timestamp (UTC), traceId, and correlation context.
  - The exception type, message, and stack trace.
  - A configurable window of recent log entries (e.g., last 30 seconds or last 50 entries) that share the same traceId or were emitted during the failed request pipeline.
  - Request metadata: HTTP method, route template (not the raw path with IDs), status code, elapsed time.
  - Environment context: app version, .NET runtime version, OS description, environment name.
- **All PII is redacted before the bundle is constructed.** The user never has to manually scrub the file.
- The feature is **disabled when logging is not configured** (no Serilog / Feature 109 = no debug export).
- Users can open the JSON file, review it, and attach it to a GitHub issue.

---

## PII Redaction Strategy

The following fields are considered PII and must be redacted or excluded from debug log exports:

| Data Category | Examples | Redaction Rule |
|---------------|----------|----------------|
| **User Identity** | UserId, Username, Email, DisplayName, AvatarUrl | Replace with `[REDACTED]` |
| **Account Names** | `Name` on Account entities/DTOs | Replace with `Account-{short hash}` |
| **Transaction Descriptions** | Merchant names, personal notes | Replace with `[REDACTED]` |
| **Financial Amounts** | Transaction amounts, balances | Replace with `[REDACTED]` |
| **Location Data** | City, Region, Country, PostalCode | Omit entirely |
| **External References** | Bank reference numbers, import IDs | Replace with `[REDACTED]` |
| **Authentication Tokens** | Bearer tokens, cookies, API keys | Never captured in log buffer |
| **IP Addresses** | Client IP from request headers | Replace with `[REDACTED]` |
| **Request Path Parameters** | `/api/v1/accounts/{guid}` raw GUIDs | Use route template only (e.g., `/api/v1/accounts/{id}`) |

### Redaction approach

1. **Allowlist, not blocklist.** The log sanitizer defines an explicit allowlist of safe properties (timestamp, level, messageTemplate, exception type/message/stack, traceId, spanId, requestMethod, routeTemplate, statusCode, elapsed, machineName, environment, appVersion, runtimeVersion, osDescription). Everything else is stripped.
2. **Structured log properties** that match known PII keys (any property name containing `UserId`, `UserName`, `Email`, `Name`, `Amount`, `Balance`, `Description`, `Location`, `Reference`, `Token`, `Password`, `Secret`, `IpAddress`, `Authorization`) are replaced with `[REDACTED]`.
3. **Exception messages** may contain PII embedded by EF Core or domain logic (e.g., "Account 'My Checking' not found"). Exception messages are included but scanned for GUIDs (preserved as correlation aids) while other quoted strings are redacted to `'[REDACTED]'`.
4. **Stack traces** are safe — they contain only code paths, line numbers, and method names.

---

## User Stories

### User Encountering an Error

#### US-110-001: Download Debug Log on Error
**As a** user who encounters an error in the application  
**I want to** download a debug log file with a single click  
**So that** I can attach it to a GitHub issue without needing technical expertise or access to server logs

**Acceptance Criteria:**
- [ ] When an error is displayed via `ErrorAlert`, a "Download Debug Log" button appears alongside Retry/Dismiss
- [ ] Clicking the button downloads a `.json` file named `budget-debug-{traceId}-{timestamp}.json`
- [ ] The file contains the error context, recent log entries, and environment metadata
- [ ] All PII is redacted before the file is generated (see redaction strategy above)
- [ ] The button is only visible when structured logging is active (Feature 109 dependency)

#### US-110-002: Review Before Submitting
**As a** privacy-conscious user  
**I want to** open and review the debug log file before submitting it  
**So that** I can verify no personal information is included

**Acceptance Criteria:**
- [ ] The exported file is human-readable JSON (indented, not minified)
- [ ] The file includes a `_notice` field at the top explaining what's included and that PII has been redacted
- [ ] The file includes a `_redactionSummary` listing how many fields were redacted and what categories

### Project Maintainer

#### US-110-003: Actionable Debug Information
**As the** project maintainer reviewing a GitHub issue  
**I want** the debug log to contain enough context to reproduce or diagnose the issue  
**So that** I can fix bugs without back-and-forth with the reporter

**Acceptance Criteria:**
- [ ] Debug log includes: exception type, message (sanitized), full stack trace
- [ ] Debug log includes: traceId for cross-referencing with server logs (if the maintainer has access)
- [ ] Debug log includes: recent log entries from the same request pipeline (up to 50 entries or 30 seconds)
- [ ] Debug log includes: app version, .NET runtime, OS, environment name
- [ ] Debug log includes: HTTP method, route template, status code, elapsed time
- [ ] Debug log does NOT include raw request/response bodies

#### US-110-004: Feature Disabled Without Logging
**As a** user running without structured logging (Feature 109 not configured)  
**I want** the debug export button to not appear  
**So that** I'm not confused by a feature that can't produce useful output

**Acceptance Criteria:**
- [ ] If Serilog / Feature 109 observability is not active, the debug export button is hidden
- [ ] No in-memory log buffer is allocated when the feature is inactive
- [ ] The API endpoint returns 404 or 501 when the feature is disabled

---

## Technical Design

### Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                        Blazor Client                             │
│                                                                  │
│  ┌──────────────┐                                                │
│  │  ErrorAlert   │──── "Download Debug Log" button               │
│  │  (enhanced)   │         │                                     │
│  └──────────────┘          ▼                                     │
│                    GET /api/v1/debug/logs/{traceId}              │
│                            │                                     │
├────────────────────────────┼─────────────────────────────────────┤
│                        API Layer                                 │
│                            │                                     │
│  ┌─────────────────────────▼──────────────────────────┐          │
│  │              DebugLogController                     │          │
│  │  GET /api/v1/debug/logs/{traceId}                  │          │
│  │  - Returns 404/501 if feature disabled              │          │
│  │  - Fetches entries from IDebugLogBuffer             │          │
│  │  - Runs PII sanitizer                               │          │
│  │  - Returns sanitized JSON file                      │          │
│  └─────────────────────────┬──────────────────────────┘          │
│                            │                                     │
│  ┌─────────────────────────▼──────────────────────────┐          │
│  │              IDebugLogBuffer                        │          │
│  │  - In-memory circular buffer of recent log entries  │          │
│  │  - Populated by Serilog sink (Feature 109)          │          │
│  │  - Queryable by traceId and time window             │          │
│  │  - Bounded size (configurable, default 1000 entries)│          │
│  └─────────────────────────┬──────────────────────────┘          │
│                            │                                     │
│  ┌─────────────────────────▼──────────────────────────┐          │
│  │              ILogSanitizer                          │          │
│  │  - Allowlist-based property filter                  │          │
│  │  - PII key pattern matching                         │          │
│  │  - Exception message redaction                      │          │
│  │  - Produces DebugLogBundle                          │          │
│  └────────────────────────────────────────────────────┘          │
└──────────────────────────────────────────────────────────────────┘
```

### New Types

**`BudgetExperiment.Api` (composition root — all new types here):**

```csharp
// In-memory circular buffer that captures recent structured log entries
public interface IDebugLogBuffer
{
    void Add(DebugLogEntry entry);
    IReadOnlyList<DebugLogEntry> GetByTraceId(string traceId, int maxEntries = 50);
    IReadOnlyList<DebugLogEntry> GetRecent(TimeSpan window, int maxEntries = 50);
    bool IsEnabled { get; }
}

// A single captured log entry (pre-sanitization)
public sealed record DebugLogEntry
{
    public required DateTime TimestampUtc { get; init; }
    public required string Level { get; init; }
    public required string MessageTemplate { get; init; }
    public required string RenderedMessage { get; init; }
    public string? ExceptionType { get; init; }
    public string? ExceptionMessage { get; init; }
    public string? ExceptionStackTrace { get; init; }
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
    public IReadOnlyDictionary<string, object?>? Properties { get; init; }
}

// Sanitizes log entries by stripping PII
public interface ILogSanitizer
{
    SanitizedDebugBundle Sanitize(
        IReadOnlyList<DebugLogEntry> entries,
        string traceId,
        EnvironmentContext environment);
}

// The final export payload
public sealed record SanitizedDebugBundle
{
    public required string Notice { get; init; }
    public required RedactionSummary RedactionSummary { get; init; }
    public required string TraceId { get; init; }
    public required DateTime ExportedAtUtc { get; init; }
    public required EnvironmentContext Environment { get; init; }
    public required RequestContext? Request { get; init; }
    public required ExceptionContext? Exception { get; init; }
    public required IReadOnlyList<SanitizedLogEntry> LogEntries { get; init; }
}
```

### API Endpoint

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/debug/logs/{traceId}` | Returns sanitized debug log bundle as downloadable JSON |

**Response:** `200 OK` with `Content-Type: application/json` and `Content-Disposition: attachment; filename="budget-debug-{traceId}-{timestamp}.json"`  
**Error responses:**
- `501 Not Implemented` — Feature disabled (no Serilog/buffer active)
- `404 Not Found` — No log entries found for the given traceId (expired from buffer or invalid)

### Serilog Integration (Feature 109 Dependency)

A custom Serilog sink (`DebugBufferSink`) writes to the `IDebugLogBuffer`:
- Registered conditionally — only when `Observability:DebugExport:Enabled` is `true` (default: `true` when Serilog is active).
- The sink captures the structured log event (template, properties, exception) into the circular buffer.
- Buffer size is configurable via `Observability:DebugExport:BufferSize` (default: `1000` entries).
- Buffer TTL is configurable via `Observability:DebugExport:RetentionSeconds` (default: `300` — 5 minutes).
- Entries older than the TTL are evicted on next write (lazy cleanup).

### Configuration

```json
{
  "Observability": {
    "DebugExport": {
      "Enabled": true,
      "BufferSize": 1000,
      "RetentionSeconds": 300
    }
  }
}
```

- `Enabled`: `true` by default **when Feature 109 (Serilog) is active**. If Serilog is not configured, this setting is ignored and the feature is inert.
- `BufferSize`: Maximum number of log entries retained in the circular buffer.
- `RetentionSeconds`: How long entries are kept before expiry.
- Setting `Enabled: false` explicitly disables the feature even when Serilog is active.

### Client Changes

**`ErrorAlert.razor` enhancement:**
- Add optional `string? TraceId` parameter (populated from the ProblemDetails `traceId` extension).
- When `TraceId` is non-null, render a "Download Debug Log" link/button.
- The button calls `GET /api/v1/debug/logs/{traceId}` using the existing `IExportDownloadService` pattern.
- If the endpoint returns 501 or 404, the button is either hidden or shows a tooltip explaining logs are unavailable.

**ViewModel / API client changes:**
- When API calls return ProblemDetails with a `traceId`, ViewModels store it alongside the error message.
- The `ErrorAlert` component receives the traceId to enable the download.

### Debug Log Bundle — Example Output

```json
{
  "_notice": "This debug log was exported from Budget Experiment. All personally identifiable information (account names, transaction details, financial amounts, user identifiers) has been redacted. You may review this file before attaching it to a GitHub issue.",
  "_redactionSummary": {
    "totalFieldsRedacted": 12,
    "categoriesRedacted": ["UserId", "AccountName", "TransactionDescription", "Amount", "Location"]
  },
  "traceId": "abc123def456",
  "exportedAtUtc": "2026-03-14T18:30:00Z",
  "environment": {
    "appVersion": "1.5.0",
    "dotnetVersion": "10.0.0",
    "osDescription": "Linux 6.1.0-rpi7-rpi-v8 #1 SMP aarch64",
    "environmentName": "Production",
    "machineName": "raspberrypi"
  },
  "request": {
    "method": "POST",
    "routeTemplate": "/api/v1/accounts/{id}/transactions",
    "statusCode": 500,
    "elapsedMs": 234
  },
  "exception": {
    "type": "System.InvalidOperationException",
    "message": "Sequence contains no matching element",
    "stackTrace": "   at System.Linq.ThrowHelper.ThrowNoMatchException()\n   at BudgetExperiment.Application.Services.TransactionService.CreateAsync(...)\n   ..."
  },
  "logEntries": [
    {
      "timestampUtc": "2026-03-14T18:29:59.812Z",
      "level": "Information",
      "messageTemplate": "Executing endpoint '{EndpointName}'",
      "properties": {
        "EndpointName": "CreateTransaction"
      }
    },
    {
      "timestampUtc": "2026-03-14T18:29:59.900Z",
      "level": "Warning",
      "messageTemplate": "Category {CategoryId} not found, assigning default",
      "properties": {
        "CategoryId": "d3b07384-d113-4ec6-a7dc-5e8bdef7e9c1"
      }
    },
    {
      "timestampUtc": "2026-03-14T18:30:00.046Z",
      "level": "Error",
      "messageTemplate": "Unhandled exception",
      "properties": {},
      "exception": {
        "type": "System.InvalidOperationException",
        "message": "Sequence contains no matching element"
      }
    }
  ]
}
```

### Existing Code Impact

| Project | Changes |
|---------|---------|
| `BudgetExperiment.Api` | New controller, new services (buffer, sanitizer, sink), updated `ObservabilityExtensions` (from Feature 109), new config section |
| `BudgetExperiment.Client` | Updated `ErrorAlert.razor` (new optional TraceId param + download button), ViewModel error state includes traceId |
| `BudgetExperiment.Contracts` | Possibly extend `ProblemDetailsResponse` or add a shared error model — or keep traceId parsing in client only |
| Domain / Application / Infrastructure | **No changes** |

---

## Implementation Plan

### Phase 1: In-Memory Debug Log Buffer and Serilog Sink

**Objective:** Create the circular buffer and Serilog sink that captures structured log entries.

**Tasks:**
- [ ] Define `IDebugLogBuffer` interface and `DebugLogBuffer` implementation (thread-safe circular buffer)
- [ ] Define `DebugLogEntry` record
- [ ] Implement `DebugBufferSink` (Serilog `ILogEventSink`) that writes to `IDebugLogBuffer`
- [ ] Register buffer and sink conditionally in `ObservabilityExtensions` (requires Feature 109)
- [ ] Add `Observability:DebugExport` configuration section
- [ ] Write unit tests: buffer capacity, TTL eviction, traceId querying, thread safety
- [ ] Write unit test: sink does not register when feature is disabled

**Commit:**
```bash
git add .
git commit -m "feat(api): add in-memory debug log buffer with Serilog sink

- Circular buffer captures recent structured log entries
- Queryable by traceId and time window
- Configurable buffer size and retention TTL
- Conditionally registered when Serilog is active

Refs: #110"
```

---

### Phase 2: PII Log Sanitizer

**Objective:** Implement the allowlist-based sanitizer that strips PII from log entries before export.

**Tasks:**
- [ ] Define `ILogSanitizer` interface
- [ ] Implement `LogSanitizer` with allowlist-based property filtering
- [ ] Implement PII key pattern matching (UserId, Email, Name, Amount, Description, Location, etc.)
- [ ] Implement exception message redaction (preserve GUIDs, redact quoted strings)
- [ ] Define `SanitizedDebugBundle`, `SanitizedLogEntry`, `RedactionSummary`, `EnvironmentContext`, `RequestContext`, `ExceptionContext` records
- [ ] Write extensive unit tests:
  - Known PII properties are redacted
  - Safe properties pass through unchanged
  - Exception messages with embedded PII are sanitized
  - GUIDs in exception messages are preserved
  - `_redactionSummary` counts are accurate
  - Unknown properties are stripped (allowlist behavior)
- [ ] Edge case tests: empty entries, null properties, entries with no exception

**Commit:**
```bash
git add .
git commit -m "feat(api): add allowlist-based PII log sanitizer

- Allowlist defines safe properties that pass through
- PII key patterns matched and redacted
- Exception messages sanitized (GUIDs preserved)
- Redaction summary included in output

Refs: #110"
```

---

### Phase 3: Debug Log API Endpoint

**Objective:** Create the controller that returns sanitized debug bundles as downloadable JSON.

**Tasks:**
- [ ] Create `DebugLogController` with `GET /api/v1/debug/logs/{traceId}`
- [ ] Inject `IDebugLogBuffer` and `ILogSanitizer`
- [ ] Return 501 when feature is disabled
- [ ] Return 404 when no entries found for traceId
- [ ] Return 200 with `Content-Disposition: attachment` and indented JSON
- [ ] Populate `EnvironmentContext` from assembly info and runtime
- [ ] Write API integration tests using `WebApplicationFactory`:
  - 501 when buffer not registered
  - 404 for unknown traceId
  - 200 with valid sanitized bundle
  - Verify PII is not present in response body
- [ ] Verify endpoint does not require authentication (debug logs are already sanitized; requiring auth would block unauthenticated error scenarios)

**Commit:**
```bash
git add .
git commit -m "feat(api): add debug log export endpoint

- GET /api/v1/debug/logs/{traceId} returns sanitized bundle
- 501 when feature disabled, 404 when no entries found
- Downloadable JSON with Content-Disposition header
- PII verified absent in integration tests

Refs: #110"
```

---

### Phase 4: Client Integration

**Objective:** Add the "Download Debug Log" button to `ErrorAlert` and wire up traceId propagation.

**Tasks:**
- [ ] Extend `ErrorAlert.razor` with optional `TraceId` parameter
- [ ] Render "Download Debug Log" button when `TraceId` is non-null
- [ ] Use existing `IExportDownloadService` to download from `/api/v1/debug/logs/{traceId}`
- [ ] Handle 501/404 gracefully (hide button or show "logs unavailable" tooltip)
- [ ] Update ViewModels that display errors to parse and store `traceId` from ProblemDetails responses
- [ ] Style the debug download button to be unobtrusive (secondary/ghost style, not primary action)
- [ ] Write bUnit tests for `ErrorAlert`:
  - Button not rendered when `TraceId` is null
  - Button rendered when `TraceId` is provided
  - Download triggered on click

**Commit:**
```bash
git add .
git commit -m "feat(client): add debug log download button to ErrorAlert

- Download Debug Log button appears when traceId is available
- Uses existing export download service pattern
- Graceful fallback when feature is disabled
- bUnit tests for visibility and interaction

Refs: #110"
```

---

### Phase 5: Documentation

**Objective:** Document the feature for users and contributors.

**Tasks:**
- [ ] Add debug export section to `docs/OBSERVABILITY.md` (created in Feature 109)
- [ ] Document configuration options (`Observability:DebugExport:*`)
- [ ] Document what's included and excluded from the export
- [ ] Add a note to `CONTRIBUTING.md` or issue template referencing debug log export
- [ ] Update GitHub issue template (if exists) to mention attaching debug logs
- [ ] Add inline help text near the download button explaining its purpose

**Commit:**
```bash
git add .
git commit -m "docs: document debug log export feature

- Configuration reference in OBSERVABILITY.md
- PII redaction policy documented
- GitHub issue template updated
- Inline help text for download button

Refs: #110"
```

---

## Security Considerations

| Concern | Mitigation |
|---------|------------|
| PII leakage in exports | Allowlist-based sanitizer — only explicitly safe properties pass through |
| Sensitive data in exception messages | Exception messages scanned; quoted strings redacted; GUIDs preserved |
| Buffer memory exhaustion | Bounded circular buffer with configurable max size and TTL eviction |
| Unauthorized access to debug endpoint | Endpoint returns only sanitized data; no raw logs exposed. Consider rate limiting. |
| TraceId enumeration / guessing | TraceIds are high-entropy (128-bit); buffer TTL limits window; no sensitive data even if guessed |
| Request/response body capture | Never captured — only metadata (method, route template, status code) |

---

## Testing Strategy

| Scope | What to Test | How |
|-------|-------------|-----|
| **Unit** | `DebugLogBuffer` — capacity, TTL, traceId query, thread safety | xUnit + concurrent writes |
| **Unit** | `LogSanitizer` — allowlist filtering, PII redaction, exception message sanitization | xUnit with known PII inputs, verify `[REDACTED]` output |
| **Unit** | `DebugBufferSink` — converts Serilog `LogEvent` to `DebugLogEntry` | xUnit with constructed `LogEvent` objects |
| **Integration** | `DebugLogController` — 501/404/200 responses, PII absence in body | `WebApplicationFactory` |
| **Component** | `ErrorAlert` — button visibility, download trigger | bUnit |
| **Manual** | End-to-end: trigger error in UI → download debug log → verify contents | Manual testing |

---

## Rollback Plan

- **Remove the feature:** Delete the controller, buffer, sanitizer, and sink. Remove the `ErrorAlert` enhancements. No database changes, no domain impact.
- **Disable without code changes:** Set `Observability:DebugExport:Enabled` to `false`. Buffer is not allocated, sink is not registered, endpoint returns 501, button is hidden.

---

## Future Considerations (Out of Scope)

- **Automatic issue creation** — pre-fill a GitHub issue with the debug log. Requires GitHub API integration; defer unless demand arises.
- **Log upload to a paste service** — upload to a private paste bin and return a link. Privacy implications; defer.
- **Client-side error capture** — capture Blazor WASM JavaScript errors and unhandled exceptions in the client. Currently only server errors trigger the export.
- **Log compression** — gzip the export for large bundles. Add if bundle sizes become unwieldy.
- **Configurable redaction rules** — let operators define custom PII patterns. Over-engineering for now; the built-in allowlist covers known data shapes.
