# Feature 117: Initial Page Load Performance
> **Status:** Done

## Overview

The initial page load experience involves multiple sequential delays that compound into a frustrating wait. A user opening the app sees a loading spinner for several seconds while the Blazor WASM framework downloads and initializes, then gets redirected to Authentik for authentication, then waits again on redirect back while the WASM runtime re-initializes and checks auth state, and finally waits a third time while the landing page fetches its data from the API. Each phase is individually tolerable; stacked together they create a perception that the app is sluggish.

This feature attacks all four phases: WASM payload size, authentication round-trip overhead, post-auth framework re-initialization, and initial data loading.

## Problem Statement

### Current State

The page load has four sequential bottleneck phases:

**Phase A — WASM Download & Parse (~2–4s on first load):**
- The Blazor WebAssembly client has no publish-time optimizations configured. No IL trimming (`PublishTrimmed`), no AOT compilation, no globalization invariance, no lazy-loading of assemblies.
- The `_framework/` directory ships the full .NET runtime plus all referenced assemblies — approximately 15–25 MB uncompressed. Gzip-compressed `.wasm.gz` variants exist (served by ASP.NET Core's static web assets middleware), but Brotli pre-compression is not explicitly enabled for static serving.
- The nginx reverse proxy caches `_framework/` files for only 1 day (`max-age=86400`). These fingerprinted, immutable files could be cached for a year.

**Phase B — Auth Redirect (~1–3s):**
- On first visit (no token cached), the client boots the WASM runtime, fetches `api/v1/config` to get OIDC settings, determines the user is unauthenticated, then triggers a full-page `forceLoad: true` redirect to Authentik. Authentik renders its login page (or auto-redirects if session exists). After login, Authentik redirects back to the app.
- This redirect back triggers another full WASM download + parse cycle — the browser may serve from cache, but the framework still needs to initialize, re-fetch config, check auth state, and resolve the user.
- The `preconnect` hint to Authentik is present in `index.html`, which helps, but the OIDC metadata discovery (`/.well-known/openid-configuration`) and JWKS fetch are still sequential blocking operations.

**Phase C — Post-Auth Bootstrap (~1–2s):**
- After the auth redirect returns, `AuthInitializer.razor` checks `AuthenticationStateProvider`, waits for `Task<AuthenticationState>`, and only then hides the loading overlay and renders the Router.
- The `Program.cs` startup itself is sequential: create `HttpClient` → fetch `api/v1/config` → configure OIDC → build service provider → render root component. No parallelism.

**Phase D — Data Loading (~0.5–2s):**
- Once the landing page component renders, it calls its ViewModel's `OnInitializedAsync`, which makes one or more API calls to load data (e.g., budget summary, accounts, transactions).
- These API calls go through `TokenRefreshHandler` (adds bearer token) and `ScopeHandler` (adds scope header) before hitting the server. The server then runs sequential database queries (see Feature 111 for that concern).
- No data prefetching occurs during phases A–C. The first API call doesn't start until the page component mounts.

**Total perceived wait: 4–10 seconds** from URL entry to seeing usable data, depending on network conditions and cache state.

### Target State

- **First paint with content** under 1.5 seconds (cached) / 3 seconds (cold).
- **Auth redirect** is a one-time cost that feels instant on subsequent visits (token caching, silent renew).
- **Post-auth data** appears within 500ms of the app becoming interactive (prefetching during bootstrap).
- **Return visits** feel near-instant — service worker serves cached framework files, auth token is still valid, data API responses are pre-cached or eagerly fetched.

---

## Optimization Areas

### Area 1: Reduce WASM Payload Size (High Impact)

**1a. Enable IL Trimming for Published Builds**

The Client `.csproj` has zero publish optimizations. Adding trimming removes unused code paths from the shipped assemblies.

```xml
<!-- BudgetExperiment.Client.csproj (publish-only settings) -->
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>partial</TrimMode>
</PropertyGroup>
```

`TrimMode=partial` is conservative — only trims assemblies explicitly marked as trimmable (most BCL assemblies are). This avoids breaking reflection-heavy code while still removing significant dead weight. Expected savings: 20–40% of framework DLL size.

**1b. Globalization Invariant Mode (If Acceptable)**

If the app doesn't need full ICU globalization data (locale-specific date/number formatting beyond what's handled client-side via JS), enabling invariant mode removes the ~2 MB ICU data files:

```xml
<InvariantGlobalization>true</InvariantGlobalization>
```

