// Copyright (c) BecauseImClever. All rights reserved.

using System.Net.Http.Headers;

using BudgetExperiment.Domain.Repositories;
using BudgetExperiment.Infrastructure.Persistence;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Performance.Tests.Infrastructure;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> for performance tests.
/// By default uses an in-memory database. Set environment variable <c>PERF_USE_REAL_DB=true</c>
/// to use the real PostgreSQL database from user secrets (same as debug sessions).
/// </summary>
public sealed class PerformanceWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// The test username used for authenticated requests.
    /// </summary>
    public const string TestUsername = "perfuser";

    /// <summary>
    /// The test user ID used for authenticated requests.
    /// </summary>
    public static readonly Guid TestUserId = new("33333333-3333-3333-3333-333333333333");

    private readonly string _dbName = "PerfDb_" + Guid.NewGuid().ToString();

    /// <summary>
    /// Gets a value indicating whether the factory is configured to use the real database.
    /// </summary>
    public static bool UseRealDb { get; } =
        string.Equals(Environment.GetEnvironmentVariable("PERF_USE_REAL_DB"), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Creates an <see cref="HttpClient"/> pre-configured with test authentication.
    /// </summary>
    /// <returns>An authenticated <see cref="HttpClient"/>.</returns>
    public HttpClient CreateApiClient()
    {
        var client = this.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestAuto", "authenticated");
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (UseRealDb)
        {
            ConfigureRealDb(builder);
        }
        else
        {
            this.ConfigureInMemoryDb(builder);
        }
    }

    private static void ConfigureRealDb(IWebHostBuilder builder)
    {
        // Keep the real Npgsql DbContext from AddInfrastructure — only override auth
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TestAuto";
                options.DefaultChallengeScheme = "TestAuto";
            })
            .AddScheme<AuthenticationSchemeOptions, AutoAuthenticatingTestHandler>("TestAuto", options => { });
        });
    }

    private void ConfigureInMemoryDb(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AppDb"] = "Host=localhost;Database=test",
                ["Authentication:Authentik:Enabled"] = "true",
            });
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
                options.UseInMemoryDatabase(this._dbName)
                       .UseInternalServiceProvider(inMemoryServiceProvider);
            });

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BudgetDbContext>());

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TestAuto";
                options.DefaultChallengeScheme = "TestAuto";
            })
            .AddScheme<AuthenticationSchemeOptions, AutoAuthenticatingTestHandler>("TestAuto", options => { });
        });
    }
}
