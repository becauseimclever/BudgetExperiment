# Navigation, AI Categories & Reconciliation (031-040) - Consolidated Summary

**Consolidated:** 2026-02-01  
**Original Features:** 031 through 040  
**Status:** All Completed (with 036 On Hold, 037 Cancelled)

---

## Overview

This document consolidates features 031-040 which focused on improving navigation UX, implementing AI-powered category suggestions, fixing critical bugs in CSV import and API routing, and enhancing the reconciliation system with manual linking capabilities and bulk transaction categorization.

---

## 031: Navigation Reorganization & UX Improvements

**Completed:** January 2026

Comprehensive navigation overhaul improving sidebar usability and consistency.

**Key Outcomes:**
- Fixed sidebar stays visible during content scrolling
- Collapsible accounts and reports sections with state persistence
- Collapsible sidebar to icon-only mode with localStorage persistence
- Tooltips for icon-only navigation mode
- Improved link naming consistency throughout the app
- AI Settings merged into main Settings page as a tab

---

## 032: AI-Powered Category Suggestions

**Completed:** January 2026

AI-driven analysis of uncategorized transactions to suggest new budget categories.

**Key Outcomes:**
- `CategorySuggestionService` analyzes transaction patterns via Ollama
- `MerchantKnowledgeBase` maps common merchants to category types
- Auto-creation of categorization rules when suggestions are accepted
- System learns from manual categorizations
- Bulk accept/dismiss suggestions
- API endpoints: `/api/v1/category-suggestions/*` and `/api/v1/merchantmappings/*`
- Full UI at `/category-suggestions` with suggestion cards

**Future Enhancement:** See [032.1-category-suggestion-restore.md](../032.1-category-suggestion-restore.md) for planned restore/undismiss functionality.

---

## 033: CSV Import Skip Header Rows Bug Fix

**Completed:** January 2026  
**Type:** Bug Fix

Fixed CSV import to properly handle bank exports with metadata rows before the actual header.

**Key Outcomes:**
- Parse endpoint accepts `rowsToSkip` query parameter
- Re-parsing triggered when skip rows value changes in mapping step
- Label changed from "Skip Rows After Header" to "Rows Before Header"
- Validation prevents skip count from exceeding total rows
- Saved mappings store skip rows setting for future imports

---

## 034: Category Suggestions Analyze 405 Fix

**Completed:** January 2026  
**Type:** Bug Fix

Fixed 405 Method Not Allowed error when clicking "Analyze Transactions".

**Key Outcomes:**
- Root cause: Route mismatch (client used kebab-case, controller used PascalCase)
- Changed controller route from `[Route("api/v1/[controller]")]` to `[Route("api/v1/category-suggestions")]`
- Updated all test URLs to use consistent kebab-case routing

---

## 035: AI Suggestions Analyze Timeout Bug

**Completed:** January 2026  
**Type:** Bug Fix

Fixed timeout issues when running AI analysis on large transaction sets.

**Key Outcomes:**
- Improved timeout handling in `OllamaAiService`
- Better error propagation when Ollama times out
- Graceful error messages instead of connection refused errors
- User-friendly timeout notifications in UI

---

## 036: Demo Environment E2E Tests

**Status:** ⏸️ On Hold  
**Reason:** Authentication issues - tests disabled until auth is fixed

Playwright E2E test suite targeting `budgetdemo.becauseimclever.com`.

**Key Outcomes (Partial):**
- `BudgetExperiment.E2E.Tests` project created
- Test fixture with environment configuration
- Authentication helper for Authentik OAuth flow
- Basic smoke and navigation tests written
- Tests currently disabled pending auth fixes

---

## 037: Automated Demo Site Deployment

**Status:** ❌ Cancelled  
**Cancelled:** 2026-02-01  
**Reason:** Will revisit when ready to approach again

Planned automated deployment to demo environment on release.

**Deferred Features:**
- SSH-based deployment from GitHub Actions
- Automatic health verification
- Deployment status visibility in GitHub

---

## 038: Reconciliation Status Endpoint Parameter Mismatch Bug

**Completed:** January 2026  
**Type:** Bug Fix

Fixed Reconciliation page failing to load due to query parameter mismatch.

