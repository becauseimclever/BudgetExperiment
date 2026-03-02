// <copyright file="ColumnMappingTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the ColumnMappingValue record.
/// </summary>
public class ColumnMappingTests
{
    [Fact]
    public void ColumnMapping_Properties_Are_Set_Correctly()
    {
        // Arrange & Act
        var mapping = new ColumnMappingValue
        {
            ColumnIndex = 2,
            ColumnHeader = "Transaction Date",
            TargetField = ImportField.Date,
            TransformExpression = null,
        };

        // Assert
        Assert.Equal(2, mapping.ColumnIndex);
        Assert.Equal("Transaction Date", mapping.ColumnHeader);
        Assert.Equal(ImportField.Date, mapping.TargetField);
        Assert.Null(mapping.TransformExpression);
    }

    [Fact]
    public void ColumnMapping_With_TransformExpression()
    {
        // Arrange & Act
        var mapping = new ColumnMappingValue
        {
            ColumnIndex = 1,
            ColumnHeader = "Desc1",
            TargetField = ImportField.Description,
            TransformExpression = "concat(Desc1, ' - ', Desc2)",
        };

        // Assert
        Assert.Equal("concat(Desc1, ' - ', Desc2)", mapping.TransformExpression);
    }

    [Fact]
    public void ColumnMapping_Equality()
    {
        // Arrange
        var mapping1 = new ColumnMappingValue { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date };
        var mapping2 = new ColumnMappingValue { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date };
        var mapping3 = new ColumnMappingValue { ColumnIndex = 1, ColumnHeader = "Date", TargetField = ImportField.Date };

        // Assert
        Assert.Equal(mapping1, mapping2);
        Assert.NotEqual(mapping1, mapping3);
    }
}
