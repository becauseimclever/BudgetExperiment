// <copyright file="RecurringMapperTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain.Common;
using Shouldly;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for <see cref="RecurringMapper"/> exception resolution logic.
/// </summary>
public class RecurringMapperTests
{
    [Fact]
    public void ToInstanceDto_NoException_UsesBaseValues()
    {
        var recurring = CreateRecurring("Netflix", -15.99m, new DateOnly(2026, 1, 15));
        var scheduledDate = new DateOnly(2026, 1, 15);

        var dto = RecurringMapper.ToInstanceDto(recurring, scheduledDate);

        dto.Description.ShouldBe("Netflix");
        dto.Amount.Amount.ShouldBe(-15.99m);
        dto.IsModified.ShouldBeFalse();
        dto.IsSkipped.ShouldBeFalse();
        dto.ScheduledDate.ShouldBe(scheduledDate);
        dto.EffectiveDate.ShouldBe(scheduledDate);
    }

    [Fact]
    public void ToInstanceDto_ModifiedException_UsesModifiedDescriptionAndAmount()
    {
        var recurring = CreateRecurring("Netflix", -15.99m, new DateOnly(2026, 1, 15));
        var scheduledDate = new DateOnly(2026, 1, 15);

        var exception = RecurringTransactionException.CreateModified(
            recurring.Id, scheduledDate, MoneyValue.Create("USD", -22.99m), "Netflix Premium", null);

        var dto = RecurringMapper.ToInstanceDto(recurring, scheduledDate, exception);

        dto.Description.ShouldBe("Netflix Premium");
        dto.Amount.Amount.ShouldBe(-22.99m);
        dto.IsModified.ShouldBeTrue();
        dto.IsSkipped.ShouldBeFalse();
    }

    [Fact]
    public void ToInstanceDto_ModifiedExceptionWithDateOnly_UsesBaseAmountAndDescription()
    {
        var recurring = CreateRecurring("Netflix", -15.99m, new DateOnly(2026, 1, 15));
        var scheduledDate = new DateOnly(2026, 1, 15);
        var newDate = new DateOnly(2026, 1, 20);

        var exception = RecurringTransactionException.CreateModified(
            recurring.Id, scheduledDate, null, null, newDate);

        var dto = RecurringMapper.ToInstanceDto(recurring, scheduledDate, exception);

        dto.Description.ShouldBe("Netflix");
        dto.Amount.Amount.ShouldBe(-15.99m);
        dto.EffectiveDate.ShouldBe(newDate);
        dto.IsModified.ShouldBeTrue();
    }

    [Fact]
    public void ToInstanceDto_SkippedException_SetsIsSkipped()
    {
        var recurring = CreateRecurring("Netflix", -15.99m, new DateOnly(2026, 1, 15));
        var scheduledDate = new DateOnly(2026, 1, 15);

        var exception = RecurringTransactionException.CreateSkipped(recurring.Id, scheduledDate);

        var dto = RecurringMapper.ToInstanceDto(recurring, scheduledDate, exception);

        dto.IsSkipped.ShouldBeTrue();
        dto.IsModified.ShouldBeFalse();
    }

    [Fact]
    public void ToInstanceDto_WithGeneratedTransactionId_SetsIsGenerated()
    {
        var recurring = CreateRecurring("Netflix", -15.99m, new DateOnly(2026, 1, 15));
        var txnId = Guid.NewGuid();

        var dto = RecurringMapper.ToInstanceDto(recurring, new DateOnly(2026, 1, 15), generatedTransactionId: txnId);

        dto.IsGenerated.ShouldBeTrue();
        dto.GeneratedTransactionId.ShouldBe(txnId);
    }

    [Fact]
    public void ToTransferInstanceDto_NoException_UsesBaseValues()
    {
        var transfer = CreateTransfer("Monthly Transfer", 500m, new DateOnly(2026, 1, 1));
        var scheduledDate = new DateOnly(2026, 1, 1);

        var dto = RecurringMapper.ToTransferInstanceDto(
            transfer, scheduledDate, "Checking", "Savings");

        dto.Description.ShouldBe("Monthly Transfer");
        dto.Amount.Amount.ShouldBe(500m);
        dto.SourceAccountName.ShouldBe("Checking");
        dto.DestinationAccountName.ShouldBe("Savings");
        dto.IsModified.ShouldBeFalse();
        dto.IsSkipped.ShouldBeFalse();
    }

    [Fact]
    public void ToTransferInstanceDto_ModifiedException_UsesModifiedValues()
    {
        var transfer = CreateTransfer("Monthly Transfer", 500m, new DateOnly(2026, 1, 1));
        var scheduledDate = new DateOnly(2026, 1, 1);

        var exception = RecurringTransferException.CreateModified(
            transfer.Id, scheduledDate, MoneyValue.Create("USD", 750m), "Extra Transfer", null);

        var dto = RecurringMapper.ToTransferInstanceDto(
            transfer, scheduledDate, "Checking", "Savings", exception);

        dto.Description.ShouldBe("Extra Transfer");
        dto.Amount.Amount.ShouldBe(750m);
        dto.IsModified.ShouldBeTrue();
    }

    [Fact]
    public void ToTransferInstanceDto_SkippedException_SetsIsSkipped()
    {
        var transfer = CreateTransfer("Monthly Transfer", 500m, new DateOnly(2026, 1, 1));
        var scheduledDate = new DateOnly(2026, 1, 1);

        var exception = RecurringTransferException.CreateSkipped(transfer.Id, scheduledDate);

        var dto = RecurringMapper.ToTransferInstanceDto(
            transfer, scheduledDate, "Checking", "Savings", exception);

        dto.IsSkipped.ShouldBeTrue();
        dto.IsModified.ShouldBeFalse();
    }

    private static RecurringTransaction CreateRecurring(string description, decimal amount, DateOnly startDate)
    {
        return RecurringTransaction.Create(
            Guid.NewGuid(),
            description,
            MoneyValue.Create("USD", amount),
            RecurrencePatternValue.CreateMonthly(1, startDate.Day),
            startDate);
    }

    private static RecurringTransfer CreateTransfer(string description, decimal amount, DateOnly startDate)
    {
        return RecurringTransfer.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            description,
            MoneyValue.Create("USD", amount),
            RecurrencePatternValue.CreateMonthly(1, startDate.Day),
            startDate);
    }
}