**Key Outcomes:**
- Client was sending `startDate`/`endDate`, API expected `year`/`month`
- Updated `IReconciliationApiService.GetStatusAsync()` signature
- Client now sends `?year=X&month=Y` matching API contract

---

## 039: Manual Reconciliation Linking

**Completed:** January 2026

Allow users to manually link transactions to recurring items when auto-matching fails.

**Key Outcomes:**
- `MatchSource` enum: `Auto`, `Manual` added to domain
- `CreateManualLink()` and `Unlink()` methods on `ReconciliationMatch`
- `ImportPatterns` collection on `RecurringTransaction` for custom matching
- API endpoints: POST `/link`, DELETE `/matches/{id}`, GET `/linkable-instances`
- Import patterns management endpoints
- "Link Manually" and "Unlink" buttons in Reconciliation UI
- "Manual" badge displayed on manually linked matches
- "Remember this description" option learns patterns from manual links

---

## 040: Bulk Categorize Uncategorized Transactions

**Completed:** January 2026

Dedicated page for viewing and bulk-categorizing uncategorized transactions.

**Key Outcomes:**
- New `/uncategorized` page listing all uncategorized transactions
- Server-side paging (50 per page default)
- Filtering by date range, amount range, description, and account
- Multi-select with "Select All on Page" functionality
- Bulk category assignment in one action
- Clear success/error feedback with update counts
- Leverages existing `GetUncategorizedAsync()` repository method

---

## API Endpoints Added

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/v1/category-suggestions/analyze` | POST | Analyze transactions for category suggestions |
| `/api/v1/category-suggestions` | GET | Get all pending suggestions |
| `/api/v1/category-suggestions/{id}/accept` | POST | Accept a suggestion |
| `/api/v1/category-suggestions/{id}/dismiss` | POST | Dismiss a suggestion |
| `/api/v1/category-suggestions/bulk-accept` | POST | Accept multiple suggestions |
| `/api/v1/category-suggestions/{id}/preview-rules` | GET | Preview rules for suggestion |
| `/api/v1/category-suggestions/{id}/create-rules` | POST | Create rules from suggestion |
| `/api/v1/merchantmappings` | GET | Get all merchant mappings |
| `/api/v1/merchantmappings/learn` | POST | Learn merchant-category mapping |
| `/api/v1/merchantmappings/{id}` | DELETE | Delete merchant mapping |
| `/api/v1/reconciliation/link` | POST | Manually link transaction |
| `/api/v1/reconciliation/matches/{matchId}` | DELETE | Unlink/reject match |
| `/api/v1/reconciliation/linkable-instances` | GET | Get linkable recurring instances |
| `/api/v1/recurring-transactions/{id}/import-patterns` | GET/PUT | Manage import patterns |
| `/api/v1/uncategorized` | GET | Get uncategorized transactions (paged, filtered) |
| `/api/v1/transactions/bulk-categorize` | POST | Bulk assign category |

---

## Domain Model Changes

### New Entities
- `CategorySuggestion` - AI-generated category recommendations
- `MerchantMapping` - Learned merchant-to-category associations
- `RecurringTransactionImportPattern` - Custom matching patterns

### Entity Enhancements
- `ReconciliationMatch` - Added `MatchSource` property (Auto/Manual)
- `RecurringTransaction` - Added `ImportPatterns` collection
- `Transaction` - Added `UnlinkFromRecurring()` method

---

## UI/UX Improvements

- **Navigation:** Fixed sidebar, collapsible sections, icon-only mode with tooltips
- **Category Suggestions Page:** Card-based suggestion display with accept/dismiss actions
- **Reconciliation Page:** Manual link/unlink buttons, linkable instances dialog
- **Import Patterns Dialog:** Manage recurring item import patterns
- **Uncategorized Page:** Filterable, paginated list with bulk selection
- **Settings Page:** AI settings integrated as tab

---

## Testing Coverage

- Unit tests for `CategorySuggestionService`, `MerchantMappingService`
- Integration tests for all new API endpoints
- Controller tests for `CategorySuggestionsController`
- bUnit tests for navigation components
- Manual test checklists for reconciliation and import flows

---

## References

- [Feature 032.1](../032.1-category-suggestion-restore.md) *(Planned - Restore dismissed suggestions)*
