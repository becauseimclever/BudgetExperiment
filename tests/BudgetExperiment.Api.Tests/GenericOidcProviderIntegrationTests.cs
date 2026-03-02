// <copyright file="GenericOidcProviderIntegrationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure.Persistence;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for Generic OIDC provider configuration.
/// Verifies that the API starts correctly with Provider=OIDC and
/// exposes the correct OIDC settings via /api/v1/config.
/// Covers Keycloak, Auth0, and similar standard OIDC providers.
/// </summary>
public sealed class GenericOidcProviderIntegrationTests
{
    /// <summary>
    /// The /api/v1/config endpoint returns mode=oidc when Provider=OIDC.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfigEndpoint_ReturnsModeOidc_WhenProviderIsOidc()
    {
        await using var factory = new GenericOidcProviderFactory();
        using var client = factory.CreateAuthenticatedClient();

        var config = await client.GetFromJsonAsync<ClientConfigDto>("/api/v1/config");

        Assert.NotNull(config);
        Assert.Equal("oidc", config.Authentication.Mode);
    }

    /// <summary>
    /// The /api/v1/config endpoint returns the configured authority when Provider=OIDC.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfigEndpoint_ReturnsConfiguredAuthority_WhenProviderIsOidc()
    {
        await using var factory = new GenericOidcProviderFactory();
        using var client = factory.CreateAuthenticatedClient();

        var config = await client.GetFromJsonAsync<ClientConfigDto>("/api/v1/config");

        Assert.NotNull(config);
        Assert.NotNull(config.Authentication.Oidc);
        Assert.Equal("https://keycloak.example.com/realms/master", config.Authentication.Oidc.Authority);
    }

    /// <summary>
    /// The /api/v1/config endpoint returns the configured ClientId when Provider=OIDC.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfigEndpoint_ReturnsConfiguredClientId_WhenProviderIsOidc()
    {
        await using var factory = new GenericOidcProviderFactory();
        using var client = factory.CreateAuthenticatedClient();

        var config = await client.GetFromJsonAsync<ClientConfigDto>("/api/v1/config");

        Assert.NotNull(config);
        Assert.NotNull(config.Authentication.Oidc);
        Assert.Equal("budget-experiment", config.Authentication.Oidc.ClientId);
    }

    /// <summary>
    /// The /api/v1/config endpoint returns standard OIDC scopes for generic OIDC provider.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfigEndpoint_ReturnsStandardScopes_WhenProviderIsOidc()
    {
        await using var factory = new GenericOidcProviderFactory();
        using var client = factory.CreateAuthenticatedClient();

        var config = await client.GetFromJsonAsync<ClientConfigDto>("/api/v1/config");

        Assert.NotNull(config);
        Assert.NotNull(config.Authentication.Oidc);
        Assert.Contains("openid", config.Authentication.Oidc.Scopes);
        Assert.Contains("profile", config.Authentication.Oidc.Scopes);
        Assert.Contains("email", config.Authentication.Oidc.Scopes);
    }

