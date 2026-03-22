// <copyright file="InMemoryDbCollection.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Collection definition for infrastructure tests using a PostgreSQL Testcontainer.
/// The container starts once and is shared across all test classes in the collection.
/// </summary>
[CollectionDefinition("InMemoryDb")]
public sealed class InMemoryDbCollection : ICollectionFixture<PostgreSqlFixture>
{
}
