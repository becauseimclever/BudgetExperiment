// <copyright file="FakeAppSettingsService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Infrastructure.ExternalServices.AI;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Fake app settings service for testing AI settings functionality.
/// </summary>
internal sealed class FakeAppSettingsService : IAppSettingsService
{
    private AiSettingsData _aiSettings;

    public FakeAppSettingsService(AiSettingsData aiSettings)
    {
        _aiSettings = aiSettings;
    }

    public Task<AppSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AppSettingsDto());
    }

    public Task<AppSettingsDto> UpdateSettingsAsync(AppSettingsUpdateDto dto, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AppSettingsDto());
    }

    public Task<AiSettingsData> GetAiSettingsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_aiSettings);
    }

    public Task<AiSettingsData> UpdateAiSettingsAsync(AiSettingsData settings, CancellationToken cancellationToken = default)
    {
        _aiSettings = settings;
        return Task.FromResult(_aiSettings);
    }
}
