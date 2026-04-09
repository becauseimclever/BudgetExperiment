# API

Budget Experiment exposes a versioned REST API documented with OpenAPI. Explore interactively at `/scalar` when the application is running.

## 🔍 API Overview

The API follows RESTful conventions with versioned endpoints and JWT authentication:

**Base Path**: `/api/v1`

Key endpoints:
- **Accounts**: `/api/v1/accounts` - CRUD for financial accounts
- **Transactions**: `/api/v1/transactions` - Transaction management
- **Recurring**: `/api/v1/recurring-transactions`, `/api/v1/recurring-transfers` - Recurring item management
- **Categories**: `/api/v1/categories` - Budget category management
- **Budgets**: `/api/v1/budgets` - Budget goals and progress tracking
- **Calendar**: `/api/v1/calendar` - Calendar view data with daily summaries
- **Reports**: `/api/v1/reports` - Category spending, spending trends, budget comparison, day summaries, date range analysis
- **Allocations**: `/api/v1/allocations` - Paycheck allocation planning
- **Chat**: `/api/v1/chat` - AI Chat Assistant for natural language commands
- **AI**: `/api/v1/ai` - AI-powered rule suggestions (requires Ollama)
- **Settings**: `/api/v1/settings` - Application settings
- **User**: `/api/v1/user` - Current user info
- **Version**: `/api/version` - Application version info

All endpoints documented with OpenAPI. Explore interactively at `/scalar`.

## 📥 CSV Import

- UI Flow:
  - Open the calendar import dialog, select bank, choose a `.csv` file, click `Preview`.
  - Review the preview table: duplicate rows are highlighted.
  - Edit Date/Description (and Category) inline or check "Import Anyway" to force duplicates.
  - Click `Import` to commit. Success counts and any errors are shown.

- Supported banks: `BankOfAmerica`, `CapitalOne`, `UnitedHeritageCreditUnion`.

- API Endpoints:
  - `POST /api/v1/csv-import` (legacy one-shot import)
  - `POST /api/v1/csv-import/preview` (multipart/form-data: `file`, `bankType`)
  - `POST /api/v1/csv-import/commit` (application/json of edited items)

- Examples:
```powershell
# Preview
curl -X POST http://localhost:5099/api/v1/csv-import/preview `
  -F "file=@transactions.csv" `
  -F "bankType=BankOfAmerica"

# Commit (JSON body contains Items array)
curl -X POST http://localhost:5099/api/v1/csv-import/commit `
  -H "Content-Type: application/json" `
  -d '{"items":[{"rowNumber":2,"date":"2025-11-10","description":"GROCERY STORE #456","amount":123.45,"transactionType":1,"category":"Groceries","forceImport":false}]}'
```

- Dedup configuration (appsettings):
```json
"CsvImportDeduplication": {
  "FuzzyDateWindowDays": 3,
  "MaxLevenshteinDistance": 5,
  "MinJaccardSimilarity": 0.6
}
```
