# Feature 061: Restore Dismissed Category Suggestions
> **Status:** 🔜 Planned · **Priority:** Low · **Estimate:** 5–8 hours

## Problem

Users can dismiss category suggestions, but there is no way to view or undo a dismissal. An accidental dismiss is unrecoverable without direct database access.

## Goal

Three capabilities, delivered as independent vertical slices that each ship end-to-end (domain → infrastructure → service → API → client → tests):

1. **View** dismissed suggestions
2. **Restore** a dismissed suggestion to Pending
3. **Clear** all dismissed-pattern memory so re-analysis can re-suggest

---

## Existing Code Inventory

| Layer | What exists today | File |
|-------|-------------------|------|
| Domain entity | `CategorySuggestion` with `Accept()`, `Dismiss()` — no `Restore()` | `Domain/Categorization/CategorySuggestion.cs` |
| Domain enum | `SuggestionStatus { Pending, Accepted, Dismissed }` | `Domain/Categorization/SuggestionStatus.cs` |
| Domain entity | `DismissedSuggestionPattern` (pattern + ownerId + timestamp) | `Domain/Categorization/DismissedSuggestionPattern.cs` |
| Repo interface | `ICategorySuggestionRepository` — has `GetByStatusAsync` but no dedicated dismissed helper | `Domain/Repositories/ICategorySuggestionRepository.cs` |
| Repo interface | `IDismissedSuggestionPatternRepository` — has `GetByOwnerAsync`, no bulk-clear | `Domain/Repositories/IDismissedSuggestionPatternRepository.cs` |
| Service | `ICategorySuggestionService` / `CategorySuggestionService` — no restore/dismissed methods | `Application/Categorization/` |
| API | `CategorySuggestionsController` — no dismissed/restore/clear endpoints | `Api/Controllers/CategorySuggestionsController.cs` |
| Client | `CategorySuggestions.razor` — pending-only view, no dismissed tab | `Client/Pages/CategorySuggestions.razor` |
| DTOs | `CategorySuggestionDto` already contains `Status` field | `Contracts/Dtos/CategorySuggestionDtos.cs` |

---

## Slice 1 — View Dismissed Suggestions

> Smallest useful increment: the user can see what they've dismissed.

### 1.1 Domain (no changes needed)

`GetByStatusAsync(ownerId, SuggestionStatus.Dismissed, skip, take)` already exists on `ICategorySuggestionRepository`. No new domain code required.

### 1.2 Application Service

| Change | Detail |
|--------|--------|
| `ICategorySuggestionService` | Add `GetDismissedSuggestionsAsync(int skip, int take)` |
| `CategorySuggestionService` | Implement: delegate to `_suggestionRepo.GetByStatusAsync(ownerId, Dismissed, skip, take)` |

### 1.3 API

| Method | Route | Status | Response |
|--------|-------|--------|----------|
| GET | `/api/v1/categorysuggestions/dismissed` | 200 | `List<CategorySuggestionDto>` |

Returns dismissed suggestions for the authenticated user, paginated via `?skip=0&take=20`.

### 1.4 Client

- Add a "Dismissed" tab/toggle to `CategorySuggestions.razor`.
- Reuse `CategorySuggestionCard.razor` with muted styling and **no** Accept/Dismiss buttons (restore comes in Slice 2).
- Add `GetDismissedAsync(skip, take)` to `ICategorySuggestionApiService` / implementation.

### 1.5 TDD Tasks

| # | Layer | Test (RED → GREEN → REFACTOR) |
|---|-------|-------------------------------|
| 1 | Application | `GetDismissedSuggestionsAsync_ReturnsDismissedSuggestions` — service delegates to repo with correct status filter |
| 2 | Application | `GetDismissedSuggestionsAsync_ReturnsEmpty_WhenNoDismissed` |
| 3 | API | `GET /dismissed` returns 200 with dismissed list |
| 4 | API | `GET /dismissed` returns empty list when none |
| 5 | Client (bUnit) | Dismissed tab renders dismissed suggestions with muted styling (optional) |

### 1.6 Definition of Done

- [ ] GET endpoint returns dismissed suggestions for current user
- [ ] Client shows "Dismissed" tab with dismissed suggestions listed
- [ ] Visual distinction from pending (muted/opacity)
- [ ] All tests green

---

## Slice 2 — Restore a Dismissed Suggestion

> Depends on Slice 1 (user must see dismissed suggestions to restore one).

### 2.1 Domain

Add `Restore()` method to `CategorySuggestion`:

```csharp
public void Restore()
{
    if (Status != SuggestionStatus.Dismissed)
    {
        throw new DomainException($"Only dismissed suggestions can be restored. Current status: {Status}");
    }

    Status = SuggestionStatus.Pending;
}
```

### 2.2 Application Service

| Change | Detail |
|--------|--------|
| `ICategorySuggestionService` | Add `RestoreSuggestionAsync(Guid id)` |
| `CategorySuggestionService` | Implement: load suggestion → verify ownership → call `Restore()` → remove matching `DismissedSuggestionPattern` entries for the suggestion's merchant patterns → save |

Removing the dismissed patterns on restore is critical — otherwise the next analysis run would skip these patterns again and the suggestion would never reappear after a re-analysis.

### 2.3 API

| Method | Route | Status | Response |
|--------|-------|--------|----------|
| POST | `/api/v1/categorysuggestions/{id}/restore` | 200 | `CategorySuggestionDto` (restored, status = Pending) |
| | | 404 | Suggestion not found |
| | | 409 | Not in Dismissed status |

