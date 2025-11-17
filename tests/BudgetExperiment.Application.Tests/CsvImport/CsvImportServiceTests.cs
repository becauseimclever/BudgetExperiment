// <copyright file="CsvImportServiceTests.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

using System.Text;

using BudgetExperiment.Application.CsvImport;
using BudgetExperiment.Application.CsvImport.Parsers;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Tests.CsvImport;

/// <summary>
/// Unit tests for CSV import service.
/// </summary>
public sealed class CsvImportServiceTests
{
    /// <summary>
    /// Imports valid CSV and creates all transactions.
    /// </summary>
    [Fact]
    public async Task ImportAsync_ValidCsv_CreatesAllTransactions()
    {
        // Arrange
        var csv = @"Date,Description,Amount,Running Bal.
10/01/2025,""Income Transaction"",""100.00"",""100.00""
10/02/2025,""Expense Transaction"",""-50.00"",""50.00""";

        var readRepo = new FakeAdhocTransactionReadRepository();
        var writeRepo = new FakeAdhocTransactionWriteRepository();
        var unitOfWork = new FakeUnitOfWork();
        var parser = new BankOfAmericaCsvParser();
        var service = new CsvImportService(readRepo, writeRepo, unitOfWork, new[] { parser });

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await service.ImportAsync(stream, BankType.BankOfAmerica, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.SuccessfulImports);
        Assert.Equal(0, result.FailedImports);
        Assert.Equal(0, result.DuplicatesSkipped);
        Assert.Empty(result.Errors);
        Assert.Equal(2, writeRepo.AddedTransactions.Count);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    /// <summary>
    /// Throws ArgumentException for unsupported bank type.
    /// </summary>
    [Fact]
    public async Task ImportAsync_UnsupportedBankType_ThrowsArgumentException()
    {
        // Arrange
        var csv = @"Date,Description,Amount,Running Bal.";

        var readRepo = new FakeAdhocTransactionReadRepository();
        var writeRepo = new FakeAdhocTransactionWriteRepository();
        var unitOfWork = new FakeUnitOfWork();
        var parser = new BankOfAmericaCsvParser();
        var service = new CsvImportService(readRepo, writeRepo, unitOfWork, new[] { parser });

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act & Assert - CapitalOne parser not registered
        await Assert.ThrowsAsync<ArgumentException>(() => service.ImportAsync(stream, BankType.CapitalOne, CancellationToken.None));
    }

    /// <summary>
    /// Returns empty result for file with no transactions.
    /// </summary>
    [Fact]
    public async Task ImportAsync_EmptyFile_ReturnsEmptyResult()
    {
        // Arrange
        var csv = @"Date,Description,Amount,Running Bal.";

        var readRepo = new FakeAdhocTransactionReadRepository();
        var writeRepo = new FakeAdhocTransactionWriteRepository();
        var unitOfWork = new FakeUnitOfWork();
        var parser = new BankOfAmericaCsvParser();
        var service = new CsvImportService(readRepo, writeRepo, unitOfWork, new[] { parser });

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await service.ImportAsync(stream, BankType.BankOfAmerica, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.TotalRows);
        Assert.Equal(0, result.SuccessfulImports);
        Assert.Equal(0, result.FailedImports);
        Assert.Empty(writeRepo.AddedTransactions);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount); // No save if no transactions
    }

    /// <summary>
    /// Parser throws exception on invalid data (partial failure handling is Phase 1 simplification).
    /// </summary>
    [Fact]
    public async Task ImportAsync_InvalidData_ThrowsDomainException()
    {
        // Arrange - invalid date will cause parser to throw
        var csv = @"Date,Description,Amount,Running Bal.
10/01/2025,""Valid Transaction"",""100.00"",""100.00""
99/99/9999,""Invalid Date"",""50.00"",""150.00""";

        var readRepo = new FakeAdhocTransactionReadRepository();
        var writeRepo = new FakeAdhocTransactionWriteRepository();
        var unitOfWork = new FakeUnitOfWork();
        var parser = new BankOfAmericaCsvParser();
        var service = new CsvImportService(readRepo, writeRepo, unitOfWork, new[] { parser });

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act & Assert - Parser throws on first invalid row
        await Assert.ThrowsAsync<DomainException>(() => service.ImportAsync(stream, BankType.BankOfAmerica, CancellationToken.None));
    }

    /// <summary>
    /// Handles large files with many transactions.
    /// </summary>
    [Fact]
    public async Task ImportAsync_LargeFile_HandlesPerformanceGracefully()
    {
        // Arrange - 100 transactions
        var csvBuilder = new StringBuilder();
        csvBuilder.AppendLine("Date,Description,Amount,Running Bal.");
        for (int i = 1; i <= 100; i++)
        {
            csvBuilder.AppendLine($"10/{i % 28 + 1:D2}/2025,\"Transaction {i}\",\"-{i}.00\",\"1000.00\"");
        }

        var csv = csvBuilder.ToString();
        var readRepo = new FakeAdhocTransactionReadRepository();
        var writeRepo = new FakeAdhocTransactionWriteRepository();
        var unitOfWork = new FakeUnitOfWork();
        var parser = new BankOfAmericaCsvParser();
        var service = new CsvImportService(readRepo, writeRepo, unitOfWork, new[] { parser });

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await service.ImportAsync(stream, BankType.BankOfAmerica, CancellationToken.None);

        // Assert
        Assert.Equal(100, result.TotalRows);
        Assert.Equal(100, result.SuccessfulImports);
        Assert.Equal(0, result.FailedImports);
        Assert.Equal(100, writeRepo.AddedTransactions.Count);
    }

    // Fake implementations for testing
    private sealed class FakeAdhocTransactionReadRepository : IAdhocTransactionReadRepository
    {
        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<AdhocTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<AdhocTransaction?>(null);

        public Task<IReadOnlyList<AdhocTransaction>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AdhocTransaction>>(Array.Empty<AdhocTransaction>());

        public Task<IReadOnlyList<AdhocTransaction>> ListAsync(int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AdhocTransaction>>(Array.Empty<AdhocTransaction>());

        public Task<IReadOnlyList<AdhocTransaction>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AdhocTransaction>>(Array.Empty<AdhocTransaction>());

        public Task<(IReadOnlyList<AdhocTransaction> Items, int Total)> GetIncomeTransactionsAsync(int page, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<AdhocTransaction>, int)>((Array.Empty<AdhocTransaction>(), 0));

        public Task<(IReadOnlyList<AdhocTransaction> Items, int Total)> GetExpenseTransactionsAsync(int page, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<AdhocTransaction>, int)>((Array.Empty<AdhocTransaction>(), 0));
    }

    private sealed class FakeAdhocTransactionWriteRepository : IAdhocTransactionWriteRepository
    {
        public List<AdhocTransaction> AddedTransactions { get; } = new List<AdhocTransaction>();

        public Task AddAsync(AdhocTransaction entity, CancellationToken cancellationToken = default)
        {
            this.AddedTransactions.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(AdhocTransaction entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RemoveAsync(AdhocTransaction entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            this.SaveChangesCallCount++;
            return Task.FromResult(1);
        }
    }
}
