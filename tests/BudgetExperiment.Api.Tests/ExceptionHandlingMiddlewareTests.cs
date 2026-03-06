// <copyright file="ExceptionHandlingMiddlewareTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.Json;

using BudgetExperiment.Api.Middleware;
using BudgetExperiment.Domain;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Unit tests for <see cref="ExceptionHandlingMiddleware"/>.
/// </summary>
public sealed class ExceptionHandlingMiddlewareTests
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = NullLogger<ExceptionHandlingMiddleware>.Instance;

    /// <summary>
    /// Middleware returns RFC 7807 ProblemDetails with Instance for an unhandled exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InvokeAsync_UnhandledException_ReturnsProblemDetails()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/test";
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("Something broke"),
            this._logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body);
        Assert.NotNull(problem);
        Assert.Equal("Internal Server Error", problem.Title);
        Assert.Equal(500, problem.Status);
        Assert.Equal("Something broke", problem.Detail);
        Assert.Equal("/api/v1/test", problem.Instance);
    }

    /// <summary>
    /// Middleware returns ProblemDetails with traceId extension for an unhandled exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InvokeAsync_UnhandledException_IncludesTraceId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "test-trace-123";
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("fail"),
            this._logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
        Assert.True(json.TryGetProperty("traceId", out var traceId));
        Assert.Equal("test-trace-123", traceId.GetString());
    }

    /// <summary>
    /// Middleware aborts without writing a response body for OperationCanceledException.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InvokeAsync_OperationCanceled_AbortsWithoutResponse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new OperationCanceledException(),
            this._logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert — no response body written, connection aborted
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        Assert.Equal(0, context.Response.Body.Length);
    }

    /// <summary>
    /// Middleware aborts without writing a response body for TaskCanceledException.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InvokeAsync_TaskCanceled_AbortsWithoutResponse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new TaskCanceledException(),
            this._logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert — no response body written
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        Assert.Equal(0, context.Response.Body.Length);
    }

    /// <summary>
    /// Middleware returns 400 ProblemDetails for DomainException.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InvokeAsync_DomainException_Returns400ProblemDetails()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/budgets";
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new DomainException("Amount must be positive."),
            this._logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body);
        Assert.NotNull(problem);
        Assert.Equal("Domain Validation Error", problem.Title);
        Assert.Equal(400, problem.Status);
        Assert.Equal("/api/v1/budgets", problem.Instance);
    }

    /// <summary>
    /// Middleware returns 404 ProblemDetails for DomainException containing "not found".
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InvokeAsync_DomainExceptionNotFound_Returns404ProblemDetails()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/accounts/123";
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new DomainException("Account not found."),
            this._logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body);
        Assert.NotNull(problem);
        Assert.Equal("Not Found", problem.Title);
        Assert.Equal(404, problem.Status);
    }

    /// <summary>
    /// Middleware passes through when no exception is thrown.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InvokeAsync_NoException_PassesThrough()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;

        var middleware = new ExceptionHandlingMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            this._logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }
}
