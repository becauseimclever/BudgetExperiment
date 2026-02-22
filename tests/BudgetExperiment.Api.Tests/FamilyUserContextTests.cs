// <copyright file="FamilyUserContextTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Api.Authentication;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Unit tests for <see cref="FamilyUserContext"/> constants.
/// </summary>
public sealed class FamilyUserContextTests
{
    /// <summary>
    /// The family user ID is the well-known GUID 00000000-0000-0000-0000-000000000001.
    /// </summary>
    [Fact]
    public void FamilyUserId_IsWellKnownGuid()
    {
        var expected = new Guid("00000000-0000-0000-0000-000000000001");
        Assert.Equal(expected, FamilyUserContext.FamilyUserId);
    }

    /// <summary>
    /// The family user ID is not Guid.Empty, as that could collide with default values.
    /// </summary>
    [Fact]
    public void FamilyUserId_IsNotEmpty()
    {
        Assert.NotEqual(Guid.Empty, FamilyUserContext.FamilyUserId);
    }

    /// <summary>
    /// The family user name is "Family".
    /// </summary>
    [Fact]
    public void FamilyUserName_IsFamily()
    {
        Assert.Equal("Family", FamilyUserContext.FamilyUserName);
    }

    /// <summary>
    /// The family user email is "family@localhost".
    /// </summary>
    [Fact]
    public void FamilyUserEmail_IsFamilyAtLocalhost()
    {
        Assert.Equal("family@localhost", FamilyUserContext.FamilyUserEmail);
    }
}
