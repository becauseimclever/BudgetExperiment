// <copyright file="EncryptedQuerySemanticsTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Infrastructure.Persistence.Repositories;

using Microsoft.Extensions.Logging.Abstractions;

namespace BudgetExperiment.Infrastructure.Tests.Encryption;

/// <summary>
/// Integration tests that validate repository query behavior when encryption converters are active.
/// </summary>
[Collection("PostgreSqlDb")]
public sealed class EncryptedQuerySemanticsTests
{
    private readonly PostgreSqlFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptedQuerySemanticsTests"/> class.
    /// </summary>
    /// <param name="fixture">Shared PostgreSQL fixture.</param>
    public EncryptedQuerySemanticsTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetUnifiedPagedAsync_WithEncryptedContext_AppliesDescriptionAndAmountFiltersUsingBusinessSemantics()
    {
        // Arrange
        var masterKey = Infrastructure.Encryption.EncryptionService.GenerateSecureKey();
        await using var context = _fixture.CreateEncryptedContext(masterKey);

        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var repository = new TransactionRepository(context, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);

        var account = Account.Create("Unified Semantic Account", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 25m), new DateOnly(2026, 4, 1), "Amazon Prime");
        account.AddTransaction(MoneyValue.Create("USD", 250m), new DateOnly(2026, 4, 2), "Amazon Large Order");
        account.AddTransaction(MoneyValue.Create("USD", 30m), new DateOnly(2026, 4, 3), "Target Order");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedEncryptedContext(context, masterKey);
        var verifyRepository = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);

        var (items, totalCount) = await verifyRepository.GetUnifiedPagedAsync(
            descriptionContains: "amazon",
            minAmount: 10m,
            maxAmount: 100m,
            sortBy: "amount",
            sortDescending: false);

        // Assert
        Assert.Equal(1, totalCount);
        var item = Assert.Single(items);
        Assert.Equal("Amazon Prime", item.Description);
        Assert.Equal(25m, item.Amount.Amount);
    }

    [Fact]
    public async Task GetUnifiedPagedAsync_WithEncryptedContext_SortsByDecryptedAccountName()
    {
        // Arrange
        var masterKey = Infrastructure.Encryption.EncryptionService.GenerateSecureKey();
        await using var context = _fixture.CreateEncryptedContext(masterKey);

        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var zeta = Account.Create("Zeta Account", AccountType.Checking);
        zeta.AddTransaction(MoneyValue.Create("USD", 10m), new DateOnly(2026, 4, 10), "From Zeta");

        var alpha = Account.Create("Alpha Account", AccountType.Checking);
        alpha.AddTransaction(MoneyValue.Create("USD", 20m), new DateOnly(2026, 4, 10), "From Alpha");

        await accountRepo.AddAsync(zeta);
        await accountRepo.AddAsync(alpha);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedEncryptedContext(context, masterKey);
        var verifyRepository = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var (items, _) = await verifyRepository.GetUnifiedPagedAsync(sortBy: "account", sortDescending: false);

        // Assert
        Assert.Equal(2, items.Count);
        Assert.Equal(alpha.Id, items[0].AccountId);
        Assert.Equal(zeta.Id, items[1].AccountId);
    }

    [Fact]
    public async Task GetAllAsync_WithEncryptedContext_OrdersAccountsByDecryptedName()
    {
        // Arrange
        var masterKey = Infrastructure.Encryption.EncryptionService.GenerateSecureKey();
        await using var context = _fixture.CreateEncryptedContext(masterKey);

        var accountRepository = new AccountRepository(context, FakeUserContext.CreateDefault());
        await accountRepository.AddAsync(Account.Create("Zulu", AccountType.Savings));
        await accountRepository.AddAsync(Account.Create("Alpha", AccountType.Checking));
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedEncryptedContext(context, masterKey);
        var verifyRepository = new AccountRepository(verifyContext, FakeUserContext.CreateDefault());
        var accounts = await verifyRepository.GetAllAsync();

        // Assert
        Assert.Equal(2, accounts.Count);
        Assert.Equal("Alpha", accounts[0].Name);
        Assert.Equal("Zulu", accounts[1].Name);
    }

    [Fact]
    public async Task ListPagedAsync_WithEncryptedContext_FiltersByEncryptedPattern()
    {
        // Arrange
        var masterKey = Infrastructure.Encryption.EncryptionService.GenerateSecureKey();
        await using var context = _fixture.CreateEncryptedContext(masterKey);

        var category = BudgetCategory.Create("Utilities", CategoryType.Expense);
        context.BudgetCategories.Add(category);

        var repository = new CategorizationRuleRepository(context);
        await repository.AddAsync(CategorizationRule.Create("Water Bill", RuleMatchType.Contains, "WATER DISTRICT", category.Id, priority: 1));
        await repository.AddAsync(CategorizationRule.Create("Groceries", RuleMatchType.Contains, "SUPERMARKET", category.Id, priority: 2));
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedEncryptedContext(context, masterKey);
        var verifyRepository = new CategorizationRuleRepository(verifyContext);
        var (items, totalCount) = await verifyRepository.ListPagedAsync(
            page: 1,
            pageSize: 25,
            search: "water",
            sortBy: "name",
            sortDirection: "asc");

        // Assert
        Assert.Equal(1, totalCount);
        var item = Assert.Single(items);
        Assert.Equal("Water Bill", item.Name);
        Assert.Equal("WATER DISTRICT", item.Pattern);
    }

    // ── Blocker 1: GetAllDescriptionsAsync prefix filter ─────────────────────
    [Fact]
    public async Task GetAllDescriptionsAsync_WithEncryptedContext_AndPrefix_ReturnsMatchingDecryptedDescriptions()
    {
        // Arrange
        var masterKey = Infrastructure.Encryption.EncryptionService.GenerateSecureKey();
        await using var context = _fixture.CreateEncryptedContext(masterKey);

        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var account = Account.Create("Desc Prefix Account", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 10m), new DateOnly(2026, 4, 1), "Amazon Prime");
        account.AddTransaction(MoneyValue.Create("USD", 20m), new DateOnly(2026, 4, 2), "Amazon Fresh");
        account.AddTransaction(MoneyValue.Create("USD", 30m), new DateOnly(2026, 4, 3), "Netflix");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedEncryptedContext(context, masterKey);
        var repo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var results = await repo.GetAllDescriptionsAsync(searchPrefix: "Amazon");

        // Assert — only the two "Amazon" entries should be returned, not "Netflix"
        Assert.Equal(2, results.Count);
        Assert.All(results, d => Assert.StartsWith("Amazon", d, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAllDescriptionsAsync_WithEncryptedContext_NoPrefix_ReturnsAllDistinctDecryptedDescriptions()
    {
        // Arrange
        var masterKey = Infrastructure.Encryption.EncryptionService.GenerateSecureKey();
        await using var context = _fixture.CreateEncryptedContext(masterKey);

        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var account = Account.Create("Desc All Account", AccountType.Checking);

        // Duplicate description to verify Distinct works over decrypted values
        account.AddTransaction(MoneyValue.Create("USD", 10m), new DateOnly(2026, 4, 1), "Target");
        account.AddTransaction(MoneyValue.Create("USD", 20m), new DateOnly(2026, 4, 2), "Target");
        account.AddTransaction(MoneyValue.Create("USD", 30m), new DateOnly(2026, 4, 3), "Walmart");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedEncryptedContext(context, masterKey);
        var repo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var results = await repo.GetAllDescriptionsAsync();

        // Assert — "Target" appears twice in DB but only once after decrypted Distinct
        Assert.Equal(2, results.Count);
        Assert.Contains("Target", results);
        Assert.Contains("Walmart", results);
    }

    // ── Blocker 2: GetUncategorizedDescriptionsAsync Distinct/OrderBy ─────────
    [Fact]
    public async Task GetUncategorizedDescriptionsAsync_WithEncryptedContext_DeduplicatesByDecryptedDescription()
    {
        // Arrange
        var masterKey = Infrastructure.Encryption.EncryptionService.GenerateSecureKey();
        await using var context = _fixture.CreateEncryptedContext(masterKey);

        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var account = Account.Create("Uncategorized Distinct Account", AccountType.Checking);

        // Three transactions with the same description — AES-GCM produces unique ciphertexts each time,
        // so SQL DISTINCT would return three rows; the fix must reduce this to one.
        account.AddTransaction(MoneyValue.Create("USD", 11m), new DateOnly(2026, 4, 1), "Coffee Shop");
        account.AddTransaction(MoneyValue.Create("USD", 12m), new DateOnly(2026, 4, 2), "Coffee Shop");
        account.AddTransaction(MoneyValue.Create("USD", 13m), new DateOnly(2026, 4, 3), "Coffee Shop");
        account.AddTransaction(MoneyValue.Create("USD", 20m), new DateOnly(2026, 4, 4), "Bookstore");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedEncryptedContext(context, masterKey);
        var repo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var results = await repo.GetUncategorizedDescriptionsAsync();

        // Assert — "Coffee Shop" must appear exactly once despite three distinct ciphertexts
        Assert.Equal(2, results.Count);
        Assert.Single(results, d => d.Equals("Coffee Shop", StringComparison.OrdinalIgnoreCase));
        Assert.Single(results, d => d.Equals("Bookstore", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetUncategorizedDescriptionsAsync_WithEncryptedContext_OrdersByDecryptedDescription()
    {
        // Arrange
        var masterKey = Infrastructure.Encryption.EncryptionService.GenerateSecureKey();
        await using var context = _fixture.CreateEncryptedContext(masterKey);

        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var account = Account.Create("Uncategorized Order Account", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 10m), new DateOnly(2026, 4, 1), "Zara");
        account.AddTransaction(MoneyValue.Create("USD", 20m), new DateOnly(2026, 4, 2), "Apple Store");
        account.AddTransaction(MoneyValue.Create("USD", 30m), new DateOnly(2026, 4, 3), "Microsoft");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedEncryptedContext(context, masterKey);
        var repo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var results = await repo.GetUncategorizedDescriptionsAsync();

        // Assert — alphabetical order by decrypted value, not ciphertext order
        Assert.Equal(3, results.Count);
        Assert.Equal("Apple Store", results[0]);
        Assert.Equal("Microsoft", results[1]);
        Assert.Equal("Zara", results[2]);
    }
}
