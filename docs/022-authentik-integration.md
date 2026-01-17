# Feature 022: Authentik Authentication Integration

## Overview

Integrate the Budget Experiment application with an existing Authentik identity provider for user authentication and authorization. This enables multi-user support, personalized budgets, and secure access control.

## Background

### What is Authentik?

[Authentik](https://goauthentik.io/) is an open-source Identity Provider (IdP) that supports:
- OpenID Connect (OIDC)
- OAuth 2.0
- SAML 2.0
- LDAP
- SCIM

### Current State

The application currently operates in single-tenant mode:
- No user authentication
- Global `AppSettings` shared by all users
- All data visible to anyone with access

### Target State

After integration:
- Users authenticate via Authentik
- **Shared budget** accessible by all authenticated users (household/family level)
- **Personal budgets** visible only to the individual user
- API endpoints protected by JWT bearer tokens
- Blazor client uses OIDC for browser-based auth

### Dual-Scope Budget Model

Each application instance supports two budget scopes:

| Scope | Description | Visibility | Use Case |
|-------|-------------|------------|----------|
| **Shared** | Single household/family budget | All authenticated users | Rent, utilities, groceries, shared expenses |
| **Personal** | Individual user budget | Only the owner | Personal spending, hobbies, individual savings |

Users can:
- View and contribute to the shared budget
- Manage their own personal accounts and transactions
- See combined views (shared + personal) or filtered views

> **Note:** Each application deployment has exactly one shared budget. For separate households, deploy separate instances.

---

## User Stories

### Authentication

#### US-001: Login via Authentik
**As a** user  
**I want to** log in using my Authentik account  
**So that** I can access my personal budget data

#### US-002: Logout
**As a** user  
**I want to** log out of the application  
**So that** my data is protected on shared devices

#### US-003: Session Persistence
**As a** user  
**I want to** remain logged in across browser sessions  
**So that** I don't have to log in every time

#### US-004: Automatic Redirect to Login
**As an** unauthenticated user  
**I want to** be redirected to the login page  
**So that** I understand I need to authenticate

### User Data Isolation

#### US-005: Shared Budget Access
**As an** authenticated user  
**I want to** view and manage the shared household accounts and transactions  
**So that** my family can collaborate on our shared budget

#### US-006: Personal Budget Privacy
**As a** user  
**I want to** have personal accounts visible only to me  
**So that** my individual spending remains private

#### US-007: Personal Settings
**As a** user  
**I want to** have my own application settings  
**So that** my preferences don't affect other users

#### US-008: Scope Switching
**As a** user  
**I want to** switch between shared and personal budget views  
**So that** I can focus on the relevant financial data

### User Profile

#### US-009: View Profile
**As a** user  
**I want to** see my profile information from Authentik  
**So that** I know which account I'm logged in as

#### US-010: Profile in Navigation
**As a** user  
**I want to** see my name/avatar in the navigation  
**So that** I have a quick confirmation of my identity

---

## Architecture

### Authentication Flow (OIDC Authorization Code + PKCE)

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Browser   │     │  Budget API │     │  Authentik  │
│   (Blazor)  │     │   (ASP.NET) │     │    (IdP)    │
└──────┬──────┘     └──────┬──────┘     └──────┬──────┘
       │                   │                   │
       │ 1. Access /       │                   │
       │─────────────────►│                   │
       │                   │                   │
       │ 2. 401 Unauthorized                  │
       │◄─────────────────│                   │
       │                   │                   │
       │ 3. Redirect to Authentik /authorize  │
       │─────────────────────────────────────►│
       │                   │                   │
       │ 4. User logs in                      │
       │◄────────────────────────────────────►│
       │                   │                   │
       │ 5. Redirect back with auth code      │
       │◄─────────────────────────────────────│
       │                   │                   │
       │ 6. Exchange code for tokens          │
       │─────────────────────────────────────►│
       │                   │                   │
       │ 7. Return access_token, id_token     │
       │◄─────────────────────────────────────│
       │                   │                   │
       │ 8. API calls with Bearer token       │
       │─────────────────►│                   │
       │                   │                   │
       │                   │ 9. Validate JWT   │
       │                   │─────────────────►│
       │                   │                   │
       │ 10. Protected data                   │
       │◄─────────────────│                   │
       │                   │                   │
```

### Token Types

| Token | Purpose | Lifetime |
|-------|---------|----------|
| **Access Token** | API authorization (JWT) | 5-15 minutes |
| **Refresh Token** | Obtain new access tokens | 7-30 days |
| **ID Token** | User identity claims (JWT) | Session |

### Claims Mapping

Map Authentik claims to application user:

| Authentik Claim | Application Use |
|-----------------|-----------------|
| `sub` | User ID (primary key) |
| `preferred_username` | Display name |
| `email` | User email |
| `name` | Full name |
| `groups` | Role assignment |
| `picture` | Avatar URL |

---

## UI Design

### Scope Switcher in Navigation

The navigation header includes a scope switcher to toggle between shared and personal budgets:

```
┌─────────────────────────────────────────────────────────────────┐
│ 🏠 Shared  ▼        │  Budget Experiment      │ 👤 John [Logout]│
├─────────────────────┼───────────────────────────────────────────┤
│ 📅 Calendar         │                                           │
│ 💳 Accounts         │   Page Content                            │
│ 🔄 Recurring        │                                           │
│ ↔️  Transfers        │                                           │
│ 📊 Budget           │                                           │
├─────────────────────┤                                           │
│ ⚙️  Settings         │                                           │
└─────────────────────┴───────────────────────────────────────────┘
```

Scope dropdown options:
```
┌───────────────────────┐
│ 🏠 Shared          ✓  │  ← Currently selected (household budget)
│ 👤 Personal           │  ← User's private budget
│ 📋 All                │  ← Combined view
└───────────────────────┘
```

### Account/Transaction Scope Indicator

When creating items, show which scope they'll belong to:

```
┌─────────────────────────────────────────────────────────────────┐
│ Add Account                                                 [X] │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ Scope:  (●) 🏠 Shared    ( ) 👤 Personal                        │
│                                                                 │
│ Name: ___________________________                               │
│ Type: [Checking ▼]                                              │
│ Initial Balance: $__________                                    │
│                                                                 │
│ ℹ️  Shared accounts are visible to all family members.           │
│                                                                 │
│                              [Cancel]  [Save]                   │
└─────────────────────────────────────────────────────────────────┘
```

### Scope Badge on Items

In lists, show scope badges to distinguish shared vs personal items:

```
┌─────────────────────────────────────────────────────────────────┐
│ Accounts                                         [+ Add Account]│
├─────────────────────────────────────────────────────────────────┤
│ 🏠 Joint Checking        $5,432.10                  [View]      │
│ 🏠 Savings               $12,500.00                 [View]      │
│ 👤 My Wallet             $150.00                    [View]      │
│ 👤 Side Hustle           $2,340.00                  [View]      │
└─────────────────────────────────────────────────────────────────┘
```

---

## Authentik Configuration

### 1. Create OAuth2/OIDC Provider

In Authentik Admin:

1. **Applications** → **Providers** → **Create**
2. Select **OAuth2/OpenID Provider**
3. Configure:
   ```
   Name: Budget Experiment
   Authorization flow: default-authorization-flow
   Client type: Public
   Client ID: budget-experiment
   Redirect URIs:
     - https://budget.local/authentication/login-callback
     - https://budget.local/authentication/logout-callback
     - https://localhost:5099/authentication/login-callback (dev)
   Scopes: openid, profile, email
   ```

### 2. Create Application

1. **Applications** → **Applications** → **Create**
2. Configure:
   ```
   Name: Budget Experiment
   Slug: budget-experiment
   Provider: Budget Experiment (from step 1)
   Launch URL: https://budget.local
   ```

### 3. Configure Groups (Optional)

For role-based access:
- Create `budget-admins` group
- Create `budget-users` group
- Assign users to appropriate groups

---

## API Implementation

### Configuration

```csharp
// appsettings.json
{
  "Authentication": {
    "Authentik": {
      "Authority": "https://auth.example.com/application/o/budget-experiment/",
      "ClientId": "budget-experiment",
      "Audience": "budget-experiment",
      "RequireHttpsMetadata": true
    }
  }
}
```

### Program.cs Configuration

```csharp
// JWT Bearer Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var authConfig = builder.Configuration.GetSection("Authentication:Authentik");
        
        options.Authority = authConfig["Authority"];
        options.Audience = authConfig["Audience"];
        options.RequireHttpsMetadata = authConfig.GetValue<bool>("RequireHttpsMetadata");
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = "preferred_username",
            RoleClaimType = "groups"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAuthenticatedUser", policy =>
        policy.RequireAuthenticatedUser());
    
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireClaim("groups", "budget-admins"));
});

