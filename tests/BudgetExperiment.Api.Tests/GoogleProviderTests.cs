// <copyright file="GoogleProviderTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Microsoft.Extensions.Configuration;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Unit tests for Google OAuth provider configuration and settings resolution.
/// </summary>
public sealed class GoogleProviderTests
{
    /// <summary>
    /// The Google OIDC authority is always <c>https://accounts.google.com</c>.
    /// </summary>
    [Fact]
    public void GoogleAuthority_IsWellKnown()
    {
        Assert.Equal("https://accounts.google.com", GoogleProviderOptions.Authority);
    }

    /// <summary>
    /// GoogleProviderOptions binds ClientId from configuration.
    /// </summary>
    [Fact]
    public void Binds_Google_ClientId_From_Config()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Provider"] = "Google",
                ["Authentication:Google:ClientId"] = "123456789-abc.apps.googleusercontent.com",
            })
            .Build();

        var options = new AuthenticationOptions();
        config.GetSection(AuthenticationOptions.SectionName).Bind(options);

        // Assert
        Assert.Equal("123456789-abc.apps.googleusercontent.com", options.Google.ClientId);
    }

    /// <summary>
    /// GoogleProviderOptions binds ClientSecret from configuration.
    /// </summary>
    [Fact]
    public void Binds_Google_ClientSecret_From_Config()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Google:ClientSecret"] = "GOCSPX-test-secret",
            })
            .Build();

        var options = new AuthenticationOptions();
        config.GetSection(AuthenticationOptions.SectionName).Bind(options);

        // Assert
        Assert.Equal("GOCSPX-test-secret", options.Google.ClientSecret);
    }

    /// <summary>
    /// Google ClientId defaults to empty string.
    /// </summary>
    [Fact]
    public void Google_ClientId_Defaults_Empty()
    {
        var options = new GoogleProviderOptions();

        Assert.Equal(string.Empty, options.ClientId);
    }

    /// <summary>
    /// Google ClientSecret defaults to empty string.
    /// </summary>
    [Fact]
    public void Google_ClientSecret_Defaults_Empty()
    {
        var options = new GoogleProviderOptions();

        Assert.Equal(string.Empty, options.ClientSecret);
    }

    /// <summary>
    /// ResolveProviderSettings returns Google authority when Provider is Google.
    /// </summary>
    [Fact]
    public void ResolveProviderSettings_ReturnsGoogleAuthority_WhenProviderIsGoogle()
    {
        // Arrange
        var authOptions = new AuthenticationOptions
        {
            Provider = AuthProviderConstants.Google,
            Google = new GoogleProviderOptions
            {
                ClientId = "test-client-id.apps.googleusercontent.com",
            },
        };

        // Act
        var (authority, _, _) = AuthenticationOptions.ResolveProviderSettings(authOptions);

        // Assert
        Assert.Equal("https://accounts.google.com", authority);
    }

    /// <summary>
    /// ResolveProviderSettings returns Google ClientId as audience when Provider is Google.
    /// </summary>
    [Fact]
    public void ResolveProviderSettings_ReturnsClientIdAsAudience_WhenProviderIsGoogle()
    {
        // Arrange
        var authOptions = new AuthenticationOptions
        {
            Provider = AuthProviderConstants.Google,
            Google = new GoogleProviderOptions
            {
                ClientId = "test-client-id.apps.googleusercontent.com",
            },
        };

        // Act
        var (_, audience, _) = AuthenticationOptions.ResolveProviderSettings(authOptions);

        // Assert
        Assert.Equal("test-client-id.apps.googleusercontent.com", audience);
    }

    /// <summary>
    /// ResolveProviderSettings returns RequireHttpsMetadata=true for Google (always HTTPS).
    /// </summary>
    [Fact]
    public void ResolveProviderSettings_RequiresHttps_WhenProviderIsGoogle()
    {
        // Arrange
        var authOptions = new AuthenticationOptions
        {
            Provider = AuthProviderConstants.Google,
            Google = new GoogleProviderOptions
            {
                ClientId = "test-client-id",
            },
        };

        // Act
        var (_, _, requireHttps) = AuthenticationOptions.ResolveProviderSettings(authOptions);

        // Assert
        Assert.True(requireHttps);
    }

    /// <summary>
    /// ResolveProviderSettings still returns Authentik settings for the default provider.
    /// </summary>
    [Fact]
    public void ResolveProviderSettings_ReturnsAuthentikSettings_ForDefaultProvider()
    {
        // Arrange
        var authOptions = new AuthenticationOptions
        {
            Authentik = new AuthentikProviderOptions
            {
                Authority = "https://auth.example.com/",
                Audience = "budget-experiment",
                RequireHttpsMetadata = false,
            },
        };

        // Act
        var (authority, audience, requireHttps) = AuthenticationOptions.ResolveProviderSettings(authOptions);

        // Assert
        Assert.Equal("https://auth.example.com/", authority);
        Assert.Equal("budget-experiment", audience);
        Assert.False(requireHttps);
    }

    /// <summary>
    /// ValidateOidcAuthority throws with a provider-agnostic message (no Authentik-specific text).
    /// </summary>
    [Fact]
    public void ValidateOidcAuthority_ErrorMessage_IsProviderAgnostic()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => AuthenticationOptions.ValidateOidcAuthority(string.Empty));

        Assert.DoesNotContain("Authentik", ex.Message);
        Assert.Contains("Authority", ex.Message);
        Assert.Contains("Mode", ex.Message);
    }

    /// <summary>
    /// Provider constant "Google" is correctly defined.
    /// </summary>
    [Fact]
    public void AuthProviderConstants_Google_Value()
    {
        Assert.Equal("Google", AuthProviderConstants.Google);
    }
}
