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
/// Custom factory for API integration tests with authentication enabled.
/// </summary>
public sealed class AuthEnabledWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// The test username used for authenticated requests.
    /// </summary>
    public const string TestUsername = "testuser";

    /// <summary>
    /// The test user ID used for authenticated requests.
    /// </summary>
    public static readonly Guid TestUserId = new("11111111-1111-1111-1111-111111111111");

    private readonly string _dbName = "TestDb_Auth_" + Guid.NewGuid().ToString();

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
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AppDb"] = "Host=localhost;Database=test",
                ["Authentication:Authentik:Enabled"] = "true",
                ["Authentication:Authentik:Authority"] = "https://test.auth.local",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the Npgsql-configured DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<BudgetDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Remove existing IUnitOfWork registration
            var uowDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IUnitOfWork));
            if (uowDescriptor != null)
            {
                services.Remove(uowDescriptor);
            }

            // Build a separate internal service provider for InMemory only
            var inMemoryServiceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            // Create a fresh DbContext with only InMemory using isolated provider
            services.AddDbContext<BudgetDbContext>(options =>
            {
                options.UseInMemoryDatabase(this._dbName)
                       .UseInternalServiceProvider(inMemoryServiceProvider);
            });

            // Register IUnitOfWork for the test DbContext
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BudgetDbContext>());

            // Override authentication with a test scheme
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
        });
    }
}
