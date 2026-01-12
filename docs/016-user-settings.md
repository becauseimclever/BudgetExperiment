# Feature 016: User Settings

## Overview

Add a Settings page to the application that serves as the central location for user runtime configuration. The settings link will be positioned at the bottom of the left navigation panel. The first configuration option will be a flag to automatically realize past-due recurring items without requiring manual confirmation.

## User Stories

### US-001: Access Settings Page
**As a** user  
**I want to** access a settings page from the navigation  
**So that** I can configure the application to my preferences

### US-002: Auto-Realize Past-Due Items
**As a** user  
**I want to** enable automatic realization of past-due recurring items  
**So that** I don't have to manually confirm each one

### US-003: Persist Settings
**As a** user  
**I want to** have my settings remembered between sessions  
**So that** I don't have to reconfigure them each time

### US-004: Settings Navigation Placement
**As a** user  
**I want to** find the settings link at the bottom of the navigation  
**So that** it's consistently located and doesn't clutter the main navigation items

---

## Settings Architecture

### Storage Decision

**Server-side storage is required** because settings like `AutoRealizePastDueItems` directly impact domain logic that executes on the server. The auto-realize process must run server-side to:
- Ensure consistency across all clients/devices
- Execute during API calls (e.g., when loading calendar data)
- Maintain data integrity through proper transactions

### Storage Approach

Since the application currently has no user authentication, we'll use a **single-tenant settings model**:
- One `AppSettings` record in the database
- All settings apply globally to the application instance
- Future: When auth is added, migrate to per-user `UserSettings` table

### Database Schema

```sql
CREATE TABLE "AppSettings" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "AutoRealizePastDueItems" boolean NOT NULL DEFAULT false,
    "PastDueLookbackDays" integer NOT NULL DEFAULT 30,
    "CreatedAtUtc" timestamp NOT NULL DEFAULT (now() at time zone 'utc'),
    "UpdatedAtUtc" timestamp NOT NULL DEFAULT (now() at time zone 'utc')
);

-- Seed single settings record
INSERT INTO "AppSettings" ("Id") VALUES ('00000000-0000-0000-0000-000000000001');
```

### Domain Entity

```csharp
// Domain/AppSettings.cs
public sealed class AppSettings
{
    public static readonly Guid SingletonId = new("00000000-0000-0000-0000-000000000001");

    private AppSettings() { }

    public Guid Id { get; private set; }
    
    /// <summary>
    /// When true, past-due recurring items are automatically realized
    /// without requiring manual confirmation.
    /// </summary>
    public bool AutoRealizePastDueItems { get; private set; }
    
    /// <summary>
    /// How many days back to look for past-due items.
    /// </summary>
    public int PastDueLookbackDays { get; private set; } = 30;

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static AppSettings CreateDefault()
    {
        var now = DateTime.UtcNow;
        return new AppSettings
        {
            Id = SingletonId,
            AutoRealizePastDueItems = false,
            PastDueLookbackDays = 30,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void UpdateAutoRealize(bool enabled)
    {
        AutoRealizePastDueItems = enabled;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdatePastDueLookbackDays(int days)
    {
        if (days < 1 || days > 365)
            throw new DomainException("Lookback days must be between 1 and 365.");
        
        PastDueLookbackDays = days;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
```

### Repository Interface

```csharp
// Domain/IAppSettingsRepository.cs
public interface IAppSettingsRepository
{
    Task<AppSettings> GetAsync(CancellationToken ct = default);
    Task SaveAsync(AppSettings settings, CancellationToken ct = default);
}
```

### Client DTO

```csharp
// Contracts/Dtos/AppSettingsDto.cs
public sealed class AppSettingsDto
{
    public bool AutoRealizePastDueItems { get; set; }
    public int PastDueLookbackDays { get; set; }
}

// Contracts/Dtos/AppSettingsUpdateDto.cs
public sealed class AppSettingsUpdateDto
{
    public bool? AutoRealizePastDueItems { get; set; }
    public int? PastDueLookbackDays { get; set; }
}
```

---

## UI Design

### Navigation Placement

The settings link should be at the bottom of the left navigation, visually separated from the main navigation items:

```
┌─────────────────────┐
│ 📊 Budget Experiment│
├─────────────────────┤
│ 📅 Calendar         │
│ 💳 Accounts         │
│ 🔄 Recurring        │
│ ↔️  Transfers        │
│                     │
│                     │
│                     │
├─────────────────────┤
│ ⚙️  Settings         │  ← Bottom section, separated
└─────────────────────┘
```

