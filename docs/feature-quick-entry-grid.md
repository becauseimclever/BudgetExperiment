# Feature: Quick Entry Grid for Transactions

Created: 2025-11-18  
Updated: 2025-11-18  
Status: Draft  
Priority: P0

## Overview
Provide a spreadsheet-like, keyboard-first grid to rapidly enter new transactions. Users can input many rows without leaving the keyboard, similar to Excel or Google Sheets. Rows post to the server as new `AdhocTransaction` items with clear per-row validation feedback.

## Business Value
- Speed: Enter dozens of transactions in minutes with no mouse.
- Accuracy: Immediate validation reduces mistakes and rework.
- Adoption: Familiar spreadsheet interaction lowers learning curve vs. traditional forms.

## Scope
- Client-only grid UI in Blazor WASM using FluentUI-Blazor (`FluentDataGrid`) with custom editing cells.
- New API bulk endpoint to create many adhoc transactions in one request.
- Application-layer bulk service to orchestrate validation, duplicate detection, and creation within a unit of work.

Out of scope (initial): multi-account selection, attachments, splits, reconciliations, advanced rules. These can be added iteratively.

## Data Model & Columns
Backed by `AdhocTransaction` domain. Grid columns (initial):
- Date (required): `DateOnly` (default today; accepts `MM/DD`, `MM/DD/YYYY`, natural shortcuts like `t` for today in later phase)
- Description (required): `string`
- Amount (required): `decimal` (typed as positive; sign normalized by Type)
- Type (required): `Income | Expense` (drives sign convention)
- Category (optional): `string` (free text; later: auto-suggest from history)
- Currency (optional): `string` (default `USD`)

Notes:
- Domain conventions enforce sign via `TransactionType` (Income => positive; Expense => negative). UI will accept any sign but normalize before submit.

## Keyboard Interaction (Phase 1 → Phase 2)
- Tab / Shift+Tab: Move to next/previous editable cell; wrap row → next/prev row.
- Enter: Commit cell; move down one row in same column.
- Shift+Enter: Commit cell; move up one row in same column.
- Ctrl+Enter: Commit current row; insert new blank row below and focus same column.
- Esc: Cancel edit, revert to previous value.
- Ctrl+;: Insert today’s date (Phase 2).
- Ctrl+D: Fill down value from cell above into selection (Phase 2).
- Paste: Multi-cell paste from clipboard (CSV/TSV/Excel) (Phase 2).

## Validation Rules
- Required: Date, Description, Amount, Type.
- Amount: finite decimal; no thousand separators required; parentheses or leading `-` allowed; sign normalized with Type before submit.
- Date: strict parse in Phase 1; error if invalid; Phase 2 may add flexible parsing (e.g., `11/5`, `t` for today).
- Category: optional; trimmed; empty → null.
- Currency: default `USD`; must be a non-empty uppercase ISO code when provided.

Client shows inline cell errors; row cannot submit until errors resolved. Server re-validates and returns RFC7807 ProblemDetails for per-row failures; UI maps to row/cell messages.

## Duplicate Detection
Use existing repository duplicate checks. Criteria: exact match on Date, trimmed case-insensitive Description, absolute Amount, and `TransactionType`.
- On bulk submit: service checks duplicates per row. Duplicates return with `409 Conflict`-like semantics in the bulk response (see API contract) and are not created.
- Future: optional preview that flags duplicates before submit.

## API Design
Base: `/api/v1/transactions`

- POST `/api/v1/transactions/bulk`
  - Request: `BulkCreateTransactionsRequest`
  - Response: `BulkCreateTransactionsResponse`
  - Status: `207 Multi-Status` on mixed success (preferred) or `200 OK` with per-item statuses.
  - Errors: Validation problems as `application/problem+json` at item level; top-level 400 only for malformed payload.

Request DTO:
```json
{
  "items": [
    {
      "date": "2025-11-18",
      "description": "GROCERY STORE #123",
      "amount": 45.67,
      "type": "Expense",
      "category": "Groceries",
      "currency": "USD"
    }
  ]
}
```

