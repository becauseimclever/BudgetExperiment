// <copyright file="ReconciliationStatusConstantsTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Constants;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Regression guard tests for <see cref="ReconciliationStatus"/> values.
/// Ensures reconciliation status strings are never accidentally changed.
/// </summary>
public sealed class ReconciliationStatusConstantsTests
{
    /// <summary>
    /// Matched status must equal "Matched".
    /// </summary>
    [Fact]
    public void Matched_EqualsExpectedValue()
    {
        Assert.Equal("Matched", ReconciliationStatus.Matched);
    }

    /// <summary>
    /// Pending status must equal "Pending".
    /// </summary>
    [Fact]
    public void Pending_EqualsExpectedValue()
    {
        Assert.Equal("Pending", ReconciliationStatus.Pending);
    }

    /// <summary>
    /// Missing status must equal "Missing".
    /// </summary>
    [Fact]
    public void Missing_EqualsExpectedValue()
    {
        Assert.Equal("Missing", ReconciliationStatus.Missing);
    }
}
