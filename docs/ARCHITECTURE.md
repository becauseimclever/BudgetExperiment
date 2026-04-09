# Architecture

This document covers the technical architecture of Budget Experiment — layer design, project structure, domain model, and technology choices.

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
