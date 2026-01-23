# Feature 034: Category Suggestions Analyze Endpoint 405 Fix

> **Status:** ✅ Complete  
> **Type:** Bug Fix  
> **Severity:** High  
> **Started:** 2026-01-23  
> **Completed:** 2026-01-23

## Overview

Fix the "Analyze Transactions" button on the Category Suggestions page which returns a **405 Method Not Allowed** error when clicking "Analyze Transactions".

## Problem Statement

### Current Behavior (Bug)

When a user navigates to the Category Suggestions page (`/category-suggestions`) and clicks "Analyze Transactions":

1. **Client sends POST request** to `/api/v1/category-suggestions/analyze`
2. **Server responds with 405 Method Not Allowed**
3. **Response headers show**: `allow: GET, HEAD` (POST is not listed as allowed)
4. **Error displayed in UI**: "Failed to load suggestions: ExpectedStartOfValueNotFound, < Path: $ | LineNumber: 0 | BytePositionInLine: 0."

### Console Evidence

```
POST http://localhost:5099/api/v1/category-suggestions/analyze [405 Method Not Allowed]
Response Headers: allow: GET, HEAD
```

### API Controller Analysis

The `CategorySuggestionsController.cs` has the correct attribute:
```csharp
[HttpPost("analyze")]
[ProducesResponseType<IReadOnlyList<CategorySuggestionDto>>(StatusCodes.Status200OK)]
public async Task<IActionResult> AnalyzeAsync(CancellationToken cancellationToken)
```

However, the 405 response with `allow: GET, HEAD` suggests:
- The route `/api/v1/category-suggestions/analyze` may be conflicting with another route
- ASP.NET Core routing may not be matching the POST method to this action
- There could be a middleware or routing configuration issue

### Expected Behavior

1. User clicks "Analyze Transactions" on Category Suggestions page
2. POST request to `/api/v1/category-suggestions/analyze` succeeds with 200 OK
3. AI analyzes uncategorized transactions
4. Response contains list of category suggestions
5. UI displays suggestions for user to accept/dismiss

---

## Root Cause Investigation

### Root Cause Found: Route Mismatch

The issue was a **route path mismatch** between the client and controller:

| Component | Route Used |
|-----------|------------|
| Client (`CategorySuggestionApiService.cs`) | `api/v1/category-suggestions` (kebab-case) |
| Controller (`CategorySuggestionsController.cs`) | `api/v1/[controller]` → `api/v1/CategorySuggestions` (PascalCase) |

ASP.NET Core's `[Route("api/v1/[controller]")]` attribute replaces `[controller]` with the controller class name without the "Controller" suffix, resulting in `CategorySuggestions` (PascalCase, no hyphen).

The client was calling `api/v1/category-suggestions/analyze` (with hyphen), which didn't match any registered route. The request fell through to the static files middleware, which returned 405 Method Not Allowed because static files only support GET and HEAD methods.

### Why Other Controllers Work

Some controllers in the codebase explicitly define lowercase kebab-case routes:
- `RecurringTransactionsController` → `[Route("api/v1/recurring-transactions")]`
- `RecurringTransfersController` → `[Route("api/v1/recurring-transfers")]`
- `ReconciliationController` → `[Route("api/v1/reconciliation")]`

But `CategorySuggestionsController` used `[Route("api/v1/[controller]")]` which doesn't add hyphens.

---

## Fix Applied

Changed the route attribute in `CategorySuggestionsController.cs` from:
```csharp
[Route("api/v1/[controller]")]
```
To:
```csharp
[Route("api/v1/category-suggestions")]
```

Also updated all test URLs in `CategorySuggestionsControllerTests.cs` to use the kebab-case route.

---

## Technical Tasks Completed

### Phase 1: Diagnosis

- [x] Check OpenAPI/Scalar docs to verify endpoint is listed with POST method - **Confirmed endpoint was registered**
- [x] Check if the controller is being registered in DI/routing - **Yes, controller was registered**
- [x] Test the endpoint directly with browser - **Reproduced 405 error**
- [x] Check for conflicting routes in other controllers - **Found route naming pattern inconsistency**
- [x] Review Program.cs for routing configuration - **Middleware order was correct**
- [x] Check if static files middleware is configured before API routing - **Yes, static files returned 405 for unmatched route**

### Phase 2: Fix Implementation

- [x] Implement fix based on root cause - **Changed to explicit kebab-case route**
- [x] Update unit tests for the endpoint - **Updated all test URLs**
- [x] Verify fix works in browser - **POST now returns 200 OK**

### Phase 3: Regression Prevention

- [x] Add integration test that POST to `/analyze` returns 200 - **Test already existed, just needed URL fix**
- [x] Document the fix - **This document**

---

## User Stories

### US-034-001: Fix Analyze Endpoint Method Not Allowed
**As a** user  
**I want** the "Analyze Transactions" button to work  
**So that** I can get AI-powered category suggestions

**Acceptance Criteria:**
- [x] POST to `/api/v1/category-suggestions/analyze` returns 200 OK
- [x] Response contains valid JSON array of suggestions
- [x] UI displays suggestions without error
- [x] No 405 errors in browser console

---

## Files to Investigate

| File | Purpose |
|------|---------|
| `CategorySuggestionsController.cs` | API controller with `[HttpPost("analyze")]` |
| `Program.cs` | Routing and middleware configuration |
| `CategorySuggestionApiService.cs` | Client-side service calling the endpoint |
| `CategorySuggestions.razor` | UI page triggering the analyze action |

---

## Testing Strategy

### Manual Testing
1. Navigate to `/category-suggestions`
2. Click "Analyze Transactions"
3. Verify no 405 error
4. Verify suggestions are displayed (if AI is configured)

### Automated Testing
- Integration test: POST to `/api/v1/category-suggestions/analyze` returns 200
- Unit test: `CategorySuggestionsController.AnalyzeAsync` calls service correctly

---

## Related Features

- Feature 032: AI-Powered Category Suggestions (original implementation)
- Feature 025: AI Rule Suggestions

---

## Notes

The error message "ExpectedStartOfValueNotFound, < Path: $" indicates the client received HTML (starting with `<`) instead of JSON. This is consistent with a 405 response that may include an HTML error page or empty body.
