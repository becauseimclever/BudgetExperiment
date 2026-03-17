# Feature 109: Production Logging & Observability
> **Status:** Done

## Overview

Add an opt-in, production-grade structured logging and observability stack to Budget Experiment. The solution must work seamlessly for homelab single-instance deployments (zero additional infrastructure by default) while scaling cleanly to multi-instance environments via industry-standard protocols. All observability features beyond the current built-in console logging are strictly opt-in and configured entirely through `appsettings.json` or environment variables — no code changes required to enable or disable.

## Problem Statement

### Current State

The application uses only the built-in ASP.NET Core `ILogger` with default console output:
- **No structured logging** — log output is plain text, difficult to parse or search.
- **No centralized log aggregation** — each container instance writes to its own stdout; correlating events across restarts or multiple instances requires manually tailing Docker logs.
- **No distributed tracing** — no request correlation across services or ability to trace a request end-to-end.
- **No metrics export** — health checks exist (`/health`) but no runtime metrics (request latency, error rates, active connections) are collected or exported.
- **No log enrichment** — logs lack machine name, environment, version, and other contextual properties that are essential for production debugging.

For a single Raspberry Pi deployment this is tolerable, but it makes debugging production issues painful and doesn't scale when running multiple instances (e.g., demo + production, or a load-balanced setup).

### Target State

A layered observability stack where each tier is independently opt-in:

| Tier | What It Provides | Default | Opt-In Via |
|------|-----------------|---------|------------|
| **Tier 0: Enhanced Console** | Structured JSON console output, log enrichment (machine, environment, version, traceId) | **ON** (replaces current plain text) | Always active |
| **Tier 1: Serilog Pipeline** | Serilog as the logging provider with `ILogger` compatibility, file sink option, filtering, and enrichment | **ON** (drop-in replacement) | `appsettings.json` / env vars |
| **Tier 2: OpenTelemetry (OTLP)** | Distributed tracing, metrics, and log export via OTLP protocol to any compatible backend (Seq, Grafana/Loki, Jaeger, Datadog, etc.) | **OFF** | `Observability:Otlp:Endpoint` |
| **Tier 3: Seq Integration** | Direct Serilog sink to Seq for simple homelab setups (free single-user license) | **OFF** | `Observability:Seq:Url` |

**Key design principles:**
- **Zero-config default** — out of the box, the app logs structured JSON to console (readable by `docker logs`, `journalctl`, and any log aggregator that reads stdout).
- **No infrastructure required** — homelab users don't need to run Seq, Loki, or any external service unless they choose to.
- **Configuration-only activation** — set an endpoint URL in config or env var to enable OTLP/Seq; remove it to disable. No code changes, no feature flags in source.
- **Standard protocols** — OTLP is the CNCF standard; any backend that speaks OTLP works (Grafana Cloud, Datadog, New Relic, self-hosted Jaeger, etc.).
- **Backward compatible** — existing `ILogger<T>` usage throughout the codebase continues to work unchanged. No service code modifications needed.

---

## User Stories

### Homelab Operator

#### US-109-001: Structured Console Logging
**As a** homelab operator  
**I want** application logs to be structured JSON output  
**So that** I can parse and search logs from `docker logs` or `journalctl` without custom tooling

**Acceptance Criteria:**
- [x] Logs output as compact JSON to console by default
- [x] Each log entry includes: timestamp (UTC), level, message, properties, traceId, machine name, environment, app version
- [x] Existing `ILogger<T>` calls throughout the codebase work without modification
- [x] Log levels remain configurable via `appsettings.json` and environment variables (Serilog section)

#### US-109-002: Optional File Logging
**As a** homelab operator  
**I want** to optionally write logs to rolling files  
**So that** I can retain logs across container restarts without an external log aggregator

**Acceptance Criteria:**
- [x] File logging is disabled by default
- [x] Setting `Observability:File:Path` enables rolling file output
- [x] File rotation is configurable (size limit, retained file count) with sensible defaults (e.g., 10 MB, 5 files)
- [ ] File sink does not block application startup if the path is not writable (logs a warning to console instead)

### Multi-Instance Operator

#### US-109-003: OpenTelemetry OTLP Export
**As a** platform operator running multiple instances  
**I want** to export logs, traces, and metrics via OTLP  
**So that** I can aggregate observability data in a centralized backend of my choice

