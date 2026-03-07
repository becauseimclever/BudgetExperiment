// <copyright file="ImportHistoryListTests.cs" company="BecauseImClever">
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
/// Unit tests for the <see cref="ImportHistoryList"/> component.
/// </summary>
public sealed class ImportHistoryListTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImportHistoryListTests"/> class.
    /// </summary>
    public ImportHistoryListTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies empty state renders when there are no batches.
    /// </summary>
    [Fact]
    public void ImportHistoryList_ShowsEmptyState_WhenNoBatches()
    {
        var cut = Render<ImportHistoryList>();

        Assert.Contains("No import history yet.", cut.Markup);
        Assert.Contains("empty-state", cut.Markup);
    }

    /// <summary>
    /// Verifies loading state renders spinner.
    /// </summary>
    [Fact]
    public void ImportHistoryList_ShowsLoadingSpinner_WhenIsLoading()
    {
        var cut = Render<ImportHistoryList>(parameters => parameters
            .Add(p => p.IsLoading, true));

        Assert.Contains("Loading import history...", cut.Markup);
        Assert.Contains("spinner-border", cut.Markup);
    }

    /// <summary>
    /// Verifies batches render in the table.
    /// </summary>
    [Fact]
    public void ImportHistoryList_RendersBatchTable_WhenBatchesProvided()
    {
        var batches = new List<ImportBatchDto>
        {
            new() { Id = Guid.NewGuid(), FileName = "test.csv", AccountName = "Checking", TransactionCount = 10, Status = ImportBatchStatus.Completed, ImportedAtUtc = DateTime.UtcNow },
        };

        var cut = Render<ImportHistoryList>(parameters => parameters
            .Add(p => p.Batches, batches));

        Assert.Contains("test.csv", cut.Markup);
        Assert.Contains("Checking", cut.Markup);
        var badges = cut.FindAll(".badge");
        Assert.True(badges.Count > 0);
    }

    /// <summary>
    /// Verifies completed status renders with success badge.
    /// </summary>
    [Fact]
    public void ImportHistoryList_ShowsCompletedBadge()
    {
        var batches = new List<ImportBatchDto>
        {
            new() { Id = Guid.NewGuid(), Status = ImportBatchStatus.Completed, ImportedAtUtc = DateTime.UtcNow },
        };

        var cut = Render<ImportHistoryList>(parameters => parameters
            .Add(p => p.Batches, batches));

        Assert.Contains("Completed", cut.Markup);
        Assert.Contains("badge-success", cut.Markup);
    }

    /// <summary>
    /// Verifies partially completed status renders with warning badge.
    /// </summary>
    [Fact]
    public void ImportHistoryList_ShowsPartialBadge()
    {
        var batches = new List<ImportBatchDto>
        {
            new() { Id = Guid.NewGuid(), Status = ImportBatchStatus.PartiallyCompleted, ImportedAtUtc = DateTime.UtcNow },
        };

        var cut = Render<ImportHistoryList>(parameters => parameters
            .Add(p => p.Batches, batches));

        Assert.Contains("Partial", cut.Markup);
        Assert.Contains("badge-warning", cut.Markup);
    }

    /// <summary>
    /// Verifies deleted status renders with secondary badge and hides delete button.
    /// </summary>
    [Fact]
    public void ImportHistoryList_ShowsDeletedBadge_HidesDeleteButton()
    {
        var batches = new List<ImportBatchDto>
        {
            new() { Id = Guid.NewGuid(), Status = ImportBatchStatus.Deleted, ImportedAtUtc = DateTime.UtcNow },
        };

        var cut = Render<ImportHistoryList>(parameters => parameters
            .Add(p => p.Batches, batches));

        Assert.Contains("Deleted", cut.Markup);
        Assert.DoesNotContain("btn-outline-danger", cut.Markup);
    }

    /// <summary>
    /// Verifies that the refresh button invokes the OnRefresh callback.
    /// </summary>
    [Fact]
    public void ImportHistoryList_RefreshButton_InvokesCallback()
    {
        var refreshCalled = false;
        var batches = new List<ImportBatchDto>
        {
            new() { Id = Guid.NewGuid(), Status = ImportBatchStatus.Completed, ImportedAtUtc = DateTime.UtcNow },
        };

        var cut = Render<ImportHistoryList>(parameters => parameters
            .Add(p => p.Batches, batches)
            .Add(p => p.OnRefresh, () => { refreshCalled = true; }));

        var refreshBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Refresh"));
        refreshBtn.Click();

        Assert.True(refreshCalled);
    }

    /// <summary>
    /// Verifies that clicking delete shows confirmation modal.
    /// </summary>
    [Fact]
    public void ImportHistoryList_DeleteButton_ShowsConfirmation()
    {
        var batchId = Guid.NewGuid();
        var batches = new List<ImportBatchDto>
        {
            new() { Id = batchId, FileName = "data.csv", TransactionCount = 5, Status = ImportBatchStatus.Completed, ImportedAtUtc = DateTime.UtcNow },
        };

        var cut = Render<ImportHistoryList>(parameters => parameters
            .Add(p => p.Batches, batches));

        var deleteBtn = cut.Find(".btn-outline-danger");
        deleteBtn.Click();

        Assert.Contains("data.csv", cut.Markup);
        Assert.Contains("Delete", cut.Markup);
        Assert.Contains("5 transactions", cut.Markup);
    }

    /// <summary>
    /// Verifies that long filenames are truncated.
    /// </summary>
    [Fact]
    public void ImportHistoryList_TruncatesLongFilenames()
    {
        var batches = new List<ImportBatchDto>
        {
            new() { Id = Guid.NewGuid(), FileName = "this-is-a-very-long-filename-that-exceeds-the-limit.csv", Status = ImportBatchStatus.Completed, ImportedAtUtc = DateTime.UtcNow },
        };

        var cut = Render<ImportHistoryList>(parameters => parameters
            .Add(p => p.Batches, batches));

        Assert.Contains("...", cut.Markup);
    }

    /// <summary>
    /// Verifies that the refresh button is hidden when no batches exist.
    /// </summary>
    [Fact]
    public void ImportHistoryList_HidesRefreshButton_WhenNoBatches()
    {
        var cut = Render<ImportHistoryList>();

        var buttons = cut.FindAll("button");
        Assert.DoesNotContain(buttons, b => b.TextContent.Contains("Refresh"));
    }

    /// <summary>
    /// Verifies transaction count is displayed as a badge.
    /// </summary>
    [Fact]
    public void ImportHistoryList_ShowsTransactionCountBadge()
    {
        var batches = new List<ImportBatchDto>
        {
            new() { Id = Guid.NewGuid(), TransactionCount = 42, Status = ImportBatchStatus.Completed, ImportedAtUtc = DateTime.UtcNow },
        };

        var cut = Render<ImportHistoryList>(parameters => parameters
            .Add(p => p.Batches, batches));

        Assert.Contains("42", cut.Markup);
    }
}
