using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace BudgetExperiment.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so dotnet-ef can create the DbContext for migrations.
/// </summary>
public sealed class DesignTimeBudgetDbContextFactory : IDesignTimeDbContextFactory<BudgetDbContext>
{
    /// <inheritdoc />
    public BudgetDbContext CreateDbContext(string[] args)
    {
        // Build minimal configuration looking for appsettings.json in API project or current.
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables();

        // Attempt to load user secrets from any loaded assembly that has a UserSecretsId attribute (e.g., the API project),
        // without creating a compile-time dependency on the API layer.
        var candidate = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(a => a.GetCustomAttributes(typeof(UserSecretsIdAttribute), false).Any());
        if (candidate is not null)
        {
            configurationBuilder.AddUserSecrets(candidate, optional: true);
        }

        var configuration = configurationBuilder.Build();

        var cs = configuration.GetConnectionString("AppDb");
        if (string.IsNullOrWhiteSpace(cs))
        {
            throw new InvalidOperationException("Design-time connection string 'AppDb' not found. Add it via user secrets or appsettings.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<BudgetDbContext>();
        optionsBuilder.UseNpgsql(cs);
        return new BudgetDbContext(optionsBuilder.Options);
    }
}
