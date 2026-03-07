// <copyright file="ImportPreviewEnricherTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Common;
using Moq;
using Shouldly;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for <see cref="ImportPreviewEnricher"/>.
/// </summary>
public class ImportPreviewEnricherTests
{
    private readonly Mock<IRecurringTransactionRepository> _recurringRepository;
    private readonly Mock<IRecurringInstanceProjector> _instanceProjector;
    private readonly Mock<ITransactionMatcher> _transactionMatcher;
    private readonly Mock<ILocationParserService> _locationParser;
    private readonly Mock<IAppSettingsRepository> _settingsRepository;
    private readonly Mock<ICurrencyProvider> _currencyProvider;

    public ImportPreviewEnricherTests()
    {
        _recurringRepository = new Mock<IRecurringTransactionRepository>();
        _instanceProjector = new Mock<IRecurringInstanceProjector>();
        _transactionMatcher = new Mock<ITransactionMatcher>();
        _locationParser = new Mock<ILocationParserService>();
        _settingsRepository = new Mock<IAppSettingsRepository>();
        _currencyProvider = new Mock<ICurrencyProvider>();

        _currencyProvider
            .Setup(c => c.GetCurrencyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("USD");
    }

    [Fact]
    public async Task EnrichWithRecurringMatchesAsync_NoValidRows_ReturnsOriginalRows()
    {
        var rows = new List<ImportPreviewRow>
        {
            new ImportPreviewRow { RowIndex = 0, Description = "Test", Date = null, Amount = null },
        };

        var enricher = CreateEnricher();

        var result = await enricher.EnrichWithRecurringMatchesAsync(rows);

        result.ShouldBeSameAs(rows);
    }

    [Fact]
    public async Task EnrichWithRecurringMatchesAsync_NoActiveRecurringTransactions_ReturnsOriginalRows()
    {
        var rows = new List<ImportPreviewRow>
        {
            new ImportPreviewRow { RowIndex = 0, Description = "Netflix", Date = new DateOnly(2026, 1, 15), Amount = -15.99m },
        };

        _recurringRepository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction>());

        var enricher = CreateEnricher();

        var result = await enricher.EnrichWithRecurringMatchesAsync(rows);

        result.ShouldBeSameAs(rows);
    }

    [Fact]
    public async Task EnrichWithRecurringMatchesAsync_NoCandidates_ReturnsOriginalRows()
    {
        var rows = new List<ImportPreviewRow>
        {
            new ImportPreviewRow { RowIndex = 0, Description = "Netflix", Date = new DateOnly(2026, 1, 15), Amount = -15.99m },
        };

        var recurring = RecurringTransaction.Create(
            Guid.NewGuid(),
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            RecurrencePatternValue.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));