// ...

app.UseAuthentication();
app.UseAuthorization();
```

### User Context Service

```csharp
// Application/Services/IUserContext.cs
public interface IUserContext
{
    Guid UserId { get; }
    string Username { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
}

// Infrastructure/Services/HttpUserContext.cs
public sealed class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid UserId
    {
        get
        {
            var sub = User?.FindFirstValue("sub");
            return sub != null ? Guid.Parse(sub) : Guid.Empty;
        }
    }

    public string Username => User?.FindFirstValue("preferred_username") ?? "anonymous";

    public string? Email => User?.FindFirstValue("email");

    public bool IsAdmin => User?.HasClaim("groups", "budget-admins") ?? false;
}
```

### Repository Filter by User

All repositories must filter by current user:

```csharp
// Infrastructure/Repositories/AccountRepository.cs
public sealed class AccountRepository : IAccountRepository
{
    private readonly BudgetDbContext _context;
    private readonly IUserContext _userContext;

    public AccountRepository(BudgetDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    public async Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Accounts
            .Where(a => a.UserId == _userContext.UserId)  // Filter by user
            .OrderBy(a => a.Name)
            .ToListAsync(ct);
    }

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Accounts
            .Where(a => a.UserId == _userContext.UserId)  // Filter by user
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    // ... other methods with user filtering
}
```

### Protect Controllers

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]  // Require authentication for all endpoints
public sealed class AccountsController : ControllerBase
{
    // ...
}

// Or use globally in Program.cs:
builder.Services.AddControllers(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});
```

