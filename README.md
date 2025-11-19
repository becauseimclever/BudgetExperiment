[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/G2G11OJJBY)
[![Build and Publish Docker Images](https://github.com/becauseimclever/BudgetExperiment/actions/workflows/docker-build-publish.yml/badge.svg)](https://github.com/becauseimclever/BudgetExperiment/actions/workflows/docker-build-publish.yml)
# Budget Experiment

A clean architecture .NET 10 budgeting application that intelligently allocates portions of each paycheck to future bills, ensuring on-time payments and surfacing potential shortfalls early.

## 🎯 Purpose

Budget Experiment helps you manage cash flow by:
- **Automatic allocation planning**: Distributes bill amounts across pay periods leading up to each due date
- **Early shortfall detection**: Alerts you before you run short, not after
- **Flexible scheduling**: Supports varied paycheck frequencies (bi-weekly, semi-monthly, monthly, custom) and bill recurrence patterns
- **Precise tracking**: Shows exactly how much to set aside from each paycheck for upcoming bills

## 🏗️ Architecture

Built using **Clean Architecture** principles with strict layer separation:

```
┌─────────────────────────────────────────┐
│   Client (Blazor WebAssembly + FluentUI)│  ← Presentation
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
- `BudgetExperiment.Api` - REST API, dependency injection, OpenAPI
- `BudgetExperiment.Client` - Blazor WebAssembly UI with FluentUI components

**Tests (`tests/`)**
- Corresponding test projects for each layer using xUnit + Shouldly
- Test-driven development (TDD) enforced throughout

## 🚀 Technology Stack

- **.NET 10** - Latest framework
- **Blazor WebAssembly** - Modern client-side UI
- **FluentUI-Blazor** - Microsoft Fluent Design components
- **ASP.NET Core** - RESTful API
- **EF Core + Npgsql** - PostgreSQL database
- **OpenAPI + Scalar** - Interactive API documentation
- **xUnit + Shouldly** - Unit testing

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
dotnet user-secrets set "ConnectionStrings:AppDb" "Host=localhost;Database=budgetexperiment;Username=your_user;Password=your_password" --project c:\ws\BudgetExpirement\src\BudgetExperiment.Api\BudgetExperiment.Api.csproj
```

### 3. Apply database migrations

```powershell
dotnet ef database update --project c:\ws\BudgetExpirement\src\BudgetExperiment.Infrastructure\BudgetExperiment.Infrastructure.csproj --startup-project c:\ws\BudgetExpirement\src\BudgetExperiment.Api\BudgetExperiment.Api.csproj
```

### 4. Run the application

**Important**: Only run the API project. The Blazor client is hosted by the API.

```powershell
dotnet run --project c:\ws\BudgetExpirement\src\BudgetExperiment.Api\BudgetExperiment.Api.csproj
```

The application will be available at:
- **Web UI**: `http://localhost:5099`
- **API Documentation (Scalar)**: `http://localhost:5099/scalar`
- **OpenAPI Spec**: `http://localhost:5099/swagger/v1/swagger.json`

## 🧪 Running Tests

Run all tests:
```powershell
dotnet test
```

## 🐳 Deployment (Docker)

For Raspberry Pi or server deployment, use pre-built images from CI/CD as documented in:
- [DEPLOY-QUICKSTART.md](DEPLOY-QUICKSTART.md)
- [README.Docker.md](README.Docker.md)

Note: Local Docker build scripts have been removed. Follow the guides above to pull images from GitHub Container Registry and run with `docker compose` on the target device.


Run tests for a specific project:
```powershell
dotnet test tests\BudgetExperiment.Domain.Tests\BudgetExperiment.Domain.Tests.csproj
```

## 🛠️ Development Guidelines

This project follows strict engineering practices:

- **TDD First**: Write failing tests before implementation
- **SOLID Principles**: Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion
- **Clean Code**: Short methods, guard clauses, no commented code
- **StyleCop Enforced**: Warnings treated as errors
- **No Forbidden Libraries**: FluentAssertions and AutoFixture are banned

See [`.github/copilot-instructions.md`](.github/copilot-instructions.md) for comprehensive contributor guidelines.

## 📚 Key Domain Concepts

### Value Objects
- **MoneyValue** - Amount with currency, arithmetic operations, validation
- **RecurrencePatternValue** - Flexible scheduling (weekly, bi-weekly, monthly, custom)
- **DateRangeValue** - Period boundaries with overlap detection
- **DueDateValue** - Bill due dates with validation

### Aggregates
- **Budget** - Root aggregate containing bills and paycheck schedules
- **Bill** - Recurring expense with amount and due date pattern
- **PaycheckSchedule** - Income source with recurrence pattern
- **PayPeriod** - Generated pay period instance
- **Allocation** - Planned distribution of funds from paycheck to bill

### Services
- **PayPeriodGeneratorService** - Generates pay period instances from schedules
- **BillOccurrenceService** - Expands bill recurrence into concrete due dates
- **AllocationPlannerService** - Core algorithm distributing funds across periods
- **ShortfallDetectionService** - Identifies potential cash flow gaps
- **ProjectionService** - Orchestrates complete budget projection

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

The API follows RESTful conventions with versioned endpoints:

**Base Path**: `/api/v1`

Key endpoints (planned):
- `POST /api/v1/budgets` - Create a new budget
- `GET /api/v1/budgets/{id}` - Retrieve budget details
- `GET /api/v1/budgets/{id}/projection` - Get allocation projection with alerts

All endpoints documented with OpenAPI. Explore interactively at `/scalar`.

## 📖 Documentation

- [Architecture Plan](docs/architecture-plan.md) - Detailed architectural decisions and algorithm design
- [FluentUI Integration](docs/fluentui-integration.md) - UI component guidelines
- [Copilot Instructions](.github/copilot-instructions.md) - Comprehensive engineering guide

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
- Some planned features may not yet be implemented
- See GitHub Issues for current status

## 📧 Contact

Repository: [https://github.com/becauseimclever/BudgetExperiment](https://github.com/becauseimclever/BudgetExperiment)

---

**Note**: The typo "Expirement" in the repository name is intentional and preserved for consistency.