// <copyright file="ImportServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Import;
using BudgetExperiment.Application.Recurring;
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
    private readonly Mock<IImportMappingRepository> _mappingRepoMock;
    private readonly Mock<IAccountRepository> _accountRepoMock;
    private readonly Mock<IUserContext> _userContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IReconciliationService> _reconciliationServiceMock;
    private readonly Mock<IImportPreviewEnricher> _previewEnricherMock;
    private readonly Mock<IImportTransactionCreator> _transactionCreatorMock;
    private readonly ImportService _service;

    public ImportServiceTests()
    {
        _transactionRepoMock = new Mock<ITransactionRepository>();
        _ruleRepoMock = new Mock<ICategorizationRuleRepository>();
        _categoryRepoMock = new Mock<IBudgetCategoryRepository>();
        _batchRepoMock = new Mock<IImportBatchRepository>();
        _mappingRepoMock = new Mock<IImportMappingRepository>();
        _accountRepoMock = new Mock<IAccountRepository>();
        _userContextMock = new Mock<IUserContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _reconciliationServiceMock = new Mock<IReconciliationService>();
        _previewEnricherMock = new Mock<IImportPreviewEnricher>();
        _transactionCreatorMock = new Mock<IImportTransactionCreator>();

        // Default: transaction creator returns a result matching the input count
        _transactionCreatorMock
            .Setup(c => c.CreateTransactionsAsync(It.IsAny<Account>(), It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ImportTransactionData>>(), It.IsAny<CancellationToken>()))
            .Returns<Account, Guid, IReadOnlyList<ImportTransactionData>, CancellationToken>((_, _, txs, _) =>
                Task.FromResult(new ImportTransactionResult(
                    txs.Select(_ => Guid.NewGuid()).ToList(),
                    txs.Count(t => t.CategorySource == CategorySource.AutoRule),
                    txs.Count(t => t.CategorySource == CategorySource.CsvColumn),
                    txs.Count(t => t.CategorySource == CategorySource.None),
                    0,
                    txs.Count(t => !string.IsNullOrEmpty(t.LocationCity) || !string.IsNullOrEmpty(t.LocationStateOrRegion)))));

        // Default: enricher passes rows through unchanged
        _previewEnricherMock
            .Setup(e => e.EnrichWithRecurringMatchesAsync(
                It.IsAny<List<ImportPreviewRow>>(),
                It.IsAny<CancellationToken>()))
            .Returns<List<ImportPreviewRow>, CancellationToken>((rows, _) => Task.FromResult(rows));

        _previewEnricherMock
            .Setup(e => e.EnrichWithLocationDataAsync(
                It.IsAny<List<ImportPreviewRow>>(),
                It.IsAny<CancellationToken>()))
            .Returns<List<ImportPreviewRow>, CancellationToken>((rows, _) => Task.FromResult(rows));

        _ruleRepoMock
            .Setup(r => r.GetActiveByPriorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategorizationRule>());

        _categoryRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BudgetCategory>());

        _transactionRepoMock
            .Setup(r => r.GetForDuplicateDetectionAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction>());

        _service = new ImportService(
            new ImportRowProcessor(new ImportDuplicateDetector()),
            _previewEnricherMock.Object,
            new Mock<IImportBatchManager>().Object,
            _transactionCreatorMock.Object,
            _transactionRepoMock.Object,
            _ruleRepoMock.Object,
            _categoryRepoMock.Object,
            _batchRepoMock.Object,
            _mappingRepoMock.Object,
            _accountRepoMock.Object,
            _reconciliationServiceMock.Object,
            new Mock<IRecurringChargeDetectionService>().Object,
            _userContextMock.Object,
            _unitOfWorkMock.Object);
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
        var result = await _service.PreviewAsync(request);

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
        var result = await _service.PreviewAsync(request);

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
        var result = await _service.PreviewAsync(request);

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
        var result = await _service.PreviewAsync(request);

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
                ["01/15/2026", "Expense", "100.00", string.Empty],
                ["01/16/2026", "Income", string.Empty, "200.00"],
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
        var result = await _service.PreviewAsync(request);

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
        var result = await _service.PreviewAsync(request);

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
        var result = await _service.PreviewAsync(request);

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
            Rows = [[string.Empty, "Test Transaction", "-50.00"]],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
            ],
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await _service.PreviewAsync(request);

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
        var result = await _service.PreviewAsync(request);

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
            Rows = [["01/15/2026", string.Empty, "-50.00"]],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
            ],
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await _service.PreviewAsync(request);

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
        var result = await _service.PreviewAsync(request);

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
        _categoryRepoMock
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
        var result = await _service.PreviewAsync(request);

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
        var result = await _service.PreviewAsync(request);

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
        _ruleRepoMock
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
        var result = await _service.PreviewAsync(request);

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
        _categoryRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BudgetCategory> { category });

        var rule = CreateCategorizationRule("Rule", RuleMatchType.Contains, "WALMART", ruleCategoryId);
        _ruleRepoMock
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
        var result = await _service.PreviewAsync(request);

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
        var result = await _service.PreviewAsync(request);

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
        var result = await _service.PreviewAsync(request);

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
        var result = await _service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal(new DateOnly(2026, 1, 15), result.Rows[0].Date);
        Assert.Equal(ImportRowStatus.Valid, result.Rows[0].Status);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoUser_ThrowsDomainException()
    {
        // Arrange
        _userContextMock.Setup(u => u.UserIdAsGuid).Returns((Guid?)null);

        var request = new ImportExecuteRequest
        {
            AccountId = Guid.NewGuid(),
            FileName = "test.csv",
            Transactions = [],
        };

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => _service.ExecuteAsync(request));
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyTransactions_ReturnsEmptyBatchId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = CreateAccount(accountId, "Test Account", userId);

        _userContextMock.Setup(u => u.UserIdAsGuid).Returns(userId);
        _accountRepoMock.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var request = new ImportExecuteRequest
        {
            AccountId = accountId,
            FileName = "test.csv",
            Transactions = [],
        };

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Equal(Guid.Empty, result.BatchId);
        Assert.Equal(0, result.ImportedCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidAccountId_ThrowsDomainException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        _userContextMock.Setup(u => u.UserIdAsGuid).Returns(userId);
        _accountRepoMock.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var request = new ImportExecuteRequest
        {
            AccountId = accountId,
            FileName = "test.csv",
            Transactions =
            [
                new ImportTransactionData { Date = new DateOnly(2026, 1, 15), Description = "Test", Amount = -50m },
            ],
        };

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => _service.ExecuteAsync(request));
    }

    [Fact]
    public async Task ExecuteAsync_CreatesTransactionsAndBatch()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = CreateAccount(accountId, "Test Account", userId);

        _userContextMock.Setup(u => u.UserIdAsGuid).Returns(userId);
        _accountRepoMock.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        ImportBatch? savedBatch = null;
        _batchRepoMock.Setup(r => r.AddAsync(It.IsAny<ImportBatch>(), It.IsAny<CancellationToken>()))
            .Callback<ImportBatch, CancellationToken>((b, _) => savedBatch = b)
            .Returns(Task.CompletedTask);

        var request = new ImportExecuteRequest
        {
            AccountId = accountId,
            FileName = "test.csv",
            Transactions =
            [
                new ImportTransactionData { Date = new DateOnly(2026, 1, 15), Description = "Transaction 1", Amount = -50m, CategorySource = CategorySource.None },
                new ImportTransactionData { Date = new DateOnly(2026, 1, 16), Description = "Transaction 2", Amount = -25m, CategorySource = CategorySource.AutoRule },
            ],
        };

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(2, result.CreatedTransactionIds.Count);
        Assert.Equal(1, result.AutoCategorizedCount);
        Assert.Equal(1, result.UncategorizedCount);
        Assert.NotNull(savedBatch);
        Assert.Equal(ImportBatchStatus.Completed, savedBatch.Status);
        _transactionCreatorMock.Verify(
            c => c.CreateTransactionsAsync(account, It.IsAny<Guid>(), request.Transactions, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_TracksCategorySourceStatistics()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = CreateAccount(accountId, "Test Account", userId);

        _userContextMock.Setup(u => u.UserIdAsGuid).Returns(userId);
        _accountRepoMock.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var request = new ImportExecuteRequest
        {
            AccountId = accountId,
            FileName = "test.csv",
            Transactions =
            [
                new ImportTransactionData { Date = new DateOnly(2026, 1, 15), Description = "Tx1", Amount = -10m, CategorySource = CategorySource.AutoRule },
                new ImportTransactionData { Date = new DateOnly(2026, 1, 15), Description = "Tx2", Amount = -20m, CategorySource = CategorySource.AutoRule },
                new ImportTransactionData { Date = new DateOnly(2026, 1, 15), Description = "Tx3", Amount = -30m, CategorySource = CategorySource.CsvColumn },
                new ImportTransactionData { Date = new DateOnly(2026, 1, 15), Description = "Tx4", Amount = -40m, CategorySource = CategorySource.None },
            ],
        };

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Equal(4, result.ImportedCount);
        Assert.Equal(2, result.AutoCategorizedCount);
        Assert.Equal(1, result.CsvCategorizedCount);
        Assert.Equal(1, result.UncategorizedCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithRunReconciliationTrue_CallsReconciliationService()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        _userContextMock.Setup(u => u.UserIdAsGuid).Returns(userId);
        _accountRepoMock.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAccount(accountId, "Test Account", userId));

        var txIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        _transactionCreatorMock
            .Setup(c => c.CreateTransactionsAsync(It.IsAny<Account>(), It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ImportTransactionData>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImportTransactionResult(txIds, 0, 0, 2, 0, 0));

        var reconciliationResult = new FindMatchesResult
        {
            TotalMatchesFound = 2,
            HighConfidenceCount = 1,
            MatchesByTransaction = new Dictionary<Guid, IReadOnlyList<ReconciliationMatchDto>>(),
        };
        _reconciliationServiceMock
            .Setup(r => r.FindMatchesAsync(It.IsAny<FindMatchesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reconciliationResult);

        var request = new ImportExecuteRequest
        {
            AccountId = accountId,
            FileName = "test.csv",
            RunReconciliation = true,
            Transactions =
            [
                new ImportTransactionData { Date = new DateOnly(2026, 1, 15), Description = "Netflix", Amount = -15m },
                new ImportTransactionData { Date = new DateOnly(2026, 1, 16), Description = "Spotify", Amount = -10m },
            ],
        };

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Equal(2, result.ReconciliationMatchCount);
        Assert.Equal(1, result.AutoMatchedCount);
        Assert.Equal(1, result.PendingMatchCount);
        _reconciliationServiceMock.Verify(
            r => r.FindMatchesAsync(
            It.Is<FindMatchesRequest>(req => req.TransactionIds.Count == 2),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithRunReconciliationFalse_SkipsReconciliation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        _userContextMock.Setup(u => u.UserIdAsGuid).Returns(userId);
        _accountRepoMock.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAccount(accountId, "Test Account", userId));

        var request = new ImportExecuteRequest
        {
            AccountId = accountId,
            FileName = "test.csv",
            RunReconciliation = false,
            Transactions =
            [
                new ImportTransactionData { Date = new DateOnly(2026, 1, 15), Description = "Netflix", Amount = -15m },
            ],
        };

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.Equal(0, result.ReconciliationMatchCount);
        _reconciliationServiceMock.Verify(
            r => r.FindMatchesAsync(It.IsAny<FindMatchesRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PreviewAsync_WithSkipRows_ProcessesAllRowsAndOffsetsRowIndex()
    {
        // Arrange - CsvParserService already stripped 2 metadata rows before header.
        // PreviewAsync receives only the data rows, RowsToSkip is for display offset only.
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows =
            [
                ["01/15/2026", "Actual Transaction", "-50.00"],
            ],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
            ],
            DateFormat = "MM/dd/yyyy",
            RowsToSkip = 2,
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await _service.PreviewAsync(request);

        // Assert - data row is processed, row index offset by skipped rows + header
        Assert.Single(result.Rows);
        Assert.Equal("Actual Transaction", result.Rows[0].Description);
        Assert.Equal(-50.00m, result.Rows[0].Amount);
        Assert.Equal(3, result.Rows[0].RowIndex); // Row index accounts for skipped rows (1-based)
    }

    [Fact]
    public async Task PreviewAsync_WithHighSkipRows_StillProcessesAllDataRows()
    {
        // Arrange - RowsToSkip is handled by CsvParserService during parsing.
        // PreviewAsync receives post-parse data rows regardless of RowsToSkip value.
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows =
            [
                ["01/15/2026", "Transaction", "-50.00"],
            ],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
            ],
            DateFormat = "MM/dd/yyyy",
            RowsToSkip = 5, // Only used for row-index display offset
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await _service.PreviewAsync(request);

        // Assert - data row is still processed, row index offset by skipped rows
        Assert.Single(result.Rows);
        Assert.Equal(6, result.Rows[0].RowIndex); // 5 skipped + 1-based
    }

    [Fact]
    public async Task PreviewAsync_WithZeroSkipRows_ProcessesAllRows()
    {
        // Arrange
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows =
            [
                ["01/15/2026", "Transaction 1", "-50.00"],
                ["01/16/2026", "Transaction 2", "-25.00"],
            ],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
            ],
            DateFormat = "MM/dd/yyyy",
            RowsToSkip = 0,
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await _service.PreviewAsync(request);

        // Assert
        Assert.Equal(2, result.Rows.Count);
    }

    [Fact]
    public async Task PreviewAsync_WithIndicatorColumn_AppliesDebitSign()
    {
        // Arrange
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows = [["01/15/2026", "Coffee Shop", "5.00", "Debit"]],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
                new ColumnMappingDto { ColumnIndex = 3, TargetField = ImportField.DebitCreditIndicator },
            ],
            DateFormat = "MM/dd/yyyy",
            AmountMode = AmountParseMode.IndicatorColumn,
            IndicatorSettings = new DebitCreditIndicatorSettingsDto
            {
                ColumnIndex = 3,
                DebitIndicators = "Debit,DR",
                CreditIndicators = "Credit,CR",
                CaseSensitive = false,
            },
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await _service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal(-5.00m, result.Rows[0].Amount); // Debit = negative
    }

    [Fact]
    public async Task PreviewAsync_WithIndicatorColumn_AppliesCreditSign()
    {
        // Arrange
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows = [["01/15/2026", "Salary", "1000.00", "Credit"]],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
                new ColumnMappingDto { ColumnIndex = 3, TargetField = ImportField.DebitCreditIndicator },
            ],
            DateFormat = "MM/dd/yyyy",
            AmountMode = AmountParseMode.IndicatorColumn,
            IndicatorSettings = new DebitCreditIndicatorSettingsDto
            {
                ColumnIndex = 3,
                DebitIndicators = "Debit,DR",
                CreditIndicators = "Credit,CR",
                CaseSensitive = false,
            },
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await _service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal(1000.00m, result.Rows[0].Amount); // Credit = positive
    }

    [Fact]
    public async Task PreviewAsync_WithIndicatorColumn_CaseInsensitiveMatch()
    {
        // Arrange
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows = [["01/15/2026", "Coffee Shop", "5.00", "DEBIT"]],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
                new ColumnMappingDto { ColumnIndex = 3, TargetField = ImportField.DebitCreditIndicator },
            ],
            DateFormat = "MM/dd/yyyy",
            AmountMode = AmountParseMode.IndicatorColumn,
            IndicatorSettings = new DebitCreditIndicatorSettingsDto
            {
                ColumnIndex = 3,
                DebitIndicators = "debit",
                CreditIndicators = "credit",
                CaseSensitive = false,
            },
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await _service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal(-5.00m, result.Rows[0].Amount);
    }

    [Fact]
    public async Task PreviewAsync_WithIndicatorColumn_UnrecognizedIndicator_AddsWarning()
    {
        // Arrange
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows = [["01/15/2026", "Coffee Shop", "5.00", "Unknown"]],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
                new ColumnMappingDto { ColumnIndex = 3, TargetField = ImportField.DebitCreditIndicator },
            ],
            DateFormat = "MM/dd/yyyy",
            AmountMode = AmountParseMode.IndicatorColumn,
            IndicatorSettings = new DebitCreditIndicatorSettingsDto
            {
                ColumnIndex = 3,
                DebitIndicators = "Debit",
                CreditIndicators = "Credit",
                CaseSensitive = false,
            },
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await _service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal(ImportRowStatus.Warning, result.Rows[0].Status);
        Assert.Contains("Unrecognized indicator", result.Rows[0].StatusMessage);
    }

    [Fact]
    public async Task PreviewAsync_WithRowsToSkip_DoesNotDoubleSkipAlreadyParsedRows()
    {
        // Arrange - The CsvParserService has already skipped metadata rows and returned
        // only data rows. RowsToSkip should NOT be applied again by PreviewAsync.
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows =
            [
                ["01/01/2026", "Transaction 1", "-10.00"],
                ["01/02/2026", "Transaction 2", "-20.00"],
                ["01/03/2026", "Transaction 3", "-30.00"],
                ["01/04/2026", "Transaction 4", "-40.00"],
                ["01/05/2026", "Transaction 5", "-50.00"],
                ["01/06/2026", "Transaction 6", "-60.00"],
                ["01/07/2026", "Transaction 7", "-70.00"],
            ],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
            ],
            RowsToSkip = 6,
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await _service.PreviewAsync(request);

        // Assert - All 7 rows should be processed, none double-skipped
        Assert.Equal(7, result.Rows.Count);
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

    private static Account CreateAccount(Guid id, string name, Guid userId)
    {
        var account = Account.CreatePersonal(name, AccountType.Checking, userId);
        typeof(Account).GetProperty("Id")!.SetValue(account, id);
        return account;
    }
}