---

## Client Implementation (Blazor WASM)

### Package References

```xml
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Authentication" Version="10.0.0" />
```

### Program.cs Configuration

```csharp
// Client/Program.cs
builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("Authentication:Authentik", options.ProviderOptions);
    options.ProviderOptions.ResponseType = "code";
    options.ProviderOptions.DefaultScopes.Add("openid");
    options.ProviderOptions.DefaultScopes.Add("profile");
    options.ProviderOptions.DefaultScopes.Add("email");
});

// Configure HttpClient to include auth token
builder.Services.AddHttpClient("BudgetApi", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
})
.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
    .CreateClient("BudgetApi"));
```

### Client appsettings.json

```json
{
  "Authentication": {
    "Authentik": {
      "Authority": "https://auth.example.com/application/o/budget-experiment/",
      "ClientId": "budget-experiment",
      "PostLogoutRedirectUri": "/",
      "RedirectUri": "/authentication/login-callback"
    }
  }
}
```

### Authentication Components

```razor
@* App.razor *@
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
                <NotAuthorized>
                    @if (context.User.Identity?.IsAuthenticated != true)
                    {
                        <RedirectToLogin />
                    }
                    else
                    {
                        <p>You are not authorized to access this resource.</p>
                    }
                </NotAuthorized>
                <Authorizing>
                    <LoadingSpinner Message="Checking authorization..." />
                </Authorizing>
            </AuthorizeRouteView>
        </Found>
        <NotFound>
            <PageTitle>Not found</PageTitle>
            <p>Sorry, there's nothing at this address.</p>
        </NotFound>
    </Router>
</CascadingAuthenticationState>
```

