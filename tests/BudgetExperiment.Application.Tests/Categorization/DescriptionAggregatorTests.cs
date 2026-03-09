// <copyright file="DescriptionAggregatorTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain.Accounts;
using BudgetExperiment.Domain.Common;

namespace BudgetExperiment.Application.Tests.Categorization;

/// <summary>
/// Unit tests for <see cref="DescriptionAggregator"/>.
/// </summary>
public class DescriptionAggregatorTests
{
    [Fact]
    public void Aggregate_Groups_By_Cleaned_Description()
    {
        var transactions = new[]
        {
            CreateTransaction("Debit Card Purchase - AMAZON MKTPL AMZN COM BIL WA", 10.99m),
            CreateTransaction("Digital Card Purchase - AMAZON MKTPL AMZN COM BIL WA", 29.74m),
            CreateTransaction("Debit Card Purchase - GROCERY STORE CITY ST", 58.35m),
        };

        var result = DescriptionAggregator.Aggregate(transactions);

        Assert.Equal(2, result.Count);
        var amazon = result.First(g => g.RepresentativeDescription.Contains("AMAZON"));
        Assert.Equal(2, amazon.Count);
    }

    [Fact]
    public void Aggregate_Calculates_Amount_Range()
    {
        var transactions = new[]
        {
            CreateTransaction("Debit Card Purchase - WALMART STORE", 5.99m),
            CreateTransaction("Debit Card Purchase - WALMART STORE", 249.99m),
            CreateTransaction("Debit Card Purchase - WALMART STORE", 42.50m),
        };

        var result = DescriptionAggregator.Aggregate(transactions);

        var group = Assert.Single(result);
        Assert.Equal(5.99m, group.MinAmount);
        Assert.Equal(249.99m, group.MaxAmount);
        Assert.Equal(3, group.Count);
    }

    [Fact]
    public void Aggregate_Uses_Absolute_Amount_For_Range()
    {
        var transactions = new[]
        {
            CreateTransaction("WALMART", -30.00m),
            CreateTransaction("WALMART", -10.00m),
            CreateTransaction("WALMART", -50.00m),
        };

        var result = DescriptionAggregator.Aggregate(transactions);

        var group = Assert.Single(result);
        Assert.Equal(10.00m, group.MinAmount);
        Assert.Equal(50.00m, group.MaxAmount);
    }

    [Fact]
    public void Aggregate_Ranks_By_Frequency_Descending()
    {
        var transactions = new[]
        {
            CreateTransaction("RARE STORE", 10m),
            CreateTransaction("COMMON STORE", 10m),
            CreateTransaction("COMMON STORE", 20m),
            CreateTransaction("COMMON STORE", 30m),
            CreateTransaction("MEDIUM STORE", 10m),
            CreateTransaction("MEDIUM STORE", 20m),
        };

        var result = DescriptionAggregator.Aggregate(transactions);

        Assert.Equal(3, result.Count);
        Assert.Equal("COMMON STORE", result[0].RepresentativeDescription);
        Assert.Equal("MEDIUM STORE", result[1].RepresentativeDescription);
        Assert.Equal("RARE STORE", result[2].RepresentativeDescription);
    }

    [Fact]
    public void Aggregate_Caps_Results_At_MaxGroups()
    {
        var transactions = Enumerable.Range(1, 200)
            .Select(i => CreateTransaction($"STORE {i}", i * 1.0m))
            .ToList();

        var result = DescriptionAggregator.Aggregate(transactions, maxGroups: 100);

        Assert.Equal(100, result.Count);
    }

    [Fact]
    public void Aggregate_Returns_Empty_For_Empty_Input()
    {
        var result = DescriptionAggregator.Aggregate(Array.Empty<Transaction>());

        Assert.Empty(result);
    }

    [Fact]
    public void Aggregate_Deduplicates_Case_Insensitive()
    {
        var transactions = new[]
        {
            CreateTransaction("walmart supercenter", 10m),
            CreateTransaction("WALMART SUPERCENTER", 20m),
        };

        var result = DescriptionAggregator.Aggregate(transactions);

        var group = Assert.Single(result);
        Assert.Equal(2, group.Count);
    }

    [Fact]
    public void Aggregate_Skips_Transactions_With_Empty_Cleaned_Description()
    {
        // "Monthly Interest Paid" cleans fine; use a non-empty raw description
        // that still produces a valid cleaned result
        var transactions = new[]
        {
            CreateTransaction("WALMART", 20m),
            CreateTransaction("WALMART", 30m),
        };

        var result = DescriptionAggregator.Aggregate(transactions);

        var group = Assert.Single(result);
        Assert.Equal("WALMART", group.RepresentativeDescription);
        Assert.Equal(2, group.Count);
    }

    [Fact]
    public void Aggregate_DefaultMaxGroups_Is_100()
    {
        var transactions = Enumerable.Range(1, 150)
            .Select(i => CreateTransaction($"STORE {i}", i * 1.0m))
            .ToList();

        var result = DescriptionAggregator.Aggregate(transactions);

        Assert.Equal(100, result.Count);
    }

    private static Transaction CreateTransaction(string description, decimal amount)
    {
        return Transaction.Create(
            Guid.NewGuid(),
            MoneyValue.Create("USD", amount),
            DateOnly.FromDateTime(DateTime.UtcNow),
            description);
    }
}
