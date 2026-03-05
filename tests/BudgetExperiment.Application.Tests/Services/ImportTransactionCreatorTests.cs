// <copyright file="ImportTransactionCreatorTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Import;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using Moq;
using Shouldly;
using Xunit;

namespace BudgetExperiment.Application.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ImportTransactionCreator"/>.
/// </summary>
public class ImportTransactionCreatorTests
{
    private readonly Mock<ITransactionRepository> _transactionRepoMock;
    private readonly Mock<ICurrencyProvider> _currencyProviderMock;
    private readonly ImportTransactionCreator _creator;

    public ImportTransactionCreatorTests()
    {
        _transactionRepoMock = new Mock<ITransactionRepository>();
        _currencyProviderMock = new Mock<ICurrencyProvider>();
        _currencyProviderMock.Setup(c => c.GetCurrencyAsync(It.IsAny<CancellationToken>())).ReturnsAsync("USD");

        _creator = new ImportTransactionCreator(
            _transactionRepoMock.Object,
            _currencyProviderMock.Object);
    }

    [Fact]
    public async Task CreateTransactionsAsync_ReturnsEmptyResult_WhenNoTransactions()
    {
        // Arrange
        var account = Account.Create("Checking", AccountType.Checking);

        // Act
        var result = await _creator.CreateTransactionsAsync(account, Guid.NewGuid(), Array.Empty<ImportTransactionData>());

        // Assert
        result.CreatedIds.ShouldBeEmpty();
        result.Skipped.ShouldBe(0);
    }

    [Fact]
    public async Task CreateTransactionsAsync_CreatesTransaction_AndReturnsId()
    {
        // Arrange
        var account = Account.Create("Checking", AccountType.Checking);
        var batchId = Guid.NewGuid();
        var txData = new ImportTransactionData
        {
            Date = new DateOnly(2025, 1, 15),
            Description = "Grocery Store",
            Amount = -50.00m,
            CategorySource = CategorySource.None,
        };

        // Act
        var result = await _creator.CreateTransactionsAsync(account, batchId, new[] { txData });

        // Assert
        result.CreatedIds.Count.ShouldBe(1);
        result.Uncategorized.ShouldBe(1);
        _transactionRepoMock.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTransactionsAsync_TracksAutoCategorizationSource()
    {
        // Arrange
        var account = Account.Create("Checking", AccountType.Checking);
        var txData = new ImportTransactionData
        {
            Date = new DateOnly(2025, 1, 15),
            Description = "Test",
            Amount = -10m,
            CategorySource = CategorySource.AutoRule,
        };

        // Act
        var result = await _creator.CreateTransactionsAsync(account, Guid.NewGuid(), new[] { txData });

        // Assert
        result.AutoCategorized.ShouldBe(1);
        result.CsvCategorized.ShouldBe(0);
        result.Uncategorized.ShouldBe(0);
    }

    [Fact]
    public async Task CreateTransactionsAsync_TracksCsvCategorizationSource()
    {
        // Arrange
        var account = Account.Create("Checking", AccountType.Checking);
        var txData = new ImportTransactionData
        {
            Date = new DateOnly(2025, 1, 15),
            Description = "Test",
            Amount = -10m,
            CategorySource = CategorySource.CsvColumn,
        };

        // Act
        var result = await _creator.CreateTransactionsAsync(account, Guid.NewGuid(), new[] { txData });

        // Assert
        result.CsvCategorized.ShouldBe(1);
    }

    [Fact]
    public async Task CreateTransactionsAsync_SetsLocationOnTransaction_WhenLocationDataPresent()
    {
        // Arrange
        var account = Account.Create("Checking", AccountType.Checking);
        var txData = new ImportTransactionData
        {
            Date = new DateOnly(2025, 1, 15),
            Description = "Store in NYC",
            Amount = -25m,
            LocationCity = "New York",
            LocationStateOrRegion = "NY",
            LocationSource = "Parsed",
            CategorySource = CategorySource.None,
        };

        // Act
        var result = await _creator.CreateTransactionsAsync(account, Guid.NewGuid(), new[] { txData });

        // Assert
        result.LocationEnriched.ShouldBe(1);
        result.CreatedIds.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CreateTransactionsAsync_SkipsOnDomainException()
    {
        // Arrange
        var account = Account.Create("Checking", AccountType.Checking);
        var validTx = new ImportTransactionData
        {
            Date = new DateOnly(2025, 1, 15),
            Description = "Valid",
            Amount = -10m,
            CategorySource = CategorySource.None,
        };

        // Set up repo to throw on first call, succeed on second
        var callCount = 0;
        _transactionRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Returns<Transaction, CancellationToken>((t, ct) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new DomainException("Duplicate transaction");
                }

                return Task.CompletedTask;
            });

        // Act
        var result = await _creator.CreateTransactionsAsync(
            account,
            Guid.NewGuid(),
            new[] { validTx, validTx with { Description = "Second" } });

        // Assert
        result.Skipped.ShouldBe(1);
        result.CreatedIds.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CreateTransactionsAsync_UsesCurrencyFromProvider()
    {
        // Arrange
        _currencyProviderMock.Setup(c => c.GetCurrencyAsync(It.IsAny<CancellationToken>())).ReturnsAsync("EUR");
        var account = Account.Create("Euro Account", AccountType.Checking);
        var txData = new ImportTransactionData
        {
            Date = new DateOnly(2025, 6, 1),
            Description = "Berlin Shop",
            Amount = -30m,
            CategorySource = CategorySource.None,
        };

        // Act
        var result = await _creator.CreateTransactionsAsync(account, Guid.NewGuid(), new[] { txData });

        // Assert
        result.CreatedIds.Count.ShouldBe(1);
        _currencyProviderMock.Verify(c => c.GetCurrencyAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