```razor
@* Components/RedirectToLogin.razor *@
@inject NavigationManager Navigation

@code {
    protected override void OnInitialized()
    {
        Navigation.NavigateTo($"authentication/login?returnUrl={Uri.EscapeDataString(Navigation.Uri)}");
    }
}
```

```razor
@* Pages/Authentication.razor *@
@page "/authentication/{action}"
@using Microsoft.AspNetCore.Components.WebAssembly.Authentication

<RemoteAuthenticatorView Action="@Action" />

@code {
    [Parameter]
    public string? Action { get; set; }
}
```

### User Profile Display

```razor
@* Components/UserProfile.razor *@
@inject AuthenticationStateProvider AuthStateProvider

<AuthorizeView>
    <Authorized>
        <div class="user-profile">
            @if (!string.IsNullOrEmpty(avatarUrl))
            {
                <img src="@avatarUrl" alt="@context.User.Identity?.Name" class="user-avatar" />
            }
            else
            {
                <div class="user-avatar-placeholder">
                    @(context.User.Identity?.Name?[0].ToString().ToUpper() ?? "?")
                </div>
            }
            <span class="user-name">@context.User.Identity?.Name</span>
            <button class="btn btn-ghost" @onclick="Logout">Logout</button>
        </div>
    </Authorized>
    <NotAuthorized>
        <a href="authentication/login" class="btn btn-primary">Login</a>
    </NotAuthorized>
</AuthorizeView>

@code {
    private string? avatarUrl;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        avatarUrl = authState.User.FindFirst("picture")?.Value;
    }

    private void Logout()
    {
        Navigation.NavigateTo("authentication/logout");
    }
}
```

---

## Database Changes

### Add Scope to Entities

All budget-related entities support both shared and personal scope:

```csharp
// Domain/BudgetScope.cs
public enum BudgetScope
{
    Shared,    // Visible to all authenticated users (household budget)
    Personal   // Private to the owning user
}

// Domain/Account.cs
public sealed class Account
{
    public Guid Id { get; private set; }
    public BudgetScope Scope { get; private set; }
    public Guid? OwnerUserId { get; private set; }  // NULL for Shared, UserId for Personal
    public Guid CreatedByUserId { get; private set; }  // Track who created it
    // ... rest of properties
    
    public static Account CreateShared(string name, AccountType type, MoneyValue initialBalance, 
        DateOnly initialBalanceDate, Guid createdByUserId);
    
    public static Account CreatePersonal(string name, AccountType type, MoneyValue initialBalance, 
        DateOnly initialBalanceDate, Guid ownerUserId);
}

// Similar pattern for: Transaction, RecurringTransaction, RecurringTransfer, BudgetCategory, BudgetGoal
```

### User Context with Scope

```csharp
// Application/Services/IUserContext.cs
public interface IUserContext
{
    Guid UserId { get; }
    string Username { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    
    // Scope filtering
    BudgetScope? CurrentScope { get; }  // NULL = show all (shared + personal)
    void SetScope(BudgetScope? scope);
}
```

### Repository Filter by Scope

Repositories filter based on current scope:

