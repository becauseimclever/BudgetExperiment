// <copyright file="ImportBatchManagerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Import;
using BudgetExperiment.Domain;
using Moq;
using Shouldly;
using Xunit;

namespace BudgetExperiment.Application.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ImportBatchManager"/>.
/// </summary>
public class ImportBatchManagerTests
{
    private readonly Mock<IImportBatchRepository> _batchRepoMock;
    private readonly Mock<IImportMappingRepository> _mappingRepoMock;
    private readonly Mock<IAccountRepository> _accountRepoMock;
    private readonly Mock<ITransactionRepository> _transactionRepoMock;
    private readonly Mock<IUserContext> _userContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ImportBatchManager _manager;

    public ImportBatchManagerTests()
    {
        _batchRepoMock = new Mock<IImportBatchRepository>();
        _mappingRepoMock = new Mock<IImportMappingRepository>();
        _accountRepoMock = new Mock<IAccountRepository>();
        _transactionRepoMock = new Mock<ITransactionRepository>();
        _userContextMock = new Mock<IUserContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _userContextMock.Setup(u => u.UserIdAsGuid).Returns(Guid.NewGuid());
        _userContextMock.Setup(u => u.UserId).Returns("test-user");

        _manager = new ImportBatchManager(
            _batchRepoMock.Object,
            _mappingRepoMock.Object,
            _accountRepoMock.Object,
            _transactionRepoMock.Object,
            _userContextMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetImportHistoryAsync_ReturnsEmpty_WhenNoBatches()
    {
        // Arrange
        _batchRepoMock
            .Setup(r => r.GetByUserAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ImportBatch>());

        // Act
        var result = await _manager.GetImportHistoryAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetImportHistoryAsync_MapsBatchToDto()
    {
        // Arrange
        var userId = _userContextMock.Object.UserIdAsGuid!.Value;
        var accountId = Guid.NewGuid();
        var batch = ImportBatch.Create(userId, accountId, "test.csv", 10, null);
        batch.Complete(10, 0, 0);

        _batchRepoMock
            .Setup(r => r.GetByUserAsync(userId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ImportBatch> { batch });

        var account = Account.Create("Checking", AccountType.Checking);
        _accountRepoMock
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // Act
        var result = await _manager.GetImportHistoryAsync();

        // Assert
        result.Count.ShouldBe(1);
        result[0].FileName.ShouldBe("test.csv");
        result[0].AccountName.ShouldBe("Checking");
    }

    [Fact]
    public async Task GetImportHistoryAsync_IncludesMappingName_WhenPresent()
    {
        // Arrange
        var userId = _userContextMock.Object.UserIdAsGuid!.Value;
        var accountId = Guid.NewGuid();
        var mappingId = Guid.NewGuid();
        var batch = ImportBatch.Create(userId, accountId, "test.csv", 10, mappingId);
        batch.Complete(10, 0, 0);

        _batchRepoMock
            .Setup(r => r.GetByUserAsync(userId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ImportBatch> { batch });

        _accountRepoMock
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Account.Create("Checking", AccountType.Checking));

        var mapping = ImportMapping.Create(
            _userContextMock.Object.UserIdAsGuid!.Value,
            "BOA Format",
            new List<ColumnMappingValue> { new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date } });
        _mappingRepoMock
            .Setup(r => r.GetByIdAsync(mappingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapping);

        // Act
        var result = await _manager.GetImportHistoryAsync();

        // Assert
        result[0].MappingName.ShouldBe("BOA Format");
    }

    [Fact]
    public async Task DeleteImportBatchAsync_ReturnsZero_WhenBatchNotFound()
    {
        // Arrange
        _batchRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImportBatch?)null);

        // Act
        var result = await _manager.DeleteImportBatchAsync(Guid.NewGuid());

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public async Task DeleteImportBatchAsync_ThrowsDomainException_WhenOwnedByAnotherUser()
    {
        // Arrange
        var otherUser = Guid.NewGuid();
        var batch = ImportBatch.Create(otherUser, Guid.NewGuid(), "test.csv", 5, null);

        _batchRepoMock
            .Setup(r => r.GetByIdAsync(batch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(batch);

        // Act & Assert
        await Should.ThrowAsync<DomainException>(
            () => _manager.DeleteImportBatchAsync(batch.Id));
    }

    [Fact]
    public async Task DeleteImportBatchAsync_DeletesTransactions_AndReturnsCou()
    {
        // Arrange
        var userId = _userContextMock.Object.UserIdAsGuid!.Value;
        var batch = ImportBatch.Create(userId, Guid.NewGuid(), "test.csv", 3, null);

        _batchRepoMock
            .Setup(r => r.GetByIdAsync(batch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(batch);

        var account = Account.Create("Checking", AccountType.Checking);
        var txn1 = account.AddTransaction(MoneyValue.Create("USD", -10m), DateOnly.FromDateTime(DateTime.UtcNow), "Test1");
        var txn2 = account.AddTransaction(MoneyValue.Create("USD", -20m), DateOnly.FromDateTime(DateTime.UtcNow), "Test2");

        _transactionRepoMock
            .Setup(r => r.GetByImportBatchAsync(batch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction> { txn1, txn2 });

        // Act
        var result = await _manager.DeleteImportBatchAsync(batch.Id);

        // Assert
        result.ShouldBe(2);
        _transactionRepoMock.Verify(r => r.RemoveAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
