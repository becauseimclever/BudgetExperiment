// <copyright file="ImportMappingTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the ImportMapping entity.
/// </summary>
public class ImportMappingTests
{
    [Fact]
    public void Create_With_Valid_Data_Creates_ImportMapping()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var name = "Chase Checking Export";
        var mappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
            new() { ColumnIndex = 1, ColumnHeader = "Description", TargetField = ImportField.Description },
            new() { ColumnIndex = 2, ColumnHeader = "Amount", TargetField = ImportField.Amount },
        };

        // Act
        var importMapping = ImportMapping.Create(userId, name, mappings);

        // Assert
        Assert.NotEqual(Guid.Empty, importMapping.Id);
        Assert.Equal(userId, importMapping.UserId);
        Assert.Equal(name, importMapping.Name);
        Assert.Equal(3, importMapping.ColumnMappings.Count);
        Assert.Equal("MM/dd/yyyy", importMapping.DateFormat);
        Assert.Equal(AmountParseMode.NegativeIsExpense, importMapping.AmountMode);
        Assert.NotNull(importMapping.DuplicateSettings);
        Assert.NotEqual(default, importMapping.CreatedAtUtc);
        Assert.NotEqual(default, importMapping.UpdatedAtUtc);
        Assert.Null(importMapping.LastUsedAtUtc);
    }

    [Fact]
    public void Create_With_Empty_UserId_Throws()
    {
        // Arrange
        var name = "Test Mapping";
        var mappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
        };

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => ImportMapping.Create(Guid.Empty, name, mappings));
        Assert.Contains("user", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Empty_Name_Throws()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
        };

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => ImportMapping.Create(userId, string.Empty, mappings));
        Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Whitespace_Name_Throws()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
        };

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => ImportMapping.Create(userId, "   ", mappings));
        Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Null_Mappings_Throws()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var name = "Test Mapping";

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => ImportMapping.Create(userId, name, null!));
        Assert.Contains("mapping", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Empty_Mappings_Throws()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var name = "Test Mapping";
        var mappings = new List<ColumnMapping>();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => ImportMapping.Create(userId, name, mappings));
        Assert.Contains("mapping", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_Trims_Name()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var name = "  Chase Export  ";
        var mappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
        };

        // Act
        var importMapping = ImportMapping.Create(userId, name, mappings);

        // Assert
        Assert.Equal("Chase Export", importMapping.Name);
    }

    [Fact]
    public void Create_Name_Exceeds_MaxLength_Throws()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var name = new string('A', 201); // Max is 200
        var mappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
        };

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => ImportMapping.Create(userId, name, mappings));
        Assert.Contains("200", ex.Message);
    }

    [Fact]
    public void Update_Changes_Mapping_Properties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
        };
        var importMapping = ImportMapping.Create(userId, "Original Name", mappings);
        var originalUpdatedAt = importMapping.UpdatedAtUtc;

        var newMappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Transaction Date", TargetField = ImportField.Date },
            new() { ColumnIndex = 1, ColumnHeader = "Memo", TargetField = ImportField.Description },
        };

        // Act
        importMapping.Update("Updated Name", newMappings, "yyyy-MM-dd", AmountParseMode.PositiveIsExpense);

        // Assert
        Assert.Equal("Updated Name", importMapping.Name);
        Assert.Equal(2, importMapping.ColumnMappings.Count);
        Assert.Equal("yyyy-MM-dd", importMapping.DateFormat);
        Assert.Equal(AmountParseMode.PositiveIsExpense, importMapping.AmountMode);
        Assert.True(importMapping.UpdatedAtUtc >= originalUpdatedAt);
    }

    [Fact]
    public void Update_With_Empty_Name_Throws()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
        };
        var importMapping = ImportMapping.Create(userId, "Original", mappings);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => importMapping.Update(string.Empty, mappings, "MM/dd/yyyy", AmountParseMode.NegativeIsExpense));
        Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Update_With_Empty_Mappings_Throws()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
        };
        var importMapping = ImportMapping.Create(userId, "Original", mappings);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => importMapping.Update("Updated", new List<ColumnMapping>(), "MM/dd/yyyy", AmountParseMode.NegativeIsExpense));
        Assert.Contains("mapping", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpdateDuplicateSettings_Changes_Settings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
        };
        var importMapping = ImportMapping.Create(userId, "Test", mappings);
        var originalUpdatedAt = importMapping.UpdatedAtUtc;

        var newSettings = new DuplicateDetectionSettings
        {
            Enabled = true,
            LookbackDays = 30,
            DescriptionMatch = DescriptionMatchMode.Exact,
        };

        // Act
        importMapping.UpdateDuplicateSettings(newSettings);

        // Assert
        Assert.True(importMapping.DuplicateSettings.Enabled);
        Assert.Equal(30, importMapping.DuplicateSettings.LookbackDays);
        Assert.Equal(DescriptionMatchMode.Exact, importMapping.DuplicateSettings.DescriptionMatch);
        Assert.True(importMapping.UpdatedAtUtc >= originalUpdatedAt);
    }

    [Fact]
    public void UpdateDuplicateSettings_With_Null_Throws()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
        };
        var importMapping = ImportMapping.Create(userId, "Test", mappings);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => importMapping.UpdateDuplicateSettings(null!));
        Assert.Contains("settings", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MarkUsed_Updates_LastUsedAtUtc()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
        };
        var importMapping = ImportMapping.Create(userId, "Test", mappings);
        Assert.Null(importMapping.LastUsedAtUtc);

        // Act
        importMapping.MarkUsed();

        // Assert
        Assert.NotNull(importMapping.LastUsedAtUtc);
        Assert.True(importMapping.LastUsedAtUtc.Value <= DateTime.UtcNow);
    }

    [Fact]
    public void MarkUsed_Called_Multiple_Times_Updates_Timestamp()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
        };
        var importMapping = ImportMapping.Create(userId, "Test", mappings);
        importMapping.MarkUsed();
        var firstUsedAt = importMapping.LastUsedAtUtc;

        // Act
        importMapping.MarkUsed();

        // Assert
        Assert.True(importMapping.LastUsedAtUtc >= firstUsedAt);
    }
}

