[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/G2G11OJJBY)
[![CI](https://github.com/becauseimclever/BudgetExperiment/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/becauseimclever/BudgetExperiment/actions/workflows/ci.yml)
[![Code Coverage](https://img.shields.io/badge/coverage-check%20CI-blue)](https://github.com/becauseimclever/BudgetExperiment/actions/workflows/ci.yml)
[![Create Release](https://github.com/becauseimclever/BudgetExperiment/actions/workflows/release.yml/badge.svg)](https://github.com/becauseimclever/BudgetExperiment/actions/workflows/release.yml)
[![Build and Publish Docker Images](https://github.com/becauseimclever/BudgetExperiment/actions/workflows/docker-build-publish.yml/badge.svg)](https://github.com/becauseimclever/BudgetExperiment/actions/workflows/docker-build-publish.yml)
[![GitHub release](https://img.shields.io/github/v/release/becauseimclever/BudgetExperiment?include_prereleases&label=version)](https://github.com/becauseimclever/BudgetExperiment/releases)
[![License](https://img.shields.io/github/license/becauseimclever/BudgetExperiment)](LICENSE)

# Budget Experiment

I built this to track my own household finances. It started as an experiment — hence the name — but it's become something I actually use every day.

It's not just a budget tracker. It's a tool for thinking about money differently: where it comes from, where it goes, and — more importantly — *why*.

## 🎯 The Philosophy

Budget Experiment is built around **Kakeibo** (家計簿) — the Japanese art of mindful household accounting. The word itself means "household ledger," and that's exactly what this is. Not a dashboard full of charts optimized for dopamine hits, but a quiet, honest ledger you return to daily.

Kakeibo asks four questions that I find genuinely useful:

> *How much did I receive? How much do I want to save? How much did I spend? How can I improve?*

The **calendar is the centerpiece** — the primary surface for every financial decision. Every day is a journal entry. Every week offers a Kakeibo spending breakdown across four intentional categories (Essentials, Wants, Culture, Unexpected). Every month closes with reflection and opens with intention-setting.

Woven through the design is **Kaizen** (改善, "continuous improvement"): small weekly micro-goals, not grand resolutions. Compare yourself to yourself, not to arbitrary benchmarks. Progress is quiet and honest — a checkmark, not confetti.

The application supports this mindful rhythm with:
- **Calendar-first design**: Daily transaction summaries, weekly Kakeibo breakdowns, monthly reflections — all anchored to the calendar
- **Four Kakeibo categories**: Every expense maps to Essentials, Wants, Culture, or Unexpected — categories with intentional meaning
- **Monthly reflection ritual**: Set savings goals at month-start, journal improvements at month-end
- **Weekly Kaizen micro-goals**: Small, self-chosen improvements (e.g., "spend $10 less on dining than last week")
- **Multi-user authentication**: Secure login via Authentik OIDC with personal and shared budget scopes
- **Transaction management**: Track income and expenses across multiple accounts
- **Paycheck allocation planning**: Distribute bill amounts across pay periods to ensure timely payments
- **Recurring transactions**: Automate regular income and expenses with flexible scheduling
- **AI Chat Assistant**: Create transactions via natural language — "Add $50 for groceries at Walmart"
- **AI-powered categorization**: Get intelligent rule suggestions using local AI (via Ollama)
- **CSV import**: Import transactions from Bank of America, Capital One, and UHCU with duplicate detection
- **Reports & analytics**: Category spending, monthly trends, budget vs. actual comparison, date range filtering, week summaries, CSV exports, and a custom report builder
- **Component showcase**: Dedicated UI page for chart and component previews

## 🙋 Who Is This For?

This is a personal project built for a specific use case — household budgeting with intention. It might be for you if:

- You want to **understand your relationship with money**, not just track numbers in a spreadsheet
- You're a **self-hoster** who wants full control over your financial data — no third-party cloud, no subscriptions, runs on a Raspberry Pi
- You want **local AI** that analyses your spending without your data ever leaving your machine
- You're a **developer** looking for a real-world .NET 10 / Clean Architecture / Blazor WASM codebase to learn from, contribute to, or adapt
- You're tired of paying monthly fees to apps that sell you back your own data

## ⚡ Quick Start (Demo Mode)

You can have it running in under 2 minutes. Try Budget Experiment with a single command — no database setup or authentication required:

```bash
docker compose -f docker-compose.demo.yml up -d
```

Then open [http://localhost:5099](http://localhost:5099) in your browser.

This bundles PostgreSQL and runs with authentication disabled for easy evaluation. Data persists across restarts via a Docker volume. To reset: `docker compose -f docker-compose.demo.yml down -v`.

When ready for production, see [DEPLOY-QUICKSTART.md](DEPLOY-QUICKSTART.md) for Raspberry Pi deployment and [docs/AUTH-PROVIDERS.md](docs/AUTH-PROVIDERS.md) for authentication provider setup (Authentik, Google, Microsoft, or any OIDC provider).

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
- `BudgetExperiment.Shared` - Shared enums (BudgetScope, CategorySource, DescriptionMatchMode, etc.)
- `BudgetExperiment.Api` - REST API, dependency injection, OpenAPI, authentication
- `BudgetExperiment.Client` - Blazor WebAssembly UI with custom design system

**Tests (`tests/`)**
- `BudgetExperiment.Domain.Tests` - Pure unit tests, no external dependencies
- `BudgetExperiment.Application.Tests` - Application services with mocked repositories
- `BudgetExperiment.Infrastructure.Tests` - Repository integration tests (**requires Docker** for Testcontainers/PostgreSQL)
- `BudgetExperiment.Api.Tests` - Endpoint integration tests (**requires Docker** for Testcontainers/PostgreSQL)
- `BudgetExperiment.Client.Tests` - Blazor component tests (bUnit)
- `BudgetExperiment.Performance.Tests` - Load and latency tests via NBomber (**requires Docker**; excluded from default test runs)
- `BudgetExperiment.E2E.Tests` - End-to-end Playwright tests (**requires a running server**; excluded from default test runs)

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
cd BudgetExperiment
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

> **Docker required** for Infrastructure, API, and Performance tests — they use Testcontainers to spin up a real PostgreSQL instance.

Run the standard test suite (excludes Performance, E2E, and tests with external dependencies):
```powershell
dotnet test --filter "FullyQualifiedName!~E2E&Category!=ExternalDependency&Category!=Performance"
```

Run all tests (including Performance; still excludes E2E which need a live server):
```powershell
dotnet test --filter "FullyQualifiedName!~E2E"
```

Run tests for a specific project:
```powershell
dotnet test tests/BudgetExperiment.Domain.Tests/BudgetExperiment.Domain.Tests.csproj
```

**E2E tests** require Playwright browsers and a running application. Set `BUDGET_APP_URL` to target the server, then run:
```powershell
dotnet test tests/BudgetExperiment.E2E.Tests/BudgetExperiment.E2E.Tests.csproj
```

## 🐳 Deployment (Docker)

**Demo mode** (includes PostgreSQL, no auth required):
```bash
docker compose -f docker-compose.demo.yml up -d
```

**Production** (Raspberry Pi or server with external PostgreSQL + OIDC auth):
- [DEPLOY-QUICKSTART.md](DEPLOY-QUICKSTART.md) - Quick deployment guide
- [docs/AUTH-PROVIDERS.md](docs/AUTH-PROVIDERS.md) - Authentication provider setup

Images are automatically built and published to GitHub Container Registry on push to `main` or version tags.

Note: Local Docker builds are not supported. Pull pre-built images from `ghcr.io/becauseimclever/budgetexperiment`.

## 🛠️ Development Guidelines

This is an actively developed personal project. Here's how it's built and what to expect if you dig into the code:

- **Philosophy First**: The calendar is the centerpiece. Every feature should deepen the Kakeibo + Kaizen rhythm — mindful recording, intentional categorization, and continuous small improvement. The design philosophy lives in [`docs/archive/`](docs/archive/).
- **TDD First**: Write failing tests before implementation
- **SOLID Principles**: Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion
- **Clean Code**: Short methods, guard clauses, no commented code
- **StyleCop Enforced**: Warnings treated as errors
- **No Forbidden Libraries**: FluentAssertions and AutoFixture are banned

See [`.github/copilot-instructions.md`](.github/copilot-instructions.md) for comprehensive contributor guidelines.

## 📚 Key Domain Concepts

### Entities
- **Account** - Financial account with type (Checking, Savings, Credit, etc.) and running balance
- **Transaction** - Individual financial transaction with amount, date, description, and category; carries optional Kakeibo override for intentional one-off categorization
- **RecurringTransaction** - Template for auto-generated transactions with recurrence pattern
- **RecurringTransfer** - Scheduled transfers between accounts
- **BudgetCategory** - Spending category with type (Income, Expense, Transfer, Savings); routes to a Kakeibo bucket (Essentials, Wants, Culture, Unexpected) for mindful aggregation
- **BudgetGoal** - Monthly or yearly spending/savings target for a category
- **KakeiboCategory** - The four spending buckets (Essentials, Wants, Culture, Unexpected) that give every expense intentional meaning
- **MonthlyReflection** - A monthly journal entry capturing savings intention, actual outcome, and improvement notes; anchored to the calendar
- **KaizenGoal** - A weekly micro-improvement goal (e.g., "spend $10 less on dining than last week")

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
- **Reports**: `/api/v1/reports` - Category spending, spending trends, budget comparison, day summaries, date range analysis
- **Allocations**: `/api/v1/allocations` - Paycheck allocation planning
- **Chat**: `/api/v1/chat` - AI Chat Assistant for natural language commands
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

## � AI Chat Assistant

A natural language interface for managing your finances through conversation.

### Features

- **Natural Language Entry**: Create transactions by typing "Add $50 for groceries at Walmart"
- **Transfers**: Move money between accounts with "Transfer $500 from Checking to Savings"
- **Recurring Items**: Set up recurring transactions via chat
- **Context Awareness**: Automatically detects your current page context (account, category)
- **Action Preview**: Review actions before confirming with preview cards
- **VS Code-style Panel**: Side panel that smoothly slides in without covering content

### Example Commands

```
"Add $45.67 for dining out at Chipotle"
"Spent $120 on groceries yesterday"
"Transfer $200 from checking to savings"
"Got paid $2500 today"
"Add monthly rent expense of $1500"
```

### Usage

1. Click the **AI Assistant** button in the header (or use the chat icon)
2. Type your command in natural language
3. Review the parsed action in the preview card
4. Click **Confirm** to execute or **Cancel** to discard
5. The assistant maintains conversation history during your session

### Privacy

Like the AI Rule Suggestions, all chat processing happens locally via Ollama. Your conversation and financial data stay on your machine.

## 📡 Observability

Structured logging via Serilog with opt-in integrations — no infrastructure required by default.

| Feature | Default | Enable Via |
|---------|---------|------------|
| Structured console logs | **ON** (JSON in prod, readable in dev) | Always active |
| Rolling file logs | OFF | `Observability:File:Path` |
| Seq centralized logging | OFF | `Observability:Seq:Url` |
| OpenTelemetry (OTLP) traces/metrics/logs | OFF | `Observability:Otlp:Endpoint` |

See [docs/OBSERVABILITY.md](docs/OBSERVABILITY.md) for full configuration reference.

## 📖 Documentation

- [Copilot Instructions](.github/copilot-instructions.md) - Comprehensive engineering guide
- [CHANGELOG.md](CHANGELOG.md) - Version history and release notes
- [DEPLOY-QUICKSTART.md](DEPLOY-QUICKSTART.md) - Raspberry Pi deployment guide
- [docs/](docs/) - Feature specifications and design documents

## 🤝 Contributing

This started as one person's experiment with his household finances. If it resonates with you — whether that's fixing a bug, adding support for your bank's CSV format, improving a UI flow, or just tidying something up — PRs are genuinely welcome.

Here's how we work:

- **Start with tests**: We follow TDD — a failing test before the implementation. It sounds like overhead, but it makes the codebase genuinely easy to change.
- **Feature branches**: Branch off `main` with `feature/your-feature-name`
- **Before submitting**: Run `dotnet test` (all tests green) and `dotnet format` (keep StyleCop happy)
- **PRs include tests**: If you add or change behavior, include the tests that prove it

Not ready to write code? That's fine too. Opening an issue with feedback, a bug report, or an idea is a real contribution. The issues page is where the roadmap lives.

## 📝 License

See [LICENSE](LICENSE) file for details.

## 📌 Current State

This is an actively developed personal project. It works well for its intended purpose — household budgeting — and is improving continuously. Check the [Issues](https://github.com/becauseimclever/BudgetExperiment/issues) page for current status and roadmap ideas.

## 📧 Contact

Repository: [https://github.com/becauseimclever/BudgetExperiment](https://github.com/becauseimclever/BudgetExperiment)