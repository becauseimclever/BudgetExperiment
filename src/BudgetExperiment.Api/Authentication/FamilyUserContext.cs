// <copyright file="FamilyUserContext.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Api.Authentication;

/// <summary>
/// Well-known constants for the default "family" user in auth-off mode.
/// All data created while authentication is disabled is scoped to this user,
/// ensuring a consistent and deterministic user identity.
/// </summary>
public static class FamilyUserContext
{
    /// <summary>
    /// Well-known GUID for the family user in auth-off mode.
    /// This ensures data is consistently scoped to the same "user".
    /// </summary>
    public static readonly Guid FamilyUserId = new("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Display name for the family user.
    /// </summary>
    public const string FamilyUserName = "Family";

    /// <summary>
    /// Email for the family user (used in audit trails, etc.).
    /// </summary>
    public const string FamilyUserEmail = "family@localhost";
}
