using System.Net.Http.Headers;

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
/// Custom factory for API integration tests backed by a real PostgreSQL Testcontainer.
/// Uses test authentication that auto-authenticates all requests.
/// Each test class that uses this fixture gets a clean database state via table truncation
/// performed in <see cref="InitializeAsync"/>.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// The test username used for authenticated requests.
    /// </summary>
    public const string TestUsername = "integrationuser";

    /// <summary>
    /// The test user ID used for authenticated requests.
    /// </summary>
    public static readonly Guid TestUserId = new("22222222-2222-2222-2222-222222222222");

    private readonly ApiPostgreSqlFixture _dbFixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomWebApplicationFactory"/> class.
    /// </summary>
    /// <param name="dbFixture">The shared PostgreSQL container fixture.</param>
    public CustomWebApplicationFactory(ApiPostgreSqlFixture dbFixture)
    {
        this._dbFixture = dbFixture;
    }

    /// <summary>Creates an <see cref="HttpClient"/> for the API.</summary>
    /// <returns>Client with automatic test authentication header.</returns>
    public HttpClient CreateApiClient()
    {
        var client = this.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestAuto", "authenticated");
        return client;
    }

    /// <summary>
    /// Truncates all database tables, providing a clean slate for per-test isolation.
    /// Call this from the test class constructor when each test method needs an empty database.
    /// </summary>
    public void ResetDatabase()
    {
        using var scope = this.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
        TruncateAllTables(db);
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

            // Override authentication with auto-authenticating test scheme
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TestAuto";
                options.DefaultChallengeScheme = "TestAuto";
            })
            .AddScheme<AuthenticationSchemeOptions, AutoAuthenticatingTestHandler>("TestAuto", _ => { });
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