```csharp
// Infrastructure/Repositories/AccountRepository.cs
public sealed class AccountRepository : IAccountRepository
{
    private readonly BudgetDbContext _context;
    private readonly IUserContext _userContext;

    public async Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken ct = default)
    {
        var query = _context.Accounts.AsQueryable();

        // Filter based on current scope
        query = _userContext.CurrentScope switch
        {
            BudgetScope.Shared =>
                query.Where(a => a.Scope == BudgetScope.Shared),
            
            BudgetScope.Personal =>
                query.Where(a => a.Scope == BudgetScope.Personal 
                              && a.OwnerUserId == _userContext.UserId),
            
            null => // Show all accessible items
                query.Where(a => 
                    a.Scope == BudgetScope.Shared ||
                    (a.Scope == BudgetScope.Personal && a.OwnerUserId == _userContext.UserId))
        };

        return await query.OrderBy(a => a.Name).ToListAsync(ct);
    }

    // ... other methods with scope filtering
}
```

### Migrate AppSettings to UserSettings

```csharp
// Domain/UserSettings.cs
public sealed class UserSettings
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }  // Links to Authentik user
    public BudgetScope? DefaultScope { get; private set; } = BudgetScope.Shared;  // NULL = All
    public bool AutoRealizePastDueItems { get; private set; }
    public int PastDueLookbackDays { get; private set; } = 30;
    public string? PreferredCurrency { get; private set; }
    public string? DateFormat { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static UserSettings CreateDefault(Guid userId);
    public void SetDefaultScope(BudgetScope? scope);
}
```

### Migration Script

```csharp
// Add Scope and OwnerUserId columns to Accounts
migrationBuilder.AddColumn<int>(
    name: "Scope",
    table: "Accounts",
    type: "integer",
    nullable: false,
    defaultValue: 0);  // Default to Shared

migrationBuilder.AddColumn<Guid>(
    name: "OwnerUserId",
    table: "Accounts",
    type: "uuid",
    nullable: true);  // NULL for Shared scope

migrationBuilder.AddColumn<Guid>(
    name: "CreatedByUserId",
    table: "Accounts",
    type: "uuid",
    nullable: false,
    defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));  // Placeholder for migration

// Add indexes for efficient scope filtering
migrationBuilder.CreateIndex(
    name: "IX_Accounts_Scope",
    table: "Accounts",
    column: "Scope");

migrationBuilder.CreateIndex(
    name: "IX_Accounts_OwnerUserId",
    table: "Accounts",
    column: "OwnerUserId");

// Repeat for all scoped tables: Transactions, RecurringTransactions, etc.
```

---

## API Endpoints for Scope Selection

