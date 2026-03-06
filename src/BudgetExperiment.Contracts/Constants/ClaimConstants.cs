// <copyright file="ClaimConstants.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Constants;

/// <summary>
/// Standard OIDC claim type strings used across the application.
/// Centralizes magic strings to prevent typos and enable single-point updates.
/// </summary>
public static class ClaimConstants
{
    /// <summary>
    /// The OIDC subject identifier claim ("sub").
    /// </summary>
    public const string Subject = "sub";

    /// <summary>
    /// The OIDC preferred username claim ("preferred_username").
    /// </summary>
    public const string PreferredUsername = "preferred_username";

    /// <summary>
    /// The OIDC email claim ("email").
    /// </summary>
    public const string Email = "email";

    /// <summary>
    /// The OIDC display name claim ("name").
    /// </summary>
    public const string Name = "name";

    /// <summary>
    /// The OIDC picture/avatar URL claim ("picture").
    /// </summary>
    public const string Picture = "picture";
}
