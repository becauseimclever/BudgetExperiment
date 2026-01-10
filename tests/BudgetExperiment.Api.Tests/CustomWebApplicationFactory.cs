using BudgetExperiment.Api;
using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Custom factory for API integration tests using an in-memory database.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "TestDb_" + Guid.NewGuid().ToString();

    /// <summary>Creates an <see cref="HttpClient"/> for the API.</summary>
    /// <returns>Client.</returns>
    public HttpClient CreateApiClient() => this.CreateClient();

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Provide a dummy connection string to satisfy Infrastructure DI validation
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AppDb"] = "Host=localhost;Database=test"
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
        });
    }
}
