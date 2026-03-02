// <copyright file="TokenRefreshHandler.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Headers;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// HTTP message handler that intercepts 401 Unauthorized responses and attempts
/// a silent token refresh before retrying the original request.
/// If refresh fails, shows a session-expired toast and returns the 401 response.
/// </summary>
public sealed class TokenRefreshHandler : DelegatingHandler
{
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);
    private static volatile bool _isRefreshing;
    private static string? _lastRefreshedToken;

    private readonly IAccessTokenProvider _tokenProvider;
    private readonly NavigationManager _navigation;
    private readonly IToastService _toastService;
    private readonly IFormStateService _formStateService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenRefreshHandler"/> class.
    /// </summary>
    /// <param name="tokenProvider">The access token provider.</param>
    /// <param name="navigation">The navigation manager.</param>
    /// <param name="toastService">The toast notification service.</param>
    /// <param name="formStateService">The form state preservation service.</param>
    public TokenRefreshHandler(
        IAccessTokenProvider tokenProvider,
        NavigationManager navigation,
        IToastService toastService,
        IFormStateService formStateService)
    {
        _tokenProvider = tokenProvider;
        _navigation = navigation;
        _toastService = toastService;
        _formStateService = formStateService;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        // Skip refresh if already on an authentication route
        if (_navigation.Uri.Contains("/authentication/", StringComparison.OrdinalIgnoreCase))
        {
            return response;
        }

        var refreshedToken = await TryRefreshTokenAsync(cancellationToken);

        if (refreshedToken is not null)
        {
            // Clone the original request and retry with the new token
            using var retryRequest = await CloneRequestAsync(request);
            retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshedToken);

            response.Dispose();
            return await base.SendAsync(retryRequest, cancellationToken);
        }

        // Refresh failed — preserve form data and notify user
        await _formStateService.SaveAllAsync();
        _toastService.ShowWarning("Session expired. Please log in again.");
        return response;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);

        if (original.Content is not null)
        {
            var content = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(content);

            foreach (var header in original.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Carry over options/properties
#pragma warning disable CS0618 // Properties is obsolete in favor of Options, but we need backward compat
        foreach (var prop in original.Properties)
        {
            clone.Properties[prop.Key] = prop.Value;
        }
#pragma warning restore CS0618

        clone.Version = original.Version;

        return clone;
    }

    private async Task<string?> TryRefreshTokenAsync(CancellationToken cancellationToken)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            // If another thread already refreshed, return the cached token
            if (_isRefreshing)
            {
                return _lastRefreshedToken;
            }

            _isRefreshing = true;

            var result = await _tokenProvider.RequestAccessToken();

            if (result.TryGetToken(out var token))
            {
                _lastRefreshedToken = token.Value;
                return token.Value;
            }

            _lastRefreshedToken = null;
            return null;
        }
        finally
        {
            _isRefreshing = false;
            _refreshLock.Release();
        }
    }
}
