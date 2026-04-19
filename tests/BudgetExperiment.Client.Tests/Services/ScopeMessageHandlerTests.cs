// <copyright file="ScopeMessageHandlerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;

using BudgetExperiment.Client.Services;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Tests for the <see cref="ScopeMessageHandler"/> class.
/// </summary>
public sealed class ScopeMessageHandlerTests
{
    /// <summary>
    /// Verifies the handler removes any X-Budget-Scope header from requests.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendAsync_RemovesScopeHeaderFromRequest()
    {
        var innerHandler = new CapturingHandler();
        using var sut = new ScopeMessageHandler
        {
            InnerHandler = innerHandler,
        };
        using var invoker = new HttpMessageInvoker(sut);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/api/v1/accounts");
        request.Headers.TryAddWithoutValidation(ScopeMessageHandler.ScopeHeaderName, "Shared");

        await invoker.SendAsync(request, CancellationToken.None);

        Assert.False(innerHandler.LastRequest!.Headers.Contains(ScopeMessageHandler.ScopeHeaderName));
    }

    /// <summary>
    /// Verifies the handler forwards requests without a scope header unchanged.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendAsync_WithoutScopeHeader_ForwardsRequest()
    {
        var innerHandler = new CapturingHandler();
        using var sut = new ScopeMessageHandler
        {
            InnerHandler = innerHandler,
        };
        using var invoker = new HttpMessageInvoker(sut);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/api/v1/accounts");

        await invoker.SendAsync(request, CancellationToken.None);

        Assert.NotNull(innerHandler.LastRequest);
        Assert.False(innerHandler.LastRequest.Headers.Contains(ScopeMessageHandler.ScopeHeaderName));
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
