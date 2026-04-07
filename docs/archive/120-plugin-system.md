# Feature 120: Plugin System
> **Status:** Canceled — feature on hold indefinitely; no value under Kakeibo/Kaizen pivot

## Overview

Add a plugin architecture that lets users build and install their own extensions without contributing code back to the repository. Plugins can add API endpoints, Blazor UI pages and navigation items, react to domain events, provide custom import parsers, and contribute new report types. The system uses a shared-AppDomain model with assembly scanning from a `/plugins` folder (MVP) and NuGet-based distribution in a later phase.

A new **Plugin SDK** (`BudgetExperiment.Plugin.Abstractions`) ships as a standalone NuGet package that plugin authors reference. The host-side infrastructure (`BudgetExperiment.Plugin.Hosting`) handles discovery, loading, lifecycle management, and domain event dispatch.

## Problem Statement

### Current State

- All functionality is built directly into the core application projects.
- Adding a new import parser, report type, or UI page requires forking the repo or submitting a pull request.
- The domain event scaffolding exists on `Transaction` (`_domainEvents` collection) but is never dispatched or consumed — there is no extension point for reacting to domain changes.
- There is no mechanism for third-party code to register services, controllers, or UI components.

### Target State

- Users drop a plugin folder (containing a DLL and optional dependencies) into a `/plugins` directory and restart the app.
- The host discovers and loads all valid plugins, registers their services, and exposes their API endpoints and UI pages.
- Plugins subscribe to domain events (e.g., `TransactionCreatedEvent`, `ImportCompletedEvent`) to implement custom side effects — notifications, audit logs, analytics, external API calls.
- Plugin API endpoints live under a `/api/v1/plugins/{pluginName}/` prefix to avoid route collisions.
- Plugin Blazor pages appear in the sidebar navigation alongside core pages.
- A management endpoint and UI page list installed plugins, their versions, and capabilities.
- Later, plugins can be distributed via NuGet feeds for easier installation and updates.

---

## User Stories

### Plugin Authoring

#### US-120-001: Create a Plugin with the SDK
**As a** developer  
**I want to** reference a Plugin SDK NuGet package and implement `IPlugin`  
**So that** I can build a self-contained extension for BudgetExperiment

**Acceptance Criteria:**
- [ ] `BudgetExperiment.Plugin.Abstractions` is a standalone project with no dependency on core application projects
- [ ] `IPlugin` interface defines `Name`, `Version`, `Description`, `ConfigureServices(IServiceCollection)`, and `Initialize(IPluginContext)`
- [ ] `IPluginContext` provides access to logging (`ILogger`), configuration (`IConfiguration`), and event subscription
- [ ] SDK includes base classes and interfaces: `PluginControllerBase`, `IDomainEventHandler<T>`, `IImportParser`, `IReportBuilder`, `IPluginNavigationProvider`
- [ ] A sample plugin project demonstrates all extension points

#### US-120-002: Add API Endpoints from a Plugin
**As a** plugin developer  
**I want to** define controllers that inherit `PluginControllerBase`  
**So that** my plugin's REST endpoints are automatically discovered and routed under `/api/v1/plugins/{pluginName}/`

**Acceptance Criteria:**
- [ ] Plugin controllers are discovered via `ApplicationPartManager` at startup
- [ ] All plugin routes are prefixed with `/api/v1/plugins/{pluginName}/`
- [ ] Plugin endpoints appear in the OpenAPI spec and Scalar UI
- [ ] Plugin endpoints participate in the same auth, validation, and error-handling pipeline as core endpoints

#### US-120-003: Add a Custom Import Parser
**As a** plugin developer  
**I want to** implement `IImportParser` to support a new file format (e.g., OFX, QIF, custom bank CSV)  
**So that** users can import transactions from sources the core app doesn't handle

