// <copyright file="AuthProviderConstants.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Api;

/// <summary>
/// String constants for authentication provider values.
/// </summary>
public static class AuthProviderConstants
{
    /// <summary>
    /// Authentik OIDC provider (default, current production provider).
    /// </summary>
    public const string Authentik = "Authentik";

    /// <summary>
    /// Google OAuth provider.
    /// </summary>
    public const string Google = "Google";

    /// <summary>
    /// Microsoft Entra ID (Azure AD) provider.
    /// </summary>
    public const string Microsoft = "Microsoft";

    /// <summary>
    /// Generic OIDC provider (Keycloak, Auth0, Okta, etc.).
    /// </summary>
    public const string Oidc = "OIDC";
}
