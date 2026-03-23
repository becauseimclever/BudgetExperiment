// <copyright file="ImportServiceLocationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Import;
using BudgetExperiment.Application.Recurring;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Settings;

using Moq;

namespace BudgetExperiment.Application.Tests.Services;

/// <summary>
/// Unit tests for location enrichment during CSV import (VS-9).
/// </summary>
public class ImportServiceLocationTests
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
    private readonly Mock<ILocationParserService> _locationParserMock;
    private readonly Mock<IAppSettingsRepository> _settingsRepoMock;
    private readonly Mock<ICurrencyProvider> _currencyProviderMock;
    private readonly ImportService _service;

    public ImportServiceLocationTests()
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
        _locationParserMock = new Mock<ILocationParserService>();
        _settingsRepoMock = new Mock<IAppSettingsRepository>();
        _currencyProviderMock = new Mock<ICurrencyProvider>();
        _currencyProviderMock.Setup(c => c.GetCurrencyAsync(It.IsAny<CancellationToken>())).ReturnsAsync("USD");

        _ruleRepoMock
            .Setup(r => r.GetActiveByPriorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategorizationRule>());

        _categoryRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BudgetCategory>());

        _transactionRepoMock
            .Setup(r => r.GetForDuplicateDetectionAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction>());

        // Use real ImportPreviewEnricher so preview enrichment tests work end-to-end
        var previewEnricher = new ImportPreviewEnricher(
            new Mock<IRecurringTransactionRepository>().Object,
            new Mock<IRecurringInstanceProjector>().Object,
            new Mock<ITransactionMatcher>().Object,
            _locationParserMock.Object,
            _settingsRepoMock.Object,
            _currencyProviderMock.Object);

        _service = new ImportService(
            new ImportRowProcessor(new ImportDuplicateDetector()),
            previewEnricher,
            new Mock<IImportBatchManager>().Object,
            new Mock<IImportTransactionCreator>().Object,
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
    public async Task PreviewAsync_WhenLocationEnabled_ParsesDescriptions()
    {
        // Arrange
        SetupLocationEnabled(true);

        var parsedLocation = TransactionLocationValue.CreateFromParsed("Seattle", "WA");
        _locationParserMock
            .Setup(p => p.ParseBatch(It.IsAny<IEnumerable<string>>()))
            .Returns(new List<LocationParseResult>
            {
                new LocationParseResult
                {
                    OriginalText = "AMAZON.COM SEATTLE WA",
                    Location = parsedLocation,
                    Confidence = 0.90m,
                    MatchedPattern = "CitySpaceStateZip",
                },
            });

        var request = CreatePreviewRequest("AMAZON.COM SEATTLE WA");

        // Act
        var result = await _service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        var row = result.Rows[0];
        Assert.NotNull(row.ParsedLocation);
        Assert.Equal("Seattle", row.ParsedLocation!.City);
        Assert.Equal("WA", row.ParsedLocation.StateOrRegion);
        Assert.Equal(0.90m, row.ParsedLocation.Confidence);
        Assert.Equal(1, result.LocationEnrichedCount);
    }

    [Fact]
    public async Task PreviewAsync_WhenLocationDisabled_SkipsParsing()
    {
        // Arrange
        SetupLocationEnabled(false);

        var request = CreatePreviewRequest("AMAZON.COM SEATTLE WA");

        // Act
        var result = await _service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Null(result.Rows[0].ParsedLocation);
        Assert.Equal(0, result.LocationEnrichedCount);

        _locationParserMock.Verify(
            p => p.ParseBatch(It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task PreviewAsync_WhenParserReturnsNull_RowHasNullLocation()
    {
        // Arrange
        SetupLocationEnabled(true);

        _locationParserMock
            .Setup(p => p.ParseBatch(It.IsAny<IEnumerable<string>>()))
            .Returns(new List<LocationParseResult>
            {
                new LocationParseResult
                {
                    OriginalText = "ONLINE PURCHASE",
                    Location = null,
                    Confidence = 0m,
                },
            });

        var request = CreatePreviewRequest("ONLINE PURCHASE");

        // Act
        var result = await _service.PreviewAsync(request);

        // Assert
        Assert.Single(result.Rows);
        Assert.Null(result.Rows[0].ParsedLocation);
        Assert.Equal(0, result.LocationEnrichedCount);
    }

    [Fact]
    public async Task PreviewAsync_MultiplRows_EnrichesOnlyMatched()
    {
        // Arrange
        SetupLocationEnabled(true);

        var parsedLocation = TransactionLocationValue.CreateFromParsed("Portland", "OR");
        _locationParserMock
            .Setup(p => p.ParseBatch(It.IsAny<IEnumerable<string>>()))
            .Returns(new List<LocationParseResult>
            {
                new LocationParseResult
                {
                    OriginalText = "COFFEE SHOP PORTLAND OR",
                    Location = parsedLocation,
                    Confidence = 0.85m,
                    MatchedPattern = "CityCommaState",
                },
                new LocationParseResult
                {
                    OriginalText = "ONLINE SUBSCRIPTION",
                    Location = null,
                    Confidence = 0m,
                },
            });

        var request = CreatePreviewRequest("COFFEE SHOP PORTLAND OR", "ONLINE SUBSCRIPTION");

        // Act
        var result = await _service.PreviewAsync(request);

        // Assert
        Assert.Equal(2, result.Rows.Count);
        Assert.NotNull(result.Rows[0].ParsedLocation);
        Assert.Null(result.Rows[1].ParsedLocation);
        Assert.Equal(1, result.LocationEnrichedCount);
    }

    private static ImportPreviewRequest CreatePreviewRequest(
        params string[] descriptions)
    {
        var rows = descriptions.Select(d =>
            (IReadOnlyList<string>)new List<string> { "01/15/2026", d, "-50.00" }).ToList();

        return new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows = rows,
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
            ],
            DateFormat = "MM/dd/yyyy",
            DuplicateSettings = new DuplicateDetectionSettingsDto { Enabled = false },
            CheckRecurringMatches = false,
        };
    }

    private void SetupLocationEnabled(bool enabled)
    {
        var settings = AppSettings.CreateDefault();
        settings.UpdateEnableLocationData(enabled);
        _settingsRepoMock
            .Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);
    }
}