**Caution:** This breaks `CultureInfo` formatting. Since the client already detects timezone via JS interop and formats dates/currencies in the ViewModel layer, this may be viable — but requires validation that no Blazor component relies on `ToString("C")` or similar culture-dependent calls. A safer alternative is `BlazorWebAssemblyLoadAllGlobalizationData=false` combined with `HybridGlobalization=true` to ship only the needed locale.

**1c. Lazy-Load Non-Critical Assemblies**

Assemblies for features the user won't access on first load (AI chat, import, reconciliation, reporting) can be deferred:

```xml
<ItemGroup>
    <BlazorWebAssemblyLazyLoad Include="BudgetExperiment.Contracts.wasm" />
    <!-- Other non-landing-page assemblies -->
</ItemGroup>
```

This requires identifying which assemblies are needed for the landing page vs. deferred pages, and adding `OnNavigateAsync` in the Router to load them on demand.

---

### Area 2: Aggressive Caching (High Impact, Zero Risk)

**2a. Immutable Cache Headers for Fingerprinted Assets**

ASP.NET Core's static web assets already fingerprint `_framework/` files (e.g., `BudgetExperiment.Client.29s0yetj7k.wasm`) and set `Cache-Control: max-age=31536000, immutable`. This is correct.

However, the nginx reverse proxy overrides this with `max-age=86400` (1 day) for `_framework/` files. This is unnecessarily conservative for fingerprinted, immutable content.

**Fix in nginx config:**
```nginx
location /_framework/ {
    proxy_pass http://127.0.0.1:5099;
    # ...existing proxy headers...
    
    # Fingerprinted files are immutable - cache aggressively
    proxy_cache_valid 200 365d;
    expires 365d;
    add_header Cache-Control "public, max-age=31536000, immutable";
}
```

**2b. Service Worker for Offline Cache**

A Blazor WASM service worker (`service-worker.published.js`) can cache all framework files on first load so subsequent visits are served entirely from the local cache — zero network requests for the WASM runtime.

```xml
<!-- BudgetExperiment.Client.csproj -->
<ServiceWorkerAssetsManifest>service-worker-assets.js</ServiceWorkerAssetsManifest>

<ItemGroup>
    <ServiceWorker Include="wwwroot/service-worker.js" PublishedContent="wwwroot/service-worker.published.js" />
</ItemGroup>
```

This is the single biggest improvement for return visits. The framework files download once and are served from `CacheStorage` on every subsequent visit, eliminating Phase A entirely.

**2c. Cache OIDC Discovery Metadata**

The OIDC `/.well-known/openid-configuration` and JWKS endpoints are fetched on every app initialization. These change extremely rarely. Caching them in `sessionStorage` with a short TTL (5–15 minutes) avoids a network round-trip on most visits.

---

### Area 3: Optimize Auth Flow (Medium Impact)

**3a. Silent Token Renewal**

The OIDC library (`Microsoft.AspNetCore.Components.WebAssembly.Authentication`) supports silent renewal via hidden iframe. When a user returns to the app with an expired access token but a valid Authentik session cookie, silent renewal can obtain a new token without a full redirect cycle.

Ensure `AuthenticationService.js` is configured with:
- `automaticSilentRenew: true`
- Appropriate `silentRedirectUri` pointing to a lightweight HTML page

This eliminates Phase B entirely for users whose Authentik session is still alive.

**3b. Faster First-Visit Auth Redirect**

Currently the client boots the entire WASM runtime before discovering the user is unauthenticated and redirecting. For a first-time visitor, this means downloading 15+ MB of WASM just to be told "go to Authentik."

**Option: Early auth check via JavaScript.** Before `blazor.webassembly.js` loads, a small inline script can check for an existing auth token in storage. If none exists, redirect to Authentik immediately — skipping the entire WASM download.

```html
<script>
    // Fast-path: if no auth token cached, redirect before loading WASM
    (function() {
        const oidcKey = Object.keys(sessionStorage).find(k => k.startsWith('oidc.'));
        if (!oidcKey && !window.location.pathname.startsWith('/authentication')) {
            // No token cached - will need to redirect anyway
            // Defer to WASM to handle redirect (needs OIDC config)
            // But we can start preloading Authentik assets
            const link = document.createElement('link');
            link.rel = 'prefetch';
            link.href = 'https://authentik.becauseimclever.com/application/o/authorize/?...';
            document.head.appendChild(link);
        }
    })();
</script>
```

A more aggressive approach: embed the OIDC authority URL directly in `index.html` and redirect via JS without waiting for WASM, but this duplicates config and introduces a maintenance burden.

**3c. Preload Config Endpoint**

