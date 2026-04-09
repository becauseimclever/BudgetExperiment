# AI Features

Budget Experiment includes two AI features, both powered by a local Ollama instance. Your financial data never leaves your machine.

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

## 💬 AI Chat Assistant

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
