// <copyright file="PostgresCollection.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Collection definition for tests sharing the PostgreSQL container.
/// </summary>
[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}
