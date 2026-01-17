// <copyright file="RecurringTransferRepositoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure.Repositories;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Integration tests for <see cref="RecurringTransferRepository"/>.
/// </summary>
[Collection("InMemoryDb")]
public class RecurringTransferRepositoryTests
{
    private readonly InMemoryDbFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransferRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared in-memory database fixture.</param>
    public RecurringTransferRepositoryTests(InMemoryDbFixture fixture)
    {
        this._fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_Persists_RecurringTransfer()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var repository = new RecurringTransferRepository(context, FakeUserContext.CreateDefault());

        var sourceAccount = Account.Create("Source Checking", AccountType.Checking);
        var destAccount = Account.Create("Destination Savings", AccountType.Savings);
        await accountRepo.AddAsync(sourceAccount);
        await accountRepo.AddAsync(destAccount);
        await context.SaveChangesAsync();

        var recurringTransfer = RecurringTransfer.Create(
            sourceAccount.Id,
            destAccount.Id,
            "Monthly Savings",
            MoneyValue.Create("USD", 500m),
            RecurrencePattern.CreateMonthly(1, 1),
            new DateOnly(2026, 2, 1));

        // Act
        await repository.AddAsync(recurringTransfer);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringTransferRepository(verifyContext, FakeUserContext.CreateDefault());
        var retrieved = await verifyRepo.GetByIdAsync(recurringTransfer.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(recurringTransfer.Id, retrieved.Id);
        Assert.Equal(sourceAccount.Id, retrieved.SourceAccountId);
        Assert.Equal(destAccount.Id, retrieved.DestinationAccountId);
        Assert.Equal("Monthly Savings", retrieved.Description);
        Assert.Equal(500m, retrieved.Amount.Amount);
        Assert.Equal("USD", retrieved.Amount.Currency);
        Assert.Equal(RecurrenceFrequency.Monthly, retrieved.RecurrencePattern.Frequency);
        Assert.Equal(1, retrieved.RecurrencePattern.DayOfMonth);
        Assert.True(retrieved.IsActive);
    }

    [Fact]
    public async Task GetByAccountIdAsync_Returns_Transfers_As_Source_Or_Destination()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var repository = new RecurringTransferRepository(context, FakeUserContext.CreateDefault());

        var account1 = Account.Create("Account 1", AccountType.Checking);
        var account2 = Account.Create("Account 2", AccountType.Savings);
        var account3 = Account.Create("Account 3", AccountType.CreditCard);
        await accountRepo.AddAsync(account1);
        await accountRepo.AddAsync(account2);
        await accountRepo.AddAsync(account3);
        await context.SaveChangesAsync();

        var transfer1 = RecurringTransfer.Create(
            account1.Id,
            account2.Id,
            "Transfer A to B",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));

        var transfer2 = RecurringTransfer.Create(
            account3.Id,
            account1.Id,
            "Transfer C to A",
            MoneyValue.Create("USD", 200m),
            RecurrencePattern.CreateWeekly(1, DayOfWeek.Friday),
            new DateOnly(2026, 1, 10));