Response DTO (per-row status):
```json
{
  "results": [
    {
      "index": 0,
      "status": "Created",          // Created | Duplicate | ValidationError | Failed
      "id": "00000000-0000-0000-0000-000000000001",
      "error": null,
      "duplicateOf": null
    },
    {
      "index": 1,
      "status": "Duplicate",
      "id": null,
      "error": null,
      "duplicateOf": "00000000-0000-0000-0000-0000000000AB"
    },
    {
      "index": 2,
      "status": "ValidationError",
      "id": null,
      "error": {
        "type": "https://datatracker.ietf.org/doc/html/rfc7807",
        "title": "One or more validation errors occurred.",
        "status": 422,
        "errors": {"amount": ["Amount must be greater than 0."]}
      },
      "duplicateOf": null
    }
  ]
}
```

Notes:
- Use optimistic happy-path `201 Created` if all rows succeed; otherwise `207 Multi-Status`.
- OpenAPI includes examples and schema for both DTOs; tag under `Transactions`.

## Application Layer
Add a dedicated bulk service to orchestrate in a single unit of work where appropriate.

- Interface: `IAdhocTransactionBulkService`
  - `Task<BulkCreateTransactionsResult> CreateManyAsync(IReadOnlyList<BulkCreateTransactionItem> items, CancellationToken ct)`
- Behavior:
  - Validate each item (date, description, amount > 0, type, currency)
  - Normalize amount sign per type
  - Duplicate check via `IAdhocTransactionReadRepository.FindDuplicatesAsync`
  - Create with `IAdhocTransactionWriteRepository.AddAsync`
  - Commit batched with `IUnitOfWork.SaveChangesAsync`
  - Return per-item statuses; never throw for individual row errors—aggregate into result instead

Models (Application):
```csharp
public sealed record BulkCreateTransactionItem(DateOnly Date, string Description, decimal Amount, TransactionType Type, string? Category, string Currency);
public enum BulkRowStatus { Created, Duplicate, ValidationError, Failed }
public sealed record BulkCreateRowResult(int Index, BulkRowStatus Status, Guid? Id, ProblemDetails? Error, Guid? DuplicateOf);
public sealed record BulkCreateTransactionsResult(IReadOnlyList<BulkCreateRowResult> Results);
```

Unit tests cover:
- Mixed success/failure rows
- Sign normalization rules
- Duplicate detection pathways
- Transactional behavior (exceptions do not block other rows; commit created rows)

## API Layer (Controller)
- `TransactionsController` under `BudgetExperiment.Api/Controllers`
  - `POST /api/v1/transactions/bulk` → maps to bulk service; returns `207` or `201`.
- ProblemDetails middleware maps unexpected exceptions to `500` and includes `traceId`.
- OpenAPI + Scalar documented with examples.

## Client (Blazor) UI
Route: `/transactions/quick-entry` (nav: “Quick Entry”).

Components:
- `Pages/Transactions/QuickEntry.razor` — page shell, toolbar, submit button, summary bar.
- `Components/Transactions/QuickEntryGrid.razor` — editable grid component with:
  - Row model: mirrors request item
  - Validation state per cell
  - Keyboard navigation handlers
  - Add row on last-cell Enter
  - Status pill per row after submit (Created/Duplicate/Error)

Toolbar:
- Default Date (sets initial for new rows)
- Default Currency (USD)
- Quick actions: Add 10 rows, Clear all, Submit

Behavior:
- Submit sends only valid rows; invalid rows remain for correction.
- Mixed results show inline; duplicates flagged with tooltip linking to existing item (future).
- Persist draft in local storage to protect from accidental reload (Phase 2/3).

## Vertical Slice Delivery

