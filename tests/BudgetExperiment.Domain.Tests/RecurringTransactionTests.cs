// <copyright file="RecurringTransactionTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Tests for <see cref="RecurringTransaction"/> entity.
/// </summary>
public class RecurringTransactionTests
{
    private readonly Guid _validAccountId = Guid.NewGuid();
    private readonly MoneyValue _validAmount = MoneyValue.Create("USD", -100m);
    private readonly RecurrencePattern _validPattern = RecurrencePattern.CreateMonthly(1, 15);
    private readonly DateOnly _validStartDate = new(2026, 1, 15);

    [Fact]
    public void Create_With_Valid_Parameters_Creates_RecurringTransaction()
    {
        var result = RecurringTransaction.Create(
            this._validAccountId,
            "Monthly Rent",
            this._validAmount,
            this._validPattern,
            this._validStartDate);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(this._validAccountId, result.AccountId);
        Assert.Equal("Monthly Rent", result.Description);
        Assert.Equal(this._validAmount, result.Amount);
        Assert.Equal(this._validPattern, result.RecurrencePattern);
        Assert.Equal(this._validStartDate, result.StartDate);
        Assert.Null(result.EndDate);
        Assert.Equal(this._validStartDate, result.NextOccurrence);
        Assert.True(result.IsActive);
        Assert.Null(result.LastGeneratedDate);
    }

    [Fact]
    public void Create_With_EndDate_Sets_EndDate()
    {
        var endDate = new DateOnly(2027, 1, 15);

        var result = RecurringTransaction.Create(
            this._validAccountId,
            "Monthly Rent",
            this._validAmount,
            this._validPattern,
            this._validStartDate,
            endDate);

        Assert.Equal(endDate, result.EndDate);
    }