    /// <summary>
    /// The /api/v1/config endpoint returns 200 OK when Provider=OIDC.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfigEndpoint_Returns200_WhenProviderIsOidc()
    {
        await using var factory = new GenericOidcProviderFactory();
        using var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/config");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Auth0 configuration works as a generic OIDC provider.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfigEndpoint_WorksWithAuth0Config()
    {
        await using var factory = new Auth0ProviderFactory();
        using var client = factory.CreateAuthenticatedClient();

        var config = await client.GetFromJsonAsync<ClientConfigDto>("/api/v1/config");

        Assert.NotNull(config);
        Assert.Equal("oidc", config.Authentication.Mode);
        Assert.NotNull(config.Authentication.Oidc);
        Assert.Equal("https://myapp.us.auth0.com/", config.Authentication.Oidc.Authority);
        Assert.Equal("auth0-client-id", config.Authentication.Oidc.ClientId);
    }

    /// <summary>
    /// Existing Authentik configuration still works when no Provider is set (backward compat).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfigEndpoint_StillWorksForAuthentik_WhenNoProviderSet()
    {
        await using var factory = new AuthentikFallbackFactory();
        using var client = factory.CreateAuthenticatedClient();

        var config = await client.GetFromJsonAsync<ClientConfigDto>("/api/v1/config");

        Assert.NotNull(config);
        Assert.Equal("oidc", config.Authentication.Mode);
        Assert.NotNull(config.Authentication.Oidc);
        Assert.Equal("https://auth.example.com/application/o/budget/", config.Authentication.Oidc.Authority);
    }

    /// <summary>
    /// Replaces the real database with an in-memory database for testing.
    /// </summary>
    private static void ReplaceDbWithInMemory(IServiceCollection services, string dbName)
    {
        var descriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<BudgetDbContext>));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }

        var uowDescriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(IUnitOfWork));
        if (uowDescriptor != null)
        {
            services.Remove(uowDescriptor);
        }

        var inMemoryServiceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        services.AddDbContext<BudgetDbContext>(options =>
        {
            options.UseInMemoryDatabase(dbName)
                   .UseInternalServiceProvider(inMemoryServiceProvider);
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BudgetDbContext>());
    }

    /// <summary>
    /// Replaces the real auth handler with a test handler that auto-authenticates.
    /// </summary>
    private static void ReplaceAuthWithTestHandler(IServiceCollection services)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "TestAuto";
            options.DefaultChallengeScheme = "TestAuto";
        })
        .AddScheme<AuthenticationSchemeOptions, AutoAuthenticatingTestHandler>(
            "TestAuto", _ => { });
    }

    /// <summary>
    /// Factory that configures Provider=OIDC with Keycloak-style settings for integration tests.
    /// Authentication is bypassed with a test handler since we can't call Keycloak.
    /// </summary>
    private sealed class GenericOidcProviderFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = "TestDb_GenericOidc_" + Guid.NewGuid().ToString();

        public HttpClient CreateAuthenticatedClient()
        {
            var client = this.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("TestAuto", "authenticated");
            return client;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.Sources.Clear();
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:AppDb"] = "Host=localhost;Database=test",
                    ["Authentication:Mode"] = "OIDC",
                    ["Authentication:Provider"] = "OIDC",
                    ["Authentication:Oidc:Authority"] = "https://keycloak.example.com/realms/master",
                    ["Authentication:Oidc:ClientId"] = "budget-experiment",
                    ["Authentication:Oidc:Audience"] = "budget-api",
                });
            });

            builder.ConfigureServices(services =>
            {
                ReplaceDbWithInMemory(services, _dbName);
                ReplaceAuthWithTestHandler(services);
            });
        }
    }

    /// <summary>
    /// Factory that configures Provider=OIDC with Auth0-style settings.
    /// </summary>
    private sealed class Auth0ProviderFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = "TestDb_Auth0_" + Guid.NewGuid().ToString();

        public HttpClient CreateAuthenticatedClient()
        {
            var client = this.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("TestAuto", "authenticated");
            return client;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.Sources.Clear();
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:AppDb"] = "Host=localhost;Database=test",
                    ["Authentication:Mode"] = "OIDC",
                    ["Authentication:Provider"] = "OIDC",
                    ["Authentication:Oidc:Authority"] = "https://myapp.us.auth0.com/",
                    ["Authentication:Oidc:ClientId"] = "auth0-client-id",
                    ["Authentication:Oidc:Audience"] = "https://budget-api.example.com",
                });
            });

            builder.ConfigureServices(services =>
            {
                ReplaceDbWithInMemory(services, _dbName);
                ReplaceAuthWithTestHandler(services);
            });
        }
    }

    /// <summary>
    /// Factory that uses existing Authentik config (no Provider set) to verify backward compatibility.
    /// </summary>
    private sealed class AuthentikFallbackFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = "TestDb_AuthFallback_Oidc_" + Guid.NewGuid().ToString();

        public HttpClient CreateAuthenticatedClient()
        {
            var client = this.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("TestAuto", "authenticated");
            return client;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.Sources.Clear();
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:AppDb"] = "Host=localhost;Database=test",
                    ["Authentication:Authentik:Authority"] = "https://auth.example.com/application/o/budget/",
                    ["Authentication:Authentik:Audience"] = "budget-experiment",
                });
            });

            builder.ConfigureServices(services =>
            {
                ReplaceDbWithInMemory(services, _dbName);
                ReplaceAuthWithTestHandler(services);
            });
        }
    }
}
