// <copyright file="UncategorizedPageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Pages;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BudgetExperiment.Client.Tests.Pages;

/// <summary>
/// Unit tests for the <see cref="Uncategorized"/> legacy redirect page.
/// </summary>
public class UncategorizedPageTests : BunitContext
{
    /// <summary>
    /// Verifies that navigating to /uncategorized redirects to /transactions?uncategorized=true.
    /// </summary>
    [Fact]
    public void Redirects_ToTransactionsWithUncategorizedFilter()
    {
        var cut = Render<Uncategorized>();

        var navManager = this.Services.GetRequiredService<NavigationManager>();
        navManager.Uri.ShouldEndWith("/transactions?uncategorized=true");
    }

    /// <summary>
    /// Verifies the redirect produces no visible markup.
    /// </summary>
    [Fact]
    public void Renders_NoVisibleMarkup()
    {
        var cut = Render<Uncategorized>();

        cut.Markup.Trim().ShouldBeEmpty();
    }
}
