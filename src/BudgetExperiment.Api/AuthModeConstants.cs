// <copyright file="AuthModeConstants.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Api;

/// <summary>
/// String constants for authentication mode values.
/// </summary>
public static class AuthModeConstants
{
    /// <summary>
    /// Authentication is disabled. All requests are treated as authenticated
    /// using the family user context.
    /// </summary>
    public const string None = "None";

    /// <summary>
    /// Authentication is enabled via an OIDC provider (Authentik, Google, Microsoft, or generic OIDC).
    /// This is the default mode.
    /// </summary>
    public const string Oidc = "OIDC";
}
