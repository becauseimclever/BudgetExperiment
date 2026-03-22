// <copyright file="RecurringChargeDetectionServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Recurring;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Accounts;
using BudgetExperiment.Domain.Common;
using BudgetExperiment.Domain.Identity;
using BudgetExperiment.Domain.Recurring;
using BudgetExperiment.Domain.Repositories;

using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for <see cref="RecurringChargeDetectionService"/>.
/// </summary>
public class RecurringChargeDetectionServiceTests
{
    private static readonly Guid TestUserId = Guid.NewGuid();
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    [Fact]
    public async Task DetectAsync_WithMonthlyPattern_CreatesSuggestion()
    {
        // Arrange
        var (service, _, transactionRepo, suggestionRepo, _, _) = CreateService();
        var transactions = BuildMonthlyTransactions("NETFLIX", -15.99m, 6);

        transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        suggestionRepo
            .Setup(r => r.GetByNormalizedDescriptionAndAccountAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringChargeSuggestion?)null);

        // Act
        var count = await service.DetectAsync(AccountId);

        // Assert
        Assert.True(count > 0);
        suggestionRepo.Verify(
            r => r.AddAsync(
                It.IsAny<RecurringChargeSuggestion>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task DetectAsync_WithExistingSuggestion_UpdatesInsteadOfCreating()
    {
        // Arrange
        var (service, _, transactionRepo, suggestionRepo, _, _) = CreateService();
        var transactions = BuildMonthlyTransactions("SPOTIFY PREMIUM", -9.99m, 4);

        transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        var existingPattern = BuildDetectedPattern("SPOTIFY PREMIUM", -9.99m, 4);
        var existingSuggestion = RecurringChargeSuggestion.Create(
            AccountId,
            existingPattern,
            BudgetScope.Shared,
            TestUserId);

        suggestionRepo
            .Setup(r => r.GetByNormalizedDescriptionAndAccountAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSuggestion);

        // Act
        var count = await service.DetectAsync(AccountId);

        // Assert
        Assert.True(count > 0);
        suggestionRepo.Verify(
            r => r.AddAsync(
                It.IsAny<RecurringChargeSuggestion>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DetectAsync_NoPatterns_ReturnsZero()
    {
        // Arrange
        var (service, _, transactionRepo, _, _, _) = CreateService();
        var transactions = new List<Transaction>
        {
            CreateTransaction("RANDOM PURCHASE 1", -50.00m, DateOnly.FromDateTime(DateTime.UtcNow)),
            CreateTransaction("ANOTHER STORE", -25.00m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30))),
        };

        transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        // Act
        var count = await service.DetectAsync(AccountId);

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetSuggestionsAsync_ReturnsPaginatedResults()
    {
        // Arrange
        var (service, _, _, suggestionRepo, _, _) = CreateService();
        var pattern = BuildDetectedPattern("NETFLIX", -15.99m, 6);
        var suggestion = RecurringChargeSuggestion.Create(
            AccountId,
            pattern,
            BudgetScope.Shared,
            TestUserId);

        suggestionRepo
            .Setup(r => r.GetByStatusAsync(
                AccountId,
                SuggestionStatus.Pending,
                0,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringChargeSuggestion> { suggestion });

        suggestionRepo
            .Setup(r => r.CountByStatusAsync(
                AccountId,
                SuggestionStatus.Pending,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var (items, totalCount) = await service.GetSuggestionsAsync(
            AccountId,
            SuggestionStatus.Pending);

        // Assert
        Assert.Single(items);
        Assert.Equal(1, totalCount);
    }

    [Fact]
    public async Task GetSuggestionByIdAsync_ReturnsSuggestion()
    {
        // Arrange
        var (service, _, _, suggestionRepo, _, _) = CreateService();
        var pattern = BuildDetectedPattern("NETFLIX", -15.99m, 6);
        var suggestion = RecurringChargeSuggestion.Create(
            AccountId,
            pattern,
            BudgetScope.Shared,
            TestUserId);

        suggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        // Act
        var result = await service.GetSuggestionByIdAsync(suggestion.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(suggestion.Id, result.Id);
    }

    [Fact]
    public async Task AcceptAsync_CreateRecurringTransactionAndLinkTransactions()
    {
        // Arrange
        var (service, _, transactionRepo, suggestionRepo, recurringRepo, unitOfWork) = CreateService();
        var pattern = BuildDetectedPattern("NETFLIX", -15.99m, 4);
        var suggestion = RecurringChargeSuggestion.Create(
            AccountId,
            pattern,
            BudgetScope.Shared,
            TestUserId);

        suggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        var matchingTransactions = BuildMonthlyTransactions("NETFLIX", -15.99m, 4);
        transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                AccountId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(matchingTransactions);

        // Act
        var result = await service.AcceptAsync(suggestion.Id);

        // Assert
        Assert.NotEqual(Guid.Empty, result.RecurringTransactionId);
        Assert.Equal(4, result.LinkedTransactionCount);
        recurringRepo.Verify(
            r => r.AddAsync(
                It.IsAny<RecurringTransaction>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcceptAsync_NotFound_ThrowsDomainException()
    {
        // Arrange
        var (service, _, _, suggestionRepo, _, _) = CreateService();
        var nonExistentId = Guid.NewGuid();

        suggestionRepo
            .Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringChargeSuggestion?)null);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(
            () => service.AcceptAsync(nonExistentId));
    }

    [Fact]
    public async Task AcceptAsync_AlreadyAccepted_ThrowsDomainException()
    {
        // Arrange
        var (service, _, _, suggestionRepo, _, _) = CreateService();
        var pattern = BuildDetectedPattern("NETFLIX", -15.99m, 4);
        var suggestion = RecurringChargeSuggestion.Create(
            AccountId,
            pattern,
            BudgetScope.Shared,
            TestUserId);
        suggestion.Accept(Guid.NewGuid());

        suggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(
            () => service.AcceptAsync(suggestion.Id));
    }

    [Fact]
    public async Task DismissAsync_SetsStatusToDismissed()
    {
        // Arrange
        var (service, _, _, suggestionRepo, _, unitOfWork) = CreateService();
        var pattern = BuildDetectedPattern("HULU", -7.99m, 3);
        var suggestion = RecurringChargeSuggestion.Create(
            AccountId,
            pattern,
            BudgetScope.Shared,
            TestUserId);

        suggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        // Act
        await service.DismissAsync(suggestion.Id);

        // Assert
        Assert.Equal(SuggestionStatus.Dismissed, suggestion.Status);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DismissAsync_NotFound_ThrowsDomainException()
    {
        // Arrange
        var (service, _, _, suggestionRepo, _, _) = CreateService();
        var nonExistentId = Guid.NewGuid();

        suggestionRepo
            .Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringChargeSuggestion?)null);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(
            () => service.DismissAsync(nonExistentId));
    }

    private static (
        RecurringChargeDetectionService Service,
        Mock<IUserContext> UserContext,
        Mock<ITransactionRepository> TransactionRepo,
        Mock<IRecurringChargeSuggestionRepository> SuggestionRepo,
        Mock<IRecurringTransactionRepository> RecurringRepo,
        Mock<IUnitOfWork> UnitOfWork) CreateService()
    {
        var transactionRepo = new Mock<ITransactionRepository>();
        var suggestionRepo = new Mock<IRecurringChargeSuggestionRepository>();
        var recurringRepo = new Mock<IRecurringTransactionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var userContext = new Mock<IUserContext>();

        userContext.Setup(u => u.UserIdAsGuid).Returns(TestUserId);
        userContext.Setup(u => u.CurrentScope).Returns((BudgetScope?)null);

        var service = new RecurringChargeDetectionService(
            transactionRepo.Object,
            suggestionRepo.Object,
            recurringRepo.Object,
            unitOfWork.Object,
            userContext.Object);

        return (service, userContext, transactionRepo, suggestionRepo, recurringRepo, unitOfWork);
    }

    private static List<Transaction> BuildMonthlyTransactions(
        string description,
        decimal amount,
        int months)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var transactions = new List<Transaction>();

        for (var i = 0; i < months; i++)
        {
            var date = today.AddMonths(-i);
            transactions.Add(CreateTransaction(description, amount, date));
        }

        return transactions;
    }

    private static Transaction CreateTransaction(
        string description,
        decimal amount,
        DateOnly date)
    {
        return Transaction.Create(
            AccountId,
            MoneyValue.Create("USD", amount),
            date,
            description);
    }

    private static DetectedPattern BuildDetectedPattern(
        string description,
        decimal amount,
        int occurrences)
    {
        var normalized = DescriptionNormalizer.Normalize(description);
        var transactionIds = Enumerable.Range(0, occurrences)
            .Select(_ => Guid.NewGuid())
            .ToList();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return new DetectedPattern(
            NormalizedDescription: normalized,
            SampleDescription: description,
            AverageAmount: MoneyValue.Create("USD", amount),
            Frequency: RecurrenceFrequency.Monthly,
            Interval: 1,
            Confidence: 0.85m,
            MatchingTransactionIds: transactionIds,
            FirstOccurrence: today.AddMonths(-(occurrences - 1)),
            LastOccurrence: today,
            MostUsedCategoryId: null);
    }
}
