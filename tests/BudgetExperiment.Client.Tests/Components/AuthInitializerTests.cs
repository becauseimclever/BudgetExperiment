// <copyright file="AuthInitializerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Security.Claims;

using Bunit;

using BudgetExperiment.Client.Components.Auth;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the AuthInitializer component.
/// </summary>
public class AuthInitializerTests : BunitContext
{
    [Fact]
    public void AuthInitializer_RendersContent_WhenAuthenticated()
    {
        // Arrange
        var authState = CreateAuthState(isAuthenticated: true);
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider(authState));
        Services.AddSingleton<IJSRuntime>(new FakeJSRuntime());

        // Act
        var cut = Render<AuthInitializer>(parameters => parameters
            .AddChildContent("<p>Protected Content</p>"));

        // Assert
        cut.MarkupMatches("<p>Protected Content</p>");
    }

    [Fact]
    public void AuthInitializer_DoesNotRenderContent_WhenNotAuthenticated()
    {
        // Arrange
        var authState = CreateAuthState(isAuthenticated: false);
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider(authState));
        Services.AddSingleton<IJSRuntime>(new FakeJSRuntime());

        // bUnit provides a FakeNavigationManager - just use the default

        // Act
        var cut = Render<AuthInitializer>(parameters => parameters
            .AddChildContent("<p>Protected Content</p>"));

        // Assert - nothing rendered (overlay stays visible, redirect will happen)
        cut.MarkupMatches(string.Empty);
    }

    [Fact]
    public void AuthInitializer_RedirectsToLogin_WhenNotAuthenticated()
    {
        // Arrange
        var authState = CreateAuthState(isAuthenticated: false);
        var fakeNav = new TrackingNavigationManager();
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider(authState));
        Services.AddSingleton<IJSRuntime>(new FakeJSRuntime());
        Services.AddSingleton<Microsoft.AspNetCore.Components.NavigationManager>(fakeNav);

        // Act
        var cut = Render<AuthInitializer>(parameters => parameters
            .AddChildContent("<p>Protected Content</p>"));

        // Assert
        Assert.True(fakeNav.NavigatedUri?.Contains("authentication/login") == true, $"Expected navigation to login, got: {fakeNav.NavigatedUri}");
        Assert.True(fakeNav.ForceLoad, "Expected forceLoad to be true");
    }

    [Fact]
    public void AuthInitializer_IncludesReturnUrl_WhenRedirecting()
    {
        // Arrange
        var authState = CreateAuthState(isAuthenticated: false);
        var fakeNav = new TrackingNavigationManager("https://localhost/calendar");
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider(authState));
        Services.AddSingleton<IJSRuntime>(new FakeJSRuntime());
        Services.AddSingleton<Microsoft.AspNetCore.Components.NavigationManager>(fakeNav);

        // Act
        var cut = Render<AuthInitializer>(parameters => parameters
            .AddChildContent("<p>Protected Content</p>"));

        // Assert - returnUrl should be encoded and contain the original URL
        Assert.True(fakeNav.NavigatedUri?.Contains("returnUrl=") == true, $"Expected returnUrl, got: {fakeNav.NavigatedUri}");
    }

    [Fact]
    public void AuthInitializer_CallsHideOverlay_WhenAuthenticated()
    {
        // Arrange
        var authState = CreateAuthState(isAuthenticated: true);
        var fakeJs = new FakeJSRuntime();
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider(authState));
        Services.AddSingleton<IJSRuntime>(fakeJs);

        // Act
        var cut = Render<AuthInitializer>(parameters => parameters
            .AddChildContent("<p>Protected Content</p>"));

        // Assert
        Assert.True(fakeJs.HideLoadingOverlayCalled);
    }

    [Fact]
    public void AuthInitializer_DoesNotCallHideOverlay_WhenNotAuthenticated()
    {
        // Arrange
        var authState = CreateAuthState(isAuthenticated: false);
        var fakeJs = new FakeJSRuntime();
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider(authState));
        Services.AddSingleton<IJSRuntime>(fakeJs);

        // Act
        var cut = Render<AuthInitializer>(parameters => parameters
            .AddChildContent("<p>Protected Content</p>"));

        // Assert - overlay should stay visible during redirect
        Assert.False(fakeJs.HideLoadingOverlayCalled);
    }

    [Fact]
    public void AuthInitializer_SkipsAuthCheck_ForAuthenticationRoutes()
    {
        // Arrange
        var authState = CreateAuthState(isAuthenticated: false);
        var fakeJs = new FakeJSRuntime();
        var fakeNav = new TrackingNavigationManager("https://localhost/authentication/login-callback");
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider(authState));
        Services.AddSingleton<IJSRuntime>(fakeJs);
        Services.AddSingleton<Microsoft.AspNetCore.Components.NavigationManager>(fakeNav);

        // Act
        var cut = Render<AuthInitializer>(parameters => parameters
            .AddChildContent("<p>Auth Callback Page</p>"));

        // Assert - should render content even if not authenticated (auth routes handle themselves)
        cut.MarkupMatches("<p>Auth Callback Page</p>");

        // No redirect should have occurred
        Assert.Null(fakeNav.NavigatedUri);
    }

    private static AuthenticationState CreateAuthState(bool isAuthenticated)
    {
        ClaimsPrincipal user;
        if (isAuthenticated)
        {
            var claims = new[] { new Claim(ClaimTypes.Name, "testuser@test.com") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            user = new ClaimsPrincipal(identity);
        }
        else
        {
            user = new ClaimsPrincipal(new ClaimsIdentity());
        }

        return new AuthenticationState(user);
    }

    private sealed class FakeAuthStateProvider : AuthenticationStateProvider
    {
        private readonly AuthenticationState _state;

        public FakeAuthStateProvider(AuthenticationState state)
        {
            _state = state;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return Task.FromResult(_state);
        }
    }

    private sealed class FakeJSRuntime : IJSRuntime
    {
        public bool HideLoadingOverlayCalled { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier == "hideLoadingOverlay")
            {
                HideLoadingOverlayCalled = true;
            }

            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, args);
        }
    }

    private sealed class TrackingNavigationManager : Microsoft.AspNetCore.Components.NavigationManager
    {
        public TrackingNavigationManager(string uri = "https://localhost/")
        {
            Initialize(uri, uri);
        }

        public string? NavigatedUri { get; private set; }

        public bool ForceLoad { get; private set; }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            NavigatedUri = uri;
            ForceLoad = forceLoad;
        }
    }
}
