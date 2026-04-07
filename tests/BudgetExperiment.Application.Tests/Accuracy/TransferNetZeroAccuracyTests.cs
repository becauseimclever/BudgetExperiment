// <copyright file="TransferNetZeroAccuracyTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

using Moq;

namespace BudgetExperiment.Application.Tests.Accuracy;

/// <summary>
/// Accuracy tests for transfer operations.
/// Core invariant: a transfer between two accounts must not create or destroy money.
/// The combined balance of source + destination must be identical before and after.
/// </summary>
public class TransferNetZeroAccuracyTests
{
    private static readonly DateOnly TransferDate = new(2026, 1, 15);

    private readonly Mock<ITransactionRepository> _transactionRepo = new();
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private readonly Account _source;
    private readonly Account _destination;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransferNetZeroAccuracyTests"/> class.
    /// </summary>
    public TransferNetZeroAccuracyTests()
    {
        _source = Account.Create(
            "Checking", AccountType.Checking, MoneyValue.Create("USD", 2000m), new DateOnly(2026, 1, 1));
        _destination = Account.Create(
            "Savings", AccountType.Savings, MoneyValue.Create("USD", 500m), new DateOnly(2026, 1, 1));

        _accountRepo
            .Setup(r => r.GetByIdAsync(_source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_source);
        _accountRepo
            .Setup(r => r.GetByIdAsync(_destination.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_destination);

        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(0.01)]
    [InlineData(1999.99)]
    public async Task Transfer_SourceTransaction_HasNegativeAmount(decimal amount)
    {
        Transaction? captured = null;
        _transactionRepo
            .Setup(r => r.AddAsync(
                It.Is<Transaction>(t => t.TransferDirection == TransferDirection.Source),
                It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => captured = t)
            .Returns(Task.CompletedTask);

        await CreateService().CreateAsync(BuildRequest(amount));

        Assert.NotNull(captured);
        Assert.Equal(-amount, captured!.Amount.Amount);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(0.01)]
    [InlineData(1999.99)]
    public async Task Transfer_DestinationTransaction_HasPositiveAmount(decimal amount)
    {
        Transaction? captured = null;
        _transactionRepo
            .Setup(r => r.AddAsync(
                It.Is<Transaction>(t => t.TransferDirection == TransferDirection.Destination),
                It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => captured = t)
            .Returns(Task.CompletedTask);

        await CreateService().CreateAsync(BuildRequest(amount));

        Assert.NotNull(captured);
        Assert.Equal(amount, captured!.Amount.Amount);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(250.75)]
    [InlineData(1)]
    public async Task Transfer_SourceAndDestinationAmounts_SumToZero(decimal amount)
    {
        var capturedAmounts = new List<decimal>();
        _transactionRepo
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => capturedAmounts.Add(t.Amount.Amount))
            .Returns(Task.CompletedTask);

        await CreateService().CreateAsync(BuildRequest(amount));

        Assert.Equal(2, capturedAmounts.Count);
        Assert.Equal(0m, capturedAmounts.Sum());
    }

    [Fact]
    public async Task Transfer_DoesNotChangeNetBalanceAcrossBothAccounts()
    {
        const decimal combinedBefore = 2000m + 500m;
        var capturedAmounts = new List<decimal>();
        _transactionRepo
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => capturedAmounts.Add(t.Amount.Amount))
            .Returns(Task.CompletedTask);

        await CreateService().CreateAsync(BuildRequest(750m));

        Assert.Equal(0m, capturedAmounts.Sum());
        Assert.Equal(combinedBefore, combinedBefore + capturedAmounts.Sum());
    }

    [Fact]
    public async Task Transfer_BothTransactions_HaveMatchingCurrency()
    {
        var capturedCurrencies = new List<string>();
        _transactionRepo
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>(
                (t, _) => capturedCurrencies.Add(t.Amount.Currency))
            .Returns(Task.CompletedTask);

        await CreateService().CreateAsync(BuildRequest(100m));

        Assert.Equal(2, capturedCurrencies.Count);
        Assert.All(capturedCurrencies, c => Assert.Equal("USD", c));
    }

    [Fact]
    public async Task Transfer_BothTransactions_ShareTheSameTransferId()
    {
        var capturedTransferIds = new List<Guid?>();
        _transactionRepo
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>(
                (t, _) => capturedTransferIds.Add(t.TransferId))
            .Returns(Task.CompletedTask);

        await CreateService().CreateAsync(BuildRequest(300m));

        Assert.Equal(2, capturedTransferIds.Count);
        Assert.NotNull(capturedTransferIds[0]);
        Assert.Equal(capturedTransferIds[0], capturedTransferIds[1]);
    }

    [Fact]
    public async Task Transfer_BothTransactions_HaveSameDate()
    {
        var capturedDates = new List<DateOnly>();
        _transactionRepo
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>(
                (t, _) => capturedDates.Add(t.Date))
            .Returns(Task.CompletedTask);

        await CreateService().CreateAsync(BuildRequest(100m));

        Assert.Equal(2, capturedDates.Count);
        Assert.Equal(capturedDates[0], capturedDates[1]);
    }

    [Fact]
    public async Task Transfer_SameSourceAndDestination_Throws()
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            CreateService().CreateAsync(new CreateTransferRequest
            {
                SourceAccountId = _source.Id,
                DestinationAccountId = _source.Id,
                Amount = 100m,
                Currency = "USD",
                Date = TransferDate,
            }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task Transfer_NonPositiveAmount_Throws(decimal amount)
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            CreateService().CreateAsync(BuildRequest(amount)));
    }

    private TransferService CreateService() =>
        new(_transactionRepo.Object, _accountRepo.Object, _uow.Object);

    private CreateTransferRequest BuildRequest(decimal amount) =>
        new()
        {
            SourceAccountId = _source.Id,
            DestinationAccountId = _destination.Id,
            Amount = amount,
            Currency = "USD",
            Date = TransferDate,
        };
}
