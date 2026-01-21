// <copyright file="InMemoryDbFixture.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// In-memory SQLite database fixture for integration tests.
/// Each test gets its own fresh database instance.
/// </summary>
public sealed class InMemoryDbFixture : IDisposable
{
    private readonly List<SqliteConnection> _connections = [];

    /// <summary>
    /// Creates a new DbContext for testing with a unique in-memory database.
    /// Call this at the start of each test to get a fresh database.
    /// The connection is kept open to preserve the in-memory database.
    /// </summary>
    /// <returns>A new <see cref="BudgetDbContext"/> with a unique database.</returns>
    public BudgetDbContext CreateContext()
    {
        // Create a new connection for each test to ensure isolation
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        this._connections.Add(connection);

        var options = new DbContextOptionsBuilder<BudgetDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new BudgetDbContext(options);

        // Ensure database is created with schema
        context.Database.EnsureCreated();

        return context;
    }

    /// <summary>
    /// Creates a new DbContext that shares the same database connection as the provided context.
    /// Use this to verify persistence within the same test.
    /// </summary>
    /// <param name="existingContext">The existing context to share database with.</param>
    /// <returns>A new <see cref="BudgetDbContext"/> connected to the same database.</returns>
    public BudgetDbContext CreateSharedContext(BudgetDbContext existingContext)
    {
        var connection = existingContext.Database.GetDbConnection();

        var options = new DbContextOptionsBuilder<BudgetDbContext>()
            .UseSqlite(connection)
            .Options;

        return new BudgetDbContext(options);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var connection in this._connections)
        {
            connection.Close();
            connection.Dispose();
        }

        this._connections.Clear();
    }
}
