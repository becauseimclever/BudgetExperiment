// <copyright file="AuthenticationConfigDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Authentication configuration for the client.
/// </summary>
public sealed class AuthenticationConfigDto
{
    /// <summary>
    /// Gets or sets the authentication mode: "none" or "oidc".
    /// When "none", authentication is disabled and all users get default scope.
    /// </summary>
    public required string Mode { get; init; }

    /// <summary>
    /// Gets or sets the OIDC provider settings (populated when Mode = "oidc").
    /// </summary>
    public OidcConfigDto? Oidc { get; init; }
}
