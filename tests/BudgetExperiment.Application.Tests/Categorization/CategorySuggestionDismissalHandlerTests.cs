// <copyright file="CategorySuggestionDismissalHandlerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Categorization;
using BudgetExperiment.Domain;

using Moq;

using Shouldly;

namespace BudgetExperiment.Application.Tests.Categorization;

/// <summary>
/// Unit tests for <see cref="CategorySuggestionDismissalHandler"/>.
/// </summary>
public sealed class CategorySuggestionDismissalHandlerTests
{
    private const string TestOwnerId = "user-123";

    private readonly Mock<ICategorySuggestionRepository> _suggestionRepo = new();
    private readonly Mock<IDismissedSuggestionPatternRepository> _dismissedRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserContext> _userContext = new();

    public CategorySuggestionDismissalHandlerTests()
    {
        _userContext.Setup(u => u.UserId).Returns(TestOwnerId);
    }

    [Fact]
    public async Task DismissSuggestionAsync_NotFound_ReturnsFalse()
    {
        // Arrange
        _suggestionRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategorySuggestion?)null);

        var sut = this.CreateSut();

        // Act
        var result = await sut.DismissSuggestionAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task DismissSuggestionAsync_OwnedByDifferentUser_ReturnsFalse()
    {
        // Arrange
        var suggestion = CategorySuggestion.Create(
            "Entertainment",
            CategoryType.Expense,
            new[] { "netflix" },
            5,
            0.85m,
            "different-user");

        _suggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        var sut = this.CreateSut();

        // Act
        var result = await sut.DismissSuggestionAsync(suggestion.Id);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task DismissSuggestionAsync_PendingSuggestion_DismissesAndCreatesPattern()
    {
        // Arrange
        var suggestion = CategorySuggestion.Create(
            "Entertainment",
            CategoryType.Expense,
            new[] { "netflix" },
            5,
            0.85m,
            TestOwnerId);

        _suggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        _dismissedRepo
            .Setup(r => r.IsDismissedAsync(TestOwnerId, "Entertainment", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = this.CreateSut();

        // Act
        var result = await sut.DismissSuggestionAsync(suggestion.Id);

        // Assert
        result.ShouldBeTrue();
        suggestion.Status.ShouldBe(SuggestionStatus.Dismissed);
        _dismissedRepo.Verify(
            r => r.AddAsync(It.IsAny<DismissedSuggestionPattern>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DismissSuggestionAsync_AlreadyDismissedPattern_DoesNotDuplicate()
    {
        // Arrange
        var suggestion = CategorySuggestion.Create(
            "Entertainment",
            CategoryType.Expense,
            new[] { "netflix" },
            5,
            0.85m,
            TestOwnerId);

        _suggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        _dismissedRepo
            .Setup(r => r.IsDismissedAsync(TestOwnerId, "Entertainment", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = this.CreateSut();

        // Act
        var result = await sut.DismissSuggestionAsync(suggestion.Id);

        // Assert
        result.ShouldBeTrue();
        _dismissedRepo.Verify(
            r => r.AddAsync(It.IsAny<DismissedSuggestionPattern>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RestoreSuggestionAsync_NotFound_ReturnsFalse()
    {
        // Arrange
        _suggestionRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategorySuggestion?)null);

        var sut = this.CreateSut();

        // Act
        var result = await sut.RestoreSuggestionAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task RestoreSuggestionAsync_OwnedByDifferentUser_ReturnsFalse()
    {
        // Arrange
        var suggestion = CategorySuggestion.Create(
            "Entertainment",
            CategoryType.Expense,
            new[] { "netflix" },
            5,
            0.85m,
            "different-user");
        suggestion.Dismiss();

        _suggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        var sut = this.CreateSut();

        // Act
        var result = await sut.RestoreSuggestionAsync(suggestion.Id);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task RestoreSuggestionAsync_DismissedSuggestion_RestoresAndRemovesPattern()
    {
        // Arrange
        var suggestion = CategorySuggestion.Create(
            "Entertainment",
            CategoryType.Expense,
            new[] { "netflix", "hulu" },
            5,
            0.85m,
            TestOwnerId);
        suggestion.Dismiss();

        _suggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        var dismissedPattern = DismissedSuggestionPattern.Create("Entertainment", TestOwnerId);
        _dismissedRepo
            .Setup(r => r.GetByPatternAsync(TestOwnerId, "ENTERTAINMENT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dismissedPattern);

        var sut = this.CreateSut();

        // Act
        var result = await sut.RestoreSuggestionAsync(suggestion.Id);

        // Assert
        result.ShouldBeTrue();
        suggestion.Status.ShouldBe(SuggestionStatus.Pending);
        _dismissedRepo.Verify(
            r => r.RemoveAsync(dismissedPattern, It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClearDismissedPatternsAsync_DelegatesToRepo_ReturnsCount()
    {
        // Arrange
        _dismissedRepo
            .Setup(r => r.ClearByOwnerAsync(TestOwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var sut = this.CreateSut();

        // Act
        var result = await sut.ClearDismissedPatternsAsync();

        // Assert
        result.ShouldBe(3);
        _dismissedRepo.Verify(
            r => r.ClearByOwnerAsync(TestOwnerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private CategorySuggestionDismissalHandler CreateSut()
    {
        return new CategorySuggestionDismissalHandler(
            _suggestionRepo.Object,
            _dismissedRepo.Object,
            _unitOfWork.Object,
            _userContext.Object);
    }
}
