// <copyright file="RulesPage.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.E2E.Tests.PageObjects;

/// <summary>
/// Page object for the Categorization Rules page.
/// </summary>
public class RulesPage : BasePage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RulesPage"/> class.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    public RulesPage(IPage page)
        : base(page)
    {
    }

    /// <summary>
    /// Gets the Add Rule button.
    /// </summary>
    public ILocator AddRuleButton => Page.GetByRole(AriaRole.Button, new() { Name = "Add Rule" });

    /// <summary>
    /// Gets all rule items.
    /// </summary>
    public ILocator RuleItems => Page.Locator(".rule-item, .card, table tbody tr");

    /// <summary>
    /// Gets the empty message.
    /// </summary>
    public ILocator EmptyMessage => Page.Locator(".empty-message");

    /// <summary>
    /// Gets the count of rules.
    /// </summary>
    /// <returns>The number of rule items.</returns>
    public async Task<int> GetRuleCountAsync()
    {
        return await RuleItems.CountAsync();
    }

    /// <summary>
    /// Finds a rule by pattern text.
    /// </summary>
    /// <param name="pattern">The rule pattern text.</param>
    /// <returns>The rule item locator.</returns>
    public ILocator GetRuleByPattern(string pattern)
    {
        return RuleItems.Filter(new() { HasText = pattern });
    }
}
