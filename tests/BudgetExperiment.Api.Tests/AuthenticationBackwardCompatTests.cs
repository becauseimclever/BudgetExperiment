// <copyright file="AuthenticationBackwardCompatTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure;
using BudgetExperiment.Infrastructure.Persistence;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests ensuring existing Authentik deployments continue to work
/// after the authentication configuration refactoring (Feature 055 Phase 1).
/// </summary>
public sealed class AuthenticationBackwardCompatTests
{
    /// <summary>
    /// Existing Authentik config (Authority + Audience only, no Mode/Provider)
    /// continues to produce a working OIDC-mode application.
    /// </summary>
    [Fact]
    public async Task ExistingAuthentikConfig_ContinuesToWork()
    {
        // Arrange — only legacy config keys, no Mode or Provider
        await using var factory = new BackwardCompatFactory(new Dictionary<string, string?>
        {
            ["Authentication:Authentik:Authority"] = "https://auth.example.com/application/o/budget/",
            ["Authentication:Authentik:Audience"] = "budget-experiment",
        });

        using var client = factory.CreateAuthenticatedClient();

        // Act — the app should start and serve the config endpoint
        var response = await client.GetAsync("/api/v1/config");
        var config = await response.Content.ReadFromJsonAsync<ClientConfigDto>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(config);
        Assert.Equal("oidc", config.Authentication.Mode);
        Assert.NotNull(config.Authentication.Oidc);
        Assert.Equal("https://auth.example.com/application/o/budget/", config.Authentication.Oidc.Authority);
    }

    /// <summary>
    /// Setting Authentication:Authentik:Enabled=false disables auth (backward compat).
    /// </summary>
    [Fact]
    public async Task AuthentikEnabled_False_DisablesAuth()
    {
        // Arrange — legacy "Enabled=false" flag, override Authority to clear base appsettings value
        await using var factory = new BackwardCompatFactory(new Dictionary<string, string?>
        {
            ["Authentication:Authentik:Enabled"] = "false",
            ["Authentication:Authentik:Authority"] = "https://auth.example.com/",
        });

        using var client = factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/v1/config");
        var config = await response.Content.ReadFromJsonAsync<ClientConfigDto>();

        // Assert — should see mode=none
        Assert.NotNull(config);
        Assert.Equal("none", config.Authentication.Mode);
    }

    /// <summary>
    /// Missing Authority when Mode=OIDC throws InvalidOperationException (preserves existing fail-fast).
    /// Tested as a unit test since WebApplicationFactory config overrides cannot affect
    /// eager config reads that occur before Build().
    /// </summary>
    [Fact]
    public void MissingAuthority_InOidcMode_Throws()
    {
        // Arrange/Act/Assert — ValidateOidcAuthority should throw for empty authority
        var ex = Assert.Throws<InvalidOperationException>(
            () => AuthenticationOptions.ValidateOidcAuthority(string.Empty));

        Assert.Contains("Authority", ex.Message);
    }

    /// <summary>
    /// ValidateOidcAuthority does not throw for a valid authority.
    /// </summary>
    [Fact]
    public void ValidAuthority_InOidcMode_DoesNotThrow()
    {
        // This should not throw
        AuthenticationOptions.ValidateOidcAuthority("https://auth.example.com/");
    }

    /// <summary>
    /// ValidateOidcAuthority throws for whitespace-only authority.
    /// </summary>
    [Fact]
    public void WhitespaceAuthority_InOidcMode_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => AuthenticationOptions.ValidateOidcAuthority("   "));

        Assert.Contains("Authority", ex.Message);
    }

    /// <summary>
    /// Config endpoint response shape is unchanged for existing Authentik deployments.
    /// The response includes authentication.mode and authentication.oidc with all expected fields.
    /// </summary>
    [Fact]
    public async Task ConfigEndpoint_BackwardCompatShape()
    {
        // Arrange
        await using var factory = new BackwardCompatFactory(new Dictionary<string, string?>
        {
            ["Authentication:Authentik:Authority"] = "https://auth.example.com/application/o/budget/",
            ["Authentication:Authentik:Audience"] = "test-audience",
        });

        using var client = factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/v1/config");
        var config = await response.Content.ReadFromJsonAsync<ClientConfigDto>();

        // Assert — shape must match existing contract
        Assert.NotNull(config);
        Assert.Equal("oidc", config.Authentication.Mode);
        Assert.NotNull(config.Authentication.Oidc);
        Assert.False(string.IsNullOrEmpty(config.Authentication.Oidc.Authority));
        Assert.False(string.IsNullOrEmpty(config.Authentication.Oidc.ClientId));
        Assert.Equal("code", config.Authentication.Oidc.ResponseType);
        Assert.NotNull(config.Authentication.Oidc.Scopes);
        Assert.Contains("openid", config.Authentication.Oidc.Scopes);
    }

    /// <summary>
    /// Simulates the full set of env vars from docker-compose.pi.yml and verifies config binds correctly.
    /// </summary>
    [Fact]
    public async Task DockerComposePi_EnvVars_BindCorrectly()
    {
        // Arrange — exact env vars from docker-compose.pi.yml
        await using var factory = new BackwardCompatFactory(new Dictionary<string, string?>
        {
            ["Authentication:Authentik:Enabled"] = "true",
            ["Authentication:Authentik:Authority"] = "https://auth.prod.example.com/application/o/budget/",
            ["Authentication:Authentik:Audience"] = "prod-audience",
            ["Authentication:Authentik:RequireHttpsMetadata"] = "true",
        });

        using var client = factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/v1/config");
        var config = await response.Content.ReadFromJsonAsync<ClientConfigDto>();

        // Assert
        Assert.NotNull(config);
        Assert.Equal("oidc", config.Authentication.Mode);
        Assert.NotNull(config.Authentication.Oidc);
        Assert.Equal("https://auth.prod.example.com/application/o/budget/", config.Authentication.Oidc.Authority);
    }

    /// <summary>
    /// Factory for backward-compatibility tests that accepts custom configuration
    /// and replaces real auth with a test scheme.
    /// </summary>
    private sealed class BackwardCompatFactory : WebApplicationFactory<Program>
    {
        private readonly Dictionary<string, string?> _config;
        private readonly string _dbName = "TestDb_BackCompat_" + Guid.NewGuid().ToString();

        public BackwardCompatFactory(Dictionary<string, string?> config)
        {
            // Always include a connection string
            _config = new Dictionary<string, string?>(config)
            {
                ["ConnectionStrings:AppDb"] = "Host=localhost;Database=test",
            };
        }

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
                // Clear all sources to give tests full control (no appsettings.json bleed-through)
                config.Sources.Clear();
                config.AddInMemoryCollection(_config);
            });

            builder.ConfigureServices(services =>
            {
                // Remove Npgsql DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<BudgetDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Remove existing IUnitOfWork
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
                    options.UseInMemoryDatabase(_dbName)
                           .UseInternalServiceProvider(inMemoryServiceProvider);
                });

                services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BudgetDbContext>());

                // Override authentication with auto-authenticating test scheme
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "TestAuto";
                    options.DefaultChallengeScheme = "TestAuto";
                })
                .AddScheme<AuthenticationSchemeOptions, AutoAuthenticatingTestHandler>(
                    "TestAuto", options => { });
            });
        }
    }
}
