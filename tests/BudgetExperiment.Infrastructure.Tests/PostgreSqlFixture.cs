// <copyright file="PostgreSqlFixture.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Testcontainers.PostgreSql;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// PostgreSQL Testcontainer fixture for infrastructure integration tests.
/// Starts one container per test collection and provides test isolation by
/// truncating all tables before each logical test database is handed out.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16")
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

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Create schema once; subsequent CreateContext() calls just truncate data.
        await using var context = this.BuildContext();
        await context.Database.EnsureCreatedAsync();
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