### Settings Page Layout

```
┌─────────────────────────────────────────────────────────────────┐
│ Settings                                                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ RECURRING ITEMS                                                 │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ Auto-realize past-due items                          [OFF]  │ │
│ │ When enabled, recurring items past their scheduled          │ │
│ │ date will be automatically converted to transactions        │ │
│ │ without requiring manual confirmation.                      │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ ─────────────────────────────────────────────────────────────── │
│                                                                 │
│ DISPLAY (Future)                                                │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ Date format                              [MMM d, yyyy ▼]    │ │
│ │ Currency                                 [USD ▼]            │ │
│ │ Calendar start day                       [Sunday ▼]         │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ ─────────────────────────────────────────────────────────────── │
│                                                                 │
│ DATA (Future)                                                   │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ Export Data                                      [Export]   │ │
│ │ Import Data                                      [Import]   │ │
│ │ Reset All Data                                   [Reset]    │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Toggle Component

Use a visual toggle switch for boolean settings:

```
OFF: [ ○    ] 
ON:  [    ● ]
```

## API Design

### Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/settings` | Get current application settings |
| PUT | `/api/v1/settings` | Update application settings |

### Response Examples

#### GET /api/v1/settings
```json
{
  "autoRealizePastDueItems": false,
  "pastDueLookbackDays": 30
}
```

#### PUT /api/v1/settings
```json
{
  "autoRealizePastDueItems": true,
  "pastDueLookbackDays": 14
}
```

---

## Implementation Plan

### Phase 1: Domain & Infrastructure
1. Create `AppSettings` domain entity
2. Create `IAppSettingsRepository` interface
3. Create `AppSettingsRepository` implementation
4. Create EF Core configuration
5. Add migration with seed data

### Phase 2: Application Service
1. Create `IAppSettingsService` interface
2. Implement `AppSettingsService`
3. Register in DI

### Phase 3: API Endpoint
1. Create `SettingsController`
2. Add GET and PUT endpoints
3. Add validation

### Phase 4: Client Service
1. Add `GetSettingsAsync` to `IBudgetApiService`
2. Add `UpdateSettingsAsync` to `IBudgetApiService`
3. Implement in `BudgetApiService`

### Phase 5: Settings Page
1. Create `/settings` route
2. Create `Settings.razor` page
3. Wire up load/save via API

### Phase 6: Navigation Update
1. Modify `NavMenu.razor` to add settings link at bottom
2. Add visual separator between main nav and settings

### Phase 7: Auto-Realize Integration
1. Modify `CalendarGridService` to check `AutoRealizePastDueItems`
2. When enabled, auto-realize past-due items during calendar load
3. Return realized items in response for client notification

---

## Technical Details

### Application Service

```csharp
// Application/Services/IAppSettingsService.cs
public interface IAppSettingsService
{
    Task<AppSettingsDto> GetSettingsAsync(CancellationToken ct = default);
    Task<AppSettingsDto> UpdateSettingsAsync(AppSettingsUpdateDto dto, CancellationToken ct = default);
}

// Application/Services/AppSettingsService.cs
public sealed class AppSettingsService : IAppSettingsService
{
    private readonly IAppSettingsRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public AppSettingsService(IAppSettingsRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AppSettingsDto> GetSettingsAsync(CancellationToken ct = default)
    {
        var settings = await _repository.GetAsync(ct);
        return new AppSettingsDto
        {
            AutoRealizePastDueItems = settings.AutoRealizePastDueItems,
            PastDueLookbackDays = settings.PastDueLookbackDays
        };
    }

    public async Task<AppSettingsDto> UpdateSettingsAsync(
        AppSettingsUpdateDto dto, 
        CancellationToken ct = default)
    {
        var settings = await _repository.GetAsync(ct);

        if (dto.AutoRealizePastDueItems.HasValue)
            settings.UpdateAutoRealize(dto.AutoRealizePastDueItems.Value);

        if (dto.PastDueLookbackDays.HasValue)
            settings.UpdatePastDueLookbackDays(dto.PastDueLookbackDays.Value);

        await _unitOfWork.SaveChangesAsync(ct);

        return new AppSettingsDto
        {
            AutoRealizePastDueItems = settings.AutoRealizePastDueItems,
            PastDueLookbackDays = settings.PastDueLookbackDays
        };
    }
}
```

