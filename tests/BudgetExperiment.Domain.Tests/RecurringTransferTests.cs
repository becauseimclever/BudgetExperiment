// <copyright file="RecurringTransferTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Tests for <see cref="RecurringTransfer"/> entity.
/// </summary>
public class RecurringTransferTests
{
    private readonly Guid _validSourceAccountId = Guid.NewGuid();
    private readonly Guid _validDestinationAccountId = Guid.NewGuid();
    private readonly MoneyValue _validAmount = MoneyValue.Create("USD", 500m);
    private readonly RecurrencePatternValue _validPattern = RecurrencePatternValue.CreateMonthly(1, 1);
    private readonly DateOnly _validStartDate = new(2026, 2, 1);

    [Fact]
    public void Create_With_Valid_Parameters_Creates_RecurringTransfer()
    {
        var result = RecurringTransfer.Create(
            _validSourceAccountId,
            _validDestinationAccountId,
            "Monthly Savings",
            _validAmount,
            _validPattern,
            _validStartDate);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(_validSourceAccountId, result.SourceAccountId);
        Assert.Equal(_validDestinationAccountId, result.DestinationAccountId);
        Assert.Equal("Monthly Savings", result.Description);
        Assert.Equal(_validAmount, result.Amount);
        Assert.Equal(_validPattern, result.RecurrencePatternValue);
        Assert.Equal(_validStartDate, result.StartDate);
        Assert.Null(result.EndDate);
        Assert.Equal(_validStartDate, result.NextOccurrence);
        Assert.True(result.IsActive);
        Assert.Null(result.LastGeneratedDate);
    }

    [Fact]
    public void Create_With_EndDate_Sets_EndDate()
    {
        var endDate = new DateOnly(2027, 2, 1);

        var result = RecurringTransfer.Create(
            _validSourceAccountId,
            _validDestinationAccountId,
            "Monthly Savings",
            _validAmount,
            _validPattern,
            _validStartDate,
            endDate);

        Assert.Equal(endDate, result.EndDate);
    }

