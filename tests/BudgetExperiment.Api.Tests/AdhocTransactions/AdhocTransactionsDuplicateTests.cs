using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Api;
using BudgetExperiment.Application.AdhocTransactions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace BudgetExperiment.Api.Tests.AdhocTransactions;

public sealed class AdhocTransactionsDuplicateTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AdhocTransactionsDuplicateTests(WebApplicationFactory<Program> factory)
    {
        this._factory = factory;
    }

    [Fact]
    public async Task CreateIncome_Duplicate_Returns400()
    {
        // Arrange
        var client = this._factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N");
        var request = new CreateIncomeTransactionRequest(
            $"Test Income {unique}",
            "USD",
            123.45m,
            new DateOnly(2025, 10, 1),
            null);

        // Act
        var r1 = await client.PostAsJsonAsync("/api/v1/adhoc-transactions/income", request);
        var r2 = await client.PostAsJsonAsync("/api/v1/adhoc-transactions/income", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, r1.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
    }

    [Fact]
    public async Task CreateExpense_Duplicate_Returns400()
    {
        // Arrange
        var client = this._factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N");
        var request = new CreateExpenseTransactionRequest(
            $"Test Expense {unique}",
            "USD",
            50m,
            new DateOnly(2025, 10, 2),
            null);

        // Act
        var r1 = await client.PostAsJsonAsync("/api/v1/adhoc-transactions/expenses", request);
        var r2 = await client.PostAsJsonAsync("/api/v1/adhoc-transactions/expenses", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, r1.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
    }
}
