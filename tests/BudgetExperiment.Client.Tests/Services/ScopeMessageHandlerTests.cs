// <copyright file="ScopeMessageHandlerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;

using BudgetExperiment.Client.Services;

using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Tests for the <see cref="ScopeMessageHandler"/> class.
/// </summary>
public sealed class ScopeMessageHandlerTests
{
    /// <summary>
    /// Verifies Phase 2 no longer sends a scope header when no scope was chosen.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendAsync_WithoutExplicitScope_DoesNotSendScopeHeader()
    {
        var scopeService = new ScopeService(new StubJSRuntime());
        var innerHandler = new CapturingHandler();
        using var sut = new ScopeMessageHandler(scopeService)
        {
            InnerHandler = innerHandler,
        };
        using var invoker = new HttpMessageInvoker(sut);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/api/v1/accounts");

        await invoker.SendAsync(request, CancellationToken.None);

        Assert.False(innerHandler.LastRequest!.Headers.Contains(ScopeMessageHandler.ScopeHeaderName));
    }

    /// <summary>
    /// Verifies legacy persisted Personal values do not leak into a removed scope header.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendAsync_WithLegacyPersonalScope_DoesNotSendScopeHeader()
    {
        var jsRuntime = new StubJSRuntime();
        jsRuntime.GetItems["budget-experiment-scope"] = "Personal";
        var scopeService = new ScopeService(jsRuntime);
        await scopeService.InitializeAsync();
        var innerHandler = new CapturingHandler();
        using var sut = new ScopeMessageHandler(scopeService)
        {
            InnerHandler = innerHandler,
        };
        using var invoker = new HttpMessageInvoker(sut);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/api/v1/accounts");

        await invoker.SendAsync(request, CancellationToken.None);

        Assert.False(innerHandler.LastRequest!.Headers.Contains(ScopeMessageHandler.ScopeHeaderName));
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

    private sealed class StubJSRuntime : IJSRuntime
    {
        public Dictionary<string, string?> GetItems { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "localStorage.getItem" && args?.Length > 0)
            {
                var key = args[0]?.ToString()!;
                if (GetItems.TryGetValue(key, out var value))
                {
                    return new ValueTask<TValue>((TValue)(object?)value!);
                }

                return new ValueTask<TValue>(default(TValue)!);
            }

            return new ValueTask<TValue>(default(TValue)!);
        }
    }
}
