// <copyright file="CategoryCreateTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests.FunctionalTests;

/// <summary>
/// Critical local E2E test validating the category creation flow (Feature 068).
/// </summary>
[Collection("Playwright")]
public class CategoryCreateTests
{
    private readonly PlaywrightFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryCreateTests"/> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public CategoryCreateTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies creating a category through the UI and confirming it appears in the categories page.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "LocalOnly")]
    [Trait("Category", "LocalCritical")]
    public async Task CreateCategory_ShouldAppearOnCategoriesPage()
    {
        if (!LocalExecutionGuard.IsLocalBaseUrl(_fixture.BaseUrl))
        {
            return;
        }

        var page = _fixture.Page;
        await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

        var categoryName = TestDataHelper.CreateUniqueName("Cat");

        // Navigate to categories page
        await page.GotoAsync($"{_fixture.BaseUrl}/categories");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Open the add category modal
        await page.GetByRole(AriaRole.Button, new()
        {
            Name = "+ Add Category",
        }).ClickAsync();
        await Expect(page.Locator(".modal-dialog", new()
        {
            HasText = "Add Category",
        })).ToBeVisibleAsync();

        // Fill in category details
        await page.Locator("#categoryName").FillAsync(categoryName);
        await page.Locator("#categoryType").SelectOptionAsync("Expense");

        // Submit the form
        await page.GetByRole(AriaRole.Button, new()
        {
            Name = "Create Category",
        }).ClickAsync();

        // Assert the category card appears on the page
        var categoryCard = page.Locator(".card", new()
        {
            HasText = categoryName,
        });
        await Expect(categoryCard).ToBeVisibleAsync(new()
        {
            Timeout = 10000,
        });

        // Verify the category name is displayed
        var cardText = await categoryCard.InnerTextAsync();
        Assert.Contains(categoryName, cardText, StringComparison.Ordinal);
        Assert.Contains("Expense", cardText, StringComparison.Ordinal);

        // Clean up
        await DeleteCategoryAsync(page, categoryName);
    }

    /// <summary>
    /// Verifies a newly created category is selectable in the transaction category picker.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "LocalOnly")]
    [Trait("Category", "LocalCritical")]
    public async Task CreateCategory_ShouldBeSelectableInTransactionForm()
    {
        if (!LocalExecutionGuard.IsLocalBaseUrl(_fixture.BaseUrl))
        {
            return;
        }

        var page = _fixture.Page;
        await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

        var categoryName = TestDataHelper.CreateUniqueName("TxnCat");
        var accountName = TestDataHelper.CreateUniqueName("CatAcct");

        // Create the category
        await page.GotoAsync($"{_fixture.BaseUrl}/categories");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByRole(AriaRole.Button, new()
        {
            Name = "+ Add Category",
        }).ClickAsync();
        await Expect(page.Locator(".modal-dialog", new()
        {
            HasText = "Add Category",
        })).ToBeVisibleAsync();

        await page.Locator("#categoryName").FillAsync(categoryName);
        await page.Locator("#categoryType").SelectOptionAsync("Expense");
        await page.GetByRole(AriaRole.Button, new()
        {
            Name = "Create Category",
        }).ClickAsync();

        await Expect(page.Locator(".card", new()
        {
            HasText = categoryName,
        })).ToBeVisibleAsync(new()
        {
            Timeout = 10000,
        });

        // Create a test account
        await CreateAccountAsync(page, accountName);
        await OpenAccountTransactionsAsync(page, accountName);

        // Open the add transaction modal
        await page.GetByRole(AriaRole.Button, new()
        {
            Name = "+ Add Transaction",
        }).ClickAsync();
        await Expect(page.Locator(".modal-dialog", new()
        {
            HasText = "Add Transaction",
        })).ToBeVisibleAsync();

        // Verify the new category appears in the category select
        var categorySelect = page.Locator("#txnCategory");
        await Expect(categorySelect).ToBeVisibleAsync();

        var categoryOption = categorySelect.Locator("option", new()
        {
            HasText = categoryName,
        });
        await Expect(categoryOption.First).ToBeAttachedAsync(new()
        {
            Timeout = 10000,
        });

        // Close the modal without saving
        await page.Locator(".modal-dialog", new()
        {
            HasText = "Add Transaction",
        })
            .GetByRole(AriaRole.Button, new()
            {
                Name = "Cancel",
            })
            .ClickAsync();

        // Clean up
        await DeleteAccountAsync(page, accountName);
        await DeleteCategoryAsync(page, categoryName);
    }

    private async Task CreateAccountAsync(IPage page, string accountName)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/accounts");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByRole(AriaRole.Button, new()
        {
            Name = "+ Add Account",
        }).ClickAsync();
        await Expect(page.Locator(".modal-dialog", new()
        {
            HasText = "Add Account",
        })).ToBeVisibleAsync();

        await page.Locator("#accountName").FillAsync(accountName);
        await page.Locator("#initialBalance").FillAsync("100.00");
        await page.Locator("#initialBalanceDate").FillAsync(DateTime.UtcNow.ToString("yyyy-MM-dd"));

        await page.Locator(".modal-dialog", new()
        {
            HasText = "Add Account",
        })
            .GetByRole(AriaRole.Button, new()
            {
                Name = "Save",
            })
            .ClickAsync();

        await Expect(page.Locator(".card", new()
        {
            HasText = accountName,
        })).ToBeVisibleAsync(new()
        {
            Timeout = 10000,
        });
    }

    private async Task OpenAccountTransactionsAsync(IPage page, string accountName)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/accounts");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var card = page.Locator(".card", new()
        {
            HasText = accountName,
        });
        await Expect(card).ToBeVisibleAsync(new()
        {
            Timeout = 10000,
        });
        await card.GetByRole(AriaRole.Button, new()
        {
            Name = "Transactions",
        }).ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(page.GetByRole(AriaRole.Button, new()
        {
            Name = "+ Add Transaction",
        })).ToBeVisibleAsync(new()
        {
            Timeout = 10000,
        });
    }

    private async Task DeleteAccountAsync(IPage page, string accountName)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/accounts");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var card = page.Locator(".card", new()
        {
            HasText = accountName,
        });
        if (await card.CountAsync() == 0)
        {
            return;
        }

        await card.GetByRole(AriaRole.Button, new()
        {
            Name = "Delete",
        }).ClickAsync();
        await Expect(page.Locator(".modal-dialog", new()
        {
            HasText = "Delete Account",
        })).ToBeVisibleAsync();

        await page.Locator(".modal-dialog", new()
        {
            HasText = "Delete Account",
        })
            .GetByRole(AriaRole.Button, new()
            {
                Name = "Delete",
            })
            .ClickAsync();

        await Expect(card).Not.ToBeVisibleAsync(new()
        {
            Timeout = 10000,
        });
    }

    private async Task DeleteCategoryAsync(IPage page, string categoryName)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/categories");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var card = page.Locator(".card", new()
        {
            HasText = categoryName,
        });
        if (await card.CountAsync() == 0)
        {
            return;
        }

        // Click the delete button (trash icon) on the category card
        await card.Locator("button[title='Delete category']").ClickAsync();
        await Expect(page.Locator(".modal-dialog", new()
        {
            HasText = "Delete Category",
        })).ToBeVisibleAsync();

        await page.Locator(".modal-dialog", new()
        {
            HasText = "Delete Category",
        })
            .GetByRole(AriaRole.Button, new()
            {
                Name = "Delete",
            })
            .ClickAsync();

        await Expect(card).Not.ToBeVisibleAsync(new()
        {
            Timeout = 10000,
        });
    }
}
