// <copyright file="CategorySuggestionServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Categorization;
using BudgetExperiment.Domain;

using Moq;

namespace BudgetExperiment.Application.Tests.Categorization;

/// <summary>
/// Unit tests for the CategorySuggestionService.
/// </summary>
public class CategorySuggestionServiceTests
{
    private readonly Mock<ITransactionRepository> _transactionRepoMock;
    private readonly Mock<IBudgetCategoryRepository> _categoryRepoMock;
    private readonly Mock<ICategorySuggestionRepository> _suggestionRepoMock;
    private readonly Mock<IDismissedSuggestionPatternRepository> _dismissedRepoMock;
    private readonly Mock<IMerchantMappingService> _merchantMappingServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IUserContext> _userContextMock;
    private readonly CategorySuggestionService _service;

    private const string TestOwnerId = "user-123";

    public CategorySuggestionServiceTests()
    {
        _transactionRepoMock = new Mock<ITransactionRepository>();
        _categoryRepoMock = new Mock<IBudgetCategoryRepository>();
        _suggestionRepoMock = new Mock<ICategorySuggestionRepository>();
        _dismissedRepoMock = new Mock<IDismissedSuggestionPatternRepository>();
        _merchantMappingServiceMock = new Mock<IMerchantMappingService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _userContextMock = new Mock<IUserContext>();

        _userContextMock.Setup(u => u.UserId).Returns(TestOwnerId);

        _service = new CategorySuggestionService(
            _transactionRepoMock.Object,
            _categoryRepoMock.Object,
            _suggestionRepoMock.Object,
            _dismissedRepoMock.Object,
            _merchantMappingServiceMock.Object,
            _unitOfWorkMock.Object,
            _userContextMock.Object);
    }

    #region AnalyzeTransactionsAsync Tests

