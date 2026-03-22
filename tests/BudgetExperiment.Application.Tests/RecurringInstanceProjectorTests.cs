// <copyright file="RecurringInstanceProjectorTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Common;

using Moq;

using Shouldly;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for <see cref="RecurringInstanceProjector"/>.
/// </summary>
public class RecurringInstanceProjectorTests
{
    private readonly Mock<IRecurringTransactionRepository> _recurringRepository;
    private readonly Mock<IAccountRepository> _accountRepository;

    public RecurringInstanceProjectorTests()
    {
        _recurringRepository = new Mock<IRecurringTransactionRepository>();
        _accountRepository = new Mock<IAccountRepository>();

        _accountRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());
    }

    [Fact]
    public async Task GetInstancesByDateRangeAsync_NoTransactions_ReturnsEmptyDictionary()
    {
        var projector = CreateProjector();
        var transactions = new List<RecurringTransaction>();

        var result = await projector.GetInstancesByDateRangeAsync(
            transactions, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetInstancesByDateRangeAsync_InactiveTransaction_IsExcluded()
    {
        var recurring = CreateRecurring("Netflix", -15.99m, new DateOnly(2026, 1, 1));
        recurring.Pause();

        SetupNoExceptions(recurring.Id);

        var projector = CreateProjector();

        var result = await projector.GetInstancesByDateRangeAsync(
            new List<RecurringTransaction> { recurring },
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetInstancesByDateRangeAsync_SkippedException_InstanceOmitted()
    {
        var recurring = CreateRecurring("Netflix", -15.99m, new DateOnly(2026, 1, 1));
        var date = new DateOnly(2026, 1, 1);

        var skippedException = RecurringTransactionException.CreateSkipped(recurring.Id, date);

        _recurringRepository
            .Setup(r => r.GetExceptionsByDateRangeAsync(
                recurring.Id, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransactionException> { skippedException });

        var projector = CreateProjector();

        var result = await projector.GetInstancesByDateRangeAsync(
            new List<RecurringTransaction> { recurring },
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.ShouldNotContainKey(date);
    }

    [Fact]
    public async Task GetInstancesByDateRangeAsync_ModifiedException_UsesModifiedValues()
    {
        var recurring = CreateRecurring("Netflix", -15.99m, new DateOnly(2026, 1, 1));
        var date = new DateOnly(2026, 1, 1);

        var modifiedException = RecurringTransactionException.CreateModified(
            recurring.Id, date, MoneyValue.Create("USD", -19.99m), "Netflix Premium", null);

        _recurringRepository
            .Setup(r => r.GetExceptionsByDateRangeAsync(
                recurring.Id, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransactionException> { modifiedException });

        var projector = CreateProjector();

        var result = await projector.GetInstancesByDateRangeAsync(
            new List<RecurringTransaction> { recurring },
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        result.ShouldContainKey(date);
        var instance = result[date].ShouldHaveSingleItem();
        instance.Description.ShouldBe("Netflix Premium");
        instance.Amount.Amount.ShouldBe(-19.99m);
        instance.IsModified.ShouldBeTrue();
    }

    [Fact]
    public async Task GetInstancesByDateRangeAsync_NoException_UsesBaseValues()
    {
        var recurring = CreateRecurring("Netflix", -15.99m, new DateOnly(2026, 1, 1));

        SetupNoExceptions(recurring.Id);

        var projector = CreateProjector();

        var result = await projector.GetInstancesByDateRangeAsync(
            new List<RecurringTransaction> { recurring },
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        var date = new DateOnly(2026, 1, 1);
        result.ShouldContainKey(date);
        var instance = result[date].ShouldHaveSingleItem();
        instance.Description.ShouldBe("Netflix");
        instance.Amount.Amount.ShouldBe(-15.99m);
        instance.IsModified.ShouldBeFalse();
        instance.RecurringTransactionId.ShouldBe(recurring.Id);
    }

    [Fact]
    public async Task GetInstancesByDateRangeAsync_MapsAccountName()
    {
        var accountId = Guid.NewGuid();
        var recurring = CreateRecurring("Netflix", -15.99m, new DateOnly(2026, 1, 1), accountId);
        var account = Account.Create("Checking", AccountType.Checking);
        typeof(Account).GetProperty(nameof(Account.Id))!.SetValue(account, accountId);

        _accountRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });

        SetupNoExceptions(recurring.Id);

        var projector = CreateProjector();

        var result = await projector.GetInstancesByDateRangeAsync(
            new List<RecurringTransaction> { recurring },
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        var instance = result[new DateOnly(2026, 1, 1)].ShouldHaveSingleItem();
        instance.AccountName.ShouldBe("Checking");
    }

    [Fact]
    public async Task GetInstancesForDateAsync_NoOccurrence_ReturnsEmpty()
    {
        var recurring = CreateRecurring("Netflix", -15.99m, new DateOnly(2026, 1, 1));

        var projector = CreateProjector();

        var result = await projector.GetInstancesForDateAsync(
            new List<RecurringTransaction> { recurring },
            new DateOnly(2026, 1, 15));

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetInstancesForDateAsync_WithModifiedException_UsesModifiedValues()
    {
        var recurring = CreateRecurring("Netflix", -15.99m, new DateOnly(2026, 1, 1));
        var date = new DateOnly(2026, 1, 1);

        var modifiedException = RecurringTransactionException.CreateModified(
            recurring.Id, date, MoneyValue.Create("USD", -25.00m), "Netflix 4K", null);

        _recurringRepository
            .Setup(r => r.GetExceptionAsync(recurring.Id, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(modifiedException);

        var projector = CreateProjector();

        var result = await projector.GetInstancesForDateAsync(
            new List<RecurringTransaction> { recurring }, date);

        var instance = result.ShouldHaveSingleItem();
        instance.Description.ShouldBe("Netflix 4K");
        instance.Amount.Amount.ShouldBe(-25.00m);
        instance.IsModified.ShouldBeTrue();
    }

    [Fact]
    public async Task GetInstancesForDateAsync_SkippedException_SetsIsSkipped()
    {
        var recurring = CreateRecurring("Netflix", -15.99m, new DateOnly(2026, 1, 1));
        var date = new DateOnly(2026, 1, 1);

        var skippedException = RecurringTransactionException.CreateSkipped(recurring.Id, date);

        _recurringRepository
            .Setup(r => r.GetExceptionAsync(recurring.Id, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(skippedException);

        var projector = CreateProjector();

        var result = await projector.GetInstancesForDateAsync(
            new List<RecurringTransaction> { recurring }, date);

        var instance = result.ShouldHaveSingleItem();
        instance.IsSkipped.ShouldBeTrue();
    }

    private static RecurringTransaction CreateRecurring(string description, decimal amount, DateOnly startDate, Guid? accountId = null)
    {
        var recurring = RecurringTransaction.Create(
            accountId ?? Guid.NewGuid(),
            description,
            MoneyValue.Create("USD", amount),
            RecurrencePatternValue.CreateMonthly(1, startDate.Day),
            startDate);
        return recurring;
    }

    private void SetupNoExceptions(Guid recurringId)
    {
        _recurringRepository
            .Setup(r => r.GetExceptionsByDateRangeAsync(
                recurringId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransactionException>());
    }

    private RecurringInstanceProjector CreateProjector()
    {
        return new RecurringInstanceProjector(
            _recurringRepository.Object,
            _accountRepository.Object);
    }
}
