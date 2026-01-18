// <copyright file="IRuleSuggestionService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Service for generating and managing AI-powered rule suggestions.
/// </summary>
public interface IRuleSuggestionService
{
    /// <summary>
    /// Analyzes uncategorized transactions and suggests new rules.
    /// </summary>
    /// <param name="maxSuggestions">Maximum number of suggestions to generate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of new rule suggestions.</returns>
    Task<IReadOnlyList<RuleSuggestion>> SuggestNewRulesAsync(
        int maxSuggestions = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Analyzes existing rules and suggests optimizations.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of optimization suggestions.</returns>
    Task<IReadOnlyList<RuleSuggestion>> SuggestOptimizationsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Detects conflicts and redundancies in existing rules.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of conflict suggestions.</returns>
    Task<IReadOnlyList<RuleSuggestion>> DetectConflictsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Runs comprehensive analysis and returns all suggestions.
    /// </summary>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Complete analysis results.</returns>
    Task<RuleSuggestionAnalysis> AnalyzeAllAsync(
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets pending suggestions.
    /// </summary>
    /// <param name="typeFilter">Optional filter by suggestion type.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of pending suggestions.</returns>
    Task<IReadOnlyList<RuleSuggestion>> GetPendingSuggestionsAsync(
        SuggestionType? typeFilter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Accepts a suggestion and creates the corresponding rule.
    /// </summary>
    /// <param name="suggestionId">The suggestion ID to accept.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created categorization rule.</returns>
    Task<CategorizationRule> AcceptSuggestionAsync(
        Guid suggestionId,
        CancellationToken ct = default);

    /// <summary>
    /// Dismisses a suggestion.
    /// </summary>
    /// <param name="suggestionId">The suggestion ID to dismiss.</param>
    /// <param name="reason">Optional reason for dismissal.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task DismissSuggestionAsync(
        Guid suggestionId,
        string? reason = null,
        CancellationToken ct = default);

    /// <summary>
    /// Provides feedback on a suggestion.
    /// </summary>
    /// <param name="suggestionId">The suggestion ID.</param>
    /// <param name="isPositive">Whether the feedback is positive.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ProvideFeedbackAsync(
        Guid suggestionId,
        bool isPositive,
        CancellationToken ct = default);
}

/// <summary>
/// Result of comprehensive rule analysis.
/// </summary>
public sealed record RuleSuggestionAnalysis
{
    /// <summary>
    /// Gets suggestions for new categorization rules.
    /// </summary>
    public IReadOnlyList<RuleSuggestion> NewRuleSuggestions { get; init; } = Array.Empty<RuleSuggestion>();

    /// <summary>
    /// Gets suggestions for optimizing existing rules.
    /// </summary>
    public IReadOnlyList<RuleSuggestion> OptimizationSuggestions { get; init; } = Array.Empty<RuleSuggestion>();

    /// <summary>
    /// Gets suggestions about rule conflicts.
    /// </summary>
    public IReadOnlyList<RuleSuggestion> ConflictSuggestions { get; init; } = Array.Empty<RuleSuggestion>();

    /// <summary>
    /// Gets the number of uncategorized transactions analyzed.
    /// </summary>
    public int UncategorizedTransactionsAnalyzed { get; init; }

    /// <summary>
    /// Gets the number of rules analyzed.
    /// </summary>
    public int RulesAnalyzed { get; init; }

    /// <summary>
    /// Gets the total duration of the analysis.
    /// </summary>
    public TimeSpan AnalysisDuration { get; init; }
}

/// <summary>
/// Progress information for analysis operations.
/// </summary>
public sealed record AnalysisProgress
{
    /// <summary>
    /// Gets the current step description.
    /// </summary>
    public string CurrentStep { get; init; } = string.Empty;

    /// <summary>
    /// Gets the percentage complete (0-100).
    /// </summary>
    public int PercentComplete { get; init; }
}
