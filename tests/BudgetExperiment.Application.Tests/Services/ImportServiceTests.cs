// <copyright file="ImportServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Services;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

using Moq;

namespace BudgetExperiment.Application.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ImportService"/>.
/// </summary>
public class ImportServiceTests
{
    private readonly Mock<ITransactionRepository> _transactionRepoMock;
    private readonly Mock<ICategorizationRuleRepository> _ruleRepoMock;
    private readonly Mock<IBudgetCategoryRepository> _categoryRepoMock;
    private readonly Mock<IImportBatchRepository> _batchRepoMock;
    private readonly Mock<IUserContext> _userContextMock;
    private readonly ImportService _service;

    public ImportServiceTests()
    {
        this._transactionRepoMock = new Mock<ITransactionRepository>();
        this._ruleRepoMock = new Mock<ICategorizationRuleRepository>();
        this._categoryRepoMock = new Mock<IBudgetCategoryRepository>();
        this._batchRepoMock = new Mock<IImportBatchRepository>();
        this._userContextMock = new Mock<IUserContext>();

        this._ruleRepoMock
            .Setup(r => r.GetActiveByPriorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategorizationRule>());

        this._categoryRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BudgetCategory>());

        this._transactionRepoMock
            .Setup(r => r.GetForDuplicateDetectionAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction>());

