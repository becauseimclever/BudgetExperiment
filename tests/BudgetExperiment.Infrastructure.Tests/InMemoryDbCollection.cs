// <copyright file="InMemoryDbCollection.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Collection definition for tests using the in-memory SQLite database.
/// </summary>
[CollectionDefinition("InMemoryDb")]
public sealed class InMemoryDbCollection : ICollectionFixture<InMemoryDbFixture>
{
}
