// <copyright file="EncryptedStringConverterTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Infrastructure.Encryption;
using BudgetExperiment.Infrastructure.Persistence.Converters;

using Microsoft.Extensions.Configuration;

namespace BudgetExperiment.Infrastructure.Tests.Encryption;

/// <summary>
/// Unit tests for encrypted EF Core value converters.
/// </summary>
public sealed class EncryptedStringConverterTests
{
    [Fact]
    public void EncryptedStringConverter_RoundTrips_StringValue()
    {
        // Arrange
        var service = CreateService();
        var converter = new EncryptedStringConverter(service);
        var toProvider = converter.ConvertToProviderExpression.Compile();
        var fromProvider = converter.ConvertFromProviderExpression.Compile();
        const string plaintext = "Paycheck from ACME Corp";

        // Act
        var providerValue = toProvider(plaintext);
        var roundTrip = fromProvider(providerValue);

        // Assert
        Assert.StartsWith("enc::v1:", providerValue, StringComparison.Ordinal);
        Assert.Equal(plaintext, roundTrip);
    }

    [Fact]
    public void EncryptedNullableStringConverter_RoundTrips_Null()
    {
        // Arrange
        var service = CreateService();
        var converter = new EncryptedNullableStringConverter(service);
        var toProvider = converter.ConvertToProviderExpression.Compile();
        var fromProvider = converter.ConvertFromProviderExpression.Compile();

        // Act
        var providerValue = toProvider(null);
        var roundTrip = fromProvider(providerValue);

        // Assert
        Assert.Null(providerValue);
        Assert.Null(roundTrip);
    }

    [Fact]
    public void EncryptedNullableStringConverter_RoundTrips_StringValue()
    {
        // Arrange
        var service = CreateService();
        var converter = new EncryptedNullableStringConverter(service);
        var toProvider = converter.ConvertToProviderExpression.Compile();
        var fromProvider = converter.ConvertFromProviderExpression.Compile();
        const string plaintext = "This month I want to reduce dining out.";

        // Act
        var providerValue = toProvider(plaintext);
        var roundTrip = fromProvider(providerValue);

        // Assert
        Assert.NotNull(providerValue);
        Assert.StartsWith("enc::v1:", providerValue, StringComparison.Ordinal);
        Assert.Equal(plaintext, roundTrip);
    }

    private static EncryptionService CreateService()
    {
        var key = EncryptionService.GenerateSecureKey();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:MasterKey"] = key,
            })
            .Build();
        return new EncryptionService(configuration);
    }
}
