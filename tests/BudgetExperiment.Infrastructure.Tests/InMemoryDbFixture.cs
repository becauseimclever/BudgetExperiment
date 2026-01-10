// <copyright file="InMemoryDbFixture.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// In-memory SQLite database fixture for integration tests.
/// Each test gets its own fresh database instance.
/// </summary>
public sealed class InMemoryDbFixture : IDisposable
{
    private int _contextCounter;

    /// <summary>
    /// Creates a new DbContext for testing with a unique in-memory database.
    /// Call this at the start of each test to get a fresh database.
    /// </summary>
    /// <returns>A new <see cref="BudgetDbContext"/> with a unique database.</returns>
    public BudgetDbContext CreateContext()
    {
        var uniqueDbName = $"TestDb_{Interlocked.Increment(ref this._contextCounter)}_{Guid.NewGuid():N}";

        var options = new DbContextOptionsBuilder<BudgetDbContext>()
            .UseSqlite($"Data Source={uniqueDbName};Mode=Memory;Cache=Shared")
            .Options;

        var context = new BudgetDbContext(options);

        // Ensure database is created with schema
        context.Database.EnsureCreated();

        return context;
    }

    /// <summary>
    /// Creates a new DbContext that shares the same database as the provided context.
    /// Use this to verify persistence within the same test.
    /// </summary>
    /// <param name="existingContext">The existing context to share database with.</param>
    /// <returns>A new <see cref="BudgetDbContext"/> connected to the same database.</returns>
    public BudgetDbContext CreateSharedContext(BudgetDbContext existingContext)
    {
        var connectionString = existingContext.Database.GetConnectionString();

        var options = new DbContextOptionsBuilder<BudgetDbContext>()
            .UseSqlite(connectionString!)
            .Options;

        return new BudgetDbContext(options);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // SQLite in-memory databases are automatically cleaned up
        // when all connections are closed
    }
}
