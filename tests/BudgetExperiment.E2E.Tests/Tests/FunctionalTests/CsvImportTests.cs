// <copyright file="CsvImportTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests.FunctionalTests;

/// <summary>
/// Functional E2E tests for CSV import workflow.
/// </summary>
[Collection("Playwright")]
public class CsvImportTests
{
    private readonly PlaywrightFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvImportTests"/> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public CsvImportTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies CSV upload, mapping, preview, and import completion end-to-end.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "LocalOnly")]
    public async Task Import_ShouldUploadMapPreviewAndCreateTransactions()
    {
        if (!LocalExecutionGuard.IsLocalBaseUrl(_fixture.BaseUrl))
        {
            return;
        }

        var page = _fixture.Page;
        await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

        var accountName = TestDataHelper.CreateUniqueName("Import");
        await CreateAccountAsync(page, accountName);

        await page.GotoAsync($"{_fixture.BaseUrl}/import");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var csvPath = TestDataHelper.GetSampleCsvPath();
        await page.Locator("input[type='file']").SetInputFilesAsync(csvPath);

        await Expect(page.GetByText("Parsed")).ToBeVisibleAsync(new() { Timeout = 20000 });

        var accountSelect = page.Locator("label:has-text('Select Target Account') + select");
        await Expect(accountSelect).ToBeVisibleAsync();
        await accountSelect.SelectOptionAsync(new SelectOptionValue { Label = accountName });

        await page.GetByRole(AriaRole.Button, new() { Name = "Continue to Mapping" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Map CSV Columns" })).ToBeVisibleAsync(new() { Timeout = 10000 });

        await EnsureRequiredMappingsAsync(page);

        var previewButton = page.GetByRole(AriaRole.Button, new() { Name = "Preview Import" });
        await Expect(previewButton).ToBeEnabledAsync();
        await previewButton.ClickAsync();

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Preview Import" })).ToBeVisibleAsync(new() { Timeout = 15000 });
        await Expect(page.Locator(".import-preview-table")).ToBeVisibleAsync();

        var importButton = page.GetByRole(AriaRole.Button, new() { Name = "Import 1 Transactions" }).Or(page.Locator("button:has-text('Import ')"));
        await Expect(importButton.First).ToBeVisibleAsync();
        await importButton.First.ClickAsync();

        await Expect(page.GetByText("Import Successful!")).ToBeVisibleAsync(new() { Timeout = 20000 });

        await page.GetByRole(AriaRole.Link, new() { Name = "View Transactions" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(page.Locator("table.table")).ToBeVisibleAsync(new() { Timeout = 10000 });
        var rows = await page.Locator("table.table tbody tr").CountAsync();
        Assert.True(rows > 0, "Expected imported transactions to appear in account transactions table.");

        await DeleteAccountAsync(page, accountName);
    }

    private static async Task EnsureRequiredMappingsAsync(IPage page)
    {
        var dateRow = page.Locator(".mapping-row", new() { HasText = "Date" }).First;
        var descriptionRow = page.Locator(".mapping-row", new() { HasText = "Description" }).First;
        var amountRow = page.Locator(".mapping-row", new() { HasText = "Amount" }).First;

        if (await dateRow.CountAsync() > 0)
        {
            await dateRow.Locator("select").SelectOptionAsync("Date");
        }

        if (await descriptionRow.CountAsync() > 0)
        {
            await descriptionRow.Locator("select").SelectOptionAsync("Description");
        }

        if (await amountRow.CountAsync() > 0)
        {
            await amountRow.Locator("select").SelectOptionAsync("Amount");
        }
    }

    private async Task CreateAccountAsync(IPage page, string accountName)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/accounts");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByRole(AriaRole.Button, new() { Name = "+ Add Account" }).ClickAsync();
        await Expect(page.Locator(".modal-dialog", new() { HasText = "Add Account" })).ToBeVisibleAsync();

        await page.Locator("#accountName").FillAsync(accountName);
        await page.Locator("#initialBalance").FillAsync("0");
        await page.Locator("#initialBalanceDate").FillAsync(DateTime.UtcNow.ToString("yyyy-MM-dd"));

        await page.Locator(".modal-dialog", new() { HasText = "Add Account" })
            .GetByRole(AriaRole.Button, new() { Name = "Save" })
            .ClickAsync();

        await Expect(page.Locator(".card", new() { HasText = accountName })).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    private async Task DeleteAccountAsync(IPage page, string accountName)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/accounts");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var card = page.Locator(".card", new() { HasText = accountName });
        if (await card.CountAsync() == 0)
        {
            return;
        }

        await card.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        await Expect(page.Locator(".modal-dialog", new() { HasText = "Delete Account" })).ToBeVisibleAsync();

        await page.Locator(".modal-dialog", new() { HasText = "Delete Account" })
            .GetByRole(AriaRole.Button, new() { Name = "Delete" })
            .ClickAsync();

        await Expect(card).Not.ToBeVisibleAsync(new() { Timeout = 10000 });
    }
}
