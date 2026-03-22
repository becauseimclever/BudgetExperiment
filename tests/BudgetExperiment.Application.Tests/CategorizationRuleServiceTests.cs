// <copyright file="CategorizationRuleServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Categorization;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Repositories;

using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for <see cref="CategorizationRuleService.ListPagedAsync"/>.
/// </summary>
public class CategorizationRuleServiceTests
{
    private static readonly Guid GroceryCategoryId = Guid.NewGuid();

    private readonly Mock<ICategorizationRuleRepository> _repoMock = new();
    private readonly Mock<ICategorizationEngine> _engineMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly CategorizationRuleService _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategorizationRuleServiceTests"/> class.
    /// </summary>
    public CategorizationRuleServiceTests()
    {
        _sut = new CategorizationRuleService(
            _repoMock.Object,
            _engineMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task ListPagedAsync_Returns_Paged_Response()
    {
        // Arrange
        var rules = CreateRules(3);
        _repoMock
            .Setup(r => r.ListPagedAsync(1, 25, null, null, null, null, null, default))
            .ReturnsAsync((rules, 3));

        var request = new CategorizationRuleListRequest { Page = 1, PageSize = 25 };

        // Act
        var result = await _sut.ListPagedAsync(request);

        // Assert
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task ListPagedAsync_Passes_Search_To_Repository()
    {
        // Arrange
        var rules = CreateRules(1);
        _repoMock
            .Setup(r => r.ListPagedAsync(1, 25, "walmart", null, null, null, null, default))
            .ReturnsAsync((rules, 1));

        var request = new CategorizationRuleListRequest { Page = 1, PageSize = 25, Search = "walmart" };

        // Act
        var result = await _sut.ListPagedAsync(request);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task ListPagedAsync_Converts_Active_Status_To_Bool()
    {
        // Arrange
        _repoMock
            .Setup(r => r.ListPagedAsync(1, 25, null, null, true, null, null, default))
            .ReturnsAsync((Array.Empty<CategorizationRule>(), 0));

        var request = new CategorizationRuleListRequest { Page = 1, PageSize = 25, Status = "active" };

        // Act
        await _sut.ListPagedAsync(request);

        // Assert
        _repoMock.Verify(
            r => r.ListPagedAsync(1, 25, null, null, true, null, null, default),
            Times.Once);
    }

    [Fact]
    public async Task ListPagedAsync_Converts_Inactive_Status_To_Bool()
    {
        // Arrange
        _repoMock
            .Setup(r => r.ListPagedAsync(1, 25, null, null, false, null, null, default))
            .ReturnsAsync((Array.Empty<CategorizationRule>(), 0));

        var request = new CategorizationRuleListRequest { Page = 1, PageSize = 25, Status = "inactive" };

        // Act
        await _sut.ListPagedAsync(request);

        // Assert
        _repoMock.Verify(
            r => r.ListPagedAsync(1, 25, null, null, false, null, null, default),
            Times.Once);
    }

    [Fact]
    public async Task ListPagedAsync_Null_Status_Passes_Null_IsActive()
    {
        // Arrange
        _repoMock
            .Setup(r => r.ListPagedAsync(1, 25, null, null, null, null, null, default))
            .ReturnsAsync((Array.Empty<CategorizationRule>(), 0));

        var request = new CategorizationRuleListRequest { Page = 1, PageSize = 25 };

        // Act
        await _sut.ListPagedAsync(request);

        // Assert
        _repoMock.Verify(
            r => r.ListPagedAsync(1, 25, null, null, null, null, null, default),
            Times.Once);
    }

    [Fact]
    public async Task ListPagedAsync_Passes_CategoryId_To_Repository()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _repoMock
            .Setup(r => r.ListPagedAsync(1, 10, null, categoryId, null, null, null, default))
            .ReturnsAsync((Array.Empty<CategorizationRule>(), 0));

        var request = new CategorizationRuleListRequest { Page = 1, PageSize = 10, CategoryId = categoryId };

        // Act
        await _sut.ListPagedAsync(request);

        // Assert
        _repoMock.Verify(
            r => r.ListPagedAsync(1, 10, null, categoryId, null, null, null, default),
            Times.Once);
    }

    [Fact]
    public async Task ListPagedAsync_Maps_Domain_To_Dto()
    {
        // Arrange
        var rule = CategorizationRule.Create("Walmart", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1);
        _repoMock
            .Setup(r => r.ListPagedAsync(1, 25, null, null, null, null, null, default))
            .ReturnsAsync((new[] { rule }, 1));

        var request = new CategorizationRuleListRequest { Page = 1, PageSize = 25 };

        // Act
        var result = await _sut.ListPagedAsync(request);

        // Assert
        var dto = Assert.Single(result.Items);
        Assert.Equal("Walmart", dto.Name);
        Assert.Equal("WALMART", dto.Pattern);
        Assert.Equal("Contains", dto.MatchType);
        Assert.Equal(GroceryCategoryId, dto.CategoryId);
        Assert.Equal(1, dto.Priority);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public async Task ListPagedAsync_Computes_TotalPages_Correctly()
    {
        // Arrange
        var rules = CreateRules(10);
        _repoMock
            .Setup(r => r.ListPagedAsync(1, 10, null, null, null, null, null, default))
            .ReturnsAsync((rules, 25));

        var request = new CategorizationRuleListRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await _sut.ListPagedAsync(request);

        // Assert
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
    }

    private static IReadOnlyList<CategorizationRule> CreateRules(int count)
    {
        var rules = new List<CategorizationRule>();
        for (var i = 0; i < count; i++)
        {
            rules.Add(CategorizationRule.Create(
                $"Rule{i}",
                RuleMatchType.Contains,
                $"PATTERN{i}",
                GroceryCategoryId,
                priority: i + 1));
        }

        return rules;
    }
}