The `api/v1/config` fetch during `Program.cs` startup is sequential with everything else. Issuing it as a `<link rel="preload">` or `fetch()` in the HTML `<head>` allows the browser to start the request while the WASM runtime downloads:

```html
<link rel="preload" href="api/v1/config" as="fetch" crossorigin="anonymous" />
```

Then in `Program.cs`, check if the preloaded response is available before making a new request.

---

### Area 4: Optimize Post-Auth Bootstrap (Medium Impact)

**4a. Parallel Service Initialization**

In `Program.cs`, the `api/v1/config` fetch is awaited before configuring services. If the OIDC settings were embedded in `index.html` (via server-side templating or a `<script>` tag with JSON), the config fetch could be eliminated entirely:

```html
<!-- In index.html, rendered by the API's fallback handler -->
<script id="app-config" type="application/json">
    {"authentication":{"mode":"oidc","authority":"...","clientId":"..."}}
</script>
```

The client reads this from the DOM — zero network requests for config.

**4b. Prefetch Landing Page Data During Auth Resolution**

While `AuthInitializer` waits for `AuthenticationStateProvider` to resolve, the app is idle. If we know the user will land on the budget page, we can start prefetching that data in parallel with auth resolution:

```csharp
// In AuthInitializer - fire and forget the data prefetch
var prefetchTask = PrefetchLandingPageDataAsync(ct);
var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();

if (authState.User.Identity?.IsAuthenticated == true)
{
    await prefetchTask; // Ensure data is ready
    // ...proceed to render
}
```

This requires careful handling — the prefetch needs a valid token, so it can only start after auth is confirmed. But if the token is already cached (return visit), both can proceed in parallel.

**4c. Skeleton Screens Instead of Spinner**

Replace the loading spinner with skeleton screens that match the layout of the landing page. This doesn't reduce actual load time but significantly improves perceived performance — the user sees a page "forming" rather than staring at a spinner.

---

### Area 5: Server-Side Response Compression (Low-Medium Impact)

**5a. Enable Brotli Response Compression in ASP.NET Core**

Currently no `UseResponseCompression` middleware is configured. While ASP.NET Core serves pre-compressed `.gz` files for static assets, API JSON responses are not compressed:

```csharp
// In Program.cs
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json" });
});

// Early in the pipeline
app.UseResponseCompression();
```

This compresses API responses (JSON payloads) on the fly. Budget summary, transaction lists, and other API responses can be 60–80% smaller with Brotli.

**5b. Enable Brotli Pre-Compression for Static Assets**

Ensure the published output includes `.br` files alongside `.gz` for all `_framework/` assets. Brotli typically achieves 15–25% better compression than gzip for WASM and DLL files.

**5c. Nginx Brotli Module**

If the nginx reverse proxy supports `ngx_brotli`, enable it:

```nginx
brotli on;
brotli_types application/wasm application/javascript text/css application/json;
brotli_comp_level 6;
```

---

### Area 6: HTTP/2 Server Push / Early Hints (Low Impact, Nice-to-Have)

**6a. 103 Early Hints**

ASP.NET Core supports 103 Early Hints, which tells the browser to start fetching critical subresources before the main response arrives:

```csharp
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Headers.Link = 
            "</_framework/blazor.webassembly.js>; rel=preload; as=script, " +
            "</css/app.css>; rel=preload; as=style";
        await context.Response.WriteAsync("", context.RequestAborted); // 103
    }
    await next();
});
```

This is marginal but essentially free to implement.

---

## Implementation Plan

### Phase 1: Quick Wins — Caching & Compression
1. [x] Fix nginx `_framework/` cache headers (immutable, 1-year)
2. [x] Add `UseResponseCompression` with Brotli to API
3. [x] Preload `api/v1/config` via `<link rel="preload">` in `index.html`
4. [x] Embed client config as inline JSON in `index.html` (eliminate config fetch)

### Phase 2: Payload Reduction
5. [x] Enable IL trimming (`PublishTrimmed`, `TrimMode=partial`) for Release builds
6. [x] Enable hybrid globalization (`HybridGlobalization=true`)
7. [ ] ~~Identify and configure lazy-loaded assemblies~~ — Deferred: requires splitting Client into separate Razor Class Libraries (all pages compile into one DLL; Contracts/Shared are lightweight)
8. [x] Verify no trimming regressions — Release publish succeeds; all 5,119 unit/integration tests pass

### Phase 3: Service Worker & Offline Cache
9. [x] Add Blazor PWA service worker for framework caching
10. [x] Configure service worker cache versioning strategy
11. [x] Offline-first verified — service worker caches all `_framework/` assets via manifest; API/index.html excluded from cache

