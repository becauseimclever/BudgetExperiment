// <copyright file="FormFieldTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Common;

namespace BudgetExperiment.Client.Tests.Components.Common;

/// <summary>
/// Unit tests for the FormField component.
/// </summary>
public class FormFieldTests : BunitContext
{
    /// <summary>
    /// Verifies that the form field renders with label.
    /// </summary>
    [Fact]
    public void FormField_RendersLabel()
    {
        // Arrange & Act
        var cut = Render<FormField>(parameters => parameters
            .Add(p => p.Label, "Email Address")
            .AddChildContent("<input type=\"email\" />"));

        // Assert
        var label = cut.Find(".form-label");
        Assert.Contains("Email Address", label.TextContent);
    }

    /// <summary>
    /// Verifies that the form field shows required indicator.
    /// </summary>
    [Fact]
    public void FormField_ShowsRequiredIndicator()
    {
        // Arrange & Act
        var cut = Render<FormField>(parameters => parameters
            .Add(p => p.Label, "Name")
            .Add(p => p.IsRequired, true)
            .AddChildContent("<input type=\"text\" />"));

        // Assert
        var indicator = cut.Find(".required-indicator");
        Assert.Equal("*", indicator.TextContent);
    }

    /// <summary>
    /// Verifies that required indicator is not shown when not required.
    /// </summary>
    [Fact]
    public void FormField_DoesNotShowRequiredIndicatorWhenNotRequired()
    {
        // Arrange & Act
        var cut = Render<FormField>(parameters => parameters
            .Add(p => p.Label, "Optional Field")
            .Add(p => p.IsRequired, false)
            .AddChildContent("<input type=\"text\" />"));

        // Assert
        Assert.Empty(cut.FindAll(".required-indicator"));
    }

    /// <summary>
    /// Verifies that the form field shows validation message.
    /// </summary>
    [Fact]
    public void FormField_ShowsValidationMessage()
    {
        // Arrange & Act
        var cut = Render<FormField>(parameters => parameters
            .Add(p => p.Label, "Email")
            .Add(p => p.ValidationMessage, "Please enter a valid email address.")
            .AddChildContent("<input type=\"email\" />"));

        // Assert
        var validation = cut.Find(".validation-message");
        Assert.Equal("Please enter a valid email address.", validation.TextContent);
    }

    /// <summary>
    /// Verifies that the form field adds has-error class when validation message present.
    /// </summary>
    [Fact]
    public void FormField_AddsHasErrorClassWhenValidationMessagePresent()
    {
        // Arrange & Act
        var cut = Render<FormField>(parameters => parameters
            .Add(p => p.Label, "Email")
            .Add(p => p.ValidationMessage, "Invalid email")
            .AddChildContent("<input type=\"email\" />"));

        // Assert
        var formGroup = cut.Find(".form-group");
        Assert.Contains("has-error", formGroup.ClassList);
    }

    /// <summary>
    /// Verifies that the form field does not have has-error class when no validation message.
    /// </summary>
    [Fact]
    public void FormField_DoesNotHaveHasErrorClassWhenNoValidationMessage()
    {
        // Arrange & Act
        var cut = Render<FormField>(parameters => parameters
            .Add(p => p.Label, "Email")
            .AddChildContent("<input type=\"email\" />"));

        // Assert
        var formGroup = cut.Find(".form-group");
        Assert.DoesNotContain("has-error", formGroup.ClassList);
    }

    /// <summary>
    /// Verifies that the form field shows help text.
    /// </summary>
    [Fact]
    public void FormField_ShowsHelpText()
    {
        // Arrange & Act
        var cut = Render<FormField>(parameters => parameters
            .Add(p => p.Label, "Password")
            .Add(p => p.HelpText, "Must be at least 8 characters.")
            .AddChildContent("<input type=\"password\" />"));

        // Assert
        var helpText = cut.Find(".help-text");
        Assert.Equal("Must be at least 8 characters.", helpText.TextContent);
    }

