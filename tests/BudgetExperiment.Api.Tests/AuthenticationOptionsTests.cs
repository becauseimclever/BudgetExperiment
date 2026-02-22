// <copyright file="AuthenticationOptionsTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Microsoft.Extensions.Configuration;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Unit tests for <see cref="AuthenticationOptions"/> configuration binding and backward compatibility.
/// </summary>
public sealed class AuthenticationOptionsTests
{
    /// <summary>
    /// Default Mode is OIDC when no configuration is provided.
    /// </summary>
    [Fact]
    public void Defaults_Mode_Is_Oidc()
    {
        // Arrange
        var options = new AuthenticationOptions();

        // Assert
        Assert.Equal(AuthModeConstants.Oidc, options.Mode);
    }

    /// <summary>
    /// Default Provider is Authentik when no configuration is provided.
    /// </summary>
    [Fact]
    public void Defaults_Provider_Is_Authentik()
    {
        // Arrange
        var options = new AuthenticationOptions();

        // Assert
        Assert.Equal(AuthProviderConstants.Authentik, options.Provider);
    }

    /// <summary>
    /// Nested Authentik options are non-null by default.
    /// </summary>
    [Fact]
    public void Defaults_Authentik_Options_Are_NonNull()
    {
        // Arrange
        var options = new AuthenticationOptions();

        // Assert
        Assert.NotNull(options.Authentik);
    }

    /// <summary>
    /// Binds correctly from IConfiguration using the Authentication section,
    /// simulating existing env vars from docker-compose.pi.yml.
    /// </summary>
    [Fact]
    public void Binds_From_Existing_AuthentikEnvVars()
    {
        // Arrange — simulate docker-compose.pi.yml env variables
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authentik:Authority"] = "https://auth.example.com/application/o/budget/",
                ["Authentication:Authentik:Audience"] = "budget-experiment",
                ["Authentication:Authentik:RequireHttpsMetadata"] = "true",
            })
            .Build();

        var options = new AuthenticationOptions();
        config.GetSection(AuthenticationOptions.SectionName).Bind(options);

        // Assert — Mode/Provider defaulted, Authentik nested properties bound
        Assert.Equal(AuthModeConstants.Oidc, options.Mode);
        Assert.Equal(AuthProviderConstants.Authentik, options.Provider);
        Assert.Equal("https://auth.example.com/application/o/budget/", options.Authentik.Authority);
        Assert.Equal("budget-experiment", options.Authentik.Audience);
        Assert.True(options.Authentik.RequireHttpsMetadata);
    }

    /// <summary>
    /// Binds Mode and Provider from configuration when explicitly set.
    /// </summary>
    [Fact]
    public void Binds_Explicit_Mode_And_Provider()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Mode"] = "None",
                ["Authentication:Provider"] = "Google",
            })
            .Build();

        var options = new AuthenticationOptions();
        config.GetSection(AuthenticationOptions.SectionName).Bind(options);

        // Assert
        Assert.Equal("None", options.Mode);
        Assert.Equal("Google", options.Provider);
    }

    /// <summary>
    /// Binds Google provider options from configuration.
    /// </summary>
    [Fact]
    public void Binds_Google_Provider_Options()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Google:ClientId"] = "google-client-id",
                ["Authentication:Google:ClientSecret"] = "google-secret",
            })
            .Build();

        var options = new AuthenticationOptions();
        config.GetSection(AuthenticationOptions.SectionName).Bind(options);

        // Assert
        Assert.Equal("google-client-id", options.Google.ClientId);
        Assert.Equal("google-secret", options.Google.ClientSecret);
    }

    /// <summary>
    /// Binds Microsoft provider options from configuration.
    /// </summary>
    [Fact]
    public void Binds_Microsoft_Provider_Options()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Microsoft:ClientId"] = "ms-client-id",
                ["Authentication:Microsoft:TenantId"] = "my-tenant",
            })
            .Build();

        var options = new AuthenticationOptions();
        config.GetSection(AuthenticationOptions.SectionName).Bind(options);

        // Assert
        Assert.Equal("ms-client-id", options.Microsoft.ClientId);
        Assert.Equal("my-tenant", options.Microsoft.TenantId);
    }

    /// <summary>
    /// Binds generic OIDC provider options from configuration.
    /// </summary>
    [Fact]
    public void Binds_GenericOidc_Provider_Options()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Oidc:Authority"] = "https://keycloak.example.com/realms/master",
                ["Authentication:Oidc:ClientId"] = "budget-experiment",
                ["Authentication:Oidc:ClientSecret"] = "keycloak-secret",
                ["Authentication:Oidc:Audience"] = "budget-api",
                ["Authentication:Oidc:RequireHttpsMetadata"] = "false",
            })
            .Build();

        var options = new AuthenticationOptions();
        config.GetSection(AuthenticationOptions.SectionName).Bind(options);

        // Assert
        Assert.Equal("https://keycloak.example.com/realms/master", options.Oidc.Authority);
        Assert.Equal("budget-experiment", options.Oidc.ClientId);
        Assert.Equal("keycloak-secret", options.Oidc.ClientSecret);
        Assert.Equal("budget-api", options.Oidc.Audience);
        Assert.False(options.Oidc.RequireHttpsMetadata);
    }

    /// <summary>
    /// Microsoft TenantId defaults to "common".
    /// </summary>
    [Fact]
    public void Microsoft_TenantId_Defaults_To_Common()
    {
        // Arrange
        var options = new AuthenticationOptions();

        // Assert
        Assert.Equal("common", options.Microsoft.TenantId);
    }

    /// <summary>
    /// Generic OIDC default scopes include openid, profile, email.
    /// </summary>
    [Fact]
    public void GenericOidc_Default_Scopes()
    {
        // Arrange
        var options = new AuthenticationOptions();

        // Assert
        Assert.Equal(new[] { "openid", "profile", "email" }, options.Oidc.Scopes);
    }

    /// <summary>
    /// Legacy AuthentikOptions class still binds from the same config section.
    /// Ensures backward compatibility for IOptions&lt;AuthentikOptions&gt; consumers.
    /// </summary>
    [Fact]
    public void Legacy_AuthentikOptions_Still_Binds()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authentik:Authority"] = "https://auth.example.com/application/o/budget/",
                ["Authentication:Authentik:Audience"] = "budget-experiment",
                ["Authentication:Authentik:ClientId"] = "my-client",
                ["Authentication:Authentik:RequireHttpsMetadata"] = "false",
            })
            .Build();

