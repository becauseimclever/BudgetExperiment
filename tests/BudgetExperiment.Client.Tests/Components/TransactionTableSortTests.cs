// <copyright file="TransactionTableSortTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Display;
using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for sortable column headers in <see cref="TransactionTable"/>.
/// </summary>
public sealed class TransactionTableSortTests : BunitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionTableSortTests"/> class.
    /// </summary>
    public TransactionTableSortTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
    }

    [Fact]
    public void Render_DefaultSort_DateDescending()
    {
        // Arrange & Act
        var cut = RenderTable(CreateTestItems());

        // Assert — first row should be the most recent date (Jan 20)
        var rows = cut.FindAll("tbody tr");
        Assert.Equal(3, rows.Count);
        Assert.Contains("Jan 20, 2026", rows[0].TextContent);
        Assert.Contains("Jan 5, 2026", rows[2].TextContent);
    }

    [Fact]
    public void ClickDateHeader_TogglesSort_ToAscending()
    {
        // Arrange
        var cut = RenderTable(CreateTestItems());
        var dateHeader = cut.Find("th.sortable.sorted");

        // Act — click to toggle from descending to ascending
        dateHeader.Click();

        // Assert — first row should now be the earliest date (Jan 5)
        var rows = cut.FindAll("tbody tr");
        Assert.Contains("Jan 5, 2026", rows[0].TextContent);
        Assert.Contains("Jan 20, 2026", rows[2].TextContent);
    }

    [Fact]
    public void ClickAmountHeader_SortsAscending_ThenTogglesToDescending()
    {
        // Arrange
        var cut = RenderTable(CreateTestItems());
        var amountHeader = cut.FindAll("th.sortable")[2]; // Date, Description, Amount

        // Act — click Amount header (first click = ascending)
        amountHeader.Click();

        // Assert — sorted by amount ascending: -200, -50, 1000
        var amounts = cut.FindAll("tbody tr td.text-right");
        Assert.Contains("($200.00)", amounts[0].TextContent);
        Assert.Contains("($50.00)", amounts[1].TextContent);
        Assert.Contains("$1,000.00", amounts[2].TextContent);

        // Act — click again to toggle descending
        amountHeader.Click();

        // Assert — sorted by amount descending: 1000, -50, -200
        amounts = cut.FindAll("tbody tr td.text-right");
        Assert.Contains("$1,000.00", amounts[0].TextContent);
        Assert.Contains("($50.00)", amounts[1].TextContent);
        Assert.Contains("($200.00)", amounts[2].TextContent);
    }

    [Fact]
    public void ClickDescriptionHeader_SortsAlphabetically()
    {
        // Arrange
        var cut = RenderTable(CreateTestItems());
        var descriptionHeader = cut.FindAll("th.sortable")[1]; // Date, Description

        // Act — click Description header (first click = ascending)
        descriptionHeader.Click();

        // Assert — sorted alphabetically: Coffee, Groceries, Paycheck
        var rows = cut.FindAll("tbody tr");
        Assert.Contains("Coffee", rows[0].TextContent);
        Assert.Contains("Groceries", rows[1].TextContent);
        Assert.Contains("Paycheck", rows[2].TextContent);
    }

    [Fact]
    public void SortArrow_ShowsOnActiveColumn_Only()
    {
        // Arrange
        var cut = RenderTable(CreateTestItems());

        // Assert — Date header has sort arrow (default), others empty
        var headers = cut.FindAll("th.sortable");
        var dateArrow = headers[0].QuerySelector(".sort-arrow");
        var descArrow = headers[1].QuerySelector(".sort-arrow");

        Assert.Equal("\u25BC", dateArrow?.TextContent.Trim()); // ▼ for descending
        Assert.Equal(string.Empty, descArrow?.TextContent.Trim());
    }

    [Fact]
    public void SortArrow_ChangesDirection_OnToggle()
    {
        // Arrange
        var cut = RenderTable(CreateTestItems());
        var dateHeader = cut.Find("th.sortable.sorted");

        // Act — toggle to ascending
        dateHeader.Click();

        // Assert
        var arrow = dateHeader.QuerySelector(".sort-arrow");
        Assert.Equal("\u25B2", arrow?.TextContent.Trim()); // ▲ for ascending
    }

    [Fact]
    public void AriaSortAttribute_ReflectsCurrentSort()
    {
        // Arrange
        var cut = RenderTable(CreateTestItems());
        var headers = cut.FindAll("th.sortable");

        // Assert — Date is descending, others are none
        Assert.Equal("descending", headers[0].GetAttribute("aria-sort"));
        Assert.Equal("none", headers[1].GetAttribute("aria-sort"));

        // Act — click Description
        headers[1].Click();

        // Assert — Description is ascending, Date is none
        headers = cut.FindAll("th.sortable");
        Assert.Equal("none", headers[0].GetAttribute("aria-sort"));
        Assert.Equal("ascending", headers[1].GetAttribute("aria-sort"));
    }

    [Fact]
    public void SortByBalance_WhenBalanceColumnVisible()
    {
        // Arrange
        var items = CreateTestItems();
        var cut = RenderTableWithBalance(items);

        // Find Balance header (last sortable header)
        var headers = cut.FindAll("th.sortable");
        var balanceHeader = headers[^1]; // last sortable = Balance

        // Act — click Balance
        balanceHeader.Click();

        // Assert — ascending by running balance: 450 (Groceries), 950 (Coffee), 1450 (Paycheck)
        var balanceCells = cut.FindAll("td.running-balance");
        Assert.Equal(3, balanceCells.Count);

        // First should be smallest balance
        Assert.Contains("$450.00", balanceCells[0].TextContent);
        Assert.Contains("$1,450.00", balanceCells[2].TextContent);
    }

    [Fact]
    public void RunningBalance_Values_NotAffected_BySortOrder()
    {
        // Arrange
        var items = CreateTestItems();
        var cut = RenderTableWithBalance(items);

        // Collect all balance values in default (date desc) order
        var balancesBefore = cut.FindAll("td.running-balance")
            .Select(td => td.TextContent.Trim())
            .ToList();

        // Act — sort by Amount ascending
        var amountHeader = cut.FindAll("th.sortable")[2]; // Date, Description, Amount
        amountHeader.Click();

        // Collect all balance values — same set of values (just reordered)
        var balancesAfter = cut.FindAll("td.running-balance")
            .Select(td => td.TextContent.Trim())
            .ToList();

        // Assert — exact same values, just possibly in different order
        Assert.Equal(balancesBefore.OrderBy(b => b), balancesAfter.OrderBy(b => b));
    }

    [Fact]
    public void EmptyList_ShowsEmptyMessage_NoHeaders()
    {
        // Arrange & Act
        var cut = Render<TransactionTable>(parameters => parameters
            .Add(p => p.Items, new List<TransactionListItem>())
            .Add(p => p.ShowDate, true));

        // Assert
        Assert.Contains("No transactions found", cut.Markup);
        Assert.Empty(cut.FindAll("th"));
    }

    private static List<TransactionListItem> CreateTestItems()
    {
        return new List<TransactionListItem>
        {
            new TransactionListItem
            {
                Id = Guid.NewGuid(),
                Date = new DateOnly(2026, 1, 10),
                Description = "Groceries",
                Amount = new MoneyDto { Currency = "USD", Amount = -200m },
                RunningBalance = new MoneyDto { Currency = "USD", Amount = 450m },
                CreatedAt = new DateTime(2026, 1, 10, 10, 0, 0, DateTimeKind.Utc),
            },
            new TransactionListItem
            {
                Id = Guid.NewGuid(),
                Date = new DateOnly(2026, 1, 20),
                Description = "Paycheck",
                Amount = new MoneyDto { Currency = "USD", Amount = 1000m },
                RunningBalance = new MoneyDto { Currency = "USD", Amount = 1450m },
                CreatedAt = new DateTime(2026, 1, 20, 10, 0, 0, DateTimeKind.Utc),
            },
            new TransactionListItem
            {
                Id = Guid.NewGuid(),
                Date = new DateOnly(2026, 1, 5),
                Description = "Coffee",
                Amount = new MoneyDto { Currency = "USD", Amount = -50m },
                RunningBalance = new MoneyDto { Currency = "USD", Amount = 950m },
                CreatedAt = new DateTime(2026, 1, 5, 10, 0, 0, DateTimeKind.Utc),
            },
        };
    }

    private IRenderedComponent<TransactionTable> RenderTable(List<TransactionListItem> items)
    {
        return Render<TransactionTable>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.ShowDate, true));
    }

    private IRenderedComponent<TransactionTable> RenderTableWithBalance(List<TransactionListItem> items)
    {
        return Render<TransactionTable>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.ShowDate, true)
            .Add(p => p.ShowBalance, true));
    }
}
