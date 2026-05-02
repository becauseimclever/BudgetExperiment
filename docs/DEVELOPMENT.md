# Development

Set up your local development environment, run tests, and learn the coding guidelines that govern this project.

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

### 3. Configure the encryption master key

Feature 163 requires a Base64-encoded 32-byte master key for encrypted persistence.

Generate a key in PowerShell:

```powershell
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
$key = [Convert]::ToBase64String($bytes)
$key
```

Store it in user secrets:

```powershell
dotnet user-secrets set "Encryption:MasterKey" "<PASTE_BASE64_KEY>" --project src/BudgetExperiment.Api/BudgetExperiment.Api.csproj
```

### 4. (Optional) Configure authentication for local development

Authentication uses Authentik OIDC. For local development without HTTPS:

```powershell
dotnet user-secrets set "Authentication:Authentik:RequireHttpsMetadata" "false" --project src/BudgetExperiment.Api/BudgetExperiment.Api.csproj
```

### 5. Run the application

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

## 🛠️ Development Guidelines

This is an actively developed personal project. Here's how it's built and what to expect if you dig into the code:

- **Philosophy First**: The calendar is the centerpiece. Every feature should deepen the Kakeibo (家計簿) + Kaizen (改善) rhythm — mindful recording, intentional categorization, and continuous small improvement. The design philosophy lives in [`docs/archive/`](../archive/).
- **TDD First**: Write failing tests before implementation
- **SOLID Principles**: Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion
- **Clean Code**: Short methods, guard clauses, no commented code
- **StyleCop Enforced**: Warnings treated as errors
- **No Forbidden Libraries**: FluentAssertions and AutoFixture are banned

For comprehensive contributor guidelines, see [`.github/copilot-instructions.md`](../.github/copilot-instructions.md).
