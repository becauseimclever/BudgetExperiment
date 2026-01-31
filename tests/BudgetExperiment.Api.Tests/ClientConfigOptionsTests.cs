// <copyright file="ClientConfigOptionsTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Unit tests for <see cref="ClientConfigOptions"/> mapping.
/// </summary>
public sealed class ClientConfigOptionsTests
{
    /// <summary>
    /// ToDto returns correct authentication mode when set to oidc.
    /// </summary>
    [Fact]
    public void ToDto_WhenOidcMode_ReturnsOidcConfig()
    {
        // Arrange
        var options = new ClientConfigOptions
        {
            AuthMode = "oidc",
            OidcAuthority = "https://auth.example.com/",
            OidcClientId = "test-client-id",
            OidcResponseType = "code",
            OidcScopes = ["openid", "profile"],
            OidcPostLogoutRedirectUri = "/logout",
            OidcRedirectUri = "auth/callback",
        };

        // Act
        var dto = options.ToDto();

        // Assert
        Assert.Equal("oidc", dto.Authentication.Mode);
        Assert.NotNull(dto.Authentication.Oidc);
        Assert.Equal("https://auth.example.com/", dto.Authentication.Oidc.Authority);
        Assert.Equal("test-client-id", dto.Authentication.Oidc.ClientId);
        Assert.Equal("code", dto.Authentication.Oidc.ResponseType);
        Assert.Equal(["openid", "profile"], dto.Authentication.Oidc.Scopes);
        Assert.Equal("/logout", dto.Authentication.Oidc.PostLogoutRedirectUri);
        Assert.Equal("auth/callback", dto.Authentication.Oidc.RedirectUri);
    }

    /// <summary>
    /// ToDto returns null Oidc when mode is none.
    /// </summary>
    [Fact]
    public void ToDto_WhenNoneMode_ReturnsNullOidc()
    {
        // Arrange
        var options = new ClientConfigOptions
        {
            AuthMode = "none",
            OidcAuthority = "https://auth.example.com/",
            OidcClientId = "test-client-id",
        };

        // Act
        var dto = options.ToDto();

        // Assert
        Assert.Equal("none", dto.Authentication.Mode);
        Assert.Null(dto.Authentication.Oidc);
    }

    /// <summary>
    /// ToDto is case-insensitive for oidc mode.
    /// </summary>
    [Theory]
    [InlineData("OIDC")]
    [InlineData("Oidc")]
    [InlineData("oidc")]
    public void ToDto_OidcModeIsCaseInsensitive(string mode)
    {
        // Arrange
        var options = new ClientConfigOptions
        {
            AuthMode = mode,
            OidcAuthority = "https://auth.example.com/",
            OidcClientId = "test-client-id",
        };

        // Act
        var dto = options.ToDto();

        // Assert
        Assert.NotNull(dto.Authentication.Oidc);
    }

    /// <summary>
    /// ToDto uses default values for OIDC settings when not explicitly set.
    /// </summary>
    [Fact]
    public void ToDto_UsesDefaultOidcValues()
    {
        // Arrange
        var options = new ClientConfigOptions
        {
            AuthMode = "oidc",
            OidcAuthority = "https://auth.example.com/",
            OidcClientId = "test-client-id",
        };

        // Act
        var dto = options.ToDto();

        // Assert
        Assert.NotNull(dto.Authentication.Oidc);
        Assert.Equal("code", dto.Authentication.Oidc.ResponseType);
        Assert.Equal(["openid", "profile", "email"], dto.Authentication.Oidc.Scopes);
        Assert.Equal("/", dto.Authentication.Oidc.PostLogoutRedirectUri);
        Assert.Equal("authentication/login-callback", dto.Authentication.Oidc.RedirectUri);
    }
}
