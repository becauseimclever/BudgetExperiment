// <copyright file="FamilyUserContext.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Constants;

namespace BudgetExperiment.Api.Authentication;

/// <summary>
/// Well-known constants for the default "family" user in auth-off mode.
/// All data created while authentication is disabled is scoped to this user,
/// ensuring a consistent and deterministic user identity.
/// Values are sourced from <see cref="FamilyUserDefaults"/> in Contracts.
/// </summary>
public static class FamilyUserContext
{
    /// <summary>
    /// Display name for the family user.
    /// </summary>
    public const string FamilyUserName = FamilyUserDefaults.UserName;

    /// <summary>
    /// Email for the family user (used in audit trails, etc.).
    /// </summary>
    public const string FamilyUserEmail = FamilyUserDefaults.UserEmail;

    /// <summary>
    /// Well-known GUID for the family user in auth-off mode.
    /// This ensures data is consistently scoped to the same "user".
    /// </summary>
    public static readonly Guid FamilyUserId = new(FamilyUserDefaults.UserId);
}
