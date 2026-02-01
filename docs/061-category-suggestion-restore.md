# Feature 032.1: Restore Dismissed Category Suggestions
> **Status:** 🔜 Planned (Future Enhancement)

## Overview

Allow users to view and restore previously dismissed category suggestions. This is a minor enhancement to Feature 032 (AI-Powered Category Suggestions) that provides an "undo" capability for dismissed suggestions.

## Problem Statement

### Current State

- Users can dismiss category suggestions they don't want
- Dismissed suggestions are persisted and don't reappear on re-analysis
- **No way to view or restore dismissed suggestions**
- If a user accidentally dismisses a suggestion, they cannot recover it

### Target State

- Users can view a list of dismissed suggestions
- Users can restore individual dismissed suggestions back to "Pending" status
- Restored suggestions reappear in the main suggestions list
- Optional: Clear all dismissed suggestions to allow fresh re-analysis

---

## User Stories

### US-032.1-001: View Dismissed Suggestions
**As a** user  
**I want to** see a list of suggestions I've previously dismissed  
**So that** I can review my decisions and potentially restore useful ones

**Acceptance Criteria:**
- [ ] "Show Dismissed" toggle or tab on Category Suggestions page
- [ ] Dismissed suggestions displayed with original details (name, patterns, confidence)
- [ ] Visual distinction from pending suggestions (e.g., muted styling)
- [ ] Shows when the suggestion was dismissed

### US-032.1-002: Restore Dismissed Suggestion
**As a** user  
**I want to** restore a dismissed suggestion  
**So that** I can reconsider a suggestion I previously rejected

**Acceptance Criteria:**
- [ ] "Restore" button on each dismissed suggestion
- [ ] Restoring changes status back to "Pending"
- [ ] Restored suggestion appears in main suggestions list
- [ ] Success feedback confirms restoration

### US-032.1-003: Clear All Dismissed
**As a** user  
**I want to** clear all dismissed suggestion patterns  
**So that** re-analysis can suggest previously dismissed categories

**Acceptance Criteria:**
- [ ] "Clear Dismissed History" action in settings or suggestions page
- [ ] Confirmation dialog before clearing
- [ ] After clearing, re-analysis may suggest previously dismissed categories
- [ ] This does not restore suggestions, just clears the dismissal memory

---

## Technical Design

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/category-suggestions/dismissed` | Get all dismissed suggestions |
| POST | `/api/v1/category-suggestions/{id}/restore` | Restore a dismissed suggestion |
| DELETE | `/api/v1/category-suggestions/dismissed` | Clear all dismissed patterns |

### Domain Changes

- Add `Restore()` method to `CategorySuggestion` entity
- Add `GetDismissedAsync()` to `ICategorySuggestionRepository`
- Add `ClearDismissedPatternsAsync()` to `IDismissedSuggestionPatternRepository`

### Service Changes

- Add `GetDismissedSuggestionsAsync()` to `ICategorySuggestionService`
- Add `RestoreSuggestionAsync()` to `ICategorySuggestionService`
- Add `ClearDismissedPatternsAsync()` to `ICategorySuggestionService`

### UI Changes

- Add "Dismissed" tab or toggle on Category Suggestions page
- Add "Restore" button to dismissed suggestion cards
- Add "Clear Dismissed History" option

---

## Implementation Plan

### Phase 1: Domain & Repository
- [ ] Add `Restore()` method to `CategorySuggestion`
- [ ] Add `GetDismissedAsync()` to repository interface and implementation
- [ ] Add `ClearAllAsync()` to `IDismissedSuggestionPatternRepository`
- [ ] Write unit tests

### Phase 2: Application Service
- [ ] Add new methods to `ICategorySuggestionService`
- [ ] Implement in `CategorySuggestionService`
- [ ] Write service tests

### Phase 3: API
- [ ] Add new endpoints to `CategorySuggestionsController`
- [ ] Add DTOs if needed
- [ ] Write API tests

### Phase 4: Client UI
- [ ] Add dismissed suggestions view
- [ ] Add restore functionality
- [ ] Add clear dismissed option
- [ ] Write bUnit tests

---

## Effort Estimate

| Phase | Estimate |
|-------|----------|
| Domain & Repository | 1-2 hours |
| Application Service | 1 hour |
| API | 1-2 hours |
| Client UI | 2-3 hours |
| **Total** | **5-8 hours** |

---

## Priority

**Low** - This is a convenience feature. The workaround is to clear dismissed patterns via direct database access or wait for the pattern memory to naturally not apply.

---

## References

- Parent Feature: [032-ai-category-suggestions.md](./archive/032-ai-category-suggestions.md)
- Related: `DismissedSuggestionPattern` entity
- Related: `IDismissedSuggestionPatternRepository`

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-01 | Created as enhancement extracted from Feature 032 | @github-copilot |
