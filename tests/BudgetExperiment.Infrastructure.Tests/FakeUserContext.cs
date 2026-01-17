// <copyright file="FakeUserContext.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Fake implementation of <see cref="IUserContext"/> for testing.
/// </summary>
internal sealed class FakeUserContext : IUserContext
{
    /// <summary>
    /// Gets the default user ID used in tests.
    /// </summary>
    public static readonly Guid DefaultUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeUserContext"/> class.
    /// </summary>
    /// <param name="userId">The user ID to use. Defaults to a fixed test GUID.</param>
    /// <param name="currentScope">The current scope filter. Defaults to null (show all).</param>
    public FakeUserContext(Guid? userId = null, BudgetScope? currentScope = null)
    {
        this.UserIdAsGuid = userId ?? DefaultUserId;
        this.UserId = this.UserIdAsGuid.Value.ToString();
        this.CurrentScope = currentScope;
    }

    /// <inheritdoc />
    public bool IsAuthenticated => true;

    /// <inheritdoc />
    public string UserId { get; }

    /// <inheritdoc />
    public Guid? UserIdAsGuid { get; }

    /// <inheritdoc />
    public string Username => "TestUser";

    /// <inheritdoc />
    public string? Email => "test@example.com";

    /// <inheritdoc />
    public string? DisplayName => "Test User";

    /// <inheritdoc />
    public string? AvatarUrl => null;

    /// <inheritdoc />
    public BudgetScope? CurrentScope { get; private set; }

    /// <inheritdoc />
    public void SetScope(BudgetScope? scope)
    {
        this.CurrentScope = scope;
    }

    /// <summary>
    /// Creates a FakeUserContext with the default user ID and null scope (show all).
    /// </summary>
    /// <returns>A new FakeUserContext instance.</returns>
    public static FakeUserContext CreateDefault() => new();

    /// <summary>
    /// Creates a FakeUserContext configured to filter for shared scope only.
    /// </summary>
    /// <returns>A new FakeUserContext instance.</returns>
    public static FakeUserContext CreateForSharedScope() => new(currentScope: BudgetScope.Shared);

    /// <summary>
    /// Creates a FakeUserContext configured to filter for personal scope for the given user.
    /// </summary>
    /// <param name="userId">The user ID for the personal scope.</param>
    /// <returns>A new FakeUserContext instance.</returns>
    public static FakeUserContext CreateForPersonalScope(Guid? userId = null) =>
        new(userId: userId ?? DefaultUserId, currentScope: BudgetScope.Personal);
}
