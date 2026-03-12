// <copyright file="ImportSummaryCardTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Import;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.Import;

/// <summary>
/// Unit tests for the <see cref="ImportSummaryCard"/> component.
/// </summary>
public sealed class ImportSummaryCardTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImportSummaryCardTests"/> class.
    /// </summary>
    public ImportSummaryCardTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies success icon when no errors.
    /// </summary>
    [Fact]
    public void ImportSummaryCard_ShowsSuccessIcon_WhenNoErrors()
    {
        var result = new ImportResult { ImportedCount = 10, ErrorCount = 0 };

        var cut = Render<ImportSummaryCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.AccountId, Guid.NewGuid()));

        Assert.Contains("Import Successful!", cut.Markup);
        Assert.Contains("success-icon", cut.Markup);
    }

    /// <summary>
    /// Verifies warning icon when there are errors.
    /// </summary>
    [Fact]
    public void ImportSummaryCard_ShowsWarningIcon_WhenErrors()
    {
        var result = new ImportResult { ImportedCount = 8, ErrorCount = 2 };

        var cut = Render<ImportSummaryCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.AccountId, Guid.NewGuid()));

        Assert.Contains("Import Completed with Issues", cut.Markup);
        Assert.Contains("warning-icon", cut.Markup);
    }

    /// <summary>
    /// Verifies imported count stat card.
    /// </summary>
    [Fact]
    public void ImportSummaryCard_ShowsImportedCount()
    {
        var result = new ImportResult { ImportedCount = 15 };

        var cut = Render<ImportSummaryCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.AccountId, Guid.NewGuid()));

        Assert.Contains("15", cut.Markup);
        Assert.Contains("Imported", cut.Markup);
    }

    /// <summary>
    /// Verifies skipped count card is shown only when skipped > 0.
    /// </summary>
    [Fact]
    public void ImportSummaryCard_ShowsSkippedCount_WhenGreaterThanZero()
    {
        var result = new ImportResult { ImportedCount = 10, SkippedCount = 3 };

        var cut = Render<ImportSummaryCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.AccountId, Guid.NewGuid()));

        Assert.Contains("3", cut.Markup);
        Assert.Contains("Skipped", cut.Markup);
    }

    /// <summary>
    /// Verifies skipped card is hidden when count is zero.
    /// </summary>
    [Fact]
    public void ImportSummaryCard_HidesSkippedCard_WhenZero()
    {
        var result = new ImportResult { ImportedCount = 10, SkippedCount = 0 };

        var cut = Render<ImportSummaryCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.AccountId, Guid.NewGuid()));

        Assert.DoesNotContain("Skipped", cut.Markup);
    }

    /// <summary>
    /// Verifies auto-categorized count stat card.
    /// </summary>
    [Fact]
    public void ImportSummaryCard_ShowsAutoCategorizedCount()
    {
        var result = new ImportResult { ImportedCount = 10, AutoCategorizedCount = 7 };

        var cut = Render<ImportSummaryCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.AccountId, Guid.NewGuid()));

        Assert.Contains("7", cut.Markup);
        Assert.Contains("Auto-Categorized", cut.Markup);
    }

    /// <summary>
    /// Verifies categorization breakdown section.
    /// </summary>
    [Fact]
    public void ImportSummaryCard_ShowsCategorizationBreakdown()
    {
        var result = new ImportResult
        {
            ImportedCount = 10,
            AutoCategorizedCount = 5,
            CsvCategorizedCount = 3,
            UncategorizedCount = 2,
        };

        var cut = Render<ImportSummaryCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.AccountId, Guid.NewGuid()));

        Assert.Contains("Categorization Summary", cut.Markup);
        Assert.Contains("5", cut.Markup);
        Assert.Contains("by rules", cut.Markup);
        Assert.Contains("3", cut.Markup);
        Assert.Contains("from CSV", cut.Markup);
        Assert.Contains("2", cut.Markup);
        Assert.Contains("uncategorized", cut.Markup);
    }

    /// <summary>
    /// Verifies view transactions link uses account ID.
    /// </summary>
    [Fact]
    public void ImportSummaryCard_ViewTransactionsLink_HasCorrectHref()
    {
        var accountId = Guid.NewGuid();
        var result = new ImportResult { ImportedCount = 1 };

        var cut = Render<ImportSummaryCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.AccountId, accountId));

        var link = cut.Find("a.btn-primary");
        Assert.Contains($"transactions?account={accountId}", link.GetAttribute("href") ?? string.Empty);
    }

    /// <summary>
    /// Verifies Import Another File button invokes callback.
    /// </summary>
    [Fact]
    public void ImportSummaryCard_ImportAnotherButton_InvokesCallback()
    {
        var callbackInvoked = false;
        var result = new ImportResult { ImportedCount = 1 };

        var cut = Render<ImportSummaryCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.AccountId, Guid.NewGuid())
            .Add(p => p.OnStartNewImport, () => { callbackInvoked = true; }));

        var button = cut.Find("button.btn-outline-secondary");
        button.Click();

        Assert.True(callbackInvoked);
    }

    /// <summary>
    /// Verifies batch ID is shown in footer.
    /// </summary>
    [Fact]
    public void ImportSummaryCard_ShowsBatchId_WhenNotEmpty()
    {
        var batchId = Guid.NewGuid();
        var result = new ImportResult { BatchId = batchId };

        var cut = Render<ImportSummaryCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.AccountId, Guid.NewGuid()));

        Assert.Contains(batchId.ToString()[..8], cut.Markup);
        Assert.Contains("Import Batch ID", cut.Markup);
    }
}
