// <copyright file="AuthorizationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for API authorization and authentication.
/// </summary>
public sealed class AuthorizationTests : IClassFixture<AuthEnabledWebApplicationFactory>
{
    private readonly HttpClient _unauthenticatedClient;
    private readonly HttpClient _authenticatedClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory with authentication enabled.</param>
    public AuthorizationTests(AuthEnabledWebApplicationFactory factory)
    {
        this._unauthenticatedClient = factory.CreateUnauthenticatedClient();
        this._authenticatedClient = factory.CreateAuthenticatedClient();
    }

    /// <summary>
    /// Unauthenticated request to /api/v1/accounts returns 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task GetAccounts_Unauthenticated_Returns_401()
    {
        // Act
        var response = await this._unauthenticatedClient.GetAsync("/api/v1/accounts");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Authenticated request to /api/v1/accounts returns 200 OK.
    /// </summary>
    [Fact]
    public async Task GetAccounts_Authenticated_Returns_200()
    {
        // Act
        var response = await this._authenticatedClient.GetAsync("/api/v1/accounts");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Unauthenticated request to /api/v1/transactions returns 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task GetTransactions_Unauthenticated_Returns_401()
    {
        // Act
        var response = await this._unauthenticatedClient.GetAsync("/api/v1/transactions?startDate=2025-01-01&endDate=2025-12-31");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Unauthenticated request to /api/v1/categories returns 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task GetCategories_Unauthenticated_Returns_401()
    {
        // Act
        var response = await this._unauthenticatedClient.GetAsync("/api/v1/categories");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Unauthenticated request to /api/v1/recurring-transactions returns 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task GetRecurringTransactions_Unauthenticated_Returns_401()
    {
        // Act
        var response = await this._unauthenticatedClient.GetAsync("/api/v1/recurring-transactions");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Unauthenticated request to /api/v1/recurring-transfers returns 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task GetRecurringTransfers_Unauthenticated_Returns_401()
    {
        // Act
        var response = await this._unauthenticatedClient.GetAsync("/api/v1/recurring-transfers");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Unauthenticated request to /api/v1/transfers returns 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task GetTransfers_Unauthenticated_Returns_401()
    {
        // Act
        var response = await this._unauthenticatedClient.GetAsync("/api/v1/transfers?startDate=2025-01-01&endDate=2025-12-31");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Unauthenticated request to /api/v1/settings returns 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task GetSettings_Unauthenticated_Returns_401()
    {
        // Act
        var response = await this._unauthenticatedClient.GetAsync("/api/v1/settings");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Unauthenticated request to /api/v1/budgets returns 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task GetBudgets_Unauthenticated_Returns_401()
    {
        // Act
        var response = await this._unauthenticatedClient.GetAsync("/api/v1/budgets?year=2025&month=1");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Unauthenticated request to /api/v1/calendar/grid returns 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task GetCalendar_Unauthenticated_Returns_401()
    {
        // Act
        var response = await this._unauthenticatedClient.GetAsync("/api/v1/calendar/grid?year=2025&month=1");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Unauthenticated request to /api/v1/allocations/paycheck returns 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task GetAllocations_Unauthenticated_Returns_401()
    {
        // Act
        var response = await this._unauthenticatedClient.GetAsync("/api/v1/allocations/paycheck?frequency=BiWeekly");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Health endpoint does NOT require authentication.
    /// </summary>
    [Fact]
    public async Task HealthEndpoint_Unauthenticated_Returns_200()
    {
        // Act
        var response = await this._unauthenticatedClient.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Unauthenticated request to /api/v1/user/me returns 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task GetUserProfile_Unauthenticated_Returns_401()
    {
        // Act
        var response = await this._unauthenticatedClient.GetAsync("/api/v1/user/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Unauthenticated request to /api/v1/user/settings returns 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task GetUserSettings_Unauthenticated_Returns_401()
    {
        // Act
        var response = await this._unauthenticatedClient.GetAsync("/api/v1/user/settings");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Unauthenticated request to /api/v1/user/scope returns 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task GetUserScope_Unauthenticated_Returns_401()
    {
        // Act
        var response = await this._unauthenticatedClient.GetAsync("/api/v1/user/scope");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
