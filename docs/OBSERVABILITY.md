# Observability Guide

Budget Experiment ships a layered, opt-in observability stack.  
By default only structured console logging is active — no external services required.

## Architecture

```
Application Code (ILogger<T> — unchanged)
        │
   Serilog Pipeline
   ┌──────────┬──────────┬───────────┬──────────┐
   │ Console  │  File    │   Seq     │  OTLP    │
   │ (always) │ (opt-in) │ (opt-in)  │ (opt-in) │
   └──────────┴──────────┴───────────┴──────────┘
        │
   OpenTelemetry SDK (when OTLP enabled)
   ┌──────────┬──────────┬───────────┐
   │ Tracing  │ Metrics  │   Logs    │
   └──────────┴──────────┴───────────┘
```

## Configuration Reference

All settings live under the `Observability` section in `appsettings.json` or can be set via environment variables.

### General

| Setting | Env Var | Default | Description |
|---------|---------|---------|-------------|
| `Observability:ServiceName` | `Observability__ServiceName` | `BudgetExperiment` | OTLP resource `service.name` |
| `Observability:ServiceVersion` | `Observability__ServiceVersion` | Assembly version | OTLP resource `service.version` |

### Serilog Log Levels

Log levels are configured via the `Serilog` configuration section:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System.Net.Http.HttpClient": "Warning"
      }
    }
  }
}
```

Override via environment: `Serilog__MinimumLevel__Default=Debug`.

### File Sink (Optional)

| Setting | Env Var | Default | Description |
|---------|---------|---------|-------------|
| `Observability:File:Path` | `Observability__File__Path` | _(empty = disabled)_ | Log file path. Set to enable file logging. |
| `Observability:File:FileSizeLimitBytes` | `Observability__File__FileSizeLimitBytes` | `10485760` (10 MB) | Max size per log file before rolling. |
| `Observability:File:RetainedFileCountLimit` | `Observability__File__RetainedFileCountLimit` | `5` | Number of rolled files to keep. |

### Seq Sink (Optional)

| Setting | Env Var | Default | Description |
|---------|---------|---------|-------------|
| `Observability:Seq:Url` | `Observability__Seq__Url` | _(empty = disabled)_ | Seq ingestion URL (e.g., `http://seq:5341`). |
| `Observability:Seq:ApiKey` | `Observability__Seq__ApiKey` | _(empty)_ | API key for authenticated Seq instances. |

### OTLP Export (Optional)

| Setting | Env Var | Default | Description |
|---------|---------|---------|-------------|
| `Observability:Otlp:Endpoint` | `Observability__Otlp__Endpoint` | _(empty = disabled)_ | OTLP collector endpoint (e.g., `http://otel-collector:4317`). |
| `Observability:Otlp:Protocol` | `Observability__Otlp__Protocol` | `grpc` | `grpc` (port 4317) or `http/protobuf` (port 4318). |
| `Observability:Otlp:Headers` | `Observability__Otlp__Headers` | _(empty)_ | Custom headers (e.g., `x-api-key=abc123`). |

## Console Output

| Environment | Format |
|-------------|--------|
| `Development` | Human-readable (colored, one line per event) |
| `Production` / others | Compact JSON (machine-parseable, compatible with `docker logs`) |

## Quickstart: Homelab with Seq

Seq provides a free single-user license — no external accounts needed.

```bash
# From the project root:
docker compose -f docker-compose.pi.yml -f docker-compose.observability.yml up -d
```

- Seq UI: `http://<your-host>:8081`
- Logs are automatically forwarded to Seq via the Serilog Seq sink.
- No application code changes or restarts needed (the compose overlay sets `Observability__Seq__Url`).

## Quickstart: OTLP with Grafana/Loki/Jaeger

Set the OTLP endpoint in your `.env` or compose override:

```env
Observability__Otlp__Endpoint=http://otel-collector:4317
Observability__Otlp__Protocol=grpc
```

This exports:
- **Traces**: ASP.NET Core requests, HttpClient calls, EF Core queries
- **Metrics**: ASP.NET Core request duration/count, HTTP client metrics
- **Logs**: All structured log events

Any OTLP-compatible backend works: Grafana Cloud, Datadog, New Relic, Jaeger, etc.

## Request Logging

Serilog request logging middleware produces a single summary line per HTTP request:

```
HTTP GET /api/v1/budgets responded 200 in 12.3 ms
```

- Health check requests (`/health`) are excluded from request logs by default.
- 4xx responses are logged as `Warning`; 5xx as `Error`.

## Enrichment

Every log entry is automatically enriched with:

| Property | Description |
|----------|-------------|
| `MachineName` | Hostname of the container/machine |
| `EnvironmentName` | ASP.NET Core environment (`Development`, `Production`, etc.) |
| `ApplicationVersion` | Assembly informational version |
| `TraceId` / `SpanId` | W3C trace context (populated by ASP.NET Core) |

## Debug Log Export

When an error occurs in the UI, users can download a **sanitized debug log bundle** and attach it to a GitHub issue. The bundle contains the exception details, recent log entries leading up to the error, and environment metadata — with all personally identifiable information (PII) stripped before the file is assembled.

### How It Works

1. When an API call returns an error with a `traceId`, the `ErrorAlert` component shows a **"Debug Log"** download button.
2. Clicking it calls `GET /api/v1/debug/logs/{traceId}`, which returns an indented JSON file.
3. The user can review the file, then attach it to a GitHub issue.

### Configuration

| Setting | Env Var | Default | Description |
|---------|---------|---------|-------------|
| `Observability:DebugExport:Enabled` | `Observability__DebugExport__Enabled` | `true` | Enable/disable the debug export feature. Only active when Serilog is configured. |
| `Observability:DebugExport:BufferSize` | `Observability__DebugExport__BufferSize` | `1000` | Maximum number of log entries retained in the in-memory circular buffer. |
| `Observability:DebugExport:RetentionSeconds` | `Observability__DebugExport__RetentionSeconds` | `300` | How long entries are kept (seconds) before expiry. Default: 5 minutes. |

### What's Included

- Exception type, sanitized message, and full stack trace
- `traceId` for cross-referencing with server-side logs (if available)
- Recent log entries from the same request pipeline (up to 50 entries or 30 seconds)
- App version, .NET runtime version, OS description, environment name
- HTTP method, route template, status code, elapsed time

### What's Excluded (PII Redaction)

The sanitizer uses an **allowlist** approach — only explicitly safe properties pass through. Everything else is stripped or replaced with `[REDACTED]`:

- User identity (UserId, Username, Email)
- Account names → replaced with `Account-{hash}`
- Transaction descriptions, financial amounts
- Location data, external references
- Authentication tokens, IP addresses
- Raw request/response bodies (never captured)
- Request path parameters (route template used instead of raw URL)

The exported file includes a `_redactionSummary` showing how many fields were redacted and what categories.

### Disabling

Set `Observability:DebugExport:Enabled` to `false` to disable the feature without code changes. The in-memory buffer will not be allocated, the Serilog sink will not register, the API endpoint returns 501, and the download button is hidden in the UI.

## Disabling Features

All opt-in features are disabled by leaving their activation setting empty or removing it:

- **File logging**: Remove or empty `Observability:File:Path`
- **Seq**: Remove or empty `Observability:Seq:Url`
- **OTLP**: Remove or empty `Observability:Otlp:Endpoint`
- **Debug export**: Set `Observability:DebugExport:Enabled` to `false`

No code changes, no feature flags — just configuration.
