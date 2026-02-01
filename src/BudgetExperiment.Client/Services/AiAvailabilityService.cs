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
        this._aiApiService = aiApiService;
    }

    /// <inheritdoc/>
    public AiAvailabilityState State => this._state;

    /// <inheritdoc/>
    public bool IsEnabled => this._cachedStatus?.IsEnabled ?? false;

    /// <inheritdoc/>
    public bool IsAvailable => this._cachedStatus?.IsAvailable ?? false;

    /// <inheritdoc/>
    public bool IsFullyOperational => this.IsEnabled && this.IsAvailable;

    /// <inheritdoc/>
    public string? ErrorMessage => this._errorMessage;

    /// <inheritdoc/>
    public event Action? StatusChanged;

    /// <inheritdoc/>
    public async Task RefreshAsync()
    {
        try
        {
            this._cachedStatus = await this._aiApiService.GetStatusAsync();

            if (this._cachedStatus == null || !this._cachedStatus.IsEnabled)
            {
                this._state = AiAvailabilityState.Disabled;
                this._errorMessage = null;
            }
            else if (!this._cachedStatus.IsAvailable)
            {
                this._state = AiAvailabilityState.Unavailable;
                this._errorMessage = this._cachedStatus.ErrorMessage ?? "AI service is unavailable";
            }
            else
            {
                this._state = AiAvailabilityState.Available;
                this._errorMessage = null;
            }
        }
        catch (Exception ex)
        {
            // When API fails, assume AI might be enabled but unreachable
            // This shows warning state rather than hiding completely
            this._state = AiAvailabilityState.Unavailable;
            this._errorMessage = ex.Message;
            this._cachedStatus = null;
        }

        this.StatusChanged?.Invoke();
    }
}