### Repository Implementation

```csharp
// Infrastructure/Repositories/AppSettingsRepository.cs
public sealed class AppSettingsRepository : IAppSettingsRepository
{
    private readonly BudgetDbContext _context;

    public AppSettingsRepository(BudgetDbContext context)
    {
        _context = context;
    }

    public async Task<AppSettings> GetAsync(CancellationToken ct = default)
    {
        var settings = await _context.AppSettings
            .FirstOrDefaultAsync(s => s.Id == AppSettings.SingletonId, ct);

        if (settings is null)
        {
            settings = AppSettings.CreateDefault();
            _context.AppSettings.Add(settings);
            await _context.SaveChangesAsync(ct);
        }

        return settings;
    }

    public Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        // Entity is tracked, SaveChangesAsync via UnitOfWork handles persistence
        return Task.CompletedTask;
    }
}
```

### API Controller

```csharp
// Api/Controllers/SettingsController.cs
[ApiController]
[Route("api/v1/[controller]")]
public sealed class SettingsController : ControllerBase
{
    private readonly IAppSettingsService _settingsService;

    public SettingsController(IAppSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<ActionResult<AppSettingsDto>> GetSettings(CancellationToken ct)
    {
        var settings = await _settingsService.GetSettingsAsync(ct);
        return Ok(settings);
    }

    [HttpPut]
    public async Task<ActionResult<AppSettingsDto>> UpdateSettings(
        [FromBody] AppSettingsUpdateDto dto,
        CancellationToken ct)
    {
        var settings = await _settingsService.UpdateSettingsAsync(dto, ct);
        return Ok(settings);
    }
}
```

### Settings Page Component

```razor
@page "/settings"
@inject IBudgetApiService ApiService

<PageTitle>Settings - Budget Experiment</PageTitle>

<div class="page-container">
    <PageHeader Title="Settings" Subtitle="Configure your preferences" />

    <ErrorAlert Message="@errorMessage" OnDismiss="() => errorMessage = null" />

    @if (isLoading)
    {
        <LoadingSpinner Message="Loading settings..." />
    }
    else if (settings != null)
    {
        <div class="settings-section">
            <h3 class="settings-section-title">Recurring Items</h3>
            
            <div class="setting-item">
                <div class="setting-info">
                    <label class="setting-label">Auto-realize past-due items</label>
                    <p class="setting-description">
                        When enabled, recurring items past their scheduled date will be 
                        automatically converted to transactions without requiring manual 
                        confirmation.
                    </p>
                </div>
                <div class="setting-control">
                    <ToggleSwitch Value="@settings.AutoRealizePastDueItems" 
                                  ValueChanged="OnAutoRealizeChanged" 
                                  Disabled="@isSaving" />
                </div>
            </div>

            <div class="setting-item">
                <div class="setting-info">
                    <label class="setting-label">Past-due lookback days</label>
                    <p class="setting-description">
                        How many days back to check for past-due recurring items.
                    </p>
                </div>
                <div class="setting-control">
                    <input type="number" 
                           min="1" max="365" 
                           value="@settings.PastDueLookbackDays"
                           @onchange="OnLookbackDaysChanged"
                           disabled="@isSaving"
                           class="form-control" 
                           style="width: 80px;" />
                </div>
            </div>
        </div>
    }
</div>

@code {
    private AppSettingsDto? settings;
    private bool isLoading = true;
    private bool isSaving = false;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            settings = await ApiService.GetSettingsAsync();
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load settings: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task OnAutoRealizeChanged(bool value)
    {
        await SaveSettingAsync(new AppSettingsUpdateDto { AutoRealizePastDueItems = value });
    }

    private async Task OnLookbackDaysChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var days))
        {
            await SaveSettingAsync(new AppSettingsUpdateDto { PastDueLookbackDays = days });
        }
    }

    private async Task SaveSettingAsync(AppSettingsUpdateDto update)
    {
        isSaving = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            settings = await ApiService.UpdateSettingsAsync(update);
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to save setting: {ex.Message}";
        }
        finally
        {
            isSaving = false;
            StateHasChanged();
        }
    }
}
```

### Updated NavMenu

