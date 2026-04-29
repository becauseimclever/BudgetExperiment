// <copyright file="EncryptedPersistenceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Infrastructure.Encryption;
using BudgetExperiment.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Infrastructure.Tests.Encryption;

/// <summary>
/// Integration tests for encrypted persistence mappings.
/// </summary>
[Collection("PostgreSqlDb")]
public sealed class EncryptedPersistenceTests
{
    private readonly PostgreSqlFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptedPersistenceTests"/> class.
    /// </summary>
    /// <param name="fixture">Shared PostgreSQL fixture.</param>
    public EncryptedPersistenceTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SaveAndLoad_StoresCiphertextAtRest_AndReturnsDecryptedValues()
    {
        // Arrange
        using var truncationContext = _fixture.CreateContext();
        var connectionString = truncationContext.Database.GetConnectionString();
        Assert.False(string.IsNullOrWhiteSpace(connectionString));

        var masterKey = EncryptionService.GenerateSecureKey();
        var serviceProvider = CreateServiceProvider(masterKey);

        var accountId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var accountName = "Primary Checking";
        var description = "Morning coffee shop";
        var amount = 42.65m;

        await using (var writeContext = CreateEncryptedContext(connectionString!, serviceProvider))
        {
            var account = Account.CreatePersonal(accountName, AccountType.Checking, ownerUserId);
            var transaction = account.AddTransaction(
                MoneyValue.Create("USD", amount),
                DateOnly.FromDateTime(DateTime.UtcNow),
                description);

            writeContext.Accounts.Add(account);
            await writeContext.SaveChangesAsync();

            accountId = account.Id;

            var encryptedAccountName = await ReadSingleStringColumnAsync(
                writeContext,
                "SELECT \"Name\" FROM \"Accounts\" WHERE \"Id\" = @p0",
                account.Id);

            var encryptedDescription = await ReadSingleStringColumnAsync(
                writeContext,
                "SELECT \"Description\" FROM \"Transactions\" WHERE \"Id\" = @p0",
                transaction.Id);

            var encryptedAmount = await ReadSingleStringColumnAsync(
                writeContext,
                "SELECT \"Amount\" FROM \"Transactions\" WHERE \"Id\" = @p0",
                transaction.Id);

            Assert.StartsWith("enc::v1:", encryptedAccountName, StringComparison.Ordinal);
            Assert.StartsWith("enc::v1:", encryptedDescription, StringComparison.Ordinal);
            Assert.StartsWith("enc::v1:", encryptedAmount, StringComparison.Ordinal);
            Assert.NotEqual(accountName, encryptedAccountName);
            Assert.NotEqual(description, encryptedDescription);
            Assert.NotEqual(amount.ToString(System.Globalization.CultureInfo.InvariantCulture), encryptedAmount);
        }

        // Act
        await using var readContext = CreateEncryptedContext(connectionString!, serviceProvider);
        var savedAccount = await readContext.Accounts.SingleAsync(a => a.Id == accountId);
        var savedTransaction = await readContext.Transactions.SingleAsync(t => t.AccountId == accountId);

        // Assert
        Assert.Equal(accountName, savedAccount.Name);
        Assert.Equal(description, savedTransaction.Description);
        Assert.Equal(amount, savedTransaction.Amount.Amount);
    }

    private static ServiceProvider CreateServiceProvider(string masterKey)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:MasterKey"] = masterKey,
            })
            .Build();

        return new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddScoped<IEncryptionService, EncryptionService>()
            .BuildServiceProvider();
    }

    private static BudgetDbContext CreateEncryptedContext(string connectionString, IServiceProvider serviceProvider)
    {
        var options = new DbContextOptionsBuilder<BudgetDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new BudgetDbContext(options, serviceProvider);
    }

    private static async Task<string> ReadSingleStringColumnAsync(BudgetDbContext context, string sql, Guid id)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "p0";
        parameter.Value = id;
        command.Parameters.Add(parameter);

        if (command.Connection!.State != System.Data.ConnectionState.Open)
        {
            await command.Connection.OpenAsync();
        }

        var result = await command.ExecuteScalarAsync();
        Assert.NotNull(result);
        return Convert.ToString(result, System.Globalization.CultureInfo.InvariantCulture)!;
    }
}
