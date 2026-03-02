// <copyright file="ImportDuplicateDetector.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Import;

/// <summary>
/// Detects duplicate transactions during CSV import using date, amount, and description matching.
/// </summary>
public sealed class ImportDuplicateDetector : IImportDuplicateDetector
{
    /// <inheritdoc />
    public Guid? FindDuplicate(
        DateOnly date,
        decimal amount,
        string description,
        IReadOnlyList<Transaction> existingTransactions,
        DuplicateDetectionSettingsDto settings)
    {
        foreach (var existing in existingTransactions)
        {
            if (!IsWithinDateRange(existing.Date, date, settings.LookbackDays))
            {
                continue;
            }

            if (existing.Amount.Amount != amount)
            {
                continue;
            }

            if (this.IsDescriptionMatch(existing.Description, description, settings.DescriptionMatch))
            {
                return existing.Id;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public double CalculateSimilarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
        {
            return 0;
        }

        a = a.ToLowerInvariant();
        b = b.ToLowerInvariant();

        int maxLen = Math.Max(a.Length, b.Length);
        int distance = LevenshteinDistance(a, b);

        return 1.0 - ((double)distance / maxLen);
    }

    private static bool IsWithinDateRange(DateOnly existingDate, DateOnly candidateDate, int lookbackDays)
    {
        var daysDiff = Math.Abs(existingDate.DayNumber - candidateDate.DayNumber);
        return daysDiff <= lookbackDays;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        int[,] dp = new int[a.Length + 1, b.Length + 1];

        for (int i = 0; i <= a.Length; i++)
        {
            dp[i, 0] = i;
        }

        for (int j = 0; j <= b.Length; j++)
        {
            dp[0, j] = j;
        }

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }
        }

        return dp[a.Length, b.Length];
    }

    private bool IsDescriptionMatch(string existingDescription, string candidateDescription, DescriptionMatchMode mode)
    {
        return mode switch
        {
            DescriptionMatchMode.Exact =>
                string.Equals(existingDescription, candidateDescription, StringComparison.OrdinalIgnoreCase),
            DescriptionMatchMode.Contains =>
                existingDescription.Contains(candidateDescription, StringComparison.OrdinalIgnoreCase) ||
                candidateDescription.Contains(existingDescription, StringComparison.OrdinalIgnoreCase),
            DescriptionMatchMode.StartsWith =>
                existingDescription.StartsWith(candidateDescription, StringComparison.OrdinalIgnoreCase) ||
                candidateDescription.StartsWith(existingDescription, StringComparison.OrdinalIgnoreCase),
            DescriptionMatchMode.Fuzzy =>
                this.CalculateSimilarity(existingDescription, candidateDescription) >= 0.8,
            _ => string.Equals(existingDescription, candidateDescription, StringComparison.OrdinalIgnoreCase),
        };
    }
}
