// <copyright file="BudgetScopeMiddlewareTests.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

using BudgetExperiment.Api.Middleware;
using BudgetExperiment.Domain;

using Microsoft.AspNetCore.Http;

using Xunit;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Unit tests for <see cref="BudgetScopeMiddleware"/>.
/// </summary>
public sealed class BudgetScopeMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithSharedHeader_SetsSharedScope()
    {
        // Arrange
        var userContext = new FakeUserContext();
        var context = CreateHttpContext();
        context.Request.Headers[BudgetScopeMiddleware.ScopeHeaderName] = "Shared";
        var middleware = new BudgetScopeMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, userContext);

        // Assert
        Assert.Equal(BudgetScope.Shared, userContext.CurrentScope);
    }

    [Fact]
    public async Task InvokeAsync_WithPersonalHeader_SetsPersonalScope()
    {
        // Arrange
        var userContext = new FakeUserContext();
        var context = CreateHttpContext();
        context.Request.Headers[BudgetScopeMiddleware.ScopeHeaderName] = "Personal";
        var middleware = new BudgetScopeMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, userContext);

        // Assert
        Assert.Equal(BudgetScope.Personal, userContext.CurrentScope);
    }

    [Fact]
    public async Task InvokeAsync_WithAllHeader_SetsNullScope()
    {
        // Arrange
        var userContext = new FakeUserContext();
        var context = CreateHttpContext();
        context.Request.Headers[BudgetScopeMiddleware.ScopeHeaderName] = "All";
        var middleware = new BudgetScopeMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, userContext);

        // Assert
        Assert.Null(userContext.CurrentScope);
    }

    [Fact]
    public async Task InvokeAsync_WithNoHeader_LeavesNullScope()
    {
        // Arrange
        var userContext = new FakeUserContext();
        var context = CreateHttpContext();
        var middleware = new BudgetScopeMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, userContext);

        // Assert
        Assert.Null(userContext.CurrentScope);
    }

    [Fact]
    public async Task InvokeAsync_WithCaseInsensitiveHeader_SetsScope()
    {
        // Arrange
        var userContext = new FakeUserContext();
        var context = CreateHttpContext();
        context.Request.Headers[BudgetScopeMiddleware.ScopeHeaderName] = "shared";
        var middleware = new BudgetScopeMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, userContext);

        // Assert
        Assert.Equal(BudgetScope.Shared, userContext.CurrentScope);
    }

    [Fact]
    public async Task InvokeAsync_WithUnknownHeader_SetsNullScope()
    {
        // Arrange
        var userContext = new FakeUserContext();
        var context = CreateHttpContext();
        context.Request.Headers[BudgetScopeMiddleware.ScopeHeaderName] = "Unknown";
        var middleware = new BudgetScopeMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, userContext);

        // Assert
        Assert.Null(userContext.CurrentScope);
    }

    [Fact]
    public async Task InvokeAsync_CallsNextDelegate()
    {
        // Arrange
        var userContext = new FakeUserContext();
        var context = CreateHttpContext();
        var nextCalled = false;
        var middleware = new BudgetScopeMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context, userContext);

        // Assert
        Assert.True(nextCalled);
    }

    private static HttpContext CreateHttpContext()
    {
        return new DefaultHttpContext();
    }

    /// <summary>
    /// Fake implementation of IUserContext for testing.
    /// </summary>
    private sealed class FakeUserContext : IUserContext
    {
        public string UserId => "test-user-id";

        public Guid? UserIdAsGuid => Guid.NewGuid();

        public string Username => "testuser";

        public string? Email => "test@example.com";

        public string? DisplayName => "Test User";

        public string? AvatarUrl => null;

        public bool IsAuthenticated => true;

        public BudgetScope? CurrentScope { get; private set; }

        public void SetScope(BudgetScope? scope)
        {
            this.CurrentScope = scope;
        }
    }
}
