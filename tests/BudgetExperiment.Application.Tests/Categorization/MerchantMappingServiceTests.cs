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
    private readonly FakeBudgetCategoryRepository _fakeCategoryRepository;
    private readonly FakeUnitOfWork _fakeUnitOfWork;
    private readonly MerchantMappingService _service;
    private const string TestOwnerId = "user-123";

    public MerchantMappingServiceTests()
    {
        _fakeRepository = new FakeLearnedMerchantMappingRepository();
        _fakeCategoryRepository = new FakeBudgetCategoryRepository();
        _fakeUnitOfWork = new FakeUnitOfWork();
        _service = new MerchantMappingService(_fakeRepository, _fakeCategoryRepository, _fakeUnitOfWork);
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

    #region LearnFromCategorizationAsync Tests

    [Fact]
    public async Task LearnFromCategorizationAsync_Creates_New_Mapping()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var description = "STARBUCKS COFFEE #123";

        // Act
        await _service.LearnFromCategorizationAsync(TestOwnerId, description, categoryId, CancellationToken.None);

        // Assert
        var mappings = await _fakeRepository.GetByOwnerAsync(TestOwnerId, CancellationToken.None);
        Assert.Single(mappings);
        Assert.Equal(categoryId, mappings[0].CategoryId);
        Assert.Equal(1, _fakeUnitOfWork.SaveCount);
    }

    [Fact]
    public async Task LearnFromCategorizationAsync_Increments_Count_For_Existing_Mapping()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingMapping = LearnedMerchantMapping.Create("STARBUCKS COFFEE", categoryId, TestOwnerId);
        _fakeRepository.AddMapping(existingMapping);

        // Act - Same pattern should increment count
        await _service.LearnFromCategorizationAsync(TestOwnerId, "STARBUCKS COFFEE #456", categoryId, CancellationToken.None);

        // Assert
        var mappings = await _fakeRepository.GetByOwnerAsync(TestOwnerId, CancellationToken.None);
        Assert.Single(mappings);
        Assert.Equal(2, mappings[0].LearnCount);
    }

    [Fact]
    public async Task LearnFromCategorizationAsync_Skips_Empty_Description()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        // Act
        await _service.LearnFromCategorizationAsync(TestOwnerId, "", categoryId, CancellationToken.None);
        await _service.LearnFromCategorizationAsync(TestOwnerId, "  ", categoryId, CancellationToken.None);

        // Assert
        var mappings = await _fakeRepository.GetByOwnerAsync(TestOwnerId, CancellationToken.None);
        Assert.Empty(mappings);
    }

    #endregion

    #region GetLearnedMappingsAsync Tests

    [Fact]
    public async Task GetLearnedMappingsAsync_Returns_All_User_Mappings()
    {
        // Arrange
        var category1Id = Guid.NewGuid();
        var category2Id = Guid.NewGuid();
        _fakeRepository.AddMapping(LearnedMerchantMapping.Create("STARBUCKS", category1Id, TestOwnerId));
        _fakeRepository.AddMapping(LearnedMerchantMapping.Create("MCDONALDS", category2Id, TestOwnerId));
        _fakeCategoryRepository.AddCategoryWithId(category1Id, "Dining");
        _fakeCategoryRepository.AddCategoryWithId(category2Id, "Fast Food");

        // Act
        var result = await _service.GetLearnedMappingsAsync(TestOwnerId, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
    }

    #endregion

    #region DeleteLearnedMappingAsync Tests

    [Fact]
    public async Task DeleteLearnedMappingAsync_Removes_Mapping()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var mapping = LearnedMerchantMapping.Create("STARBUCKS", categoryId, TestOwnerId);
        _fakeRepository.AddMapping(mapping);

        // Act
        var result = await _service.DeleteLearnedMappingAsync(TestOwnerId, mapping.Id, CancellationToken.None);

        // Assert
        Assert.True(result);
        var mappings = await _fakeRepository.GetByOwnerAsync(TestOwnerId, CancellationToken.None);
        Assert.Empty(mappings);
    }

    [Fact]
    public async Task DeleteLearnedMappingAsync_Returns_False_For_Not_Found()
    {
        // Act
        var result = await _service.DeleteLearnedMappingAsync(TestOwnerId, Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.False(result);
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

    private sealed class FakeBudgetCategoryRepository : IBudgetCategoryRepository
    {
        private readonly List<(Guid Id, string Name)> _categoryEntries = new();

        public void AddCategoryWithId(Guid id, string name)
        {
            _categoryEntries.Add((id, name));
        }

        public Task<BudgetCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entry = _categoryEntries.FirstOrDefault(c => c.Id == id);
            if (entry == default)
            {
                return Task.FromResult<BudgetCategory?>(null);
            }

            return Task.FromResult<BudgetCategory?>(CreateCategoryStub(entry.Id, entry.Name));
        }

        public Task<BudgetCategory?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            var entry = _categoryEntries.FirstOrDefault(c => c.Name == name);
            if (entry == default)
            {
                return Task.FromResult<BudgetCategory?>(null);
            }

            return Task.FromResult<BudgetCategory?>(CreateCategoryStub(entry.Id, entry.Name));
        }

        public Task<IReadOnlyList<BudgetCategory>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<BudgetCategory>>(
                _categoryEntries.Select(e => CreateCategoryStub(e.Id, e.Name)).ToList());
        }

        public Task<IReadOnlyList<BudgetCategory>> GetByTypeAsync(CategoryType type, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<BudgetCategory>>(
                _categoryEntries.Select(e => CreateCategoryStub(e.Id, e.Name)).ToList());
        }

        public Task<IReadOnlyList<BudgetCategory>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<BudgetCategory>>(
                _categoryEntries.Select(e => CreateCategoryStub(e.Id, e.Name)).ToList());
        }

        public Task<IReadOnlyList<BudgetCategory>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            var idList = ids.ToList();
            return Task.FromResult<IReadOnlyList<BudgetCategory>>(
                _categoryEntries.Where(e => idList.Contains(e.Id))
                    .Select(e => CreateCategoryStub(e.Id, e.Name)).ToList());
        }

        public Task<IReadOnlyList<BudgetCategory>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<BudgetCategory>>(
                _categoryEntries.Skip(skip).Take(take)
                    .Select(e => CreateCategoryStub(e.Id, e.Name)).ToList());
        }

        public Task<long> CountAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult((long)_categoryEntries.Count);
        }

        public Task AddAsync(BudgetCategory entity, CancellationToken cancellationToken = default)
        {
            _categoryEntries.Add((entity.Id, entity.Name));
            return Task.CompletedTask;
        }

        public Task RemoveAsync(BudgetCategory entity, CancellationToken cancellationToken = default)
        {
            _categoryEntries.RemoveAll(e => e.Id == entity.Id);
            return Task.CompletedTask;
        }

        private static BudgetCategory CreateCategoryStub(Guid id, string name)
        {
            // Use reflection to create a BudgetCategory with a specific ID for testing
            var category = BudgetCategory.Create(name, CategoryType.Expense);

            // Use reflection to set the Id field
            var idProperty = typeof(BudgetCategory).GetProperty("Id");
            idProperty?.SetValue(category, id);

            return category;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
