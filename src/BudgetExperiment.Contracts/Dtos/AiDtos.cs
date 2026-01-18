// <copyright file="AiDtos.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Response DTO for AI service status.
/// </summary>
public sealed class AiStatusDto
{
    /// <summary>
    /// Gets or sets a value indicating whether the AI service is available.
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether AI features are enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the currently configured model name.
    /// </summary>
    public string? CurrentModel { get; set; }

    /// <summary>
    /// Gets or sets the Ollama endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets any error message if the service is unavailable.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// DTO for AI model information.
/// </summary>
public sealed class AiModelDto
{
    /// <summary>
    /// Gets or sets the model name/identifier.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the model was last modified.
    /// </summary>
    public DateTime ModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the model size in bytes.
    /// </summary>
    public long SizeBytes { get; set; }
}

/// <summary>
/// DTO for AI settings configuration.
/// </summary>
public sealed class AiSettingsDto
{
    /// <summary>
    /// Gets or sets the Ollama API endpoint URL.
    /// </summary>
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Gets or sets the model name to use.
    /// </summary>
    public string ModelName { get; set; } = "llama3.2";

    /// <summary>
    /// Gets or sets the temperature for generation (0.0 to 1.0).
    /// </summary>
    public decimal Temperature { get; set; } = 0.3m;

    /// <summary>
    /// Gets or sets the maximum tokens for responses.
    /// </summary>
    public int MaxTokens { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Gets or sets a value indicating whether AI features are enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Request DTO for generating AI suggestions.
/// </summary>
public sealed class GenerateSuggestionsRequest
{
    /// <summary>
    /// Gets or sets the suggestion type filter (NewRule, Optimization, Conflict, or null for all).
    /// </summary>
    public string? SuggestionType { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of suggestions to generate.
    /// </summary>
    public int MaxSuggestions { get; set; } = 10;
}

/// <summary>
/// Response DTO for comprehensive AI analysis.
/// </summary>
public sealed class AnalysisResponseDto
{
    /// <summary>
    /// Gets or sets the number of new rule suggestions generated.
    /// </summary>
    public int NewRuleSuggestions { get; set; }

    /// <summary>
    /// Gets or sets the number of optimization suggestions generated.
    /// </summary>
    public int OptimizationSuggestions { get; set; }

    /// <summary>
    /// Gets or sets the number of conflict suggestions generated.
    /// </summary>
    public int ConflictSuggestions { get; set; }

    /// <summary>
    /// Gets or sets the number of uncategorized transactions analyzed.
    /// </summary>
    public int UncategorizedTransactionsAnalyzed { get; set; }

    /// <summary>
    /// Gets or sets the number of rules analyzed.
    /// </summary>
    public int RulesAnalyzed { get; set; }

    /// <summary>
    /// Gets or sets the analysis duration in seconds.
    /// </summary>
    public double AnalysisDurationSeconds { get; set; }
}

/// <summary>
/// Request DTO for dismissing a suggestion.
/// </summary>
public sealed class DismissSuggestionRequest
{
    /// <summary>
    /// Gets or sets the optional reason for dismissal.
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Request DTO for providing feedback on a suggestion.
/// </summary>
public sealed class FeedbackRequest
{
    /// <summary>
    /// Gets or sets a value indicating whether the feedback is positive.
    /// </summary>
    public bool IsPositive { get; set; }
}
