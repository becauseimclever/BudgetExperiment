// <copyright file="ScopeMessageHandler.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// HTTP message handler that adds the X-Budget-Scope header to outgoing requests.
/// </summary>
public sealed class ScopeMessageHandler : DelegatingHandler
{
    /// <summary>
    /// The header name for budget scope.
    /// </summary>
    public const string ScopeHeaderName = "X-Budget-Scope";

    private readonly ScopeService _scopeService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopeMessageHandler"/> class.
    /// </summary>
    /// <param name="scopeService">The scope service.</param>
    public ScopeMessageHandler(ScopeService scopeService)
    {
        this._scopeService = scopeService;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var scope = this._scopeService.CurrentScope;
        var headerValue = scope switch
        {
            BudgetScope.Shared => "Shared",
            BudgetScope.Personal => "Personal",
            _ => "All",
        };

        // Remove any existing header and add the current scope
        request.Headers.Remove(ScopeHeaderName);
        request.Headers.Add(ScopeHeaderName, headerValue);

        return await base.SendAsync(request, cancellationToken);
    }
}
