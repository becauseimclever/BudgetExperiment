// <copyright file="MerchantMappingServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Categorization;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Tests.Categorization;

/// <summary>
/// Unit tests for the MerchantMappingService.
/// </summary>
public class MerchantMappingServiceTests
{
    private readonly FakeLearnedMerchantMappingRepository _fakeRepository;
    private readonly MerchantMappingService _service;
    private const string TestOwnerId = "user-123";

    public MerchantMappingServiceTests()
    {
        _fakeRepository = new FakeLearnedMerchantMappingRepository();
        _service = new MerchantMappingService(_fakeRepository);
    }

    #region GetMappingAsync Tests

    [Fact]
    public async Task GetMappingAsync_With_Known_Merchant_Returns_Default_Mapping()
    {
        // Act
        var result = await _service.GetMappingAsync(TestOwnerId, "NETFLIX.COM*123", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Entertainment", result.Value.Category);
        Assert.Equal("movie", result.Value.Icon);
        Assert.False(result.Value.IsLearned);
    }

    [Fact]
    public async Task GetMappingAsync_With_Unknown_Merchant_Returns_Null()
    {
        // Act
        var result = await _service.GetMappingAsync(TestOwnerId, "RANDOM UNKNOWN STORE", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetMappingAsync_Learned_Mapping_Takes_Precedence_Over_Default()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var learnedMapping = LearnedMerchantMapping.Create("NETFLIX", categoryId, TestOwnerId);
        _fakeRepository.AddMapping(learnedMapping);

        // Act
        var result = await _service.GetMappingAsync(TestOwnerId, "NETFLIX.COM", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Value.IsLearned);
        Assert.Equal(categoryId, result.Value.CategoryId);
    }

    [Fact]
    public async Task GetMappingAsync_Is_Case_Insensitive()
    {
        // Act
        var resultLower = await _service.GetMappingAsync(TestOwnerId, "netflix", CancellationToken.None);
        var resultUpper = await _service.GetMappingAsync(TestOwnerId, "NETFLIX", CancellationToken.None);
        var resultMixed = await _service.GetMappingAsync(TestOwnerId, "NeTfLiX", CancellationToken.None);

        // Assert
        Assert.NotNull(resultLower);
        Assert.NotNull(resultUpper);
        Assert.NotNull(resultMixed);
        Assert.Equal(resultLower.Value.Category, resultUpper.Value.Category);
        Assert.Equal(resultLower.Value.Category, resultMixed.Value.Category);
    }

    #endregion

    #region FindMatchingPatternsAsync Tests

    [Fact]
    public async Task FindMatchingPatternsAsync_Returns_Matched_Patterns()
    {
        // Arrange
        var descriptions = new[]
        {
            "NETFLIX.COM*123",
            "SPOTIFY PREMIUM",
            "AMAZON.COM*456",
            "RANDOM STORE",
            "WALMART #123"
        };

        // Act
        var result = await _service.FindMatchingPatternsAsync(TestOwnerId, descriptions, CancellationToken.None);

        // Assert
        Assert.Equal(4, result.Count); // Netflix, Spotify, Amazon, Walmart
        Assert.Contains(result, r => r.Pattern.Contains("netflix", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, r => r.Pattern.Contains("spotify", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, r => r.Pattern.Contains("amazon", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, r => r.Pattern.Contains("walmart", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FindMatchingPatternsAsync_Groups_By_Category()
    {
        // Arrange
        var descriptions = new[]
        {
            "NETFLIX.COM",
            "SPOTIFY",
            "HULU"
        };

        // Act
        var result = await _service.FindMatchingPatternsAsync(TestOwnerId, descriptions, CancellationToken.None);

        // Assert
        var entertainmentPatterns = result.Where(r => r.Category == "Entertainment").ToList();
        Assert.Equal(3, entertainmentPatterns.Count);
    }

    [Fact]
    public async Task FindMatchingPatternsAsync_Returns_Empty_For_No_Matches()
    {
        // Arrange
        var descriptions = new[]
        {
            "RANDOM STORE 1",
            "UNKNOWN MERCHANT",
            "LOCAL BUSINESS"
        };

        // Act
        var result = await _service.FindMatchingPatternsAsync(TestOwnerId, descriptions, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    private sealed class FakeLearnedMerchantMappingRepository : ILearnedMerchantMappingRepository
    {
        private readonly List<LearnedMerchantMapping> _mappings = new();

        public void AddMapping(LearnedMerchantMapping mapping)
        {
            _mappings.Add(mapping);
        }

        public Task<LearnedMerchantMapping?> GetByPatternAsync(string ownerId, string pattern, CancellationToken cancellationToken = default)
        {
            var normalizedPattern = pattern.Trim().ToUpperInvariant();
            var mapping = _mappings.FirstOrDefault(m =>
                m.OwnerId == ownerId &&
                normalizedPattern.Contains(m.MerchantPattern, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(mapping);
        }

        public Task<IReadOnlyList<LearnedMerchantMapping>> GetByOwnerAsync(string ownerId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<LearnedMerchantMapping>>(
                _mappings.Where(m => m.OwnerId == ownerId).ToList());
        }

        public Task<IReadOnlyList<LearnedMerchantMapping>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<LearnedMerchantMapping>>(
                _mappings.Where(m => m.CategoryId == categoryId).ToList());
        }

        public Task<bool> ExistsAsync(string ownerId, string pattern, CancellationToken cancellationToken = default)
        {
            var normalizedPattern = pattern.Trim().ToUpperInvariant();
            return Task.FromResult(_mappings.Any(m =>
                m.OwnerId == ownerId && m.MerchantPattern == normalizedPattern));
        }

        public Task<LearnedMerchantMapping?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_mappings.FirstOrDefault(m => m.Id == id));
        }

        public Task<IReadOnlyList<LearnedMerchantMapping>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<LearnedMerchantMapping>>(
                _mappings.Skip(skip).Take(take).ToList());
        }

        public Task<long> CountAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult((long)_mappings.Count);
        }

        public Task AddAsync(LearnedMerchantMapping entity, CancellationToken cancellationToken = default)
        {
            _mappings.Add(entity);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(LearnedMerchantMapping entity, CancellationToken cancellationToken = default)
        {
            _mappings.Remove(entity);
            return Task.CompletedTask;
        }
    }
}
