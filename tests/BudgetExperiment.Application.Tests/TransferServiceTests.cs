// <copyright file="TransferServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

using BudgetExperiment.Domain;
using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for TransferService.
/// </summary>
public class TransferServiceTests
{
    private readonly Mock<ITransactionRepository> _transactionRepo;
    private readonly Mock<IAccountRepository> _accountRepo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly TransferService _service;
    private readonly Account _sourceAccount;
    private readonly Account _destinationAccount;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransferServiceTests"/> class.
    /// </summary>
    public TransferServiceTests()
    {
        this._transactionRepo = new Mock<ITransactionRepository>();
        this._accountRepo = new Mock<IAccountRepository>();
        this._uow = new Mock<IUnitOfWork>();
        this._service = new TransferService(
            this._transactionRepo.Object,
            this._accountRepo.Object,
            this._uow.Object);

        this._sourceAccount = Account.Create("Checking", AccountType.Checking);
        this._destinationAccount = Account.Create("Savings", AccountType.Savings);
    }

    [Fact]
    public async Task CreateAsync_Creates_Paired_Transactions()
    {
        // Arrange
        this._accountRepo.Setup(r => r.GetByIdAsync(this._sourceAccount.Id, default))
            .ReturnsAsync(this._sourceAccount);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._destinationAccount.Id, default))
            .ReturnsAsync(this._destinationAccount);
        this._uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(2);

        var request = new CreateTransferRequest
        {
            SourceAccountId = this._sourceAccount.Id,
            DestinationAccountId = this._destinationAccount.Id,
            Amount = 500m,
            Currency = "USD",
            Date = new DateOnly(2026, 1, 10),
            Description = "Monthly savings",
        };

        // Act
        var result = await this._service.CreateAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, result.TransferId);
        Assert.Equal(this._sourceAccount.Id, result.SourceAccountId);
        Assert.Equal(this._sourceAccount.Name, result.SourceAccountName);
        Assert.Equal(this._destinationAccount.Id, result.DestinationAccountId);
        Assert.Equal(this._destinationAccount.Name, result.DestinationAccountName);
        Assert.Equal(500m, result.Amount);
        Assert.Equal("USD", result.Currency);
        Assert.Equal(new DateOnly(2026, 1, 10), result.Date);
        Assert.Equal("Monthly savings", result.Description);

        // Verify both transactions were added
        this._transactionRepo.Verify(
            r => r.AddAsync(
            It.Is<Transaction>(t => t.TransferDirection == TransferDirection.Source && t.Amount.Amount == -500m),
            default),
            Times.Once);
        this._transactionRepo.Verify(
            r => r.AddAsync(
            It.Is<Transaction>(t => t.TransferDirection == TransferDirection.Destination && t.Amount.Amount == 500m),
            default),
            Times.Once);
        this._uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Throws_When_SourceAndDestinationSame()
    {
        // Arrange
        var request = new CreateTransferRequest
        {
            SourceAccountId = this._sourceAccount.Id,
            DestinationAccountId = this._sourceAccount.Id,
            Amount = 500m,
            Date = new DateOnly(2026, 1, 10),
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            this._service.CreateAsync(request));
        Assert.Contains("different", ex.Message);
        this._uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_Throws_When_AmountNotPositive()
    {
        // Arrange
        var request = new CreateTransferRequest
        {
            SourceAccountId = this._sourceAccount.Id,
            DestinationAccountId = this._destinationAccount.Id,
            Amount = 0m,
            Date = new DateOnly(2026, 1, 10),
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            this._service.CreateAsync(request));
        Assert.Contains("positive", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_Throws_When_NegativeAmount()
    {
        // Arrange
        var request = new CreateTransferRequest
        {
            SourceAccountId = this._sourceAccount.Id,
            DestinationAccountId = this._destinationAccount.Id,
            Amount = -100m,
            Date = new DateOnly(2026, 1, 10),
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            this._service.CreateAsync(request));
        Assert.Contains("positive", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_Throws_When_SourceAccountNotFound()
    {
        // Arrange
        this._accountRepo.Setup(r => r.GetByIdAsync(this._sourceAccount.Id, default))
            .ReturnsAsync((Account?)null);

        var request = new CreateTransferRequest
        {
            SourceAccountId = this._sourceAccount.Id,
            DestinationAccountId = this._destinationAccount.Id,
            Amount = 500m,
            Date = new DateOnly(2026, 1, 10),
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            this._service.CreateAsync(request));
        Assert.Contains("Source account not found", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_Throws_When_DestinationAccountNotFound()
    {
        // Arrange
        this._accountRepo.Setup(r => r.GetByIdAsync(this._sourceAccount.Id, default))
            .ReturnsAsync(this._sourceAccount);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._destinationAccount.Id, default))
            .ReturnsAsync((Account?)null);

        var request = new CreateTransferRequest
        {
            SourceAccountId = this._sourceAccount.Id,
            DestinationAccountId = this._destinationAccount.Id,
            Amount = 500m,
            Date = new DateOnly(2026, 1, 10),
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            this._service.CreateAsync(request));
        Assert.Contains("Destination account not found", ex.Message);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Transfer_When_Found()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        var sourceTransaction = Transaction.CreateTransfer(
            this._sourceAccount.Id,
            MoneyValue.Create("USD", -500m),
            new DateOnly(2026, 1, 10),
            $"Transfer to {this._destinationAccount.Name}: Monthly savings",
            transferId,
            TransferDirection.Source);
        var destinationTransaction = Transaction.CreateTransfer(
            this._destinationAccount.Id,
            MoneyValue.Create("USD", 500m),
            new DateOnly(2026, 1, 10),
            $"Transfer from {this._sourceAccount.Name}: Monthly savings",
            transferId,
            TransferDirection.Destination);

        this._transactionRepo.Setup(r => r.GetByTransferIdAsync(transferId, default))
            .ReturnsAsync(new List<Transaction> { sourceTransaction, destinationTransaction });
        this._accountRepo.Setup(r => r.GetByIdAsync(this._sourceAccount.Id, default))
            .ReturnsAsync(this._sourceAccount);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._destinationAccount.Id, default))
            .ReturnsAsync(this._destinationAccount);

        // Act
        var result = await this._service.GetByIdAsync(transferId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(transferId, result.TransferId);
        Assert.Equal(this._sourceAccount.Id, result.SourceAccountId);
        Assert.Equal(this._destinationAccount.Id, result.DestinationAccountId);
        Assert.Equal(500m, result.Amount);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_NotFound()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        this._transactionRepo.Setup(r => r.GetByTransferIdAsync(transferId, default))
            .ReturnsAsync(new List<Transaction>());

        // Act
        var result = await this._service.GetByIdAsync(transferId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_Removes_Both_Transactions()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        var sourceTransaction = Transaction.CreateTransfer(
            this._sourceAccount.Id,
            MoneyValue.Create("USD", -500m),
            new DateOnly(2026, 1, 10),
            "Transfer to Savings: Test",
            transferId,
            TransferDirection.Source);
        var destinationTransaction = Transaction.CreateTransfer(
            this._destinationAccount.Id,
            MoneyValue.Create("USD", 500m),
            new DateOnly(2026, 1, 10),
            "Transfer from Checking: Test",
            transferId,
            TransferDirection.Destination);

        this._transactionRepo.Setup(r => r.GetByTransferIdAsync(transferId, default))
            .ReturnsAsync(new List<Transaction> { sourceTransaction, destinationTransaction });
        this._uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(2);

        // Act
        var result = await this._service.DeleteAsync(transferId);

        // Assert
        Assert.True(result);
        this._transactionRepo.Verify(r => r.RemoveAsync(sourceTransaction, default), Times.Once);
        this._transactionRepo.Verify(r => r.RemoveAsync(destinationTransaction, default), Times.Once);
        this._uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Returns_False_When_NotFound()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        this._transactionRepo.Setup(r => r.GetByTransferIdAsync(transferId, default))
            .ReturnsAsync(new List<Transaction>());

        // Act
        var result = await this._service.DeleteAsync(transferId);

        // Assert
        Assert.False(result);
        this._uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Updates_Both_Transactions()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        var sourceTransaction = Transaction.CreateTransfer(
            this._sourceAccount.Id,
            MoneyValue.Create("USD", -500m),
            new DateOnly(2026, 1, 10),
            "Transfer to Savings: Original",
            transferId,
            TransferDirection.Source);
        var destinationTransaction = Transaction.CreateTransfer(
            this._destinationAccount.Id,
            MoneyValue.Create("USD", 500m),
            new DateOnly(2026, 1, 10),
            "Transfer from Checking: Original",
            transferId,
            TransferDirection.Destination);

        this._transactionRepo.Setup(r => r.GetByTransferIdAsync(transferId, default))
            .ReturnsAsync(new List<Transaction> { sourceTransaction, destinationTransaction });
        this._accountRepo.Setup(r => r.GetByIdAsync(this._sourceAccount.Id, default))
            .ReturnsAsync(this._sourceAccount);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._destinationAccount.Id, default))
            .ReturnsAsync(this._destinationAccount);
        this._uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(2);

        var request = new UpdateTransferRequest
        {
            Amount = 750m,
            Currency = "USD",
            Date = new DateOnly(2026, 1, 15),
            Description = "Updated transfer",
        };

        // Act
        var result = await this._service.UpdateAsync(transferId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(750m, result.Amount);
        Assert.Equal(new DateOnly(2026, 1, 15), result.Date);
        Assert.Equal("Updated transfer", result.Description);
        this._uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Returns_Null_When_NotFound()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        this._transactionRepo.Setup(r => r.GetByTransferIdAsync(transferId, default))
            .ReturnsAsync(new List<Transaction>());

        var request = new UpdateTransferRequest
        {
            Amount = 750m,
            Currency = "USD",
            Date = new DateOnly(2026, 1, 15),
        };

        // Act
        var result = await this._service.UpdateAsync(transferId, request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_Throws_When_AmountNotPositive()
    {
        // Arrange
        var request = new UpdateTransferRequest
        {
            Amount = 0m,
            Currency = "USD",
            Date = new DateOnly(2026, 1, 15),
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            this._service.UpdateAsync(Guid.NewGuid(), request));
        Assert.Contains("positive", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_Uses_Default_Description_When_Empty()
    {
        // Arrange
        this._accountRepo.Setup(r => r.GetByIdAsync(this._sourceAccount.Id, default))
            .ReturnsAsync(this._sourceAccount);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._destinationAccount.Id, default))
            .ReturnsAsync(this._destinationAccount);
        this._uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(2);

        var request = new CreateTransferRequest
        {
            SourceAccountId = this._sourceAccount.Id,
            DestinationAccountId = this._destinationAccount.Id,
            Amount = 100m,
            Currency = "USD",
            Date = new DateOnly(2026, 1, 10),
            Description = null,
        };

        // Act
        var result = await this._service.CreateAsync(request);

        // Assert
        Assert.Null(result.Description); // Extracted description should be null for default
        this._transactionRepo.Verify(
            r => r.AddAsync(
            It.Is<Transaction>(t => t.Description.Contains("Transfer")),
            default),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ListAsync_Returns_PagedResponse_With_Correct_TotalCount()
    {
        // Arrange — create 3 transfer pairs across a date range
        var transferId1 = Guid.NewGuid();
        var transferId2 = Guid.NewGuid();
        var transferId3 = Guid.NewGuid();

        var source1 = Transaction.CreateTransfer(this._sourceAccount.Id, MoneyValue.Create("USD", -100m), new DateOnly(2026, 1, 1), "Transfer to Savings: A", transferId1, TransferDirection.Source);
        var dest1 = Transaction.CreateTransfer(this._destinationAccount.Id, MoneyValue.Create("USD", 100m), new DateOnly(2026, 1, 1), "Transfer from Checking: A", transferId1, TransferDirection.Destination);
        var source2 = Transaction.CreateTransfer(this._sourceAccount.Id, MoneyValue.Create("USD", -200m), new DateOnly(2026, 1, 2), "Transfer to Savings: B", transferId2, TransferDirection.Source);
        var dest2 = Transaction.CreateTransfer(this._destinationAccount.Id, MoneyValue.Create("USD", 200m), new DateOnly(2026, 1, 2), "Transfer from Checking: B", transferId2, TransferDirection.Destination);
        var source3 = Transaction.CreateTransfer(this._sourceAccount.Id, MoneyValue.Create("USD", -300m), new DateOnly(2026, 1, 3), "Transfer to Savings: C", transferId3, TransferDirection.Source);
        var dest3 = Transaction.CreateTransfer(this._destinationAccount.Id, MoneyValue.Create("USD", 300m), new DateOnly(2026, 1, 3), "Transfer from Checking: C", transferId3, TransferDirection.Destination);

        this._transactionRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), null, default))
            .ReturnsAsync(new List<Transaction> { source1, dest1, source2, dest2, source3, dest3 });
        this._transactionRepo.Setup(r => r.GetByTransferIdAsync(transferId1, default)).ReturnsAsync(new List<Transaction> { source1, dest1 });
        this._transactionRepo.Setup(r => r.GetByTransferIdAsync(transferId2, default)).ReturnsAsync(new List<Transaction> { source2, dest2 });
        this._transactionRepo.Setup(r => r.GetByTransferIdAsync(transferId3, default)).ReturnsAsync(new List<Transaction> { source3, dest3 });
        this._accountRepo.Setup(r => r.GetByIdAsync(this._sourceAccount.Id, default)).ReturnsAsync(this._sourceAccount);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._destinationAccount.Id, default)).ReturnsAsync(this._destinationAccount);

        // Act — request page 1 of size 2 (out of 3 total)
        var result = await this._service.ListAsync(page: 1, pageSize: 2);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.TotalPages);
    }
}
