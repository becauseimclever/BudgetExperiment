// <copyright file="StubAiApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Tests.TestHelpers;

/// <summary>
/// Shared stub implementation of <see cref="IAiApiService"/> for page-level bUnit tests.
/// </summary>
internal class StubAiApiService : IAiApiService
{
    /// <summary>
    /// Gets or sets the AI status returned by <see cref="GetStatusAsync"/>.
    /// </summary>
    public AiStatusDto? AiStatus { get; set; }

    /// <summary>
    /// Gets the list of models returned by <see cref="GetModelsAsync"/>.
    /// </summary>
    public List<AiModelDto> Models { get; } = new();

    /// <summary>
    /// Gets or sets the settings returned by <see cref="GetSettingsAsync"/>.
    /// </summary>
    public AiSettingsDto? Settings { get; set; }

    /// <summary>
    /// Gets the list of pending suggestions returned by <see cref="GetPendingSuggestionsAsync"/>.
    /// </summary>
    public List<RuleSuggestionDto> PendingSuggestions { get; } = new();

    /// <inheritdoc/>
    public Task<AiStatusDto?> GetStatusAsync() =>
        Task.FromResult(this.AiStatus);

    /// <inheritdoc/>
    public Task<IReadOnlyList<AiModelDto>> GetModelsAsync() =>
        Task.FromResult<IReadOnlyList<AiModelDto>>(this.Models);

    /// <inheritdoc/>
    public Task<AiSettingsDto?> GetSettingsAsync() =>
        Task.FromResult(this.Settings);

    /// <inheritdoc/>
    public Task<AiSettingsDto?> UpdateSettingsAsync(AiSettingsDto settings) =>
        Task.FromResult<AiSettingsDto?>(settings);

    /// <inheritdoc/>
    public Task<AnalysisResponseDto?> AnalyzeAsync() =>
        Task.FromResult<AnalysisResponseDto?>(null);

    /// <inheritdoc/>
    public Task<IReadOnlyList<RuleSuggestionDto>> GenerateSuggestionsAsync(GenerateSuggestionsRequest request) =>
        Task.FromResult<IReadOnlyList<RuleSuggestionDto>>([]);

    /// <inheritdoc/>
    public Task<IReadOnlyList<RuleSuggestionDto>> GetPendingSuggestionsAsync(string? type = null) =>
        Task.FromResult<IReadOnlyList<RuleSuggestionDto>>(this.PendingSuggestions);

    /// <inheritdoc/>
    public Task<RuleSuggestionDto?> GetSuggestionAsync(Guid id) =>
        Task.FromResult<RuleSuggestionDto?>(null);

    /// <inheritdoc/>
    public Task<CategorizationRuleDto?> AcceptSuggestionAsync(Guid id) =>
        Task.FromResult<CategorizationRuleDto?>(null);

    /// <inheritdoc/>
    public Task<bool> DismissSuggestionAsync(Guid id, string? reason = null) =>
        Task.FromResult(false);

    /// <inheritdoc/>
    public Task<bool> ProvideFeedbackAsync(Guid id, bool isPositive) =>
        Task.FromResult(false);
}
