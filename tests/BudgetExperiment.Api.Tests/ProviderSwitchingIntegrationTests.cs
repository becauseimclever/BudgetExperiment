// <copyright file="ProviderSwitchingIntegrationTests.cs" company="BecauseImClever">
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
/// Integration tests verifying that provider switching via configuration
/// correctly updates the /api/v1/config endpoint response. Each test
/// creates a factory with a different provider to simulate switching.
/// </summary>
[Collection("ApiDb")]
public sealed class ProviderSwitchingIntegrationTests
{
    private readonly ApiPostgreSqlFixture _dbFixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderSwitchingIntegrationTests"/> class.
    /// </summary>
    /// <param name="dbFixture">The shared PostgreSQL container fixture.</param>
    public ProviderSwitchingIntegrationTests(ApiPostgreSqlFixture dbFixture)
    {
        _dbFixture = dbFixture;
    }

    /// <summary>
    /// Switching from Authentik to Google changes the authority in /api/v1/config.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SwitchingToGoogle_ChangesAuthorityToGoogle()
    {
        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "OIDC",
            ["Authentication:Provider"] = "Google",
            ["Authentication:Google:ClientId"] = "google-client-id",
            ["Authentication:Google:ClientSecret"] = "google-secret",
        });
        using var client = factory.CreateAuthenticatedClient();

        var config = await client.GetFromJsonAsync<ClientConfigDto>("/api/v1/config");

        Assert.NotNull(config);
        Assert.Equal("oidc", config.Authentication.Mode);
        Assert.NotNull(config.Authentication.Oidc);
        Assert.Equal("https://accounts.google.com", config.Authentication.Oidc.Authority);
        Assert.Equal("google-client-id", config.Authentication.Oidc.ClientId);
    }

    /// <summary>
    /// Switching from Authentik to Microsoft changes the authority in /api/v1/config.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SwitchingToMicrosoft_ChangesAuthorityToMicrosoft()
    {
        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "OIDC",
            ["Authentication:Provider"] = "Microsoft",
            ["Authentication:Microsoft:ClientId"] = "ms-client-id",
            ["Authentication:Microsoft:TenantId"] = "my-tenant",
        });
        using var client = factory.CreateAuthenticatedClient();

        var config = await client.GetFromJsonAsync<ClientConfigDto>("/api/v1/config");

        Assert.NotNull(config);
        Assert.Equal("oidc", config.Authentication.Mode);
        Assert.NotNull(config.Authentication.Oidc);
        Assert.Equal("https://login.microsoftonline.com/my-tenant/v2.0", config.Authentication.Oidc.Authority);
        Assert.Equal("ms-client-id", config.Authentication.Oidc.ClientId);
    }

    /// <summary>
    /// Switching from Authentik to generic OIDC uses the configured authority.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SwitchingToGenericOidc_UsesConfiguredAuthority()
    {
        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "OIDC",
            ["Authentication:Provider"] = "OIDC",
            ["Authentication:Oidc:Authority"] = "https://keycloak.example.com/realms/budget",
            ["Authentication:Oidc:ClientId"] = "kc-client",
        });
        using var client = factory.CreateAuthenticatedClient();

        var config = await client.GetFromJsonAsync<ClientConfigDto>("/api/v1/config");

        Assert.NotNull(config);
        Assert.Equal("oidc", config.Authentication.Mode);
        Assert.NotNull(config.Authentication.Oidc);
        Assert.Equal("https://keycloak.example.com/realms/budget", config.Authentication.Oidc.Authority);
        Assert.Equal("kc-client", config.Authentication.Oidc.ClientId);
    }

    /// <summary>
    /// Switching from OIDC to None removes OIDC settings from config.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SwitchingToNone_RemovesOidcSettings()
    {
        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "None",
        });
        using var client = factory.CreateNoAuthClient();

        var config = await client.GetFromJsonAsync<ClientConfigDto>("/api/v1/config");

        Assert.NotNull(config);
        Assert.Equal("none", config.Authentication.Mode);
        Assert.Null(config.Authentication.Oidc);
    }

    /// <summary>
    /// Switching from None back to Authentik restores OIDC settings.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SwitchingFromNoneToAuthentik_RestoresOidcSettings()
    {
        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "OIDC",
            ["Authentication:Provider"] = "Authentik",
            ["Authentication:Authentik:Authority"] = "https://auth.example.com/application/o/budget/",
            ["Authentication:Authentik:Audience"] = "budget-app",
        });
        using var client = factory.CreateAuthenticatedClient();

        var config = await client.GetFromJsonAsync<ClientConfigDto>("/api/v1/config");

        Assert.NotNull(config);
        Assert.Equal("oidc", config.Authentication.Mode);
        Assert.NotNull(config.Authentication.Oidc);
        Assert.Equal("https://auth.example.com/application/o/budget/", config.Authentication.Oidc.Authority);
    }

    /// <summary>
    /// Provider is case-insensitive (e.g., "google" works same as "Google").
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ProviderIsCaseInsensitive()
    {
        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "OIDC",
            ["Authentication:Provider"] = "google",
            ["Authentication:Google:ClientId"] = "test-client",
            ["Authentication:Google:ClientSecret"] = "test-secret",
        });
        using var client = factory.CreateAuthenticatedClient();

        var config = await client.GetFromJsonAsync<ClientConfigDto>("/api/v1/config");

        Assert.NotNull(config);
        Assert.Equal("oidc", config.Authentication.Mode);
        Assert.NotNull(config.Authentication.Oidc);
        Assert.Equal("https://accounts.google.com", config.Authentication.Oidc.Authority);
    }

    /// <summary>
    /// Mode is case-insensitive (e.g., "none" works same as "None").
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ModeIsCaseInsensitive()
    {
        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "none",
        });
        using var client = factory.CreateNoAuthClient();

        var config = await client.GetFromJsonAsync<ClientConfigDto>("/api/v1/config");

        Assert.NotNull(config);
        Assert.Equal("none", config.Authentication.Mode);
        Assert.Null(config.Authentication.Oidc);
    }

    /// <summary>
    /// Verifies config returns 200 OK for all supported providers.
    /// </summary>
    /// <param name="provider">The provider name.</param>
    /// <param name="extraKey">An extra config key needed by the provider.</param>
    /// <param name="extraValue">The value for the extra config key.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Theory]
    [InlineData("Authentik", "Authentication:Authentik:Authority", "https://auth.example.com/")]
    [InlineData("Google", "Authentication:Google:ClientId", "google-id")]
    [InlineData("Microsoft", "Authentication:Microsoft:ClientId", "ms-id")]
    [InlineData("OIDC", "Authentication:Oidc:Authority", "https://oidc.example.com/")]
    public async Task AllProviders_Return200FromConfig(string provider, string extraKey, string extraValue)
    {
        var config = new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "OIDC",
            ["Authentication:Provider"] = provider,
            [extraKey] = extraValue,
        };

        // Google also needs a client secret
        if (provider == "Google")
        {
            config["Authentication:Google:ClientSecret"] = "secret";
        }

        // OIDC also needs a client ID
        if (provider == "OIDC")
        {
            config["Authentication:Oidc:ClientId"] = "oidc-id";
        }

        // Microsoft also needs a tenant
        if (provider == "Microsoft")
        {
            config["Authentication:Microsoft:TenantId"] = "common";
        }

        await using var factory = CreateFactory(config);
        using var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/config");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private ProviderSwitchingFactory CreateFactory(Dictionary<string, string?> configValues)
    {
        return new ProviderSwitchingFactory(_dbFixture, configValues);
    }

    /// <summary>
    /// Factory that accepts arbitrary configuration for provider switching tests.
    /// </summary>
    private sealed class ProviderSwitchingFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly ApiPostgreSqlFixture _dbFixture;
        private readonly Dictionary<string, string?> _configValues;

        public ProviderSwitchingFactory(ApiPostgreSqlFixture dbFixture, Dictionary<string, string?> configValues)
        {
            _dbFixture = dbFixture;
            _configValues = configValues;

            // Ensure connection string is always present
            if (!_configValues.ContainsKey("ConnectionStrings:AppDb"))
            {
                _configValues["ConnectionStrings:AppDb"] = _dbFixture.ConnectionString;
            }
        }

        /// <summary>
        /// Creates an HTTP client with a test authentication header for OIDC mode.
        /// </summary>
        public HttpClient CreateAuthenticatedClient()
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("TestAuto", "authenticated");
            return client;
        }

        /// <summary>
        /// Creates an HTTP client without auth headers (for Mode=None tests).
        /// </summary>
        public HttpClient CreateNoAuthClient()
        {
            return CreateClient();
        }

        public async Task InitializeAsync()
        {
            using var scope = this.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
            await db.Database.MigrateAsync();
            TruncateAllTables(db);
        }

        public new async Task DisposeAsync() => await base.DisposeAsync();

        /// <inheritdoc />
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.Sources.Clear();
                config.AddInMemoryCollection(_configValues);
            });

            builder.ConfigureServices(services =>
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

                services.AddDbContext<BudgetDbContext>(options =>
                    options.UseNpgsql(_dbFixture.ConnectionString));

                services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BudgetDbContext>());

                var isNoAuth = _configValues.TryGetValue("Authentication:Mode", out var mode)
                    && mode != null
                    && mode.Equals("None", StringComparison.OrdinalIgnoreCase);

                if (isNoAuth)
                {
                    // NoAuth mode uses the real NoAuthHandler registered by Program.cs
                    // We only need to re-register it since ConfigureWebHost runs after
                    // the program's ConfigureAuthentication has already run with defaults.
                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = Authentication.NoAuthHandler.SchemeName;
                        options.DefaultChallengeScheme = Authentication.NoAuthHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, Authentication.NoAuthHandler>(
                        Authentication.NoAuthHandler.SchemeName, _ => { });
                }
                else
                {
                    // OIDC mode uses a test handler since we can't call real providers
                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "TestAuto";
                        options.DefaultChallengeScheme = "TestAuto";
                    })
                    .AddScheme<AuthenticationSchemeOptions, AutoAuthenticatingTestHandler>(
                        "TestAuto", _ => { });
                }
            });
        }

        private static void TruncateAllTables(BudgetDbContext context)
        {
            var tableNames = context.Model
                .GetEntityTypes()
                .Select(e => e.GetTableName())
                .Where(name => name != null)
                .Distinct()
                .ToList();

            if (tableNames.Count == 0)
            {
                return;
            }

            var quotedTables = string.Join(", ", tableNames.Select(t => $"\"{t}\""));

            // Table names are sourced from the EF model, not user input; suppression is safe here.
#pragma warning disable EF1002
            context.Database.ExecuteSqlRaw($"TRUNCATE TABLE {quotedTables} CASCADE");
#pragma warning restore EF1002
        }
    }
}
