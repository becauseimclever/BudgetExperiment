// <copyright file="IconTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Common;
using BudgetExperiment.Client.Services;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Components.Common;

/// <summary>
/// Unit tests for the <see cref="Icon"/> component.
/// </summary>
public class IconTests : BunitContext, IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IconTests"/> class.
    /// </summary>
    public IconTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
    }

    /// <summary>
    /// Verifies the icon renders an SVG element.
    /// </summary>
    [Fact]
    public void Renders_SvgElement()
    {
        var cut = Render<Icon>(p => p.Add(x => x.Name, "calendar"));

        cut.Find("svg").ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies the icon uses specified size.
    /// </summary>
    [Fact]
    public void UsesSpecifiedSize()
    {
        var cut = Render<Icon>(p => p
            .Add(x => x.Name, "calendar")
            .Add(x => x.Size, 32));

        var svg = cut.Find("svg");
        svg.GetAttribute("width").ShouldBe("32");
        svg.GetAttribute("height").ShouldBe("32");
    }

    /// <summary>
    /// Verifies the icon applies CSS class.
    /// </summary>
    [Fact]
    public void AppliesCssClass()
    {
        var cut = Render<Icon>(p => p
            .Add(x => x.Name, "calendar")
            .Add(x => x.Class, "my-custom-class"));

        var svg = cut.Find("svg");
        svg.ClassList.ShouldContain("my-custom-class");
    }

    /// <summary>
    /// Verifies default size is 20.
    /// </summary>
    [Fact]
    public void DefaultSize_Is20()
    {
        var cut = Render<Icon>(p => p.Add(x => x.Name, "calendar"));

        var svg = cut.Find("svg");
        svg.GetAttribute("width").ShouldBe("20");
    }

    /// <summary>
    /// Verifies title element is rendered when Title is set.
    /// </summary>
    [Fact]
    public void RendersTitle_WhenProvided()
    {
        var cut = Render<Icon>(p => p
            .Add(x => x.Name, "calendar")
            .Add(x => x.Title, "Calendar Icon"));

        cut.Markup.ShouldContain("<title>Calendar Icon</title>");
    }

    /// <summary>
    /// Verifies aria-hidden is true when no title.
    /// </summary>
    [Fact]
    public void AriaHidden_WhenNoTitle()
    {
        var cut = Render<Icon>(p => p.Add(x => x.Name, "calendar"));

        var svg = cut.Find("svg");
        svg.GetAttribute("aria-hidden").ShouldBe("true");
    }

    /// <summary>
    /// Verifies role is img when title is set.
    /// </summary>
    [Fact]
    public void RoleIsImg_WhenTitleSet()
    {
        var cut = Render<Icon>(p => p
            .Add(x => x.Name, "calendar")
            .Add(x => x.Title, "Calendar"));

        var svg = cut.Find("svg");
        svg.GetAttribute("role").ShouldBe("img");
    }

    /// <summary>
    /// Verifies stroke width parameter.
    /// </summary>
    [Fact]
    public void UsesStrokeWidth()
    {
        var cut = Render<Icon>(p => p
            .Add(x => x.Name, "calendar")
            .Add(x => x.StrokeWidth, 3));

        var svg = cut.Find("svg");
        svg.GetAttribute("stroke-width").ShouldBe("3");
    }

    /// <summary>
    /// Verifies various icon names render correctly.
    /// </summary>
    /// <param name="iconName">The icon name to test.</param>
    [Theory]
    [InlineData("calendar")]
    [InlineData("refresh")]
    [InlineData("repeat")]
    [InlineData("transfer")]
    [InlineData("wallet")]
    [InlineData("money")]
    [InlineData("cart")]
    [InlineData("home")]
    [InlineData("utensils")]
    [InlineData("car")]
    [InlineData("heart")]
    [InlineData("plane")]
    [InlineData("gift")]
    [InlineData("plus")]
    [InlineData("pencil")]
    [InlineData("trash")]
    [InlineData("check")]
    [InlineData("x")]
    [InlineData("chevron-left")]
    [InlineData("chevron-right")]
    [InlineData("chevron-up")]
    [InlineData("chevron-down")]
    [InlineData("search")]
    [InlineData("filter")]
    [InlineData("download")]
    [InlineData("upload")]
    [InlineData("settings")]
    [InlineData("user")]
    [InlineData("info")]
    [InlineData("alert-triangle")]
    [InlineData("eye")]
    [InlineData("eye-off")]
    [InlineData("save")]
    [InlineData("copy")]
    [InlineData("arrow-left")]
    [InlineData("arrow-right")]
    [InlineData("list")]
    [InlineData("bar-chart")]
    [InlineData("pie-chart")]
    [InlineData("tag")]
    [InlineData("moon")]
    [InlineData("sun")]
    [InlineData("sparkles")]
    [InlineData("calculator")]
    [InlineData("lightbulb")]
    [InlineData("external-link")]
    [InlineData("spinner")]
    [InlineData("briefcase")]
    [InlineData("bolt")]
    [InlineData("book")]
    [InlineData("film")]
    [InlineData("shopping-bag")]
    [InlineData("trending-up")]
    [InlineData("trending-down")]
    [InlineData("map-pin")]
    [InlineData("palette")]
    [InlineData("laptop")]
    [InlineData("menu")]
    [InlineData("more-vertical")]
    [InlineData("file")]
    [InlineData("help-circle")]
    [InlineData("check-circle")]
    [InlineData("alert-circle")]
    [InlineData("plus-circle")]
    [InlineData("chart-pie")]
    [InlineData("play")]
    [InlineData("pause")]
    [InlineData("skip-forward")]
    [InlineData("minus")]
    [InlineData("upload-cloud")]
    [InlineData("dice")]
    [InlineData("code")]
    [InlineData("monitor")]
    [InlineData("win95-calendar")]
    [InlineData("win95-home")]
    [InlineData("win95-settings")]
    [InlineData("geo-calendar")]
    [InlineData("crayon-calendar")]
    public void RendersIconPath_ForKnownNames(string iconName)
    {
        var cut = Render<Icon>(p => p.Add(x => x.Name, iconName));

        cut.Find("svg").InnerHtml.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies unknown icon name still renders SVG.
    /// </summary>
    [Fact]
    public void UnknownIcon_StillRendersSvg()
    {
        var cut = Render<Icon>(p => p.Add(x => x.Name, "nonexistent-icon-xyz"));

        cut.Find("svg").ShouldNotBeNull();
    }
}
