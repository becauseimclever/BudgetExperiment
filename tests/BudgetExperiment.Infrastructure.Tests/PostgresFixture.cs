// <copyright file="PostgresFixture.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Shared PostgreSQL container fixture for integration tests.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    /// <summary>
    /// Gets the connection string to the test database.
    /// </summary>
    public string ConnectionString => this._container.GetConnectionString();

    /// <summary>
    /// Creates a new DbContext for testing.
    /// </summary>
    /// <returns>A new <see cref="BudgetDbContext"/>.</returns>
    public BudgetDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BudgetDbContext>()
            .UseNpgsql(this.ConnectionString)
            .Options;

        return new BudgetDbContext(options);
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await this._container.StartAsync();

        // Apply migrations
        await using var context = this.CreateContext();
        await context.Database.MigrateAsync();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await this._container.DisposeAsync();
    }
}
