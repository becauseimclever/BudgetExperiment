// <copyright file="GeolocationService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

using System.Text.Json;

using Microsoft.JSInterop;

/// <summary>
/// Service for capturing GPS coordinates via browser Geolocation API.
/// </summary>
public sealed class GeolocationService : IAsyncDisposable, IDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeolocationService"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime.</param>
    public GeolocationService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Gets the current GPS position from the browser.
    /// </summary>
    /// <returns>A <see cref="GeolocationResult"/> with coordinates or an error message.</returns>
    public async Task<GeolocationResult> GetCurrentPositionAsync()
    {
        try
        {
            var module = await EnsureModuleAsync();
            var json = await module.InvokeAsync<JsonElement>("getCurrentPosition");

            var lat = json.GetProperty("latitude").GetDecimal();
            var lon = json.GetProperty("longitude").GetDecimal();

            return GeolocationResult.Success(lat, lon);
        }
        catch (JSException ex)
        {
            return GeolocationResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Checks if the browser supports the Geolocation API.
    /// </summary>
    /// <returns><see langword="true"/> if geolocation is supported.</returns>
    public async Task<bool> IsSupportedAsync()
    {
        try
        {
            var module = await EnsureModuleAsync();
            return await module.InvokeAsync<bool>("isSupported");
        }
        catch (JSException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Circuit disconnected during disposal — safe to ignore.
            }

            _module = null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No-op for synchronous disposal (bUnit compatibility).
    }

    private async Task<IJSObjectReference> EnsureModuleAsync()
    {
        _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/geolocation.js");
        return _module;
    }
}