**Acceptance Criteria:**
- [ ] `IImportParser` defines `SupportedFormats` (file extensions/MIME types), `ParseAsync(Stream, CancellationToken)`
- [ ] The import pipeline resolves all registered `IImportParser` implementations (core + plugins)
- [ ] The correct parser is selected based on file extension or content type
- [ ] Plugin parsers return the same DTO structure as core parsers

#### US-120-004: Add a Custom Report Type
**As a** plugin developer  
**I want to** implement `IReportBuilder` to contribute a new report type  
**So that** users can access custom analytics beyond what ships with the core app

**Acceptance Criteria:**
- [ ] `IReportBuilder` defines `ReportName`, `ReportDescription`, and `BuildAsync(ReportParameters, CancellationToken)`
- [ ] Plugin report builders are discoverable alongside core report builders
- [ ] Plugin reports appear in the reports UI and are accessible via the reports API

### Plugin Installation & Management

#### US-120-005: Install a Plugin via Folder
**As an** application administrator  
**I want to** drop a plugin folder into the `/plugins` directory and restart the app  
**So that** the plugin is loaded and functional without code changes

**Acceptance Criteria:**
- [ ] The app scans a configurable plugins directory at startup (default: `{AppRoot}/plugins/`)
- [ ] Each subfolder is treated as a plugin; the loader looks for assemblies containing `IPlugin` implementations
- [ ] Plugin load order is deterministic (alphabetical by folder name)
- [ ] Invalid or incompatible plugins are logged as warnings and skipped (do not crash the host)
- [ ] The plugins path is configurable via `appsettings.json` under `Plugins:Path`

#### US-120-006: View Installed Plugins
**As an** application administrator  
**I want to** see a list of installed plugins with their name, version, status, and capabilities  
**So that** I can verify what's loaded and troubleshoot issues

**Acceptance Criteria:**
- [ ] `GET /api/v1/plugins` returns a list of loaded plugins with name, version, description, and capabilities
- [ ] A Blazor management page at `/plugins` displays the same information
- [ ] Each plugin entry shows its status (Loaded, Failed, Disabled)

#### US-120-007: Enable/Disable a Plugin
**As an** application administrator  
**I want to** disable a plugin without removing its files  
**So that** I can troubleshoot or temporarily deactivate functionality

**Acceptance Criteria:**
- [ ] `Plugins:Disabled` configuration section accepts a list of plugin names to skip during loading
- [ ] Disabled plugins appear in the management UI with a "Disabled" status
- [ ] Enabling/disabling requires an app restart (no hot-reload in MVP)

### Domain Events

#### US-120-008: React to Domain Events
**As a** plugin developer  
**I want to** implement `IDomainEventHandler<TransactionCreatedEvent>` (or any domain event)  
**So that** my plugin can react to changes in the system — sending notifications, updating external systems, or computing derived data

**Acceptance Criteria:**
- [ ] `IDomainEvent` marker interface exists in the Domain project
- [ ] Core domain event types are defined: `TransactionCreatedEvent`, `TransactionUpdatedEvent`, `TransactionDeletedEvent`, `TransactionCategorizedEvent`, `ImportCompletedEvent`, `RuleSuggestionAcceptedEvent`, `ReconciliationMatchedEvent`
- [ ] `IDomainEventDispatcher` dispatches events to all registered `IDomainEventHandler<T>` implementations
- [ ] Events are dispatched **after** `SaveChangesAsync` succeeds (post-commit) to ensure data consistency
- [ ] Handler failures are logged but do not roll back the committed transaction
- [ ] Existing `Transaction._domainEvents` collection is wired to use `IDomainEvent`

### Blazor UI Integration

#### US-120-009: Add Navigation Items and Pages
**As a** plugin developer  
**I want to** implement `IPluginNavigationProvider` to register sidebar navigation items and routable Blazor pages  
**So that** my plugin's UI is discoverable alongside core pages

