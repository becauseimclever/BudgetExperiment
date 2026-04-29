// <copyright file="EncryptionServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Infrastructure.Encryption;

using Microsoft.Extensions.Configuration;

namespace BudgetExperiment.Infrastructure.Tests.Encryption;

/// <summary>
/// Unit tests for <see cref="EncryptionService"/>.
/// </summary>
public sealed class EncryptionServiceTests
{
    private const string EncryptionMasterKeyEnvironmentVariable = "ENCRYPTION_MASTER_KEY";

    [Fact]
    public async Task EncryptAsync_ProducesPrefixedCiphertext_And_DecryptsRoundTrip()
    {
        // Arrange
        var service = CreateService();
        const string plaintext = "Sensitive merchant note";

        // Act
        var ciphertext = await service.EncryptAsync(plaintext);
        var decrypted = await service.DecryptAsync(ciphertext);

        // Assert
        Assert.StartsWith("enc::v1:", ciphertext, StringComparison.Ordinal);
        Assert.NotEqual(plaintext, ciphertext);
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public async Task DecryptAsync_WhenPlaintextValueProvided_ReturnsPlaintextForSafeRollout()
    {
        // Arrange
        var service = CreateService();
        const string legacyPlaintext = "Legacy row without encryption";

        // Act
        var decrypted = await service.DecryptAsync(legacyPlaintext);

        // Assert
        Assert.Equal(legacyPlaintext, decrypted);
    }

    [Fact]
    public async Task DecryptAsync_WhenCiphertextIsTampered_ThrowsDomainException()
    {
        // Arrange
        var service = CreateService();
        var ciphertext = await service.EncryptAsync("Do not tamper");
        var tampered = ciphertext[..^1] + "A";

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => service.DecryptAsync(tampered));
    }

    [Fact]
    public async Task DecryptAsync_WhenCiphertextVersionIsUnknown_ThrowsDomainException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => service.DecryptAsync("enc::v2:YWJjZA=="));
        Assert.Contains("Unsupported encrypted payload version", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_WhenKeyMissing_ThrowsDomainException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act & Assert
        RunWithEnvironmentVariableUnset(
            EncryptionMasterKeyEnvironmentVariable,
            () => Assert.Throws<DomainException>(() => new EncryptionService(config)));
    }

    [Fact]
    public void Constructor_WhenKeyIsInvalidBase64_ThrowsDomainException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:MasterKey"] = "not-base64",
            })
            .Build();

        // Act & Assert
        RunWithEnvironmentVariableUnset(
            EncryptionMasterKeyEnvironmentVariable,
            () => Assert.Throws<DomainException>(() => new EncryptionService(config)));
    }

    [Fact]
    public void Constructor_WhenKeyLengthIsInvalid_ThrowsDomainException()
    {
        // Arrange
        var shortKey = Convert.ToBase64String(new byte[16]);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:MasterKey"] = shortKey,
            })
            .Build();

        // Act & Assert
        RunWithEnvironmentVariableUnset(
            EncryptionMasterKeyEnvironmentVariable,
            () => Assert.Throws<DomainException>(() => new EncryptionService(config)));
    }

    private static EncryptionService CreateService()
    {
        var key = EncryptionService.GenerateSecureKey();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:MasterKey"] = key,
            })
            .Build();
        return new EncryptionService(config);
    }

    private static void RunWithEnvironmentVariableUnset(string variableName, Action testAction)
    {
        var originalValue = Environment.GetEnvironmentVariable(variableName);
        try
        {
            Environment.SetEnvironmentVariable(variableName, null);
            testAction();
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, originalValue);
        }
    }
}
