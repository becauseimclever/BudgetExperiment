// <copyright file="ImportServiceValidationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Import;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Tests.Services;

/// <summary>
/// Unit tests for import execute request validation in <see cref="ImportExecuteRequestValidator"/>.
/// </summary>
public class ImportServiceValidationTests
{
    [Fact]
    public void Validate_WithTooManyTransactions_ReturnsError()
    {
        // Arrange
        var transactions = Enumerable.Range(0, ImportValidationConstants.MaxTransactionsPerImport + 1)
            .Select(i => new ImportTransactionData
            {
                Date = new DateOnly(2026, 1, 15),
                Description = $"Transaction {i}",
                Amount = -10m,
            })
            .ToList();

        var request = new ImportExecuteRequest
        {
            AccountId = Guid.NewGuid(),
            FileName = "test.csv",
            Transactions = transactions,
        };

        // Act
        var result = ImportExecuteRequestValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("exceeds maximum", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, e => e.Contains("5000", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WithEmptyTransactions_ReturnsError()
    {
        // Arrange
        var request = new ImportExecuteRequest
        {
            AccountId = Guid.NewGuid(),
            FileName = "test.csv",
            Transactions = [],
        };

        // Act
        var result = ImportExecuteRequestValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("No transactions", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithDescriptionTooLong_ReturnsError()
    {
        // Arrange
        var request = new ImportExecuteRequest
        {
            AccountId = Guid.NewGuid(),
            FileName = "test.csv",
            Transactions =
            [
                new ImportTransactionData
                {
                    Date = new DateOnly(2026, 1, 15),
                    Description = new string('A', ImportValidationConstants.MaxDescriptionLength + 1),
                    Amount = -50m,
                },
            ],
        };

        // Act
        var result = ImportExecuteRequestValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Description exceeds", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, e => e.Contains("row 1", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithDateTooFarInFuture_ReturnsError()
    {
        // Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(ImportValidationConstants.MaxFutureDateDays + 1);
        var request = new ImportExecuteRequest
        {
            AccountId = Guid.NewGuid(),
            FileName = "test.csv",
            Transactions =
            [
                new ImportTransactionData
                {
                    Date = futureDate,
                    Description = "Future transaction",
                    Amount = -50m,
                },
            ],
        };

        // Act
        var result = ImportExecuteRequestValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("too far in the future", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, e => e.Contains("row 1", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithAmountOutOfRange_ReturnsError()
    {
        // Arrange
        var request = new ImportExecuteRequest
        {
            AccountId = Guid.NewGuid(),
            FileName = "test.csv",
            Transactions =
            [
                new ImportTransactionData
                {
                    Date = new DateOnly(2026, 1, 15),
                    Description = "Big purchase",
                    Amount = ImportValidationConstants.MaxAmountAbsoluteValue + 1m,
                },
            ],
        };

        // Act
        var result = ImportExecuteRequestValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Amount out of range", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, e => e.Contains("row 1", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithNegativeAmountOutOfRange_ReturnsError()
    {
        // Arrange
        var request = new ImportExecuteRequest
        {
            AccountId = Guid.NewGuid(),
            FileName = "test.csv",
            Transactions =
            [
                new ImportTransactionData
                {
                    Date = new DateOnly(2026, 1, 15),
                    Description = "Big refund",
                    Amount = -(ImportValidationConstants.MaxAmountAbsoluteValue + 1m),
                },
            ],
        };

        // Act
        var result = ImportExecuteRequestValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Amount out of range", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithValidRequest_ReturnsValid()
    {
        // Arrange
        var request = new ImportExecuteRequest
        {
            AccountId = Guid.NewGuid(),
            FileName = "test.csv",
            Transactions =
            [
                new ImportTransactionData
                {
                    Date = new DateOnly(2026, 1, 15),
                    Description = "Normal transaction",
                    Amount = -50m,
                },
                new ImportTransactionData
                {
                    Date = new DateOnly(2026, 1, 16),
                    Description = "Another transaction",
                    Amount = -25.99m,
                },
            ],
        };

        // Act
        var result = ImportExecuteRequestValidator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithExactMaxTransactions_ReturnsValid()
    {
        // Arrange
        var transactions = Enumerable.Range(0, ImportValidationConstants.MaxTransactionsPerImport)
            .Select(i => new ImportTransactionData
            {
                Date = new DateOnly(2026, 1, 15),
                Description = $"Transaction {i}",
                Amount = -10m,
            })
            .ToList();

        var request = new ImportExecuteRequest
        {
            AccountId = Guid.NewGuid(),
            FileName = "test.csv",
            Transactions = transactions,
        };

        // Act
        var result = ImportExecuteRequestValidator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithExactMaxAmount_ReturnsValid()
    {
        // Arrange
        var request = new ImportExecuteRequest
        {
            AccountId = Guid.NewGuid(),
            FileName = "test.csv",
            Transactions =
            [
                new ImportTransactionData
                {
                    Date = new DateOnly(2026, 1, 15),
                    Description = "Max amount purchase",
                    Amount = ImportValidationConstants.MaxAmountAbsoluteValue,
                },
            ],
        };

        // Act
        var result = ImportExecuteRequestValidator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithExactMaxDescriptionLength_ReturnsValid()
    {
        // Arrange
        var request = new ImportExecuteRequest
        {
            AccountId = Guid.NewGuid(),
            FileName = "test.csv",
            Transactions =
            [
                new ImportTransactionData
                {
                    Date = new DateOnly(2026, 1, 15),
                    Description = new string('A', ImportValidationConstants.MaxDescriptionLength),
                    Amount = -50m,
                },
            ],
        };

        // Act
        var result = ImportExecuteRequestValidator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithDateExactlyAtLimit_ReturnsValid()
    {
        // Arrange — exactly 365 days in the future
        var maxDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(ImportValidationConstants.MaxFutureDateDays);
        var request = new ImportExecuteRequest
        {
            AccountId = Guid.NewGuid(),
            FileName = "test.csv",
            Transactions =
            [
                new ImportTransactionData
                {
                    Date = maxDate,
                    Description = "Boundary transaction",
                    Amount = -50m,
                },
            ],
        };

        // Act
        var result = ImportExecuteRequestValidator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_MultipleErrors_ReportsAll()
    {
        // Arrange — multiple violations in a single request
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(ImportValidationConstants.MaxFutureDateDays + 100);
        var request = new ImportExecuteRequest
        {
            AccountId = Guid.NewGuid(),
            FileName = "test.csv",
            Transactions =
            [
                new ImportTransactionData
                {
                    Date = futureDate,
                    Description = new string('A', ImportValidationConstants.MaxDescriptionLength + 1),
                    Amount = ImportValidationConstants.MaxAmountAbsoluteValue + 1m,
                },
            ],
        };

        // Act
        var result = ImportExecuteRequestValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 3, $"Expected at least 3 errors but got {result.Errors.Count}");
    }

    [Fact]
    public void Validate_ErrorIncludesCorrectRowNumber()
    {
        // Arrange — error at row 3 (1-indexed)
        var request = new ImportExecuteRequest
        {
            AccountId = Guid.NewGuid(),
            FileName = "test.csv",
            Transactions =
            [
                new ImportTransactionData { Date = new DateOnly(2026, 1, 15), Description = "OK 1", Amount = -10m },
                new ImportTransactionData { Date = new DateOnly(2026, 1, 16), Description = "OK 2", Amount = -20m },
                new ImportTransactionData
                {
                    Date = new DateOnly(2026, 1, 17),
                    Description = new string('A', ImportValidationConstants.MaxDescriptionLength + 1),
                    Amount = -30m,
                },
            ],
        };

        // Act
        var result = ImportExecuteRequestValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("row 3", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ErrorType_CountErrors_AreBadRequest()
    {
        // Arrange
        var request = new ImportExecuteRequest
        {
            AccountId = Guid.NewGuid(),
            FileName = "test.csv",
            Transactions = [],
        };

        // Act
        var result = ImportExecuteRequestValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.IsBadRequest);
    }

    [Fact]
    public void Validate_ErrorType_FieldErrors_AreUnprocessable()
    {
        // Arrange
        var request = new ImportExecuteRequest
        {
            AccountId = Guid.NewGuid(),
            FileName = "test.csv",
            Transactions =
            [
                new ImportTransactionData
                {
                    Date = new DateOnly(2026, 1, 15),
                    Description = new string('A', ImportValidationConstants.MaxDescriptionLength + 1),
                    Amount = -50m,
                },
            ],
        };

        // Act
        var result = ImportExecuteRequestValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.False(result.IsBadRequest);
    }

    [Fact]
    public void Validate_WithPastDates_ReturnsValid()
    {
        // Arrange — past dates should be fine (no lower bound)
        var request = new ImportExecuteRequest
        {
            AccountId = Guid.NewGuid(),
            FileName = "test.csv",
            Transactions =
            [
                new ImportTransactionData
                {
                    Date = new DateOnly(2020, 1, 1),
                    Description = "Old transaction",
                    Amount = -50m,
                },
            ],
        };

        // Act
        var result = ImportExecuteRequestValidator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }
}
