// <copyright file="AuthEnabledWebApplicationFactory.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;

using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure;
using BudgetExperiment.Infrastructure.Persistence;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Custom factory for API integration tests with real authentication middleware enabled,
/// backed by a real PostgreSQL Testcontainer.
/// </summary>
public sealed class AuthEnabledWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// The test username used for authenticated requests.
    /// </summary>
    public const string TestUsername = "testuser";

    /// <summary>
    /// The test user ID used for authenticated requests.
    /// </summary>
    public static readonly Guid TestUserId = new("11111111-1111-1111-1111-111111111111");

    private readonly ApiPostgreSqlFixture _dbFixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthEnabledWebApplicationFactory"/> class.
    /// </summary>
    /// <param name="dbFixture">The shared PostgreSQL container fixture.</param>
    public AuthEnabledWebApplicationFactory(ApiPostgreSqlFixture dbFixture)
    {
        this._dbFixture = dbFixture;
    }

    /// <summary>
    /// Creates an unauthenticated <see cref="HttpClient"/> for the API.
    /// </summary>
    /// <returns>A client without authentication headers.</returns>
    public HttpClient CreateUnauthenticatedClient() => this.CreateClient();

    /// <summary>
    /// Creates an authenticated <see cref="HttpClient"/> for the API.
    /// </summary>
    /// <returns>A client with a valid test authentication token.</returns>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = this.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "authenticated");
        return client;
    }

    /// <summary>
    /// Creates an authenticated <see cref="HttpClient"/> for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="username">The username.</param>
    /// <returns>A client with a valid test authentication token for the specified user.</returns>
    public HttpClient CreateAuthenticatedClient(Guid userId, string username)
    {
        var client = this.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", $"{userId}:{username}");
        return client;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        using var scope = this.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
        await db.Database.EnsureCreatedAsync();
        TruncateAllTables(db);
    }

    /// <inheritdoc />
    public new async Task DisposeAsync() => await base.DisposeAsync();

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AppDb"] = this._dbFixture.ConnectionString,
                ["Authentication:Authentik:Enabled"] = "true",
                ["Authentication:Authentik:Authority"] = "https://test.auth.local",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the Npgsql-configured DbContext and IUnitOfWork registered by production code
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

            // Point the app at the Testcontainer database
            services.AddDbContext<BudgetDbContext>(options =>
                options.UseNpgsql(this._dbFixture.ConnectionString));

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BudgetDbContext>());

            // Override authentication with a test scheme
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
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
