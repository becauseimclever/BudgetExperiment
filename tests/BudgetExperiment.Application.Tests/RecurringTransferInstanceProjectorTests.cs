// <copyright file="RecurringTransferInstanceProjectorTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Common;

using Moq;

using Shouldly;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for <see cref="RecurringTransferInstanceProjector"/>.
/// </summary>
public class RecurringTransferInstanceProjectorTests
{
    private readonly Mock<IRecurringTransferRepository> _transferRepository;
    private readonly Mock<IAccountRepository> _accountRepository;

    public RecurringTransferInstanceProjectorTests()
    {
        _transferRepository = new Mock<IRecurringTransferRepository>();
        _accountRepository = new Mock<IAccountRepository>();

        _accountRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());
    }

    [Fact]
    public async Task GetInstancesByDateRangeAsync_NoTransfers_ReturnsEmptyDictionary()
    {
        var projector = CreateProjector();

        var result = await projector.GetInstancesByDateRangeAsync(
            new List<RecurringTransfer>(),
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetInstancesByDateRangeAsync_ActiveTransfer_CreatesBothSourceAndDestinationEntries()
    {
        var sourceAccountId = Guid.NewGuid();
        var destAccountId = Guid.NewGuid();
        var transfer = CreateTransfer(sourceAccountId, destAccountId, "Savings Transfer", 500m, new DateOnly(2026, 1, 1));

        SetupAccounts(sourceAccountId, "Checking", destAccountId, "Savings");
        SetupNoExceptions(transfer.Id);

        var projector = CreateProjector();

        var result = await projector.GetInstancesByDateRangeAsync(
            new List<RecurringTransfer> { transfer },
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        var date = new DateOnly(2026, 1, 1);
        result.ShouldContainKey(date);
        result[date].Count.ShouldBe(2);

        var source = result[date].First(i => i.TransferDirection == "Source");
        source.Amount.Amount.ShouldBe(-500m);
        source.AccountId.ShouldBe(sourceAccountId);
        source.Description.ShouldContain("Transfer to Savings");

        var dest = result[date].First(i => i.TransferDirection == "Destination");
        dest.Amount.Amount.ShouldBe(500m);
        dest.AccountId.ShouldBe(destAccountId);
        dest.Description.ShouldContain("Transfer from Checking");
    }

    [Fact]
    public async Task GetInstancesByDateRangeAsync_InactiveTransfer_IsExcluded()
    {
        var transfer = CreateTransfer(Guid.NewGuid(), Guid.NewGuid(), "Transfer", 100m, new DateOnly(2026, 1, 1));
        transfer.Pause();

        var projector = CreateProjector();

        var result = await projector.GetInstancesByDateRangeAsync(
            new List<RecurringTransfer> { transfer },
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetInstancesByDateRangeAsync_SkippedException_InstanceOmitted()
    {
        var transfer = CreateTransfer(Guid.NewGuid(), Guid.NewGuid(), "Transfer", 100m, new DateOnly(2026, 1, 1));
        var date = new DateOnly(2026, 1, 1);

        var skippedException = RecurringTransferException.CreateSkipped(transfer.Id, date);

        _transferRepository
            .Setup(r => r.GetExceptionsByDateRangeAsync(
                transfer.Id, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransferException> { skippedException });

        var projector = CreateProjector();

        var result = await projector.GetInstancesByDateRangeAsync(
            new List<RecurringTransfer> { transfer },
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.ShouldNotContainKey(date);
    }

    [Fact]
    public async Task GetInstancesByDateRangeAsync_ModifiedException_UsesModifiedValues()
    {
        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        var transfer = CreateTransfer(sourceId, destId, "Transfer", 100m, new DateOnly(2026, 1, 1));
        var date = new DateOnly(2026, 1, 1);

        var modifiedException = RecurringTransferException.CreateModified(
            transfer.Id, date, MoneyValue.Create("USD", 200m), "Extra Transfer", null);

        _transferRepository
            .Setup(r => r.GetExceptionsByDateRangeAsync(
                transfer.Id, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransferException> { modifiedException });

        SetupAccounts(sourceId, "Checking", destId, "Savings");

        var projector = CreateProjector();

        var result = await projector.GetInstancesByDateRangeAsync(
            new List<RecurringTransfer> { transfer },
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.ShouldContainKey(date);
        var source = result[date].First(i => i.TransferDirection == "Source");
        source.Amount.Amount.ShouldBe(-200m);
        source.IsModified.ShouldBeTrue();

        var dest = result[date].First(i => i.TransferDirection == "Destination");
        dest.Amount.Amount.ShouldBe(200m);
        dest.IsModified.ShouldBeTrue();
    }

    [Fact]
    public async Task GetInstancesByDateRangeAsync_WithAccountFilter_ReturnsOnlyMatchingSide()
    {
        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        var transfer = CreateTransfer(sourceId, destId, "Transfer", 100m, new DateOnly(2026, 1, 1));

        SetupAccounts(sourceId, "Checking", destId, "Savings");
        SetupNoExceptions(transfer.Id);

        var projector = CreateProjector();

        var result = await projector.GetInstancesByDateRangeAsync(
            new List<RecurringTransfer> { transfer },
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            accountId: sourceId);

        var date = new DateOnly(2026, 1, 1);
        result.ShouldContainKey(date);
        result[date].ShouldHaveSingleItem();
        result[date][0].TransferDirection.ShouldBe("Source");
    }

    [Fact]
    public async Task GetInstancesForDateAsync_SourceGetsNegativeAmount_DestinationGetsPositive()
    {
        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        var transfer = CreateTransfer(sourceId, destId, "Transfer", 300m, new DateOnly(2026, 1, 1));
        var date = new DateOnly(2026, 1, 1);

        SetupAccounts(sourceId, "Checking", destId, "Savings");

        _transferRepository
            .Setup(r => r.GetExceptionAsync(transfer.Id, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringTransferException?)null);

        var projector = CreateProjector();

        var result = await projector.GetInstancesForDateAsync(
            new List<RecurringTransfer> { transfer }, date);

        result.Count.ShouldBe(2);
        result.First(i => i.TransferDirection == "Source").Amount.Amount.ShouldBe(-300m);
        result.First(i => i.TransferDirection == "Destination").Amount.Amount.ShouldBe(300m);
    }

    private static RecurringTransfer CreateTransfer(Guid sourceAccountId, Guid destAccountId, string description, decimal amount, DateOnly startDate)
    {
        return RecurringTransfer.Create(
            sourceAccountId,
            destAccountId,
            description,
            MoneyValue.Create("USD", amount),
            RecurrencePatternValue.CreateMonthly(1, startDate.Day),
            startDate);
    }

    private void SetupAccounts(Guid sourceId, string sourceName, Guid destId, string destName)
    {
        var source = Account.Create(sourceName, AccountType.Checking);
        typeof(Account).GetProperty(nameof(Account.Id))!.SetValue(source, sourceId);
        var dest = Account.Create(destName, AccountType.Savings);
        typeof(Account).GetProperty(nameof(Account.Id))!.SetValue(dest, destId);

        _accountRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { source, dest });
    }

    private void SetupNoExceptions(Guid transferId)
    {
        _transferRepository
            .Setup(r => r.GetExceptionsByDateRangeAsync(
                transferId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransferException>());
    }

    private RecurringTransferInstanceProjector CreateProjector()
    {
        return new RecurringTransferInstanceProjector(
            _transferRepository.Object,
            _accountRepository.Object);
    }
}
