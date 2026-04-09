// <copyright file="FeatureFlagClientService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net.Http.Json;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Client-side service that fetches feature flags from the API and provides fast local lookup.
/// Registered as singleton; uses <see cref="IHttpClientFactory"/> to avoid capturing a scoped <see cref="HttpClient"/>.
/// </summary>
public sealed class FeatureFlagClientService : IFeatureFlagClientService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private Dictionary<string, bool> _flags = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureFlagClientService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory used to create clients for API calls.</param>
    public FeatureFlagClientService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
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
            var client = _httpClientFactory.CreateClient("BudgetApi");
            var flags = await client.GetFromJsonAsync<Dictionary<string, bool>>("api/v1/features");
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
