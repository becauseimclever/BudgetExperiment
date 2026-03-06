// <copyright file="FamilyUserDefaultsTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Constants;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Regression guard tests for <see cref="FamilyUserDefaults"/> values.
/// Ensures family user identity values are never accidentally changed.
/// </summary>
public sealed class FamilyUserDefaultsTests
{
    /// <summary>
    /// User ID must be the well-known GUID string.
    /// </summary>
    [Fact]
    public void UserId_IsWellKnownGuidString()
    {
        Assert.Equal("00000000-0000-0000-0000-000000000001", FamilyUserDefaults.UserId);
    }

    /// <summary>
    /// User ID must parse as a valid, non-empty GUID.
    /// </summary>
    [Fact]
    public void UserId_ParsesAsNonEmptyGuid()
    {
        var parsed = Guid.Parse(FamilyUserDefaults.UserId);
        Assert.NotEqual(Guid.Empty, parsed);
    }

    /// <summary>
    /// User name must be "Family".
    /// </summary>
    [Fact]
    public void UserName_IsFamily()
    {
        Assert.Equal("Family", FamilyUserDefaults.UserName);
    }

    /// <summary>
    /// User email must be "family@localhost".
    /// </summary>
    [Fact]
    public void UserEmail_IsFamilyAtLocalhost()
    {
        Assert.Equal("family@localhost", FamilyUserDefaults.UserEmail);
    }
}
