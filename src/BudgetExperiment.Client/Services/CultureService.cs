// <copyright file="CultureService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Service for detecting and providing browser culture and timezone information.
/// Initialized from browser JS interop on first render.
/// </summary>
public sealed class CultureService : IAsyncDisposable, IDisposable
{
    private const string FallbackTimeZone = "UTC";

    private static readonly CultureInfo FallbackCulture = CultureInfo.GetCultureInfo("en-US");

    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;
    private CultureInfo _currentCulture = FallbackCulture;
    private string _currentTimeZone = FallbackTimeZone;
    private bool _isInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="CultureService"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime.</param>
    public CultureService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Gets the current culture detected from the browser, or en-US as fallback.
    /// </summary>
    public CultureInfo CurrentCulture => _currentCulture;

    /// <summary>
    /// Gets the current timezone ID (IANA format) detected from the browser, or UTC as fallback.
    /// </summary>
    public string CurrentTimeZone => _currentTimeZone;

    /// <summary>
    /// Initializes the service by detecting the browser's culture and timezone via JS interop.
    /// This method is idempotent — subsequent calls are no-ops.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        try
        {
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/culture.js");
            var result = await _module.InvokeAsync<CultureDetectionResult>("detectCulture");

            _currentCulture = ParseCultureSafe(result.Language);
            _currentTimeZone = string.IsNullOrWhiteSpace(result.TimeZone) ? FallbackTimeZone : result.TimeZone;
            _isInitialized = true;
        }
        catch (JSException)
        {
            _currentCulture = FallbackCulture;
            _currentTimeZone = FallbackTimeZone;
            _isInitialized = true;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Synchronous dispose is intentionally a no-op.
    }

    /// <inheritdoc/>
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
                // Circuit disconnected — safe to ignore.
            }

            _module = null;
        }
    }

    private static CultureInfo ParseCultureSafe(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return FallbackCulture;
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(language);

            // .NET ICU creates CultureInfo for arbitrary strings; verify the name
            // matches the input to detect fabricated cultures (e.g., "not-a-locale" → "not").
            if (!string.Equals(culture.Name, language, StringComparison.OrdinalIgnoreCase))
            {
                return FallbackCulture;
            }

            return culture;
        }
        catch (CultureNotFoundException)
        {
            return FallbackCulture;
        }
    }
}
