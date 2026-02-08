// <copyright file="MobilePlaywrightCollection.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;

namespace BudgetExperiment.E2E.Tests;

/// <summary>
/// Collection definition for mobile Playwright tests to share browser instances.
/// </summary>
[CollectionDefinition("MobilePlaywright")]
public class MobilePlaywrightCollection : ICollectionFixture<MobilePlaywrightFixture>
{
}
