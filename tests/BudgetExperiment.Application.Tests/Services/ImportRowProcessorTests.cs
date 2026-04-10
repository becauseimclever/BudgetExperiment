// <copyright file="ImportRowProcessorTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Import;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Categorization;

using Moq;

namespace BudgetExperiment.Application.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ImportRowProcessor"/>.
/// </summary>
public class ImportRowProcessorTests
{
    private readonly Mock<IImportDuplicateDetector> _duplicateDetectorMock;
    private readonly ImportRowProcessor _processor;

    public ImportRowProcessorTests()
    {
        _duplicateDetectorMock = new Mock<IImportDuplicateDetector>();
        _processor = new ImportRowProcessor(_duplicateDetectorMock.Object);
    }

    [Fact]
    public void ProcessRow_WithValidData_ReturnsValidRow()
    {
        // Arrange
        var row = new List<string> { "01/15/2026", "AMAZON PURCHASE", "-50.00" };
        var mappings = CreateMappings(0, 1, 2);

        // Act
        var result = _processor.ProcessRow(
            1,
            row,
            mappings,
            "MM/dd/yyyy",
            AmountParseMode.NegativeIsExpense,
            null,
            [],
            new Dictionary<string, BudgetCategory>(StringComparer.OrdinalIgnoreCase),
            [],
            DisabledDuplicateSettings());

        // Assert
        Assert.Equal(ImportRowStatus.Valid, result.Status);
        Assert.Equal(new DateOnly(2026, 1, 15), result.Date);
        Assert.Equal(-50.00m, result.Amount);
        Assert.Equal("AMAZON PURCHASE", result.Description);
    }

    [Fact]
    public void ProcessRow_MissingDate_ReturnsError()
    {
        // Arrange
        var row = new List<string> { string.Empty, "AMAZON PURCHASE", "-50.00" };
        var mappings = CreateMappings(0, 1, 2);

        // Act
        var result = _processor.ProcessRow(
            1,
            row,
            mappings,
            "MM/dd/yyyy",
            AmountParseMode.NegativeIsExpense,
            null,
            [],
            new Dictionary<string, BudgetCategory>(StringComparer.OrdinalIgnoreCase),
            [],
            DisabledDuplicateSettings());

        // Assert
        Assert.Equal(ImportRowStatus.Error, result.Status);
        Assert.Contains("Date is required", result.StatusMessage);
    }

    [Fact]
    public void ProcessRow_InvalidDate_ReturnsError()
    {
        // Arrange
        var row = new List<string> { "not-a-date", "AMAZON PURCHASE", "-50.00" };
        var mappings = CreateMappings(0, 1, 2);

        // Act
        var result = _processor.ProcessRow(
            1,
            row,
            mappings,
            "MM/dd/yyyy",
            AmountParseMode.NegativeIsExpense,
            null,
            [],
            new Dictionary<string, BudgetCategory>(StringComparer.OrdinalIgnoreCase),
            [],
            DisabledDuplicateSettings());

        // Assert
        Assert.Equal(ImportRowStatus.Error, result.Status);
        Assert.Contains("Could not parse date", result.StatusMessage);
    }

    [Fact]
    public void ProcessRow_MissingDescription_ReturnsError()
    {
        // Arrange
        var row = new List<string> { "01/15/2026", string.Empty, "-50.00" };
        var mappings = CreateMappings(0, 1, 2);

        // Act
        var result = _processor.ProcessRow(
            1,
            row,
            mappings,
            "MM/dd/yyyy",
            AmountParseMode.NegativeIsExpense,
            null,
            [],
            new Dictionary<string, BudgetCategory>(StringComparer.OrdinalIgnoreCase),
            [],
            DisabledDuplicateSettings());

        // Assert
        Assert.Equal(ImportRowStatus.Error, result.Status);
        Assert.Contains("Description is required", result.StatusMessage);
    }