```razor
<!-- NavMenu.razor -->
<nav class="nav-menu">
    <div class="nav-header">
        <span class="nav-brand">Budget Experiment</span>
    </div>
    
    <div class="nav-items">
        <NavLink class="nav-link" href="/" Match="NavLinkMatch.All">
            <Icon Name="calendar" /> Calendar
        </NavLink>
        <NavLink class="nav-link" href="/accounts">
            <Icon Name="accounts" /> Accounts
        </NavLink>
        <NavLink class="nav-link" href="/recurring">
            <Icon Name="recurring" /> Recurring
        </NavLink>
        <NavLink class="nav-link" href="/transfers">
            <Icon Name="transfer" /> Transfers
        </NavLink>
    </div>
    
    <div class="nav-footer">
        <NavLink class="nav-link" href="/settings">
            <Icon Name="settings" /> Settings
        </NavLink>
    </div>
</nav>
```

### Auto-Realize Integration (Server-Side)

The auto-realize logic executes on the server during calendar data loading:

```csharp
// In CalendarGridService.GetCalendarGridAsync
public async Task<CalendarGridDto> GetCalendarGridAsync(
    int year,
    int month,
    Guid? accountId = null,
    CancellationToken ct = default)
{
    var settings = await _settingsRepository.GetAsync(ct);
    
    if (settings.AutoRealizePastDueItems)
    {
        // Auto-realize past-due items before building grid
        var realizedCount = await AutoRealizePastDueItemsAsync(
            settings.PastDueLookbackDays, 
            accountId, 
            ct);
        
        // Could include realizedCount in response for client notification
    }

    // Continue with normal grid building...
}

private async Task<int> AutoRealizePastDueItemsAsync(
    int lookbackDays,
    Guid? accountId,
    CancellationToken ct)
{
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var lookbackDate = today.AddDays(-lookbackDays);
    var realizedCount = 0;

    // Get recurring transactions
    var recurringTransactions = accountId.HasValue
        ? await _recurringRepository.GetByAccountIdAsync(accountId.Value, ct)
        : await _recurringRepository.GetActiveAsync(ct);

    foreach (var recurring in recurringTransactions)
    {
        var occurrences = recurring.GetOccurrencesBetween(lookbackDate, today.AddDays(-1));
        
        foreach (var date in occurrences)
        {
            // Skip if already realized
            var existing = await _transactionRepository.GetByRecurringInstanceAsync(
                recurring.Id, date, ct);
            if (existing != null) continue;

            // Skip if marked as skipped
            var exception = await _recurringRepository.GetExceptionAsync(
                recurring.Id, date, ct);
            if (exception?.ExceptionType == ExceptionType.Skipped) continue;

            // Realize the instance
            var amount = exception?.ModifiedAmount ?? recurring.Amount;
            var description = exception?.ModifiedDescription ?? recurring.Description;
            var actualDate = exception?.ModifiedDate ?? date;

            var transaction = Transaction.CreateFromRecurring(
                recurring.AccountId,
                amount,
                actualDate,
                description,
                recurring.Id,
                date,
                recurring.Category);

            await _transactionRepository.AddAsync(transaction, ct);
            realizedCount++;
        }
    }

    // Similar logic for recurring transfers...

    if (realizedCount > 0)
    {
        await _unitOfWork.SaveChangesAsync(ct);
    }

    return realizedCount;
}
```

---

## Files to Create/Modify

### New Files (Domain)
- `src/BudgetExperiment.Domain/AppSettings.cs`
- `src/BudgetExperiment.Domain/IAppSettingsRepository.cs`

### New Files (Contracts)
- `src/BudgetExperiment.Contracts/Dtos/AppSettingsDto.cs`
- `src/BudgetExperiment.Contracts/Dtos/AppSettingsUpdateDto.cs`

### New Files (Infrastructure)
- `src/BudgetExperiment.Infrastructure/Repositories/AppSettingsRepository.cs`
- `src/BudgetExperiment.Infrastructure/Configurations/AppSettingsConfiguration.cs`
- Migration for `AppSettings` table

### New Files (Application)
- `src/BudgetExperiment.Application/Services/IAppSettingsService.cs`
- `src/BudgetExperiment.Application/Services/AppSettingsService.cs`

### New Files (API)
- `src/BudgetExperiment.Api/Controllers/SettingsController.cs`

