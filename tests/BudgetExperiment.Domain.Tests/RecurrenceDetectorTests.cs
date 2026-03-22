// <copyright file="RecurrenceDetectorTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the RecurrenceDetector static class.
/// </summary>
public class RecurrenceDetectorTests
{
    private static readonly Guid TestAccountId = Guid.NewGuid();
    private static readonly RecurrenceDetectionOptions DefaultOptions = new();

    [Fact]
    public void Detect_MonthlyNetflix_ReturnsMonthlyPattern()
    {
        // Arrange — Netflix $15.99 monthly for 6 months
        var transactions = CreateMonthlyTransactions(
            "NETFLIX.COM",
            -15.99m,
            new DateOnly(2025, 1, 15),
            count: 6);
        var today = new DateOnly(2025, 7, 1);

        // Act
        var results = RecurrenceDetector.Detect(transactions, DefaultOptions, today);

        // Assert
        Assert.Single(results);
        var pattern = results[0];
        Assert.Equal("NETFLIX.COM", pattern.NormalizedDescription);
        Assert.Equal(RecurrenceFrequency.Monthly, pattern.Frequency);
        Assert.Equal(1, pattern.Interval);
        Assert.True(pattern.Confidence >= 0.5m);
        Assert.Equal(6, pattern.MatchingTransactionIds.Count);
    }

    [Fact]
    public void Detect_WeeklyPattern_ReturnsWeekly()
    {
        // Arrange — Weekly charge every 7 days for 5 weeks
        var transactions = CreateWeeklyTransactions(
            "GYM MEMBERSHIP",
            -25.00m,
            new DateOnly(2025, 3, 1),
            count: 5);
        var today = new DateOnly(2025, 4, 5);

        // Act
        var results = RecurrenceDetector.Detect(transactions, DefaultOptions, today);

        // Assert
        Assert.Single(results);
        Assert.Equal(RecurrenceFrequency.Weekly, results[0].Frequency);
    }

    [Fact]
    public void Detect_BiWeeklyPattern_ReturnsBiWeekly()
    {
        // Arrange — Bi-weekly charge every 14 days for 4 occurrences
        var transactions = CreateTransactionsAtInterval(
            "PAYROLL DIRECT DEP",
            2500.00m,
            new DateOnly(2025, 1, 3),
            intervalDays: 14,
            count: 4);
        var today = new DateOnly(2025, 3, 1);

        // Act
        var results = RecurrenceDetector.Detect(transactions, DefaultOptions, today);

        // Assert
        Assert.Single(results);
        Assert.Equal(RecurrenceFrequency.BiWeekly, results[0].Frequency);
    }

    [Fact]
    public void Detect_QuarterlyPattern_ReturnsQuarterly()
    {
        // Arrange — Quarterly insurance premium
        var transactions = CreateTransactionsAtInterval(
            "STATE FARM INSURANCE",
            -450.00m,
            new DateOnly(2024, 1, 15),
            intervalDays: 91,
            count: 4);
        var today = new DateOnly(2025, 1, 20);

        // Act
        var results = RecurrenceDetector.Detect(transactions, DefaultOptions, today);

        // Assert
        Assert.Single(results);
        Assert.Equal(RecurrenceFrequency.Quarterly, results[0].Frequency);
    }

    [Fact]
    public void Detect_ExcludesAlreadyLinkedTransactions()
    {
        // Arrange — 3 linked + 2 unlinked for same description
        var recurringId = Guid.NewGuid();
        var transactions = new List<TransactionSnapshot>();

        // These are already linked — should be excluded
        for (int i = 0; i < 3; i++)
        {
            transactions.Add(new TransactionSnapshot(
                Guid.NewGuid(),
                TestAccountId,
                "NETFLIX.COM",
                -15.99m,
                "USD",
                new DateOnly(2025, 1 + i, 15),
                null,
                recurringId));
        }

        // Only 2 unlinked — below minimum
        transactions.Add(new TransactionSnapshot(
            Guid.NewGuid(),
            TestAccountId,
            "NETFLIX.COM",
            -15.99m,
            "USD",
            new DateOnly(2025, 4, 15),
            null,
            null));
        transactions.Add(new TransactionSnapshot(
            Guid.NewGuid(),
            TestAccountId,
            "NETFLIX.COM",
            -15.99m,
            "USD",
            new DateOnly(2025, 5, 15),
            null,
            null));

        var today = new DateOnly(2025, 6, 1);

        // Act
        var results = RecurrenceDetector.Detect(transactions, DefaultOptions, today);

        // Assert — only 2 unlinked, below minimum of 3
        Assert.Empty(results);
    }