#pragma warning disable CS0618 // Type or member is obsolete
        var options = new AuthentikOptions();
        config.GetSection(AuthentikOptions.SectionName).Bind(options);
#pragma warning restore CS0618

        // Assert
        Assert.Equal("https://auth.example.com/application/o/budget/", options.Authority);
        Assert.Equal("budget-experiment", options.Audience);
        Assert.Equal("my-client", options.ClientId);
        Assert.False(options.RequireHttpsMetadata);
    }

    /// <summary>
    /// AuthenticationOptions.ResolveEffectiveMode returns None when legacy Enabled=false is set
    /// and Mode is not explicitly configured.
    /// </summary>
    [Fact]
    public void ResolveEffectiveMode_LegacyEnabledFalse_ReturnsNone()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authentik:Enabled"] = "false",
                ["Authentication:Authentik:Authority"] = "https://auth.example.com/",
            })
            .Build();

        // Act
        var effectiveMode = AuthenticationOptions.ResolveEffectiveMode(config);

        // Assert
        Assert.Equal(AuthModeConstants.None, effectiveMode);
    }

    /// <summary>
    /// AuthenticationOptions.ResolveEffectiveMode returns OIDC when legacy Enabled=true is set
    /// and Mode is not explicitly configured.
    /// </summary>
    [Fact]
    public void ResolveEffectiveMode_LegacyEnabledTrue_ReturnsOidc()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authentik:Enabled"] = "true",
                ["Authentication:Authentik:Authority"] = "https://auth.example.com/",
            })
            .Build();

        // Act
        var effectiveMode = AuthenticationOptions.ResolveEffectiveMode(config);

        // Assert
        Assert.Equal(AuthModeConstants.Oidc, effectiveMode);
    }

    /// <summary>
    /// AuthenticationOptions.ResolveEffectiveMode returns OIDC when neither Mode
    /// nor Enabled is set (default behavior, backward compat).
    /// </summary>
    [Fact]
    public void ResolveEffectiveMode_NothingSet_DefaultsToOidc()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authentik:Authority"] = "https://auth.example.com/",
            })
            .Build();

        // Act
        var effectiveMode = AuthenticationOptions.ResolveEffectiveMode(config);

        // Assert
        Assert.Equal(AuthModeConstants.Oidc, effectiveMode);
    }

    /// <summary>
    /// AuthenticationOptions.ResolveEffectiveMode returns explicit Mode when set,
    /// even if legacy Enabled flag contradicts it.
    /// </summary>
    [Fact]
    public void ResolveEffectiveMode_ExplicitMode_TakesPrecedenceOverEnabled()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Mode"] = "None",
                ["Authentication:Authentik:Enabled"] = "true", // contradicts Mode
            })
            .Build();

        // Act
        var effectiveMode = AuthenticationOptions.ResolveEffectiveMode(config);

        // Assert — explicit Mode wins
        Assert.Equal(AuthModeConstants.None, effectiveMode);
    }

    /// <summary>
    /// AuthModeConstants defines expected constants.
    /// </summary>
    [Fact]
    public void AuthModeConstants_HasExpectedValues()
    {
        Assert.Equal("None", AuthModeConstants.None);
        Assert.Equal("OIDC", AuthModeConstants.Oidc);
    }

    /// <summary>
    /// AuthProviderConstants defines expected constants.
    /// </summary>
    [Fact]
    public void AuthProviderConstants_HasExpectedValues()
    {
        Assert.Equal("Authentik", AuthProviderConstants.Authentik);
        Assert.Equal("Google", AuthProviderConstants.Google);
        Assert.Equal("Microsoft", AuthProviderConstants.Microsoft);
        Assert.Equal("OIDC", AuthProviderConstants.Oidc);
    }
}