VS1 – MVP Quick Entry (Row-by-Row, End-to-End):
- UI: Quick Entry page with `FluentDataGrid` inline editing for Date, Description, Amount, Type, Category, Currency (USD default).
- Keyboard: Tab/Shift+Tab, Enter/Shift+Enter, Esc; add-new-row on last-cell Enter.
- API: Reuse existing per-transaction create path via controller endpoints (Income/Expense) or minimal new endpoints mapping to `IAdhocTransactionService`.
- Application: Use `IAdhocTransactionService.CreateIncomeAsync/CreateExpenseAsync` sequentially per row; enforce sign normalization and duplicate checks already in service.
- Behavior: Submit selected/valid rows; per-row status (Created/Duplicate/ValidationError) surfaced inline.
- Tests: bUnit focus/order + submit wiring; API happy/failure paths; application mapping tests.
- Docs: OpenAPI examples for create endpoints; Scalar visible.

VS2 – Bulk Submission Endpoint (Batch, Per-Row Status):
- UI: Submit all valid rows in one request; per-row result mapping (Created/Duplicate/Error).
- API: `POST /api/v1/transactions/bulk` returning `201` on all-created or `207 Multi-Status` on mixed results.
- Application: `IAdhocTransactionBulkService.CreateManyAsync` validates, dedupes, creates, commits; returns per-row results; never throws for per-row failures.
- Tests: Application mixed-results tests; API multi-status tests; client per-row rendering.

VS3 – Keyboard & Paste Enhancements:
- Shortcuts: Ctrl+Enter (insert row), Ctrl+; (today), Ctrl+D (fill down), flexible date parsing (MM/DD, `t`).
- Clipboard: Multi-cell paste (CSV/TSV/Excel) with range mapping and validation.
- Persistence: Local storage draft autosave/restore.
- Tests: bUnit for shortcuts and paste; parsing utilities unit tests.

VS4 – Duplicate Preview & Undo:
- Preview: Client checks duplicates pre-submit (read repo via API) and highlights duplicate rows.
- Undo: Offer undo last submit (track created IDs and provide bulk delete for that session).
- Tests: API preview checks; client highlighting; undo flow integration tests.

VS5 – Suggestions & Advanced UX (Optional):
- Auto-suggest Category/Description from recent history.
- Templates/presets; per-account support; split transactions.
- Tests: Suggestion ranking and selection; persistence of templates.

## Testing Strategy

Application (xUnit):
- `CreateManyAsync_AllValid_AllCreated`
- `CreateManyAsync_MixedDuplicatesAndErrors_ReturnsPerRowStatuses`
- `CreateManyAsync_NormalizesSignByType`
- `CreateManyAsync_ContinuesOnRowFailure_OtherRowsCreated`

API (WebApplicationFactory):
- `POST /transactions/bulk_AllSuccess_201Created`
- `POST /transactions/bulk_Mixed_207MultiStatus`
- `POST /transactions/bulk_ValidationErrors_200WithErrors`
- OpenAPI docs include examples

Client (bUnit):
- Keyboard navigation focus order
- Row add on last-cell Enter
- Validation messages per cell
- Submission wiring and per-row status rendering

Manual Checklist:
- Enter 20 rows quickly without mouse
- Tab and Enter behaviors feel natural
- Invalid cells block submission; messages are clear
- Duplicates reported clearly without crashing the batch

## OpenAPI Examples
- Request/response examples for `bulk` endpoint with mixed statuses.
- ProblemDetails schema reused for per-row validation.

## Security & Config
- Auth: N/A for now; future-ready.
- Limits: Max 500 items per bulk request (configurable via `appsettings`).
- Validation: Server enforces max lengths on Description/Category (e.g., 200 chars).

## Dependencies
- Uses existing FluentUI-Blazor (already integrated).
- No new NuGet packages required for client; server reuses existing layers.

## Risks & Mitigations
- Complex keyboard handling: Implement incrementally; keep bindings scoped to grid to avoid conflicts.
- Clipboard nuances: Test on Windows browsers first; implement robust parsing.
- User confusion on sign/type: Always normalize and show preview badge (+/-) before submit.

## Next Steps
1. Define DTOs and add Application bulk service + tests.
2. Add API endpoint + OpenAPI docs.
3. Build Blazor grid MVP and wire submission.