        this._service = new ImportService(
            this._transactionRepoMock.Object,
            this._ruleRepoMock.Object,
            this._categoryRepoMock.Object,
            this._batchRepoMock.Object,
            this._userContextMock.Object);
    }

    [Fact]
    public async Task PreviewAsync_WithEmptyRows_ReturnsEmptyResult()
    {
        // Arrange
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows = [],
            Mappings = [],
        };

        // Act
        var result = await this._service.PreviewAsync(request);

        // Assert
        Assert.Empty(result.Rows);
        Assert.Equal(0, result.ValidCount);
    }

    [Fact]
    public async Task PreviewAsync_ParsesDateCorrectly()
    {
        // Arrange
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows = [["01/15/2026", "Test Transaction", "-50.00"]],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
            ],
            DateFormat = "MM/dd/yyyy",
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await this._service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal(new DateOnly(2026, 1, 15), result.Rows[0].Date);
        Assert.Equal(ImportRowStatus.Valid, result.Rows[0].Status);
    }

    [Fact]
    public async Task PreviewAsync_ParsesNegativeAmountAsExpense()
    {
        // Arrange
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows = [["01/15/2026", "Grocery Store", "-75.50"]],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
            ],
            AmountMode = AmountParseMode.NegativeIsExpense,
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await this._service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal(-75.50m, result.Rows[0].Amount);
    }

    [Fact]
    public async Task PreviewAsync_ParsesPositiveAmountAsExpense()
    {
        // Arrange
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows = [["01/15/2026", "Grocery Store", "75.50"]],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
            ],
            AmountMode = AmountParseMode.PositiveIsExpense,
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await this._service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal(-75.50m, result.Rows[0].Amount);
    }

    [Fact]
    public async Task PreviewAsync_ParsesDebitCreditColumns()
    {
        // Arrange
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows =
            [
                ["01/15/2026", "Expense", "100.00", ""],
                ["01/16/2026", "Income", "", "200.00"],
            ],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.DebitAmount },
                new ColumnMappingDto { ColumnIndex = 3, TargetField = ImportField.CreditAmount },
            ],
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await this._service.PreviewAsync(request);

        // Assert
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(-100.00m, result.Rows[0].Amount); // Debit = expense
        Assert.Equal(200.00m, result.Rows[1].Amount);  // Credit = income
    }

    [Fact]
    public async Task PreviewAsync_ParsesAmountWithCurrencySymbol()
    {
        // Arrange
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows = [["01/15/2026", "Test", "$1,234.56"]],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
            ],
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await this._service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal(1234.56m, result.Rows[0].Amount);
    }

    [Fact]
    public async Task PreviewAsync_ParsesAmountWithParentheses()
    {
        // Arrange
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows = [["01/15/2026", "Test", "(50.00)"]],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
            ],
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await this._service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal(-50.00m, result.Rows[0].Amount);
    }

    [Fact]
    public async Task PreviewAsync_MissingDate_ReturnsError()
    {
        // Arrange
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows = [["", "Test Transaction", "-50.00"]],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
            ],
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await this._service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal(ImportRowStatus.Error, result.Rows[0].Status);
        Assert.Contains("Date", result.Rows[0].StatusMessage);
    }

    [Fact]
    public async Task PreviewAsync_InvalidDate_ReturnsError()
    {
        // Arrange
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows = [["not-a-date", "Test", "-50.00"]],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
            ],
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await this._service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal(ImportRowStatus.Error, result.Rows[0].Status);
    }

    [Fact]
    public async Task PreviewAsync_MissingDescription_ReturnsError()
    {
        // Arrange
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows = [["01/15/2026", "", "-50.00"]],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
            ],
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await this._service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal(ImportRowStatus.Error, result.Rows[0].Status);
        Assert.Contains("Description", result.Rows[0].StatusMessage);
    }

    [Fact]
    public async Task PreviewAsync_CombinesMultipleDescriptionColumns()
    {
        // Arrange
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows = [["01/15/2026", "Part One", "Part Two", "-50.00"]],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 3, TargetField = ImportField.Amount },
            ],
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await this._service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal("Part One Part Two", result.Rows[0].Description);
    }

    [Fact]
    public async Task PreviewAsync_MatchesCategoryFromCsv()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = CreateCategory(categoryId, "Groceries");
        this._categoryRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BudgetCategory> { category });

        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows = [["01/15/2026", "Test", "-50.00", "Groceries"]],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
                new ColumnMappingDto { ColumnIndex = 3, TargetField = ImportField.Category },
            ],
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await this._service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal(categoryId, result.Rows[0].CategoryId);
        Assert.Equal(CategorySource.CsvColumn, result.Rows[0].CategorySource);
    }

    [Fact]
    public async Task PreviewAsync_UnknownCategory_ShowsWarning()
    {
        // Arrange
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows = [["01/15/2026", "Test", "-50.00", "NonExistent"]],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
                new ColumnMappingDto { ColumnIndex = 3, TargetField = ImportField.Category },
            ],
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await this._service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal(ImportRowStatus.Warning, result.Rows[0].Status);
        Assert.Null(result.Rows[0].CategoryId);
        Assert.Contains("NonExistent", result.Rows[0].StatusMessage);
    }

    [Fact]
    public async Task PreviewAsync_AppliesCategorizationRule()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var rule = CreateCategorizationRule("Grocery Rule", RuleMatchType.Contains, "GROCERY", categoryId);
        this._ruleRepoMock
            .Setup(r => r.GetActiveByPriorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategorizationRule> { rule });

        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows = [["01/15/2026", "WALMART GROCERY #1234", "-50.00"]],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
            ],
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await this._service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal(categoryId, result.Rows[0].CategoryId);
        Assert.Equal(CategorySource.AutoRule, result.Rows[0].CategorySource);
        Assert.Equal("Grocery Rule", result.Rows[0].MatchedRuleName);
    }

    [Fact]
    public async Task PreviewAsync_CsvCategoryTakesPrecedenceOverRule()
    {
        // Arrange
        var csvCategoryId = Guid.NewGuid();
        var ruleCategoryId = Guid.NewGuid();

        var category = CreateCategory(csvCategoryId, "Food");
        this._categoryRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BudgetCategory> { category });

        var rule = CreateCategorizationRule("Rule", RuleMatchType.Contains, "WALMART", ruleCategoryId);
        this._ruleRepoMock
            .Setup(r => r.GetActiveByPriorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategorizationRule> { rule });

        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows = [["01/15/2026", "WALMART GROCERY", "-50.00", "Food"]],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
                new ColumnMappingDto { ColumnIndex = 3, TargetField = ImportField.Category },
            ],
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await this._service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal(csvCategoryId, result.Rows[0].CategoryId);
        Assert.Equal(CategorySource.CsvColumn, result.Rows[0].CategorySource);
    }

    [Fact]
    public async Task PreviewAsync_CountsSummaryCorrectly()
    {
        // Arrange
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows =
            [
                ["01/15/2026", "Valid1", "-50.00"],
                ["01/16/2026", "Valid2", "-25.00"],
                ["invalid", "Error", "-10.00"],
            ],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
            ],
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await this._service.PreviewAsync(request);

        // Assert
        Assert.Equal(3, result.Rows.Count);
        Assert.Equal(2, result.ValidCount);
        Assert.Equal(1, result.ErrorCount);
        Assert.Equal(-75.00m, result.TotalAmount);
    }

    [Fact]
    public async Task PreviewAsync_ExtractsReference()
    {
        // Arrange
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows = [["01/15/2026", "Test", "-50.00", "REF123"]],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
                new ColumnMappingDto { ColumnIndex = 3, TargetField = ImportField.Reference },
            ],
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await this._service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal("REF123", result.Rows[0].Reference);
    }

    [Theory]
    [InlineData("MM/dd/yyyy", "01/15/2026")]
    [InlineData("dd/MM/yyyy", "15/01/2026")]
    [InlineData("yyyy-MM-dd", "2026-01-15")]
    [InlineData("M/d/yyyy", "1/15/2026")]
    public async Task PreviewAsync_ParsesVariousDateFormats(string format, string dateStr)
    {
        // Arrange
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows = [[dateStr, "Test", "-50.00"]],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
            ],
            DateFormat = format,
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await this._service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal(new DateOnly(2026, 1, 15), result.Rows[0].Date);
        Assert.Equal(ImportRowStatus.Valid, result.Rows[0].Status);
    }

    private static BudgetCategory CreateCategory(Guid id, string name)
    {
        // Use reflection to create the entity since constructor is private
        var category = (BudgetCategory)Activator.CreateInstance(typeof(BudgetCategory), nonPublic: true)!;
        typeof(BudgetCategory).GetProperty("Id")!.SetValue(category, id);
        typeof(BudgetCategory).GetProperty("Name")!.SetValue(category, name);
        return category;
    }

    private static CategorizationRule CreateCategorizationRule(string name, RuleMatchType matchType, string pattern, Guid categoryId)
    {
        return CategorizationRule.Create(name, matchType, pattern, categoryId);
    }
}
