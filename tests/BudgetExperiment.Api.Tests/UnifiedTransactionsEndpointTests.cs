// <copyright file="UnifiedTransactionsEndpointTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the unified transaction list endpoints
/// (GET /paged, POST /suggest-categories, PATCH /{id}/category).
/// </summary>
public sealed class UnifiedTransactionsEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedTransactionsEndpointTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public UnifiedTransactionsEndpointTests(CustomWebApplicationFactory factory)
    {
        this._client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/transactions/paged returns 200 OK with default pagination.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPaged_Returns_200_WithDefaultPagination()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/transactions/paged");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UnifiedTransactionPageDto>();
        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(50, result.PageSize);
        Assert.True(result.TotalCount >= 0);
    }

    /// <summary>
    /// GET /api/v1/transactions/paged returns X-Pagination-TotalCount header.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPaged_Returns_PaginationHeader()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/transactions/paged");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Pagination-TotalCount"));
        var headerValue = response.Headers.GetValues("X-Pagination-TotalCount").FirstOrDefault();
        Assert.NotNull(headerValue);
        Assert.True(int.TryParse(headerValue, out _));
    }

    /// <summary>
    /// GET /api/v1/transactions/paged returns created transactions after seeding.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPaged_Returns_SeededTransactions()
    {
        // Arrange — create an account and transactions
        var account = await this.CreateAccountAsync("PagedSeed");
        await this.CreateTransactionAsync(account.Id, -50m, "Groceries at Walmart");
        await this.CreateTransactionAsync(account.Id, -25m, "Gas Station");

        // Act
        var response = await this._client.GetAsync("/api/v1/transactions/paged");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UnifiedTransactionPageDto>();
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 2);
        Assert.True(result.Items.Count >= 2);
        Assert.NotNull(result.Summary);
    }

    /// <summary>
    /// GET /api/v1/transactions/paged respects page and pageSize parameters.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPaged_Respects_PageAndPageSize()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/transactions/paged?page=2&pageSize=25");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UnifiedTransactionPageDto>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Page);
        Assert.Equal(25, result.PageSize);
    }

    /// <summary>
    /// GET /api/v1/transactions/paged filters by account.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPaged_Filters_ByAccount()
    {
        // Arrange — create two accounts with transactions
        var account1 = await this.CreateAccountAsync("FilterAcct1");
        var account2 = await this.CreateAccountAsync("FilterAcct2");
        await this.CreateTransactionAsync(account1.Id, -10m, "Account1 Item");
        await this.CreateTransactionAsync(account2.Id, -20m, "Account2 Item");

        // Act — filter to account1 only
        var response = await this._client.GetAsync($"/api/v1/transactions/paged?accountId={account1.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UnifiedTransactionPageDto>();
        Assert.NotNull(result);
        Assert.All(result.Items, item => Assert.Equal(account1.Id, item.AccountId));
    }

    /// <summary>
    /// GET /api/v1/transactions/paged filters by uncategorized.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPaged_Filters_Uncategorized()
    {
        // Arrange — create account + uncategorized transaction
        var account = await this.CreateAccountAsync("UncatFilter");
        await this.CreateTransactionAsync(account.Id, -15m, "Uncategorized Item");

        // Act
        var response = await this._client.GetAsync("/api/v1/transactions/paged?uncategorized=true");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UnifiedTransactionPageDto>();
        Assert.NotNull(result);
        Assert.All(result.Items, item => Assert.Null(item.CategoryId));
    }

    /// <summary>
    /// GET /api/v1/transactions/paged filters by date range.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPaged_Filters_ByDateRange()
    {
        // Arrange
        var account = await this.CreateAccountAsync("DateRange");
        await this.CreateTransactionAsync(account.Id, -10m, "Jan Item", new DateOnly(2025, 1, 15));
        await this.CreateTransactionAsync(account.Id, -20m, "Mar Item", new DateOnly(2025, 3, 15));

        // Act — filter to January only
        var response = await this._client.GetAsync(
            "/api/v1/transactions/paged?startDate=2025-01-01&endDate=2025-01-31");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UnifiedTransactionPageDto>();
        Assert.NotNull(result);
        Assert.All(result.Items, item =>
        {
            Assert.True(item.Date >= new DateOnly(2025, 1, 1));
            Assert.True(item.Date <= new DateOnly(2025, 1, 31));
        });
    }

    /// <summary>
    /// GET /api/v1/transactions/paged filters by description.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPaged_Filters_ByDescription()
    {
        // Arrange
        var account = await this.CreateAccountAsync("DescFilter");
        await this.CreateTransactionAsync(account.Id, -10m, "UNIQUE_DESC_ALPHA_123");
        await this.CreateTransactionAsync(account.Id, -20m, "Something Else");

        // Act
        var response = await this._client.GetAsync(
            "/api/v1/transactions/paged?description=UNIQUE_DESC_ALPHA_123");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UnifiedTransactionPageDto>();
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.Contains(result.Items, item => item.Description == "UNIQUE_DESC_ALPHA_123");
    }

    /// <summary>
    /// GET /api/v1/transactions/paged returns balance info for single-account filter.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPaged_Returns_BalanceInfo_ForSingleAccount()
    {
        // Arrange
        var account = await this.CreateAccountAsync("BalanceAcct");
        await this.CreateTransactionAsync(account.Id, -30m, "Balance Test Item");

        // Act
        var response = await this._client.GetAsync($"/api/v1/transactions/paged?accountId={account.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UnifiedTransactionPageDto>();
        Assert.NotNull(result);

        // Balance info should be populated when single account is filtered
        Assert.NotNull(result.BalanceInfo);
    }

    /// <summary>
    /// GET /api/v1/transactions/paged returns no balance info when no account filter.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPaged_Returns_NoBalanceInfo_WhenNoAccountFilter()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/transactions/paged");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UnifiedTransactionPageDto>();
        Assert.NotNull(result);
        Assert.Null(result.BalanceInfo);
    }

    /// <summary>
    /// GET /api/v1/transactions/paged accepts all filter parameters.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPaged_Accepts_AllFilterParameters()
    {
        // Act
        var response = await this._client.GetAsync(
            "/api/v1/transactions/paged?" +
            "startDate=2026-01-01&endDate=2026-12-31&" +
            "minAmount=1&maxAmount=500&" +
            "description=test&" +
            "sortBy=amount&sortDescending=false&" +
            "page=1&pageSize=25&" +
            "uncategorized=true");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UnifiedTransactionPageDto>();
        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(25, result.PageSize);
    }

    /// <summary>
    /// GET /api/v1/transactions/paged includes summary statistics.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPaged_Includes_Summary()
    {
        // Arrange
        var account = await this.CreateAccountAsync("SummaryAcct");
        await this.CreateTransactionAsync(account.Id, -100m, "Expense for summary");
        await this.CreateTransactionAsync(account.Id, 200m, "Income for summary");

        // Act
        var response = await this._client.GetAsync("/api/v1/transactions/paged");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UnifiedTransactionPageDto>();
        Assert.NotNull(result);
        Assert.NotNull(result.Summary);
        Assert.True(result.Summary.TotalCount > 0);
    }

    /// <summary>
    /// POST /api/v1/transactions/suggest-categories returns 400 when TransactionIds is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SuggestCategories_Returns_400_WhenEmpty()
    {
        // Arrange
        var request = new BatchSuggestCategoriesRequest { TransactionIds = [] };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/transactions/suggest-categories", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/transactions/suggest-categories returns 400 when exceeding 100 IDs.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SuggestCategories_Returns_400_WhenExceeds100()
    {
        // Arrange — 101 transaction IDs
        var ids = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();
        var request = new BatchSuggestCategoriesRequest { TransactionIds = ids };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/transactions/suggest-categories", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/transactions/suggest-categories returns 200 with empty suggestions for unknown IDs.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SuggestCategories_Returns_200_EmptyForUnknownIds()
    {
        // Arrange
        var request = new BatchSuggestCategoriesRequest
        {
            TransactionIds = [Guid.NewGuid(), Guid.NewGuid()],
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/transactions/suggest-categories", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BatchSuggestCategoriesResponse>();
        Assert.NotNull(result);
        Assert.Empty(result.Suggestions);
    }

    /// <summary>
    /// POST /api/v1/transactions/suggest-categories returns suggestion when rule matches uncategorized transaction.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SuggestCategories_Returns_Suggestion_WhenRuleMatches()
    {
        // Arrange — create transaction FIRST (before rule) so it remains uncategorized,
        // then create the rule that will match it for suggestions
        var account = await this.CreateAccountAsync("SuggestAcct");
        var transaction = await this.CreateTransactionAsync(account.Id, -50m, "WALMART SUPERCENTER #1234");

        var category = await this.CreateCategoryAsync("Suggest Groceries");
        await this.CreateRuleAsync("Suggest Walmart Rule", "WALMART", "Contains", category.Id);

        var request = new BatchSuggestCategoriesRequest
        {
            TransactionIds = [transaction.Id],
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/transactions/suggest-categories", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BatchSuggestCategoriesResponse>();
        Assert.NotNull(result);
        Assert.True(result.Suggestions.ContainsKey(transaction.Id));
        var suggestion = result.Suggestions[transaction.Id];
        Assert.Equal(category.Id, suggestion.CategoryId);
        Assert.Equal("Suggest Groceries", suggestion.CategoryName);
    }

    /// <summary>
    /// POST /api/v1/transactions/suggest-categories skips already-categorized transactions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SuggestCategories_Skips_CategorizedTransactions()
    {
        // Arrange — create transaction FIRST (before rule) so auto-categorization doesn't apply,
        // then create rule, then manually categorize the transaction
        var account = await this.CreateAccountAsync("CatSkipAcct");
        var transaction = await this.CreateTransactionAsync(account.Id, -30m, "CATSKIP STORE");

        var category = await this.CreateCategoryAsync("CatSkip Category");
        await this.CreateRuleAsync("CatSkip Rule", "CATSKIP", "Contains", category.Id);

        // Categorize the transaction
        var categoryUpdate = new TransactionCategoryUpdateDto { CategoryId = category.Id };
        await this._client.PatchAsJsonAsync($"/api/v1/transactions/{transaction.Id}/category", categoryUpdate);

        var request = new BatchSuggestCategoriesRequest
        {
            TransactionIds = [transaction.Id],
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/transactions/suggest-categories", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BatchSuggestCategoriesResponse>();
        Assert.NotNull(result);
        Assert.False(result.Suggestions.ContainsKey(transaction.Id));
    }

    /// <summary>
    /// PATCH /api/v1/transactions/{id}/category returns 200 when assigning category.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PatchCategory_Returns_200_WhenAssigningCategory()
    {
        // Arrange
        var category = await this.CreateCategoryAsync("PatchCat Category");
        var account = await this.CreateAccountAsync("PatchCatAcct");
        var transaction = await this.CreateTransactionAsync(account.Id, -40m, "PatchCat Item");

        var categoryUpdate = new TransactionCategoryUpdateDto { CategoryId = category.Id };

        // Act
        var response = await this._client.PatchAsJsonAsync(
            $"/api/v1/transactions/{transaction.Id}/category",
            categoryUpdate);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(result);
        Assert.Equal(category.Id, result.CategoryId);
    }

    /// <summary>
    /// PATCH /api/v1/transactions/{id}/category returns 404 for non-existent transaction.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PatchCategory_Returns_404_WhenNotFound()
    {
        // Arrange
        var categoryUpdate = new TransactionCategoryUpdateDto { CategoryId = Guid.NewGuid() };

        // Act
        var response = await this._client.PatchAsJsonAsync(
            $"/api/v1/transactions/{Guid.NewGuid()}/category",
            categoryUpdate);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// PATCH /api/v1/transactions/{id}/category clears category when null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PatchCategory_ClearsCategory_WhenNull()
    {
        // Arrange — create and categorize, then clear
        var category = await this.CreateCategoryAsync("ClearCat Category");
        var account = await this.CreateAccountAsync("ClearCatAcct");
        var transaction = await this.CreateTransactionAsync(account.Id, -20m, "ClearCat Item");

        // Assign category first
        var assignDto = new TransactionCategoryUpdateDto { CategoryId = category.Id };
        await this._client.PatchAsJsonAsync(
            $"/api/v1/transactions/{transaction.Id}/category", assignDto);

        // Act — clear category
        var clearDto = new TransactionCategoryUpdateDto { CategoryId = null };
        var response = await this._client.PatchAsJsonAsync(
            $"/api/v1/transactions/{transaction.Id}/category", clearDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(result);
        Assert.Null(result.CategoryId);
    }

    /// <summary>
    /// PATCH /api/v1/transactions/{id}/category returns ETag header.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PatchCategory_Returns_ETag()
    {
        // Arrange
        var category = await this.CreateCategoryAsync("ETagCat Category");
        var account = await this.CreateAccountAsync("ETagCatAcct");
        var transaction = await this.CreateTransactionAsync(account.Id, -10m, "ETagCat Item");

        var categoryUpdate = new TransactionCategoryUpdateDto { CategoryId = category.Id };

        // Act
        var response = await this._client.PatchAsJsonAsync(
            $"/api/v1/transactions/{transaction.Id}/category", categoryUpdate);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        Assert.False(string.IsNullOrEmpty(response.Headers.ETag.Tag));
    }

    /// <summary>
    /// PATCH /api/v1/transactions/{id}/category with valid If-Match succeeds.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PatchCategory_With_Valid_IfMatch_Succeeds()
    {
        // Arrange
        var category = await this.CreateCategoryAsync("IfMatchCat");
        var account = await this.CreateAccountAsync("IfMatchCatAcct");
        var transaction = await this.CreateTransactionAsync(account.Id, -15m, "IfMatchCat Item");

        // Get ETag
        var getResponse = await this._client.GetAsync($"/api/v1/transactions/{transaction.Id}");
        var etag = getResponse.Headers.ETag;

        var categoryUpdate = new TransactionCategoryUpdateDto { CategoryId = category.Id };
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/transactions/{transaction.Id}/category")
        {
            Content = JsonContent.Create(categoryUpdate),
        };
        request.Headers.IfMatch.Add(etag!);

        // Act
        var response = await this._client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// PATCH /api/v1/transactions/{id}/category with stale If-Match returns 409.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PatchCategory_With_Stale_IfMatch_Returns_409()
    {
        // Arrange
        var category = await this.CreateCategoryAsync("StaleCat");
        var account = await this.CreateAccountAsync("StaleCatAcct");
        var transaction = await this.CreateTransactionAsync(account.Id, -15m, "StaleCat Item");

        var staleETag = new EntityTagHeaderValue("\"99999999\"");
        var categoryUpdate = new TransactionCategoryUpdateDto { CategoryId = category.Id };
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/transactions/{transaction.Id}/category")
        {
            Content = JsonContent.Create(categoryUpdate),
        };
        request.Headers.IfMatch.Add(staleETag);

        // Act
        var response = await this._client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<AccountDto> CreateAccountAsync(string name)
    {
        var dto = new AccountCreateDto { Name = name, Type = "Checking" };
        var response = await this._client.PostAsJsonAsync("/api/v1/accounts", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(result);
        return result;
    }

    private async Task<TransactionDto> CreateTransactionAsync(
        Guid accountId,
        decimal amount,
        string description,
        DateOnly? date = null)
    {
        var dto = new TransactionCreateDto
        {
            AccountId = accountId,
            Amount = new MoneyDto { Currency = "USD", Amount = amount },
            Date = date ?? new DateOnly(2026, 3, 1),
            Description = description,
        };
        var response = await this._client.PostAsJsonAsync("/api/v1/transactions", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(result);
        return result;
    }

    private async Task<BudgetCategoryDto> CreateCategoryAsync(string name)
    {
        var dto = new BudgetCategoryCreateDto { Name = name, Type = "Expense" };
        var response = await this._client.PostAsJsonAsync("/api/v1/categories", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BudgetCategoryDto>();
        Assert.NotNull(result);
        return result;
    }

    private async Task<CategorizationRuleDto> CreateRuleAsync(
        string name,
        string pattern,
        string matchType,
        Guid categoryId)
    {
        var dto = new CategorizationRuleCreateDto
        {
            Name = name,
            Pattern = pattern,
            MatchType = matchType,
            CaseSensitive = false,
            CategoryId = categoryId,
        };
        var response = await this._client.PostAsJsonAsync("/api/v1/categorizationrules", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CategorizationRuleDto>();
        Assert.NotNull(result);
        return result;
    }
}
