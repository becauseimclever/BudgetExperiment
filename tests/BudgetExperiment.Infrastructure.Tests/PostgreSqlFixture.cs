// <copyright file="PostgreSqlFixture.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Infrastructure.Encryption;
using BudgetExperiment.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// PostgreSQL Testcontainer fixture for infrastructure integration tests.
/// Starts one container per test collection and provides test isolation by
/// truncating all tables before each logical test database is handed out.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .Build();

    /// <summary>
    /// Creates a fresh <see cref="BudgetDbContext"/> backed by the running PostgreSQL container.
    /// All tables are truncated before the context is returned so each test starts with a clean slate.
    /// </summary>
    /// <returns>A new <see cref="BudgetDbContext"/> pointing at the Testcontainer database.</returns>
    public BudgetDbContext CreateContext()
    {
        var context = this.BuildContext();
        this.TruncateAllTables(context);
        return context;
    }

    /// <summary>
    /// Creates a fresh <see cref="BudgetDbContext"/> configured with encryption converters.
    /// All tables are truncated before the context is returned.
    /// </summary>
    /// <param name="masterKey">Optional Base64 AES-256 key. When null, a secure key is generated.</param>
    /// <returns>An encryption-enabled context using the shared PostgreSQL container database.</returns>
    public BudgetDbContext CreateEncryptedContext(string? masterKey = null)
    {
        var key = masterKey ?? EncryptionService.GenerateSecureKey();
        var context = this.BuildEncryptedContext(key);
        this.TruncateAllTables(context);
        return context;
    }

    /// <summary>
    /// Creates a second <see cref="BudgetDbContext"/> that shares the same PostgreSQL database
    /// as <paramref name="existingContext"/> without truncating tables.
    /// Use this to verify that data committed by the first context is visible to a fresh reader.
    /// </summary>
    /// <param name="existingContext">The context whose committed data should be visible.</param>
    /// <returns>A new <see cref="BudgetDbContext"/> pointing at the same database.</returns>
    public BudgetDbContext CreateSharedContext(BudgetDbContext existingContext)
    {
        // PostgreSQL persists committed data server-side; a new connection will see it.
        _ = existingContext; // parameter kept for API parity with InMemoryDbFixture
        return this.BuildContext();
    }

    /// <summary>
    /// Creates a second encryption-enabled context that shares the same database as <paramref name="existingContext"/>.
    /// </summary>
    /// <param name="existingContext">An existing context whose committed data should be visible.</param>
    /// <param name="masterKey">Base64 AES-256 key used to decrypt encrypted values.</param>
    /// <returns>A new encryption-enabled context without truncating data.</returns>
    public BudgetDbContext CreateSharedEncryptedContext(BudgetDbContext existingContext, string masterKey)
    {
        _ = existingContext;
        return this.BuildEncryptedContext(masterKey);
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Apply migrations once; subsequent CreateContext() calls just truncate data.
        await using var context = this.BuildContext();
        await context.Database.MigrateAsync();
    }

    /// <inheritdoc />
    public async Task DisposeAsync() => await _container.DisposeAsync();

    private BudgetDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<BudgetDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;
        return new BudgetDbContext(options);
    }

    private BudgetDbContext BuildEncryptedContext(string masterKey)
    {
        var options = new DbContextOptionsBuilder<BudgetDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:MasterKey"] = masterKey,
            })
            .Build();

        var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddScoped<IEncryptionService, EncryptionService>()
            .BuildServiceProvider();

        return new BudgetDbContext(options, serviceProvider);
    }

    private void TruncateAllTables(BudgetDbContext context)
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

        // TRUNCATE with CASCADE handles FK dependencies in a single statement.
        // Table names are sourced from the EF model (not user input); suppression is safe here.
        var quotedTables = string.Join(", ", tableNames.Select(t => $"\"{t}\""));
#pragma warning disable EF1002 // Table names are from EF model, not user input
        context.Database.ExecuteSqlRaw($"TRUNCATE TABLE {quotedTables} CASCADE");
#pragma warning restore EF1002
    }
}
