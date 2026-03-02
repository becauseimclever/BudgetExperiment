// <copyright file="MicrosoftProviderIntegrationTests.cs" company="BecauseImClever">
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
/// Integration tests for Microsoft Entra ID provider configuration.
/// Verifies that the API starts correctly with Provider=Microsoft and
/// exposes the correct OIDC settings via /api/v1/config.
/// </summary>
public sealed class MicrosoftProviderIntegrationTests
{
    /// <summary>
    /// The /api/v1/config endpoint returns mode=oidc when Provider=Microsoft.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfigEndpoint_ReturnsModeOidc_WhenProviderIsMicrosoft()
    {
        await using var factory = new MicrosoftProviderFactory();
        using var client = factory.CreateAuthenticatedClient();

        var config = await client.GetFromJsonAsync<ClientConfigDto>("/api/v1/config");

        Assert.NotNull(config);
        Assert.Equal("oidc", config.Authentication.Mode);
    }

    /// <summary>
    /// The /api/v1/config endpoint returns Microsoft authority when Provider=Microsoft.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfigEndpoint_ReturnsMicrosoftAuthority_WhenProviderIsMicrosoft()
    {
        await using var factory = new MicrosoftProviderFactory();
        using var client = factory.CreateAuthenticatedClient();

        var config = await client.GetFromJsonAsync<ClientConfigDto>("/api/v1/config");

        Assert.NotNull(config);
        Assert.NotNull(config.Authentication.Oidc);
        Assert.Equal("https://login.microsoftonline.com/test-tenant-id/v2.0", config.Authentication.Oidc.Authority);
    }

    /// <summary>
    /// The /api/v1/config endpoint returns the Microsoft ClientId when Provider=Microsoft.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfigEndpoint_ReturnsMicrosoftClientId_WhenProviderIsMicrosoft()
    {
        await using var factory = new MicrosoftProviderFactory();
        using var client = factory.CreateAuthenticatedClient();

        var config = await client.GetFromJsonAsync<ClientConfigDto>("/api/v1/config");

        Assert.NotNull(config);
        Assert.NotNull(config.Authentication.Oidc);
        Assert.Equal("ms-test-client-id", config.Authentication.Oidc.ClientId);
    }

    /// <summary>
    /// The /api/v1/config endpoint returns standard OIDC scopes for Microsoft provider.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfigEndpoint_ReturnsStandardScopes_WhenProviderIsMicrosoft()
    {
        await using var factory = new MicrosoftProviderFactory();
        using var client = factory.CreateAuthenticatedClient();

        var config = await client.GetFromJsonAsync<ClientConfigDto>("/api/v1/config");

        Assert.NotNull(config);
        Assert.NotNull(config.Authentication.Oidc);
        Assert.Contains("openid", config.Authentication.Oidc.Scopes);
        Assert.Contains("profile", config.Authentication.Oidc.Scopes);
        Assert.Contains("email", config.Authentication.Oidc.Scopes);
    }

    /// <summary>
    /// The /api/v1/config endpoint returns 200 OK when Provider=Microsoft.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfigEndpoint_Returns200_WhenProviderIsMicrosoft()
    {
        await using var factory = new MicrosoftProviderFactory();
        using var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/config");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Microsoft authority uses multi-tenant ("common") when TenantId is not specified.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfigEndpoint_UsesCommonTenant_WhenTenantIdNotSpecified()
    {
        await using var factory = new MicrosoftMultiTenantFactory();
        using var client = factory.CreateAuthenticatedClient();

        var config = await client.GetFromJsonAsync<ClientConfigDto>("/api/v1/config");

        Assert.NotNull(config);
        Assert.NotNull(config.Authentication.Oidc);
        Assert.Equal("https://login.microsoftonline.com/common/v2.0", config.Authentication.Oidc.Authority);
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
    /// Factory that configures Provider=Microsoft with a specific tenant for integration tests.
    /// Authentication is bypassed with a test handler since we can't call Microsoft Entra ID.
    /// </summary>
    private sealed class MicrosoftProviderFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = "TestDb_Microsoft_" + Guid.NewGuid().ToString();

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
                    ["Authentication:Provider"] = "Microsoft",
                    ["Authentication:Microsoft:ClientId"] = "ms-test-client-id",
                    ["Authentication:Microsoft:TenantId"] = "test-tenant-id",
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
    /// Factory that configures Provider=Microsoft without TenantId (multi-tenant default).
    /// </summary>
    private sealed class MicrosoftMultiTenantFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = "TestDb_MsMultiTenant_" + Guid.NewGuid().ToString();

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
                    ["Authentication:Provider"] = "Microsoft",
                    ["Authentication:Microsoft:ClientId"] = "ms-multi-tenant-client-id",

                    // TenantId intentionally omitted — defaults to "common"
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
        private readonly string _dbName = "TestDb_AuthFallback_Ms_" + Guid.NewGuid().ToString();

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
