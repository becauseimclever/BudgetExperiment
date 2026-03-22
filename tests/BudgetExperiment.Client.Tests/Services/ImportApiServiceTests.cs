// <copyright file="ImportApiServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="ImportApiService"/> class.
/// Tests focus on service logic (ETag handling, conflict detection, URL construction)
/// rather than HTTP framework behavior.
/// </summary>
public sealed class ImportApiServiceTests
{
    /// <summary>
    /// Verifies that UpdateMappingAsync adds ETag header when version is provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateMappingAsync_WithVersion_AddsIfMatchHeader()
    {
        // Arrange
        string? capturedIfMatch = null;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedIfMatch = req.Headers.IfMatch.FirstOrDefault()?.Tag;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ImportMappingDto()),
            });
        });
        var sut = CreateService(handler);

        // Act
        await sut.UpdateMappingAsync(Guid.NewGuid(), new UpdateImportMappingRequest(), "v2");

        // Assert
        Assert.Equal("\"v2\"", capturedIfMatch);
    }

    /// <summary>
    /// Verifies that UpdateMappingAsync does not add ETag header when version is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateMappingAsync_WithoutVersion_NoIfMatchHeader()
    {
        // Arrange
        bool hasIfMatch = true;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            hasIfMatch = req.Headers.IfMatch.Any();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ImportMappingDto()),
            });
        });
        var sut = CreateService(handler);

        // Act
        await sut.UpdateMappingAsync(Guid.NewGuid(), new UpdateImportMappingRequest());

        // Assert
        Assert.False(hasIfMatch);
    }

    /// <summary>
    /// Verifies that UpdateMappingAsync returns conflict result on 409 status.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateMappingAsync_On409_ReturnsConflict()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)));
        var sut = CreateService(handler);

        // Act
        var result = await sut.UpdateMappingAsync(Guid.NewGuid(), new UpdateImportMappingRequest(), "stale-etag");

        // Assert
        Assert.True(result.IsConflict);
        Assert.False(result.IsSuccess);
    }

    /// <summary>
    /// Verifies that UpdateMappingAsync returns success result on 200 status.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateMappingAsync_OnSuccess_ReturnsSuccessResult()
    {
        // Arrange
        var mappingId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ImportMappingDto { Id = mappingId }),
            }));
        var sut = CreateService(handler);

        // Act
        var result = await sut.UpdateMappingAsync(mappingId, new UpdateImportMappingRequest());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsConflict);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// Verifies that UpdateMappingAsync returns failure on non-success status.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateMappingAsync_OnNonSuccessStatus_ReturnsFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var sut = CreateService(handler);

        // Act
        var result = await sut.UpdateMappingAsync(Guid.NewGuid(), new UpdateImportMappingRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(result.IsConflict);
    }

    /// <summary>
    /// Verifies that SuggestMappingAsync returns null for 204 NoContent.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SuggestMappingAsync_OnNoContent_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));
        var sut = CreateService(handler);

        // Act
        var result = await sut.SuggestMappingAsync(["Date", "Amount", "Description"]);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that DeleteMappingAsync returns true on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteMappingAsync_OnSuccess_ReturnsTrue()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var sut = CreateService(handler);

        // Act
        var result = await sut.DeleteMappingAsync(Guid.NewGuid());

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Verifies that DeleteMappingAsync returns false on non-success status.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteMappingAsync_OnNotFound_ReturnsFalse()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var sut = CreateService(handler);

        // Act
        var result = await sut.DeleteMappingAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies that GetMappingsAsync propagates HttpRequestException (not caught).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetMappingsAsync_OnHttpFailure_PropagatesException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Network error"));
        var sut = CreateService(handler);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetMappingsAsync());
    }

    /// <summary>
    /// Verifies that GetMappingsAsync returns mappings on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetMappingsAsync_OnSuccess_ReturnsMappings()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<ImportMappingDto>
                {
                    new ImportMappingDto { Id = Guid.NewGuid(), Name = "BOA" },
                }),
            }));
        var sut = CreateService(handler);

        // Act
        var result = await sut.GetMappingsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("BOA", result[0].Name);
    }

    /// <summary>
    /// Verifies that GetMappingAsync returns mapping on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetMappingAsync_OnSuccess_ReturnsMapping()
    {
        // Arrange
        var id = Guid.NewGuid();
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedUrl = req.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ImportMappingDto { Id = id, Name = "Test" }),
            });
        });
        var sut = CreateService(handler);

        // Act
        var result = await sut.GetMappingAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal($"/api/v1/import/mappings/{id}", capturedUrl);
    }

    /// <summary>
    /// Verifies that GetMappingAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetMappingAsync_OnFailure_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Not found"));
        var sut = CreateService(handler);

        // Act
        var result = await sut.GetMappingAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that CreateMappingAsync returns mapping on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateMappingAsync_OnSuccess_ReturnsMapping()
    {
        // Arrange
        var id = Guid.NewGuid();
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedUrl = req.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent.Create(new ImportMappingDto { Id = id, Name = "New Mapping" }),
            });
        });
        var sut = CreateService(handler);

        // Act
        var result = await sut.CreateMappingAsync(new CreateImportMappingRequest
        {
            Name = "New Mapping",
            ColumnMappings = [],
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Mapping", result.Name);
        Assert.Equal("/api/v1/import/mappings", capturedUrl);
    }

    /// <summary>
    /// Verifies that CreateMappingAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateMappingAsync_OnFailure_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));
        var sut = CreateService(handler);

        // Act
        var result = await sut.CreateMappingAsync(new CreateImportMappingRequest
        {
            Name = "Bad",
            ColumnMappings = [],
        });

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that SuggestMappingAsync returns mapping on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SuggestMappingAsync_OnSuccess_ReturnsMapping()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ImportMappingDto { Id = Guid.NewGuid(), Name = "Suggested" }),
            }));
        var sut = CreateService(handler);

        // Act
        var result = await sut.SuggestMappingAsync(["Date", "Amount", "Description"]);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Suggested", result.Name);
    }

    /// <summary>
    /// Verifies that PreviewAsync calls correct endpoint and returns result.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PreviewAsync_OnSuccess_ReturnsResult()
    {
        // Arrange
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedUrl = req.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ImportPreviewResult
                {
                    Rows = [],
                    ValidCount = 10,
                    ErrorCount = 0,
                }),
            });
        });
        var sut = CreateService(handler);

        // Act
        var result = await sut.PreviewAsync(new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows = [],
            Mappings = [],
            DateFormat = "MM/dd/yyyy",
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.ValidCount);
        Assert.Equal("/api/v1/import/preview", capturedUrl);
    }

    /// <summary>
    /// Verifies that PreviewAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PreviewAsync_OnFailure_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));
        var sut = CreateService(handler);

        // Act
        var result = await sut.PreviewAsync(new ImportPreviewRequest
        {
            AccountId = Guid.NewGuid(),
            Rows = [],
            Mappings = [],
            DateFormat = "MM/dd/yyyy",
        });

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that ExecuteAsync returns result on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteAsync_OnSuccess_ReturnsResult()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedUrl = req.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ImportResult
                {
                    BatchId = batchId,
                    ImportedCount = 25,
                    SkippedCount = 2,
                    ErrorCount = 0,
                    CreatedTransactionIds = [],
                }),
            });
        });
        var sut = CreateService(handler);

        // Act
        var result = await sut.ExecuteAsync(new ImportExecuteRequest
        {
            AccountId = Guid.NewGuid(),
            FileName = "test.csv",
            Transactions = [],
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(batchId, result.BatchId);
        Assert.Equal(25, result.ImportedCount);
        Assert.Equal("/api/v1/import/execute", capturedUrl);
    }

    /// <summary>
    /// Verifies that ExecuteAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteAsync_OnFailure_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var sut = CreateService(handler);

        // Act
        var result = await sut.ExecuteAsync(new ImportExecuteRequest
        {
            AccountId = Guid.NewGuid(),
            FileName = "test.csv",
            Transactions = [],
        });

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that GetHistoryAsync returns list on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetHistoryAsync_OnSuccess_ReturnsBatches()
    {
        // Arrange
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedUrl = req.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<ImportBatchDto>
                {
                    new ImportBatchDto
                    {
                        Id = Guid.NewGuid(),
                        AccountId = Guid.NewGuid(),
                        AccountName = "Checking",
                        FileName = "import.csv",
                        TransactionCount = 50,
                    },
                }),
            });
        });
        var sut = CreateService(handler);

        // Act
        var result = await sut.GetHistoryAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Checking", result[0].AccountName);
        Assert.Equal("/api/v1/import/history", capturedUrl);
    }

    /// <summary>
    /// Verifies that GetBatchAsync returns batch on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetBatchAsync_OnSuccess_ReturnsBatch()
    {
        // Arrange
        var id = Guid.NewGuid();
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedUrl = req.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ImportBatchDto
                {
                    Id = id,
                    AccountId = Guid.NewGuid(),
                    AccountName = "Savings",
                    FileName = "batch.csv",
                    TransactionCount = 20,
                }),
            });
        });
        var sut = CreateService(handler);

        // Act
        var result = await sut.GetBatchAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal($"/api/v1/import/batches/{id}", capturedUrl);
    }

    /// <summary>
    /// Verifies that GetBatchAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetBatchAsync_OnFailure_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Not found"));
        var sut = CreateService(handler);

        // Act
        var result = await sut.GetBatchAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that DeleteBatchAsync returns deleted count on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteBatchAsync_OnSuccess_ReturnsDeletedCount()
    {
        // Arrange
        var id = Guid.NewGuid();
        string? capturedUrl = null;
        string? capturedMethod = null;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedUrl = req.RequestUri?.PathAndQuery;
            capturedMethod = req.Method.Method;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { DeletedCount = 15 }),
            });
        });
        var sut = CreateService(handler);

        // Act
        var result = await sut.DeleteBatchAsync(id);

        // Assert
        Assert.Equal(15, result);
        Assert.Equal("DELETE", capturedMethod);
        Assert.Equal($"/api/v1/import/batches/{id}", capturedUrl);
    }

    /// <summary>
    /// Verifies that DeleteBatchAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteBatchAsync_OnFailure_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var sut = CreateService(handler);

        // Act
        var result = await sut.DeleteBatchAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    private static ImportApiService CreateService(MockHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        return new ImportApiService(client);
    }

    /// <summary>
    /// Mock HTTP message handler for testing.
    /// </summary>
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }
}
