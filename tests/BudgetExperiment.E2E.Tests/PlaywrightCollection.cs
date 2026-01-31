// <copyright file="PlaywrightCollection.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;

namespace BudgetExperiment.E2E.Tests;

/// <summary>
/// Collection definition for Playwright tests to share browser instances.
/// </summary>
[CollectionDefinition("Playwright")]
public class PlaywrightCollection : ICollectionFixture<PlaywrightFixture>
{
}
