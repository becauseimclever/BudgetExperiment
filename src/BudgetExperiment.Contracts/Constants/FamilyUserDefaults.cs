// <copyright file="FamilyUserDefaults.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Constants;

/// <summary>
/// Well-known identity values for the default "family" user in auth-off mode.
/// Shared between Api and Client to keep values in sync.
/// </summary>
public static class FamilyUserDefaults
{
    /// <summary>
    /// Well-known GUID (as string) for the family user.
    /// </summary>
    public const string UserId = "00000000-0000-0000-0000-000000000001";

    /// <summary>
    /// Display name for the family user.
    /// </summary>
    public const string UserName = "Family";

    /// <summary>
    /// Email for the family user.
    /// </summary>
    public const string UserEmail = "family@localhost";
}
