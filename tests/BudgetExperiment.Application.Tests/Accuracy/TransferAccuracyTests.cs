// <copyright file="TransferAccuracyTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

using Moq;

namespace BudgetExperiment.Application.Tests.Accuracy;

/// <summary>
/// Accuracy tests for transfer operations: verifies that no money is created or
/// destroyed and that decimal precision is preserved end-to-end.
/// Complements <see cref="TransferNetZeroAccuracyTests"/> with additional edge cases.
/// </summary>
[Trait("Category", "Accuracy")]
public class TransferAccuracyTests
{
    private static readonly DateOnly TransferDate = new(2026, 1, 15);

    private readonly Mock<ITransactionRepository> _transactionRepo = new();
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private readonly Account _source;
    private readonly Account _destination;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransferAccuracyTests"/> class.
    /// </summary>
    public TransferAccuracyTests()
    {
        _source = Account.Create(
            "Source", AccountType.Checking, MoneyValue.Create("USD", 5000m), new DateOnly(2026, 1, 1));
        _destination = Account.Create(
            "Destination", AccountType.Savings, MoneyValue.Create("USD", 0m), new DateOnly(2026, 1, 1));

        _accountRepo
            .Setup(r => r.GetByIdAsync(_source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_source);
        _accountRepo
            .Setup(r => r.GetByIdAsync(_destination.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_destination);

        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);

        _transactionRepo
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Theory]
    [InlineData(1234.56)]
    [InlineData(0.01)]
    [InlineData(9999.99)]
    public async Task Transfer_ArbitraryDecimalAmount_SourceAndDestinationAmountsSumToZero(double rawAmount)
    {
        var amount = (decimal)rawAmount;
        var capturedAmounts = new List<decimal>();
        _transactionRepo
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => capturedAmounts.Add(t.Amount.Amount))
            .Returns(Task.CompletedTask);

        await CreateService().CreateAsync(BuildRequest(amount));

        Assert.Equal(0m, capturedAmounts.Sum());
        Assert.Equal(-amount, capturedAmounts.Min());
        Assert.Equal(amount, capturedAmounts.Max());
    }

    [Fact]
    public async Task Transfer_CreatesExactlyTwoTransactions()
    {
        var callCount = 0;
        _transactionRepo
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((_, _) => callCount++)
            .Returns(Task.CompletedTask);

        await CreateService().CreateAsync(BuildRequest(500m));

        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task Transfer_SourceAmountIsNegative_DestinationAmountIsPositive()
    {
        var capturedTransactions = new List<Transaction>();
        _transactionRepo
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => capturedTransactions.Add(t))
            .Returns(Task.CompletedTask);

        await CreateService().CreateAsync(BuildRequest(750m));

        var sourceTx = capturedTransactions.Single(t => t.TransferDirection == TransferDirection.Source);
        var destTx = capturedTransactions.Single(t => t.TransferDirection == TransferDirection.Destination);

        Assert.Equal(-750m, sourceTx.Amount.Amount);
        Assert.Equal(750m, destTx.Amount.Amount);
    }

    [Fact]
    public async Task Transfer_AbsoluteAmountMatch_SourceLossEqualDestinationGain()
    {
        var capturedAmounts = new List<decimal>();
        _transactionRepo
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => capturedAmounts.Add(t.Amount.Amount))
            .Returns(Task.CompletedTask);

        const decimal transferAmount = 333.33m;
        await CreateService().CreateAsync(BuildRequest(transferAmount));

        Assert.Equal(2, capturedAmounts.Count);
        Assert.Equal(Math.Abs(capturedAmounts[0]), Math.Abs(capturedAmounts[1]));
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
