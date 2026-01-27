# Performance Optimization: Time to First Page & Page Load
> **Status:** 🗒️ Planning

## Feature Overview

This feature targets performance improvements for Budget Experiment, with a focus on reducing time to first page (TTFP) and overall page load times. The goal is to deliver a seamless, fast user experience, minimizing visible authentication flashes (Authentik) and ensuring all pages load quickly.

## Motivation

Users expect instant access to budgeting tools. Current performance issues include multiple flashes of the Authentik login and slow initial/page loads, especially on mobile or slow networks. Improving these areas will:
- Increase user satisfaction and engagement
- Reduce bounce rates
- Support accessibility and inclusivity for users on slower connections

## Goals

- Time to first page (TTFP) < 1 second (SLA)
- All subsequent page loads < 1 second (SLA)
- Eliminate or minimize visible Authentik flashes during authentication
- Comprehensive automated performance test suite (CI/CD)
- Monitor and alert on performance regressions

## Non-Goals

- Offline support (future feature)
- Major architectural rewrites (focus on targeted optimizations)

## User Stories

1. **As a user, I want the app to load the first page in under 1 second, so I can start budgeting immediately.**
2. **As a user, I want to avoid seeing repeated Authentik login flashes, so the experience feels seamless.**
3. **As a developer, I want automated performance tests to catch regressions before release.**

## Acceptance Criteria

- TTFP and all page loads are consistently < 1 second in production
- Authentik flashes are eliminated or reduced to a single, brief occurrence
- Automated performance tests run in CI/CD and fail builds on SLA violations
- Performance metrics are tracked and reported

## Implementation Notes

- Profile and optimize Blazor WASM and API startup
- Cache static assets aggressively (with proper cache busting)
- Optimize Authentik/OIDC flow to avoid unnecessary redirects or reloads
- Lazy-load non-critical resources
- Use browser and server-side performance monitoring tools
- Add Playwright or similar E2E performance tests
- Implement both a loading skeleton and a progress bar overlay for any significant delays (e.g., authentication, data fetches)

## Open Questions

- The initial public performance dashboard will be a static Markdown page in the repo, updated with each release. We will plan for a more dynamic dashboard in the future as needs evolve.

---

*Created: 2026-01-26*
*Author: @becauseimclever*
