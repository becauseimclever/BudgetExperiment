// <copyright file="ScopeMessageHandler.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// HTTP message handler that strips the removed X-Budget-Scope header from outgoing requests.
/// </summary>
public sealed class ScopeMessageHandler : DelegatingHandler
{
    /// <summary>
    /// The header name for budget scope.
    /// </summary>
    public const string ScopeHeaderName = "X-Budget-Scope";

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopeMessageHandler"/> class.
    /// </summary>
    /// <param name="scopeService">The scope service.</param>
    public ScopeMessageHandler(ScopeService scopeService)
    {
        _ = scopeService;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Remove(ScopeHeaderName);
        return await base.SendAsync(request, cancellationToken);
    }
}