### Scope Endpoint

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/user/scope` | Get current scope |
| PUT | `/api/v1/user/scope` | Set current scope (shared, personal, or all) |

### Scope DTOs

```csharp
public sealed class ScopeDto
{
    public string? Scope { get; set; }  // "Shared", "Personal", or null for All
}
```

### Updated Create/Update DTOs

All entity creation endpoints accept an optional scope:

```csharp
public sealed class AccountCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public MoneyDto InitialBalance { get; set; } = new();
    public DateOnly InitialBalanceDate { get; set; }
    public string Scope { get; set; } = "Shared";  // "Shared" or "Personal"
}
```

---

## Implementation Plan

### Phase 1: Infrastructure Setup
- [x] Configure Authentik application and provider
- [x] Add authentication packages to API
- [x] Configure JWT Bearer authentication in Program.cs
- [x] Create `IUserContext` interface and implementation
- [x] Write authentication middleware tests
- [x] Update `.env.example` with authentication environment variables (do NOT commit actual secrets)
- [x] Update `docker-compose.pi.yml` to pass authentication settings via environment variables

### Phase 2: Database Migration
- [x] Add `Scope`, `OwnerUserId`, `CreatedByUserId` columns to all entities
- [x] Create `UserSettings` entity (migrate from `AppSettings`)
- [x] Create EF Core migration
- [x] Update all repository implementations with scope filtering
- [x] Write repository tests with scope filtering

### Phase 3: API Protection
- [x] Add `[Authorize]` to all controllers
- [x] Update service layer to use `IUserContext`
- [x] Create user provisioning endpoint (first login creates user record)
- [x] Add scope selection endpoint
- [x] Write API integration tests with authentication

### Phase 4: Client Authentication
- [ ] Add OIDC authentication packages
- [ ] Configure authentication in Program.cs
- [ ] Create Authentication.razor page
- [ ] Create RedirectToLogin component
- [ ] Update App.razor with CascadingAuthenticationState
- [ ] Configure HttpClient with auth message handler

### Phase 5: Client - Scope Management
- [ ] Create ScopeSwitcher component
- [ ] Add scope to navigation header
- [ ] Update create/edit forms with scope selection
- [ ] Add scope badges to list views
- [ ] Persist scope preference in user settings

### Phase 6: User Experience
- [ ] Create UserProfile component
- [ ] Add user profile to navigation header
- [ ] Add login/logout buttons
- [ ] Handle token expiration gracefully
- [ ] Show appropriate errors for 401/403 responses

### Phase 7: Testing & Polish
- [ ] End-to-end authentication flow testing
- [ ] Token refresh testing
- [ ] Scope filtering isolation testing
- [ ] Performance testing with auth overhead
- [ ] Documentation updates

---

## Security Considerations

### Token Storage
- Access tokens stored in memory (Blazor WASM)
- Refresh tokens managed by OIDC library
- Never store tokens in localStorage (XSS vulnerability)

### API Security
- All endpoints require authentication
- User can only access their own data
- Admin endpoints require admin group membership
- Rate limiting on authentication endpoints

### CORS Configuration
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAuthentik", policy =>
    {
        policy.WithOrigins("https://auth.example.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

### Secure Defaults
- HTTPS required in production
- Secure cookie settings
- Token validation strict by default
- Short access token lifetime

---

## Environment Configuration

### Development
```json
{
  "Authentication": {
    "Authentik": {
      "Authority": "https://auth.dev.example.com/application/o/budget-experiment-dev/",
      "ClientId": "budget-experiment-dev",
      "RequireHttpsMetadata": false
    }
  }
}
```

### Production
```json
{
  "Authentication": {
    "Authentik": {
      "Authority": "https://auth.example.com/application/o/budget-experiment/",
      "ClientId": "budget-experiment",
      "RequireHttpsMetadata": true
    }
  }
}
```

---

## Success Criteria

1. Users can log in via Authentik OIDC flow
2. Users can log out and are redirected appropriately
3. Unauthenticated requests are redirected to login
4. Shared budget data is accessible to all authenticated users
5. Personal data is visible only to the owner
6. Users can switch between shared, personal, and combined views
7. Tokens refresh automatically before expiration
8. All existing functionality works with authentication enabled
9. Performance impact is minimal (< 50ms auth overhead per request)

---

## Rollback Plan

If issues arise:
1. Feature flag to disable authentication requirement
2. API can accept both authenticated and anonymous requests during transition
3. Data migration includes rollback script
4. Client can detect auth failure and show appropriate message

---

## Future Enhancements

- **Social Login**: Add Google, GitHub OAuth providers to Authentik
- **Multi-Factor Authentication**: Enforce MFA for sensitive operations
- **API Keys**: Allow API key authentication for integrations
- **Audit Logging**: Track user actions for security compliance
- **Session Management**: Allow users to view/revoke active sessions
- **Multi-Household Support**: Allow multiple named households per instance (if needed)
- **Admin Role**: Designate admin users who can manage shared data settings
- **Activity Feed**: See what other users have added/changed in shared scope
- **Per-User Spending Limits**: Track individual contributions to shared budget

---

## Related Documents

- [016-user-settings.md](016-user-settings.md) - Settings migration to per-user
- [021-budget-categories-goals.md](021-budget-categories-goals.md) - User-scoped budgets
- [Authentik Documentation](https://goauthentik.io/docs/)
- [ASP.NET Core Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [Blazor WASM Authentication](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/)
