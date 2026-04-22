// <copyright file="ScopeMessageHandler.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// HTTP message handler that removes the X-Budget-Scope header from outgoing requests.
/// </summary>
/// <remarks>
/// This handler strips the internal Budget Scope header before requests leave the client,
/// ensuring the header is managed server-side only.
/// </remarks>
public sealed class ScopeMessageHandler : DelegatingHandler
{
    /// <summary>
    /// The name of the HTTP header that contains the budget scope identifier.
    /// </summary>
    public const string ScopeHeaderName = "X-Budget-Scope";

    /// <summary>
    /// Sends an HTTP request, removing the scope header from the request.
    /// </summary>
    /// <param name="request">The HTTP request message to send.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>The HTTP response message.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // Remove the scope header if it exists
        request.Headers.Remove(ScopeHeaderName);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