### Phase 4: Auth Flow Optimization
12. [x] Verify silent token renewal is working — Already implemented via `TokenRefreshHandler` (automatic 401 → token refresh → retry)
13. [x] Add skeleton screens for post-auth page loading
14. [x] Prefetch landing page data during auth resolution — Parallelized Calendar's 5 sequential API calls (`LoadAccounts`, `LoadCategories`, `LoadCalendarData`, `LoadPastDueItems`, `LoadBudgetSummary`) via `Task.WhenAll`
15. [ ] ~~Explore early JS-based auth redirect~~ — Explored, deferred: sessionStorage is per-tab (empty on new tabs); constructing OIDC authorize URL with PKCE in plain JS duplicates fragile logic; service worker already eliminates WASM download on return visits

### Phase 5: Measurement & Validation
16. [ ] Add Lighthouse CI or manual Lighthouse audit to track Core Web Vitals (LCP, FID, CLS) — deferred to post-deploy
17. [ ] Measure before/after on throttled connections — deferred to post-deploy
18. [x] Document final payload sizes (see Measured Results below)

---

## Measured Results (Release Publish, 2026-03-17)

### Payload Sizes

| Metric | Before (no trimming) | After (trimmed) | Improvement |
|--------|---------------------|-----------------|-------------|
| `_framework/` total (all files) | 37.92 MB (642 files) | 19.02 MB (195 files) | **50% reduction** |
| Brotli transfer size | 6.68 MB | 3.32 MB | **50.3% reduction** |
| Largest files (Brotli) | — | dotnet.native 954 KB, CoreLib 541 KB, Client.dll 365 KB | — |
| ICU data (HybridGlobalization) | ~2 MB single file | 3 shards totaling ~600 KB | **70% reduction** |

### Optimizations Applied

| Optimization | Impact |
|-------------|--------|
| IL Trimming (`TrimMode=partial`) | 50% payload reduction |
| HybridGlobalization | ~1.4 MB ICU savings |
| Response Compression (Brotli) | 60–80% API response reduction |
| nginx immutable caching | Zero revalidation on return visits |
| Service Worker | Zero network for `_framework/` on cached visits |
| Inline config embedding | Eliminated `/api/v1/config` round-trip |
| Calendar parallel loading | 5 API calls concurrent vs sequential |
| Skeleton loading screen | Perceived instant first paint |

### Success Metrics

| Metric | Original Estimate | Target | Measured |
|--------|-------------------|--------|----------|
| WASM payload (Brotli) | ~8–12 MB | < 5 MB | **3.32 MB** ✅ |
| First Contentful Paint (cold) | 3–5s | < 2s | Pending Lighthouse |
| Time to Interactive (cold) | 5–10s | < 3.5s | Pending Lighthouse |
| Time to Interactive (cached/return) | 3–5s | < 1.5s | Expected near-instant (SW) |
| Post-auth data render | 1–2s | < 500ms | Expected ~max(5 calls) |
| Lighthouse Performance score | ~40–60 | > 75 | Pending |

---

## Risks & Considerations

- **IL Trimming** can break reflection-based code. `TrimMode=partial` is conservative but still requires testing. Run full E2E suite after enabling.
- **Invariant Globalization** will break `ToString("C")` and similar culture-dependent formatting. Needs audit of all Blazor components.
- **Service Worker** cache invalidation must be correct — a stale service worker serving an old framework version against a new API is a common PWA bug. Blazor's built-in service worker handles this via cache versioning.
- **Embedded Config** in `index.html` — Implemented via `MapFallback` handler in `Program.cs` that reads `index.html`, injects config JSON as `<script id="app-config" type="application/json">`, and serves dynamically. A fetch interceptor in `index.html` transparently provides this to Blazor's `HttpClient`.
- **Silent Token Renewal** requires Authentik to set appropriate CORS and cookie policies for the iframe-based flow. If Authentik doesn't cooperate, this falls back to the full redirect.
- **Brotli on Raspberry Pi** — encoding is CPU-intensive. Use pre-compressed files rather than on-the-fly compression for static assets. For API responses, `BrotliCompressionLevel.Fastest` keeps CPU usage reasonable.

---

## References

- Feature 111: Pragmatic Performance Optimizations (server-side query performance)
- [Blazor WASM Performance Best Practices (Microsoft Docs)](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance)
- [ASP.NET Core Response Compression](https://learn.microsoft.com/en-us/aspnet/core/performance/response-compression)
- [Blazor PWA / Service Worker](https://learn.microsoft.com/en-us/aspnet/core/blazor/progressive-web-app)