**Acceptance Criteria:**
- [x] OTLP export is disabled by default (no network calls, no additional overhead)
- [x] Setting `Observability:Otlp:Endpoint` (e.g., `http://otel-collector:4317`) enables OTLP export
- [x] Logs, traces, and metrics are all exported when OTLP is enabled
- [x] HTTP requests are automatically instrumented (trace spans for incoming requests, outgoing HTTP calls)
- [x] EF Core database calls are instrumented with trace spans
- [x] ASP.NET Core metrics (request duration, active requests, error count) are exported
- [x] Service name, version, and environment are set as OTLP resource attributes
- [x] Both gRPC (`4317`) and HTTP/protobuf (`4318`) OTLP endpoints are supported via configuration

#### US-109-004: Optional Seq Integration
**As a** homelab operator who wants simple centralized logging  
**I want** to send logs directly to a Seq instance  
**So that** I can search and analyze logs through Seq's UI without running a full OTLP collector stack

**Acceptance Criteria:**
- [x] Seq integration is disabled by default
- [x] Setting `Observability:Seq:Url` (e.g., `http://seq:5341`) enables the Seq sink
- [x] Optional `Observability:Seq:ApiKey` for authenticated Seq instances
- [x] Seq sink works independently of or alongside OTLP export

### Developer

#### US-109-005: Development-Friendly Defaults
**As a** developer running locally  
**I want** readable, colored console output during development  
**So that** I can quickly scan logs without parsing JSON

**Acceptance Criteria:**
- [x] In `Development` environment, console output uses Serilog's human-readable format (not JSON)
- [x] In `Production` / `Staging` / all other environments, console output uses compact JSON
- [x] `appsettings.Development.json` can override to JSON if desired

#### US-109-006: Request Logging Middleware
**As a** developer or operator  
**I want** HTTP request/response logging with configurable verbosity  
**So that** I can see request timings and status codes without enabling full debug logging

**Acceptance Criteria:**
- [x] Serilog request logging middleware replaces the verbose default ASP.NET Core request logging
- [x] Each request produces a single summary log line: method, path, status code, elapsed time
- [x] Health check endpoint requests are excluded from request logs by default (reduces noise)
- [x] Request log level is configurable (e.g., suppress 2xx, log 4xx as Warning, 5xx as Error)

---

## Technical Design

### Architecture Changes

```
┌─────────────────────────────────────────────────────────┐
│                    Application Code                     │
│              (ILogger<T> — no changes)                  │
├─────────────────────────────────────────────────────────┤
│                   Serilog Pipeline                      │
│  ┌──────────┐ ┌──────────┐ ┌───────────┐ ┌──────────┐  │
│  │ Console  │ │  File    │ │   Seq     │ │  OTLP    │  │
│  │ Sink     │ │  Sink    │ │   Sink    │ │  Export  │  │
│  │ (always) │ │ (opt-in) │ │ (opt-in)  │ │ (opt-in) │  │
│  └──────────┘ └──────────┘ └───────────┘ └──────────┘  │
├─────────────────────────────────────────────────────────┤
│              OpenTelemetry SDK (opt-in)                  │
│  ┌──────────┐ ┌──────────┐ ┌───────────┐               │
│  │ Tracing  │ │ Metrics  │ │   Logs    │               │
│  │ (ASP.NET,│ │ (ASP.NET,│ │   (OTLP   │               │
│  │  HTTP,   │ │  runtime)│ │  exporter)│               │
│  │  EF Core)│ │          │ │           │               │
│  └──────────┘ └──────────┘ └───────────┘               │
└─────────────────────────────────────────────────────────┘
```

All configuration lives in `BudgetExperiment.Api` (the composition root). No other projects gain new dependencies.

### NuGet Packages

| Package | Purpose | Project |
|---------|---------|---------|
| `Serilog.AspNetCore` | Serilog integration with ASP.NET Core (includes Console sink, enrichers, request logging) | Api |
| `Serilog.Sinks.File` | Rolling file sink | Api |
| `Serilog.Sinks.Seq` | Direct Seq sink | Api |
| `Serilog.Expressions` | Log filtering expressions (e.g., exclude health checks) | Api |
| `OpenTelemetry.Extensions.Hosting` | OTel SDK host integration | Api |
| `OpenTelemetry.Instrumentation.AspNetCore` | Auto-instrument incoming HTTP requests | Api |
| `OpenTelemetry.Instrumentation.Http` | Auto-instrument outgoing HttpClient calls | Api |
| `OpenTelemetry.Instrumentation.EntityFrameworkCore` | Auto-instrument EF Core queries | Api |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | OTLP exporter (gRPC + HTTP) | Api |

