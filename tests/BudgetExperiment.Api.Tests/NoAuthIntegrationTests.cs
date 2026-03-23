// <copyright file="NoAuthIntegrationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Api.Authentication;
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
/// Integration tests verifying the no-auth (Mode=None) pipeline.
/// Ensures that all API requests auto-authenticate as the family user.
/// </summary>
[Collection("ApiDb")]
public sealed class NoAuthIntegrationTests : IAsyncLifetime
{
    private readonly ApiPostgreSqlFixture _dbFixture;
    private NoAuthFactory? _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="NoAuthIntegrationTests"/> class.
    /// </summary>
    /// <param name="dbFixture">The shared PostgreSQL container fixture.</param>
    public NoAuthIntegrationTests(ApiPostgreSqlFixture dbFixture)
    {
        _dbFixture = dbFixture;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _factory = new NoAuthFactory(_dbFixture);
        await _factory.InitializeAsync();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }
    }

    /// <summary>
    /// An unauthenticated GET to /api/v1/config returns 200 when auth is off.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfigEndpoint_ReturnsOk_WhenAuthDisabled()
    {
        using var client = _factory!.CreateClient();

        var response = await client.GetAsync("/api/v1/config");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// The /api/v1/config endpoint returns mode="none" when auth is disabled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfigEndpoint_ReturnsModeNone()
    {
        using var client = _factory!.CreateClient();

        var config = await client.GetFromJsonAsync<ClientConfigDto>("/api/v1/config");

        Assert.NotNull(config);
        Assert.Equal("none", config.Authentication.Mode);
    }

    /// <summary>
    /// The /api/v1/config endpoint does not expose OIDC settings when mode is none.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfigEndpoint_OidcIsNull_WhenModeNone()
    {
        using var client = _factory!.CreateClient();

        var config = await client.GetFromJsonAsync<ClientConfigDto>("/api/v1/config");

        Assert.NotNull(config);
        Assert.Null(config.Authentication.Oidc);
    }

    /// <summary>
    /// A request to an authenticated endpoint (e.g., /api/v1/user/me) succeeds
    /// without any Authorization header when auth is disabled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AuthenticatedEndpoint_ReturnsOk_WithoutAuthorizationHeader()
    {
        using var client = _factory!.CreateClient();

        // /api/v1/user/me requires authentication — should work automatically in no-auth mode
        var response = await client.GetAsync("/api/v1/user/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// The user context returns the family user ID for requests in no-auth mode.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UserEndpoint_ReturnsFamilyUserId()
    {
        using var client = _factory!.CreateClient();

        var user = await client.GetFromJsonAsync<UserProfileResponse>("/api/v1/user/me");

        Assert.NotNull(user);
        Assert.Equal(FamilyUserContext.FamilyUserId, user.UserId);
    }

    /// <summary>
    /// The user context returns "Family" as the display name in no-auth mode.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UserEndpoint_ReturnsFamilyDisplayName()
    {
        using var client = _factory!.CreateClient();

        var user = await client.GetFromJsonAsync<UserProfileResponse>("/api/v1/user/me");

        Assert.NotNull(user);
        Assert.Equal(FamilyUserContext.FamilyUserName, user.DisplayName);
    }

    /// <summary>
    /// The user context returns the family email in no-auth mode.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UserEndpoint_ReturnsFamilyEmail()
    {
        using var client = _factory!.CreateClient();

        var user = await client.GetFromJsonAsync<UserProfileResponse>("/api/v1/user/me");

        Assert.NotNull(user);
        Assert.Equal(FamilyUserContext.FamilyUserEmail, user.Email);
    }

    /// <summary>
    /// The user context returns "Family" as username in no-auth mode.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UserEndpoint_ReturnsFamilyUsername()
    {
        using var client = _factory!.CreateClient();

        var user = await client.GetFromJsonAsync<UserProfileResponse>("/api/v1/user/me");

        Assert.NotNull(user);
        Assert.Equal(FamilyUserContext.FamilyUserName, user.Username);
    }

    /// <summary>
    /// DTO matching the shape of <see cref="BudgetExperiment.Contracts.Dtos.UserProfileDto"/>.
    /// </summary>
    private sealed class UserProfileResponse
    {
        /// <summary>Gets or sets the user ID.</summary>
        public Guid UserId
        {
            get; set;
        }

        /// <summary>Gets or sets the username.</summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>Gets or sets the email.</summary>
        public string? Email
        {
            get; set;
        }

        /// <summary>Gets or sets the display name.</summary>
        public string? DisplayName
        {
            get; set;
        }

        /// <summary>Gets or sets the avatar URL.</summary>
        public string? AvatarUrl
        {
            get; set;
        }
    }

    /// <summary>
    /// Factory that configures <c>Authentication:Mode=None</c> for integration tests.
    /// Does NOT replace the authentication scheme — tests use the real NoAuthHandler.
    /// </summary>
    private sealed class NoAuthFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly ApiPostgreSqlFixture _dbFixture;

        public NoAuthFactory(ApiPostgreSqlFixture dbFixture)
        {
            _dbFixture = dbFixture;
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
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:AppDb"] = _dbFixture.ConnectionString,
                    ["Authentication:Mode"] = "None",
                });
            });

            builder.ConfigureServices(services =>
            {
                // Replace Npgsql DbContext with test container
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

                // Register NoAuthHandler — ConfigureAuthentication in Program.cs
                // runs before ConfigureWebHost overrides are applied, so the
                // production code's Mode=None branch hasn't seen our config yet.
                // We register the real NoAuthHandler here to test its behavior.
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = NoAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = NoAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, NoAuthHandler>(
                    NoAuthHandler.SchemeName, _ => { });
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
