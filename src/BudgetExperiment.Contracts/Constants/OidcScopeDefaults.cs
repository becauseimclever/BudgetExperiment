// <copyright file="OidcScopeDefaults.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Constants;

/// <summary>
/// Default OIDC scope strings used across the application.
/// Centralizes magic strings to prevent typos and enable single-point updates.
/// </summary>
public static class OidcScopeDefaults
{
    /// <summary>
    /// The OpenID Connect core scope ("openid").
    /// </summary>
    public const string OpenId = "openid";

    /// <summary>
    /// The OIDC profile scope ("profile").
    /// </summary>
    public const string Profile = "profile";

    /// <summary>
    /// The OIDC email scope ("email").
    /// </summary>
    public const string Email = "email";

    /// <summary>
    /// The default set of scopes requested during authentication.
    /// </summary>
    public static readonly string[] DefaultScopes = [OpenId, Profile, Email];
}
