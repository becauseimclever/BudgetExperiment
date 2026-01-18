// <copyright file="IAiApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Service interface for communicating with the AI API endpoints.
/// </summary>
public interface IAiApiService
{
    /// <summary>
    /// Gets the AI service status.
    /// </summary>
    /// <returns>The AI service status.</returns>
    Task<AiStatusDto?> GetStatusAsync();

    /// <summary>
    /// Gets available AI models.
    /// </summary>
    /// <returns>List of available models.</returns>
    Task<IReadOnlyList<AiModelDto>> GetModelsAsync();

    /// <summary>
    /// Gets the current AI settings.
    /// </summary>
    /// <returns>The current settings.</returns>
    Task<AiSettingsDto?> GetSettingsAsync();

    /// <summary>
    /// Updates AI settings.
    /// </summary>
    /// <param name="settings">The new settings.</param>
    /// <returns>The updated settings.</returns>
    Task<AiSettingsDto?> UpdateSettingsAsync(AiSettingsDto settings);

    /// <summary>
    /// Runs comprehensive AI analysis.
    /// </summary>
    /// <returns>The analysis results, or null if AI unavailable.</returns>
    Task<AnalysisResponseDto?> AnalyzeAsync();

    /// <summary>
    /// Generates AI suggestions.
    /// </summary>
    /// <param name="request">The generation request.</param>
    /// <returns>List of generated suggestions.</returns>
    Task<IReadOnlyList<RuleSuggestionDto>> GenerateSuggestionsAsync(GenerateSuggestionsRequest request);

    /// <summary>
    /// Gets pending suggestions.
    /// </summary>
    /// <param name="type">Optional filter by suggestion type.</param>
    /// <returns>List of pending suggestions.</returns>
    Task<IReadOnlyList<RuleSuggestionDto>> GetPendingSuggestionsAsync(string? type = null);

    /// <summary>
    /// Gets a suggestion by ID.
    /// </summary>
    /// <param name="id">The suggestion ID.</param>
    /// <returns>The suggestion or null if not found.</returns>
    Task<RuleSuggestionDto?> GetSuggestionAsync(Guid id);

    /// <summary>
    /// Accepts a suggestion.
    /// </summary>
    /// <param name="id">The suggestion ID.</param>
    /// <returns>The created/modified rule, or null if failed.</returns>
    Task<CategorizationRuleDto?> AcceptSuggestionAsync(Guid id);

    /// <summary>
    /// Dismisses a suggestion.
    /// </summary>
    /// <param name="id">The suggestion ID.</param>
    /// <param name="reason">Optional dismissal reason.</param>
    /// <returns>True if successful.</returns>
    Task<bool> DismissSuggestionAsync(Guid id, string? reason = null);

    /// <summary>
    /// Provides feedback on a suggestion.
    /// </summary>
    /// <param name="id">The suggestion ID.</param>
    /// <param name="isPositive">Whether the feedback is positive.</param>
    /// <returns>True if successful.</returns>
    Task<bool> ProvideFeedbackAsync(Guid id, bool isPositive);
}
