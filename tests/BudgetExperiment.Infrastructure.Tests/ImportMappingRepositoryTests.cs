// <copyright file="ImportMappingRepositoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure.Repositories;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Integration tests for <see cref="ImportMappingRepository"/>.
/// </summary>
[Collection("InMemoryDb")]
public class ImportMappingRepositoryTests
{
    private readonly InMemoryDbFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportMappingRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared in-memory database fixture.</param>
    public ImportMappingRepositoryTests(InMemoryDbFixture fixture)
    {
        this._fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_Persists_Mapping()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new ImportMappingRepository(context);
        var userId = Guid.NewGuid();
        var mappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
            new() { ColumnIndex = 1, ColumnHeader = "Description", TargetField = ImportField.Description },
            new() { ColumnIndex = 2, ColumnHeader = "Amount", TargetField = ImportField.Amount },
        };
        var mapping = ImportMapping.Create(userId, "Chase Checking", mappings);

        // Act
        await repository.AddAsync(mapping);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new ImportMappingRepository(verifyContext);
        var retrieved = await verifyRepo.GetByIdAsync(mapping.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(mapping.Id, retrieved.Id);
        Assert.Equal(userId, retrieved.UserId);
        Assert.Equal("Chase Checking", retrieved.Name);
        Assert.Equal(3, retrieved.ColumnMappings.Count);
        Assert.Equal("MM/dd/yyyy", retrieved.DateFormat);
        Assert.Equal(AmountParseMode.NegativeIsExpense, retrieved.AmountMode);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_Not_Found()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new ImportMappingRepository(context);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUserAsync_Returns_Mappings_For_User()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new ImportMappingRepository(context);
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        var mappings1 = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
        };

        var mapping1 = ImportMapping.Create(userId1, "Bank A", mappings1);
        var mapping2 = ImportMapping.Create(userId1, "Bank B", mappings1);
        var mapping3 = ImportMapping.Create(userId2, "Bank C", mappings1);

        await repository.AddAsync(mapping1);
        await repository.AddAsync(mapping2);
        await repository.AddAsync(mapping3);
        await context.SaveChangesAsync();

        // Act
        var results = await repository.GetByUserAsync(userId1);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, m => Assert.Equal(userId1, m.UserId));
    }

    [Fact]
    public async Task GetByNameAsync_Returns_Mapping_By_Name()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new ImportMappingRepository(context);
        var userId = Guid.NewGuid();

        var mappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
        };
        var mapping = ImportMapping.Create(userId, "Wells Fargo Export", mappings);

        await repository.AddAsync(mapping);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByNameAsync(userId, "Wells Fargo Export");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mapping.Id, result.Id);
    }

    [Fact]
    public async Task GetByNameAsync_Returns_Null_For_Other_User()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new ImportMappingRepository(context);
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        var mappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
        };
        var mapping = ImportMapping.Create(userId1, "My Mapping", mappings);

        await repository.AddAsync(mapping);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByNameAsync(userId2, "My Mapping");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveAsync_Deletes_Mapping()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new ImportMappingRepository(context);
        var userId = Guid.NewGuid();

        var mappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
        };
        var mapping = ImportMapping.Create(userId, "To Delete", mappings);

        await repository.AddAsync(mapping);
        await context.SaveChangesAsync();

        // Act
        await repository.RemoveAsync(mapping);
        await context.SaveChangesAsync();

        // Assert
        var result = await repository.GetByIdAsync(mapping.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task CountAsync_Returns_Total_Count()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new ImportMappingRepository(context);
        var userId = Guid.NewGuid();

        var mappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
        };

        await repository.AddAsync(ImportMapping.Create(userId, "Mapping 1", mappings));
        await repository.AddAsync(ImportMapping.Create(userId, "Mapping 2", mappings));
        await repository.AddAsync(ImportMapping.Create(userId, "Mapping 3", mappings));
        await context.SaveChangesAsync();

        // Act
        var count = await repository.CountAsync();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task ColumnMappings_Are_Persisted_Correctly()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new ImportMappingRepository(context);
        var userId = Guid.NewGuid();

        var mappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Trans Date", TargetField = ImportField.Date },
            new() { ColumnIndex = 1, ColumnHeader = "Memo", TargetField = ImportField.Description },
            new() { ColumnIndex = 2, ColumnHeader = "Debit", TargetField = ImportField.DebitAmount },
            new() { ColumnIndex = 3, ColumnHeader = "Credit", TargetField = ImportField.CreditAmount },
            new() { ColumnIndex = 4, ColumnHeader = "Balance", TargetField = ImportField.Ignore },
        };
        var importMapping = ImportMapping.Create(userId, "Complex Mapping", mappings);

        await repository.AddAsync(importMapping);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new ImportMappingRepository(verifyContext);
        var retrieved = await verifyRepo.GetByIdAsync(importMapping.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(5, retrieved.ColumnMappings.Count);
        Assert.Equal("Trans Date", retrieved.ColumnMappings[0].ColumnHeader);
        Assert.Equal(ImportField.Date, retrieved.ColumnMappings[0].TargetField);
        Assert.Equal("Memo", retrieved.ColumnMappings[1].ColumnHeader);
        Assert.Equal(ImportField.Description, retrieved.ColumnMappings[1].TargetField);
        Assert.Equal(ImportField.DebitAmount, retrieved.ColumnMappings[2].TargetField);
        Assert.Equal(ImportField.CreditAmount, retrieved.ColumnMappings[3].TargetField);
        Assert.Equal(ImportField.Ignore, retrieved.ColumnMappings[4].TargetField);
    }

    [Fact]
    public async Task DuplicateSettings_Are_Persisted_Correctly()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new ImportMappingRepository(context);
        var userId = Guid.NewGuid();

        var mappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
        };
        var importMapping = ImportMapping.Create(userId, "Settings Test", mappings);
        importMapping.UpdateDuplicateSettings(new DuplicateDetectionSettings
        {
            Enabled = false,
            LookbackDays = 60,
            DescriptionMatch = DescriptionMatchMode.Fuzzy,
        });

        await repository.AddAsync(importMapping);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new ImportMappingRepository(verifyContext);
        var retrieved = await verifyRepo.GetByIdAsync(importMapping.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved.DuplicateSettings);
        Assert.False(retrieved.DuplicateSettings.Enabled);
        Assert.Equal(60, retrieved.DuplicateSettings.LookbackDays);
        Assert.Equal(DescriptionMatchMode.Fuzzy, retrieved.DuplicateSettings.DescriptionMatch);
    }

    [Fact]
    public async Task Update_Persists_Changes()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new ImportMappingRepository(context);
        var userId = Guid.NewGuid();

        var mappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
        };
        var importMapping = ImportMapping.Create(userId, "Original Name", mappings);
        await repository.AddAsync(importMapping);
        await context.SaveChangesAsync();

        // Act
        var newMappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Transaction Date", TargetField = ImportField.Date },
            new() { ColumnIndex = 1, ColumnHeader = "Desc", TargetField = ImportField.Description },
        };
        importMapping.Update("Updated Name", newMappings, "yyyy-MM-dd", AmountParseMode.PositiveIsExpense);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new ImportMappingRepository(verifyContext);
        var retrieved = await verifyRepo.GetByIdAsync(importMapping.Id);

        Assert.NotNull(retrieved);
        Assert.Equal("Updated Name", retrieved.Name);
        Assert.Equal(2, retrieved.ColumnMappings.Count);
        Assert.Equal("yyyy-MM-dd", retrieved.DateFormat);
        Assert.Equal(AmountParseMode.PositiveIsExpense, retrieved.AmountMode);
    }
}
