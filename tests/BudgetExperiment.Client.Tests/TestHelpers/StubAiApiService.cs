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
    public AiStatusDto? AiStatus
    {
        get; set;
    }

    /// <summary>
    /// Gets the list of models returned by <see cref="GetModelsAsync"/>.
    /// </summary>
    public List<AiModelDto> Models { get; } = new();

    /// <summary>
    /// Gets or sets the settings returned by <see cref="GetSettingsAsync"/>.
    /// </summary>
    public AiSettingsDto? Settings
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="UpdateSettingsAsync"/>.
    /// </summary>
    public AiSettingsDto? UpdateSettingsResult
    {
        get; set;
    }

    /// <summary>
    /// Gets the last settings payload received by <see cref="UpdateSettingsAsync"/>.
    /// </summary>
    public AiSettingsDto? LastUpdatedSettings
    {
        get; private set;
    }

    /// <summary>
    /// Gets the number of times <see cref="GetStatusAsync"/> has been called.
    /// </summary>
    public int GetStatusCallCount
    {
        get; private set;
    }

    /// <summary>
    /// Gets the list of pending suggestions returned by <see cref="GetPendingSuggestionsAsync"/>.
    /// </summary>
    public List<RuleSuggestionDto> PendingSuggestions { get; } = new();

    /// <summary>
    /// Gets or sets the result returned by <see cref="AcceptSuggestionAsync"/>.
    /// </summary>
    public CategorizationRuleDto? AcceptSuggestionResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="DismissSuggestionAsync"/> returns true.
    /// </summary>
    public bool DismissSuggestionResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ProvideFeedbackAsync"/> returns true.
    /// </summary>
    public bool ProvideFeedbackResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="AnalyzeAsync"/>.
    /// </summary>
    public AnalysisResponseDto? AnalyzeResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the exception to throw from <see cref="GetPendingSuggestionsAsync"/>.
    /// </summary>
    public Exception? GetPendingSuggestionsException
    {
        get; set;
    }

    /// <inheritdoc/>
    public Task<AiStatusDto?> GetStatusAsync()
    {
        GetStatusCallCount++;
        return Task.FromResult(this.AiStatus);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<AiModelDto>> GetModelsAsync() =>
        Task.FromResult<IReadOnlyList<AiModelDto>>(this.Models);

    /// <inheritdoc/>
    public Task<AiSettingsDto?> GetSettingsAsync() =>
        Task.FromResult(this.Settings);

    /// <inheritdoc/>
    public Task<AiSettingsDto?> UpdateSettingsAsync(AiSettingsDto settings)
    {
        LastUpdatedSettings = new AiSettingsDto
        {
            BackendType = settings.BackendType,
            EndpointUrl = settings.EndpointUrl,
            ModelName = settings.ModelName,
            Temperature = settings.Temperature,
            MaxTokens = settings.MaxTokens,
            TimeoutSeconds = settings.TimeoutSeconds,
            IsEnabled = settings.IsEnabled,
        };

        Settings = UpdateSettingsResult ?? settings;
        return Task.FromResult<AiSettingsDto?>(Settings);
    }

    /// <inheritdoc/>
    public Task<AnalysisResponseDto?> AnalyzeAsync() =>
        Task.FromResult(this.AnalyzeResult);

    /// <inheritdoc/>
    public Task<IReadOnlyList<RuleSuggestionDto>> GenerateSuggestionsAsync(GenerateSuggestionsRequest request) =>
        Task.FromResult<IReadOnlyList<RuleSuggestionDto>>([]);

    /// <inheritdoc/>
    public Task<IReadOnlyList<RuleSuggestionDto>> GetPendingSuggestionsAsync(string? type = null)
    {
        if (this.GetPendingSuggestionsException != null)
        {
            return Task.FromException<IReadOnlyList<RuleSuggestionDto>>(this.GetPendingSuggestionsException);
        }

        return Task.FromResult<IReadOnlyList<RuleSuggestionDto>>(this.PendingSuggestions);
    }

    /// <inheritdoc/>
    public Task<RuleSuggestionDto?> GetSuggestionAsync(Guid id) =>
        Task.FromResult<RuleSuggestionDto?>(null);

    /// <inheritdoc/>
    public Task<CategorizationRuleDto?> AcceptSuggestionAsync(Guid id) =>
        Task.FromResult(this.AcceptSuggestionResult);

    /// <inheritdoc/>
    public Task<bool> DismissSuggestionAsync(Guid id, string? reason = null) =>
        Task.FromResult(this.DismissSuggestionResult);

    /// <inheritdoc/>
    public Task<bool> ProvideFeedbackAsync(Guid id, bool isPositive) =>
        Task.FromResult(this.ProvideFeedbackResult);

    /// <inheritdoc/>
    public Task<SuggestionMetricsDto?> GetMetricsAsync() =>
        Task.FromResult<SuggestionMetricsDto?>(null);
}
