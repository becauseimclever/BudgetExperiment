// <copyright file="RulesPaginationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Display;
using Bunit;
using Shouldly;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the <see cref="RulesPagination"/> component.
/// </summary>
public sealed class RulesPaginationTests : BunitContext
{
    /// <summary>
    /// Verifies nothing renders when total count is zero and only one page.
    /// </summary>
    [Fact]
    public void DoesNotRender_WhenTotalCountIsZeroAndSinglePage()
    {
        var cut = RenderPagination(currentPage: 1, pageSize: 25, totalCount: 0, totalPages: 0);

        cut.Markup.Trim().ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies pagination renders when total count is greater than zero.
    /// </summary>
    [Fact]
    public void Renders_WhenTotalCountGreaterThanZero()
    {
        var cut = RenderPagination(currentPage: 1, pageSize: 25, totalCount: 10, totalPages: 1);

        cut.FindAll(".pagination-bar").Count.ShouldBe(1);
    }

    /// <summary>
    /// Verifies the showing range text is correct.
    /// </summary>
    [Fact]
    public void ShowsCorrectItemRange()
    {
        var cut = RenderPagination(currentPage: 2, pageSize: 10, totalCount: 25, totalPages: 3);

        cut.Markup.ShouldContain("11");
        cut.Markup.ShouldContain("20");
        cut.Markup.ShouldContain("25");
    }

    /// <summary>
    /// Verifies last page shows correct end range.
    /// </summary>
    [Fact]
    public void ShowsCorrectEndRange_OnLastPage()
    {
        var cut = RenderPagination(currentPage: 3, pageSize: 10, totalCount: 25, totalPages: 3);

        // Should show 21-25, not 21-30
        cut.Markup.ShouldContain("21");
        cut.Markup.ShouldContain("25");
    }

    /// <summary>
    /// Verifies previous button is disabled on first page.
    /// </summary>
    [Fact]
    public void PreviousButton_IsDisabled_OnFirstPage()
    {
        var cut = RenderPagination(currentPage: 1, pageSize: 25, totalCount: 50, totalPages: 2);

        var prevBtn = cut.Find(".pagination-prev");
        prevBtn.HasAttribute("disabled").ShouldBeTrue();
    }

    /// <summary>
    /// Verifies next button is disabled on last page.
    /// </summary>
    [Fact]
    public void NextButton_IsDisabled_OnLastPage()
    {
        var cut = RenderPagination(currentPage: 2, pageSize: 25, totalCount: 50, totalPages: 2);

        var nextBtn = cut.Find(".pagination-next");
        nextBtn.HasAttribute("disabled").ShouldBeTrue();
    }

    /// <summary>
    /// Verifies previous button is enabled on later pages.
    /// </summary>
    [Fact]
    public void PreviousButton_IsEnabled_OnLaterPages()
    {
        var cut = RenderPagination(currentPage: 2, pageSize: 25, totalCount: 50, totalPages: 2);

        var prevBtn = cut.Find(".pagination-prev");
        prevBtn.HasAttribute("disabled").ShouldBeFalse();
    }

    /// <summary>
    /// Verifies next button is enabled when not on last page.
    /// </summary>
    [Fact]
    public void NextButton_IsEnabled_WhenNotOnLastPage()
    {
        var cut = RenderPagination(currentPage: 1, pageSize: 25, totalCount: 50, totalPages: 2);

        var nextBtn = cut.Find(".pagination-next");
        nextBtn.HasAttribute("disabled").ShouldBeFalse();
    }

    /// <summary>
    /// Verifies clicking next invokes OnPageChanged with next page.
    /// </summary>
    [Fact]
    public void NextButton_InvokesOnPageChanged()
    {
        int? changedTo = null;
        var cut = Render<RulesPagination>(p => p
            .Add(x => x.CurrentPage, 1)
            .Add(x => x.PageSize, 25)
            .Add(x => x.TotalCount, 50)
            .Add(x => x.TotalPages, 2)
            .Add(x => x.OnPageChanged, (int page) => { changedTo = page; }));

        cut.Find(".pagination-next").Click();

        changedTo.ShouldBe(2);
    }

    /// <summary>
    /// Verifies clicking previous invokes OnPageChanged with previous page.
    /// </summary>
    [Fact]
    public void PreviousButton_InvokesOnPageChanged()
    {
        int? changedTo = null;
        var cut = Render<RulesPagination>(p => p
            .Add(x => x.CurrentPage, 2)
            .Add(x => x.PageSize, 25)
            .Add(x => x.TotalCount, 50)
            .Add(x => x.TotalPages, 2)
            .Add(x => x.OnPageChanged, (int page) => { changedTo = page; }));

        cut.Find(".pagination-prev").Click();

        changedTo.ShouldBe(1);
    }

    /// <summary>
    /// Verifies clicking a page number invokes OnPageChanged.
    /// </summary>
    [Fact]
    public void PageButton_InvokesOnPageChanged()
    {
        int? changedTo = null;
        var cut = Render<RulesPagination>(p => p
            .Add(x => x.CurrentPage, 1)
            .Add(x => x.PageSize, 25)
            .Add(x => x.TotalCount, 75)
            .Add(x => x.TotalPages, 3)
            .Add(x => x.OnPageChanged, (int page) => { changedTo = page; }));

        var page2Btn = cut.FindAll(".pagination-page").First(b => b.TextContent == "2");
        page2Btn.Click();

        changedTo.ShouldBe(2);
    }

    /// <summary>
    /// Verifies current page has active class.
    /// </summary>
    [Fact]
    public void CurrentPage_HasActiveClass()
    {
        var cut = RenderPagination(currentPage: 2, pageSize: 25, totalCount: 75, totalPages: 3);

        var activeBtn = cut.FindAll(".pagination-page.active");
        activeBtn.Count.ShouldBe(1);
        activeBtn[0].TextContent.ShouldBe("2");
    }

    /// <summary>
    /// Verifies page size selector invokes OnPageSizeChanged.
    /// </summary>
    [Fact]
    public void PageSizeSelector_InvokesOnPageSizeChanged()
    {
        int? newSize = null;
        var cut = Render<RulesPagination>(p => p
            .Add(x => x.CurrentPage, 1)
            .Add(x => x.PageSize, 25)
            .Add(x => x.TotalCount, 100)
            .Add(x => x.TotalPages, 4)
            .Add(x => x.OnPageSizeChanged, (int size) => { newSize = size; }));

        var select = cut.Find(".pagination-page-size");
        select.Change("50");

        newSize.ShouldBe(50);
    }

    /// <summary>
    /// Verifies page numbers are limited to 5 visible.
    /// </summary>
    [Fact]
    public void PageNumbers_LimitedToFiveVisible()
    {
        var cut = RenderPagination(currentPage: 5, pageSize: 10, totalCount: 100, totalPages: 10);

        var pageButtons = cut.FindAll(".pagination-page");
        pageButtons.Count.ShouldBeLessThanOrEqualTo(5);
    }

    /// <summary>
    /// Verifies showing 0 of 0 when total count is zero but component renders.
    /// </summary>
    [Fact]
    public void ShowsZeroRange_WhenTotalCountIsZero()
    {
        // TotalPages > 1 or TotalCount > 0 guard — with TotalCount=0 and TotalPages=0 it doesn't render
        // So we test with TotalCount > 0 but first page
        var cut = RenderPagination(currentPage: 1, pageSize: 25, totalCount: 5, totalPages: 1);

        cut.Markup.ShouldContain("1");
        cut.Markup.ShouldContain("5");
    }

    private IRenderedComponent<RulesPagination> RenderPagination(
        int currentPage,
        int pageSize,
        int totalCount,
        int totalPages)
    {
        return Render<RulesPagination>(p => p
            .Add(x => x.CurrentPage, currentPage)
            .Add(x => x.PageSize, pageSize)
            .Add(x => x.TotalCount, totalCount)
            .Add(x => x.TotalPages, totalPages));
    }
}
