// <copyright file="AppSettingsService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Application service for settings operations.
/// </summary>
public sealed class AppSettingsService : IAppSettingsService
{
    private readonly IAppSettingsRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppSettingsService"/> class.
    /// </summary>
    /// <param name="repository">The app settings repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public AppSettingsService(IAppSettingsRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<AppSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _repository.GetAsync(cancellationToken);
        return new AppSettingsDto
        {
            AutoRealizePastDueItems = settings.AutoRealizePastDueItems,
            PastDueLookbackDays = settings.PastDueLookbackDays,
        };
    }

    /// <inheritdoc />
    public async Task<AppSettingsDto> UpdateSettingsAsync(
        AppSettingsUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var settings = await _repository.GetAsync(cancellationToken);

        if (dto.AutoRealizePastDueItems.HasValue)
        {
            settings.UpdateAutoRealize(dto.AutoRealizePastDueItems.Value);
        }

        if (dto.PastDueLookbackDays.HasValue)
        {
            settings.UpdatePastDueLookbackDays(dto.PastDueLookbackDays.Value);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AppSettingsDto
        {
            AutoRealizePastDueItems = settings.AutoRealizePastDueItems,
            PastDueLookbackDays = settings.PastDueLookbackDays,
        };
    }

    /// <inheritdoc />
    public async Task<AiSettingsData> GetAiSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _repository.GetAsync(cancellationToken);
        return new AiSettingsData(
            OllamaEndpoint: settings.AiOllamaEndpoint,
            ModelName: settings.AiModelName,
            Temperature: settings.AiTemperature,
            MaxTokens: settings.AiMaxTokens,
            TimeoutSeconds: settings.AiTimeoutSeconds,
            IsEnabled: settings.AiIsEnabled);
    }

    /// <inheritdoc />
    public async Task<AiSettingsData> UpdateAiSettingsAsync(
        AiSettingsData newSettings,
        CancellationToken cancellationToken = default)
    {
        var settings = await _repository.GetAsync(cancellationToken);

        settings.UpdateAiSettings(
            ollamaEndpoint: newSettings.OllamaEndpoint,
            modelName: newSettings.ModelName,
            temperature: newSettings.Temperature,
            maxTokens: newSettings.MaxTokens,
            timeoutSeconds: newSettings.TimeoutSeconds,
            isEnabled: newSettings.IsEnabled);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AiSettingsData(
            OllamaEndpoint: settings.AiOllamaEndpoint,
            ModelName: settings.AiModelName,
            Temperature: settings.AiTemperature,
            MaxTokens: settings.AiMaxTokens,
            TimeoutSeconds: settings.AiTimeoutSeconds,
            IsEnabled: settings.AiIsEnabled);
    }
}
