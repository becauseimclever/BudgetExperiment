// <copyright file="GenericOidcProviderTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Microsoft.Extensions.Configuration;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Unit tests for Generic OIDC provider configuration and settings resolution.
/// Covers Keycloak, Auth0, Okta, and other standard OIDC providers.
/// </summary>
public sealed class GenericOidcProviderTests
{
    /// <summary>
    /// GenericOidcProviderOptions binds Authority from configuration.
    /// </summary>
    [Fact]
    public void Binds_Oidc_Authority_From_Config()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Provider"] = "OIDC",
                ["Authentication:Oidc:Authority"] = "https://keycloak.example.com/realms/master",
            })
            .Build();

        var options = new AuthenticationOptions();
        config.GetSection(AuthenticationOptions.SectionName).Bind(options);

        // Assert
        Assert.Equal("https://keycloak.example.com/realms/master", options.Oidc.Authority);
    }

    /// <summary>
    /// GenericOidcProviderOptions binds ClientId from configuration.
    /// </summary>
    [Fact]
    public void Binds_Oidc_ClientId_From_Config()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Oidc:ClientId"] = "budget-experiment",
            })
            .Build();

        var options = new AuthenticationOptions();
        config.GetSection(AuthenticationOptions.SectionName).Bind(options);

        // Assert
        Assert.Equal("budget-experiment", options.Oidc.ClientId);
    }

    /// <summary>
    /// GenericOidcProviderOptions binds ClientSecret from configuration.
    /// </summary>
    [Fact]
    public void Binds_Oidc_ClientSecret_From_Config()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Oidc:ClientSecret"] = "oidc-secret-123",
            })
            .Build();

        var options = new AuthenticationOptions();
        config.GetSection(AuthenticationOptions.SectionName).Bind(options);

        // Assert
        Assert.Equal("oidc-secret-123", options.Oidc.ClientSecret);
    }

    /// <summary>
    /// GenericOidcProviderOptions binds Audience from configuration.
    /// </summary>
    [Fact]
    public void Binds_Oidc_Audience_From_Config()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Oidc:Audience"] = "budget-api",
            })
            .Build();

        var options = new AuthenticationOptions();
        config.GetSection(AuthenticationOptions.SectionName).Bind(options);

        // Assert
        Assert.Equal("budget-api", options.Oidc.Audience);
    }

    /// <summary>
    /// GenericOidcProviderOptions binds RequireHttpsMetadata from configuration.
    /// </summary>
    [Fact]
    public void Binds_Oidc_RequireHttpsMetadata_From_Config()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Oidc:RequireHttpsMetadata"] = "false",
            })
            .Build();

        var options = new AuthenticationOptions();
        config.GetSection(AuthenticationOptions.SectionName).Bind(options);

        // Assert
        Assert.False(options.Oidc.RequireHttpsMetadata);
    }

    /// <summary>
    /// GenericOidcProviderOptions binds custom Scopes from configuration.
    /// </summary>
    [Fact]
    public void Binds_Oidc_CustomScopes_From_Config()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Oidc:Scopes:0"] = "openid",
                ["Authentication:Oidc:Scopes:1"] = "profile",
                ["Authentication:Oidc:Scopes:2"] = "email",
                ["Authentication:Oidc:Scopes:3"] = "offline_access",
            })
            .Build();

        var options = new AuthenticationOptions();
        config.GetSection(AuthenticationOptions.SectionName).Bind(options);

        // Assert — config binding merges with defaults; verify custom scope is present
        Assert.Contains("openid", options.Oidc.Scopes);
        Assert.Contains("profile", options.Oidc.Scopes);
        Assert.Contains("email", options.Oidc.Scopes);
        Assert.Contains("offline_access", options.Oidc.Scopes);
    }

    /// <summary>
    /// GenericOidcProviderOptions binds ClaimMappings from configuration.
    /// </summary>
    [Fact]
    public void Binds_Oidc_ClaimMappings_From_Config()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Oidc:ClaimMappings:name"] = "preferred_username",
                ["Authentication:Oidc:ClaimMappings:nickname"] = "display_name",
            })
            .Build();

        var options = new AuthenticationOptions();
        config.GetSection(AuthenticationOptions.SectionName).Bind(options);

        // Assert
        Assert.Equal(2, options.Oidc.ClaimMappings.Count);
        Assert.Equal("preferred_username", options.Oidc.ClaimMappings["name"]);
        Assert.Equal("display_name", options.Oidc.ClaimMappings["nickname"]);
    }

    /// <summary>
    /// Oidc Authority defaults to empty string.
    /// </summary>
    [Fact]
    public void Oidc_Authority_Defaults_Empty()
    {
        var options = new GenericOidcProviderOptions();

        Assert.Equal(string.Empty, options.Authority);
    }

    /// <summary>
    /// Oidc ClientId defaults to empty string.
    /// </summary>
    [Fact]
    public void Oidc_ClientId_Defaults_Empty()
    {
        var options = new GenericOidcProviderOptions();

        Assert.Equal(string.Empty, options.ClientId);
    }

    /// <summary>
    /// Oidc ClientSecret defaults to empty string.
    /// </summary>
    [Fact]
    public void Oidc_ClientSecret_Defaults_Empty()
    {
        var options = new GenericOidcProviderOptions();

        Assert.Equal(string.Empty, options.ClientSecret);
    }

    /// <summary>
    /// Oidc Scopes default to openid, profile, email.
    /// </summary>
    [Fact]
    public void Oidc_Scopes_DefaultToStandard()
    {
        var options = new GenericOidcProviderOptions();

        Assert.Equal(3, options.Scopes.Length);
        Assert.Contains("openid", options.Scopes);
        Assert.Contains("profile", options.Scopes);
        Assert.Contains("email", options.Scopes);
    }

    /// <summary>
    /// Oidc Audience defaults to empty string.
    /// </summary>
    [Fact]
    public void Oidc_Audience_Defaults_Empty()
    {
        var options = new GenericOidcProviderOptions();

        Assert.Equal(string.Empty, options.Audience);
    }

    /// <summary>
    /// Oidc RequireHttpsMetadata defaults to true.
    /// </summary>
    [Fact]
    public void Oidc_RequireHttpsMetadata_Defaults_True()
    {
        var options = new GenericOidcProviderOptions();

        Assert.True(options.RequireHttpsMetadata);
    }

    /// <summary>
    /// Oidc ClaimMappings defaults to empty dictionary.
    /// </summary>
    [Fact]
    public void Oidc_ClaimMappings_Defaults_Empty()
    {
        var options = new GenericOidcProviderOptions();

        Assert.Empty(options.ClaimMappings);
    }

    /// <summary>
    /// ResolveProviderSettings returns Oidc authority when Provider is OIDC.
    /// </summary>
    [Fact]
    public void ResolveProviderSettings_ReturnsOidcAuthority_WhenProviderIsOidc()
    {
        // Arrange
        var authOptions = new AuthenticationOptions
        {
            Provider = AuthProviderConstants.Oidc,
            Oidc = new GenericOidcProviderOptions
            {
                Authority = "https://keycloak.example.com/realms/master",
                ClientId = "budget-experiment",
            },
        };

        // Act
        var (authority, _, _) = AuthenticationOptions.ResolveProviderSettings(authOptions);

        // Assert
        Assert.Equal("https://keycloak.example.com/realms/master", authority);
    }

    /// <summary>
    /// ResolveProviderSettings returns Oidc Audience when Provider is OIDC and Audience is set.
    /// </summary>
    [Fact]
    public void ResolveProviderSettings_ReturnsAudience_WhenProviderIsOidc()
    {
        // Arrange
        var authOptions = new AuthenticationOptions
        {
            Provider = AuthProviderConstants.Oidc,
            Oidc = new GenericOidcProviderOptions
            {
                Authority = "https://auth0.example.com",
                Audience = "budget-api",
            },
        };

        // Act
        var (_, audience, _) = AuthenticationOptions.ResolveProviderSettings(authOptions);

        // Assert
        Assert.Equal("budget-api", audience);
    }

    /// <summary>
    /// ResolveProviderSettings returns RequireHttpsMetadata from Oidc options when Provider is OIDC.
    /// </summary>
    [Fact]
    public void ResolveProviderSettings_ReturnsRequireHttps_WhenProviderIsOidc()
    {
        // Arrange
        var authOptions = new AuthenticationOptions
        {
            Provider = AuthProviderConstants.Oidc,
            Oidc = new GenericOidcProviderOptions
            {
                Authority = "https://auth.example.com",
                RequireHttpsMetadata = false,
            },
        };

        // Act
        var (_, _, requireHttps) = AuthenticationOptions.ResolveProviderSettings(authOptions);

        // Assert
        Assert.False(requireHttps);
    }

    /// <summary>
    /// ResolveProviderSettings is case-insensitive for OIDC provider name.
    /// </summary>
    [Fact]
    public void ResolveProviderSettings_IsCaseInsensitive_ForOidcProvider()
    {
        // Arrange
        var authOptions = new AuthenticationOptions
        {
            Provider = "oidc",
            Oidc = new GenericOidcProviderOptions
            {
                Authority = "https://keycloak.example.com",
                Audience = "test-audience",
            },
        };

        // Act
        var (authority, audience, _) = AuthenticationOptions.ResolveProviderSettings(authOptions);

        // Assert
        Assert.Equal("https://keycloak.example.com", authority);
        Assert.Equal("test-audience", audience);
    }

    /// <summary>
    /// Provider constant "OIDC" is correctly defined.
    /// </summary>
    [Fact]
    public void ProviderConstant_Oidc_IsCorrect()
    {
        Assert.Equal("OIDC", AuthProviderConstants.Oidc);
    }
}