        _recurringRepository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });

        _instanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfoValue>>());

        var enricher = CreateEnricher();

        var result = await enricher.EnrichWithRecurringMatchesAsync(rows);

        result.ShouldBeSameAs(rows);
    }

    [Fact]
    public async Task EnrichWithRecurringMatchesAsync_CalculatesDateRangeWithBuffer()
    {
        var rows = new List<ImportPreviewRow>
        {
            new ImportPreviewRow { RowIndex = 0, Description = "Netflix", Date = new DateOnly(2026, 3, 10), Amount = -15.99m },
            new ImportPreviewRow { RowIndex = 1, Description = "Spotify", Date = new DateOnly(2026, 3, 20), Amount = -9.99m },
        };

        var recurring = RecurringTransaction.Create(
            Guid.NewGuid(),
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            RecurrencePatternValue.CreateMonthly(1, 10),
            new DateOnly(2026, 1, 10));

        _recurringRepository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });

        DateOnly capturedFrom = default;
        DateOnly capturedTo = default;

        _instanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<RecurringTransaction>, DateOnly, DateOnly, CancellationToken>(
                (_, from, to, _) =>
                {
                    capturedFrom = from;
                    capturedTo = to;
                })
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfoValue>>());

        var enricher = CreateEnricher();
        await enricher.EnrichWithRecurringMatchesAsync(rows);

        capturedFrom.ShouldBe(new DateOnly(2026, 3, 3));  // min(10,20) - 7
        capturedTo.ShouldBe(new DateOnly(2026, 3, 27));    // max(10,20) + 7
    }

    [Fact]
    public async Task EnrichWithRecurringMatchesAsync_WithMatch_AddsRecurringMatchPreview()
    {
        var recurringId = Guid.NewGuid();
        var date = new DateOnly(2026, 1, 15);

        var rows = new List<ImportPreviewRow>
        {
            new ImportPreviewRow { RowIndex = 0, Description = "Netflix", Date = date, Amount = -15.99m },
        };

        var recurring = RecurringTransaction.Create(
            Guid.NewGuid(),
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            RecurrencePatternValue.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));

        _recurringRepository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });

        var candidate = new RecurringInstanceInfoValue(
            RecurringTransactionId: recurringId,
            InstanceDate: date,
            AccountId: Guid.NewGuid(),
            AccountName: "Checking",
            Description: "Netflix",
            Amount: MoneyValue.Create("USD", -15.99m),
            CategoryId: null,
            CategoryName: null,
            IsModified: false,
            IsSkipped: false);

        _instanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfoValue>>
            {
                [date] = new List<RecurringInstanceInfoValue> { candidate },
            });

        var matchResult = new TransactionMatchResultValue(
            recurringId, date, 0.90m, MatchConfidenceLevel.High, 0m, 0, 0.95m);

        _transactionMatcher
            .Setup(m => m.CalculateMatch(
                It.IsAny<Transaction>(),
                It.Is<RecurringInstanceInfoValue>(c => c.RecurringTransactionId == recurringId),
                It.IsAny<MatchingTolerancesValue>()))
            .Returns(matchResult);

        var enricher = CreateEnricher();

        var result = await enricher.EnrichWithRecurringMatchesAsync(rows);

        result.Count.ShouldBe(1);
        var match = result[0].RecurringMatch;
        match.ShouldNotBeNull();
        match!.RecurringTransactionId.ShouldBe(recurringId);
        match.ConfidenceScore.ShouldBe(0.90m);
    }

    [Fact]
    public async Task EnrichWithRecurringMatchesAsync_RowWithoutDateOrAmount_PassedThroughUnchanged()
    {
        var rows = new List<ImportPreviewRow>
        {
            new ImportPreviewRow { RowIndex = 0, Description = "Header Row", Date = null, Amount = null },
            new ImportPreviewRow { RowIndex = 1, Description = "Netflix", Date = new DateOnly(2026, 1, 15), Amount = -15.99m },
        };

        var recurring = RecurringTransaction.Create(
            Guid.NewGuid(),
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            RecurrencePatternValue.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));

        _recurringRepository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });

        _instanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfoValue>>
            {
                [new DateOnly(2026, 1, 15)] = new List<RecurringInstanceInfoValue>
                {
                    new RecurringInstanceInfoValue(
                        RecurringTransactionId: Guid.NewGuid(),
                        InstanceDate: new DateOnly(2026, 1, 15),
                        AccountId: Guid.NewGuid(),
                        AccountName: "Checking",
                        Description: "Netflix",
                        Amount: MoneyValue.Create("USD", -15.99m),
                        CategoryId: null,
                        CategoryName: null,
                        IsModified: false,
                        IsSkipped: false),
                },
            });

        _transactionMatcher
            .Setup(m => m.CalculateMatch(
                It.IsAny<Transaction>(),
                It.IsAny<RecurringInstanceInfoValue>(),
                It.IsAny<MatchingTolerancesValue>()))
            .Returns((TransactionMatchResultValue?)null);

        var enricher = CreateEnricher();

        var result = await enricher.EnrichWithRecurringMatchesAsync(rows);

        result.Count.ShouldBe(2);
        result[0].RecurringMatch.ShouldBeNull();
    }

    [Fact]
    public async Task EnrichWithLocationDataAsync_LocationDisabled_ReturnsOriginalRows()
    {
        var settings = AppSettings.CreateDefault();
        _settingsRepository
            .Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var rows = new List<ImportPreviewRow>
        {
            new ImportPreviewRow { RowIndex = 0, Description = "STARBUCKS SEATTLE WA" },
        };

        var enricher = CreateEnricher();

        var result = await enricher.EnrichWithLocationDataAsync(rows);

        result.ShouldBeSameAs(rows);
        _locationParser.Verify(
            p => p.ParseBatch(It.IsAny<IEnumerable<string>>()),
            Times.Never);
    }

    [Fact]
    public async Task EnrichWithLocationDataAsync_WithParsedLocation_EnrichesRow()
    {
        var settings = AppSettings.CreateDefault();
        EnableLocationData(settings);

        _settingsRepository
            .Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var rows = new List<ImportPreviewRow>
        {
            new ImportPreviewRow { RowIndex = 0, Description = "STARBUCKS SEATTLE WA" },
        };

        var location = TransactionLocationValue.CreateFromParsed("SEATTLE", "WA");
        _locationParser
            .Setup(p => p.ParseBatch(It.IsAny<IEnumerable<string>>()))
            .Returns(new List<LocationParseResult>
            {
                new LocationParseResult
                {
                    OriginalText = "STARBUCKS SEATTLE WA",
                    Location = location,
                    Confidence = 0.85m,
                },
            });

        var enricher = CreateEnricher();

        var result = await enricher.EnrichWithLocationDataAsync(rows);

        result.Count.ShouldBe(1);
        var loc = result[0].ParsedLocation;
        loc.ShouldNotBeNull();
        loc!.City.ShouldBe("SEATTLE");
        loc.StateOrRegion.ShouldBe("WA");
        loc.Confidence.ShouldBe(0.85m);
    }

    [Fact]
    public async Task EnrichWithLocationDataAsync_NullLocation_RowPassedThrough()
    {
        var settings = AppSettings.CreateDefault();
        EnableLocationData(settings);

        _settingsRepository
            .Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var rows = new List<ImportPreviewRow>
        {
            new ImportPreviewRow { RowIndex = 0, Description = "RANDOM PURCHASE" },
        };

        _locationParser
            .Setup(p => p.ParseBatch(It.IsAny<IEnumerable<string>>()))
            .Returns(new List<LocationParseResult>
            {
                new LocationParseResult
                {
                    OriginalText = "RANDOM PURCHASE",
                    Location = null,
                    Confidence = 0m,
                },
            });

        var enricher = CreateEnricher();

        var result = await enricher.EnrichWithLocationDataAsync(rows);

        result.Count.ShouldBe(1);
        result[0].ParsedLocation.ShouldBeNull();
    }

    private static void EnableLocationData(AppSettings settings)
    {
        settings.UpdateEnableLocationData(true);
    }

    private ImportPreviewEnricher CreateEnricher()
    {
        return new ImportPreviewEnricher(
            _recurringRepository.Object,
            _instanceProjector.Object,
            _transactionMatcher.Object,
            _locationParser.Object,
            _settingsRepository.Object,
            _currencyProvider.Object);
    }
}