### Configuration Schema

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
  },
  "Observability": {
    "File": {
      "Path": "",
      "FileSizeLimitBytes": 10485760,
      "RetainedFileCountLimit": 5
    },
    "Seq": {
      "Url": "",
      "ApiKey": ""
    },
    "Otlp": {
      "Endpoint": "",
      "Protocol": "grpc",
      "Headers": ""
    },
    "ServiceName": "BudgetExperiment",
    "ServiceVersion": ""
  }
}
```

- Empty/missing `Path`, `Url`, or `Endpoint` = feature disabled.
- `ServiceVersion` auto-detected from assembly informational version if not set.
- All values overridable via environment variables: `Observability__Otlp__Endpoint`, `Observability__Seq__Url`, etc.

### Docker Compose Extension (Optional)

Provide a `docker-compose.observability.yml` overlay that users can optionally include:

```yaml
# Usage: docker compose -f docker-compose.pi.yml -f docker-compose.observability.yml up -d
services:
  seq:
    image: datalust/seq:latest
    environment:
      ACCEPT_EULA: "Y"
    ports:
      - "5341:5341"  # Ingestion
      - "8081:80"    # UI
    volumes:
      - seq-data:/data
    restart: unless-stopped

  budget-app:
    environment:
      - Observability__Seq__Url=http://seq:5341

volumes:
  seq-data:
```

This keeps the main compose files untouched — observability is purely additive.

### Code Changes

All changes are in `BudgetExperiment.Api`:

**New file: `Observability/ObservabilityExtensions.cs`**
- `AddObservability(this WebApplicationBuilder)` extension method
- Configures Serilog host, enrichers, sinks (console always, file/Seq conditionally)
- Conditionally registers OpenTelemetry tracing, metrics, and log export when OTLP endpoint is configured
- Reads all config from `IConfiguration`

**Modified: `Program.cs`**
- Replace `builder.Logging` setup with `builder.AddObservability()`
- Add `app.UseSerilogRequestLogging()` in the middleware pipeline
- Remove existing `Logging` configuration section usage (Serilog reads from `Serilog` section and falls back to `Logging` section)

### Existing Code Impact

- **Zero changes** to Domain, Application, Infrastructure, Client, Contracts, or Shared projects.
- **Zero changes** to any `ILogger<T>` injection or usage — Serilog implements the `ILogger` interface.
- **Zero changes** to existing tests — logging is an infrastructure concern at the composition root.

---

## Implementation Plan

### Phase 1: Serilog Integration with Console Sink

**Objective:** Replace built-in logging with Serilog, maintaining current behavior with structured console output.

**Tasks:**
- [x] Add `Serilog.AspNetCore` and `Serilog.Expressions` NuGet packages to Api project
- [x] Create `Observability/ObservabilityExtensions.cs` with `AddObservability()` method
- [x] Configure Serilog with console sink (compact JSON for Production, human-readable for Development)
- [x] Add enrichers: machine name, environment, version, traceId
- [x] Wire up in `Program.cs` — replace built-in logging bootstrap
- [x] Add `UseSerilogRequestLogging()` middleware with health check exclusion filter
- [x] Update `appsettings.json` with `Serilog` configuration section
- [x] Verify existing log output matches expectations
- [x] Verify existing tests still pass

**Commit:**
```bash
git add .
git commit -m "feat(api): add Serilog structured logging with console sink

- Replace built-in logging with Serilog pipeline
- Compact JSON output in Production, readable in Development
- Add machine name, environment, version enrichment
- Add request logging middleware (excludes health checks)
- All existing ILogger<T> usage unchanged

Refs: #109"
```

---

### Phase 2: Optional File and Seq Sinks

**Objective:** Add opt-in file logging and Seq integration, activated by configuration only.

**Tasks:**
- [x] Add `Serilog.Sinks.File` and `Serilog.Sinks.Seq` NuGet packages to Api project
- [x] Extend `ObservabilityExtensions` to conditionally add file sink when `Observability:File:Path` is set
- [x] Extend `ObservabilityExtensions` to conditionally add Seq sink when `Observability:Seq:Url` is set
- [x] Add default configuration values in `appsettings.json` (all disabled)
- [x] Log an informational message at startup listing which sinks are active
- [ ] Write unit test verifying sinks are not registered when config is empty
- [x] Verify no new dependencies are loaded when sinks are disabled (no unnecessary network calls)

**Commit:**
```bash
git add .
git commit -m "feat(api): add optional file and Seq logging sinks

- File sink enabled via Observability:File:Path
- Seq sink enabled via Observability:Seq:Url
- Both disabled by default — zero overhead when unused
- Startup log reports active sinks