        var transfer3 = RecurringTransfer.Create(
            account2.Id,
            account3.Id,
            "Transfer B to C",
            MoneyValue.Create("USD", 300m),
            RecurrencePattern.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));

        await repository.AddAsync(transfer1);
        await repository.AddAsync(transfer2);
        await repository.AddAsync(transfer3);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringTransferRepository(verifyContext, FakeUserContext.CreateDefault());
        var account1Transfers = await verifyRepo.GetByAccountIdAsync(account1.Id);

        // Assert - account1 is source in transfer1 and destination in transfer2
        Assert.Equal(2, account1Transfers.Count);
        Assert.Contains(account1Transfers, t => t.Description == "Transfer A to B");
        Assert.Contains(account1Transfers, t => t.Description == "Transfer C to A");
        Assert.DoesNotContain(account1Transfers, t => t.Description == "Transfer B to C");
    }

    [Fact]
    public async Task GetBySourceAccountIdAsync_Returns_Only_Source_Transfers()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var repository = new RecurringTransferRepository(context, FakeUserContext.CreateDefault());

        var source = Account.Create("Source Only", AccountType.Checking);
        var dest1 = Account.Create("Dest 1", AccountType.Savings);
        var dest2 = Account.Create("Dest 2", AccountType.CreditCard);
        await accountRepo.AddAsync(source);
        await accountRepo.AddAsync(dest1);
        await accountRepo.AddAsync(dest2);
        await context.SaveChangesAsync();

        var transferOut1 = RecurringTransfer.Create(
            source.Id,
            dest1.Id,
            "Outgoing 1",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));

        var transferOut2 = RecurringTransfer.Create(
            source.Id,
            dest2.Id,
            "Outgoing 2",
            MoneyValue.Create("USD", 200m),
            RecurrencePattern.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));

        var transferIn = RecurringTransfer.Create(
            dest1.Id,
            source.Id,
            "Incoming",
            MoneyValue.Create("USD", 50m),
            RecurrencePattern.CreateWeekly(1, DayOfWeek.Monday),
            new DateOnly(2026, 1, 6));

        await repository.AddAsync(transferOut1);
        await repository.AddAsync(transferOut2);
        await repository.AddAsync(transferIn);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringTransferRepository(verifyContext, FakeUserContext.CreateDefault());
        var sourceTransfers = await verifyRepo.GetBySourceAccountIdAsync(source.Id);

        // Assert
        Assert.Equal(2, sourceTransfers.Count);
        Assert.Contains(sourceTransfers, t => t.Description == "Outgoing 1");
        Assert.Contains(sourceTransfers, t => t.Description == "Outgoing 2");
        Assert.DoesNotContain(sourceTransfers, t => t.Description == "Incoming");
    }

    [Fact]
    public async Task GetByDestinationAccountIdAsync_Returns_Only_Destination_Transfers()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var repository = new RecurringTransferRepository(context, FakeUserContext.CreateDefault());

        var dest = Account.Create("Dest Only", AccountType.Savings);
        var src1 = Account.Create("Src 1", AccountType.Checking);
        var src2 = Account.Create("Src 2", AccountType.CreditCard);
        await accountRepo.AddAsync(dest);
        await accountRepo.AddAsync(src1);
        await accountRepo.AddAsync(src2);
        await context.SaveChangesAsync();

        var transferIn1 = RecurringTransfer.Create(
            src1.Id,
            dest.Id,
            "Incoming 1",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));

        var transferIn2 = RecurringTransfer.Create(
            src2.Id,
            dest.Id,
            "Incoming 2",
            MoneyValue.Create("USD", 200m),
            RecurrencePattern.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));

        var transferOut = RecurringTransfer.Create(
            dest.Id,
            src1.Id,
            "Outgoing",
            MoneyValue.Create("USD", 50m),
            RecurrencePattern.CreateWeekly(1, DayOfWeek.Friday),
            new DateOnly(2026, 1, 10));

        await repository.AddAsync(transferIn1);
        await repository.AddAsync(transferIn2);
        await repository.AddAsync(transferOut);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringTransferRepository(verifyContext, FakeUserContext.CreateDefault());
        var destTransfers = await verifyRepo.GetByDestinationAccountIdAsync(dest.Id);

        // Assert
        Assert.Equal(2, destTransfers.Count);
        Assert.Contains(destTransfers, t => t.Description == "Incoming 1");
        Assert.Contains(destTransfers, t => t.Description == "Incoming 2");
        Assert.DoesNotContain(destTransfers, t => t.Description == "Outgoing");
    }

    [Fact]
    public async Task GetActiveAsync_Returns_Only_Active_Transfers()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var repository = new RecurringTransferRepository(context, FakeUserContext.CreateDefault());

        var source = Account.Create("Active Source", AccountType.Checking);
        var dest = Account.Create("Active Dest", AccountType.Savings);
        await accountRepo.AddAsync(source);
        await accountRepo.AddAsync(dest);
        await context.SaveChangesAsync();

        var activeTransfer = RecurringTransfer.Create(
            source.Id,
            dest.Id,
            "Active Transfer",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));

        var pausedTransfer = RecurringTransfer.Create(
            source.Id,
            dest.Id,
            "Paused Transfer",
            MoneyValue.Create("USD", 200m),
            RecurrencePattern.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));
        pausedTransfer.Pause();

        await repository.AddAsync(activeTransfer);
        await repository.AddAsync(pausedTransfer);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringTransferRepository(verifyContext, FakeUserContext.CreateDefault());
        var activeTransfers = await verifyRepo.GetActiveAsync();

        // Assert
        Assert.Single(activeTransfers);
        Assert.Equal("Active Transfer", activeTransfers[0].Description);
    }

    [Fact]
    public async Task GetAllAsync_Returns_All_Transfers_Ordered_By_Description()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var repository = new RecurringTransferRepository(context, FakeUserContext.CreateDefault());

        var source = Account.Create("All Source", AccountType.Checking);
        var dest = Account.Create("All Dest", AccountType.Savings);
        await accountRepo.AddAsync(source);
        await accountRepo.AddAsync(dest);
        await context.SaveChangesAsync();

        var transferB = RecurringTransfer.Create(
            source.Id,
            dest.Id,
            "B Transfer",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));

        var transferA = RecurringTransfer.Create(
            source.Id,
            dest.Id,
            "A Transfer",
            MoneyValue.Create("USD", 200m),
            RecurrencePattern.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));

        var transferC = RecurringTransfer.Create(
            source.Id,
            dest.Id,
            "C Transfer",
            MoneyValue.Create("USD", 300m),
            RecurrencePattern.CreateWeekly(1, DayOfWeek.Monday),
            new DateOnly(2026, 1, 6));

        await repository.AddAsync(transferB);
        await repository.AddAsync(transferA);
        await repository.AddAsync(transferC);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringTransferRepository(verifyContext, FakeUserContext.CreateDefault());
        var allTransfers = await verifyRepo.GetAllAsync();

        // Assert
        Assert.Equal(3, allTransfers.Count);
        Assert.Equal("A Transfer", allTransfers[0].Description);
        Assert.Equal("B Transfer", allTransfers[1].Description);
        Assert.Equal("C Transfer", allTransfers[2].Description);
    }

    [Fact]
    public async Task ListAsync_Returns_Paginated_Results()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var repository = new RecurringTransferRepository(context, FakeUserContext.CreateDefault());

        var source = Account.Create("List Source", AccountType.Checking);
        var dest = Account.Create("List Dest", AccountType.Savings);
        await accountRepo.AddAsync(source);
        await accountRepo.AddAsync(dest);
        await context.SaveChangesAsync();

        for (int i = 1; i <= 5; i++)
        {
            var transfer = RecurringTransfer.Create(
                source.Id,
                dest.Id,
                $"Transfer {i:D2}",
                MoneyValue.Create("USD", i * 100m),
                RecurrencePattern.CreateMonthly(1, i),
                new DateOnly(2026, 1, i));
            await repository.AddAsync(transfer);
        }

        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringTransferRepository(verifyContext, FakeUserContext.CreateDefault());
        var page1 = await verifyRepo.ListAsync(0, 2);
        var page2 = await verifyRepo.ListAsync(2, 2);
        var page3 = await verifyRepo.ListAsync(4, 2);

        // Assert
        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
        Assert.Single(page3);
    }

    [Fact]
    public async Task CountAsync_Returns_Correct_Count()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var repository = new RecurringTransferRepository(context, FakeUserContext.CreateDefault());

        var initialCount = await repository.CountAsync();
        Assert.Equal(0, initialCount);

        var source = Account.Create("Count Source", AccountType.Checking);
        var dest = Account.Create("Count Dest", AccountType.Savings);
        await accountRepo.AddAsync(source);
        await accountRepo.AddAsync(dest);
        await context.SaveChangesAsync();

        var transfer1 = RecurringTransfer.Create(
            source.Id,
            dest.Id,
            "Count Transfer 1",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));

        var transfer2 = RecurringTransfer.Create(
            source.Id,
            dest.Id,
            "Count Transfer 2",
            MoneyValue.Create("USD", 200m),
            RecurrencePattern.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));

        await repository.AddAsync(transfer1);
        await repository.AddAsync(transfer2);
        await context.SaveChangesAsync();

        // Act
        var newCount = await repository.CountAsync();

        // Assert
        Assert.Equal(2, newCount);
    }

    [Fact]
    public async Task RemoveAsync_Deletes_RecurringTransfer()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var repository = new RecurringTransferRepository(context, FakeUserContext.CreateDefault());

        var source = Account.Create("Remove Source", AccountType.Checking);
        var dest = Account.Create("Remove Dest", AccountType.Savings);
        await accountRepo.AddAsync(source);
        await accountRepo.AddAsync(dest);
        await context.SaveChangesAsync();

        var transfer = RecurringTransfer.Create(
            source.Id,
            dest.Id,
            "To Remove",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));

        await repository.AddAsync(transfer);
        await context.SaveChangesAsync();

        // Act
        await repository.RemoveAsync(transfer);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringTransferRepository(verifyContext, FakeUserContext.CreateDefault());
        var retrieved = await verifyRepo.GetByIdAsync(transfer.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task AddExceptionAsync_And_GetExceptionAsync_Persists_Exception()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var repository = new RecurringTransferRepository(context, FakeUserContext.CreateDefault());

        var source = Account.Create("Exception Source", AccountType.Checking);
        var dest = Account.Create("Exception Dest", AccountType.Savings);
        await accountRepo.AddAsync(source);
        await accountRepo.AddAsync(dest);
        await context.SaveChangesAsync();

        var transfer = RecurringTransfer.Create(
            source.Id,
            dest.Id,
            "Exception Test",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));
        await repository.AddAsync(transfer);
        await context.SaveChangesAsync();

        var exception = RecurringTransferException.CreateModified(
            transfer.Id,
            new DateOnly(2026, 2, 15),
            MoneyValue.Create("USD", 150m),
            "Extra this month",
            null);

        // Act
        await repository.AddExceptionAsync(exception);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringTransferRepository(verifyContext, FakeUserContext.CreateDefault());
        var retrieved = await verifyRepo.GetExceptionAsync(transfer.Id, new DateOnly(2026, 2, 15));

        Assert.NotNull(retrieved);
        Assert.Equal(exception.Id, retrieved.Id);
        Assert.Equal(ExceptionType.Modified, retrieved.ExceptionType);
        Assert.Equal(150m, retrieved.ModifiedAmount!.Amount);
        Assert.Equal("Extra this month", retrieved.ModifiedDescription);
    }

    [Fact]
    public async Task GetExceptionsByDateRangeAsync_Returns_Exceptions_In_Range()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var repository = new RecurringTransferRepository(context, FakeUserContext.CreateDefault());

        var source = Account.Create("Range Source", AccountType.Checking);
        var dest = Account.Create("Range Dest", AccountType.Savings);
        await accountRepo.AddAsync(source);
        await accountRepo.AddAsync(dest);
        await context.SaveChangesAsync();

        var transfer = RecurringTransfer.Create(
            source.Id,
            dest.Id,
            "Range Test",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));
        await repository.AddAsync(transfer);
        await context.SaveChangesAsync();

        var jan = RecurringTransferException.CreateSkipped(transfer.Id, new DateOnly(2026, 1, 15));
        var feb = RecurringTransferException.CreateModified(transfer.Id, new DateOnly(2026, 2, 15), MoneyValue.Create("USD", 200m), null, null);
        var mar = RecurringTransferException.CreateSkipped(transfer.Id, new DateOnly(2026, 3, 15));
        var apr = RecurringTransferException.CreateModified(transfer.Id, new DateOnly(2026, 4, 15), MoneyValue.Create("USD", 300m), null, null);

        await repository.AddExceptionAsync(jan);
        await repository.AddExceptionAsync(feb);
        await repository.AddExceptionAsync(mar);
        await repository.AddExceptionAsync(apr);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringTransferRepository(verifyContext, FakeUserContext.CreateDefault());
        var exceptions = await verifyRepo.GetExceptionsByDateRangeAsync(
            transfer.Id,
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 3, 31));

        // Assert
        Assert.Equal(2, exceptions.Count);
        Assert.Contains(exceptions, e => e.OriginalDate == new DateOnly(2026, 2, 15));
        Assert.Contains(exceptions, e => e.OriginalDate == new DateOnly(2026, 3, 15));
    }

    [Fact]
    public async Task RemoveExceptionAsync_Deletes_Exception()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var repository = new RecurringTransferRepository(context, FakeUserContext.CreateDefault());

        var source = Account.Create("RemoveEx Source", AccountType.Checking);
        var dest = Account.Create("RemoveEx Dest", AccountType.Savings);
        await accountRepo.AddAsync(source);
        await accountRepo.AddAsync(dest);
        await context.SaveChangesAsync();

        var transfer = RecurringTransfer.Create(
            source.Id,
            dest.Id,
            "RemoveEx Test",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));
        await repository.AddAsync(transfer);
        await context.SaveChangesAsync();

        var exception = RecurringTransferException.CreateSkipped(transfer.Id, new DateOnly(2026, 2, 15));
        await repository.AddExceptionAsync(exception);
        await context.SaveChangesAsync();

        // Act
        await repository.RemoveExceptionAsync(exception);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringTransferRepository(verifyContext, FakeUserContext.CreateDefault());
        var retrieved = await verifyRepo.GetExceptionAsync(transfer.Id, new DateOnly(2026, 2, 15));
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task RemoveExceptionsFromDateAsync_Removes_Future_Exceptions()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var repository = new RecurringTransferRepository(context, FakeUserContext.CreateDefault());

        var source = Account.Create("Future Source", AccountType.Checking);
        var dest = Account.Create("Future Dest", AccountType.Savings);
        await accountRepo.AddAsync(source);
        await accountRepo.AddAsync(dest);
        await context.SaveChangesAsync();

        var transfer = RecurringTransfer.Create(
            source.Id,
            dest.Id,
            "Future Test",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));
        await repository.AddAsync(transfer);
        await context.SaveChangesAsync();

        var jan = RecurringTransferException.CreateSkipped(transfer.Id, new DateOnly(2026, 1, 15));
        var feb = RecurringTransferException.CreateSkipped(transfer.Id, new DateOnly(2026, 2, 15));
        var mar = RecurringTransferException.CreateSkipped(transfer.Id, new DateOnly(2026, 3, 15));
        var apr = RecurringTransferException.CreateSkipped(transfer.Id, new DateOnly(2026, 4, 15));

        await repository.AddExceptionAsync(jan);
        await repository.AddExceptionAsync(feb);
        await repository.AddExceptionAsync(mar);
        await repository.AddExceptionAsync(apr);
        await context.SaveChangesAsync();

        // Act - Remove March and future exceptions
        await repository.RemoveExceptionsFromDateAsync(transfer.Id, new DateOnly(2026, 3, 1));
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringTransferRepository(verifyContext, FakeUserContext.CreateDefault());

        var janEx = await verifyRepo.GetExceptionAsync(transfer.Id, new DateOnly(2026, 1, 15));
        var febEx = await verifyRepo.GetExceptionAsync(transfer.Id, new DateOnly(2026, 2, 15));
        var marEx = await verifyRepo.GetExceptionAsync(transfer.Id, new DateOnly(2026, 3, 15));
        var aprEx = await verifyRepo.GetExceptionAsync(transfer.Id, new DateOnly(2026, 4, 15));

        Assert.NotNull(janEx);
        Assert.NotNull(febEx);
        Assert.Null(marEx);
        Assert.Null(aprEx);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_For_NonExistent_Id()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new RecurringTransferRepository(context, FakeUserContext.CreateDefault());

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetExceptionAsync_Returns_Null_For_NonExistent_Exception()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var repository = new RecurringTransferRepository(context, FakeUserContext.CreateDefault());

        var source = Account.Create("NonExistent Source", AccountType.Checking);
        var dest = Account.Create("NonExistent Dest", AccountType.Savings);
        await accountRepo.AddAsync(source);
        await accountRepo.AddAsync(dest);
        await context.SaveChangesAsync();

        var transfer = RecurringTransfer.Create(
            source.Id,
            dest.Id,
            "NonExistent Test",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));
        await repository.AddAsync(transfer);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetExceptionAsync(transfer.Id, new DateOnly(2026, 5, 15));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Deleting_Account_Cascades_To_RecurringTransfer()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var repository = new RecurringTransferRepository(context, FakeUserContext.CreateDefault());

        var source = Account.Create("Cascade Source", AccountType.Checking);
        var dest = Account.Create("Cascade Dest", AccountType.Savings);
        await accountRepo.AddAsync(source);
        await accountRepo.AddAsync(dest);
        await context.SaveChangesAsync();

        var transfer = RecurringTransfer.Create(
            source.Id,
            dest.Id,
            "Cascade Test",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));
        await repository.AddAsync(transfer);
        await context.SaveChangesAsync();

        // Act - Delete the source account
        await accountRepo.RemoveAsync(source);
        await context.SaveChangesAsync();

        // Assert - Transfer should be cascade deleted
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringTransferRepository(verifyContext, FakeUserContext.CreateDefault());
        var retrieved = await verifyRepo.GetByIdAsync(transfer.Id);
        Assert.Null(retrieved);
    }
}