**Acceptance Criteria:**
- [ ] `IPluginNavigationProvider` returns a list of nav items (label, icon CSS class, route path)
- [ ] Plugin nav items appear in a "Plugins" section of the sidebar
- [ ] Blazor router discovers `@page` components from plugin assemblies
- [ ] Plugin static assets (CSS, JS) are served from the plugin folder via a file provider
- [ ] Plugin pages participate in the same layout and theming as core pages

---

## Technical Design

### Architecture Changes

Two new projects are added to the solution:

| Layer | New Component | Responsibility |
|-------|---------------|----------------|
| SDK | `BudgetExperiment.Plugin.Abstractions` | Plugin interfaces, base classes, attributes. No dependency on core projects. Shipped as NuGet for plugin authors. |
| Hosting | `BudgetExperiment.Plugin.Hosting` | Plugin scanner, loader, registry, domain event dispatcher. Referenced by API project. |
| Domain | `IDomainEvent`, `IDomainEventDispatcher`, core event types | Marker interface and event definitions for the domain event system |
| Infrastructure | Domain event dispatch wiring in `BudgetDbContext.SaveChangesAsync` | Publishes collected domain events after successful commit |
| API | Plugin loading in `Program.cs`, `PluginsController` | Composition root integration and management endpoint |
| Client | `PluginNavigationService`, sidebar modifications | Aggregates and renders plugin nav items |

**Dependency graph (new projects):**

```
Plugin.Abstractions  ← (referenced by plugin authors, NO core deps)
        ↑
Plugin.Hosting       ← (references Abstractions + Domain)
        ↑
    API/Program.cs   ← (calls Plugin.Hosting to discover & load)
```

### Plugin Lifecycle

```
Startup
  │
  ├─ 1. Discovery   ─ Scan /plugins/ for assemblies containing IPlugin
  ├─ 2. Validation   ─ Check SDK version compatibility, skip disabled plugins
  ├─ 3. Registration ─ Call IPlugin.ConfigureServices(IServiceCollection)
  ├─ 4. Build Host   ─ Normal ASP.NET Core host build (all DI finalized)
  ├─ 5. Initialize   ─ Call IPlugin.Initialize(IPluginContext) for each plugin
  └─ 6. Runtime      ─ Plugin services participate in normal DI resolution
```

### Domain Model

```csharp
// src/BudgetExperiment.Domain/Events/IDomainEvent.cs
public interface IDomainEvent
{
    DateTime OccurredAtUtc { get; }
}

// src/BudgetExperiment.Domain/Events/TransactionCreatedEvent.cs
public sealed record TransactionCreatedEvent(
    Guid TransactionId,
    Guid AccountId,
    decimal Amount,
    string Description,
    DateOnly Date,
    DateTime OccurredAtUtc) : IDomainEvent;

// src/BudgetExperiment.Domain/Events/ImportCompletedEvent.cs
public sealed record ImportCompletedEvent(
    Guid AccountId,
    int TransactionCount,
    int DuplicateCount,
    DateTime OccurredAtUtc) : IDomainEvent;

// Additional events: TransactionUpdatedEvent, TransactionDeletedEvent,
// TransactionCategorizedEvent, RuleSuggestionAcceptedEvent, ReconciliationMatchedEvent
```

```csharp
// src/BudgetExperiment.Domain/Events/IDomainEventDispatcher.cs
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct);
}
```

```csharp
// src/BudgetExperiment.Plugin.Abstractions/IPlugin.cs
public interface IPlugin
{
    string Name { get; }
    string Version { get; }
    string Description { get; }

    void ConfigureServices(IServiceCollection services);
    Task InitializeAsync(IPluginContext context, CancellationToken ct);
}

// src/BudgetExperiment.Plugin.Abstractions/IPluginContext.cs
public interface IPluginContext
{
    IServiceProvider Services { get; }
    IConfiguration Configuration { get; }
    ILoggerFactory LoggerFactory { get; }
}
```