    [Fact]
    public void Create_With_Empty_AccountId_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransaction.Create(
                Guid.Empty,
                "Monthly Rent",
                this._validAmount,
                this._validPattern,
                this._validStartDate));

        Assert.Contains("Account ID is required", ex.Message);
    }

    [Fact]
    public void Create_With_Null_Description_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransaction.Create(
                this._validAccountId,
                null!,
                this._validAmount,
                this._validPattern,
                this._validStartDate));

        Assert.Contains("Description is required", ex.Message);
    }

    [Fact]
    public void Create_With_Empty_Description_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransaction.Create(
                this._validAccountId,
                "   ",
                this._validAmount,
                this._validPattern,
                this._validStartDate));

        Assert.Contains("Description is required", ex.Message);
    }

    [Fact]
    public void Create_With_Null_Amount_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransaction.Create(
                this._validAccountId,
                "Monthly Rent",
                null!,
                this._validPattern,
                this._validStartDate));

        Assert.Contains("Amount is required", ex.Message);
    }

    [Fact]
    public void Create_With_Null_Pattern_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransaction.Create(
                this._validAccountId,
                "Monthly Rent",
                this._validAmount,
                null!,
                this._validStartDate));

        Assert.Contains("Recurrence pattern is required", ex.Message);
    }

    [Fact]
    public void Create_With_EndDate_Before_StartDate_Throws()
    {
        var endDate = new DateOnly(2025, 1, 15); // Before start date

        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransaction.Create(
                this._validAccountId,
                "Monthly Rent",
                this._validAmount,
                this._validPattern,
                this._validStartDate,
                endDate));

        Assert.Contains("End date must be on or after start date", ex.Message);
    }

    [Fact]
    public void Create_Trims_Description()
    {
        var result = RecurringTransaction.Create(
            this._validAccountId,
            "  Monthly Rent  ",
            this._validAmount,
            this._validPattern,
            this._validStartDate);

        Assert.Equal("Monthly Rent", result.Description);
    }

    [Fact]
    public void Pause_Sets_IsActive_To_False()
    {
        var recurring = RecurringTransaction.Create(
            this._validAccountId,
            "Monthly Rent",
            this._validAmount,
            this._validPattern,
            this._validStartDate);

        recurring.Pause();

        Assert.False(recurring.IsActive);
    }

    [Fact]
    public void Resume_Sets_IsActive_To_True()
    {
        var recurring = RecurringTransaction.Create(
            this._validAccountId,
            "Monthly Rent",
            this._validAmount,
            this._validPattern,
            this._validStartDate);
        recurring.Pause();

        recurring.Resume();

        Assert.True(recurring.IsActive);
    }

    [Fact]
    public void AdvanceNextOccurrence_Updates_NextOccurrence_To_Pattern_Calculated_Date()
    {
        var recurring = RecurringTransaction.Create(
            this._validAccountId,
            "Monthly Rent",
            this._validAmount,
            this._validPattern,
            this._validStartDate);

        recurring.AdvanceNextOccurrence();

        Assert.Equal(new DateOnly(2026, 2, 15), recurring.NextOccurrence);
    }

    [Fact]
    public void AdvanceNextOccurrence_Updates_LastGeneratedDate()
    {
        var recurring = RecurringTransaction.Create(
            this._validAccountId,
            "Monthly Rent",
            this._validAmount,
            this._validPattern,
            this._validStartDate);
        var originalNextOccurrence = recurring.NextOccurrence;

        recurring.AdvanceNextOccurrence();

        Assert.Equal(originalNextOccurrence, recurring.LastGeneratedDate);
    }

    [Fact]
    public void AdvanceNextOccurrence_When_Past_EndDate_Sets_IsActive_To_False()
    {
        var endDate = new DateOnly(2026, 2, 1); // Before the next occurrence after Jan 15
        var recurring = RecurringTransaction.Create(
            this._validAccountId,
            "Monthly Rent",
            this._validAmount,
            this._validPattern,
            this._validStartDate,
            endDate);

        recurring.AdvanceNextOccurrence();

        Assert.False(recurring.IsActive);
    }

    [Fact]
    public void Update_With_Valid_Parameters_Updates_Properties()
    {
        var recurring = RecurringTransaction.Create(
            this._validAccountId,
            "Monthly Rent",
            this._validAmount,
            this._validPattern,
            this._validStartDate);

        var newAmount = MoneyValue.Create("USD", -1500m);
        var newPattern = RecurrencePattern.CreateMonthly(1, 1);
        var newEndDate = new DateOnly(2027, 12, 31);

        recurring.Update(
            "New Monthly Rent",
            newAmount,
            newPattern,
            newEndDate,
            null);

        Assert.Equal("New Monthly Rent", recurring.Description);
        Assert.Equal(newAmount, recurring.Amount);
        Assert.Equal(newPattern, recurring.RecurrencePattern);
        Assert.Equal(newEndDate, recurring.EndDate);
    }

    [Fact]
    public void Update_With_Empty_Description_Throws()
    {
        var recurring = RecurringTransaction.Create(
            this._validAccountId,
            "Monthly Rent",
            this._validAmount,
            this._validPattern,
            this._validStartDate);

        var ex = Assert.Throws<DomainException>(() =>
            recurring.Update(
                "",
                this._validAmount,
                this._validPattern,
                null,
                null));

        Assert.Contains("Description is required", ex.Message);
    }

    [Fact]
    public void Update_With_EndDate_Before_StartDate_Throws()
    {
        var recurring = RecurringTransaction.Create(
            this._validAccountId,
            "Monthly Rent",
            this._validAmount,
            this._validPattern,
            this._validStartDate);

        var invalidEndDate = new DateOnly(2025, 1, 1);

        var ex = Assert.Throws<DomainException>(() =>
            recurring.Update(
                "Monthly Rent",
                this._validAmount,
                this._validPattern,
                invalidEndDate,
                null));

        Assert.Contains("End date must be on or after start date", ex.Message);
    }

    [Fact]
    public void GetOccurrencesBetween_Returns_All_Occurrences_In_Range()
    {
        var recurring = RecurringTransaction.Create(
            this._validAccountId,
            "Monthly Rent",
            this._validAmount,
            this._validPattern,
            this._validStartDate);

        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 4, 30);

        var occurrences = recurring.GetOccurrencesBetween(from, to).ToList();

        Assert.Equal(4, occurrences.Count);
        Assert.Equal(new DateOnly(2026, 1, 15), occurrences[0]);
        Assert.Equal(new DateOnly(2026, 2, 15), occurrences[1]);
        Assert.Equal(new DateOnly(2026, 3, 15), occurrences[2]);
        Assert.Equal(new DateOnly(2026, 4, 15), occurrences[3]);
    }

    [Fact]
    public void GetOccurrencesBetween_Respects_EndDate()
    {
        var endDate = new DateOnly(2026, 3, 1);
        var recurring = RecurringTransaction.Create(
            this._validAccountId,
            "Monthly Rent",
            this._validAmount,
            this._validPattern,
            this._validStartDate,
            endDate);

        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 4, 30);

        var occurrences = recurring.GetOccurrencesBetween(from, to).ToList();

        Assert.Equal(2, occurrences.Count);
        Assert.Equal(new DateOnly(2026, 1, 15), occurrences[0]);
        Assert.Equal(new DateOnly(2026, 2, 15), occurrences[1]);
    }

    [Fact]
    public void GetOccurrencesBetween_Returns_Empty_When_StartDate_After_Range()
    {
        var recurring = RecurringTransaction.Create(
            this._validAccountId,
            "Monthly Rent",
            this._validAmount,
            this._validPattern,
            new DateOnly(2027, 1, 15));

        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 12, 31);

        var occurrences = recurring.GetOccurrencesBetween(from, to).ToList();

        Assert.Empty(occurrences);
    }

    [Fact]
    public void GetOccurrencesBetween_Returns_Empty_When_Not_Active()
    {
        var recurring = RecurringTransaction.Create(
            this._validAccountId,
            "Monthly Rent",
            this._validAmount,
            this._validPattern,
            this._validStartDate);
        recurring.Pause();

        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 4, 30);

        var occurrences = recurring.GetOccurrencesBetween(from, to).ToList();

        Assert.Empty(occurrences);
    }

    [Fact]
    public void CreatedAtUtc_And_UpdatedAtUtc_Are_Set_On_Create()
    {
        var beforeCreate = DateTime.UtcNow;

        var recurring = RecurringTransaction.Create(
            this._validAccountId,
            "Monthly Rent",
            this._validAmount,
            this._validPattern,
            this._validStartDate);

        var afterCreate = DateTime.UtcNow;

        Assert.InRange(recurring.CreatedAtUtc, beforeCreate, afterCreate);
        Assert.InRange(recurring.UpdatedAtUtc, beforeCreate, afterCreate);
    }

    [Fact]
    public void Update_Updates_UpdatedAtUtc()
    {
        var recurring = RecurringTransaction.Create(
            this._validAccountId,
            "Monthly Rent",
            this._validAmount,
            this._validPattern,
            this._validStartDate);

        var originalUpdatedAt = recurring.UpdatedAtUtc;

        // Small delay to ensure timestamp changes
        System.Threading.Thread.Sleep(10);

        recurring.Update(
            "Updated Rent",
            this._validAmount,
            this._validPattern,
            null,
            null);

        Assert.True(recurring.UpdatedAtUtc > originalUpdatedAt);
    }
}