    [Fact]
    public void Detect_BelowMinimumOccurrences_ReturnsEmpty()
    {
        // Arrange — only 2 transactions, below default minimum of 3
        var transactions = CreateMonthlyTransactions("NETFLIX.COM", -15.99m, new DateOnly(2025, 1, 15), 2);
        var today = new DateOnly(2025, 3, 20);

        // Act
        var results = RecurrenceDetector.Detect(transactions, DefaultOptions, today);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Detect_IrregularDates_ReturnsEmptyOrLowConfidence()
    {
        // Arrange — random dates, no pattern
        var transactions = new List<TransactionSnapshot>
        {
            CreateSnapshot("STORE", -20.00m, new DateOnly(2025, 1, 5)),
            CreateSnapshot("STORE", -20.00m, new DateOnly(2025, 2, 20)),
            CreateSnapshot("STORE", -20.00m, new DateOnly(2025, 3, 2)),
            CreateSnapshot("STORE", -20.00m, new DateOnly(2025, 5, 28)),
        };
        var today = new DateOnly(2025, 6, 1);

        // Act
        var results = RecurrenceDetector.Detect(transactions, DefaultOptions, today);

        // Assert — either empty or all below threshold
        Assert.True(results.Count == 0 || results.All(r => r.Confidence < 0.5m));
    }

    [Fact]
    public void Detect_AmountVarianceBeyondTolerance_ReturnsEmpty()
    {
        // Arrange — same description but wildly varying amounts
        var transactions = new List<TransactionSnapshot>
        {
            CreateSnapshot("RESTAURANT", -10.00m, new DateOnly(2025, 1, 15)),
            CreateSnapshot("RESTAURANT", -50.00m, new DateOnly(2025, 2, 15)),
            CreateSnapshot("RESTAURANT", -100.00m, new DateOnly(2025, 3, 15)),
        };
        var today = new DateOnly(2025, 4, 1);

        // Act
        var results = RecurrenceDetector.Detect(transactions, DefaultOptions, today);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Detect_SlightAmountVariation_StillDetects()
    {
        // Arrange — Small variations within 5% tolerance
        var transactions = new List<TransactionSnapshot>
        {
            CreateSnapshot("UTILITY BILL", -100.00m, new DateOnly(2025, 1, 15)),
            CreateSnapshot("UTILITY BILL", -102.00m, new DateOnly(2025, 2, 15)),
            CreateSnapshot("UTILITY BILL", -99.00m, new DateOnly(2025, 3, 15)),
            CreateSnapshot("UTILITY BILL", -101.50m, new DateOnly(2025, 4, 15)),
        };
        var today = new DateOnly(2025, 5, 1);

        // Act
        var results = RecurrenceDetector.Detect(transactions, DefaultOptions, today);

        // Assert
        Assert.Single(results);
        Assert.Equal(RecurrenceFrequency.Monthly, results[0].Frequency);
    }

    [Fact]
    public void Detect_NormalizesDescriptions_GroupsCorrectly()
    {
        // Arrange — Different bank prefixes, same merchant
        var transactions = new List<TransactionSnapshot>
        {
            CreateSnapshot("POS PURCHASE NETFLIX.COM", -15.99m, new DateOnly(2025, 1, 15)),
            CreateSnapshot("CHECKCARD NETFLIX.COM", -15.99m, new DateOnly(2025, 2, 15)),
            CreateSnapshot("POS NETFLIX.COM", -15.99m, new DateOnly(2025, 3, 15)),
        };
        var today = new DateOnly(2025, 4, 1);

        // Act
        var results = RecurrenceDetector.Detect(transactions, DefaultOptions, today);

        // Assert — all should have been normalized to "NETFLIX.COM" and grouped
        Assert.Single(results);
        Assert.Equal("NETFLIX.COM", results[0].NormalizedDescription);
    }

    [Fact]
    public void Detect_SortsByConfidenceDescending()
    {
        // Arrange — two patterns, create one with higher regularity
        var transactions = new List<TransactionSnapshot>();

        // Perfect monthly Netflix
        transactions.AddRange(CreateMonthlyTransactions("NETFLIX.COM", -15.99m, new DateOnly(2025, 1, 15), 8));

        // Less regular Spotify (3 occurrences only)
        transactions.AddRange(CreateMonthlyTransactions("SPOTIFY", -9.99m, new DateOnly(2025, 1, 1), 3));

        var today = new DateOnly(2025, 9, 1);

        // Act
        var results = RecurrenceDetector.Detect(transactions, DefaultOptions, today);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.True(results[0].Confidence >= results[1].Confidence);
    }

    [Fact]
    public void Detect_FindsMostUsedCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var otherCategoryId = Guid.NewGuid();
        var transactions = new List<TransactionSnapshot>
        {
            new(Guid.NewGuid(), TestAccountId, "NETFLIX.COM", -15.99m, "USD", new DateOnly(2025, 1, 15), categoryId, null),
            new(Guid.NewGuid(), TestAccountId, "NETFLIX.COM", -15.99m, "USD", new DateOnly(2025, 2, 15), categoryId, null),
            new(Guid.NewGuid(), TestAccountId, "NETFLIX.COM", -15.99m, "USD", new DateOnly(2025, 3, 15), otherCategoryId, null),
        };
        var today = new DateOnly(2025, 4, 1);

        // Act
        var results = RecurrenceDetector.Detect(transactions, DefaultOptions, today);

        // Assert
        Assert.Single(results);
        Assert.Equal(categoryId, results[0].MostUsedCategoryId);
    }

    [Fact]
    public void Detect_EmptyList_ReturnsEmpty()
    {
        var results = RecurrenceDetector.Detect([], DefaultOptions);
        Assert.Empty(results);
    }

    [Fact]
    public void Detect_CustomMinimumOccurrences_Respected()
    {
        // Arrange — 4 transactions, options requires 5
        var options = new RecurrenceDetectionOptions(MinimumOccurrences: 5);
        var transactions = CreateMonthlyTransactions("NETFLIX.COM", -15.99m, new DateOnly(2025, 1, 15), 4);
        var today = new DateOnly(2025, 5, 20);

        // Act
        var results = RecurrenceDetector.Detect(transactions, options, today);

        // Assert
        Assert.Empty(results);
    }

    private static List<TransactionSnapshot> CreateMonthlyTransactions(
        string description,
        decimal amount,
        DateOnly startDate,
        int count)
    {
        var transactions = new List<TransactionSnapshot>();
        for (int i = 0; i < count; i++)
        {
            var date = startDate.AddMonths(i);
            transactions.Add(CreateSnapshot(description, amount, date));
        }

        return transactions;
    }

    private static List<TransactionSnapshot> CreateWeeklyTransactions(
        string description,
        decimal amount,
        DateOnly startDate,
        int count)
    {
        return CreateTransactionsAtInterval(description, amount, startDate, 7, count);
    }

    private static List<TransactionSnapshot> CreateTransactionsAtInterval(
        string description,
        decimal amount,
        DateOnly startDate,
        int intervalDays,
        int count)
    {
        var transactions = new List<TransactionSnapshot>();
        for (int i = 0; i < count; i++)
        {
            var date = startDate.AddDays(i * intervalDays);
            transactions.Add(CreateSnapshot(description, amount, date));
        }

        return transactions;
    }

    private static TransactionSnapshot CreateSnapshot(
        string description,
        decimal amount,
        DateOnly date,
        Guid? categoryId = null,
        Guid? recurringTransactionId = null)
    {
        return new TransactionSnapshot(
            Guid.NewGuid(),
            TestAccountId,
            description,
            amount,
            "USD",
            date,
            categoryId,
            recurringTransactionId);
    }
}