```csharp
// src/BudgetExperiment.Plugin.Abstractions/IDomainEventHandler.cs
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken ct);
}
```

```csharp
// src/BudgetExperiment.Plugin.Abstractions/IImportParser.cs
public interface IImportParser
{
    string Name { get; }
    IReadOnlyList<string> SupportedExtensions { get; }
    Task<IReadOnlyList<ParsedTransactionRow>> ParseAsync(
        Stream fileStream,
        CancellationToken ct);
}
```

```csharp
// src/BudgetExperiment.Plugin.Abstractions/IReportBuilder.cs
public interface IReportBuilder
{
    string ReportName { get; }
    string ReportDescription { get; }
    Task<ReportResult> BuildAsync(
        ReportParameters parameters,
        CancellationToken ct);
}
```

```csharp
// src/BudgetExperiment.Plugin.Abstractions/IPluginNavigationProvider.cs
public interface IPluginNavigationProvider
{
    IReadOnlyList<PluginNavItem> GetNavItems();
}

public sealed record PluginNavItem(
    string Label,
    string Route,
    string IconCssClass,
    int Order = 100);
```

```csharp
// src/BudgetExperiment.Plugin.Abstractions/PluginControllerBase.cs
[ApiController]
public abstract class PluginControllerBase : ControllerBase
{
    // Route convention applied by host: /api/v1/plugins/{pluginName}/[controller]
}
```

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET    | `/api/v1/plugins` | List all loaded plugins with name, version, status, capabilities |
| GET    | `/api/v1/plugins/{name}` | Get detail for a specific plugin |
| *      | `/api/v1/plugins/{pluginName}/**` | Plugin-contributed endpoints (routed by ApplicationPartManager) |

### Database Changes

No database schema changes in MVP. Plugins that need persistence should manage their own storage (e.g., a separate SQLite database in their plugin folder, or request access to the host `IUnitOfWork` via `IPluginContext`). A future phase may add a plugin metadata table to track installed plugin versions and enable/disable state.

### Configuration

```json
// appsettings.json
{
  "Plugins": {
    "Path": "plugins",
    "Disabled": []
  }
}
```

### UI Components

| Component | Location | Purpose |
|-----------|----------|---------|
| Plugin management page | `/plugins` | List installed plugins, show status, version, capabilities |
| Sidebar "Plugins" section | `MainLayout.razor` | Renders nav items from all `IPluginNavigationProvider` implementations |
| Plugin page host | Dynamic | Blazor router discovers `@page` components from plugin assemblies |

---

## Implementation Plan

### Phase 1: Domain Events Foundation

**Objective:** Wire up the existing domain event scaffolding into a functional event dispatch system. This is a prerequisite for plugin event subscriptions and valuable on its own.

**Tasks:**
- [ ] Create `IDomainEvent` marker interface in `Domain/Events/`
- [ ] Create core domain event record types (`TransactionCreatedEvent`, `TransactionUpdatedEvent`, `TransactionDeletedEvent`, `TransactionCategorizedEvent`, `ImportCompletedEvent`, `RuleSuggestionAcceptedEvent`, `ReconciliationMatchedEvent`)
- [ ] Create `IDomainEventDispatcher` interface in `Domain/Events/`
- [ ] Refactor `Transaction._domainEvents` to use `IDomainEvent` instead of `object`
- [ ] Add domain event raising to entity methods where appropriate
- [ ] Implement `DomainEventDispatcher` (resolves `IDomainEventHandler<T>` from DI, catches and logs handler failures)
- [ ] Wire dispatcher into `BudgetDbContext.SaveChangesAsync` — collect events before save, dispatch after commit
- [ ] Write unit tests for dispatcher logic, event types, entity event collection

