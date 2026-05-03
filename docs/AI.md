# AI Features

Budget Experiment includes two local AI features. You can choose the AI backend: Ollama or llama.cpp. Your financial data stays on your machine.

## 🤖 AI-Powered Rule Suggestions

Budget Experiment includes AI-powered categorization rule suggestions using a local LLM backend.

### Features

- **New Rule Suggestions**: Analyzes uncategorized transactions and suggests patterns for automatic categorization
- **Pattern Optimizations**: Improves existing rule patterns for better matching
- **Conflict Detection**: Identifies overlapping rules that may cause unexpected behavior
- **Rule Consolidation**: Suggests merging similar rules to reduce complexity
- **Unused Rule Detection**: Finds rules that no longer match any transactions

### Setup

1. Install and start your preferred backend:
   - Ollama: [ollama.ai](https://ollama.ai/) (default endpoint: `http://localhost:11434`)
   - llama.cpp server (OpenAI-compatible mode) (default endpoint: `http://localhost:8080`)
2. Ensure the model you want is available on that backend.
3. Configure AI settings (optional) with backend-agnostic keys:
   ```powershell
   dotnet user-secrets set "AiSettings:BackendType" "Ollama" --project c:\ws\BudgetExperiment\src\BudgetExperiment.Api\BudgetExperiment.Api.csproj
   dotnet user-secrets set "AiSettings:EndpointUrl" "http://localhost:11434" --project c:\ws\BudgetExperiment\src\BudgetExperiment.Api\BudgetExperiment.Api.csproj
   dotnet user-secrets set "AiSettings:ModelName" "llama3.2" --project c:\ws\BudgetExperiment\src\BudgetExperiment.Api\BudgetExperiment.Api.csproj
   ```

Use `AiSettings:BackendType` as `Ollama` or `LlamaCpp`.

### Configuration Examples

Use one of the following backend profiles.

Ollama (`appsettings.json`):

```json
{
   "AiSettings": {
      "BackendType": "Ollama",
      "EndpointUrl": "http://localhost:11434",
      "ModelName": "llama3.2"
   }
}
```

llama.cpp (`appsettings.json`):

```json
{
   "AiSettings": {
      "BackendType": "LlamaCpp",
      "EndpointUrl": "http://localhost:8080",
      "ModelName": "Meta-Llama-3.1-8B-Instruct"
   }
}
```

Development override (`user-secrets`) for llama.cpp:

```powershell
dotnet user-secrets set "AiSettings:BackendType" "LlamaCpp" --project c:\ws\BudgetExperiment\src\BudgetExperiment.Api\BudgetExperiment.Api.csproj
dotnet user-secrets set "AiSettings:EndpointUrl" "http://localhost:8080" --project c:\ws\BudgetExperiment\src\BudgetExperiment.Api\BudgetExperiment.Api.csproj
```

### Usage

1. Navigate to **AI Suggestions** in the sidebar
2. Click **Run AI Analysis** to generate suggestions
3. Review each suggestion with AI reasoning and confidence scores
4. **Accept** to create rules automatically, or **Dismiss** to skip
5. Provide feedback (thumbs up/down) to help improve future suggestions

### Privacy

All AI processing happens through your configured local backend. Your financial data is never sent to external services.

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

Like AI Rule Suggestions, chat processing uses your configured local backend (Ollama or llama.cpp). Your conversation and financial data stay on your machine.
