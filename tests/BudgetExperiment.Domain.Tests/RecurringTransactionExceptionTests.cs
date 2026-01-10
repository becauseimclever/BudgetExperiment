// <copyright file="RecurringTransactionExceptionTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Tests for <see cref="RecurringTransactionException"/> entity.
/// </summary>
public class RecurringTransactionExceptionTests
{
    private readonly Guid _validRecurringTransactionId = Guid.NewGuid();
    private readonly DateOnly _validOriginalDate = new(2026, 1, 15);

    [Fact]
    public void CreateModified_With_Valid_Parameters_Creates_Exception()
    {
        var modifiedAmount = MoneyValue.Create("USD", -150m);
        var modifiedDescription = "Updated description";
        var modifiedDate = new DateOnly(2026, 1, 16);

        var result = RecurringTransactionException.CreateModified(
            this._validRecurringTransactionId,
            this._validOriginalDate,
            modifiedAmount,
            modifiedDescription,
            modifiedDate);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(this._validRecurringTransactionId, result.RecurringTransactionId);
        Assert.Equal(this._validOriginalDate, result.OriginalDate);
        Assert.Equal(ExceptionType.Modified, result.ExceptionType);
        Assert.Equal(modifiedAmount, result.ModifiedAmount);
        Assert.Equal("Updated description", result.ModifiedDescription);
        Assert.Equal(modifiedDate, result.ModifiedDate);
    }

    [Fact]
    public void CreateModified_With_Only_Amount_Creates_Exception()
    {
        var modifiedAmount = MoneyValue.Create("USD", -150m);

        var result = RecurringTransactionException.CreateModified(
            this._validRecurringTransactionId,
            this._validOriginalDate,
            modifiedAmount,
            null,
            null);

        Assert.Equal(ExceptionType.Modified, result.ExceptionType);
        Assert.Equal(modifiedAmount, result.ModifiedAmount);
        Assert.Null(result.ModifiedDescription);
        Assert.Null(result.ModifiedDate);
    }

    [Fact]
    public void CreateModified_With_Only_Description_Creates_Exception()
    {
        var result = RecurringTransactionException.CreateModified(
            this._validRecurringTransactionId,
            this._validOriginalDate,
            null,
            "New description",
            null);

        Assert.Equal(ExceptionType.Modified, result.ExceptionType);
        Assert.Null(result.ModifiedAmount);
        Assert.Equal("New description", result.ModifiedDescription);
        Assert.Null(result.ModifiedDate);
    }

    [Fact]
    public void CreateModified_With_Only_Date_Creates_Exception()
    {
        var modifiedDate = new DateOnly(2026, 1, 20);

        var result = RecurringTransactionException.CreateModified(
            this._validRecurringTransactionId,
            this._validOriginalDate,
            null,
            null,
            modifiedDate);

        Assert.Equal(ExceptionType.Modified, result.ExceptionType);
        Assert.Null(result.ModifiedAmount);
        Assert.Null(result.ModifiedDescription);
        Assert.Equal(modifiedDate, result.ModifiedDate);
    }

