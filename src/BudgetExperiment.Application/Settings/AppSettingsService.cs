// <copyright file="AppSettingsService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Shared;

namespace BudgetExperiment.Application.Settings;

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
            EnableLocationData = settings.EnableLocationData,
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

        if (dto.EnableLocationData.HasValue)
        {
            settings.UpdateEnableLocationData(dto.EnableLocationData.Value);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AppSettingsDto
        {
            AutoRealizePastDueItems = settings.AutoRealizePastDueItems,
            PastDueLookbackDays = settings.PastDueLookbackDays,
            EnableLocationData = settings.EnableLocationData,
        };
    }

    /// <inheritdoc />
    public async Task<AiSettingsData> GetAiSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _repository.GetAsync(cancellationToken);
        return MapAiSettings(settings);
    }

    /// <inheritdoc />
    public async Task<AiSettingsData> UpdateAiSettingsAsync(
        AiSettingsData newSettings,
        CancellationToken cancellationToken = default)
    {
        var settings = await _repository.GetAsync(cancellationToken);
        var normalizedEndpointUrl = NormalizeEndpointUrl(newSettings.EndpointUrl, newSettings.BackendType);

        settings.UpdateAiSettings(
            endpointUrl: normalizedEndpointUrl,
            modelName: newSettings.ModelName,
            temperature: newSettings.Temperature,
            maxTokens: newSettings.MaxTokens,
            timeoutSeconds: newSettings.TimeoutSeconds,
            isEnabled: newSettings.IsEnabled,
            backendType: newSettings.BackendType);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapAiSettings(settings);
    }

    private static AiSettingsData MapAiSettings(Domain.Settings.AppSettings settings)
    {
        var backendType = Enum.IsDefined(settings.AiBackendType)
            ? settings.AiBackendType
            : AiDefaults.DefaultBackendType;

        return new AiSettingsData(
            EndpointUrl: NormalizeEndpointUrl(settings.AiEndpointUrl, backendType),
            ModelName: settings.AiModelName,
            Temperature: settings.AiTemperature,
            MaxTokens: settings.AiMaxTokens,
            TimeoutSeconds: settings.AiTimeoutSeconds,
            IsEnabled: settings.AiIsEnabled,
            BackendType: backendType);
    }

    private static string NormalizeEndpointUrl(string? endpointUrl, AiBackendType backendType) =>
        string.IsNullOrWhiteSpace(endpointUrl)
            ? AiDefaults.GetDefaultEndpointUrl(backendType)
            : endpointUrl.Trim();
}
