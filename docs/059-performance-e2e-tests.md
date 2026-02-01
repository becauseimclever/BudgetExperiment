# Feature 059: Performance E2E Tests with Core Web Vitals
> **Status:** In Progress (Phase 1-3 Complete)
> **Priority:** Medium
> **Deferred From:** Feature 052

## Overview

This feature adds automated performance testing to the E2E test suite using Playwright. The tests will capture Core Web Vitals (FCP, LCP, TTI, CLS), verify zero-flash authentication, and enforce performance thresholds in CI/CD to catch regressions before they reach production.

## Problem Statement

### Current State

- No automated performance testing exists
- Performance regressions can slip into production unnoticed
- Core Web Vitals are not measured or tracked
- Zero-flash auth implementation (Feature 052) has no automated verification

### Target State

- Playwright tests capture Core Web Vitals on every CI run
- Performance thresholds enforce minimum standards
- Zero-flash auth is verified automatically
- Performance trends are tracked over releases

---

## User Stories

### US-059-001: Automated Zero-Flash Verification
**As a** developer  
**I want to** have automated tests that verify zero auth flashes  
**So that** I don't accidentally regress the user experience

**Acceptance Criteria:**
- [x] Playwright test navigates to app and captures all visible text during load
- [x] Test fails if "Checking authentication", "Loading...", or "Redirecting to login" appears
- [x] Test passes for both authenticated and unauthenticated flows

### US-059-002: Core Web Vitals Capture
**As a** developer  
**I want to** capture FCP, LCP, TTI, and CLS metrics  
**So that** I can track performance over time

**Acceptance Criteria:**
- [x] Playwright tests capture First Contentful Paint (FCP)
- [x] Playwright tests capture Largest Contentful Paint (LCP)
- [x] Playwright tests capture Time to Interactive (TTI)
- [x] Playwright tests capture Cumulative Layout Shift (CLS)
- [x] Metrics are logged to CI output

### US-059-003: Performance Threshold Enforcement
**As a** maintainer  
**I want to** CI builds to fail when performance degrades  
**So that** regressions are caught before release

**Acceptance Criteria:**
- [x] FCP threshold: < 1.5s (warning at 1.0s)
- [x] LCP threshold: < 2.5s (warning at 2.0s)
- [x] TTI threshold: < 3.0s (warning at 2.5s)
- [x] CLS threshold: < 0.1 (warning at 0.05)
- [x] Tests fail if thresholds exceeded

---

## Technical Design

### Test Architecture

```
tests/BudgetExperiment.E2E.Tests/
├── Tests/
│   ├── PerformanceTests.cs      # Core Web Vitals tests
│   └── ZeroFlashAuthTests.cs    # Auth flash verification
├── Helpers/
│   └── PerformanceHelper.cs     # Web Vitals capture utilities
└── Fixtures/
    └── PlaywrightFixture.cs     # (existing) add performance context
```

### Performance Metrics Capture

Using Playwright's built-in performance APIs:

```csharp
public class PerformanceHelper
{
    public static async Task<PerformanceMetrics> CaptureMetricsAsync(IPage page)
    {
        // Use Performance Observer API
        var metrics = await page.EvaluateAsync<PerformanceMetrics>(@"() => {
            const nav = performance.getEntriesByType('navigation')[0];
            const paint = performance.getEntriesByType('paint');
            const fcp = paint.find(p => p.name === 'first-contentful-paint');
            
            return {
                fcp: fcp?.startTime ?? 0,
                domContentLoaded: nav?.domContentLoadedEventEnd ?? 0,
                loadComplete: nav?.loadEventEnd ?? 0
            };
        }");
        
        return metrics;
    }
}
```

### Zero-Flash Verification

```csharp
[Fact]
public async Task Auth_ShouldNotShowFlashMessages_WhenLoading()
{
    var page = fixture.Page;
    var flashMessages = new List<string>();
    
    // Capture all text that appears during load
    page.Console += (_, msg) => { /* log console */ };
    
    await page.GotoAsync(fixture.BaseUrl);
    
    // Check for forbidden flash messages
    var forbiddenTexts = new[] {
        "Checking authentication",
        "Redirecting to login",
        "Loading..."
    };
    
    foreach (var text in forbiddenTexts)
    {
        var visible = await page.GetByText(text).IsVisibleAsync();
        Assert.False(visible, $"Flash message detected: '{text}'");
    }
}
```

---

## Implementation Plan

### Phase 1: Performance Helper Utilities

**Objective:** Create utilities for capturing Core Web Vitals

**Tasks:**
- [x] Create `PerformanceHelper.cs` with metrics capture methods
- [x] Create `PerformanceMetrics` record type
- [x] Add threshold constants (`PerformanceThresholds.cs`)
- [ ] Write unit tests for threshold logic

### Phase 2: Zero-Flash Auth Tests

**Objective:** Verify no auth flash messages appear

**Tasks:**
- [x] Create `ZeroFlashAuthTests.cs`
- [x] Test unauthenticated user flow (redirect to Authentik)
- [x] Test authenticated user flow (direct to content)
- [x] Verify branded loading overlay is visible during load

### Phase 3: Core Web Vitals Tests

**Objective:** Capture and validate Core Web Vitals

**Tasks:**
- [x] Create `PerformanceTests.cs`
- [x] Capture FCP, LCP, TTI, CLS
- [x] Assert against thresholds
- [x] Log metrics to console for CI visibility

### Phase 4: CI Integration

**Objective:** Run performance tests in GitHub Actions

**Tasks:**
- [ ] Update `.github/workflows/ci.yml` to include performance tests
- [ ] Add performance test environment variables
- [ ] Configure test to use demo environment
- [ ] Add performance summary to CI output

---

## Performance Thresholds

| Metric | Good | Warning | Fail |
|--------|------|---------|------|
| FCP | < 1.0s | 1.0-1.5s | > 1.5s |
| LCP | < 2.0s | 2.0-2.5s | > 2.5s |
| TTI | < 2.5s | 2.5-3.0s | > 3.0s |
| CLS | < 0.05 | 0.05-0.1 | > 0.1 |
| Auth Flashes | 0 | - | > 0 |

---

## Dependencies

- Feature 052 (Performance TTFP) - ✅ Complete
- E2E test infrastructure decisions
- Demo environment availability for CI

---

## Changelog

| Date | Author | Description |
|------|--------|-------------|
| 2026-02-01 | AI | Created feature doc (deferred from Feature 052) |
| 2026-02-01 | AI | Implemented Phase 1-3: PerformanceHelper, ZeroFlashAuthTests, PerformanceTests |
