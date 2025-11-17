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
            Assert.Empty(result.Duplicates);
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

    /// <summary>
    /// Skips duplicate transactions when importing.
    /// </summary>
    [Fact]
    public async Task ImportAsync_DuplicateTransactions_SkipsCorrectly()
    {
        // Arrange
        var csv = @"Date,Description,Amount,Running Bal.
10/01/2025,""Income Transaction"",""100.00"",""100.00""
10/02/2025,""Expense Transaction"",""-50.00"",""50.00""
10/01/2025,""Income Transaction"",""100.00"",""100.00""";

        // Setup fake repo with existing transaction that matches rows 1 and 3
        var existingTransaction = AdhocTransaction.CreateIncome(
            "Income Transaction",
            MoneyValue.Create("USD", 100.00m),
            new DateOnly(2025, 10, 1));

        var readRepo = new FakeAdhocTransactionReadRepositoryWithDuplicates(new[] { existingTransaction });
        var writeRepo = new FakeAdhocTransactionWriteRepository();
        var unitOfWork = new FakeUnitOfWork();
        var parser = new BankOfAmericaCsvParser();
        var service = new CsvImportService(readRepo, writeRepo, unitOfWork, new[] { parser });

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

            // Act
            var result = await service.ImportAsync(stream, BankType.BankOfAmerica, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalRows);
            Assert.Equal(1, result.SuccessfulImports); // Only the expense is new
            Assert.Equal(0, result.FailedImports);
            Assert.Equal(2, result.DuplicatesSkipped); // Rows 1 and 3 are duplicates
            Assert.Empty(result.Errors);
            Assert.Collection(result.Duplicates,
                dup1 =>
                {
                    Assert.Equal(2, dup1.RowNumber); // Row 2 in CSV (accounting for header)
                },
                dup2 =>
                {
                    Assert.Equal(4, dup2.RowNumber); // Row 4 in CSV
                });
            Assert.Single(writeRepo.AddedTransactions); // Only expense added
            Assert.Equal(1, unitOfWork.SaveChangesCallCount);
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

        public Task<IReadOnlyList<AdhocTransaction>> FindDuplicatesAsync(DateOnly date, string description, decimal amount, TransactionType transactionType, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AdhocTransaction>>(Array.Empty<AdhocTransaction>());
            }

    private sealed class FakeAdhocTransactionReadRepositoryWithDuplicates : IAdhocTransactionReadRepository
    {
        private readonly IReadOnlyList<AdhocTransaction> _existingTransactions;

        public FakeAdhocTransactionReadRepositoryWithDuplicates(IEnumerable<AdhocTransaction> existingTransactions)
        {
            this._existingTransactions = existingTransactions.ToList();
        }

        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(this._existingTransactions.Count);

        public Task<AdhocTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(this._existingTransactions.FirstOrDefault(t => t.Id == id));

        public Task<IReadOnlyList<AdhocTransaction>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AdhocTransaction>>(this._existingTransactions.Where(t => t.Date == date).ToList());

        public Task<IReadOnlyList<AdhocTransaction>> ListAsync(int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AdhocTransaction>>(this._existingTransactions.Skip(skip).Take(take).ToList());

        public Task<IReadOnlyList<AdhocTransaction>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AdhocTransaction>>(this._existingTransactions.Where(t => t.Date >= startDate && t.Date <= endDate).ToList());

        public Task<(IReadOnlyList<AdhocTransaction> Items, int Total)> GetIncomeTransactionsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var items = this._existingTransactions.Where(t => t.TransactionType == TransactionType.Income).ToList();
            return Task.FromResult<(IReadOnlyList<AdhocTransaction>, int)>((items, items.Count));
        }

        public Task<(IReadOnlyList<AdhocTransaction> Items, int Total)> GetExpenseTransactionsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var items = this._existingTransactions.Where(t => t.TransactionType == TransactionType.Expense).ToList();
            return Task.FromResult<(IReadOnlyList<AdhocTransaction>, int)>((items, items.Count));
        }

        public Task<IReadOnlyList<AdhocTransaction>> FindDuplicatesAsync(
            DateOnly date,
            string description,
            decimal amount,
            TransactionType transactionType,
            CancellationToken cancellationToken = default)
        {
            // Match based on date, description (case-insensitive), amount (absolute), and type
            var matches = this._existingTransactions.Where(t =>
                t.Date == date &&
                string.Equals(t.Description.Trim(), description.Trim(), StringComparison.OrdinalIgnoreCase) &&
                Math.Abs(t.Money.Amount) == amount &&
                t.TransactionType == transactionType).ToList();

            return Task.FromResult<IReadOnlyList<AdhocTransaction>>(matches);
        }
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
