[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/G2G11OJJBY)
[![Build and Publish Docker Images](https://github.com/becauseimclever/BudgetExperiment/actions/workflows/docker-build-publish.yml/badge.svg)](https://github.com/becauseimclever/BudgetExperiment/actions/workflows/docker-build-publish.yml)
[![GitHub release](https://img.shields.io/github/v/release/becauseimclever/BudgetExperiment?include_prereleases&label=version)](https://github.com/becauseimclever/BudgetExperiment/releases)
[![License](https://img.shields.io/github/license/becauseimclever/BudgetExperiment)](LICENSE)

# Budget Experiment

A clean architecture .NET 10 budgeting application with multi-user authentication, transaction tracking, budget categories, and intelligent paycheck allocation planning.

## 🎯 Purpose

Budget Experiment helps you manage your finances by:
- **Multi-user authentication**: Secure login via Authentik OIDC with personal and shared budget scopes
- **Transaction management**: Track income and expenses across multiple accounts
- **Budget categories & goals**: Set spending targets and monitor progress
- **Paycheck allocation planning**: Distribute bill amounts across pay periods to ensure timely payments
- **Recurring transactions**: Automate regular income and expenses with flexible scheduling
- **AI-powered categorization**: Get intelligent rule suggestions using local AI (via Ollama)
- **CSV import**: Import transactions from Bank of America, Capital One, and UHCU with duplicate detection
- **Calendar view**: Visualize daily transaction summaries and navigate spending history

## 🏗️ Architecture

Built using **Clean Architecture** principles with strict layer separation:

```
┌─────────────────────────────────────────┐
│   Client (Blazor WebAssembly)           │  ← Presentation
├─────────────────────────────────────────┤
│   API (ASP.NET Core + OpenAPI/Scalar)   │  ← Interface/Controllers
├─────────────────────────────────────────┤
│   Application (Services, DTOs, Use Cases│  ← Business Workflows
├─────────────────────────────────────────┤
│   Domain (Entities, Value Objects)      │  ← Core Business Logic
├─────────────────────────────────────────┤
│   Infrastructure (EF Core + Postgres)   │  ← Data Access
└─────────────────────────────────────────┘
```

### Projects

**Source (`src/`)**
- `BudgetExperiment.Domain` - Pure domain models, value objects, business rules
- `BudgetExperiment.Application` - Use cases, services, DTOs, orchestration
- `BudgetExperiment.Infrastructure` - EF Core, repositories, database migrations
- `BudgetExperiment.Contracts` - Shared DTOs for API and client communication
- `BudgetExperiment.Api` - REST API, dependency injection, OpenAPI, authentication
- `BudgetExperiment.Client` - Blazor WebAssembly UI with custom design system

**Tests (`tests/`)**
- Corresponding test projects for each layer using xUnit + Shouldly
- Test-driven development (TDD) enforced throughout

## 🚀 Technology Stack

- **.NET 10** - Latest framework
- **Blazor WebAssembly** - Modern client-side UI with custom design system
- **ASP.NET Core** - RESTful API with JWT authentication
- **Authentik** - OIDC identity provider integration
- **EF Core + Npgsql** - PostgreSQL database
- **OpenAPI + Scalar** - Interactive API documentation
- **xUnit + Shouldly** - Unit testing
- **Docker** - Multi-architecture container builds (amd64, arm64)

## 📋 Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL](https://www.postgresql.org/download/) (local or remote instance)
- (Optional) [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

## ⚙️ Setup

### 1. Clone the repository

```powershell
git clone https://github.com/becauseimclever/BudgetExperiment.git
cd BudgetExpirement
```

### 2. Configure the database connection

The connection string is stored in user secrets for security:

```powershell
dotnet user-secrets set "ConnectionStrings:AppDb" "Host=localhost;Database=budgetexperiment;Username=your_user;Password=your_password" --project src/BudgetExperiment.Api/BudgetExperiment.Api.csproj
```

### 3. (Optional) Configure authentication for local development

Authentication uses Authentik OIDC. For local development without HTTPS:

```powershell
dotnet user-secrets set "Authentication:Authentik:RequireHttpsMetadata" "false" --project src/BudgetExperiment.Api/BudgetExperiment.Api.csproj
```

### 4. Run the application

**Important**: Only run the API project. The Blazor client is hosted by the API.

```powershell
dotnet run --project src/BudgetExperiment.Api/BudgetExperiment.Api.csproj
```

Database migrations are applied automatically at startup. No manual `dotnet ef database update` is required.

The application will be available at:
- **Web UI**: `http://localhost:5099`
- **API Documentation (Scalar)**: `http://localhost:5099/scalar`
- **OpenAPI Spec**: `http://localhost:5099/swagger/v1/swagger.json`

## 🧪 Running Tests

Run all tests:
```powershell
dotnet test
```

Run tests for a specific project:
```powershell
dotnet test tests/BudgetExperiment.Domain.Tests/BudgetExperiment.Domain.Tests.csproj
```

## 🐳 Deployment (Docker)

For Raspberry Pi or server deployment, use pre-built images from CI/CD:
- [DEPLOY-QUICKSTART.md](DEPLOY-QUICKSTART.md) - Quick deployment guide

Images are automatically built and published to GitHub Container Registry on push to `main` or version tags.

Note: Local Docker builds are not supported. Pull pre-built images from `ghcr.io/becauseimclever/budgetexperiment`.

## 🛠️ Development Guidelines

This project follows strict engineering practices:

- **TDD First**: Write failing tests before implementation
- **SOLID Principles**: Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion
- **Clean Code**: Short methods, guard clauses, no commented code
- **StyleCop Enforced**: Warnings treated as errors
- **No Forbidden Libraries**: FluentAssertions and AutoFixture are banned

See [`.github/copilot-instructions.md`](.github/copilot-instructions.md) for comprehensive contributor guidelines.

## 📚 Key Domain Concepts

### Entities
- **Account** - Financial account with type (Checking, Savings, Credit, etc.) and running balance
- **Transaction** - Individual financial transaction with amount, date, description, and category
- **RecurringTransaction** - Template for auto-generated transactions with recurrence pattern
- **RecurringTransfer** - Scheduled transfers between accounts
- **BudgetCategory** - Spending category with type (Income, Expense, Transfer, Savings)
- **BudgetGoal** - Monthly or yearly spending/savings target for a category

### Value Objects
- **MoneyValue** - Amount with currency validation and arithmetic operations
- **RecurrencePattern** - Flexible scheduling (daily, weekly, bi-weekly, monthly, yearly)

### Services
- **PaycheckAllocationCalculator** - Core algorithm distributing funds across pay periods
- **RecurringInstanceProjector** - Projects future recurring transaction instances
- **AutoRealizeService** - Converts due recurring items into actual transactions

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
curl -X POST http://localhost:5099/api/v1/csv-import/preview \`n  -F "file=@transactions.csv" \`n  -F "bankType=BankOfAmerica"

# Commit (JSON body contains Items array)
curl -X POST http://localhost:5099/api/v1/csv-import/commit \`n  -H "Content-Type: application/json" \`n  -d '{"items":[{"rowNumber":2,"date":"2025-11-10","description":"GROCERY STORE #456","amount":123.45,"transactionType":1,"category":"Groceries","forceImport":false}]}'
```

- Dedup configuration (appsettings):
```json
"CsvImportDeduplication": {
  "FuzzyDateWindowDays": 3,
  "MaxLevenshteinDistance": 5,
  "MinJaccardSimilarity": 0.6
}
```

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
- **Allocations**: `/api/v1/allocations` - Paycheck allocation planning
- **AI**: `/api/v1/ai` - AI-powered rule suggestions (requires Ollama)
- **Settings**: `/api/v1/settings` - Application settings
- **User**: `/api/v1/user` - Current user info
- **Version**: `/api/version` - Application version info

All endpoints documented with OpenAPI. Explore interactively at `/scalar`.

## 🤖 AI-Powered Rule Suggestions

Budget Experiment includes AI-powered categorization rule suggestions using a local LLM via [Ollama](https://ollama.ai/).

### Features

- **New Rule Suggestions**: Analyzes uncategorized transactions and suggests patterns for automatic categorization
- **Pattern Optimizations**: Improves existing rule patterns for better matching
- **Conflict Detection**: Identifies overlapping rules that may cause unexpected behavior
- **Rule Consolidation**: Suggests merging similar rules to reduce complexity
- **Unused Rule Detection**: Finds rules that no longer match any transactions

### Setup

1. **Install Ollama**: Download from [ollama.ai](https://ollama.ai/)
2. **Pull a model**: `ollama pull llama3.2` (or another supported model)
3. **Start Ollama**: Ensure the Ollama service is running (default: `http://localhost:11434`)
4. **Configure (optional)**: Customize settings via appsettings or user secrets:
   ```powershell
   dotnet user-secrets set "AiSettings:OllamaEndpoint" "http://localhost:11434" --project src/BudgetExperiment.Api
   dotnet user-secrets set "AiSettings:ModelName" "llama3.2" --project src/BudgetExperiment.Api
   ```

### Usage

1. Navigate to **AI Suggestions** in the sidebar
2. Click **Run AI Analysis** to generate suggestions
3. Review each suggestion with AI reasoning and confidence scores
4. **Accept** to create rules automatically, or **Dismiss** to skip
5. Provide feedback (thumbs up/down) to help improve future suggestions

### Privacy

All AI processing happens locally on your machine. Your financial data is never sent to external services. The AI runs entirely through your local Ollama instance.

## 📖 Documentation

- [Copilot Instructions](.github/copilot-instructions.md) - Comprehensive engineering guide
- [CHANGELOG.md](CHANGELOG.md) - Version history and release notes
- [DEPLOY-QUICKSTART.md](DEPLOY-QUICKSTART.md) - Raspberry Pi deployment guide
- [docs/](docs/) - Feature specifications and design documents

## 🤝 Contributing

1. Create a feature branch: `feature/your-feature-name`
2. Follow TDD: Write tests first
3. Ensure all tests pass: `dotnet test`
4. Format code: `dotnet format`
5. Submit PR with tests included

## 📝 License

See [LICENSE](LICENSE) file for details.

## 🐛 Known Issues

- Project is in active development
- See GitHub Issues for current status

## 📧 Contact

Repository: [https://github.com/becauseimclever/BudgetExperiment](https://github.com/becauseimclever/BudgetExperiment)