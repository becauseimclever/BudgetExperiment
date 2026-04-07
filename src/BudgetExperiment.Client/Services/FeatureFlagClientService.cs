// <copyright file="FeatureFlagClientService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net.Http.Json;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Client-side service that fetches feature flags from the API and provides fast local lookup.
/// </summary>
public sealed class FeatureFlagClientService : IFeatureFlagClientService
{
    private readonly HttpClient _httpClient;
    private Dictionary<string, bool> _flags = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureFlagClientService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to call the API.</param>
    public FeatureFlagClientService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public Dictionary<string, bool> Flags => _flags;

    /// <inheritdoc />
    public bool IsEnabled(string flagName) =>
        _flags.TryGetValue(flagName, out var enabled) && enabled;

    /// <inheritdoc />
    public async Task LoadFlagsAsync()
    {
        try
        {
            var flags = await _httpClient.GetFromJsonAsync<Dictionary<string, bool>>("api/v1/features");
            _flags = flags ?? new Dictionary<string, bool>();
        }
        catch
        {
            _flags = new Dictionary<string, bool>();
        }
    }

    /// <inheritdoc />
    public Task RefreshAsync() => this.LoadFlagsAsync();
}