### New Files (Client)
- `src/BudgetExperiment.Client/Pages/Settings.razor`
- `src/BudgetExperiment.Client/Components/Input/ToggleSwitch.razor` (if not using FluentUI)

### Modified Files
- `src/BudgetExperiment.Infrastructure/BudgetDbContext.cs` - Add `DbSet<AppSettings>`
- `src/BudgetExperiment.Infrastructure/DependencyInjection.cs` - Register repository
- `src/BudgetExperiment.Application/DependencyInjection.cs` - Register service
- `src/BudgetExperiment.Client/Layout/NavMenu.razor` - Add settings link at bottom
- `src/BudgetExperiment.Client/Services/IBudgetApiService.cs` - Add settings methods
- `src/BudgetExperiment.Client/Services/BudgetApiService.cs` - Implement methods
- `src/BudgetExperiment.Application/Services/CalendarGridService.cs` - Add auto-realize logic

---

## CSS Styling

```css
/* Settings page styles */
.settings-section {
    background: var(--surface-color);
    border-radius: var(--radius-md);
    padding: var(--space-4);
    margin-bottom: var(--space-4);
}

.settings-section-title {
    font-size: var(--font-size-lg);
    font-weight: 600;
    margin-bottom: var(--space-4);
    color: var(--text-primary);
}

.setting-item {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    padding: var(--space-3) 0;
    border-bottom: 1px solid var(--border-color);
}

.setting-item:last-child {
    border-bottom: none;
}

.setting-info {
    flex: 1;
    margin-right: var(--space-4);
}

.setting-label {
    font-weight: 500;
    color: var(--text-primary);
    margin-bottom: var(--space-1);
    display: block;
}

.setting-description {
    font-size: var(--font-size-sm);
    color: var(--text-secondary);
    margin: 0;
}

.setting-control {
    flex-shrink: 0;
}

/* Nav footer for settings */
.nav-footer {
    margin-top: auto;
    padding-top: var(--space-4);
    border-top: 1px solid var(--border-color);
}
```

---

## Future Settings Roadmap

| Setting | Type | Default | Description | Storage |
|---------|------|---------|-------------|---------|
| `AutoRealizePastDueItems` | bool | false | Auto-confirm past-due recurring items | Server |
| `PastDueLookbackDays` | int | 30 | How far back to check for past-due items | Server |
| `DefaultCurrency` | string | "USD" | Default currency for new transactions | Server |
| `ConfirmOnDelete` | bool | true | Show confirmation dialog on delete | Server |

Note: Settings that affect domain logic (like auto-realize) must be server-side. Future display-only preferences (date format, theme) could optionally use client-side storage if authentication is not implemented.

---

## Testing Strategy

### Unit Tests (Domain)
1. `AppSettings.CreateDefault` returns correct defaults
2. `AppSettings.UpdateAutoRealize` updates value and timestamp
3. `AppSettings.UpdatePastDueLookbackDays` validates range
4. `AppSettings.UpdatePastDueLookbackDays` throws for invalid values

### Unit Tests (Application)
1. `AppSettingsService.GetSettingsAsync` returns current settings
2. `AppSettingsService.UpdateSettingsAsync` applies partial updates
3. `AppSettingsService.UpdateSettingsAsync` validates input

### Unit Tests (Auto-Realize)
1. Auto-realize creates transactions when enabled
2. Auto-realize skips when disabled
3. Auto-realize respects lookback days
4. Auto-realize skips already-realized items
5. Auto-realize skips skipped items
6. Auto-realize applies exception modifications

### Integration Tests
1. GET `/api/v1/settings` returns settings
2. PUT `/api/v1/settings` updates settings
3. PUT `/api/v1/settings` with invalid data returns 400
4. Settings persist across requests

### Component Tests
1. Settings page loads current values
2. Toggle triggers API update
3. Number input validates range
4. Error states display correctly

---

## Success Criteria

1. ✅ Settings link appears at bottom of navigation
2. ✅ Settings page loads with current values
3. ✅ Toggle persists auto-realize preference
4. ✅ Settings survive browser refresh
5. ✅ Auto-realize integrates with Feature 015
6. ✅ UI is responsive and accessible

---

## Dependencies

- **Feature 015**: Realize Recurring Items - Auto-realize setting depends on realize functionality

---

**Document Version**: 1.0  
**Created**: 2026-01-11  
**Status**: 📋 Planning  
**Author**: Engineering Team
