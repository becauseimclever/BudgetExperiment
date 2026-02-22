// <copyright file="NoAuthAuthenticationStateProviderTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Security.Claims;

using BudgetExperiment.Client.Services;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="NoAuthAuthenticationStateProvider"/>.
/// </summary>
public class NoAuthAuthenticationStateProviderTests
{
    [Fact]
    public async Task GetAuthenticationStateAsync_ReturnsAuthenticatedState()
    {
        // Arrange
        var provider = new NoAuthAuthenticationStateProvider();

        // Act
        var state = await provider.GetAuthenticationStateAsync();

        // Assert
        Assert.True(state.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_HasFamilyUserId()
    {
        // Arrange
        var provider = new NoAuthAuthenticationStateProvider();

        // Act
        var state = await provider.GetAuthenticationStateAsync();

        // Assert
        var nameId = state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Assert.Equal(NoAuthAuthenticationStateProvider.FamilyUserId, nameId);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_HasFamilyUserName()
    {
        // Arrange
        var provider = new NoAuthAuthenticationStateProvider();

        // Act
        var state = await provider.GetAuthenticationStateAsync();

        // Assert
        var name = state.User.FindFirst(ClaimTypes.Name)?.Value;
        Assert.Equal(NoAuthAuthenticationStateProvider.FamilyUserName, name);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_HasFamilyUserEmail()
    {
        // Arrange
        var provider = new NoAuthAuthenticationStateProvider();

        // Act
        var state = await provider.GetAuthenticationStateAsync();

        // Assert
        var email = state.User.FindFirst(ClaimTypes.Email)?.Value;
        Assert.Equal(NoAuthAuthenticationStateProvider.FamilyUserEmail, email);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_HasSubClaim()
    {
        // Arrange
        var provider = new NoAuthAuthenticationStateProvider();

        // Act
        var state = await provider.GetAuthenticationStateAsync();

        // Assert
        var sub = state.User.FindFirst("sub")?.Value;
        Assert.Equal(NoAuthAuthenticationStateProvider.FamilyUserId, sub);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_UsesNoAuthScheme()
    {
        // Arrange
        var provider = new NoAuthAuthenticationStateProvider();

        // Act
        var state = await provider.GetAuthenticationStateAsync();

        // Assert
        Assert.Equal("NoAuth", state.User.Identity?.AuthenticationType);
    }

    [Fact]
    public void FamilyUserId_IsWellKnownGuid()
    {
        // The family user ID must match the API's FamilyUserContext constant
        Assert.Equal("00000000-0000-0000-0000-000000000001", NoAuthAuthenticationStateProvider.FamilyUserId);
    }

    [Fact]
    public void FamilyUserName_IsFamily()
    {
        Assert.Equal("Family", NoAuthAuthenticationStateProvider.FamilyUserName);
    }

    [Fact]
    public void FamilyUserEmail_IsFamilyAtLocalhost()
    {
        Assert.Equal("family@localhost", NoAuthAuthenticationStateProvider.FamilyUserEmail);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ReturnsSameStateOnMultipleCalls()
    {
        // Arrange
        var provider = new NoAuthAuthenticationStateProvider();

        // Act
        var state1 = await provider.GetAuthenticationStateAsync();
        var state2 = await provider.GetAuthenticationStateAsync();

        // Assert - should be the same cached instance
        Assert.Same(state1, state2);
    }
}
