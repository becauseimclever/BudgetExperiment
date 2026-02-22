// <copyright file="UserProfileAuthOffTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Security.Claims;

using Bunit;

using BudgetExperiment.Client.Components.Auth;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Tests for <see cref="UserProfile"/> behavior in auth-off mode.
/// </summary>
public class UserProfileAuthOffTests : BunitContext, IAsyncLifetime
{
    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    [Fact]
    public void UserProfile_HidesEntireComponent_WhenModeIsNone()
    {
        // Arrange
        Services.AddSingleton(new AuthenticationConfigDto { Mode = "none" });
        RegisterAuthenticatedUser();

        // Act
        var cut = Render<UserProfile>();

        // Assert — no markup should be rendered
        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void UserProfile_HidesLoginButton_WhenModeIsNone()
    {
        // Arrange
        Services.AddSingleton(new AuthenticationConfigDto { Mode = "none" });
        RegisterUnauthenticatedUser();

        // Act
        var cut = Render<UserProfile>();

        // Assert — no sign-in button rendered
        Assert.DoesNotContain("Sign in", cut.Markup);
    }

    [Fact]
    public void UserProfile_ShowsContent_WhenModeIsOidc()
    {
        // Arrange
        Services.AddSingleton(new AuthenticationConfigDto { Mode = "oidc" });
        RegisterAuthenticatedUser();

        // Act
        var cut = Render<UserProfile>();

        // Assert — user profile content should be rendered
        Assert.NotEmpty(cut.Markup.Trim());
        Assert.Contains("user-profile", cut.Markup);
    }

    [Fact]
    public void UserProfile_ShowsSignIn_WhenModeIsOidcAndNotAuthenticated()
    {
        // Arrange
        Services.AddSingleton(new AuthenticationConfigDto { Mode = "oidc" });
        RegisterUnauthenticatedUser();

        // Act
        var cut = Render<UserProfile>();

        // Assert
        Assert.Contains("Sign in", cut.Markup);
    }

    private void RegisterAuthenticatedUser()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim("email", "test@test.com"),
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);

        Services.AddSingleton<AuthenticationStateProvider>(
            new FakeAuthStateProvider(authState));
        Services.AddAuthorizationCore();
        Services.AddCascadingAuthenticationState();
        Services.AddSingleton<IAuthorizationService, AlwaysAllowAuthorizationService>();
        Services.AddSingleton<ThemeService>();
    }

    private void RegisterUnauthenticatedUser()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = new AuthenticationState(principal);

        Services.AddSingleton<AuthenticationStateProvider>(
            new FakeAuthStateProvider(authState));
        Services.AddAuthorizationCore();
        Services.AddCascadingAuthenticationState();
        Services.AddSingleton<IAuthorizationService, DenyAllAuthorizationService>();
        Services.AddSingleton<ThemeService>();
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

    /// <summary>
    /// Authorization service that always allows for testing authenticated views.
    /// </summary>
    private sealed class AlwaysAllowAuthorizationService : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object? resource,
            IEnumerable<IAuthorizationRequirement> requirements)
        {
            return Task.FromResult(AuthorizationResult.Success());
        }

        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object? resource,
            string policyName)
        {
            return Task.FromResult(AuthorizationResult.Success());
        }
    }

    /// <summary>
    /// Authorization service that always denies for testing not-authorized views.
    /// </summary>
    private sealed class DenyAllAuthorizationService : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object? resource,
            IEnumerable<IAuthorizationRequirement> requirements)
        {
            return Task.FromResult(AuthorizationResult.Failed());
        }

        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object? resource,
            string policyName)
        {
            return Task.FromResult(AuthorizationResult.Failed());
        }
    }
}