### 2.4 Client

- Add "Restore" button to dismissed suggestion cards (visible only when viewing the Dismissed tab).
- On success: remove from dismissed list, show toast/notification.
- Add `RestoreAsync(Guid id)` to `ICategorySuggestionApiService` / implementation.

### 2.5 TDD Tasks

| # | Layer | Test (RED → GREEN → REFACTOR) |
|---|-------|-------------------------------|
| 1 | Domain | `Restore_FromDismissed_SetsPending` |
| 2 | Domain | `Restore_FromPending_ThrowsDomainException` |
| 3 | Domain | `Restore_FromAccepted_ThrowsDomainException` |
| 4 | Application | `RestoreSuggestionAsync_RestoresSuggestion_AndRemovesDismissedPatterns` |
| 5 | Application | `RestoreSuggestionAsync_ThrowsNotFound_WhenIdInvalid` |
| 6 | Application | `RestoreSuggestionAsync_ThrowsNotFound_WhenOwnedByDifferentUser` |
| 7 | API | `POST /{id}/restore` returns 200 with updated suggestion |
| 8 | API | `POST /{id}/restore` returns 404 for unknown id |
| 9 | Client (bUnit) | Restore button visible on dismissed cards, calls API on click (optional) |

### 2.6 Definition of Done

- [ ] `Restore()` enforces Dismissed → Pending transition only
- [ ] Service removes matching `DismissedSuggestionPattern` entries on restore
- [ ] POST endpoint returns restored suggestion with Pending status
- [ ] Client "Restore" button works on dismissed tab
- [ ] Restored suggestion reappears in pending list
- [ ] All tests green

---

## Slice 3 — Clear All Dismissed Patterns

> Independent of Slice 2. Can be built after Slice 1 or in parallel with Slice 2.

### 3.1 Domain / Repository

Add to `IDismissedSuggestionPatternRepository`:

```csharp
Task<int> ClearByOwnerAsync(string ownerId, CancellationToken cancellationToken = default);
```

Implement in `DismissedSuggestionPatternRepository` — bulk delete all patterns for the owner. Returns count of deleted patterns.

### 3.2 Application Service

| Change | Detail |
|--------|--------|
| `ICategorySuggestionService` | Add `ClearDismissedPatternsAsync()` returning `int` (count cleared) |
| `CategorySuggestionService` | Implement: delegate to `_dismissedPatternRepo.ClearByOwnerAsync(ownerId)` + save |

Note: this does **not** change the status of existing dismissed `CategorySuggestion` entities. It only clears the pattern memory so that *future* analysis runs can re-suggest those categories. Existing dismissed suggestions remain dismissed.

### 3.3 API

| Method | Route | Status | Response |
|--------|-------|--------|----------|
| DELETE | `/api/v1/categorysuggestions/dismissed-patterns` | 200 | `{ "clearedCount": 5 }` |

### 3.4 Client

- Add "Clear Dismissed History" button below the dismissed tab (visible when dismissed list is non-empty).
- Confirmation dialog before clearing.
- On success: show count of cleared patterns, optionally prompt re-analysis.
- Add `ClearDismissedPatternsAsync()` to `ICategorySuggestionApiService` / implementation.

### 3.5 TDD Tasks

| # | Layer | Test (RED → GREEN → REFACTOR) |
|---|-------|-------------------------------|
| 1 | Infrastructure | `ClearByOwnerAsync_DeletesAllPatternsForOwner` |
| 2 | Infrastructure | `ClearByOwnerAsync_DoesNotDeleteOtherOwnerPatterns` |
| 3 | Infrastructure | `ClearByOwnerAsync_ReturnsZero_WhenNoneExist` |
| 4 | Application | `ClearDismissedPatternsAsync_DelegatesToRepo_ReturnsCount` |
| 5 | API | `DELETE /dismissed-patterns` returns 200 with cleared count |
| 6 | Client (bUnit) | Clear button shows confirmation dialog, calls API on confirm (optional) |

### 3.6 Definition of Done

- [ ] Bulk delete removes dismissed patterns only for current user
- [ ] Existing dismissed suggestions are NOT modified (status stays Dismissed)
- [ ] Re-analysis after clearing can re-suggest previously dismissed categories
- [ ] Confirmation dialog prevents accidental clearing
- [ ] All tests green

---

## Summary & Dependencies

```
Slice 1: View Dismissed ──► Slice 2: Restore Suggestion
                          │
                          └► Slice 3: Clear Patterns (independent of Slice 2)
```

| Slice | Layers touched | New tests | Estimate |
|-------|---------------|-----------|----------|
| 1 — View Dismissed | Service · API · Client | ~5 | 1.5–2 h |
| 2 — Restore Suggestion | Domain · Service · API · Client | ~9 | 2–3 h |
| 3 — Clear Patterns | Repo · Service · API · Client | ~6 | 1.5–2 h |
| **Total** | | **~20** | **5–7 h** |

Each slice is independently deployable and testable. Slice 1 ships first; Slices 2 and 3 can be built in parallel after that.

---

## References

- Parent Feature: [032-ai-category-suggestions.md](./archive/032-ai-category-suggestions.md) (archived)
- `DismissedSuggestionPattern` entity — `Domain/Categorization/DismissedSuggestionPattern.cs`
- `ICategorySuggestionRepository.GetByStatusAsync` — already supports querying by `Dismissed`

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-01 | Created as enhancement extracted from Feature 032 | @github-copilot |
| 2026-02-22 | Rewritten as vertical testable slices with TDD task breakdown | @github-copilot |
