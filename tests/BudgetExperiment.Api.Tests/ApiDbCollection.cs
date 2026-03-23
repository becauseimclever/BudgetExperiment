// <copyright file="ApiDbCollection.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Collection definition for API integration tests that use a real PostgreSQL Testcontainer.
/// The container starts once and is shared across all test classes in the collection,
/// with each class fixture truncating tables on initialisation to guarantee a clean slate.
/// </summary>
[CollectionDefinition("ApiDb")]
public sealed class ApiDbCollection : ICollectionFixture<ApiPostgreSqlFixture>
{
}
