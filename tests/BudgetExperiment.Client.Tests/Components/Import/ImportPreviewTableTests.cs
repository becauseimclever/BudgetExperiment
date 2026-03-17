// <copyright file="ImportPreviewTableTests.cs" company="BecauseImClever">
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
/// Unit tests for the <see cref="ImportPreviewTable"/> component.
/// </summary>
public sealed class ImportPreviewTableTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImportPreviewTableTests"/> class.
    /// </summary>
    public ImportPreviewTableTests()
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
    /// Verifies that the header shows row count and valid count.
    /// </summary>
    [Fact]
    public void ImportPreviewTable_ShowsHeaderStats()
    {
        var preview = new ImportPreviewResult
        {
            Rows = [new ImportPreviewRow { RowIndex = 1, Status = ImportRowStatus.Valid }],
            ValidCount = 1,
        };

        var cut = Render<ImportPreviewTable>(parameters => parameters
            .Add(p => p.PreviewResult, preview)
            .Add(p => p.SelectedIndices, new HashSet<int> { 1 }));

        Assert.Contains("1 rows found", cut.Markup);
        Assert.Contains("1 valid", cut.Markup);
    }

    /// <summary>
    /// Verifies warning count appears in header when present.
    /// </summary>
    [Fact]
    public void ImportPreviewTable_ShowsWarningCount_WhenPresent()
    {
        var preview = new ImportPreviewResult
        {
            Rows = [new ImportPreviewRow { RowIndex = 1, Status = ImportRowStatus.Warning }],
            ValidCount = 0,
            WarningCount = 1,
        };

        var cut = Render<ImportPreviewTable>(parameters => parameters
            .Add(p => p.PreviewResult, preview)
            .Add(p => p.SelectedIndices, new HashSet<int>()));

        Assert.Contains("1 warnings", cut.Markup);
    }

    /// <summary>
    /// Verifies error count appears in header when present.
    /// </summary>
    [Fact]
    public void ImportPreviewTable_ShowsErrorCount_WhenPresent()
    {
        var preview = new ImportPreviewResult
        {
            Rows = [new ImportPreviewRow { RowIndex = 1, Status = ImportRowStatus.Error }],
            ErrorCount = 1,
        };

        var cut = Render<ImportPreviewTable>(parameters => parameters
            .Add(p => p.PreviewResult, preview)
            .Add(p => p.SelectedIndices, new HashSet<int>()));

        Assert.Contains("1 errors", cut.Markup);
    }

    /// <summary>
    /// Verifies valid rows get success badge.
    /// </summary>
    [Fact]
    public void ImportPreviewTable_ValidRow_ShowsSuccessBadge()
    {
        var preview = new ImportPreviewResult
        {
            Rows =
            [
                new ImportPreviewRow
                {
                    RowIndex = 1,
                    Date = new DateOnly(2024, 6, 15),
                    Description = "Test Transaction",
                    Amount = -50.00m,
                    Status = ImportRowStatus.Valid,
                },
            ],
            ValidCount = 1,
        };

        var cut = Render<ImportPreviewTable>(parameters => parameters
            .Add(p => p.PreviewResult, preview)
            .Add(p => p.SelectedIndices, new HashSet<int> { 1 }));

        Assert.Contains("badge-success", cut.Markup);
        Assert.Contains("Valid", cut.Markup);
    }

    /// <summary>
    /// Verifies error rows get danger badge and disabled checkbox.
    /// </summary>
    [Fact]
    public void ImportPreviewTable_ErrorRow_ShowsDangerBadge_DisablesCheckbox()
    {
        var preview = new ImportPreviewResult
        {
            Rows =
            [
                new ImportPreviewRow
                {
                    RowIndex = 1,
                    Status = ImportRowStatus.Error,
                    StatusMessage = "Invalid date",
                },
            ],
            ErrorCount = 1,
        };

        var cut = Render<ImportPreviewTable>(parameters => parameters
            .Add(p => p.PreviewResult, preview)
            .Add(p => p.SelectedIndices, new HashSet<int>()));

        Assert.Contains("badge-danger", cut.Markup);
        Assert.Contains("Error", cut.Markup);

        var checkboxes = cut.FindAll("input[type='checkbox']");
        var rowCheckbox = checkboxes.Last();
        Assert.NotNull(rowCheckbox.GetAttribute("disabled"));
    }

    /// <summary>
    /// Verifies duplicate rows get info badge.
    /// </summary>
    [Fact]
    public void ImportPreviewTable_DuplicateRow_ShowsInfoBadge()
    {
        var preview = new ImportPreviewResult
        {
            Rows =
            [
                new ImportPreviewRow
                {
                    RowIndex = 1,
                    Status = ImportRowStatus.Duplicate,
                    StatusMessage = "Possible duplicate",
                },
            ],
            DuplicateCount = 1,
        };

        var cut = Render<ImportPreviewTable>(parameters => parameters
            .Add(p => p.PreviewResult, preview)
            .Add(p => p.SelectedIndices, new HashSet<int>()));

        Assert.Contains("badge-info", cut.Markup);
        Assert.Contains("Duplicate", cut.Markup);
    }

    /// <summary>
    /// Verifies negative amounts are styled as danger (red).
    /// </summary>
    [Fact]
    public void ImportPreviewTable_NegativeAmount_ShowsDangerText()
    {
        var preview = new ImportPreviewResult
        {
            Rows =
            [
                new ImportPreviewRow
                {
                    RowIndex = 1,
                    Amount = -100.50m,
                    Status = ImportRowStatus.Valid,
                },
            ],
        };

        var cut = Render<ImportPreviewTable>(parameters => parameters
            .Add(p => p.PreviewResult, preview)
            .Add(p => p.SelectedIndices, new HashSet<int> { 1 }));

        Assert.Contains("text-danger", cut.Markup);
    }

    /// <summary>
    /// Verifies uncategorized rows show "Uncategorized" text.
    /// </summary>
    [Fact]
    public void ImportPreviewTable_NullCategory_ShowsUncategorized()
    {
        var preview = new ImportPreviewResult
        {
            Rows =
            [
                new ImportPreviewRow
                {
                    RowIndex = 1,
                    Category = null,
                    Status = ImportRowStatus.Valid,
                },
            ],
        };

        var cut = Render<ImportPreviewTable>(parameters => parameters
            .Add(p => p.PreviewResult, preview)
            .Add(p => p.SelectedIndices, new HashSet<int> { 1 }));

        Assert.Contains("Uncategorized", cut.Markup);
    }

    /// <summary>
    /// Verifies auto-categorized count appears in footer.
    /// </summary>
    [Fact]
    public void ImportPreviewTable_ShowsAutoCategorizedCount_InFooter()
    {
        var preview = new ImportPreviewResult
        {
            Rows =
            [
                new ImportPreviewRow { RowIndex = 1, Status = ImportRowStatus.Valid },
            ],
            AutoCategorizedCount = 3,
        };

        var cut = Render<ImportPreviewTable>(parameters => parameters
            .Add(p => p.PreviewResult, preview)
            .Add(p => p.SelectedIndices, new HashSet<int> { 1 }));

        Assert.Contains("3 rows auto-categorized by rules", cut.Markup);
    }

    /// <summary>
    /// Verifies the footer shows selection count and amount.
    /// </summary>
    [Fact]
    public void ImportPreviewTable_FooterShowsSelectionSummary()
    {
        var preview = new ImportPreviewResult
        {
            Rows =
            [
                new ImportPreviewRow { RowIndex = 1, Amount = -50.00m, Status = ImportRowStatus.Valid },
                new ImportPreviewRow { RowIndex = 2, Amount = -30.00m, Status = ImportRowStatus.Valid },
            ],
            ValidCount = 2,
        };

        var cut = Render<ImportPreviewTable>(parameters => parameters
            .Add(p => p.PreviewResult, preview)
            .Add(p => p.SelectedIndices, new HashSet<int> { 1 }));

        Assert.Contains("1", cut.Find(".selection-summary").TextContent);
        Assert.Contains("of 2 rows selected", cut.Find(".selection-summary").TextContent);
    }

    /// <summary>
    /// Verifies status message row appears for non-valid rows.
    /// </summary>
    [Fact]
    public void ImportPreviewTable_ShowsStatusMessage_ForErrorRow()
    {
        var preview = new ImportPreviewResult
        {
            Rows =
            [
                new ImportPreviewRow
                {
                    RowIndex = 1,
                    Status = ImportRowStatus.Error,
                    StatusMessage = "Date parsing failed",
                },
            ],
        };

        var cut = Render<ImportPreviewTable>(parameters => parameters
            .Add(p => p.PreviewResult, preview)
            .Add(p => p.SelectedIndices, new HashSet<int>()));

        Assert.Contains("Date parsing failed", cut.Markup);
    }

    /// <summary>
    /// Verifies row with category from auto-rule shows primary badge.
    /// </summary>
    [Fact]
    public void ImportPreviewTable_AutoRuleCategory_ShowsPrimaryBadge()
    {
        var preview = new ImportPreviewResult
        {
            Rows =
            [
                new ImportPreviewRow
                {
                    RowIndex = 1,
                    Category = "Groceries",
                    CategorySource = CategorySource.AutoRule,
                    Status = ImportRowStatus.Valid,
                },
            ],
        };

        var cut = Render<ImportPreviewTable>(parameters => parameters
            .Add(p => p.PreviewResult, preview)
            .Add(p => p.SelectedIndices, new HashSet<int> { 1 }));

        Assert.Contains("badge-primary", cut.Markup);
        Assert.Contains("Groceries", cut.Markup);
    }
}
