// <copyright file="CustomReportLayoutTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Reports;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the <see cref="CustomReportLayout"/> entity.
/// </summary>
public class CustomReportLayoutTests
{
    private static readonly Guid ValidUserId = Guid.NewGuid();

    [Fact]
    public void CreateShared_WithValidInputs_SetsPropertiesCorrectly()
    {
        // Act
        var layout = CustomReportLayout.CreateShared("Monthly Summary", "{\"type\":\"bar\"}", ValidUserId);

        // Assert
        Assert.NotEqual(Guid.Empty, layout.Id);
        Assert.Equal("Monthly Summary", layout.Name);
        Assert.Equal("{\"type\":\"bar\"}", layout.LayoutJson);
        Assert.Equal(BudgetScope.Shared, layout.Scope);
        Assert.Null(layout.OwnerUserId);
        Assert.Equal(ValidUserId, layout.CreatedByUserId);
        Assert.Equal(layout.CreatedAtUtc, layout.UpdatedAtUtc);
    }

    [Fact]
    public void CreatePersonal_WithValidInputs_SetsPropertiesCorrectly()
    {
        // Act
        var layout = CustomReportLayout.CreatePersonal("My Report", "{\"columns\":2}", ValidUserId);

        // Assert
        Assert.NotEqual(Guid.Empty, layout.Id);
        Assert.Equal("My Report", layout.Name);
        Assert.Equal("{\"columns\":2}", layout.LayoutJson);
        Assert.Equal(BudgetScope.Personal, layout.Scope);
        Assert.Equal(ValidUserId, layout.OwnerUserId);
        Assert.Equal(ValidUserId, layout.CreatedByUserId);
    }

    [Fact]
    public void CreateShared_WithEmptyUserId_ThrowsDomainException()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            CustomReportLayout.CreateShared("Test", "{}", Guid.Empty));
        Assert.Contains("Created by user ID", ex.Message);
    }

    [Fact]
    public void CreatePersonal_WithEmptyOwnerUserId_ThrowsDomainException()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            CustomReportLayout.CreatePersonal("Test", "{}", Guid.Empty));
        Assert.Contains("Owner user ID", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateShared_WithEmptyName_ThrowsDomainException(string? name)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            CustomReportLayout.CreateShared(name!, "{}", ValidUserId));
        Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateShared_WithNameExceedingMaxLength_ThrowsDomainException()
    {
        // Arrange
        var longName = new string('A', CustomReportLayout.MaxNameLength + 1);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            CustomReportLayout.CreateShared(longName, "{}", ValidUserId));
        Assert.Contains($"{CustomReportLayout.MaxNameLength}", ex.Message);
    }

    [Fact]
    public void CreateShared_WithNameAtMaxLength_Succeeds()
    {
        // Arrange
        var maxName = new string('A', CustomReportLayout.MaxNameLength);

        // Act
        var layout = CustomReportLayout.CreateShared(maxName, "{}", ValidUserId);

        // Assert
        Assert.Equal(maxName, layout.Name);
    }

    [Fact]
    public void CreateShared_TrimsName()
    {
        // Act
        var layout = CustomReportLayout.CreateShared("  My Report  ", "{}", ValidUserId);

        // Assert
        Assert.Equal("My Report", layout.Name);
    }

    [Theory]
    [InlineData(null, "{}")]
    [InlineData("", "{}")]
    [InlineData("   ", "{}")]
    public void CreateShared_NormalizesEmptyLayoutJson(string? json, string expected)
    {
        // Act
        var layout = CustomReportLayout.CreateShared("Test", json!, ValidUserId);

        // Assert
        Assert.Equal(expected, layout.LayoutJson);
    }

    [Fact]
    public void CreateShared_TrimsLayoutJson()
    {
        // Act
        var layout = CustomReportLayout.CreateShared("Test", "  {\"a\":1}  ", ValidUserId);

        // Assert
        Assert.Equal("{\"a\":1}", layout.LayoutJson);
    }

    [Fact]
    public void UpdateName_WithValidName_UpdatesNameAndTimestamp()
    {
        // Arrange
        var layout = CustomReportLayout.CreateShared("Original", "{}", ValidUserId);
        var originalUpdatedAt = layout.UpdatedAtUtc;

        // Act
        layout.UpdateName("Updated Name");

        // Assert
        Assert.Equal("Updated Name", layout.Name);
        Assert.True(layout.UpdatedAtUtc >= originalUpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateName_WithEmptyName_ThrowsDomainException(string? name)
    {
        // Arrange
        var layout = CustomReportLayout.CreateShared("Original", "{}", ValidUserId);

        // Act & Assert
        Assert.Throws<DomainException>(() => layout.UpdateName(name!));
    }

    [Fact]
    public void UpdateName_WithNameExceedingMaxLength_ThrowsDomainException()
    {
        // Arrange
        var layout = CustomReportLayout.CreateShared("Original", "{}", ValidUserId);
        var longName = new string('X', CustomReportLayout.MaxNameLength + 1);

        // Act & Assert
        Assert.Throws<DomainException>(() => layout.UpdateName(longName));
    }

    [Fact]
    public void UpdateLayout_WithValidJson_UpdatesLayoutAndTimestamp()
    {
        // Arrange
        var layout = CustomReportLayout.CreateShared("Test", "{}", ValidUserId);
        var originalUpdatedAt = layout.UpdatedAtUtc;

        // Act
        layout.UpdateLayout("{\"columns\":3}");

        // Assert
        Assert.Equal("{\"columns\":3}", layout.LayoutJson);
        Assert.True(layout.UpdatedAtUtc >= originalUpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateLayout_WithEmptyJson_NormalizesToEmptyObject(string? json)
    {
        // Arrange
        var layout = CustomReportLayout.CreateShared("Test", "{\"old\":1}", ValidUserId);

        // Act
        layout.UpdateLayout(json!);

        // Assert
        Assert.Equal("{}", layout.LayoutJson);
    }

    [Fact]
    public void CreateShared_GeneratesUniqueIds()
    {
        // Act
        var layout1 = CustomReportLayout.CreateShared("Report 1", "{}", ValidUserId);
        var layout2 = CustomReportLayout.CreateShared("Report 2", "{}", ValidUserId);

        // Assert
        Assert.NotEqual(layout1.Id, layout2.Id);
    }
}
