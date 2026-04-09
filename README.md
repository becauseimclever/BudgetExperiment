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

## ✨ Features

- **Calendar-first ledger** — Daily summaries, weekly Kakeibo breakdowns, monthly reflections
- **Kakeibo + Kaizen** — Four intentional spending categories, weekly micro-goals, monthly reflection ritual
- **AI (local, private)** — Chat assistant and rule suggestions via Ollama — data stays on your machine
- **CSV import** — Bank of America, Capital One, and UHCU with duplicate detection
- **Reports** — Category spending, trends, budget vs. actual, custom report builder
- **Self-hosted** — Runs on a Raspberry Pi, single Docker command for demo mode
- **Multi-user** — Secure OIDC authentication via Authentik (or any provider)

→ See [docs/AI.md](docs/AI.md) for AI setup · [docs/API.md](docs/API.md) for the API reference · [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) for local setup

## 📖 Documentation

- [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) - Local setup, prerequisites, running tests
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) - System design, project structure, domain model
- [docs/API.md](docs/API.md) - REST API reference and CSV import
- [docs/AI.md](docs/AI.md) - AI features (chat assistant + rule suggestions)
- [docs/OBSERVABILITY.md](docs/OBSERVABILITY.md) - Logging and observability configuration
- [docs/AUTH-PROVIDERS.md](docs/AUTH-PROVIDERS.md) - Authentication provider setup
- [DEPLOY-QUICKSTART.md](DEPLOY-QUICKSTART.md) - Raspberry Pi deployment guide
- [CHANGELOG.md](CHANGELOG.md) - Version history
- [.github/copilot-instructions.md](.github/copilot-instructions.md) - Engineering guide for contributors

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
