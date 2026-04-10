// <copyright file="CategorySuggestionServiceAiDiscoveryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Categorization;
using BudgetExperiment.Domain;

using Moq;

namespace BudgetExperiment.Application.Tests.Categorization;

/// <summary>
/// Unit tests for AI category discovery integration in CategorySuggestionService.
/// </summary>
public class CategorySuggestionServiceAiDiscoveryTests
{
    private const string TestOwnerId = "user-123";

    private readonly Mock<ITransactionRepository> _transactionRepoMock;
    private readonly Mock<IBudgetCategoryRepository> _categoryRepoMock;
    private readonly Mock<ICategorySuggestionRepository> _suggestionRepoMock;
    private readonly Mock<IDismissedSuggestionPatternRepository> _dismissedRepoMock;
    private readonly Mock<IMerchantMappingService> _merchantMappingServiceMock;
    private readonly Mock<IAiService> _aiServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IUserContext> _userContextMock;
    private readonly CategorySuggestionService _service;

    public CategorySuggestionServiceAiDiscoveryTests()
    {
        _transactionRepoMock = new Mock<ITransactionRepository>();
        _categoryRepoMock = new Mock<IBudgetCategoryRepository>();
        _suggestionRepoMock = new Mock<ICategorySuggestionRepository>();
        _dismissedRepoMock = new Mock<IDismissedSuggestionPatternRepository>();
        _merchantMappingServiceMock = new Mock<IMerchantMappingService>();
        _aiServiceMock = new Mock<IAiService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _userContextMock = new Mock<IUserContext>();

        _userContextMock.Setup(u => u.UserId).Returns(TestOwnerId);

        _dismissedRepoMock
            .Setup(r => r.IsDismissedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _suggestionRepoMock
            .Setup(r => r.GetByStatusAsync(It.IsAny<string>(), SuggestionStatus.Dismissed, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CategorySuggestion>());

        _service = new CategorySuggestionService(
            _transactionRepoMock.Object,
            _categoryRepoMock.Object,
            _suggestionRepoMock.Object,
            _dismissedRepoMock.Object,
            _merchantMappingServiceMock.Object,
            _aiServiceMock.Object,
            _unitOfWorkMock.Object,
            _userContextMock.Object,
            new Mock<ICategorySuggestionDismissalHandler>().Object);
    }

    [Fact]
    public async Task AnalyzeTransactionsAsync_WithAiAvailable_CreatesAiDiscoveredSuggestions()
    {
        // Arrange
        var account = CreateTestAccount();
        var transactions = new[]
        {
            CreateTransaction(account, "HOME DEPOT #1234", -125.00m),
            CreateTransaction(account, "LOWES #5678", -89.99m),
        };

        SetupUncategorizedTransactions(transactions);
        SetupNoExistingCategories();
        SetupNoPatternMatches();
        SetupAiAvailable();
        SetupAiResponse("""
            [
              {
                "categoryName": "Home Improvement",
                "icon": "🔨",
                "color": "#8B4513",
                "confidence": 0.85,
                "reasoning": "Hardware stores cluster.",
                "matchedDescriptions": ["HOME DEPOT", "LOWES"]
              }
            ]
            """);

        // Act
        var result = await _service.AnalyzeTransactionsAsync(CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Home Improvement", result[0].SuggestedName);
        Assert.Equal(CategorySuggestionSource.AiDiscovered, result[0].Source);
        Assert.Equal("Hardware stores cluster.", result[0].Reasoning);
        Assert.Equal(0.85m, result[0].Confidence);
    }

    [Fact]
    public async Task AnalyzeTransactionsAsync_WithAiUnavailable_ReturnsPatternOnlySuggestions()
    {
        // Arrange
        var account = CreateTestAccount();
        var transactions = new[]
        {
            CreateTransaction(account, "NETFLIX.COM*123", -15.99m),
            CreateTransaction(account, "LOCAL SHOP #99", -25.00m),
        };

        SetupUncategorizedTransactions(transactions);
        SetupNoExistingCategories();
        SetupPatternMatches(new PatternMatch
        {
            Pattern = "netflix",
            Category = "Entertainment",
            Icon = "movie",
            TransactionCount = 1,
        });

        _aiServiceMock.Setup(a => a.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiServiceStatus(false, null, "Not configured"));

        // Act
        var result = await _service.AnalyzeTransactionsAsync(CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Entertainment", result[0].SuggestedName);
        Assert.Equal(CategorySuggestionSource.PatternMatch, result[0].Source);
    }

    [Fact]
    public async Task AnalyzeTransactionsAsync_WhenAiCallFails_ReturnsPatternsOnly()
    {
        // Arrange
        var account = CreateTestAccount();
        var transactions = new[]
        {
            CreateTransaction(account, "NETFLIX.COM*123", -15.99m),
            CreateTransaction(account, "LOCAL SHOP #99", -25.00m),
        };

        SetupUncategorizedTransactions(transactions);
        SetupNoExistingCategories();
        SetupPatternMatches(new PatternMatch
        {
            Pattern = "netflix",
            Category = "Entertainment",
            Icon = "movie",
            TransactionCount = 1,
        });
        SetupAiAvailable();

        _aiServiceMock.Setup(a => a.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(false, string.Empty, "Service error", 0, TimeSpan.Zero));

        // Act
        var result = await _service.AnalyzeTransactionsAsync(CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Entertainment", result[0].SuggestedName);
    }

    [Fact]
    public async Task AnalyzeTransactionsAsync_WhenAiThrows_ReturnsPatternsGracefully()
    {
        // Arrange
        var account = CreateTestAccount();
        var transactions = new[]
        {
            CreateTransaction(account, "NETFLIX.COM*123", -15.99m),
            CreateTransaction(account, "LOCAL SHOP #99", -25.00m),
        };

        SetupUncategorizedTransactions(transactions);
        SetupNoExistingCategories();
        SetupPatternMatches(new PatternMatch
        {
            Pattern = "netflix",
            Category = "Entertainment",
            Icon = "movie",
            TransactionCount = 1,
        });
        SetupAiAvailable();

        _aiServiceMock.Setup(a => a.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection refused"));

        // Act
        var result = await _service.AnalyzeTransactionsAsync(CancellationToken.None);

        // Assert — pattern suggestions still returned despite AI exception
        Assert.Single(result);
        Assert.Equal("Entertainment", result[0].SuggestedName);
    }

    [Fact]
    public async Task AnalyzeTransactionsAsync_MergesPatternAndAiSuggestions()
    {
        // Arrange
        var account = CreateTestAccount();
        var transactions = new[]
        {
            CreateTransaction(account, "NETFLIX.COM*123", -15.99m),
            CreateTransaction(account, "HOME DEPOT #1234", -125.00m),
            CreateTransaction(account, "LOWES #5678", -89.99m),
        };

        SetupUncategorizedTransactions(transactions);
        SetupNoExistingCategories();
        SetupPatternMatches(new PatternMatch
        {
            Pattern = "netflix",
            Category = "Entertainment",
            Icon = "movie",
            TransactionCount = 1,
        });
        SetupAiAvailable();
        SetupAiResponse("""
            [
              {
                "categoryName": "Home Improvement",
                "icon": "🔨",
                "color": "#8B4513",
                "confidence": 0.85,
                "reasoning": "Hardware stores cluster.",
                "matchedDescriptions": ["HOME DEPOT", "LOWES"]
              }
            ]
            """);

        // Act
        var result = await _service.AnalyzeTransactionsAsync(CancellationToken.None);

        // Assert — both pattern and AI suggestions present
        Assert.Equal(2, result.Count);
        Assert.Contains(result, s => s.SuggestedName == "Entertainment" && s.Source == CategorySuggestionSource.PatternMatch);
        Assert.Contains(result, s => s.SuggestedName == "Home Improvement" && s.Source == CategorySuggestionSource.AiDiscovered);
    }

    [Fact]
    public async Task AnalyzeTransactionsAsync_AiSkipsExistingCategories()
    {
        // Arrange
        var account = CreateTestAccount();
        var transactions = new[]
        {
            CreateTransaction(account, "HOME DEPOT #1234", -125.00m),
        };

        var existingCategory = BudgetCategory.Create("Home Improvement", CategoryType.Expense, null, null);

        SetupUncategorizedTransactions(transactions);
        _categoryRepoMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { existingCategory });
        SetupNoPatternMatches();
        SetupAiAvailable();
        SetupAiResponse("""
            [
              {
                "categoryName": "Home Improvement",
                "icon": "🔨",
                "color": "#8B4513",
                "confidence": 0.85,
                "reasoning": "Hardware stores cluster.",
                "matchedDescriptions": ["HOME DEPOT"]
              }
            ]
            """);

        // Act
        var result = await _service.AnalyzeTransactionsAsync(CancellationToken.None);

        // Assert — AI suggestion filtered out because category already exists
        Assert.Empty(result);
    }

    [Fact]
    public async Task AnalyzeTransactionsAsync_AiSkipsNearDuplicateCategories()
    {
        // Arrange
        var account = CreateTestAccount();
        var transactions = new[]
        {
            CreateTransaction(account, "HOME DEPOT #1234", -125.00m),
        };

        var existingCategory = BudgetCategory.Create("Home", CategoryType.Expense, null, null);

        SetupUncategorizedTransactions(transactions);
        _categoryRepoMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { existingCategory });
        SetupNoPatternMatches();
        SetupAiAvailable();
        SetupAiResponse("""
            [
              {
                "categoryName": "Home Improvement",
                "icon": "🔨",
                "color": "#8B4513",
                "confidence": 0.85,
                "reasoning": "Hardware stores cluster.",
                "matchedDescriptions": ["HOME DEPOT"]
              }
            ]
            """);

        // Act
        var result = await _service.AnalyzeTransactionsAsync(CancellationToken.None);

        // Assert — "Home Improvement" contains "Home" → near-duplicate filtered
        Assert.Empty(result);
    }

    [Fact]
    public async Task AnalyzeTransactionsAsync_AllTransactionsMatched_SkipsAiCall()
    {
        // Arrange
        var account = CreateTestAccount();
        var transactions = new[]
        {
            CreateTransaction(account, "NETFLIX.COM*123", -15.99m),
        };

        SetupUncategorizedTransactions(transactions);
        SetupNoExistingCategories();
        SetupPatternMatches(new PatternMatch
        {
            Pattern = "netflix",
            Category = "Entertainment",
            Icon = "movie",
            TransactionCount = 1,
        });
        SetupAiAvailable();

        // Act
        await _service.AnalyzeTransactionsAsync(CancellationToken.None);

        // Assert — AI CompleteAsync was NOT called because all descriptions were matched
        _aiServiceMock.Verify(
            a => a.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AnalyzeTransactionsAsync_AiSuggestionSetsSourceToAiDiscovered()
    {
        // Arrange
        var account = CreateTestAccount();
        var transactions = new[]
        {
            CreateTransaction(account, "PETCO PET SUPPLIES", -45.00m),
            CreateTransaction(account, "PETSMART #2345", -32.00m),
        };

        SetupUncategorizedTransactions(transactions);
        SetupNoExistingCategories();
        SetupNoPatternMatches();
        SetupAiAvailable();
        SetupAiResponse("""
            [
              {
                "categoryName": "Pet Care",
                "icon": "🐾",
                "color": "#4CAF50",
                "confidence": 0.80,
                "reasoning": "Pet stores and supplies.",
                "matchedDescriptions": ["PETCO", "PETSMART"]
              }
            ]
            """);

        // Act
        var result = await _service.AnalyzeTransactionsAsync(CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(CategorySuggestionSource.AiDiscovered, result[0].Source);
        Assert.Equal("Pet stores and supplies.", result[0].Reasoning);
    }

    private static Account CreateTestAccount()
    {
        return Account.Create("Test Account", AccountType.Checking, MoneyValue.Create("USD", 1000m));
    }

    private static Transaction CreateTransaction(Account account, string description, decimal amount)
    {
        return TransactionFactory.Create(
            account.Id,
            MoneyValue.Create("USD", amount),
            DateOnly.FromDateTime(DateTime.UtcNow),
            description,
            null);
    }

    private void SetupUncategorizedTransactions(Transaction[] transactions)
    {
        _transactionRepoMock
            .Setup(r => r.GetUncategorizedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);
    }

    private void SetupNoExistingCategories()
    {
        _categoryRepoMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<BudgetCategory>());
    }

    private void SetupNoPatternMatches()
    {
        _merchantMappingServiceMock
            .Setup(m => m.FindMatchingPatternsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PatternMatch>());
    }

    private void SetupPatternMatches(params PatternMatch[] matches)
    {
        _merchantMappingServiceMock
            .Setup(m => m.FindMatchingPatternsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(matches.ToList());
    }

    private void SetupAiAvailable()
    {
        _aiServiceMock.Setup(a => a.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiServiceStatus(true, "llama3", null));
    }

    private void SetupAiResponse(string jsonContent)
    {
        _aiServiceMock.Setup(a => a.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(true, jsonContent, null, 100, TimeSpan.FromSeconds(1)));
    }
}
