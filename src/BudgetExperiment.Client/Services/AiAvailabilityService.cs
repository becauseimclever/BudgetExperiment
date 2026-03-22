// <copyright file="AiAvailabilityService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Service for checking AI feature availability with caching.
/// </summary>
public sealed class AiAvailabilityService : IAiAvailabilityService
{
    private readonly IAiApiService _aiApiService;
    private AiStatusDto? _cachedStatus;
    private AiAvailabilityState _state = AiAvailabilityState.Disabled;
    private string? _errorMessage;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiAvailabilityService"/> class.
    /// </summary>
    /// <param name="aiApiService">The AI API service.</param>
    public AiAvailabilityService(IAiApiService aiApiService)
    {
        _aiApiService = aiApiService;
    }

    /// <inheritdoc/>
    public event Action? StatusChanged;

    /// <inheritdoc/>
    public AiAvailabilityState State => _state;

    /// <inheritdoc/>
    public bool IsEnabled => _cachedStatus?.IsEnabled ?? false;

    /// <inheritdoc/>
    public bool IsAvailable => _cachedStatus?.IsAvailable ?? false;

    /// <inheritdoc/>
    public bool IsFullyOperational => this.IsEnabled && this.IsAvailable;

    /// <inheritdoc/>
    public string? ErrorMessage => _errorMessage;

    /// <inheritdoc/>
    public async Task RefreshAsync()
    {
        try
        {
            _cachedStatus = await _aiApiService.GetStatusAsync();

            if (_cachedStatus == null || !_cachedStatus.IsEnabled)
            {
                _state = AiAvailabilityState.Disabled;
                _errorMessage = null;
            }
            else if (!_cachedStatus.IsAvailable)
            {
                _state = AiAvailabilityState.Unavailable;
                _errorMessage = _cachedStatus.ErrorMessage ?? "AI service is unavailable";
            }
            else
            {
                _state = AiAvailabilityState.Available;
                _errorMessage = null;
            }
        }
        catch (Exception ex)
        {
            // When API fails, assume AI might be enabled but unreachable
            // This shows warning state rather than hiding completely
            _state = AiAvailabilityState.Unavailable;
            _errorMessage = ex.Message;
            _cachedStatus = null;
        }

        this.StatusChanged?.Invoke();
    }
}
