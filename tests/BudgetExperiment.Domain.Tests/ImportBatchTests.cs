// <copyright file="ImportBatchTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the ImportBatch entity.
/// </summary>
public class ImportBatchTests
{
    [Fact]
    public void Create_With_Valid_Data_Creates_ImportBatch()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var fileName = "transactions.csv";
        var totalRows = 50;
        var mappingId = Guid.NewGuid();

        // Act
        var batch = ImportBatch.Create(userId, accountId, fileName, totalRows, mappingId);

        // Assert
        Assert.NotEqual(Guid.Empty, batch.Id);
        Assert.Equal(userId, batch.UserId);
        Assert.Equal(accountId, batch.AccountId);
        Assert.Equal(fileName, batch.FileName);
        Assert.Equal(totalRows, batch.TotalRows);
        Assert.Equal(mappingId, batch.MappingId);
        Assert.Equal(0, batch.ImportedCount);
        Assert.Equal(0, batch.SkippedCount);
        Assert.Equal(0, batch.ErrorCount);
        Assert.Equal(ImportBatchStatus.Pending, batch.Status);
        Assert.NotEqual(default, batch.ImportedAtUtc);
    }

    [Fact]
    public void Create_Without_MappingId_Sets_Null()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        // Act
        var batch = ImportBatch.Create(userId, accountId, "test.csv", 10, mappingId: null);

        // Assert
        Assert.Null(batch.MappingId);
    }

    [Fact]
    public void Create_With_Empty_UserId_Throws()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            ImportBatch.Create(Guid.Empty, accountId, "test.csv", 10, null));
        Assert.Contains("user", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Empty_AccountId_Throws()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            ImportBatch.Create(userId, Guid.Empty, "test.csv", 10, null));
        Assert.Contains("account", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Empty_FileName_Throws()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            ImportBatch.Create(userId, accountId, string.Empty, 10, null));
        Assert.Contains("file", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Whitespace_FileName_Throws()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            ImportBatch.Create(userId, accountId, "   ", 10, null));
        Assert.Contains("file", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Zero_TotalRows_Throws()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            ImportBatch.Create(userId, accountId, "test.csv", 0, null));
        Assert.Contains("row", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Negative_TotalRows_Throws()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            ImportBatch.Create(userId, accountId, "test.csv", -5, null));
        Assert.Contains("row", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_Trims_FileName()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        // Act
        var batch = ImportBatch.Create(userId, accountId, "  test.csv  ", 10, null);

        // Assert
        Assert.Equal("test.csv", batch.FileName);
    }

    [Fact]
    public void Create_FileName_Exceeds_MaxLength_Throws()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var longFileName = new string('a', 501) + ".csv"; // Max is 500

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            ImportBatch.Create(userId, accountId, longFileName, 10, null));
        Assert.Contains("500", ex.Message);
    }

    [Fact]
    public void Complete_Sets_Counts_And_Status_Completed()
    {
        // Arrange
        var batch = ImportBatch.Create(Guid.NewGuid(), Guid.NewGuid(), "test.csv", 100, null);

        // Act - no errors means Completed status
        batch.Complete(90, 10, 0);

        // Assert
        Assert.Equal(90, batch.ImportedCount);
        Assert.Equal(10, batch.SkippedCount);
        Assert.Equal(0, batch.ErrorCount);
        Assert.Equal(ImportBatchStatus.Completed, batch.Status);
    }

    [Fact]
    public void Complete_With_Zero_Imported_Still_Completes()
    {
        // Arrange
        var batch = ImportBatch.Create(Guid.NewGuid(), Guid.NewGuid(), "test.csv", 10, null);

        // Act
        batch.Complete(0, 10, 0);

        // Assert
        Assert.Equal(0, batch.ImportedCount);
        Assert.Equal(10, batch.SkippedCount);
        Assert.Equal(ImportBatchStatus.Completed, batch.Status);
    }

    [Fact]
    public void Complete_With_Errors_Sets_PartiallyCompleted()
    {
        // Arrange
        var batch = ImportBatch.Create(Guid.NewGuid(), Guid.NewGuid(), "test.csv", 10, null);

        // Act
        batch.Complete(5, 2, 3);

        // Assert
        Assert.Equal(ImportBatchStatus.PartiallyCompleted, batch.Status);
    }

    [Fact]
    public void Complete_With_All_Errors_Sets_PartiallyCompleted()
    {
        // Arrange
        var batch = ImportBatch.Create(Guid.NewGuid(), Guid.NewGuid(), "test.csv", 10, null);

        // Act
        batch.Complete(0, 0, 10);

        // Assert
        Assert.Equal(ImportBatchStatus.PartiallyCompleted, batch.Status);
    }

    [Fact]
    public void Complete_With_Negative_Imported_Throws()
    {
        // Arrange
        var batch = ImportBatch.Create(Guid.NewGuid(), Guid.NewGuid(), "test.csv", 10, null);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => batch.Complete(-1, 5, 0));
        Assert.Contains("imported", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Complete_With_Negative_Skipped_Throws()
    {
        // Arrange
        var batch = ImportBatch.Create(Guid.NewGuid(), Guid.NewGuid(), "test.csv", 10, null);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => batch.Complete(5, -1, 0));
        Assert.Contains("skipped", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Complete_With_Negative_Errors_Throws()
    {
        // Arrange
        var batch = ImportBatch.Create(Guid.NewGuid(), Guid.NewGuid(), "test.csv", 10, null);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => batch.Complete(5, 0, -1));
        Assert.Contains("error", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MarkDeleted_Sets_Status_To_Deleted()
    {
        // Arrange
        var batch = ImportBatch.Create(Guid.NewGuid(), Guid.NewGuid(), "test.csv", 10, null);
        batch.Complete(10, 0, 0);

        // Act
        batch.MarkDeleted();

        // Assert
        Assert.Equal(ImportBatchStatus.Deleted, batch.Status);
    }

    [Fact]
    public void MarkDeleted_From_Pending_Sets_Deleted()
    {
        // Arrange
        var batch = ImportBatch.Create(Guid.NewGuid(), Guid.NewGuid(), "test.csv", 10, null);

        // Act
        batch.MarkDeleted();

        // Assert
        Assert.Equal(ImportBatchStatus.Deleted, batch.Status);
    }

    [Fact]
    public void MarkDeleted_Already_Deleted_Stays_Deleted()
    {
        // Arrange
        var batch = ImportBatch.Create(Guid.NewGuid(), Guid.NewGuid(), "test.csv", 10, null);
        batch.MarkDeleted();

        // Act
        batch.MarkDeleted();

        // Assert
        Assert.Equal(ImportBatchStatus.Deleted, batch.Status);
    }
}

/// <summary>
/// Unit tests for the ImportBatchStatus enum.
/// </summary>
public class ImportBatchStatusTests
{
    [Theory]
    [InlineData(ImportBatchStatus.Pending, 0)]
    [InlineData(ImportBatchStatus.Completed, 1)]
    [InlineData(ImportBatchStatus.PartiallyCompleted, 2)]
    [InlineData(ImportBatchStatus.Deleted, 3)]
    public void ImportBatchStatus_Has_Expected_Values(ImportBatchStatus status, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)status);
    }
}
