// <copyright file="PlaywrightCollection.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.E2E.Tests.Infrastructure;

/// <summary>
/// Defines the collection for sharing the PlaywrightFixture across tests.
/// </summary>
[CollectionDefinition(Name)]
public class PlaywrightCollection : ICollectionFixture<PlaywrightFixture>
{
    /// <summary>
    /// The name of the Playwright test collection.
    /// </summary>
    public const string Name = "Playwright";
}