Refs: #109"
```

---

### Phase 3: OpenTelemetry OTLP Export

**Objective:** Add opt-in OpenTelemetry tracing, metrics, and log export via OTLP.

**Tasks:**
- [x] Add OpenTelemetry NuGet packages to Api project (Hosting, AspNetCore, Http, EF Core, OTLP exporter)
- [x] Extend `ObservabilityExtensions` to conditionally register OTel SDK when `Observability:Otlp:Endpoint` is set
- [x] Configure tracing: ASP.NET Core, HttpClient, EF Core instrumentation
- [x] Configure metrics: ASP.NET Core, HTTP client meters
- [x] Configure log export via OTLP
- [x] Set resource attributes: service name, version, environment
- [x] Support both gRPC and HTTP/protobuf protocols via `Observability:Otlp:Protocol`
- [x] Log an informational message at startup when OTLP is enabled (showing endpoint)
- [x] Verify OTLP is completely inert when endpoint is not configured (no SDK overhead)

**Commit:**
```bash
git add .
git commit -m "feat(api): add optional OpenTelemetry OTLP export

- Tracing: ASP.NET Core, HttpClient, EF Core auto-instrumentation
- Metrics: ASP.NET Core, runtime, HTTP client
- Logs: OTLP log exporter
- Enabled only when Observability:Otlp:Endpoint is set
- Supports gRPC and HTTP/protobuf protocols

Refs: #109"
```

---

### Phase 4: Docker Compose Overlay and Documentation

**Objective:** Provide an optional docker-compose overlay for Seq and document the full observability stack.

**Tasks:**
- [x] Create `docker-compose.observability.yml` with Seq service
- [x] Update `README.md` with observability section (brief, links to detailed doc)
- [x] Create `docs/OBSERVABILITY.md` with:
  - Configuration reference (all `Observability:*` settings with examples)
  - Quickstart: homelab with Seq
  - Quickstart: multi-instance with OTLP collector
  - Environment variable mapping for Docker deployments
  - Example Grafana/Loki stack overlay (reference only, not provided as compose file)
- [x] Update `.env.example` with observability-related environment variables (commented out)
- [x] Update `DEPLOY-QUICKSTART.md` with optional observability section

**Commit:**
```bash
git add .
git commit -m "docs: add observability documentation and Docker Compose overlay

- docker-compose.observability.yml with Seq service
- OBSERVABILITY.md configuration reference
- Homelab and multi-instance quickstart guides
- Updated deployment docs

Refs: #109"
```

---

## Testing Strategy

| Scope | What to Test | How |
|-------|-------------|-----|
| **Unit** | `ObservabilityExtensions` registers/skips sinks based on configuration | Mock `IConfiguration`, assert service registrations |
| **Integration** | Application starts successfully with default config (no OTLP, no Seq) | `WebApplicationFactory` startup test |
| **Integration** | Application starts successfully with OTLP endpoint configured (uses test collector or verifies no crash) | `WebApplicationFactory` with config override |
| **Integration** | Structured log output contains expected properties (traceId, machine, version) | Capture console output, parse JSON, assert fields |
| **Manual** | Seq receives logs when configured | Run `docker-compose.observability.yml`, verify Seq UI |
| **Manual** | OTLP export to Jaeger/Grafana | Run local Jaeger, configure endpoint, verify traces appear |

---

## Rollback Plan

Since all features are opt-in and the only "always-on" change is Serilog replacing the built-in logger:
- **Serilog removal:** Remove `Serilog.AspNetCore` package, revert `Program.cs` to use `builder.Logging`. All `ILogger<T>` calls continue to work unchanged.
- **OTLP/Seq removal:** Remove endpoint from configuration — features deactivate immediately. No code rollback needed.
- **Full rollback:** Revert the commits from this feature branch. No database migrations, no domain model changes, no contract changes.

---

## Future Considerations (Out of Scope)

- **Grafana/Loki docker-compose overlay** — document as reference; let users bring their own stack.
- **Custom metrics** — domain-specific counters (e.g., transactions imported per minute). Add when specific needs arise.
- **Alerting** — Seq or Grafana alerting rules. Operator responsibility, not application concern.
- **Log sampling** — reduce volume in high-throughput scenarios. Add if performance data justifies.
- **Dashboard templates** — pre-built Grafana dashboards. Nice-to-have, not MVP.

---

## Conventional Commit Reference

| Type | When to Use | Example |
|------|-------------|---------|
| `feat` | New observability capability | `feat(api): add Serilog structured logging` |
| `docs` | Documentation updates | `docs: add OBSERVABILITY.md` |
| `chore` | NuGet package additions | `chore(api): add OpenTelemetry packages` |
| `test` | Observability tests | `test(api): verify OTLP disabled by default` |
