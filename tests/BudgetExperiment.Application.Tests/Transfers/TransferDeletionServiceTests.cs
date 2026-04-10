// <copyright file="TransferDeletionServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Moq;

namespace BudgetExperiment.Application.Tests.Transfers;

/// <summary>
/// Unit tests for <see cref="TransferService.DeleteTransferAsync"/>.
/// Validates the atomic-deletion path (feature 146), distinct from the existing
/// non-atomic <c>DeleteAsync</c> path which is covered in the root-level
/// <c>TransferServiceTests</c>.
/// </summary>
public sealed class TransferDeletionServiceTests
{
    private readonly Mock<ITransactionRepository> _transactionRepo = new();
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    /// <summary>
    /// <see cref="TransferService"/> constructor throws when <c>transactionRepository</c> is null.
    /// </summary>
    [Fact]
    public void TransferService_Constructor_NullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new TransferService(null!, _accountRepo.Object, _uow.Object));
    }

    /// <summary>
    /// Both legs present - atomic delete called - service returns <c>true</c>.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TransferService_DeleteTransfer_TransferExists_ReturnsTrue()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        _transactionRepo
            .Setup(r => r.GetByTransferIdAsync(transferId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction> { CreateSource(transferId), CreateDest(transferId) });
        _transactionRepo
            .Setup(r => r.DeleteTransferAsync(transferId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        var result = await service.DeleteTransferAsync(transferId);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// No legs found for the given transfer ID - service returns <c>false</c>
    /// without calling the repository delete.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TransferService_DeleteTransfer_TransferNotFound_ReturnsFalse()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        _transactionRepo
            .Setup(r => r.GetByTransferIdAsync(transferId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction>());
        var service = CreateService();

        // Act
        var result = await service.DeleteTransferAsync(transferId);

        // Assert
        Assert.False(result);
        _transactionRepo.Verify(
            r => r.DeleteTransferAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Repository throws during atomic delete - exception propagates to the caller unchanged.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TransferService_DeleteTransfer_RepositoryThrows_PropagatesException()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        _transactionRepo
            .Setup(r => r.GetByTransferIdAsync(transferId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction> { CreateSource(transferId) });
        _transactionRepo
            .Setup(r => r.DeleteTransferAsync(transferId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database failure"));
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteTransferAsync(transferId));
    }

    /// <summary>
    /// Only one leg exists (orphaned state) - service still treats the transfer as found
    /// and allows deletion to proceed, returning <c>true</c>.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TransferService_DeleteTransfer_OrphanedLeg_DeletedWithoutError()
    {
        // Arrange - only the source leg survives (destination was already removed)
        var transferId = Guid.NewGuid();
        _transactionRepo
            .Setup(r => r.GetByTransferIdAsync(transferId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction> { CreateSource(transferId) });
        _transactionRepo
            .Setup(r => r.DeleteTransferAsync(transferId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        var result = await service.DeleteTransferAsync(transferId);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Confirms <see cref="ITransactionRepository.DeleteTransferAsync"/> is called exactly
    /// once when the transfer is found, delegating atomicity to the repository.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TransferService_DeleteTransfer_TransferExists_CallsRepositoryDeleteExactlyOnce()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        _transactionRepo
            .Setup(r => r.GetByTransferIdAsync(transferId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction> { CreateSource(transferId), CreateDest(transferId) });
        _transactionRepo
            .Setup(r => r.DeleteTransferAsync(transferId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.DeleteTransferAsync(transferId);

        // Assert
        _transactionRepo.Verify(
            r => r.DeleteTransferAsync(transferId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Transaction CreateSource(Guid transferId) =>
        TransactionFactory.CreateTransfer(
            Guid.NewGuid(),
            MoneyValue.Create("USD", -100m),
            new DateOnly(2060, 3, 1),
            "Transfer to Savings: Test",
            transferId,
            TransferDirection.Source);

    private static Transaction CreateDest(Guid transferId) =>
        TransactionFactory.CreateTransfer(
            Guid.NewGuid(),
            MoneyValue.Create("USD", 100m),
            new DateOnly(2060, 3, 1),
            "Transfer from Checking: Test",
            transferId,
            TransferDirection.Destination);

    private TransferService CreateService() =>
        new(_transactionRepo.Object, _accountRepo.Object, _uow.Object);
}
