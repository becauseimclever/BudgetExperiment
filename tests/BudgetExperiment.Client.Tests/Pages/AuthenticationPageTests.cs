// <copyright file="AuthenticationPageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Pages;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Pages;

/// <summary>
/// Tests for the <see cref="Authentication"/> page in auth-off mode.
/// </summary>
public class AuthenticationPageTests : BunitContext
{
    [Fact]
    public void Authentication_RedirectsToHome_WhenModeIsNone()
    {
        // Arrange
        Services.AddSingleton(new AuthenticationConfigDto { Mode = "none" });

        // Act
        var cut = Render<Authentication>(parameters => parameters
            .Add(p => p.Action, "login"));

        // Assert — nothing rendered (redirect happened)
        Assert.Empty(cut.Markup.Trim());

        // Verify navigation to home via bUnit's NavigationManager
        var navManager = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        Assert.EndsWith("/", navManager.Uri);
    }

    [Fact]
    public void Authentication_RedirectsToHome_ForLogoutAction_WhenModeIsNone()
    {
        // Arrange
        Services.AddSingleton(new AuthenticationConfigDto { Mode = "none" });

        // Act
        var cut = Render<Authentication>(parameters => parameters
            .Add(p => p.Action, "logout"));

        // Assert — nothing rendered
        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void Authentication_RedirectsToHome_ForCallbackAction_WhenModeIsNone()
    {
        // Arrange
        Services.AddSingleton(new AuthenticationConfigDto { Mode = "none" });

        // Act
        var cut = Render<Authentication>(parameters => parameters
            .Add(p => p.Action, "login-callback"));

        // Assert — nothing rendered
        Assert.Empty(cut.Markup.Trim());
    }
}
