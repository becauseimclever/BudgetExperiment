// <copyright file="TokenRefreshHandlerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;

using BudgetExperiment.Client.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="TokenRefreshHandler"/> class.
/// </summary>
public sealed class TokenRefreshHandlerTests : IDisposable
{
    private readonly StubTokenProvider _tokenProvider = new();
    private readonly StubNavigationManager _navigation = new();
    private readonly StubToastService _toastService = new();
    private readonly StubFormStateService _formStateService = new();

    /// <summary>
    /// Verifies that non-401 responses are returned unchanged.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendAsync_NonUnauthorized_ReturnsResponseUnchanged()
    {
        // Arrange
        var innerHandler = new StubInnerHandler(HttpStatusCode.OK);
        using var handler = CreateHandler(innerHandler);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };

        // Act
        var response = await client.GetAsync("/api/test");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, innerHandler.CallCount);
    }

    /// <summary>
    /// Verifies that a 401 response triggers a token refresh attempt.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendAsync_Unauthorized_AttemptsTokenRefresh()
    {
        // Arrange
        _tokenProvider.SetNextResult(AccessTokenResultStatus.Success, "new-token");
        var innerHandler = new StubInnerHandler(HttpStatusCode.Unauthorized, HttpStatusCode.OK);
        using var handler = CreateHandler(innerHandler);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };

        // Act
        var response = await client.GetAsync("/api/test");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, innerHandler.CallCount);
        Assert.True(_tokenProvider.WasRefreshRequested);
    }

    /// <summary>
    /// Verifies that a successful refresh retries the original request.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendAsync_RefreshSucceeds_RetriesOriginalRequest()
    {
        // Arrange
        _tokenProvider.SetNextResult(AccessTokenResultStatus.Success, "new-token");
        var innerHandler = new StubInnerHandler(HttpStatusCode.Unauthorized, HttpStatusCode.OK);
        using var handler = CreateHandler(innerHandler);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var content = new StringContent("{\"name\":\"test\"}");

        // Act
        var response = await client.PostAsync("/api/budgets", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, innerHandler.CallCount);

        // The retry should preserve the HTTP method and URI
        Assert.Equal(HttpMethod.Post, innerHandler.LastRequest!.Method);
        Assert.Equal("/api/budgets", innerHandler.LastRequest.RequestUri!.PathAndQuery);
    }

    /// <summary>
    /// Verifies that a failed token refresh shows session expired toast and redirects.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendAsync_RefreshFails_ShowsToastAndRedirects()
    {
        // Arrange
        _tokenProvider.SetNextResult(AccessTokenResultStatus.RequiresRedirect, null);
        var innerHandler = new StubInnerHandler(HttpStatusCode.Unauthorized);
        using var handler = CreateHandler(innerHandler);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        _navigation.SetCurrentUri("https://localhost/budgets");

        // Act
        var response = await client.GetAsync("/api/test");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(_toastService.WarningShown);
        Assert.Contains("Session expired", _toastService.LastMessage!);
    }

    /// <summary>
    /// Verifies that authentication routes are skipped (no refresh attempted).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendAsync_OnAuthenticationRoute_SkipsRefresh()
    {
        // Arrange
        var innerHandler = new StubInnerHandler(HttpStatusCode.Unauthorized);
        using var handler = CreateHandler(innerHandler);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        _navigation.SetCurrentUri("https://localhost/authentication/login");

        // Act
        var response = await client.GetAsync("/api/test");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, innerHandler.CallCount);
        Assert.False(_tokenProvider.WasRefreshRequested);
    }

    /// <summary>
    /// Verifies that concurrent 401 responses only trigger one refresh attempt.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendAsync_ConcurrentUnauthorized_OnlyOneRefreshAttempt()
    {
        // Arrange
        _tokenProvider.SetNextResult(AccessTokenResultStatus.Success, "new-token");
        _tokenProvider.DelayMs = 100;
        var innerHandler = new StubInnerHandler(HttpStatusCode.Unauthorized, HttpStatusCode.OK);
        using var handler = CreateHandler(innerHandler);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };

        // Act — fire two concurrent requests
        var task1 = client.GetAsync("/api/test1");
        var task2 = client.GetAsync("/api/test2");
        await Task.WhenAll(task1, task2);

        // Assert — only one refresh should have been attempted
        Assert.Equal(1, _tokenProvider.RefreshCount);
    }

    /// <summary>
    /// Verifies that the retry request includes the refreshed token in Authorization header.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendAsync_RefreshSucceeds_RetryIncludesNewToken()
    {
        // Arrange
        _tokenProvider.SetNextResult(AccessTokenResultStatus.Success, "refreshed-token-xyz");
        var innerHandler = new StubInnerHandler(HttpStatusCode.Unauthorized, HttpStatusCode.OK);
        using var handler = CreateHandler(innerHandler);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };

        // Act
        await client.GetAsync("/api/test");

        // Assert — the retried request should have the new bearer token
        Assert.NotNull(innerHandler.LastRequest);
        var authHeader = innerHandler.LastRequest!.Headers.Authorization;
        Assert.NotNull(authHeader);
        Assert.Equal("Bearer", authHeader!.Scheme);
        Assert.Equal("refreshed-token-xyz", authHeader.Parameter);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Cleanup handled by using blocks in test methods
    }

    /// <summary>
    /// Verifies that a failed refresh saves form state before showing toast.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendAsync_RefreshFails_SavesFormState()
    {
        // Arrange
        _tokenProvider.SetNextResult(AccessTokenResultStatus.RequiresRedirect, null);
        var innerHandler = new StubInnerHandler(HttpStatusCode.Unauthorized);
        using var handler = CreateHandler(innerHandler);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };

        // Act
        await client.GetAsync("/api/test");

        // Assert
        Assert.True(_formStateService.SaveAllCalled);
    }

    /// <summary>
    /// Verifies that a successful refresh does not save form state.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendAsync_RefreshSucceeds_DoesNotSaveFormState()
    {
        // Arrange
        _tokenProvider.SetNextResult(AccessTokenResultStatus.Success, "new-token");
        var innerHandler = new StubInnerHandler(HttpStatusCode.Unauthorized, HttpStatusCode.OK);
        using var handler = CreateHandler(innerHandler);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };

        // Act
        await client.GetAsync("/api/test");

        // Assert
        Assert.False(_formStateService.SaveAllCalled);
    }

    private TokenRefreshHandler CreateHandler(HttpMessageHandler innerHandler)
    {
        var handler = new TokenRefreshHandler(
            _tokenProvider,
            _navigation,
            _toastService,
            _formStateService);
        handler.InnerHandler = innerHandler;
        return handler;
    }

    /// <summary>
    /// Stub inner handler that returns pre-configured responses.
    /// </summary>
    private sealed class StubInnerHandler : HttpMessageHandler
    {
        private readonly Queue<HttpStatusCode> _responses;
        private int _callCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubInnerHandler"/> class.
        /// </summary>
        /// <param name="responses">Status codes to return in sequence. Last one repeats.</param>
        public StubInnerHandler(params HttpStatusCode[] responses)
        {
            _responses = new Queue<HttpStatusCode>(responses);
        }

        /// <summary>
        /// Gets the number of times SendAsync was called.
        /// </summary>
        public int CallCount => _callCount;

        /// <summary>
        /// Gets the last request that was sent.
        /// </summary>
        public HttpRequestMessage? LastRequest { get; private set; }

        /// <inheritdoc/>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _callCount);
            LastRequest = request;

            var statusCode = _responses.Count > 1
                ? _responses.Dequeue()
                : _responses.Peek();

            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }

    /// <summary>
    /// Stub token provider for testing.
    /// </summary>
    private sealed class StubTokenProvider : IAccessTokenProvider
    {
        private AccessTokenResultStatus _nextStatus = AccessTokenResultStatus.RequiresRedirect;
        private string? _nextToken;
        private int _refreshCount;

        /// <summary>
        /// Gets a value indicating whether a refresh was requested.
        /// </summary>
        public bool WasRefreshRequested => _refreshCount > 0;

        /// <summary>
        /// Gets the number of refresh attempts.
        /// </summary>
        public int RefreshCount => _refreshCount;

        /// <summary>
        /// Gets or sets the delay in milliseconds for simulating async token refresh.
        /// </summary>
        public int DelayMs { get; set; }

        /// <summary>
        /// Sets the next result to return.
        /// </summary>
        /// <param name="status">The status to return.</param>
        /// <param name="token">The token value to return.</param>
        public void SetNextResult(AccessTokenResultStatus status, string? token)
        {
            _nextStatus = status;
            _nextToken = token;
        }

        /// <inheritdoc/>
        public async ValueTask<AccessTokenResult> RequestAccessToken()
        {
            Interlocked.Increment(ref _refreshCount);

            if (DelayMs > 0)
            {
                await Task.Delay(DelayMs);
            }

            return CreateResult();
        }

        /// <inheritdoc/>
        public async ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options)
        {
            Interlocked.Increment(ref _refreshCount);

            if (DelayMs > 0)
            {
                await Task.Delay(DelayMs);
            }

            return CreateResult();
        }

        private AccessTokenResult CreateResult()
        {
            if (_nextStatus == AccessTokenResultStatus.Success && _nextToken is not null)
            {
                var token = new AccessToken { Value = _nextToken, Expires = DateTimeOffset.UtcNow.AddHours(1) };
                return new AccessTokenResult(AccessTokenResultStatus.Success, token, null, null);
            }

            return new AccessTokenResult(
                _nextStatus,
                new AccessToken(),
                "authentication/login",
                new InteractiveRequestOptions { Interaction = InteractionType.SignIn, ReturnUrl = "/" });
        }
    }

    /// <summary>
    /// Stub NavigationManager for testing.
    /// </summary>
    private sealed class StubNavigationManager : NavigationManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StubNavigationManager"/> class.
        /// </summary>
        public StubNavigationManager()
        {
            Initialize("https://localhost/", "https://localhost/");
        }

        /// <summary>
        /// Sets the current URI for testing.
        /// </summary>
        /// <param name="uri">The URI to set as current.</param>
        public void SetCurrentUri(string uri)
        {
            Uri = uri;
        }

        /// <inheritdoc/>
        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            // No-op in tests
        }
    }

    /// <summary>
    /// Stub toast service for testing.
    /// </summary>
    private sealed class StubToastService : IToastService
    {
        /// <inheritdoc/>
        public event Action? OnChange;

        /// <summary>
        /// Gets a value indicating whether a warning was shown.
        /// </summary>
        public bool WarningShown { get; private set; }

        /// <summary>
        /// Gets the last message shown.
        /// </summary>
        public string? LastMessage { get; private set; }

        /// <inheritdoc/>
        public IReadOnlyList<ToastItem> Toasts { get; } = [];

        /// <inheritdoc/>
        public void ShowSuccess(string message, string? title = null)
        {
        }

        /// <inheritdoc/>
        public void ShowError(string message, string? title = null)
        {
        }

        /// <inheritdoc/>
        public void ShowInfo(string message, string? title = null)
        {
        }

        /// <inheritdoc/>
        public void ShowWarning(string message, string? title = null)
        {
            WarningShown = true;
            LastMessage = message;
            OnChange?.Invoke();
        }

        /// <inheritdoc/>
        public void Remove(Guid id)
        {
        }
    }

    /// <summary>
    /// Stub form state service for testing.
    /// </summary>
    private sealed class StubFormStateService : IFormStateService
    {
        /// <summary>
        /// Gets a value indicating whether SaveAllAsync was called.
        /// </summary>
        public bool SaveAllCalled { get; private set; }

        /// <inheritdoc/>
        public void RegisterForm(string formKey, Func<object?> dataProvider)
        {
        }

        /// <inheritdoc/>
        public void UnregisterForm(string formKey)
        {
        }

        /// <inheritdoc/>
        public Task SaveAllAsync()
        {
            SaveAllCalled = true;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<T?> RestoreAsync<T>(string formKey)
        {
            return Task.FromResult(default(T));
        }

        /// <inheritdoc/>
        public Task ClearAsync(string formKey)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<bool> HasSavedStateAsync(string formKey)
        {
            return Task.FromResult(false);
        }
    }
}
