// <copyright file="VersionService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Service for retrieving and caching version information from the API.
/// </summary>
public sealed class VersionService
{
    private readonly HttpClient _httpClient;
    private VersionInfoDto? _cachedVersion;
    private bool _hasLoaded;

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    public VersionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Gets the current application version string.
    /// </summary>
    public string CurrentVersion => _cachedVersion?.Version ?? "loading...";

    /// <summary>
    /// Gets the version information if loaded, otherwise null.
    /// </summary>
    public VersionInfoDto? VersionInfo => _cachedVersion;

    /// <summary>
    /// Gets a value indicating whether version information has been loaded.
    /// </summary>
    public bool IsLoaded => _hasLoaded;

    /// <summary>
    /// Loads version information from the API.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LoadVersionAsync()
    {
        if (_hasLoaded)
        {
            return;
        }

        try
        {
            _cachedVersion = await _httpClient.GetFromJsonAsync<VersionInfoDto>("api/version");
            _hasLoaded = true;
        }
        catch (Exception)
        {
            // Silently fail - version display is not critical
            _cachedVersion = new VersionInfoDto("unknown", DateTime.UtcNow, null, "unknown");
            _hasLoaded = true;
        }
    }
}
