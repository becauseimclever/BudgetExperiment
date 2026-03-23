// <copyright file="MatchReviewModalTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Reconciliation;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.Reconciliation;

/// <summary>
/// Unit tests for the <see cref="MatchReviewModal"/> component.
/// </summary>
public sealed class MatchReviewModalTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchReviewModalTests"/> class.
    /// </summary>
    public MatchReviewModalTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
        Services.AddSingleton<CultureService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies the modal renders nothing when not visible.
    /// </summary>
    [Fact]
    public void MatchReviewModal_RendersNothing_WhenNotVisible()
    {
        var cut = Render<MatchReviewModal>(parameters => parameters
            .Add(p => p.IsVisible, false)
            .Add(p => p.Match, CreateMatch()));

        Assert.DoesNotContain("match-review", cut.Markup);
    }

    /// <summary>
    /// Verifies the modal renders the review content when visible.
    /// </summary>
    [Fact]
    public void MatchReviewModal_RendersContent_WhenVisible()
    {
        var cut = Render<MatchReviewModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Match, CreateMatch()));

        Assert.Contains("match-review", cut.Markup);
    }

    /// <summary>
    /// Verifies the modal shows the confidence score.
    /// </summary>
    [Fact]
    public void MatchReviewModal_ShowsConfidenceScore()
    {
        var cut = Render<MatchReviewModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Match, CreateMatch(confidence: 0.85m)));

        Assert.Contains("85", cut.Markup);
    }

    /// <summary>
    /// Verifies the confidence bar has the correct CSS class for high confidence.
    /// </summary>
    [Fact]
    public void MatchReviewModal_ShowsHighConfidenceClass()
    {
        var cut = Render<MatchReviewModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Match, CreateMatch(confidence: 0.9m, level: "High")));

        Assert.Contains("high", cut.Markup);
    }

    /// <summary>
    /// Verifies the modal shows the expected comparison section.
    /// </summary>
    [Fact]
    public void MatchReviewModal_ShowsExpectedComparison()
    {
        var cut = Render<MatchReviewModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Match, CreateMatch()));

        Assert.Contains("Expected", cut.Markup);
    }

    /// <summary>
    /// Verifies the modal shows the actual comparison section.
    /// </summary>
    [Fact]
    public void MatchReviewModal_ShowsActualComparison()
    {
        var cut = Render<MatchReviewModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Match, CreateMatch()));

        Assert.Contains("Actual", cut.Markup);
    }

    /// <summary>
    /// Verifies the modal shows the recurring transaction description.
    /// </summary>
    [Fact]
    public void MatchReviewModal_ShowsRecurringDescription()
    {
        var cut = Render<MatchReviewModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Match, CreateMatch()));

        Assert.Contains("Electric Bill", cut.Markup);
    }

    /// <summary>
    /// Verifies the modal shows the imported transaction description.
    /// </summary>
    [Fact]
    public void MatchReviewModal_ShowsImportedTransactionDescription()
    {
        var match = CreateMatch();

        var cut = Render<MatchReviewModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Match, match));

        Assert.Contains("ELECTRIC CO PAYMENT", cut.Markup);
    }

    /// <summary>
    /// Verifies the accept button invokes the OnAccept callback.
    /// </summary>
    [Fact]
    public void MatchReviewModal_AcceptButton_InvokesCallback()
    {
        Guid? acceptedId = null;
        var match = CreateMatch();

        var cut = Render<MatchReviewModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Match, match)
            .Add(p => p.OnAccept, (Guid id) => { acceptedId = id; }));

        var acceptBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Accept"));
        acceptBtn.Click();

        Assert.Equal(match.Id, acceptedId);
    }

    /// <summary>
    /// Verifies the reject button invokes the OnReject callback.
    /// </summary>
    [Fact]
    public void MatchReviewModal_RejectButton_InvokesCallback()
    {
        Guid? rejectedId = null;
        var match = CreateMatch();

        var cut = Render<MatchReviewModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Match, match)
            .Add(p => p.OnReject, (Guid id) => { rejectedId = id; }));

        var rejectBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Reject"));
        rejectBtn.Click();

        Assert.Equal(match.Id, rejectedId);
    }

    /// <summary>
    /// Verifies the modal shows variance section with amount difference.
    /// </summary>
    [Fact]
    public void MatchReviewModal_ShowsAmountVariance()
    {
        var cut = Render<MatchReviewModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Match, CreateMatch(amountVariance: 5.50m)));

        Assert.Contains("5.50", cut.Markup);
    }

    /// <summary>
    /// Verifies the modal shows date offset in variance section.
    /// </summary>
    [Fact]
    public void MatchReviewModal_ShowsDateOffset()
    {
        var cut = Render<MatchReviewModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Match, CreateMatch(dateOffsetDays: 2)));

        Assert.Contains("2", cut.Markup);
        Assert.Contains("day", cut.Markup);
    }

    /// <summary>
    /// Verifies the cancel button invokes the OnCancel callback.
    /// </summary>
    [Fact]
    public void MatchReviewModal_CancelButton_InvokesCallback()
    {
        var cancelled = false;

        var cut = Render<MatchReviewModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Match, CreateMatch())
            .Add(p => p.OnCancel, () => { cancelled = true; }));

        var cancelBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
        cancelBtn.Click();

        Assert.True(cancelled);
    }

    private static ReconciliationMatchDto CreateMatch(
        decimal confidence = 0.85m,
        string level = "High",
        decimal amountVariance = 0m,
        int dateOffsetDays = 0) => new()
        {
            Id = Guid.NewGuid(),
            ImportedTransactionId = Guid.NewGuid(),
            RecurringTransactionId = Guid.NewGuid(),
            RecurringInstanceDate = new DateOnly(2026, 3, 1),
            ConfidenceScore = confidence,
            ConfidenceLevel = level,
            Status = "Suggested",
            Source = "Auto",
            AmountVariance = amountVariance,
            DateOffsetDays = dateOffsetDays,
            CreatedAtUtc = DateTime.UtcNow,
            RecurringTransactionDescription = "Electric Bill",
            ExpectedAmount = new MoneyDto { Amount = 150.00m, Currency = "USD" },
            ImportedTransaction = new TransactionDto
            {
                Id = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                Description = "ELECTRIC CO PAYMENT",
                Date = new DateOnly(2026, 3, 1),
                Amount = new MoneyDto { Amount = -150.00m, Currency = "USD" },
            },
        };
}
