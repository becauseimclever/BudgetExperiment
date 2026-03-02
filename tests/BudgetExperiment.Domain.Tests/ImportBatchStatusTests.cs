// <copyright file="ImportBatchStatusTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the ImportBatchStatus enum.
/// </summary>
public class ImportBatchStatusTests
{
    [Theory]
    [InlineData(ImportBatchStatus.Pending, 0)]
    [InlineData(ImportBatchStatus.Completed, 1)]
    [InlineData(ImportBatchStatus.PartiallyCompleted, 2)]
    [InlineData(ImportBatchStatus.Deleted, 3)]
    public void ImportBatchStatus_Has_Expected_Values(ImportBatchStatus status, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)status);
    }
}
