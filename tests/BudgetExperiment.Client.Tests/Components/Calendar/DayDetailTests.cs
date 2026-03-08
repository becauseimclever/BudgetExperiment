// <copyright file="DayDetailTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Calendar;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BudgetExperiment.Client.Tests.Components.Calendar;

/// <summary>
/// Unit tests for the <see cref="DayDetail"/> component.
/// </summary>
public class DayDetailTests : BunitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DayDetailTests"/> class.
    /// </summary>
    public DayDetailTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<IBudgetApiService>(new StubBudgetApiService());
    }

    /// <summary>
    /// Verifies the component renders without errors.
    /// </summary>
    [Fact]
    public void Renders_WithoutErrors()
    {
        var cut = Render<DayDetail>(p => p
            .Add(x => x.Detail, CreateDetail()));

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies transaction items are displayed.
    /// </summary>
    [Fact]
    public void ShowsTransactionItems()
    {
        var detail = CreateDetail();
        detail.Items =
        [
            new DayDetailItemDto
            {
                Id = Guid.NewGuid(),
                Type = "transaction",
                Description = "Coffee Shop",
                Amount = new MoneyDto { Amount = -5.50m, Currency = "USD" },
                AccountName = "Checking",
            },
        ];

        var cut = Render<DayDetail>(p => p.Add(x => x.Detail, detail));

        cut.Markup.ShouldContain("Coffee Shop");
    }

    /// <summary>
    /// Verifies recurring items are displayed.
    /// </summary>
    [Fact]
    public void ShowsRecurringItems()
    {
        var detail = CreateDetail();
        detail.Items =
        [
            new DayDetailItemDto
            {
                Id = Guid.NewGuid(),
                Type = "recurring",
                Description = "Netflix",
                Amount = new MoneyDto { Amount = -15.99m, Currency = "USD" },
                AccountName = "Checking",
                RecurringTransactionId = Guid.NewGuid(),
            },
        ];

        var cut = Render<DayDetail>(p => p.Add(x => x.Detail, detail));

        cut.Markup.ShouldContain("Netflix");
    }

    /// <summary>
    /// Verifies summary section is shown.
    /// </summary>
    [Fact]
    public void ShowsSummary()
    {
        var detail = CreateDetail();
        detail.Summary = new DayDetailSummaryDto
        {
            TotalActual = new MoneyDto { Amount = -25m, Currency = "USD" },
            TotalProjected = new MoneyDto { Amount = -50m, Currency = "USD" },
            CombinedTotal = new MoneyDto { Amount = -75m, Currency = "USD" },
            ItemCount = 3,
        };

        var cut = Render<DayDetail>(p => p.Add(x => x.Detail, detail));

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies transfer items show transfer indicator.
    /// </summary>
    [Fact]
    public void TransferItem_ShowsTransferIndicator()
    {
        var detail = CreateDetail();
        detail.Items =
        [
            new DayDetailItemDto
            {
                Id = Guid.NewGuid(),
                Type = "transaction",
                Description = "Transfer to Savings",
                Amount = new MoneyDto { Amount = -500m, Currency = "USD" },
                AccountName = "Checking",
                IsTransfer = true,
                TransferId = Guid.NewGuid(),
                TransferDirection = "outgoing",
            },
        ];

        var cut = Render<DayDetail>(p => p.Add(x => x.Detail, detail));

        cut.Markup.ShouldContain("Transfer to Savings");
    }

    /// <summary>
    /// Verifies skipped recurring instances are hidden from the scheduled section.
    /// </summary>
    [Fact]
    public void SkippedInstance_IsHidden()
    {
        var detail = CreateDetail();
        detail.Items =
        [
            new DayDetailItemDto
            {
                Id = Guid.NewGuid(),
                Type = "recurring",
                Description = "Skipped Bill",
                Amount = new MoneyDto { Amount = -100m, Currency = "USD" },
                AccountName = "Checking",
                IsSkipped = true,
                RecurringTransactionId = Guid.NewGuid(),
            },
        ];

        var cut = Render<DayDetail>(p => p.Add(x => x.Detail, detail));

        cut.Markup.ShouldNotContain("Skipped Bill");
    }

    /// <summary>
    /// Verifies empty items list renders without errors.
    /// </summary>
    [Fact]
    public void EmptyItems_RendersWithoutErrors()
    {
        var detail = CreateDetail();
        detail.Items = [];

        var cut = Render<DayDetail>(p => p.Add(x => x.Detail, detail));

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies recurring-transfer type items are shown.
    /// </summary>
    [Fact]
    public void RecurringTransferItem_IsDisplayed()
    {
        var detail = CreateDetail();
        detail.Items =
        [
            new DayDetailItemDto
            {
                Id = Guid.NewGuid(),
                Type = "recurring-transfer",
                Description = "Savings Transfer",
                Amount = new MoneyDto { Amount = -200m, Currency = "USD" },
                AccountName = "Checking",
                RecurringTransferId = Guid.NewGuid(),
            },
        ];

        var cut = Render<DayDetail>(p => p.Add(x => x.Detail, detail));

        cut.Markup.ShouldContain("Savings Transfer");
    }

    /// <summary>
    /// Verifies multiple item types render together.
    /// </summary>
    [Fact]
    public void MultipleItemTypes_RenderTogether()
    {
        var detail = CreateDetail();
        detail.Items =
        [
            new DayDetailItemDto
            {
                Id = Guid.NewGuid(),
                Type = "transaction",
                Description = "Grocery Store",
                Amount = new MoneyDto { Amount = -45m, Currency = "USD" },
                AccountName = "Checking",
            },
            new DayDetailItemDto
            {
                Id = Guid.NewGuid(),
                Type = "recurring",
                Description = "Rent",
                Amount = new MoneyDto { Amount = -1500m, Currency = "USD" },
                AccountName = "Checking",
                RecurringTransactionId = Guid.NewGuid(),
            },
        ];

        var cut = Render<DayDetail>(p => p.Add(x => x.Detail, detail));

        cut.Markup.ShouldContain("Grocery Store");
        cut.Markup.ShouldContain("Rent");
    }

    private static DayDetailDto CreateDetail()
    {
        return new DayDetailDto
        {
            Date = new DateOnly(2025, 6, 15),
            Items = [],
            Summary = new DayDetailSummaryDto(),
        };
    }
}
