// <copyright file="ImportBatchRepositoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure.Persistence;
using BudgetExperiment.Infrastructure.Persistence.Repositories;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Integration tests for <see cref="ImportBatchRepository"/>.
/// </summary>
[Collection("InMemoryDb")]
public class ImportBatchRepositoryTests
{
    private readonly InMemoryDbFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportBatchRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared in-memory database fixture.</param>
    public ImportBatchRepositoryTests(InMemoryDbFixture fixture)
    {
        this._fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_Persists_Batch()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var account = await this.CreateAccountAsync(context, "Test Account");
        var repository = new ImportBatchRepository(context);
        var userId = Guid.NewGuid();
        var batch = ImportBatch.Create(userId, account.Id, "transactions.csv", 100, mappingId: null);

        // Act
        await repository.AddAsync(batch);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new ImportBatchRepository(verifyContext);
        var retrieved = await verifyRepo.GetByIdAsync(batch.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(batch.Id, retrieved.Id);
        Assert.Equal(userId, retrieved.UserId);
        Assert.Equal(account.Id, retrieved.AccountId);
        Assert.Equal("transactions.csv", retrieved.FileName);
        Assert.Equal(100, retrieved.TotalRows);
        Assert.Equal(ImportBatchStatus.Pending, retrieved.Status);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_Not_Found()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new ImportBatchRepository(context);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUserAsync_Returns_Batches_For_User_Ordered_By_Date()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var account = await this.CreateAccountAsync(context, "Test Account");
        var repository = new ImportBatchRepository(context);
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        var batch1 = ImportBatch.Create(userId1, account.Id, "file1.csv", 10, null);
        var batch2 = ImportBatch.Create(userId1, account.Id, "file2.csv", 20, null);
        var batch3 = ImportBatch.Create(userId2, account.Id, "file3.csv", 30, null);

        await repository.AddAsync(batch1);
        await repository.AddAsync(batch2);
        await repository.AddAsync(batch3);
        await context.SaveChangesAsync();

        // Act
        var results = await repository.GetByUserAsync(userId1);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, b => Assert.Equal(userId1, b.UserId));
    }

    [Fact]
    public async Task GetByAccountAsync_Returns_Batches_For_Account()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var account1 = await this.CreateAccountAsync(context, "Account 1");
        var account2 = await this.CreateAccountAsync(context, "Account 2");
        var repository = new ImportBatchRepository(context);
        var userId = Guid.NewGuid();

        var batch1 = ImportBatch.Create(userId, account1.Id, "file1.csv", 10, null);
        var batch2 = ImportBatch.Create(userId, account1.Id, "file2.csv", 20, null);
        var batch3 = ImportBatch.Create(userId, account2.Id, "file3.csv", 30, null);

        await repository.AddAsync(batch1);
        await repository.AddAsync(batch2);
        await repository.AddAsync(batch3);
        await context.SaveChangesAsync();

        // Act
        var results = await repository.GetByAccountAsync(account1.Id);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, b => Assert.Equal(account1.Id, b.AccountId));
    }

    [Fact]
    public async Task GetByMappingAsync_Returns_Batches_Using_Mapping()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var account = await this.CreateAccountAsync(context, "Test Account");
        var mapping = await this.CreateMappingAsync(context, "Test Mapping");
        var repository = new ImportBatchRepository(context);
        var userId = Guid.NewGuid();

        var batch1 = ImportBatch.Create(userId, account.Id, "file1.csv", 10, mapping.Id);
        var batch2 = ImportBatch.Create(userId, account.Id, "file2.csv", 20, mapping.Id);
        var batch3 = ImportBatch.Create(userId, account.Id, "file3.csv", 30, null);

        await repository.AddAsync(batch1);
        await repository.AddAsync(batch2);
        await repository.AddAsync(batch3);
        await context.SaveChangesAsync();

        // Act
        var results = await repository.GetByMappingAsync(mapping.Id);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, b => Assert.Equal(mapping.Id, b.MappingId));
    }

    [Fact]
    public async Task RemoveAsync_Deletes_Batch()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var account = await this.CreateAccountAsync(context, "Test Account");
        var repository = new ImportBatchRepository(context);
        var userId = Guid.NewGuid();
        var batch = ImportBatch.Create(userId, account.Id, "to-delete.csv", 10, null);

        await repository.AddAsync(batch);
        await context.SaveChangesAsync();

        // Act
        await repository.RemoveAsync(batch);
        await context.SaveChangesAsync();

        // Assert
        var result = await repository.GetByIdAsync(batch.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task CountAsync_Returns_Total_Count()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var account = await this.CreateAccountAsync(context, "Test Account");
        var repository = new ImportBatchRepository(context);
        var userId = Guid.NewGuid();

        await repository.AddAsync(ImportBatch.Create(userId, account.Id, "file1.csv", 10, null));
        await repository.AddAsync(ImportBatch.Create(userId, account.Id, "file2.csv", 20, null));
        await context.SaveChangesAsync();

        // Act
        var count = await repository.CountAsync();

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Complete_Updates_Status_And_Counts()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var account = await this.CreateAccountAsync(context, "Test Account");
        var repository = new ImportBatchRepository(context);
        var userId = Guid.NewGuid();
        var batch = ImportBatch.Create(userId, account.Id, "test.csv", 100, null);

        await repository.AddAsync(batch);
        await context.SaveChangesAsync();

        // Act
        batch.Complete(95, 5, 0);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new ImportBatchRepository(verifyContext);
        var retrieved = await verifyRepo.GetByIdAsync(batch.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(95, retrieved.ImportedCount);
        Assert.Equal(5, retrieved.SkippedCount);
        Assert.Equal(0, retrieved.ErrorCount);
        Assert.Equal(ImportBatchStatus.Completed, retrieved.Status);
    }

    [Fact]
    public async Task MarkDeleted_Updates_Status()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var account = await this.CreateAccountAsync(context, "Test Account");
        var repository = new ImportBatchRepository(context);
        var userId = Guid.NewGuid();
        var batch = ImportBatch.Create(userId, account.Id, "test.csv", 50, null);
        batch.Complete(50, 0, 0);

        await repository.AddAsync(batch);
        await context.SaveChangesAsync();

        // Act
        batch.MarkDeleted();
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new ImportBatchRepository(verifyContext);
        var retrieved = await verifyRepo.GetByIdAsync(batch.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(ImportBatchStatus.Deleted, retrieved.Status);
    }

    [Fact]
    public async Task Batch_With_Mapping_Creates_Valid_Foreign_Key()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var account = await this.CreateAccountAsync(context, "Test Account");
        var mapping = await this.CreateMappingAsync(context, "My Mapping");
        var repository = new ImportBatchRepository(context);
        var userId = Guid.NewGuid();
        var batch = ImportBatch.Create(userId, account.Id, "with-mapping.csv", 50, mapping.Id);

        // Act
        await repository.AddAsync(batch);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new ImportBatchRepository(verifyContext);
        var retrieved = await verifyRepo.GetByIdAsync(batch.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(mapping.Id, retrieved.MappingId);
    }

    private async Task<Account> CreateAccountAsync(BudgetDbContext context, string name)
    {
        var account = Account.Create(name, AccountType.Checking);
        await context.Accounts.AddAsync(account);
        await context.SaveChangesAsync();
        return account;
    }

    private async Task<ImportMapping> CreateMappingAsync(BudgetDbContext context, string name)
    {
        var mappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
        };
        var mapping = ImportMapping.Create(Guid.NewGuid(), name, mappings);
        await context.ImportMappings.AddAsync(mapping);
        await context.SaveChangesAsync();
        return mapping;
    }
}

