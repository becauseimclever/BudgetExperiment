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
    private readonly Mock<IImportMappingRepository> _mappingRepoMock;
    private readonly Mock<IAccountRepository> _accountRepoMock;
    private readonly Mock<IUserContext> _userContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRecurringTransactionRepository> _recurringRepoMock;
    private readonly Mock<IRecurringInstanceProjector> _projectorMock;
    private readonly Mock<ITransactionMatcher> _matcherMock;
    private readonly Mock<IReconciliationService> _reconciliationServiceMock;
    private readonly ImportService _service;

    public ImportServiceTests()
    {
        this._transactionRepoMock = new Mock<ITransactionRepository>();
        this._ruleRepoMock = new Mock<ICategorizationRuleRepository>();
        this._categoryRepoMock = new Mock<IBudgetCategoryRepository>();
        this._batchRepoMock = new Mock<IImportBatchRepository>();
        this._mappingRepoMock = new Mock<IImportMappingRepository>();
        this._accountRepoMock = new Mock<IAccountRepository>();
        this._userContextMock = new Mock<IUserContext>();
        this._unitOfWorkMock = new Mock<IUnitOfWork>();
        this._recurringRepoMock = new Mock<IRecurringTransactionRepository>();
        this._projectorMock = new Mock<IRecurringInstanceProjector>();
        this._matcherMock = new Mock<ITransactionMatcher>();
        this._reconciliationServiceMock = new Mock<IReconciliationService>();

        this._ruleRepoMock
            .Setup(r => r.GetActiveByPriorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategorizationRule>());

        this._categoryRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BudgetCategory>());

        this._transactionRepoMock
            .Setup(r => r.GetForDuplicateDetectionAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction>());

        // Setup default empty returns for recurring transaction matching
        this._recurringRepoMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction>());

        this._service = new ImportService(
            this._transactionRepoMock.Object,
            this._ruleRepoMock.Object,
            this._categoryRepoMock.Object,
            this._batchRepoMock.Object,
            this._mappingRepoMock.Object,
            this._accountRepoMock.Object,
            this._recurringRepoMock.Object,
            this._projectorMock.Object,
            this._matcherMock.Object,
            this._reconciliationServiceMock.Object,
            this._userContextMock.Object,
            this._unitOfWorkMock.Object);
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

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_WithNoUser_ThrowsDomainException()
    {
        // Arrange
        this._userContextMock.Setup(u => u.UserIdAsGuid).Returns((Guid?)null);

        var request = new ImportExecuteRequest
        {
            AccountId = Guid.NewGuid(),
            FileName = "test.csv",
            Transactions = [],
        };

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => this._service.ExecuteAsync(request));
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyTransactions_ReturnsEmptyBatchId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = CreateAccount(accountId, "Test Account", userId);

        this._userContextMock.Setup(u => u.UserIdAsGuid).Returns(userId);
        this._accountRepoMock.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var request = new ImportExecuteRequest
        {
            AccountId = accountId,
            FileName = "test.csv",
            Transactions = [],
        };

        // Act
        var result = await this._service.ExecuteAsync(request);

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
        this._userContextMock.Setup(u => u.UserIdAsGuid).Returns(userId);
        this._accountRepoMock.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
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
        await Assert.ThrowsAsync<DomainException>(() => this._service.ExecuteAsync(request));
    }

    [Fact]
    public async Task ExecuteAsync_CreatesTransactionsAndBatch()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = CreateAccount(accountId, "Test Account", userId);

        this._userContextMock.Setup(u => u.UserIdAsGuid).Returns(userId);
        this._accountRepoMock.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        ImportBatch? savedBatch = null;
        this._batchRepoMock.Setup(r => r.AddAsync(It.IsAny<ImportBatch>(), It.IsAny<CancellationToken>()))
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
        var result = await this._service.ExecuteAsync(request);

        // Assert
        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(2, result.CreatedTransactionIds.Count);
        Assert.Equal(1, result.AutoCategorizedCount);
        Assert.Equal(1, result.UncategorizedCount);
        Assert.NotNull(savedBatch);
        Assert.Equal(ImportBatchStatus.Completed, savedBatch.Status);
        this._transactionRepoMock.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteAsync_TracksCategorySourceStatistics()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = CreateAccount(accountId, "Test Account", userId);

        this._userContextMock.Setup(u => u.UserIdAsGuid).Returns(userId);
        this._accountRepoMock.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
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
        var result = await this._service.ExecuteAsync(request);

        // Assert
        Assert.Equal(4, result.ImportedCount);
        Assert.Equal(2, result.AutoCategorizedCount);
        Assert.Equal(1, result.CsvCategorizedCount);
        Assert.Equal(1, result.UncategorizedCount);
    }

    [Fact]
    public async Task ExecuteAsync_SetsImportBatchOnTransactions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = CreateAccount(accountId, "Test Account", userId);
        var reference = "REF123";

        this._userContextMock.Setup(u => u.UserIdAsGuid).Returns(userId);
        this._accountRepoMock.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        Transaction? savedTransaction = null;
        this._transactionRepoMock.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => savedTransaction = t)
            .Returns(Task.CompletedTask);

        var request = new ImportExecuteRequest
        {
            AccountId = accountId,
            FileName = "test.csv",
            Transactions =
            [
                new ImportTransactionData { Date = new DateOnly(2026, 1, 15), Description = "Test", Amount = -50m, Reference = reference },
            ],
        };

        // Act
        var result = await this._service.ExecuteAsync(request);

        // Assert
        Assert.NotNull(savedTransaction);
        Assert.NotNull(savedTransaction.ImportBatchId);
        Assert.Equal(reference, savedTransaction.ExternalReference);
    }

    #endregion

    #region GetImportHistoryAsync Tests

    [Fact]
    public async Task GetImportHistoryAsync_WithNoUser_ThrowsDomainException()
    {
        // Arrange
        this._userContextMock.Setup(u => u.UserIdAsGuid).Returns((Guid?)null);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => this._service.GetImportHistoryAsync());
    }

    [Fact]
    public async Task GetImportHistoryAsync_ReturnsEmptyList_WhenNoBatches()
    {
        // Arrange
        var userId = Guid.NewGuid();
        this._userContextMock.Setup(u => u.UserIdAsGuid).Returns(userId);
        this._batchRepoMock.Setup(r => r.GetByUserAsync(userId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ImportBatch>());

        // Act
        var result = await this._service.GetImportHistoryAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetImportHistoryAsync_ReturnsBatchesWithAccountAndMappingNames()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var mappingId = Guid.NewGuid();

        var batch = ImportBatch.Create(userId, accountId, "test.csv", 10, mappingId);
        batch.Complete(8, 2, 0);

        var account = CreateAccount(accountId, "Checking Account", userId);
        var mapping = ImportMapping.Create(userId, "Bank CSV", [new ColumnMapping { ColumnIndex = 0, TargetField = ImportField.Date }]);

        // Use reflection to set mapping ID
        typeof(ImportMapping).GetProperty("Id")!.SetValue(mapping, mappingId);

        this._userContextMock.Setup(u => u.UserIdAsGuid).Returns(userId);
        this._batchRepoMock.Setup(r => r.GetByUserAsync(userId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ImportBatch> { batch });
        this._accountRepoMock.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        this._mappingRepoMock.Setup(r => r.GetByIdAsync(mappingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapping);

        // Act
        var result = await this._service.GetImportHistoryAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("test.csv", result[0].FileName);
        Assert.Equal("Checking Account", result[0].AccountName);
        Assert.Equal("Bank CSV", result[0].MappingName);
        Assert.Equal(8, result[0].TransactionCount);
    }

    [Fact]
    public async Task GetImportHistoryAsync_ReturnsUnknownForMissingAccount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var batch = ImportBatch.Create(userId, accountId, "test.csv", 10, null);
        batch.Complete(10, 0, 0);

        this._userContextMock.Setup(u => u.UserIdAsGuid).Returns(userId);
        this._batchRepoMock.Setup(r => r.GetByUserAsync(userId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ImportBatch> { batch });
        this._accountRepoMock.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await this._service.GetImportHistoryAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Unknown", result[0].AccountName);
        Assert.Null(result[0].MappingName);
    }

    #endregion

    #region DeleteImportBatchAsync Tests

    [Fact]
    public async Task DeleteImportBatchAsync_WithNoUser_ThrowsDomainException()
    {
        // Arrange
        this._userContextMock.Setup(u => u.UserIdAsGuid).Returns((Guid?)null);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => this._service.DeleteImportBatchAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteImportBatchAsync_WithNonExistentBatch_ReturnsZero()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var batchId = Guid.NewGuid();
        this._userContextMock.Setup(u => u.UserIdAsGuid).Returns(userId);
        this._batchRepoMock.Setup(r => r.GetByIdAsync(batchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImportBatch?)null);

        // Act
        var result = await this._service.DeleteImportBatchAsync(batchId);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task DeleteImportBatchAsync_WithOtherUsersBatch_ThrowsDomainException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        var batch = ImportBatch.Create(otherUserId, accountId, "test.csv", 10, null);
        typeof(ImportBatch).GetProperty("Id")!.SetValue(batch, batchId);

        this._userContextMock.Setup(u => u.UserIdAsGuid).Returns(userId);
        this._batchRepoMock.Setup(r => r.GetByIdAsync(batchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(batch);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => this._service.DeleteImportBatchAsync(batchId));
    }

    [Fact]
    public async Task DeleteImportBatchAsync_DeletesTransactionsAndMarksBatchDeleted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        var batch = ImportBatch.Create(userId, accountId, "test.csv", 5, null);
        typeof(ImportBatch).GetProperty("Id")!.SetValue(batch, batchId);

        var account = CreateAccount(accountId, "Test Account", userId);
        var tx1 = account.AddTransaction(MoneyValue.Create("USD", -10m), new DateOnly(2026, 1, 15), "Tx1");
        var tx2 = account.AddTransaction(MoneyValue.Create("USD", -20m), new DateOnly(2026, 1, 15), "Tx2");
        var tx3 = account.AddTransaction(MoneyValue.Create("USD", -30m), new DateOnly(2026, 1, 15), "Tx3");

        this._userContextMock.Setup(u => u.UserIdAsGuid).Returns(userId);
        this._batchRepoMock.Setup(r => r.GetByIdAsync(batchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(batch);
        this._transactionRepoMock.Setup(r => r.GetByImportBatchAsync(batchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction> { tx1, tx2, tx3 });

        // Act
        var result = await this._service.DeleteImportBatchAsync(batchId);

        // Assert
        Assert.Equal(3, result);
        Assert.Equal(ImportBatchStatus.Deleted, batch.Status);
        this._transactionRepoMock.Verify(r => r.RemoveAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    #endregion

    #region Reconciliation Integration Tests

    [Fact]
    public async Task ExecuteAsync_WithRunReconciliationTrue_CallsReconciliationService()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        this._userContextMock.Setup(u => u.UserIdAsGuid).Returns(userId);
        this._accountRepoMock.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAccount(accountId, "Test Account", userId));

        var txIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var txIdIndex = 0;
        this._transactionRepoMock.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((tx, _) =>
            {
                typeof(Transaction).GetProperty("Id")!.SetValue(tx, txIds[txIdIndex++]);
            })
            .Returns(Task.CompletedTask);

        var reconciliationResult = new FindMatchesResult
        {
            TotalMatchesFound = 2,
            HighConfidenceCount = 1,
            MatchesByTransaction = new Dictionary<Guid, IReadOnlyList<ReconciliationMatchDto>>(),
        };
        this._reconciliationServiceMock
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
        var result = await this._service.ExecuteAsync(request);

        // Assert
        Assert.Equal(2, result.ReconciliationMatchCount);
        Assert.Equal(1, result.AutoMatchedCount);
        Assert.Equal(1, result.PendingMatchCount);
        this._reconciliationServiceMock.Verify(r => r.FindMatchesAsync(
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

        this._userContextMock.Setup(u => u.UserIdAsGuid).Returns(userId);
        this._accountRepoMock.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
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
        var result = await this._service.ExecuteAsync(request);

        // Assert
        Assert.Equal(0, result.ReconciliationMatchCount);
        this._reconciliationServiceMock.Verify(
            r => r.FindMatchesAsync(It.IsAny<FindMatchesRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Skip Rows Tests

    [Fact]
    public async Task PreviewAsync_WithSkipRows_SkipsFirstNRows()
    {
        // Arrange
        var request = new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows =
            [
                ["Account: 12345", "", ""],  // Metadata row 1 (should be skipped)
                ["Date Range: 01/01/2026", "", ""],  // Metadata row 2 (should be skipped)
                ["01/15/2026", "Actual Transaction", "-50.00"],  // Real data row
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
        var result = await this._service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal("Actual Transaction", result.Rows[0].Description);
        Assert.Equal(-50.00m, result.Rows[0].Amount);
        Assert.Equal(3, result.Rows[0].RowIndex); // Row index accounts for skipped rows (1-based)
    }

    [Fact]
    public async Task PreviewAsync_WithSkipRowsExceedingTotal_ReturnsEmptyResult()
    {
        // Arrange
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
            RowsToSkip = 5, // Skip more rows than available
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
        };

        // Act
        var result = await this._service.PreviewAsync(request);

        // Assert
        Assert.Empty(result.Rows);
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
        var result = await this._service.PreviewAsync(request);

        // Assert
        Assert.Equal(2, result.Rows.Count);
    }

    #endregion

    #region Indicator Column Tests

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
        var result = await this._service.PreviewAsync(request);

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
        var result = await this._service.PreviewAsync(request);

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
        var result = await this._service.PreviewAsync(request);

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
        var result = await this._service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal(ImportRowStatus.Warning, result.Rows[0].Status);
        Assert.Contains("Unrecognized indicator", result.Rows[0].StatusMessage);
    }

    #endregion

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