    [Fact]
    public void Create_With_Empty_SourceAccountId_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransfer.Create(
                Guid.Empty,
                _validDestinationAccountId,
                "Monthly Savings",
                _validAmount,
                _validPattern,
                _validStartDate));

        Assert.Contains("Source account ID is required", ex.Message);
    }

    [Fact]
    public void Create_With_Empty_DestinationAccountId_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransfer.Create(
                _validSourceAccountId,
                Guid.Empty,
                "Monthly Savings",
                _validAmount,
                _validPattern,
                _validStartDate));

        Assert.Contains("Destination account ID is required", ex.Message);
    }

    [Fact]
    public void Create_With_Same_Source_And_Destination_Throws()
    {
        var sameAccountId = Guid.NewGuid();

        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransfer.Create(
                sameAccountId,
                sameAccountId,
                "Invalid Transfer",
                _validAmount,
                _validPattern,
                _validStartDate));

        Assert.Contains("Source and destination accounts must be different", ex.Message);
    }

    [Fact]
    public void Create_With_Null_Description_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransfer.Create(
                _validSourceAccountId,
                _validDestinationAccountId,
                null!,
                _validAmount,
                _validPattern,
                _validStartDate));

        Assert.Contains("Description is required", ex.Message);
    }

    [Fact]
    public void Create_With_Empty_Description_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransfer.Create(
                _validSourceAccountId,
                _validDestinationAccountId,
                "   ",
                _validAmount,
                _validPattern,
                _validStartDate));

        Assert.Contains("Description is required", ex.Message);
    }

    [Fact]
    public void Create_With_Null_Amount_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransfer.Create(
                _validSourceAccountId,
                _validDestinationAccountId,
                "Monthly Savings",
                null!,
                _validPattern,
                _validStartDate));

        Assert.Contains("Amount is required", ex.Message);
    }

    [Fact]
    public void Create_With_Zero_Amount_Throws()
    {
        var zeroAmount = MoneyValue.Create("USD", 0m);

        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransfer.Create(
                _validSourceAccountId,
                _validDestinationAccountId,
                "Monthly Savings",
                zeroAmount,
                _validPattern,
                _validStartDate));

        Assert.Contains("Transfer amount must be positive", ex.Message);
    }

    [Fact]
    public void Create_With_Negative_Amount_Throws()
    {
        var negativeAmount = MoneyValue.Create("USD", -100m);

        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransfer.Create(
                _validSourceAccountId,
                _validDestinationAccountId,
                "Monthly Savings",
                negativeAmount,
                _validPattern,
                _validStartDate));

        Assert.Contains("Transfer amount must be positive", ex.Message);
    }

    [Fact]
    public void Create_With_Null_Pattern_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransfer.Create(
                _validSourceAccountId,
                _validDestinationAccountId,
                "Monthly Savings",
                _validAmount,
                null!,
                _validStartDate));

        Assert.Contains("Recurrence pattern is required", ex.Message);
    }

    [Fact]
    public void Create_With_EndDate_Before_StartDate_Throws()
    {
        var endDate = new DateOnly(2025, 1, 1);

        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransfer.Create(
                _validSourceAccountId,
                _validDestinationAccountId,
                "Monthly Savings",
                _validAmount,
                _validPattern,
                _validStartDate,
                endDate));

        Assert.Contains("End date must be on or after start date", ex.Message);
    }

    [Fact]
    public void Create_Trims_Description()
    {
        var result = RecurringTransfer.Create(
            _validSourceAccountId,
            _validDestinationAccountId,
            "  Monthly Savings  ",
            _validAmount,
            _validPattern,
            _validStartDate);

        Assert.Equal("Monthly Savings", result.Description);
    }

    [Fact]
    public void Update_With_Valid_Parameters_Updates_Properties()
    {
        var recurringTransfer = RecurringTransfer.Create(
            _validSourceAccountId,
            _validDestinationAccountId,
            "Monthly Savings",
            _validAmount,
            _validPattern,
            _validStartDate);

        var newAmount = MoneyValue.Create("USD", 750m);
        var newPattern = RecurrencePatternValue.CreateMonthly(1, 15);
        var newEndDate = new DateOnly(2028, 12, 31);

        recurringTransfer.Update("Increased Savings", newAmount, newPattern, newEndDate);

        Assert.Equal("Increased Savings", recurringTransfer.Description);
        Assert.Equal(newAmount, recurringTransfer.Amount);
        Assert.Equal(newPattern, recurringTransfer.RecurrencePatternValue);
        Assert.Equal(newEndDate, recurringTransfer.EndDate);
    }

    [Fact]
    public void Update_With_Zero_Amount_Throws()
    {
        var recurringTransfer = RecurringTransfer.Create(
            _validSourceAccountId,
            _validDestinationAccountId,
            "Monthly Savings",
            _validAmount,
            _validPattern,
            _validStartDate);

        var zeroAmount = MoneyValue.Create("USD", 0m);

        var ex = Assert.Throws<DomainException>(() =>
            recurringTransfer.Update("Savings", zeroAmount, _validPattern, null));

        Assert.Contains("Transfer amount must be positive", ex.Message);
    }

    [Fact]
    public void Update_With_Negative_Amount_Throws()
    {
        var recurringTransfer = RecurringTransfer.Create(
            _validSourceAccountId,
            _validDestinationAccountId,
            "Monthly Savings",
            _validAmount,
            _validPattern,
            _validStartDate);

        var negativeAmount = MoneyValue.Create("USD", -100m);

        var ex = Assert.Throws<DomainException>(() =>
            recurringTransfer.Update("Savings", negativeAmount, _validPattern, null));

        Assert.Contains("Transfer amount must be positive", ex.Message);
    }

    [Fact]
    public void Pause_Sets_IsActive_To_False()
    {
        var recurringTransfer = RecurringTransfer.Create(
            _validSourceAccountId,
            _validDestinationAccountId,
            "Monthly Savings",
            _validAmount,
            _validPattern,
            _validStartDate);

        recurringTransfer.Pause();

        Assert.False(recurringTransfer.IsActive);
    }

    [Fact]
    public void Resume_When_Paused_Recalculates_NextOccurrence_From_Today()
    {
        var recurringTransfer = RecurringTransfer.Create(
            _validSourceAccountId,
            _validDestinationAccountId,
            "Monthly Savings",
            _validAmount,
            _validPattern,
            _validStartDate);

        recurringTransfer.Pause();
        var resumeDate = new DateOnly(2026, 3, 15);
        recurringTransfer.Resume(resumeDate);

        Assert.True(recurringTransfer.IsActive);
        Assert.True(recurringTransfer.NextOccurrence >= resumeDate);
    }

    [Fact]
    public void Resume_When_Already_Active_Does_Nothing()
    {
        var recurringTransfer = RecurringTransfer.Create(
            _validSourceAccountId,
            _validDestinationAccountId,
            "Monthly Savings",
            _validAmount,
            _validPattern,
            _validStartDate);

        var originalNextOccurrence = recurringTransfer.NextOccurrence;
        var resumeDate = new DateOnly(2026, 3, 15);

        recurringTransfer.Resume(resumeDate);

        Assert.True(recurringTransfer.IsActive);
        Assert.Equal(originalNextOccurrence, recurringTransfer.NextOccurrence);
    }

    [Fact]
    public void AdvanceToNextOccurrence_Updates_NextOccurrence_And_LastGeneratedDate()
    {
        var recurringTransfer = RecurringTransfer.Create(
            _validSourceAccountId,
            _validDestinationAccountId,
            "Monthly Savings",
            _validAmount,
            _validPattern,
            _validStartDate);

        var originalNext = recurringTransfer.NextOccurrence;

        recurringTransfer.AdvanceToNextOccurrence();

        Assert.Equal(originalNext, recurringTransfer.LastGeneratedDate);
        Assert.True(recurringTransfer.NextOccurrence > originalNext);
    }

    [Fact]
    public void AdvanceToNextOccurrence_When_Inactive_Throws()
    {
        var recurringTransfer = RecurringTransfer.Create(
            _validSourceAccountId,
            _validDestinationAccountId,
            "Monthly Savings",
            _validAmount,
            _validPattern,
            _validStartDate);

        recurringTransfer.Pause();

        var ex = Assert.Throws<DomainException>(() =>
            recurringTransfer.AdvanceToNextOccurrence());

        Assert.Contains("Cannot advance inactive recurring transfer", ex.Message);
    }

    [Fact]
    public void AdvanceToNextOccurrence_Beyond_EndDate_Deactivates()
    {
        var endDate = new DateOnly(2026, 2, 28);
        var recurringTransfer = RecurringTransfer.Create(
            _validSourceAccountId,
            _validDestinationAccountId,
            "Monthly Savings",
            _validAmount,
            _validPattern,
            _validStartDate,
            endDate);

        recurringTransfer.AdvanceToNextOccurrence();

        Assert.False(recurringTransfer.IsActive);
    }

    [Fact]
    public void SkipNextOccurrence_Advances_Without_Setting_LastGeneratedDate()
    {
        var recurringTransfer = RecurringTransfer.Create(
            _validSourceAccountId,
            _validDestinationAccountId,
            "Monthly Savings",
            _validAmount,
            _validPattern,
            _validStartDate);

        var originalNext = recurringTransfer.NextOccurrence;
        var originalLastGenerated = recurringTransfer.LastGeneratedDate;

        recurringTransfer.SkipNextOccurrence();

        Assert.Equal(originalLastGenerated, recurringTransfer.LastGeneratedDate);
        Assert.True(recurringTransfer.NextOccurrence > originalNext);
    }

    [Fact]
    public void SkipNextOccurrence_When_Inactive_Throws()
    {
        var recurringTransfer = RecurringTransfer.Create(
            _validSourceAccountId,
            _validDestinationAccountId,
            "Monthly Savings",
            _validAmount,
            _validPattern,
            _validStartDate);

        recurringTransfer.Pause();

        var ex = Assert.Throws<DomainException>(() =>
            recurringTransfer.SkipNextOccurrence());

        Assert.Contains("Cannot skip occurrence for inactive recurring transfer", ex.Message);
    }
}
