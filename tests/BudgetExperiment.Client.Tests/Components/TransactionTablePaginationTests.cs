// <copyright file="TransactionTablePaginationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Display;
using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for client-side pagination in <see cref="TransactionTable"/>.
/// </summary>
public sealed class TransactionTablePaginationTests : BunitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionTablePaginationTests"/> class.
    /// </summary>
    public TransactionTablePaginationTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
    }

    [Fact]
    public void NoPaginationBar_WhenItemsWithinPageSize()
    {
        // Arrange & Act
        var cut = RenderTable(CreateItems(10));

        // Assert — no pagination bar when all items fit in default page size (50)
        Assert.Empty(cut.FindAll(".pagination-bar"));
    }

    [Fact]
    public void PaginationBar_Appears_WhenItemsExceedPageSize()
    {
        // Arrange & Act — render with 60 items (exceeds default 50)
        var cut = RenderTable(CreateItems(60));

        // Assert — pagination bar should be visible
        Assert.Single(cut.FindAll(".pagination-bar"));
    }

    [Fact]
    public void ShowsCorrectItemRange_OnFirstPage()
    {
        // Arrange & Act
        var cut = RenderTable(CreateItems(75));

        // Assert — "Showing 1–50 of 75"
        var paginationInfo = cut.Find(".pagination-info");
        Assert.Contains("1", paginationInfo.TextContent);
        Assert.Contains("50", paginationInfo.TextContent);
        Assert.Contains("75", paginationInfo.TextContent);
    }

    [Fact]
    public void ShowsCorrectItemRange_OnLastPage()
    {
        // Arrange
        var cut = RenderTable(CreateItems(75));

        // Act — navigate to page 2
        var nextButton = cut.Find(".pagination-next");
        nextButton.Click();

        // Assert — "Showing 51–75 of 75"
        var paginationInfo = cut.Find(".pagination-info");
        Assert.Contains("51", paginationInfo.TextContent);
        Assert.Contains("75", paginationInfo.TextContent);
    }

    [Fact]
    public void FirstPage_ShowsCorrectNumberOfRows()
    {
        // Arrange & Act
        var cut = RenderTable(CreateItems(75));

        // Assert — 50 rows on first page
        var rows = cut.FindAll("tbody tr");
        Assert.Equal(50, rows.Count);
    }

    [Fact]
    public void SecondPage_ShowsRemainingRows()
    {
        // Arrange
        var cut = RenderTable(CreateItems(75));

        // Act — navigate to page 2
        var nextButton = cut.Find(".pagination-next");
        nextButton.Click();

        // Assert — 25 rows on second page
        var rows = cut.FindAll("tbody tr");
        Assert.Equal(25, rows.Count);
    }

    [Fact]
    public void PreviousButton_Disabled_OnFirstPage()
    {
        // Arrange & Act
        var cut = RenderTable(CreateItems(60));

        // Assert
        var prevButton = cut.Find(".pagination-prev");
        Assert.True(prevButton.HasAttribute("disabled"));
    }

    [Fact]
    public void NextButton_Disabled_OnLastPage()
    {
        // Arrange
        var cut = RenderTable(CreateItems(60));

        // Act — navigate to last page
        var nextButton = cut.Find(".pagination-next");
        nextButton.Click();

        // Assert
        Assert.True(nextButton.HasAttribute("disabled"));
    }

    [Fact]
    public void ClickPageNumber_NavigatesToPage()
    {
        // Arrange
        var cut = RenderTable(CreateItems(150));

        // Act — click page 3
        var pageButtons = cut.FindAll(".pagination-page");
        var page3Button = pageButtons.First(b => b.TextContent.Trim() == "3");
        page3Button.Click();

        // Assert — showing items 101–150
        var paginationInfo = cut.Find(".pagination-info");
        Assert.Contains("101", paginationInfo.TextContent);
        Assert.Contains("150", paginationInfo.TextContent);
    }

    [Fact]
    public void ActivePage_HasActiveClass()
    {
        // Arrange & Act
        var cut = RenderTable(CreateItems(150));

        // Assert — page 1 is active
        var activeButtons = cut.FindAll(".pagination-page.active");
        Assert.Single(activeButtons);
        Assert.Equal("1", activeButtons[0].TextContent.Trim());
    }

    [Fact]
    public void ChangePageSize_UpdatesDisplayedRows()
    {
        // Arrange
        var cut = RenderTable(CreateItems(75));

        // Act — change page size to 25
        var pageSizeSelect = cut.Find(".pagination-page-size");
        pageSizeSelect.Change("25");

        // Assert — only 25 rows shown
        var rows = cut.FindAll("tbody tr");
        Assert.Equal(25, rows.Count);
    }

    [Fact]
    public void ChangePageSize_ResetsToPageOne()
    {
        // Arrange
        var cut = RenderTable(CreateItems(75));

        // Navigate to page 2 first
        var nextButton = cut.Find(".pagination-next");
        nextButton.Click();

        // Act — change page size to 25
        var pageSizeSelect = cut.Find(".pagination-page-size");
        pageSizeSelect.Change("25");

        // Assert — back to page 1, showing items 1–25
        var paginationInfo = cut.Find(".pagination-info");
        Assert.Contains("1", paginationInfo.TextContent);
        Assert.Contains("25", paginationInfo.TextContent);

        var activeButtons = cut.FindAll(".pagination-page.active");
        Assert.Single(activeButtons);
        Assert.Equal("1", activeButtons[0].TextContent.Trim());
    }

    [Fact]
    public void SortChange_ResetsToPageOne()
    {
        // Arrange
        var cut = RenderTable(CreateItems(75));

        // Navigate to page 2
        var nextButton = cut.Find(".pagination-next");
        nextButton.Click();

        // Act — click Description header to change sort
        var descriptionHeader = cut.FindAll("th.sortable")[1];
        descriptionHeader.Click();

        // Assert — back to page 1
        var activeButtons = cut.FindAll(".pagination-page.active");
        Assert.Single(activeButtons);
        Assert.Equal("1", activeButtons[0].TextContent.Trim());
    }

    [Fact]
    public void DefaultPageSizeSelector_Has25_50_100_Options()
    {
        // Arrange & Act
        var cut = RenderTable(CreateItems(60));

        // Assert
        var options = cut.FindAll(".pagination-page-size option");
        Assert.Equal(3, options.Count);
        Assert.Equal("25", options[0].GetAttribute("value"));
        Assert.Equal("50", options[1].GetAttribute("value"));
        Assert.Equal("100", options[2].GetAttribute("value"));
    }

    [Fact]
    public void DefaultPageSize_Is50()
    {
        // Arrange & Act
        var cut = RenderTable(CreateItems(60));

        // Assert — page size selector value
        var pageSizeSelect = cut.Find(".pagination-page-size");
        Assert.Equal("50", pageSizeSelect.GetAttribute("value"));

        // Also verify by row count
        var rows = cut.FindAll("tbody tr");
        Assert.Equal(50, rows.Count);
    }

    [Fact]
    public void PageSize100_ShowsAllWhenUnder100Items()
    {
        // Arrange
        var cut = RenderTable(CreateItems(75));

        // Act — change page size to 100
        var pageSizeSelect = cut.Find(".pagination-page-size");
        pageSizeSelect.Change("100");

        // Assert — all 75 items shown, no pagination bar needed
        var rows = cut.FindAll("tbody tr");
        Assert.Equal(75, rows.Count);
        Assert.Empty(cut.FindAll(".pagination-bar"));
    }

    [Fact]
    public void PaginationBar_NotShown_ForTransactionDtoMode()
    {
        // Arrange & Act — legacy mode (TransactionDto list)
        var transactions = Enumerable.Range(1, 60).Select(i => new TransactionDto
        {
            Id = Guid.NewGuid(),
            Date = new DateOnly(2026, 1, 1).AddDays(i % 30),
            Description = $"Txn {i}",
            Amount = new MoneyDto { Currency = "USD", Amount = -10m * i },
            CreatedAt = DateTime.UtcNow,
        }).ToList();

        var cut = Render<TransactionTable>(parameters => parameters
            .Add(p => p.Transactions, transactions)
            .Add(p => p.ShowDate, true));

        // Assert — pagination only works with Items mode
        // For legacy mode, all items are shown without pagination
        var rows = cut.FindAll("tbody tr");
        Assert.Equal(60, rows.Count);
    }

    [Fact]
    public void PaginationBar_HasNavLandmark_WithAriaLabel()
    {
        // Arrange & Act
        var cut = RenderTable(CreateItems(60));

        // Assert — pagination uses <nav> with aria-label
        var nav = cut.Find("nav.pagination-bar");
        Assert.Equal("Transaction table pagination", nav.GetAttribute("aria-label"));
    }

    [Fact]
    public void PrevNextButtons_HaveAriaLabels()
    {
        // Arrange & Act
        var cut = RenderTable(CreateItems(60));

        // Assert
        var prevButton = cut.Find(".pagination-prev");
        var nextButton = cut.Find(".pagination-next");
        Assert.Equal("Previous page", prevButton.GetAttribute("aria-label"));
        Assert.Equal("Next page", nextButton.GetAttribute("aria-label"));
    }

    [Fact]
    public void PageSizeSelect_HasAriaLabel()
    {
        // Arrange & Act
        var cut = RenderTable(CreateItems(60));

        // Assert
        var select = cut.Find(".pagination-page-size");
        Assert.Equal("Page size", select.GetAttribute("aria-label"));
    }

    private static List<TransactionListItem> CreateItems(int count)
    {
        return Enumerable.Range(1, count).Select(i => new TransactionListItem
        {
            Id = Guid.NewGuid(),
            Date = new DateOnly(2026, 1, 1).AddDays(i % 30),
            Description = $"Transaction {i:D4}",
            Amount = new MoneyDto { Currency = "USD", Amount = -10m * i },
            RunningBalance = new MoneyDto { Currency = "USD", Amount = 1000m - (10m * i) },
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i % 30).AddMinutes(i),
        }).ToList();
    }

    private IRenderedComponent<TransactionTable> RenderTable(List<TransactionListItem> items)
    {
        return Render<TransactionTable>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.ShowDate, true));
    }
}