**Commit:**
```bash
git commit -m "feat(domain): wire up domain event dispatch system

- IDomainEvent marker interface and core event types
- IDomainEventDispatcher with DI-based handler resolution
- Events dispatched post-commit in BudgetDbContext.SaveChangesAsync
- Transaction._domainEvents refactored to IDomainEvent

Refs: #120"
```

---

### Phase 2: Plugin SDK (Abstractions)

**Objective:** Create the plugin SDK project with all interfaces and base classes that plugin authors will reference.

**Tasks:**
- [ ] Create `BudgetExperiment.Plugin.Abstractions` project (targets .NET 10, minimal dependencies)
- [ ] Add to solution under `src/`
- [ ] Implement `IPlugin`, `IPluginContext` interfaces
- [ ] Implement `IDomainEventHandler<T>` interface
- [ ] Implement `IImportParser` interface with `ParsedTransactionRow` DTO
- [ ] Implement `IReportBuilder` interface with `ReportParameters` and `ReportResult` types
- [ ] Implement `IPluginNavigationProvider` with `PluginNavItem` record
- [ ] Implement `PluginControllerBase` abstract class
- [ ] Add `PluginAttribute` for assembly-level metadata (name, version)
- [ ] Write contract tests verifying interface shapes and constraints

**Commit:**
```bash
git commit -m "feat(plugin): create Plugin.Abstractions SDK project

- IPlugin, IPluginContext for lifecycle management
- IDomainEventHandler<T> for event subscriptions
- IImportParser, IReportBuilder for pipeline extensions
- IPluginNavigationProvider for UI integration
- PluginControllerBase for API endpoints

Refs: #120"
```

---

### Phase 3: Plugin Hosting (Loader + Registry)

**Objective:** Build the host-side infrastructure to discover, load, validate, and register plugins.

**Tasks:**
- [ ] Create `BudgetExperiment.Plugin.Hosting` project, reference Abstractions + Domain
- [ ] Add to solution under `src/`
- [ ] Implement `PluginScanner` — scans configured directory for assemblies with `IPlugin` implementations
- [ ] Implement `PluginDescriptor` — metadata record (name, version, assembly, capabilities, status)
- [ ] Implement `PluginRegistry` — tracks all discovered plugins and their descriptors
- [ ] Implement `PluginLoader` — loads assemblies, instantiates `IPlugin`, calls `ConfigureServices`
- [ ] Implement `PluginHostedService` — calls `IPlugin.InitializeAsync` after host startup
- [ ] Add `AddPlugins(IConfiguration)` extension method for DI registration
- [ ] Read `Plugins:Path` and `Plugins:Disabled` from configuration
- [ ] Log plugin discovery, loading, and any failures
- [ ] Unit tests for scanner (mock file system), loader, registry
- [ ] Integration test with a sample plugin DLL

**Commit:**
```bash
git commit -m "feat(plugin): create Plugin.Hosting with loader and registry

- PluginScanner discovers IPlugin implementations from /plugins directory
- PluginLoader handles assembly loading and DI registration
- PluginRegistry tracks loaded plugins and capabilities
- PluginHostedService calls InitializeAsync on startup
- Configurable via Plugins:Path and Plugins:Disabled

Refs: #120"
```

---

### Phase 4: API Integration + Management Endpoint

**Objective:** Wire plugin loading into the API composition root and expose plugin management endpoints.

**Tasks:**
- [ ] Add `builder.Services.AddPlugins(builder.Configuration)` call to `Program.cs` (after `AddInfrastructure`)
- [ ] Configure `ApplicationPartManager` to add plugin assemblies as application parts (controller discovery)
- [ ] Apply route convention to prefix plugin controllers with `/api/v1/plugins/{pluginName}/`
- [ ] Create `PluginsController` with `GET /api/v1/plugins` and `GET /api/v1/plugins/{name}`
- [ ] Add `PluginInfoResponse` DTO to Contracts
- [ ] Add `Plugins` configuration section to `appsettings.json`
- [ ] Write API integration tests (plugin list, plugin detail, plugin controller routing)