/// <summary>
/// Unit tests for the ColumnMapping record.
/// </summary>
public class ColumnMappingTests
{
    [Fact]
    public void ColumnMapping_Properties_Are_Set_Correctly()
    {
        // Arrange & Act
        var mapping = new ColumnMapping
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
        var mapping = new ColumnMapping
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
        var mapping1 = new ColumnMapping { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date };
        var mapping2 = new ColumnMapping { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date };
        var mapping3 = new ColumnMapping { ColumnIndex = 1, ColumnHeader = "Date", TargetField = ImportField.Date };

        // Assert
        Assert.Equal(mapping1, mapping2);
        Assert.NotEqual(mapping1, mapping3);
    }
}

/// <summary>
/// Unit tests for the DuplicateDetectionSettings record.
/// </summary>
public class DuplicateDetectionSettingsTests
{
    [Fact]
    public void Default_Values_Are_Correct()
    {
        // Arrange & Act
        var settings = new DuplicateDetectionSettings();

        // Assert
        Assert.True(settings.Enabled);
        Assert.Equal(30, settings.LookbackDays);
        Assert.Equal(DescriptionMatchMode.Exact, settings.DescriptionMatch);
    }

    [Fact]
    public void Custom_Values_Are_Set_Correctly()
    {
        // Arrange & Act
        var settings = new DuplicateDetectionSettings
        {
            Enabled = false,
            LookbackDays = 60,
            DescriptionMatch = DescriptionMatchMode.Contains,
        };

        // Assert
        Assert.False(settings.Enabled);
        Assert.Equal(60, settings.LookbackDays);
        Assert.Equal(DescriptionMatchMode.Contains, settings.DescriptionMatch);
    }

    [Fact]
    public void DuplicateDetectionSettings_Equality()
    {
        // Arrange
        var settings1 = new DuplicateDetectionSettings { Enabled = true, LookbackDays = 30, DescriptionMatch = DescriptionMatchMode.Exact };
        var settings2 = new DuplicateDetectionSettings { Enabled = true, LookbackDays = 30, DescriptionMatch = DescriptionMatchMode.Exact };
        var settings3 = new DuplicateDetectionSettings { Enabled = true, LookbackDays = 60, DescriptionMatch = DescriptionMatchMode.Exact };

        // Assert
        Assert.Equal(settings1, settings2);
        Assert.NotEqual(settings1, settings3);
    }
}

/// <summary>
/// Unit tests for the ImportField enum.
/// </summary>
public class ImportFieldTests
{
    [Theory]
    [InlineData(ImportField.Ignore, 0)]
    [InlineData(ImportField.Date, 1)]
    [InlineData(ImportField.Description, 2)]
    [InlineData(ImportField.Amount, 3)]
    [InlineData(ImportField.DebitAmount, 4)]
    [InlineData(ImportField.CreditAmount, 5)]
    [InlineData(ImportField.Category, 6)]
    [InlineData(ImportField.Reference, 7)]
    public void ImportField_Has_Expected_Values(ImportField field, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)field);
    }
}

/// <summary>
/// Unit tests for the AmountParseMode enum.
/// </summary>
public class AmountParseModeTests
{
    [Theory]
    [InlineData(AmountParseMode.NegativeIsExpense, 0)]
    [InlineData(AmountParseMode.PositiveIsExpense, 1)]
    [InlineData(AmountParseMode.SeparateColumns, 2)]
    public void AmountParseMode_Has_Expected_Values(AmountParseMode mode, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)mode);
    }
}

/// <summary>
/// Unit tests for the DescriptionMatchMode enum.
/// </summary>
public class DescriptionMatchModeTests
{
    [Theory]
    [InlineData(DescriptionMatchMode.Exact, 0)]
    [InlineData(DescriptionMatchMode.Contains, 1)]
    [InlineData(DescriptionMatchMode.Fuzzy, 2)]
    public void DescriptionMatchMode_Has_Expected_Values(DescriptionMatchMode mode, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)mode);
    }
}
