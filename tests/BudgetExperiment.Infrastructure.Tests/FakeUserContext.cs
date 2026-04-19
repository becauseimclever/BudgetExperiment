// <copyright file="FakeUserContext.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

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
    public FakeUserContext(Guid? userId = null)
    {
        this.UserIdAsGuid = userId ?? DefaultUserId;
        this.UserId = this.UserIdAsGuid.Value.ToString();
    }

    /// <inheritdoc />
    public bool IsAuthenticated => true;

    /// <inheritdoc />
    public string UserId
    {
        get;
    }

    /// <inheritdoc />
    public Guid? UserIdAsGuid
    {
        get;
    }

    /// <inheritdoc />
    public string Username => "TestUser";

    /// <inheritdoc />
    public string? Email => "test@example.com";

    /// <inheritdoc />
    public string? DisplayName => "Test User";

    /// <inheritdoc />
    public string? AvatarUrl => null;

    /// <summary>
    /// Creates a FakeUserContext with the default user ID.
    /// </summary>
    /// <returns>A new FakeUserContext instance.</returns>
    public static FakeUserContext CreateDefault() => new();
}
