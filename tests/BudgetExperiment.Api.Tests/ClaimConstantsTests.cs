// <copyright file="ClaimConstantsTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Constants;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Regression guard tests for <see cref="ClaimConstants"/> values.
/// Ensures claim type strings are never accidentally changed.
/// </summary>
public sealed class ClaimConstantsTests
{
    /// <summary>
    /// Subject claim must equal the standard OIDC "sub" value.
    /// </summary>
    [Fact]
    public void Subject_EqualsExpectedValue()
    {
        Assert.Equal("sub", ClaimConstants.Subject);
    }

    /// <summary>
    /// PreferredUsername claim must equal "preferred_username".
    /// </summary>
    [Fact]
    public void PreferredUsername_EqualsExpectedValue()
    {
        Assert.Equal("preferred_username", ClaimConstants.PreferredUsername);
    }

    /// <summary>
    /// Email claim must equal "email".
    /// </summary>
    [Fact]
    public void Email_EqualsExpectedValue()
    {
        Assert.Equal("email", ClaimConstants.Email);
    }

    /// <summary>
    /// Name claim must equal "name".
    /// </summary>
    [Fact]
    public void Name_EqualsExpectedValue()
    {
        Assert.Equal("name", ClaimConstants.Name);
    }

    /// <summary>
    /// Picture claim must equal "picture".
    /// </summary>
    [Fact]
    public void Picture_EqualsExpectedValue()
    {
        Assert.Equal("picture", ClaimConstants.Picture);
    }
}