    [Fact]
    public async Task AnalyzeTransactionsAsync_With_No_Uncategorized_Returns_Empty()
    {
        // Arrange
        _transactionRepoMock
            .Setup(r => r.GetUncategorizedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Transaction>());

        // Act
        var result = await _service.AnalyzeTransactionsAsync(CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task AnalyzeTransactionsAsync_Creates_Suggestions_For_Missing_Categories()
    {
        // Arrange
        var account = CreateTestAccount();
        var transactions = new[]
        {
            CreateTransaction(account, "NETFLIX.COM*123", -15.99m),
            CreateTransaction(account, "NETFLIX MONTHLY", -15.99m),
            CreateTransaction(account, "SPOTIFY PREMIUM", -9.99m),
        };

        _transactionRepoMock
            .Setup(r => r.GetUncategorizedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        _categoryRepoMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<BudgetCategory>()); // No categories exist

        _dismissedRepoMock
            .Setup(r => r.IsDismissedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _merchantMappingServiceMock
            .Setup(m => m.FindMatchingPatternsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PatternMatch>
            {
                new PatternMatch
                {
                    Pattern = "netflix",
                    Category = "Entertainment",
                    Icon = "movie",
                    TransactionCount = 2,
                    SampleDescriptions = new List<string> { "NETFLIX.COM*123", "NETFLIX MONTHLY" },
                },
                new PatternMatch
                {
                    Pattern = "spotify",
                    Category = "Entertainment",
                    Icon = "music",
                    TransactionCount = 1,
                    SampleDescriptions = new List<string> { "SPOTIFY PREMIUM" },
                },
            });

        // Act
        var result = await _service.AnalyzeTransactionsAsync(CancellationToken.None);

        // Assert
        Assert.Single(result); // Both Netflix and Spotify map to "Entertainment"
        Assert.Equal("Entertainment", result[0].SuggestedName);
        Assert.Equal(3, result[0].MatchingTransactionCount);
    }

    [Fact]
    public async Task AnalyzeTransactionsAsync_Skips_Existing_Categories()
    {
        // Arrange
        var account = CreateTestAccount();
        var transactions = new[]
        {
            CreateTransaction(account, "NETFLIX.COM*123", -15.99m),
            CreateTransaction(account, "AMAZON.COM*456", -49.99m),
        };

        var existingCategory = CreateTestCategory("Entertainment", CategoryType.Expense);

        _transactionRepoMock
            .Setup(r => r.GetUncategorizedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        _categoryRepoMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { existingCategory });

        _dismissedRepoMock
            .Setup(r => r.IsDismissedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _merchantMappingServiceMock
            .Setup(m => m.FindMatchingPatternsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PatternMatch>
            {
                new PatternMatch { Pattern = "netflix", Category = "Entertainment", Icon = "movie", TransactionCount = 1 },
                new PatternMatch { Pattern = "amazon", Category = "Shopping", Icon = "shopping-cart", TransactionCount = 1 },
            });

        // Act
        var result = await _service.AnalyzeTransactionsAsync(CancellationToken.None);

        // Assert
        Assert.Single(result); // Entertainment already exists, only Shopping suggested
        Assert.Equal("Shopping", result[0].SuggestedName);
    }

    [Fact]
    public async Task AnalyzeTransactionsAsync_Skips_Dismissed_Patterns()
    {
        // Arrange
        var account = CreateTestAccount();
        var transactions = new[]
        {
            CreateTransaction(account, "NETFLIX.COM*123", -15.99m),
        };

        _transactionRepoMock
            .Setup(r => r.GetUncategorizedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        _categoryRepoMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<BudgetCategory>());

        _dismissedRepoMock
            .Setup(r => r.IsDismissedAsync(TestOwnerId, "Entertainment", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _merchantMappingServiceMock
            .Setup(m => m.FindMatchingPatternsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PatternMatch>
            {
                new PatternMatch { Pattern = "netflix", Category = "Entertainment", Icon = "movie", TransactionCount = 1 },
            });

        // Act
        var result = await _service.AnalyzeTransactionsAsync(CancellationToken.None);

        // Assert
        Assert.Empty(result); // Entertainment was dismissed
    }

    [Fact]
    public async Task AnalyzeTransactionsAsync_Saves_Suggestions_To_Repository()
    {
        // Arrange
        var account = CreateTestAccount();
        var transactions = new[] { CreateTransaction(account, "AMAZON.COM*123", -29.99m) };

        _transactionRepoMock
            .Setup(r => r.GetUncategorizedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        _categoryRepoMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<BudgetCategory>());

        _dismissedRepoMock
            .Setup(r => r.IsDismissedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _merchantMappingServiceMock
            .Setup(m => m.FindMatchingPatternsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PatternMatch>
            {
                new PatternMatch { Pattern = "amazon", Category = "Shopping", Icon = "shopping-cart", TransactionCount = 1 },
            });

        // Act
        await _service.AnalyzeTransactionsAsync(CancellationToken.None);

        // Assert
        _suggestionRepoMock.Verify(
            r => r.AddRangeAsync(It.Is<IEnumerable<CategorySuggestion>>(s => s.Count() == 1), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetPendingSuggestionsAsync Tests

    [Fact]
    public async Task GetPendingSuggestionsAsync_Returns_Pending_Suggestions()
    {
        // Arrange
        var suggestion = CategorySuggestion.Create(
            "Entertainment",
            CategoryType.Expense,
            new[] { "netflix" },
            5,
            0.85m,
            TestOwnerId,
            "movie");

        _suggestionRepoMock
            .Setup(r => r.GetPendingByOwnerAsync(TestOwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { suggestion });

        // Act
        var result = await _service.GetPendingSuggestionsAsync(CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Entertainment", result[0].SuggestedName);
    }

    #endregion

    #region AcceptSuggestionAsync Tests

    [Fact]
    public async Task AcceptSuggestionAsync_Creates_Category_And_Updates_Suggestion()
    {
        // Arrange
        var suggestion = CategorySuggestion.Create(
            "Entertainment",
            CategoryType.Expense,
            new[] { "netflix" },
            5,
            0.85m,
            TestOwnerId,
            "movie",
            "#FF5733");

        _suggestionRepoMock
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        // Act
        var result = await _service.AcceptSuggestionAsync(suggestion.Id, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.CreatedCategoryId);
        _categoryRepoMock.Verify(r => r.AddAsync(It.IsAny<BudgetCategory>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(SuggestionStatus.Accepted, suggestion.Status);
    }

    [Fact]
    public async Task AcceptSuggestionAsync_With_Invalid_Id_Returns_Failure()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        _suggestionRepoMock
            .Setup(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategorySuggestion?)null);

        // Act
        var result = await _service.AcceptSuggestionAsync(invalidId, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region DismissSuggestionAsync Tests

    [Fact]
    public async Task DismissSuggestionAsync_Updates_Status_And_Creates_Dismissed_Pattern()
    {
        // Arrange
        var suggestion = CategorySuggestion.Create(
            "Entertainment",
            CategoryType.Expense,
            new[] { "netflix" },
            5,
            0.85m,
            TestOwnerId);

        _suggestionRepoMock
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        _dismissedRepoMock
            .Setup(r => r.IsDismissedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DismissSuggestionAsync(suggestion.Id, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(SuggestionStatus.Dismissed, suggestion.Status);
        _dismissedRepoMock.Verify(
            r => r.AddAsync(It.IsAny<DismissedSuggestionPattern>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static Account CreateTestAccount()
    {
        return Account.Create("Test Account", AccountType.Checking, MoneyValue.Create("USD", 1000m));
    }

    private static Transaction CreateTransaction(Account account, string description, decimal amount)
    {
        return Transaction.Create(
            account.Id,
            MoneyValue.Create("USD", amount),
            DateOnly.FromDateTime(DateTime.UtcNow),
            description,
            null);
    }

    private static BudgetCategory CreateTestCategory(string name, CategoryType type)
    {
        return BudgetCategory.Create(name, type, null, null);
    }

    #endregion
}