    /// <summary>
    /// Verifies that validation message hides help text.
    /// </summary>
    [Fact]
    public void FormField_ValidationMessageHidesHelpText()
    {
        // Arrange & Act
        var cut = Render<FormField>(parameters => parameters
            .Add(p => p.Label, "Password")
            .Add(p => p.HelpText, "Must be at least 8 characters.")
            .Add(p => p.ValidationMessage, "Password is too short.")
            .AddChildContent("<input type=\"password\" />"));

        // Assert
        Assert.Single(cut.FindAll(".validation-message"));
        Assert.Empty(cut.FindAll(".help-text"));
    }

    /// <summary>
    /// Verifies that label has correct for attribute.
    /// </summary>
    [Fact]
    public void FormField_LabelHasCorrectForAttribute()
    {
        // Arrange & Act
        var cut = Render<FormField>(parameters => parameters
            .Add(p => p.Label, "Username")
            .Add(p => p.InputId, "username-input")
            .AddChildContent("<input id=\"username-input\" type=\"text\" />"));

        // Assert
        var label = cut.Find(".form-label");
        Assert.Equal("username-input", label.GetAttribute("for"));
    }

    /// <summary>
    /// Verifies that the form field renders child content.
    /// </summary>
    [Fact]
    public void FormField_RendersChildContent()
    {
        // Arrange & Act
        var cut = Render<FormField>(parameters => parameters
            .Add(p => p.Label, "Amount")
            .AddChildContent("<input type=\"number\" class=\"amount-input\" />"));

        // Assert
        var input = cut.Find("input.amount-input");
        Assert.NotNull(input);
    }

    /// <summary>
    /// Verifies that additional classes are applied.
    /// </summary>
    [Fact]
    public void FormField_AppliesAdditionalClasses()
    {
        // Arrange & Act
        var cut = Render<FormField>(parameters => parameters
            .Add(p => p.Label, "Test")
            .Add(p => p.AdditionalClasses, "mb-4 col-6")
            .AddChildContent("<input type=\"text\" />"));

        // Assert
        var formGroup = cut.Find(".form-group");
        Assert.Contains("mb-4", formGroup.ClassList);
        Assert.Contains("col-6", formGroup.ClassList);
    }

    /// <summary>
    /// Verifies that additional attributes are passed through.
    /// </summary>
    [Fact]
    public void FormField_PassesThroughAdditionalAttributes()
    {
        // Arrange & Act
        var cut = Render<FormField>(parameters => parameters
            .Add(p => p.Label, "Test")
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object>
            {
                { "data-testid", "email-field" },
            })
            .AddChildContent("<input type=\"text\" />"));

        // Assert
        var formGroup = cut.Find(".form-group");
        Assert.Equal("email-field", formGroup.GetAttribute("data-testid"));
    }

    /// <summary>
    /// Verifies that the form field works without label.
    /// </summary>
    [Fact]
    public void FormField_WorksWithoutLabel()
    {
        // Arrange & Act
        var cut = Render<FormField>(parameters => parameters
            .Add(p => p.HelpText, "Helper text only")
            .AddChildContent("<input type=\"text\" />"));

        // Assert
        Assert.Empty(cut.FindAll(".form-label"));
        Assert.Single(cut.FindAll(".help-text"));
    }

    /// <summary>
    /// Verifies validation message has role alert for accessibility.
    /// </summary>
    [Fact]
    public void FormField_ValidationMessageHasRoleAlert()
    {
        // Arrange & Act
        var cut = Render<FormField>(parameters => parameters
            .Add(p => p.Label, "Email")
            .Add(p => p.ValidationMessage, "Required field")
            .AddChildContent("<input type=\"email\" />"));

        // Assert
        var validation = cut.Find(".validation-message");
        Assert.Equal("alert", validation.GetAttribute("role"));
    }
}
