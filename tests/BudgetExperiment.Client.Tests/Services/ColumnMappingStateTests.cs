// <copyright file="ColumnMappingStateTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Contracts.Dtos;
using Shouldly;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ColumnMappingState"/> conversion logic.
/// </summary>
public sealed class ColumnMappingStateTests
{
    /// <summary>
    /// Verifies ToDto returns null when TargetField is null.
    /// </summary>
    [Fact]
    public void ToDto_NullTargetField_ReturnsNull()
    {
        var state = new ColumnMappingState
        {
            ColumnIndex = 0,
            ColumnHeader = "Date",
            TargetField = null,
        };

        state.ToDto().ShouldBeNull();
    }

    /// <summary>
    /// Verifies ToDto returns null when TargetField is Ignore.
    /// </summary>
    [Fact]
    public void ToDto_IgnoreField_ReturnsNull()
    {
        var state = new ColumnMappingState
        {
            ColumnIndex = 1,
            ColumnHeader = "Extra",
            TargetField = ImportField.Ignore,
        };

        state.ToDto().ShouldBeNull();
    }

    /// <summary>
    /// Verifies ToDto returns correct DTO when a valid field is mapped.
    /// </summary>
    [Fact]
    public void ToDto_ValidField_ReturnsCorrectDto()
    {
        var state = new ColumnMappingState
        {
            ColumnIndex = 2,
            ColumnHeader = "Amount",
            TargetField = ImportField.Amount,
        };

        var dto = state.ToDto();

        dto.ShouldNotBeNull();
        dto.ColumnIndex.ShouldBe(2);
        dto.ColumnHeader.ShouldBe("Amount");
        dto.TargetField.ShouldBe(ImportField.Amount);
    }

    /// <summary>
    /// Verifies ToDto maps Date field correctly.
    /// </summary>
    [Fact]
    public void ToDto_DateField_MapsCorrectly()
    {
        var state = new ColumnMappingState
        {
            ColumnIndex = 0,
            ColumnHeader = "Transaction Date",
            TargetField = ImportField.Date,
        };

        var dto = state.ToDto();

        dto.ShouldNotBeNull();
        dto.TargetField.ShouldBe(ImportField.Date);
    }

    /// <summary>
    /// Verifies ToDto maps Description field correctly.
    /// </summary>
    [Fact]
    public void ToDto_DescriptionField_MapsCorrectly()
    {
        var state = new ColumnMappingState
        {
            ColumnIndex = 1,
            ColumnHeader = "Memo",
            TargetField = ImportField.Description,
        };

        var dto = state.ToDto();

        dto.ShouldNotBeNull();
        dto.TargetField.ShouldBe(ImportField.Description);
    }

    /// <summary>
    /// Verifies default SampleValues is empty.
    /// </summary>
    [Fact]
    public void Default_SampleValues_IsEmpty()
    {
        var state = new ColumnMappingState();

        state.SampleValues.ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies default ColumnHeader is empty string.
    /// </summary>
    [Fact]
    public void Default_ColumnHeader_IsEmpty()
    {
        var state = new ColumnMappingState();

        state.ColumnHeader.ShouldBe(string.Empty);
    }
}
