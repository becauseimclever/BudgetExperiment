// <copyright file="MigrationRollbackSafetyTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BudgetExperiment.Infrastructure.Tests.Encryption;

/// <summary>
/// Integration tests for rollback safety around encrypted ciphertext storage migrations.
/// </summary>
[Collection("PostgreSqlDb")]
public sealed class MigrationRollbackSafetyTests
{
    private const string PreFeature163Migration = "20260422033920_AddSoftDeleteFields";

    private readonly PostgreSqlFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationRollbackSafetyTests"/> class.
    /// </summary>
    /// <param name="fixture">Shared PostgreSQL fixture.</param>
    public MigrationRollbackSafetyTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DownMigration_WhenCiphertextExists_ThrowsExplicitRollbackGuardError()
    {
        // Arrange
        var masterKey = Infrastructure.Encryption.EncryptionService.GenerateSecureKey();
        await using var context = _fixture.CreateEncryptedContext(masterKey);

        var account = Account.Create("Encrypted Rollback Guard", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 12.34m), new DateOnly(2026, 4, 28), "Rollback Guard Transaction");
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var migrator = context.GetService<IMigrator>();

        // Act
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => migrator.MigrateAsync(PreFeature163Migration));

        // Assert
        Assert.Contains("Feature 163 rollback is blocked", ex.ToString(), StringComparison.Ordinal);
    }
}
