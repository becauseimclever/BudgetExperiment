# Feature 060: Silent Token Refresh Handling
> **Status:** Planning
> **Priority:** Low
> **Deferred From:** Feature 052

## Overview

This feature adds graceful handling for token expiry during active app usage. When a user's access token expires mid-session, the app will attempt a silent token refresh before redirecting to login, preserving the user's context and avoiding jarring interruptions.

## Problem Statement

### Current State

- When the access token expires, API calls return 401 Unauthorized
- User sees an error or is abruptly redirected to login
- Any unsaved work or context is lost
- No attempt is made to silently refresh the token

### Target State

- 401 responses trigger automatic silent token refresh
- If refresh succeeds, the original request is retried transparently
- If refresh fails, user sees a brief "Session expired" message before redirect
- Return URL is preserved for seamless re-authentication
- User experience is smooth even after long idle periods

---

## User Stories

### US-060-001: Silent Token Refresh
**As a** user  
**I want to** have my session automatically refreshed  
**So that** I don't get interrupted while using the app

**Acceptance Criteria:**
- [ ] 401 responses trigger automatic token refresh attempt
- [ ] Successful refresh retries the original request
- [ ] User is unaware refresh happened (no UI indication)
- [ ] Works for all API calls (transactions, budgets, etc.)

### US-060-002: Graceful Session Expiry
**As a** user  
**I want to** see a clear message when my session can't be refreshed  
**So that** I understand why I'm being redirected

**Acceptance Criteria:**
- [ ] If silent refresh fails, show "Session expired" notification
- [ ] Notification is brief (2-3 seconds) before redirect
- [ ] Return URL is preserved so user returns to same page after login
- [ ] No error messages or broken UI states

### US-060-003: Preserve Unsaved Work
**As a** user  
**I want to** not lose unsaved form data when session expires  
**So that** I don't have to re-enter information

**Acceptance Criteria:**
- [ ] Form data persisted to local storage before redirect
- [ ] Form data restored after re-authentication
- [ ] User informed that data was preserved

---

## Technical Design

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     HTTP Request Flow                        │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  HttpClient                                                  │
│      │                                                       │
│      ▼                                                       │
│  ┌─────────────────────────────────────────┐                │
│  │       TokenRefreshHandler               │                │
│  │  (DelegatingHandler)                    │                │
│  │                                          │                │
│  │  1. Send request                         │                │
│  │  2. If 401 response:                     │                │
│  │     a. Attempt silent refresh            │                │
│  │     b. If success: retry original        │                │
│  │     c. If fail: redirect to login        │                │
│  └─────────────────────────────────────────┘                │
│      │                                                       │
│      ▼                                                       │
│  API Response                                                │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Token Refresh Handler

```csharp
public class TokenRefreshHandler : DelegatingHandler
{
    private readonly IAccessTokenProvider tokenProvider;
    private readonly NavigationManager navigation;
    private readonly ILogger<TokenRefreshHandler> logger;
    private static readonly SemaphoreSlim refreshLock = new(1, 1);
    private static bool isRefreshing = false;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Attempt silent refresh
            var refreshed = await TryRefreshTokenAsync(cancellationToken);
            
            if (refreshed)
            {
                // Retry original request with new token
                var newRequest = await CloneRequestAsync(request);
                return await base.SendAsync(newRequest, cancellationToken);
            }
            else
            {
                // Redirect to login
                await HandleSessionExpiredAsync();
            }
        }

        return response;
    }

    private async Task<bool> TryRefreshTokenAsync(CancellationToken ct)
    {
        await refreshLock.WaitAsync(ct);
        try
        {
            if (isRefreshing) return false;
            isRefreshing = true;

            var result = await tokenProvider.RequestAccessToken(
                new AccessTokenRequestOptions { ReturnUrl = navigation.Uri });

            return result.Status == AccessTokenResultStatus.Success;
        }
        finally
        {
            isRefreshing = false;
            refreshLock.Release();
        }
    }

    private async Task HandleSessionExpiredAsync()
    {
        // Show brief notification
        // Then redirect with return URL
        var returnUrl = Uri.EscapeDataString(navigation.Uri);
        navigation.NavigateTo($"authentication/login?returnUrl={returnUrl}", forceLoad: true);
    }
}
```

### Registration

```csharp
// In Program.cs
builder.Services.AddTransient<TokenRefreshHandler>();
builder.Services.AddHttpClient("BudgetApi", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
})
.AddHttpMessageHandler<TokenRefreshHandler>();
```

### Session Expired Notification

```razor
@* Components/SessionExpiredToast.razor *@
<div class="session-expired-toast @(IsVisible ? "visible" : "")">
    <span>Session expired. Redirecting to login...</span>
</div>

@code {
    [Parameter]
    public bool IsVisible { get; set; }
}
```

---

## Implementation Plan

### Phase 1: Token Refresh Handler

**Objective:** Create HTTP handler that intercepts 401 responses

**Tasks:**
- [ ] Create `TokenRefreshHandler` DelegatingHandler
- [ ] Implement silent token refresh logic
- [ ] Add thread-safe refresh lock (prevent parallel refreshes)
- [ ] Write unit tests for handler logic

### Phase 2: Session Expired UI

**Objective:** Show user-friendly notification on session expiry

**Tasks:**
- [ ] Create `SessionExpiredToast` component
- [ ] Add CSS for toast animation
- [ ] Integrate with `TokenRefreshHandler`
- [ ] Test notification timing

### Phase 3: Form Data Preservation

**Objective:** Preserve unsaved form data across re-auth

**Tasks:**
- [ ] Create `FormStateService` for local storage persistence
- [ ] Add form state save before redirect
- [ ] Add form state restore after login
- [ ] Test with TransactionForm component

### Phase 4: Integration & Testing

**Objective:** Full integration with existing auth flow

**Tasks:**
- [ ] Register handler in Program.cs
- [ ] Update `AuthInitializer` to work with refresh handler
- [ ] E2E tests for token expiry scenarios
- [ ] Test idle session for extended period (30+ min)

---

## Edge Cases

| Scenario | Behavior |
|----------|----------|
| Multiple concurrent 401s | Only one refresh attempt via semaphore |
| Refresh token also expired | Redirect to login immediately |
| User on `/authentication/*` route | Skip refresh, let OIDC handle |
| Offline / network error | Show network error, not session expired |

---

## Dependencies

- Feature 052 (Performance TTFP) - ✅ Complete
- OIDC token refresh support in Authentik

---

## Changelog

| Date | Author | Description |
|------|--------|-------------|
| 2026-02-01 | AI | Created feature doc (deferred from Feature 052) |
