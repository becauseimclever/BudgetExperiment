// <copyright file="ApiPostgreSqlFixture.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Testcontainers.PostgreSql;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// PostgreSQL Testcontainer fixture for API integration tests.
/// Starts one container per collection and exposes the connection string
/// so that <see cref="CustomWebApplicationFactory"/> and
/// <see cref="AuthEnabledWebApplicationFactory"/> can point the hosted app at it.
/// </summary>
public sealed class ApiPostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16")
        .Build();

    /// <summary>Gets the connection string for the running PostgreSQL container.</summary>
    public string ConnectionString => this._container.GetConnectionString();

    /// <inheritdoc />
    public async Task InitializeAsync() => await this._container.StartAsync();

    /// <inheritdoc />
    public async Task DisposeAsync() => await this._container.DisposeAsync();
}