    [Fact]
    public void ProcessRow_CombinesMultipleDescriptionColumns()
    {
        // Arrange
        var row = new List<string> { "01/15/2026", "AMAZON", "50.00", "PURCHASE" };
        var mappings = new List<ColumnMappingDto>
        {
            new() { ColumnIndex = 0, TargetField = ImportField.Date },
            new() { ColumnIndex = 1, TargetField = ImportField.Description },
            new() { ColumnIndex = 2, TargetField = ImportField.Amount },
            new() { ColumnIndex = 3, TargetField = ImportField.Description },
        };

        // Act
        var result = _processor.ProcessRow(
            1,
            row,
            mappings,
            "MM/dd/yyyy",
            AmountParseMode.NegativeIsExpense,
            null,
            [],
            new Dictionary<string, BudgetCategory>(StringComparer.OrdinalIgnoreCase),
            [],
            DisabledDuplicateSettings());

        // Assert
        Assert.Equal("AMAZON PURCHASE", result.Description);
    }

    [Fact]
    public void ProcessRow_ParsesNegativeAmountAsExpense()
    {
        // Arrange
        var row = new List<string> { "01/15/2026", "STORE", "-50.00" };
        var mappings = CreateMappings(0, 1, 2);

        // Act
        var result = _processor.ProcessRow(
            1,
            row,
            mappings,
            "MM/dd/yyyy",
            AmountParseMode.NegativeIsExpense,
            null,
            [],
            new Dictionary<string, BudgetCategory>(StringComparer.OrdinalIgnoreCase),
            [],
            DisabledDuplicateSettings());

        // Assert
        Assert.Equal(-50.00m, result.Amount);
    }

    [Fact]
    public void ProcessRow_ParsesPositiveAsExpense()
    {
        // Arrange
        var row = new List<string> { "01/15/2026", "STORE", "50.00" };
        var mappings = CreateMappings(0, 1, 2);

        // Act
        var result = _processor.ProcessRow(
            1,
            row,
            mappings,
            "MM/dd/yyyy",
            AmountParseMode.PositiveIsExpense,
            null,
            [],
            new Dictionary<string, BudgetCategory>(StringComparer.OrdinalIgnoreCase),
            [],
            DisabledDuplicateSettings());

        // Assert
        Assert.Equal(-50.00m, result.Amount);
    }

    [Fact]
    public void ProcessRow_ParsesDebitCreditColumns()
    {
        // Arrange
        var row = new List<string> { "01/15/2026", "STORE", "50.00", string.Empty };
        var mappings = new List<ColumnMappingDto>
        {
            new() { ColumnIndex = 0, TargetField = ImportField.Date },
            new() { ColumnIndex = 1, TargetField = ImportField.Description },
            new() { ColumnIndex = 2, TargetField = ImportField.DebitAmount },
            new() { ColumnIndex = 3, TargetField = ImportField.CreditAmount },
        };

        // Act
        var result = _processor.ProcessRow(
            1,
            row,
            mappings,
            "MM/dd/yyyy",
            AmountParseMode.NegativeIsExpense,
            null,
            [],
            new Dictionary<string, BudgetCategory>(StringComparer.OrdinalIgnoreCase),
            [],
            DisabledDuplicateSettings());

        // Assert
        Assert.Equal(-50.00m, result.Amount); // Debits are negative
    }

    [Fact]
    public void ProcessRow_ParsesAmountWithCurrencySymbol()
    {
        // Arrange
        var row = new List<string> { "01/15/2026", "STORE", "$50.00" };
        var mappings = CreateMappings(0, 1, 2);

        // Act
        var result = _processor.ProcessRow(
            1,
            row,
            mappings,
            "MM/dd/yyyy",
            AmountParseMode.NegativeIsExpense,
            null,
            [],
            new Dictionary<string, BudgetCategory>(StringComparer.OrdinalIgnoreCase),
            [],
            DisabledDuplicateSettings());

        // Assert
        Assert.Equal(50.00m, result.Amount);
    }

    [Fact]
    public void ProcessRow_ParsesAmountWithParentheses()
    {
        // Arrange
        var row = new List<string> { "01/15/2026", "STORE", "(50.00)" };
        var mappings = CreateMappings(0, 1, 2);

        // Act
        var result = _processor.ProcessRow(
            1,
            row,
            mappings,
            "MM/dd/yyyy",
            AmountParseMode.NegativeIsExpense,
            null,
            [],
            new Dictionary<string, BudgetCategory>(StringComparer.OrdinalIgnoreCase),
            [],
            DisabledDuplicateSettings());

        // Assert
        Assert.Equal(-50.00m, result.Amount); // Parentheses = negative
    }

