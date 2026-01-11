// <copyright file="RecurringTransferExceptionTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Tests for <see cref="RecurringTransferException"/> entity.
/// </summary>
public class RecurringTransferExceptionTests
{
    private readonly Guid _validRecurringTransferId = Guid.NewGuid();
    private readonly DateOnly _validOriginalDate = new(2026, 2, 1);

    [Fact]
    public void CreateModified_With_Valid_Parameters_Creates_Exception()
    {
        var modifiedAmount = MoneyValue.Create("USD", 750m);
        var modifiedDescription = "Updated transfer description";
        var modifiedDate = new DateOnly(2026, 2, 5);

        var result = RecurringTransferException.CreateModified(
            this._validRecurringTransferId,
            this._validOriginalDate,
            modifiedAmount,
            modifiedDescription,
            modifiedDate);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(this._validRecurringTransferId, result.RecurringTransferId);
        Assert.Equal(this._validOriginalDate, result.OriginalDate);
        Assert.Equal(ExceptionType.Modified, result.ExceptionType);
        Assert.Equal(modifiedAmount, result.ModifiedAmount);
        Assert.Equal("Updated transfer description", result.ModifiedDescription);
        Assert.Equal(modifiedDate, result.ModifiedDate);
    }

    [Fact]
    public void CreateModified_With_Only_Amount_Creates_Exception()
    {
        var modifiedAmount = MoneyValue.Create("USD", 750m);

        var result = RecurringTransferException.CreateModified(
            this._validRecurringTransferId,
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
        var result = RecurringTransferException.CreateModified(
            this._validRecurringTransferId,
            this._validOriginalDate,
            null,
            "New transfer description",
            null);

        Assert.Equal(ExceptionType.Modified, result.ExceptionType);
        Assert.Null(result.ModifiedAmount);
        Assert.Equal("New transfer description", result.ModifiedDescription);
        Assert.Null(result.ModifiedDate);
    }

    [Fact]
    public void CreateModified_With_Only_Date_Creates_Exception()
    {
        var modifiedDate = new DateOnly(2026, 2, 10);

        var result = RecurringTransferException.CreateModified(
            this._validRecurringTransferId,
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
    public void CreateModified_With_Empty_RecurringTransferId_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransferException.CreateModified(
                Guid.Empty,
                this._validOriginalDate,
                MoneyValue.Create("USD", 500m),
                null,
                null));

        Assert.Contains("Recurring transfer ID is required", ex.Message);
    }

    [Fact]
    public void CreateModified_With_No_Modifications_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransferException.CreateModified(
                this._validRecurringTransferId,
                this._validOriginalDate,
                null,
                null,
                null));

        Assert.Contains("At least one modification is required", ex.Message);
    }

    [Fact]
    public void CreateModified_With_Zero_Amount_Throws()
    {
        var zeroAmount = MoneyValue.Create("USD", 0m);

        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransferException.CreateModified(
                this._validRecurringTransferId,
                this._validOriginalDate,
                zeroAmount,
                null,
                null));

        Assert.Contains("Modified amount must be positive", ex.Message);
    }

    [Fact]
    public void CreateModified_With_Negative_Amount_Throws()
    {
        var negativeAmount = MoneyValue.Create("USD", -100m);

        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransferException.CreateModified(
                this._validRecurringTransferId,
                this._validOriginalDate,
                negativeAmount,
                null,
                null));

        Assert.Contains("Modified amount must be positive", ex.Message);
    }

    [Fact]
    public void CreateModified_Trims_Description()
    {
        var result = RecurringTransferException.CreateModified(
            this._validRecurringTransferId,
            this._validOriginalDate,
            null,
            "  Trimmed description  ",
            null);

        Assert.Equal("Trimmed description", result.ModifiedDescription);
    }

    [Fact]
    public void CreateSkipped_Creates_Skipped_Exception()
    {
        var result = RecurringTransferException.CreateSkipped(
            this._validRecurringTransferId,
            this._validOriginalDate);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(this._validRecurringTransferId, result.RecurringTransferId);
        Assert.Equal(this._validOriginalDate, result.OriginalDate);
        Assert.Equal(ExceptionType.Skipped, result.ExceptionType);
        Assert.Null(result.ModifiedAmount);
        Assert.Null(result.ModifiedDescription);
        Assert.Null(result.ModifiedDate);
    }

    [Fact]
    public void CreateSkipped_With_Empty_RecurringTransferId_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            RecurringTransferException.CreateSkipped(
                Guid.Empty,
                this._validOriginalDate));

        Assert.Contains("Recurring transfer ID is required", ex.Message);
    }

    [Fact]
    public void Update_With_Valid_Parameters_Updates_Exception()
    {
        var exception = RecurringTransferException.CreateModified(
            this._validRecurringTransferId,
            this._validOriginalDate,
            MoneyValue.Create("USD", 500m),
            null,
            null);

        var newAmount = MoneyValue.Create("USD", 1000m);
        var newDescription = "Updated description";
        var newDate = new DateOnly(2026, 2, 15);

        exception.Update(newAmount, newDescription, newDate);

        Assert.Equal(newAmount, exception.ModifiedAmount);
        Assert.Equal(newDescription, exception.ModifiedDescription);
        Assert.Equal(newDate, exception.ModifiedDate);
    }

    [Fact]
    public void Update_Skipped_Exception_Throws()
    {
        var exception = RecurringTransferException.CreateSkipped(
            this._validRecurringTransferId,
            this._validOriginalDate);

        var ex = Assert.Throws<DomainException>(() =>
            exception.Update(MoneyValue.Create("USD", 500m), null, null));

        Assert.Contains("Cannot update a skipped exception", ex.Message);
    }

    [Fact]
    public void Update_With_Zero_Amount_Throws()
    {
        var exception = RecurringTransferException.CreateModified(
            this._validRecurringTransferId,
            this._validOriginalDate,
            MoneyValue.Create("USD", 500m),
            null,
            null);

        var zeroAmount = MoneyValue.Create("USD", 0m);

        var ex = Assert.Throws<DomainException>(() =>
            exception.Update(zeroAmount, null, null));

        Assert.Contains("Modified amount must be positive", ex.Message);
    }

    [Fact]
    public void Update_With_All_Null_Throws()
    {
        var exception = RecurringTransferException.CreateModified(
            this._validRecurringTransferId,
            this._validOriginalDate,
            MoneyValue.Create("USD", 500m),
            null,
            null);

        var ex = Assert.Throws<DomainException>(() =>
            exception.Update(null, null, null));

        Assert.Contains("At least one modification is required", ex.Message);
    }
}
