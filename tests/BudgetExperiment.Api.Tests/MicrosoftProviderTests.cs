// <copyright file="MicrosoftProviderTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Microsoft.Extensions.Configuration;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Unit tests for Microsoft Entra ID provider configuration and settings resolution.
/// </summary>
public sealed class MicrosoftProviderTests
{
    /// <summary>
    /// The Microsoft OIDC authority template uses the well-known Entra ID login URL.
    /// </summary>
    [Fact]
    public void AuthorityTemplate_ContainsMicrosoftLogin()
    {
        Assert.Contains("login.microsoftonline.com", MicrosoftProviderOptions.AuthorityTemplate);
    }

    /// <summary>
    /// ResolveAuthority replaces the {tenantId} placeholder with the configured TenantId.
    /// </summary>
    [Fact]
    public void ResolveAuthority_ReplacesPlaceholder_WithTenantId()
    {
        var options = new MicrosoftProviderOptions
        {
            TenantId = "my-tenant-guid",
        };

        var authority = options.ResolveAuthority();

        Assert.Equal("https://login.microsoftonline.com/my-tenant-guid/v2.0", authority);
    }

    /// <summary>
    /// ResolveAuthority defaults to "common" for multi-tenant applications.
    /// </summary>
    [Fact]
    public void ResolveAuthority_DefaultsToCommon()
    {
        var options = new MicrosoftProviderOptions();

        var authority = options.ResolveAuthority();

        Assert.Equal("https://login.microsoftonline.com/common/v2.0", authority);
    }

    /// <summary>
    /// ResolveAuthority supports the "organizations" tenant for work/school accounts only.
    /// </summary>
    [Fact]
    public void ResolveAuthority_SupportsOrganizations()
    {
        var options = new MicrosoftProviderOptions
        {
            TenantId = "organizations",
        };

        var authority = options.ResolveAuthority();

        Assert.Equal("https://login.microsoftonline.com/organizations/v2.0", authority);
    }

    /// <summary>
    /// MicrosoftProviderOptions binds ClientId from configuration.
    /// </summary>
    [Fact]
    public void Binds_Microsoft_ClientId_From_Config()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Provider"] = "Microsoft",
                ["Authentication:Microsoft:ClientId"] = "00000000-1111-2222-3333-444444444444",
            })
            .Build();

        var options = new AuthenticationOptions();
        config.GetSection(AuthenticationOptions.SectionName).Bind(options);

        // Assert
        Assert.Equal("00000000-1111-2222-3333-444444444444", options.Microsoft.ClientId);
    }

    /// <summary>
    /// MicrosoftProviderOptions binds TenantId from configuration.
    /// </summary>
    [Fact]
    public void Binds_Microsoft_TenantId_From_Config()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Microsoft:TenantId"] = "my-specific-tenant",
            })
            .Build();

        var options = new AuthenticationOptions();
        config.GetSection(AuthenticationOptions.SectionName).Bind(options);

        // Assert
        Assert.Equal("my-specific-tenant", options.Microsoft.TenantId);
    }

    /// <summary>
    /// MicrosoftProviderOptions binds ClientSecret from configuration.
    /// </summary>
    [Fact]
    public void Binds_Microsoft_ClientSecret_From_Config()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Microsoft:ClientSecret"] = "ms-test-secret",
            })
            .Build();

        var options = new AuthenticationOptions();
        config.GetSection(AuthenticationOptions.SectionName).Bind(options);

        // Assert
        Assert.Equal("ms-test-secret", options.Microsoft.ClientSecret);
    }

    /// <summary>
    /// Microsoft ClientId defaults to empty string.
    /// </summary>
    [Fact]
    public void Microsoft_ClientId_Defaults_Empty()
    {
        var options = new MicrosoftProviderOptions();

        Assert.Equal(string.Empty, options.ClientId);
    }

    /// <summary>
    /// Microsoft TenantId defaults to "common".
    /// </summary>
    [Fact]
    public void Microsoft_TenantId_Defaults_Common()
    {
        var options = new MicrosoftProviderOptions();

        Assert.Equal("common", options.TenantId);
    }

    /// <summary>
    /// Microsoft ClientSecret defaults to empty string.
    /// </summary>
    [Fact]
    public void Microsoft_ClientSecret_Defaults_Empty()
    {
        var options = new MicrosoftProviderOptions();

        Assert.Equal(string.Empty, options.ClientSecret);
    }

    /// <summary>
    /// ResolveProviderSettings returns Microsoft authority when Provider is Microsoft.
    /// </summary>
    [Fact]
    public void ResolveProviderSettings_ReturnsMicrosoftAuthority_WhenProviderIsMicrosoft()
    {
        // Arrange
        var authOptions = new AuthenticationOptions
        {
            Provider = AuthProviderConstants.Microsoft,
            Microsoft = new MicrosoftProviderOptions
            {
                ClientId = "test-client-id",
                TenantId = "test-tenant",
            },
        };

        // Act
        var (authority, _, _) = AuthenticationOptions.ResolveProviderSettings(authOptions);

        // Assert
        Assert.Equal("https://login.microsoftonline.com/test-tenant/v2.0", authority);
    }

    /// <summary>
    /// ResolveProviderSettings returns Microsoft ClientId as audience when Provider is Microsoft.
    /// </summary>
    [Fact]
    public void ResolveProviderSettings_ReturnsClientIdAsAudience_WhenProviderIsMicrosoft()
    {
        // Arrange
        var authOptions = new AuthenticationOptions
        {
            Provider = AuthProviderConstants.Microsoft,
            Microsoft = new MicrosoftProviderOptions
            {
                ClientId = "ms-app-id-12345",
            },
        };

        // Act
        var (_, audience, _) = AuthenticationOptions.ResolveProviderSettings(authOptions);

        // Assert
        Assert.Equal("ms-app-id-12345", audience);
    }

    /// <summary>
    /// ResolveProviderSettings returns RequireHttpsMetadata=true for Microsoft (always HTTPS).
    /// </summary>
    [Fact]
    public void ResolveProviderSettings_RequiresHttps_WhenProviderIsMicrosoft()
    {
        // Arrange
        var authOptions = new AuthenticationOptions
        {
            Provider = AuthProviderConstants.Microsoft,
            Microsoft = new MicrosoftProviderOptions
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
    /// Provider constant "Microsoft" is correctly defined.
    /// </summary>
    [Fact]
    public void ProviderConstant_Microsoft_IsCorrect()
    {
        Assert.Equal("Microsoft", AuthProviderConstants.Microsoft);
    }
}
