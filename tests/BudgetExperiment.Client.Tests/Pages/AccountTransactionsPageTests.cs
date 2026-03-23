// <copyright file="AccountTransactionsPageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Pages;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Pages;

/// <summary>
/// Unit tests for the <see cref="AccountTransactions"/> legacy redirect page.
/// </summary>
public class AccountTransactionsPageTests : BunitContext
{
    /// <summary>
    /// Verifies that navigating to /accounts/{id}/transactions redirects to /transactions?account={id}.
    /// </summary>
    [Fact]
    public void Redirects_ToTransactionsWithAccountFilter()
    {
        var accountId = Guid.NewGuid();

        var cut = Render<AccountTransactions>(parameters => parameters
            .Add(p => p.AccountId, accountId));

        var navManager = this.Services.GetRequiredService<NavigationManager>();
        navManager.Uri.ShouldEndWith($"/transactions?account={accountId}");
    }

    /// <summary>
    /// Verifies the redirect produces no visible markup.
    /// </summary>
    [Fact]
    public void Renders_NoVisibleMarkup()
    {
        var cut = Render<AccountTransactions>(parameters => parameters
            .Add(p => p.AccountId, Guid.NewGuid()));

        cut.Markup.Trim().ShouldBeEmpty();
    }
}