    [Fact]
    public void ProcessRow_MatchesCsvCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        var row = new List<string> { "01/15/2026", "STORE", "-50.00", "Groceries" };
        var mappings = new List<ColumnMappingDto>
        {
            new() { ColumnIndex = 0, TargetField = ImportField.Date },
            new() { ColumnIndex = 1, TargetField = ImportField.Description },
            new() { ColumnIndex = 2, TargetField = ImportField.Amount },
            new() { ColumnIndex = 3, TargetField = ImportField.Category },
        };

        var categoryByName = new Dictionary<string, BudgetCategory>(StringComparer.OrdinalIgnoreCase)
        {
            ["Groceries"] = category,
        };

        // Act
        var result = _processor.ProcessRow(
            1,
            row,
            mappings,
            "MM/dd/yyyy",
            AmountParseMode.NegativeIsExpense,
            null,
            [],
            categoryByName,
            [],
            DisabledDuplicateSettings());

        // Assert
        Assert.Equal(CategorySource.CsvColumn, result.CategorySource);
        Assert.Equal(category.Id, result.CategoryId);
    }

    [Fact]
    public void ProcessRow_UnknownCategory_ShowsWarning()
    {
        // Arrange
        var row = new List<string> { "01/15/2026", "STORE", "-50.00", "Unknown Category" };
        var mappings = new List<ColumnMappingDto>
        {
            new() { ColumnIndex = 0, TargetField = ImportField.Date },
            new() { ColumnIndex = 1, TargetField = ImportField.Description },
            new() { ColumnIndex = 2, TargetField = ImportField.Amount },
            new() { ColumnIndex = 3, TargetField = ImportField.Category },
        };

        // Act
        var result = _processor.ProcessRow(
            1,
            row,
            mappings,
            "MM/dd/yyyy",
            AmountParseMode.NegativeIsExpense,
            null,
            [],
            new Dictionary<string, BudgetCategory>(StringComparer.OrdinalIgnoreCase),
            [],
            DisabledDuplicateSettings());

        // Assert
        Assert.Equal(ImportRowStatus.Warning, result.Status);
        Assert.Contains("not found", result.StatusMessage);
    }

    [Fact]
    public void ProcessRow_AppliesCategorizationRule()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var rule = CategorizationRule.Create("Amazon Rule", RuleMatchType.Contains, "AMAZON", categoryId);
        var row = new List<string> { "01/15/2026", "AMAZON PURCHASE", "-50.00" };
        var mappings = CreateMappings(0, 1, 2);

        // Act
        var result = _processor.ProcessRow(
            1,
            row,
            mappings,
            "MM/dd/yyyy",
            AmountParseMode.NegativeIsExpense,
            null,
            [rule],
            new Dictionary<string, BudgetCategory>(StringComparer.OrdinalIgnoreCase),
            [],
            DisabledDuplicateSettings());

        // Assert
        Assert.Equal(CategorySource.AutoRule, result.CategorySource);
        Assert.Equal(categoryId, result.CategoryId);
        Assert.Equal("Amazon Rule", result.MatchedRuleName);
    }

    [Fact]
    public void ProcessRow_ExtractsReference()
    {
        // Arrange
        var row = new List<string> { "01/15/2026", "STORE", "-50.00", "REF-12345" };
        var mappings = new List<ColumnMappingDto>
        {
            new() { ColumnIndex = 0, TargetField = ImportField.Date },
            new() { ColumnIndex = 1, TargetField = ImportField.Description },
            new() { ColumnIndex = 2, TargetField = ImportField.Amount },
            new() { ColumnIndex = 3, TargetField = ImportField.Reference },
        };

        // Act
        var result = _processor.ProcessRow(
            1,
            row,
            mappings,
            "MM/dd/yyyy",
            AmountParseMode.NegativeIsExpense,
            null,
            [],
            new Dictionary<string, BudgetCategory>(StringComparer.OrdinalIgnoreCase),
            [],
            DisabledDuplicateSettings());

        // Assert
        Assert.Equal("REF-12345", result.Reference);
    }

    [Fact]
    public void ProcessRow_WithIndicatorColumn_AppliesDebitSign()
    {
        // Arrange
        var row = new List<string> { "01/15/2026", "STORE", "50.00", "Debit" };
        var mappings = new List<ColumnMappingDto>
        {
            new() { ColumnIndex = 0, TargetField = ImportField.Date },
            new() { ColumnIndex = 1, TargetField = ImportField.Description },
            new() { ColumnIndex = 2, TargetField = ImportField.Amount },
            new() { ColumnIndex = 3, TargetField = ImportField.DebitCreditIndicator },
        };
        var indicatorSettings = new DebitCreditIndicatorSettingsDto
        {
            ColumnIndex = 3,
            DebitIndicators = "Debit",
            CreditIndicators = "Credit",
            CaseSensitive = false,
        };

        // Act
        var result = _processor.ProcessRow(
            1,
            row,
            mappings,
            "MM/dd/yyyy",
            AmountParseMode.IndicatorColumn,
            indicatorSettings,
            [],
            new Dictionary<string, BudgetCategory>(StringComparer.OrdinalIgnoreCase),
            [],
            DisabledDuplicateSettings());

        // Assert
        Assert.Equal(-50.00m, result.Amount);
    }

    [Fact]
    public void ProcessRow_WithDuplicateDetected_ReturnsDuplicateStatus()
    {
        // Arrange
        var existingId = Guid.NewGuid();
        var existing = TransactionFactory.Create(
            Guid.NewGuid(),
            MoneyValue.Create("USD", -50.00m),
            new DateOnly(2026, 1, 15),
            "AMAZON PURCHASE");

        _duplicateDetectorMock
            .Setup(d => d.FindDuplicate(
                It.IsAny<DateOnly>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<Transaction>>(),
                It.IsAny<DuplicateDetectionSettingsDto>()))
            .Returns(existingId);

        var row = new List<string> { "01/15/2026", "AMAZON PURCHASE", "-50.00" };
        var mappings = CreateMappings(0, 1, 2);
        var duplicateSettings = new DuplicateDetectionSettingsDto { Enabled = true, LookbackDays = 3 };

        // Act
        var result = _processor.ProcessRow(
            1,
            row,
            mappings,
            "MM/dd/yyyy",
            AmountParseMode.NegativeIsExpense,
            null,
            [],
            new Dictionary<string, BudgetCategory>(StringComparer.OrdinalIgnoreCase),
            [existing],
            duplicateSettings);

        // Assert
        Assert.Equal(ImportRowStatus.Duplicate, result.Status);
        Assert.Equal(existingId, result.DuplicateOfTransactionId);
        Assert.False(result.IsSelected);
    }

    [Theory]
    [InlineData("01/15/2026", "MM/dd/yyyy")]
    [InlineData("2026-01-15", "yyyy-MM-dd")]
    [InlineData("15/01/2026", "dd/MM/yyyy")]
    [InlineData("Jan 15, 2026", "MMM dd, yyyy")]
    public void ProcessRow_ParsesVariousDateFormats(string dateStr, string format)
    {
        // Arrange
        var row = new List<string> { dateStr, "STORE", "-50.00" };
        var mappings = CreateMappings(0, 1, 2);

        // Act
        var result = _processor.ProcessRow(
            1,
            row,
            mappings,
            format,
            AmountParseMode.NegativeIsExpense,
            null,
            [],
            new Dictionary<string, BudgetCategory>(StringComparer.OrdinalIgnoreCase),
            [],
            DisabledDuplicateSettings());

        // Assert
        Assert.Equal(new DateOnly(2026, 1, 15), result.Date);
    }

    [Fact]
    public void ExtractDatesFromRows_ExtractsValidDates()
    {
        // Arrange
        var rows = new List<IReadOnlyList<string>>
        {
            new List<string> { "01/15/2026", "STORE" },
            new List<string> { "01/16/2026", "STORE" },
            new List<string> { "bad-date", "STORE" },
        };
        var mappings = new List<ColumnMappingDto>
        {
            new() { ColumnIndex = 0, TargetField = ImportField.Date },
        };

        // Act
        var dates = _processor.ExtractDatesFromRows(rows, mappings, "MM/dd/yyyy");

        // Assert
        Assert.Equal(2, dates.Count);
        Assert.Contains(new DateOnly(2026, 1, 15), dates);
        Assert.Contains(new DateOnly(2026, 1, 16), dates);
    }

    [Fact]
    public void ExtractDatesFromRows_NoDateMapping_ReturnsEmpty()
    {
        // Arrange
        var rows = new List<IReadOnlyList<string>> { new List<string> { "01/15/2026" } };
        var mappings = new List<ColumnMappingDto>
        {
            new() { ColumnIndex = 0, TargetField = ImportField.Description },
        };

        // Act
        var dates = _processor.ExtractDatesFromRows(rows, mappings, "MM/dd/yyyy");

        // Assert
        Assert.Empty(dates);
    }

    [Fact]
    public void ProcessRow_SetsCorrectRowIndex()
    {
        // Arrange
        var row = new List<string> { "01/15/2026", "STORE", "-50.00" };
        var mappings = CreateMappings(0, 1, 2);

        // Act
        var result = _processor.ProcessRow(
            42,
            row,
            mappings,
            "MM/dd/yyyy",
            AmountParseMode.NegativeIsExpense,
            null,
            [],
            new Dictionary<string, BudgetCategory>(StringComparer.OrdinalIgnoreCase),
            [],
            DisabledDuplicateSettings());

        // Assert
        Assert.Equal(42, result.RowIndex);
    }

    [Fact]
    public void ProcessRow_SanitizedNegativeAmount_ParsesCorrectly()
    {
        // Arrange — CsvSanitizer prefixes '-' values with apostrophe for display safety
        var row = new List<string> { "01/15/2026", "GROCERY STORE", "'-10.05" };
        var mappings = CreateMappings(0, 1, 2);

        // Act
        var result = _processor.ProcessRow(
            1,
            row,
            mappings,
            "MM/dd/yyyy",
            AmountParseMode.NegativeIsExpense,
            null,
            [],
            new Dictionary<string, BudgetCategory>(StringComparer.OrdinalIgnoreCase),
            [],
            DisabledDuplicateSettings());

        // Assert
        Assert.Equal(ImportRowStatus.Valid, result.Status);
        Assert.Equal(-10.05m, result.Amount);
    }

    [Fact]
    public void ProcessRow_SanitizedPositiveAmount_ParsesCorrectly()
    {
        // Arrange — CsvSanitizer prefixes '+' values with apostrophe for display safety
        var row = new List<string> { "01/15/2026", "REFUND", "'+250.00" };
        var mappings = CreateMappings(0, 1, 2);

        // Act
        var result = _processor.ProcessRow(
            1,
            row,
            mappings,
            "MM/dd/yyyy",
            AmountParseMode.NegativeIsExpense,
            null,
            [],
            new Dictionary<string, BudgetCategory>(StringComparer.OrdinalIgnoreCase),
            [],
            DisabledDuplicateSettings());

        // Assert
        Assert.Equal(ImportRowStatus.Valid, result.Status);
        Assert.Equal(250.00m, result.Amount);
    }

    [Fact]
    public void ProcessRow_SanitizedNegativeAmount_PositiveIsExpenseMode_ParsesCorrectly()
    {
        // Arrange — sanitized negative with PositiveIsExpense mode
        var row = new List<string> { "01/15/2026", "STORE", "'-45.99" };
        var mappings = CreateMappings(0, 1, 2);

        // Act
        var result = _processor.ProcessRow(
            1,
            row,
            mappings,
            "MM/dd/yyyy",
            AmountParseMode.PositiveIsExpense,
            null,
            [],
            new Dictionary<string, BudgetCategory>(StringComparer.OrdinalIgnoreCase),
            [],
            DisabledDuplicateSettings());

        // Assert — PositiveIsExpense negates: -(-45.99) = 45.99
        Assert.Equal(ImportRowStatus.Valid, result.Status);
        Assert.Equal(45.99m, result.Amount);
    }

    private static List<ColumnMappingDto> CreateMappings(int dateCol, int descCol, int amountCol)
    {
        return
        [
            new() { ColumnIndex = dateCol, TargetField = ImportField.Date },
            new() { ColumnIndex = descCol, TargetField = ImportField.Description },
            new() { ColumnIndex = amountCol, TargetField = ImportField.Amount },
        ];
    }

    private static DuplicateDetectionSettingsDto DisabledDuplicateSettings()
    {
        return new DuplicateDetectionSettingsDto { Enabled = false };
    }
}
