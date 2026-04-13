// <copyright file="FakeAiApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Tests.Components.AI;

/// <summary>
/// Fake implementation of <see cref="IAiApiService"/> for component tests.
/// </summary>
internal sealed class FakeAiApiService : IAiApiService
{
    /// <summary>
    /// Gets or sets the status to return from <see cref="GetStatusAsync"/>.
    /// </summary>
    public AiStatusDto? StatusResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the models to return from <see cref="GetModelsAsync"/>.
    /// </summary>
    public IReadOnlyList<AiModelDto> ModelsResult { get; set; } = Array.Empty<AiModelDto>();

    /// <summary>
    /// Gets or sets the settings to return from <see cref="GetSettingsAsync"/>.
    /// </summary>
    public AiSettingsDto? SettingsResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned from <see cref="UpdateSettingsAsync"/>.
    /// </summary>
    public AiSettingsDto? UpdateSettingsResult
    {
        get; set;
    }

    /// <summary>
    /// Gets the last settings sent to <see cref="UpdateSettingsAsync"/>.
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

    /// <inheritdoc/>
    public Task<AiStatusDto?> GetStatusAsync()
    {
        GetStatusCallCount++;
        return Task.FromResult(StatusResult);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<AiModelDto>> GetModelsAsync() => Task.FromResult(ModelsResult);

    /// <inheritdoc/>
    public Task<AiSettingsDto?> GetSettingsAsync() => Task.FromResult(SettingsResult);

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

        return Task.FromResult<AiSettingsDto?>(UpdateSettingsResult ?? settings);
    }

    /// <inheritdoc/>
    public Task<AnalysisResponseDto?> AnalyzeAsync() => Task.FromResult<AnalysisResponseDto?>(null);

    /// <inheritdoc/>
    public Task<IReadOnlyList<RuleSuggestionDto>> GenerateSuggestionsAsync(GenerateSuggestionsRequest request) =>
        Task.FromResult<IReadOnlyList<RuleSuggestionDto>>(Array.Empty<RuleSuggestionDto>());

    /// <inheritdoc/>
    public Task<IReadOnlyList<RuleSuggestionDto>> GetPendingSuggestionsAsync(string? type = null) =>
        Task.FromResult<IReadOnlyList<RuleSuggestionDto>>(Array.Empty<RuleSuggestionDto>());

    /// <inheritdoc/>
    public Task<RuleSuggestionDto?> GetSuggestionAsync(Guid id) => Task.FromResult<RuleSuggestionDto?>(null);

    /// <inheritdoc/>
    public Task<CategorizationRuleDto?> AcceptSuggestionAsync(Guid id) => Task.FromResult<CategorizationRuleDto?>(null);

    /// <inheritdoc/>
    public Task<bool> DismissSuggestionAsync(Guid id, string? reason = null) => Task.FromResult(true);

    /// <inheritdoc/>
    public Task<bool> ProvideFeedbackAsync(Guid id, bool isPositive) => Task.FromResult(true);

    /// <inheritdoc/>
    public Task<SuggestionMetricsDto?> GetMetricsAsync() => Task.FromResult<SuggestionMetricsDto?>(null);
}
