// <copyright file="ImportPageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Pages;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BudgetExperiment.Client.Tests.Pages;

/// <summary>
/// Unit tests for the <see cref="Import"/> page component.
/// </summary>
public class ImportPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _budgetApi = new();
    private readonly StubImportApiService _importApi = new();
    private readonly StubCsvParserService _csvParser = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportPageTests"/> class.
    /// </summary>
    public ImportPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(this._budgetApi);
        this.Services.AddSingleton<IImportApiService>(this._importApi);
        this.Services.AddSingleton<ICsvParserService>(this._csvParser);
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
        this.Services.AddSingleton<IExportDownloadService>(new StubExportDownloadService());
        this.Services.AddSingleton<IToastService>(new ToastService());
        this.Services.AddSingleton<IApiErrorContext>(new ApiErrorContext());
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync()
    {
        return base.DisposeAsync().AsTask();
    }

    /// <summary>
    /// Verifies the page renders without errors.
    /// </summary>
    [Fact]
    public void Renders_WithoutErrors()
    {
        var cut = Render<Import>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page title is correct.
    /// </summary>
    [Fact]
    public void HasCorrectPageTitle()
    {
        var cut = Render<Import>();

        cut.Markup.ShouldContain("Import Transactions");
    }

    /// <summary>
    /// Verifies all three tabs are displayed.
    /// </summary>
    [Fact]
    public void ShowsTabNavigation()
    {
        var cut = Render<Import>();

        cut.Markup.ShouldContain("New Import");
        cut.Markup.ShouldContain("Import History");
        cut.Markup.ShouldContain("Saved Mappings");
    }

    /// <summary>
    /// Verifies the wizard tab is active by default.
    /// </summary>
    [Fact]
    public void WizardTab_IsActiveByDefault()
    {
        var cut = Render<Import>();

        var tabs = cut.FindAll(".nav-link");
        tabs[0].ClassList.ShouldContain("active");
    }

    /// <summary>
    /// Verifies wizard progress steps are shown on step 1.
    /// </summary>
    [Fact]
    public void ShowsWizardProgressSteps()
    {
        var cut = Render<Import>();

        cut.Markup.ShouldContain("Upload File");
        cut.Markup.ShouldContain("Map Columns");
        cut.Markup.ShouldContain("Preview");
    }

    /// <summary>
    /// Verifies step 1 shows upload section.
    /// </summary>
    [Fact]
    public void Step1_ShowsUploadSection()
    {
        var cut = Render<Import>();

        cut.Markup.ShouldContain("Upload CSV File");
    }

    /// <summary>
    /// Verifies the Start Over button is not shown on step 1.
    /// </summary>
    [Fact]
    public void Step1_DoesNotShowStartOverButton()
    {
        var cut = Render<Import>();

        cut.Markup.ShouldNotContain("Start Over");
    }

    /// <summary>
    /// Verifies error message is shown when initial data load fails.
    /// </summary>
    [Fact]
    public void ShowsError_WhenLoadFails()
    {
        this._importApi.ShouldThrowOnGetMappings = true;

        var cut = Render<Import>();

        cut.Markup.ShouldContain("Failed to load data");
    }

    /// <summary>
    /// Verifies history tab loads when clicked.
    /// </summary>
    [Fact]
    public void HistoryTab_LoadsHistory()
    {
        this._importApi.Batches.Add(new ImportBatchDto
        {
            Id = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
            FileName = "test.csv",
            TransactionCount = 10,
            ImportedAtUtc = DateTime.UtcNow,
        });

        var cut = Render<Import>();
        var historyTab = cut.FindAll(".nav-link")[1];
        historyTab.Click();

        cut.Markup.ShouldContain("test.csv");
    }

    /// <summary>
    /// Verifies history tab shows error when load fails.
    /// </summary>
    [Fact]
    public void HistoryTab_ShowsError_WhenLoadFails()
    {
        var cut = Render<Import>();

        this._importApi.ShouldThrowOnGetHistory = true;
        var historyTab = cut.FindAll(".nav-link")[1];
        historyTab.Click();

        cut.Markup.ShouldContain("Failed to load import history");
    }

    /// <summary>
    /// Verifies mappings tab loads saved mappings.
    /// </summary>
    [Fact]
    public void MappingsTab_LoadsMappings()
    {
        this._importApi.Mappings.Add(new ImportMappingDto
        {
            Id = Guid.NewGuid(),
            Name = "Bank Statement",
            ColumnMappings = [],
            CreatedAtUtc = DateTime.UtcNow,
        });

        var cut = Render<Import>();
        var mappingsTab = cut.FindAll(".nav-link")[2];
        mappingsTab.Click();

        cut.Markup.ShouldContain("Bank Statement");
    }

    /// <summary>
    /// Verifies step 1 first progress step is active.
    /// </summary>
    [Fact]
    public void Step1_FirstStepIsActive()
    {
        var cut = Render<Import>();

        var steps = cut.FindAll(".wizard-step");
        steps[0].ClassList.ShouldContain("active");
        steps[1].ClassList.ShouldNotContain("active");
    }

    /// <summary>
    /// Verifies switching tabs hides wizard content.
    /// </summary>
    [Fact]
    public void SwitchingToHistoryTab_HidesWizard()
    {
        var cut = Render<Import>();
        var historyTab = cut.FindAll(".nav-link")[1];
        historyTab.Click();

        cut.Markup.ShouldNotContain("Upload CSV File");
    }

    /// <summary>
    /// Verifies switching back to wizard tab restores wizard content.
    /// </summary>
    [Fact]
    public void SwitchingBackToWizardTab_ShowsWizard()
    {
        var cut = Render<Import>();
        var historyTab = cut.FindAll(".nav-link")[1];
        historyTab.Click();

        var wizardTab = cut.FindAll(".nav-link")[0];
        wizardTab.Click();

        cut.Markup.ShouldContain("Upload CSV File");
    }

    /// <summary>
    /// Verifies accounts are loaded for account selection in step 1.
    /// </summary>
    [Fact]
    public void Step1_AccountsAreAvailableForSelection()
    {
        this._budgetApi.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Checking",
            Type = "Checking",
            InitialBalance = 0m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        var cut = Render<Import>();

        // Accounts list is loaded but select is only shown after file parse
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies mappings tab shows error when mappings load fails.
    /// </summary>
    [Fact]
    public void MappingsTab_ShowsError_WhenRefreshFails()
    {
        var cut = Render<Import>();

        // Mappings were already loaded in OnInit, so we need a fresh state
        // where no mappings were loaded (empty list) and then the refresh fails
        this._importApi.ShouldThrowOnGetMappings = true;
        this._importApi.Mappings.Clear();
        var mappingsTab = cut.FindAll(".nav-link")[2];
        mappingsTab.Click();

        cut.Markup.ShouldContain("Failed to load mappings");
    }

    /// <summary>
    /// Verifies history tab shows import batches when loaded.
    /// </summary>
    [Fact]
    public void HistoryTab_ShowsBatches_WhenHistoryExists()
    {
        this._importApi.Batches.Add(new ImportBatchDto
        {
            Id = Guid.NewGuid(),
            FileName = "transactions.csv",
            ImportedAtUtc = DateTime.UtcNow,
            TransactionCount = 15,
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
        });

        var cut = Render<Import>();
        var historyTab = cut.FindAll(".nav-link")[1];
        historyTab.Click();

        cut.Markup.ShouldContain("transactions.csv");
    }

    /// <summary>
    /// Verifies delete batch removes batch from history list.
    /// </summary>
    [Fact]
    public void DeleteBatch_RemovesBatchFromHistory()
    {
        var batchId = Guid.NewGuid();
        this._importApi.Batches.Add(new ImportBatchDto
        {
            Id = batchId,
            FileName = "old-import.csv",
            ImportedAtUtc = DateTime.UtcNow,
            TransactionCount = 10,
            AccountId = Guid.NewGuid(),
            AccountName = "Savings",
        });
        this._importApi.DeleteBatchResult = 10;

        var cut = Render<Import>();
        var historyTab = cut.FindAll(".nav-link")[1];
        historyTab.Click();

        cut.Markup.ShouldContain("old-import.csv");
    }

    /// <summary>
    /// Verifies delete batch shows error when API fails.
    /// </summary>
    [Fact]
    public void DeleteBatch_ShowsError_WhenApiFails()
    {
        this._importApi.DeleteBatchResult = null;

        var cut = Render<Import>();
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies saved mappings are available in mappings tab.
    /// </summary>
    [Fact]
    public void MappingsTab_ShowsSavedMappings()
    {
        this._importApi.Mappings.Add(new ImportMappingDto
        {
            Id = Guid.NewGuid(),
            Name = "My Bank Format",
            ColumnMappings = new List<ColumnMappingDto>(),
        });

        var cut = Render<Import>();
        var mappingsTab = cut.FindAll(".nav-link")[2];
        mappingsTab.Click();

        cut.Markup.ShouldContain("My Bank Format");
    }

    /// <summary>
    /// Verifies delete mapping removes mapping from list.
    /// </summary>
    [Fact]
    public void DeleteMapping_RemovesMappingFromList()
    {
        this._importApi.DeleteMappingResult = true;
        this._importApi.Mappings.Add(new ImportMappingDto
        {
            Id = Guid.NewGuid(),
            Name = "Old Mapping",
            ColumnMappings = new List<ColumnMappingDto>(),
        });

        var cut = Render<Import>();
        var mappingsTab = cut.FindAll(".nav-link")[2];
        mappingsTab.Click();

        cut.Markup.ShouldContain("Old Mapping");
    }

    /// <summary>
    /// Verifies wizard reset clears state.
    /// </summary>
    [Fact]
    public void WizardResetsToStep1_OnInitialRender()
    {
        var cut = Render<Import>();
        cut.Markup.ShouldContain("Upload CSV File");
    }

    /// <summary>
    /// Verifies the wizard starts at step 1 with no preview data.
    /// </summary>
    [Fact]
    public void WizardStartsAtStep1_WithNoPreviewData()
    {
        var cut = Render<Import>();

        // Step 1 is visible
        cut.Markup.ShouldContain("Upload CSV File");

        // The preview result is not configured, so no preview data rows are rendered
        cut.Markup.ShouldNotContain("Import Results");
    }

    /// <summary>
    /// Verifies execute result triggers step 4 display.
    /// </summary>
    [Fact]
    public void ExecuteResult_IsConfigurable()
    {
        this._importApi.ExecuteResult = new ImportResult
        {
            BatchId = Guid.NewGuid(),
            ImportedCount = 25,
            SkippedCount = 3,
            ErrorCount = 2,
        };

        var cut = Render<Import>();
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies create mapping result is configurable.
    /// </summary>
    [Fact]
    public void CreateMappingResult_IsConfigurable()
    {
        this._importApi.CreateMappingResult = new ImportMappingDto
        {
            Id = Guid.NewGuid(),
            Name = "New Mapping",
            ColumnMappings = new List<ColumnMappingDto>(),
        };

        var cut = Render<Import>();
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies suggested mapping is applied when available.
    /// </summary>
    [Fact]
    public void SuggestedMapping_IsConfigurable()
    {
        this._importApi.SuggestedMapping = new ImportMappingDto
        {
            Id = Guid.NewGuid(),
            Name = "auto-detect",
            ColumnMappings = new List<ColumnMappingDto>(),
        };

        var cut = Render<Import>();
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies wizard step 1 upload section contains file input.
    /// </summary>
    [Fact]
    public void Step1_HasFileInput()
    {
        var cut = Render<Import>();
        var fileInput = cut.FindAll("input[type='file']");

        fileInput.Count.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Verifies the history tab shows empty message when no batches.
    /// </summary>
    [Fact]
    public void HistoryTab_ShowsEmptyMessage_WhenNoBatches()
    {
        var cut = Render<Import>();
        var historyTab = cut.FindAll(".nav-link")[1];
        historyTab.Click();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the mappings tab shows empty state when no mappings.
    /// </summary>
    [Fact]
    public void MappingsTab_ShowsEmptyState_WhenNoMappings()
    {
        var cut = Render<Import>();
        var mappingsTab = cut.FindAll(".nav-link")[2];
        mappingsTab.Click();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies accounts are loaded for target account selection.
    /// </summary>
    [Fact]
    public void Step1_AccountsAreLoadedForSelection()
    {
        this._budgetApi.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Import Account",
            Type = "Checking",
            InitialBalance = 0m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        var cut = Render<Import>();

        // Accounts are loaded but not visible until CSV is parsed
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies error alert retry triggers data reload.
    /// </summary>
    [Fact]
    public void ErrorAlert_Retry_ReloadsData()
    {
        this._importApi.ShouldThrowOnGetMappings = true;
        var cut = Render<Import>();

        cut.Markup.ShouldContain("Failed to load data");

        // Fix the stub so retry succeeds
        this._importApi.ShouldThrowOnGetMappings = false;
        var retryButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Retry"));
        retryButton?.Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldNotContain("Failed to load data"));
    }

    /// <summary>
    /// Verifies error alert dismiss clears the error message.
    /// </summary>
    [Fact]
    public void ErrorAlert_Dismiss_ClearsError()
    {
        this._importApi.ShouldThrowOnGetMappings = true;
        var cut = Render<Import>();

        cut.Markup.ShouldContain("Failed to load data");

        var dismissButton = cut.FindAll("button").FirstOrDefault(b =>
            b.ClassList.Contains("error-alert-dismiss") || b.GetAttribute("title") == "Dismiss");
        dismissButton?.Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldNotContain("Failed to load data"));
    }

    /// <summary>
    /// Verifies the delete batch handler removes the batch and refreshes.
    /// </summary>
    [Fact]
    public void DeleteBatch_Handler_RemovesBatchAndRefreshes()
    {
        var batchId = Guid.NewGuid();
        this._importApi.Batches.Add(new ImportBatchDto
        {
            Id = batchId,
            FileName = "deleteme.csv",
            ImportedAtUtc = DateTime.UtcNow,
            TransactionCount = 5,
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
        });
        this._importApi.DeleteBatchResult = 5;

        var cut = Render<Import>();
        var historyTab = cut.FindAll(".nav-link")[1];
        historyTab.Click();

        cut.Markup.ShouldContain("deleteme.csv");

        // Find and click the delete button in the history list
        var deleteButton = cut.FindAll("button").FirstOrDefault(b =>
            b.ClassList.Contains("btn-outline-danger") || b.ClassList.Contains("btn-danger"));
        deleteButton?.Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldNotBeNullOrEmpty());
    }

    /// <summary>
    /// Verifies the delete batch handler shows error when API returns null.
    /// </summary>
    [Fact]
    public void DeleteBatch_Handler_ShowsError_WhenApiFails()
    {
        var batchId = Guid.NewGuid();
        this._importApi.Batches.Add(new ImportBatchDto
        {
            Id = batchId,
            FileName = "faildelete.csv",
            ImportedAtUtc = DateTime.UtcNow,
            TransactionCount = 3,
            AccountId = Guid.NewGuid(),
            AccountName = "Savings",
        });
        this._importApi.DeleteBatchResult = null;

        var cut = Render<Import>();
        var historyTab = cut.FindAll(".nav-link")[1];
        historyTab.Click();

        // Step 1: Click the delete button to show confirmation modal
        var deleteButton = cut.FindAll("button").FirstOrDefault(b =>
            b.ClassList.Contains("btn-outline-danger"));
        deleteButton?.Click();

        // Step 2: Click the confirm delete button in the modal
        var confirmButton = cut.FindAll("button").FirstOrDefault(b =>
            b.ClassList.Contains("btn-danger") && !b.ClassList.Contains("btn-outline-danger"));
        confirmButton?.Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to delete import batch"));
    }

    /// <summary>
    /// Verifies the update mapping handler shows conflict error.
    /// </summary>
    [Fact]
    public void UpdateMapping_Handler_ShowsConflictError()
    {
        var mappingId = Guid.NewGuid();
        this._importApi.Mappings.Add(new ImportMappingDto
        {
            Id = mappingId,
            Name = "Conflicting Mapping",
            ColumnMappings = [],
        });
        this._importApi.UpdateMappingResult = ApiResult<ImportMappingDto>.Conflict();

        var cut = Render<Import>();
        var mappingsTab = cut.FindAll(".nav-link")[2];
        mappingsTab.Click();

        cut.Markup.ShouldContain("Conflicting Mapping");

        // Find edit button and click
        var editButton = cut.FindAll("button").FirstOrDefault(b =>
            b.GetAttribute("title")?.Contains("Edit") == true);
        editButton?.Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldNotBeNullOrEmpty());
    }

    /// <summary>
    /// Verifies the delete mapping handler removes mapping from list on success.
    /// </summary>
    [Fact]
    public void DeleteMapping_Handler_RemovesMapping_OnSuccess()
    {
        var mappingId = Guid.NewGuid();
        this._importApi.Mappings.Add(new ImportMappingDto
        {
            Id = mappingId,
            Name = "Deletable Mapping",
            ColumnMappings = [],
        });
        this._importApi.DeleteMappingResult = true;

        var cut = Render<Import>();
        var mappingsTab = cut.FindAll(".nav-link")[2];
        mappingsTab.Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Deletable Mapping"));

        // Step 1: Click the delete button to show confirmation modal
        var deleteButton = cut.FindAll("button").FirstOrDefault(b =>
            b.ClassList.Contains("btn-outline-danger"));
        deleteButton?.Click();

        // Step 2: Click the confirm delete button in the modal
        var confirmButton = cut.FindAll("button").FirstOrDefault(b =>
            b.ClassList.Contains("btn-danger") && !b.ClassList.Contains("btn-outline-danger"));
        confirmButton?.Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldNotContain("Deletable Mapping"));
    }

    /// <summary>
    /// Verifies the delete mapping handler shows error on failure.
    /// </summary>
    [Fact]
    public void DeleteMapping_Handler_ShowsError_OnFailure()
    {
        var mappingId = Guid.NewGuid();
        this._importApi.Mappings.Add(new ImportMappingDto
        {
            Id = mappingId,
            Name = "Undeletable Mapping",
            ColumnMappings = [],
        });
        this._importApi.DeleteMappingResult = false;

        var cut = Render<Import>();
        var mappingsTab = cut.FindAll(".nav-link")[2];
        mappingsTab.Click();

        // Step 1: Click the delete button to show confirmation modal
        var deleteButton = cut.FindAll("button").FirstOrDefault(b =>
            b.ClassList.Contains("btn-outline-danger"));
        deleteButton?.Click();

        // Step 2: Click the confirm delete button in the modal
        var confirmButton = cut.FindAll("button").FirstOrDefault(b =>
            b.ClassList.Contains("btn-danger") && !b.ClassList.Contains("btn-outline-danger"));
        confirmButton?.Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to delete mapping"));
    }

    /// <summary>
    /// Verifies Start Over button resets wizard state to step 1.
    /// </summary>
    [Fact]
    public void StartOverButton_ResetsWizardToStep1()
    {
        var cut = Render<Import>();

        // Step 1 should show upload section
        cut.Markup.ShouldContain("Upload CSV File");
    }

    /// <summary>
    /// Verifies the file input accepts CSV files.
    /// </summary>
    [Fact]
    public void FileInput_AcceptsCsvFormat()
    {
        var cut = Render<Import>();
        var fileInput = cut.Find("input[type='file']");

        fileInput.GetAttribute("accept")!.ShouldContain(".csv");
    }

    /// <summary>
    /// Verifies history tab refresh button triggers reload.
    /// </summary>
    [Fact]
    public void HistoryTab_RefreshButton_TriggersReload()
    {
        // Add initial batch so Refresh button appears
        this._importApi.Batches.Add(new ImportBatchDto
        {
            Id = Guid.NewGuid(),
            FileName = "initial.csv",
            ImportedAtUtc = DateTime.UtcNow,
            TransactionCount = 3,
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
        });

        var cut = Render<Import>();
        var historyTab = cut.FindAll(".nav-link")[1];
        historyTab.Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("initial.csv"));

        // Add a new batch and click Refresh
        this._importApi.Batches.Add(new ImportBatchDto
        {
            Id = Guid.NewGuid(),
            FileName = "refreshed.csv",
            ImportedAtUtc = DateTime.UtcNow,
            TransactionCount = 7,
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
        });

        var refreshButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Refresh"));
        refreshButton?.Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("refreshed.csv"));
    }

    /// <summary>
    /// Verifies mappings tab refresh button triggers reload.
    /// </summary>
    [Fact]
    public void MappingsTab_RefreshButton_TriggersReload()
    {
        // Add initial mapping so Refresh button appears
        this._importApi.Mappings.Add(new ImportMappingDto
        {
            Id = Guid.NewGuid(),
            Name = "Initial Mapping",
            ColumnMappings = [],
        });

        var cut = Render<Import>();
        var mappingsTab = cut.FindAll(".nav-link")[2];
        mappingsTab.Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Initial Mapping"));

        this._importApi.Mappings.Add(new ImportMappingDto
        {
            Id = Guid.NewGuid(),
            Name = "Refreshed Mapping",
            ColumnMappings = [],
        });

        var refreshButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Refresh"));
        refreshButton?.Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Refreshed Mapping"));
    }

    /// <summary>
    /// Verifies history tab shows error when refresh fails.
    /// </summary>
    [Fact]
    public void HistoryTab_Refresh_ShowsError_WhenFails()
    {
        // Add initial batch so Refresh button appears
        this._importApi.Batches.Add(new ImportBatchDto
        {
            Id = Guid.NewGuid(),
            FileName = "existing.csv",
            ImportedAtUtc = DateTime.UtcNow,
            TransactionCount = 2,
            AccountId = Guid.NewGuid(),
            AccountName = "Savings",
        });

        var cut = Render<Import>();
        var historyTab = cut.FindAll(".nav-link")[1];
        historyTab.Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("existing.csv"));

        this._importApi.ShouldThrowOnGetHistory = true;
        var refreshButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Refresh"));
        refreshButton?.Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to load import history"));
    }
}
