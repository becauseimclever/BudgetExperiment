// <copyright file="TestDataHelper.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.E2E.Tests.Helpers;

/// <summary>
/// Helper methods for generating deterministic E2E test data.
/// </summary>
public static class TestDataHelper
{
    /// <summary>
    /// Creates a unique, readable name for test-created entities.
    /// </summary>
    /// <param name="prefix">Name prefix (for example: Account, Txn).</param>
    /// <returns>A unique name safe for UI assertions.</returns>
    public static string CreateUniqueName(string prefix)
    {
        return $"E2E-{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..48];
    }

    /// <summary>
    /// Finds a sample CSV file from the repository's sample data folder.
    /// </summary>
    /// <returns>Absolute path to sample data CSV.</returns>
    public static string GetSampleCsvPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "sample data", "boa.csv");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException("Could not locate sample CSV file at sample data/boa.csv from test execution directory.");
    }
}
