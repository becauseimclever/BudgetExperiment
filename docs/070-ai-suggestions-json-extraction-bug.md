# Bug Fix 070: AI Suggestions Silent JSON Parsing Failure
> **Status:** ✅ Done  
> **Priority:** High  
> **Estimated Effort:** Tiny (< 1 day)  
> **Dependencies:** None

## Overview

Fix a bug where the Smart Insights / AI Suggestions feature (`/ai/suggestions`) always displayed "No Suggestions" after running AI analysis, even when the AI model returned valid suggestion data. The root cause was that AI responses wrapped in markdown code blocks or containing preamble text were silently discarded during JSON parsing.

## Problem Statement

### Root Cause

The `RuleSuggestionService` parsing methods (`ParseNewRuleSuggestions`, `ParseOptimizationSuggestions`, `ParseConflictSuggestions`) passed raw AI response text directly to `JsonSerializer.Deserialize<T>()`. Despite the system prompt instructing the AI to respond with "ONLY valid JSON" and "no markdown formatting or code blocks," AI models frequently wrap their JSON output in markdown code blocks:

````
```json
{"suggestions": [{"pattern": "WALMART", ...}]}
```
````

Or include preamble text:

```
Here are my suggestions:
{"suggestions": [{"pattern": "WALMART", ...}]}
```

When `JsonSerializer.Deserialize` encountered these non-pure-JSON strings, it threw a `JsonException`. Each parsing method **silently caught** the exception and returned an empty array:

```csharp
catch (JsonException)
{
    return Array.Empty<RuleSuggestion>();  // ← Bug: valid data silently discarded
}
```

This meant that even successful AI analysis with valid suggestions always resulted in zero suggestions being persisted, and the user saw "No Suggestions" on every run.

### Symptom

1. User navigates to `/ai/suggestions` (Smart Insights page)
2. Clicks "Run AI Analysis" — analysis runs and completes with a result showing counts of 0
3. Page reloads suggestions — "No Suggestions" displayed
4. No error messages shown because the `catch (JsonException)` block returns empty instead of surfacing the error

### Impact

- The entire AI Suggestions feature was non-functional — no suggestions were ever generated
- The bug was invisible: no error messages, no logged warnings, no indication of failure
- Users had no way to diagnose why AI analysis always found nothing

### Contrast with Working Code

The `NaturalLanguageParser` (chat feature) already had correct JSON extraction logic:

```csharp
// NaturalLanguageParser.ParseAiResponse — already handled this correctly
var jsonStart = content.IndexOf('{');
var jsonEnd = content.LastIndexOf('}');
var jsonContent = content.Substring(jsonStart, jsonEnd - jsonStart + 1);
```

The `RuleSuggestionService` was missing this same extraction step.

---

## Fix

### Changed Files

| File | Change |
|------|--------|
| [RuleSuggestionService.cs](../src/BudgetExperiment.Application/Categorization/RuleSuggestionService.cs) | Added `ExtractJson()` method; applied it in all three parsing methods before `JsonSerializer.Deserialize` |
| [RuleSuggestionServiceTests.cs](../tests/BudgetExperiment.Application.Tests/RuleSuggestionServiceTests.cs) | Added 6 unit tests for `ExtractJson` and 2 integration tests for markdown-wrapped AI responses |

### Technical Detail

Added a public static `ExtractJson` method to `RuleSuggestionService` that locates the first `{` and last `}` in the AI response text and extracts the JSON object between them:

```csharp
public static string ExtractJson(string content)
{
    var jsonStart = content.IndexOf('{');
    var jsonEnd = content.LastIndexOf('}');

    if (jsonStart < 0 || jsonEnd < 0 || jsonEnd <= jsonStart)
    {
        throw new JsonException("No JSON object found in AI response.");
    }

    return content.Substring(jsonStart, jsonEnd - jsonStart + 1);
}
```

Applied in all three parsing methods (`ParseNewRuleSuggestions`, `ParseOptimizationSuggestions`, `ParseConflictSuggestions`):

```csharp
// BEFORE (broken):
var parsed = JsonSerializer.Deserialize<NewRuleSuggestionResponse>(jsonContent, JsonOptions);

// AFTER (fixed):
var extracted = ExtractJson(jsonContent);
var parsed = JsonSerializer.Deserialize<NewRuleSuggestionResponse>(extracted, JsonOptions);
```

This handles all common AI response wrapping patterns:
- Pure JSON (no change needed — passes through correctly)
- Markdown code blocks (`` ```json {...} ``` ``)
- Preamble text ("Here is the JSON:" followed by JSON)
- Trailing text (JSON followed by explanatory text)

---

## Tests Added

### ExtractJson Unit Tests (6)

| Test | Validates |
|------|-----------|
| `ExtractJson_Returns_Pure_Json_Unchanged` | Pure JSON passes through without modification |
| `ExtractJson_Strips_Markdown_Code_Block_Wrapping` | Removes `` ```json `` / `` ``` `` wrapping |
| `ExtractJson_Strips_Preamble_Text` | Ignores text before the JSON object |
| `ExtractJson_Strips_Trailing_Text` | Ignores text after the JSON object |
| `ExtractJson_Throws_JsonException_When_No_Json_Found` | Throws when content has no JSON |
| `ExtractJson_Throws_JsonException_For_Empty_Content` | Throws on empty string |

### Integration Tests (2)

| Test | Validates |
|------|-----------|
| `SuggestNewRulesAsync_Parses_Markdown_Wrapped_Json_Response` | End-to-end: markdown-wrapped AI response → parsed suggestion persisted |
| `SuggestNewRulesAsync_Parses_Response_With_Preamble_Text` | End-to-end: preamble text AI response → parsed suggestion persisted |

---

## Verification

```powershell
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.Application.Tests\BudgetExperiment.Application.Tests.csproj --filter "FullyQualifiedName~RuleSuggestionServiceTests"
# Result: Passed! - Failed: 0, Passed: 42, Skipped: 0
```
