// <copyright file="AppSettingsConfigurationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain.Settings;
using BudgetExperiment.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Metadata tests for <see cref="AppSettings"/> persistence compatibility.
/// </summary>
public sealed class AppSettingsConfigurationTests
{
    [Fact]
    public void BudgetDbContext_Maps_Generic_AiEndpoint_To_Legacy_Column_Name()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BudgetDbContext>()
            .UseNpgsql("Host=localhost;Database=budgetexperiment;Username=test;Password=test")
            .Options;

        using var context = new BudgetDbContext(options);

        // Act
        var entityType = context.Model.FindEntityType(typeof(AppSettings));
        var endpointProperty = entityType?.FindProperty(nameof(AppSettings.AiEndpointUrl));
        var aliasProperty = entityType?.FindProperty(nameof(AppSettings.AiOllamaEndpoint));

        // Assert
        Assert.NotNull(endpointProperty);
        Assert.Equal("AiOllamaEndpoint", endpointProperty.GetColumnName());
        Assert.Null(aliasProperty);
    }
}
