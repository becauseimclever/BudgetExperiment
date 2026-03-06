// <copyright file="OidcScopeDefaultsTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Constants;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Regression guard tests for <see cref="OidcScopeDefaults"/> values.
/// Ensures OIDC scope strings are never accidentally changed.
/// </summary>
public sealed class OidcScopeDefaultsTests
{
    /// <summary>
    /// OpenId scope must equal "openid".
    /// </summary>
    [Fact]
    public void OpenId_EqualsExpectedValue()
    {
        Assert.Equal("openid", OidcScopeDefaults.OpenId);
    }

    /// <summary>
    /// Profile scope must equal "profile".
    /// </summary>
    [Fact]
    public void Profile_EqualsExpectedValue()
    {
        Assert.Equal("profile", OidcScopeDefaults.Profile);
    }

    /// <summary>
    /// Email scope must equal "email".
    /// </summary>
    [Fact]
    public void Email_EqualsExpectedValue()
    {
        Assert.Equal("email", OidcScopeDefaults.Email);
    }

    /// <summary>
    /// DefaultScopes must contain all three standard scopes in order.
    /// </summary>
    [Fact]
    public void DefaultScopes_ContainsAllStandardScopes()
    {
        Assert.Equal(
            new[] { "openid", "profile", "email" },
            OidcScopeDefaults.DefaultScopes);
    }
}