**Commit:**
```bash
git commit -m "feat(api): integrate plugin system into API pipeline

- Plugin loading in Program.cs composition root
- ApplicationPartManager discovers plugin controllers
- PluginsController for management endpoints
- Plugin routes prefixed with /api/v1/plugins/{name}/

Refs: #120"
```

---

### Phase 5: Import Parser + Report Builder Extension Points

**Objective:** Allow plugins to contribute custom import parsers and report builders.

**Tasks:**
- [ ] Modify `ImportService` to resolve all `IImportParser` implementations from DI
- [ ] Add parser selection logic based on file extension / content type
- [ ] Modify report services to resolve all `IReportBuilder` implementations from DI
- [ ] Expose plugin report types via the existing reports API
- [ ] Create a sample import parser plugin (e.g., OFX format)
- [ ] Tests: plugin parser selected for matching extension, report builder contributes to report list

**Commit:**
```bash
git commit -m "feat(plugin): add import parser and report builder extension points

- ImportService resolves plugin IImportParser implementations
- Report services resolve plugin IReportBuilder implementations
- Parser selection by file extension
- Sample OFX parser plugin for testing

Refs: #120"
```

---

### Phase 6: Blazor UI Integration

**Objective:** Allow plugins to add pages and navigation items to the Blazor client.

**Tasks:**
- [ ] Create `PluginNavigationService` in Client that aggregates `IPluginNavigationProvider` nav items
- [ ] Modify `MainLayout.razor` to render a "Plugins" section in the sidebar with plugin nav items
- [ ] Configure Blazor router (`AdditionalAssemblies`) to discover `@page` components from plugin assemblies
- [ ] Add file provider middleware to serve plugin static assets from plugin folders
- [ ] Create a sample plugin with a Blazor page (`/plugins/sample`) and nav item
- [ ] Tests: nav item renders, page routes correctly, static assets served

**Commit:**
```bash
git commit -m "feat(client): add plugin UI integration

- PluginNavigationService aggregates plugin nav items
- Sidebar renders plugin navigation section
- Blazor router discovers plugin page components
- Static file middleware for plugin assets

Refs: #120"
```

---

### Phase 7: Plugin Management Page + Documentation

**Objective:** Provide a management UI for viewing installed plugins and create a plugin authoring guide.

**Tasks:**
- [ ] Create `Plugins.razor` page at `/plugins` showing installed plugin list (name, version, status, capabilities)
- [ ] Show plugin load errors inline for troubleshooting
- [ ] Display disabled plugins with visual indicator
- [ ] Create `docs/plugin-authoring-guide.md` with SDK reference, sample plugin walkthrough, and best practices
- [ ] Create a complete sample plugin project under `samples/SamplePlugin/` as a reference implementation
- [ ] Add plugin system section to `README.md`

**Commit:**
```bash
git commit -m "feat(client): add plugin management page and authoring guide

- /plugins page lists installed plugins with status and capabilities
- Plugin authoring guide with SDK reference
- Sample plugin project as reference implementation

Refs: #120"
```

---

### Phase 8: NuGet Plugin Distribution (Future)

**Objective:** Support installing plugins from NuGet feeds for easier distribution and updates.

**Tasks:**
- [ ] Add NuGet-based plugin resolution (configure feed URL in settings)
- [ ] Download, extract, and cache plugin packages at startup
- [ ] Plugin version compatibility checks against SDK version
- [ ] Plugin install/uninstall via management API
- [ ] Update management UI with install-from-feed capability
- [ ] Publish `BudgetExperiment.Plugin.Abstractions` as a public NuGet package

**Commit:**
```bash
git commit -m "feat(plugin): add NuGet-based plugin distribution

- Configure NuGet feed for plugin packages
- Auto-download and cache plugin packages
- Version compatibility validation
- Management API for install/uninstall

Refs: #120"
```