    [Fact]
    public void CreateModified_With_Empty_RecurringTransactionId_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransactionException.CreateModified(
                Guid.Empty,
                this._validOriginalDate,
                MoneyValue.Create("USD", -100m),
                null,
                null));

        Assert.Contains("Recurring transaction ID is required", ex.Message);
    }

    [Fact]
    public void CreateModified_With_No_Modifications_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransactionException.CreateModified(
                this._validRecurringTransactionId,
                this._validOriginalDate,
                null,
                null,
                null));

        Assert.Contains("At least one modification is required", ex.Message);
    }

    [Fact]
    public void CreateModified_Trims_Description()
    {
        var result = RecurringTransactionException.CreateModified(
            this._validRecurringTransactionId,
            this._validOriginalDate,
            null,
            "  Trimmed  ",
            null);

        Assert.Equal("Trimmed", result.ModifiedDescription);
    }

    [Fact]
    public void CreateSkipped_Creates_Skipped_Exception()
    {
        var result = RecurringTransactionException.CreateSkipped(
            this._validRecurringTransactionId,
            this._validOriginalDate);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(this._validRecurringTransactionId, result.RecurringTransactionId);
        Assert.Equal(this._validOriginalDate, result.OriginalDate);
        Assert.Equal(ExceptionType.Skipped, result.ExceptionType);
        Assert.Null(result.ModifiedAmount);
        Assert.Null(result.ModifiedDescription);
        Assert.Null(result.ModifiedDate);
    }

    [Fact]
    public void CreateSkipped_With_Empty_RecurringTransactionId_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransactionException.CreateSkipped(
                Guid.Empty,
                this._validOriginalDate));

        Assert.Contains("Recurring transaction ID is required", ex.Message);
    }

    [Fact]
    public void Update_Modifies_Exception_Values()
    {
        var exception = RecurringTransactionException.CreateModified(
            this._validRecurringTransactionId,
            this._validOriginalDate,
            MoneyValue.Create("USD", -100m),
            null,
            null);

        var newAmount = MoneyValue.Create("USD", -200m);
        var newDescription = "New description";
        var newDate = new DateOnly(2026, 1, 20);

        exception.Update(newAmount, newDescription, newDate);

        Assert.Equal(newAmount, exception.ModifiedAmount);
        Assert.Equal("New description", exception.ModifiedDescription);
        Assert.Equal(newDate, exception.ModifiedDate);
    }

    [Fact]
    public void Update_With_No_Modifications_Throws()
    {
        var exception = RecurringTransactionException.CreateModified(
            this._validRecurringTransactionId,
            this._validOriginalDate,
            MoneyValue.Create("USD", -100m),
            null,
            null);

        var ex = Assert.Throws<DomainException>(() =>
            exception.Update(null, null, null));

        Assert.Contains("At least one modification is required", ex.Message);
    }

    [Fact]
    public void CreatedAtUtc_And_UpdatedAtUtc_Are_Set_On_Create()
    {
        var beforeCreate = DateTime.UtcNow;

        var exception = RecurringTransactionException.CreateModified(
            this._validRecurringTransactionId,
            this._validOriginalDate,
            MoneyValue.Create("USD", -100m),
            null,
            null);

        var afterCreate = DateTime.UtcNow;

        Assert.InRange(exception.CreatedAtUtc, beforeCreate, afterCreate);
        Assert.InRange(exception.UpdatedAtUtc, beforeCreate, afterCreate);
    }

    [Fact]
    public void Update_Updates_UpdatedAtUtc()
    {
        var exception = RecurringTransactionException.CreateModified(
            this._validRecurringTransactionId,
            this._validOriginalDate,
            MoneyValue.Create("USD", -100m),
            null,
            null);

        var originalUpdatedAt = exception.UpdatedAtUtc;

        // Small delay to ensure timestamp changes
        System.Threading.Thread.Sleep(10);

        exception.Update(MoneyValue.Create("USD", -200m), null, null);

        Assert.True(exception.UpdatedAtUtc > originalUpdatedAt);
    }

    [Fact]
    public void GetEffectiveDate_Returns_ModifiedDate_When_Set()
    {
        var modifiedDate = new DateOnly(2026, 1, 20);
        var exception = RecurringTransactionException.CreateModified(
            this._validRecurringTransactionId,
            this._validOriginalDate,
            null,
            null,
            modifiedDate);

        Assert.Equal(modifiedDate, exception.GetEffectiveDate());
    }

    [Fact]
    public void GetEffectiveDate_Returns_OriginalDate_When_ModifiedDate_Is_Null()
    {
        var exception = RecurringTransactionException.CreateModified(
            this._validRecurringTransactionId,
            this._validOriginalDate,
            MoneyValue.Create("USD", -100m),
            null,
            null);

        Assert.Equal(this._validOriginalDate, exception.GetEffectiveDate());
    }
}
