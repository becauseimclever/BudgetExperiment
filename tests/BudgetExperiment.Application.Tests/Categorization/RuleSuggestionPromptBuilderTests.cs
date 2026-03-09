// <copyright file="RuleSuggestionPromptBuilderTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Ai;
using BudgetExperiment.Application.Categorization;
using BudgetExperiment.Domain.Accounts;
using BudgetExperiment.Domain.Budgeting;
using BudgetExperiment.Domain.Categorization;
using BudgetExperiment.Domain.Common;

namespace BudgetExperiment.Application.Tests.Categorization;

/// <summary>
/// Unit tests for <see cref="RuleSuggestionPromptBuilder"/>.
/// </summary>
public class RuleSuggestionPromptBuilderTests
{
    private static readonly Guid GroceryCategoryId = Guid.NewGuid();
    private static readonly Guid DiningCategoryId = Guid.NewGuid();

    [Fact]
    public void BuildNewRulePrompt_Uses_Enriched_Format_With_Frequency_And_Amounts()
    {
        var categories = CreateCategories();
        var rules = CreateRules(categories);
        var transactions = new List<Transaction>
        {
            CreateTransaction("Debit Card Purchase - WALMART STORE", 42.50m),
            CreateTransaction("Debit Card Purchase - WALMART STORE", 85.00m),
            CreateTransaction("Debit Card Purchase - WALMART STORE", 12.99m),
            CreateTransaction("Debit Card Purchase - STARBUCKS COFFEE", 5.75m),
        };

        var prompt = RuleSuggestionPromptBuilder.BuildNewRulePrompt(transactions, categories, rules);

        // Prompt should contain enriched format: description — count transactions, $min–$max
        Assert.Contains("WALMART STORE", prompt.UserPrompt);
        Assert.Contains("3 transactions", prompt.UserPrompt);
        Assert.Contains("$12.99", prompt.UserPrompt);
        Assert.Contains("$85.00", prompt.UserPrompt);
    }

    [Fact]
    public void BuildNewRulePrompt_Ranks_Descriptions_By_Frequency()
    {
        var categories = CreateCategories();
        var rules = CreateRules(categories);
        var transactions = new List<Transaction>
        {
            CreateTransaction("RARE STORE", 10m),
            CreateTransaction("COMMON STORE", 10m),
            CreateTransaction("COMMON STORE", 20m),
            CreateTransaction("COMMON STORE", 30m),
        };

        var prompt = RuleSuggestionPromptBuilder.BuildNewRulePrompt(transactions, categories, rules);

        var commonIdx = prompt.UserPrompt.IndexOf("COMMON STORE", StringComparison.Ordinal);
        var rareIdx = prompt.UserPrompt.IndexOf("RARE STORE", StringComparison.Ordinal);
        Assert.True(commonIdx < rareIdx, "Higher-frequency descriptions should appear first");
    }

    [Fact]
    public void BuildNewRulePrompt_Caps_Descriptions_At_100()
    {
        var categories = CreateCategories();
        var rules = CreateRules(categories);
        var transactions = Enumerable.Range(1, 150)
            .Select(i => CreateTransaction($"STORE {i}", i * 1.0m))
            .ToList();

        var prompt = RuleSuggestionPromptBuilder.BuildNewRulePrompt(transactions, categories, rules);

        // Numbered lines go up to 100 max. Line "100." should exist but "101. STORE" should not.
        Assert.Contains("100.", prompt.UserPrompt);
        Assert.DoesNotContain("101. STORE", prompt.UserPrompt);
    }

    [Fact]
    public void BuildNewRulePrompt_Deduplicates_Similar_Descriptions()
    {
        var categories = CreateCategories();
        var rules = CreateRules(categories);
        var transactions = new List<Transaction>
        {
            CreateTransaction("Debit Card Purchase - AMAZON MKTPL AMZN COM BIL WA", 10.99m),
            CreateTransaction("Digital Card Purchase - AMAZON MKTPL AMZN COM BIL WA", 29.74m),
        };

        var prompt = RuleSuggestionPromptBuilder.BuildNewRulePrompt(transactions, categories, rules);

        // Both should be cleaned and grouped; should see count of 2
        Assert.Contains("2 transactions", prompt.UserPrompt);
    }

    [Fact]
    public void BuildNewRulePrompt_Contains_SystemPrompt()
    {
        var categories = CreateCategories();
        var rules = CreateRules(categories);
        var transactions = new List<Transaction>
        {
            CreateTransaction("WALMART", 10m),
        };

        var prompt = RuleSuggestionPromptBuilder.BuildNewRulePrompt(transactions, categories, rules);

        Assert.Equal(AiPrompts.SystemPrompt, prompt.SystemPrompt);
    }

    [Fact]
    public void BuildNewRulePrompt_Contains_Categories()
    {
        var categories = CreateCategories();
        var rules = CreateRules(categories);
        var transactions = new List<Transaction>
        {
            CreateTransaction("WALMART", 10m),
        };

        var prompt = RuleSuggestionPromptBuilder.BuildNewRulePrompt(transactions, categories, rules);

        Assert.Contains("Groceries", prompt.UserPrompt);
        Assert.Contains("Dining", prompt.UserPrompt);
    }

    [Fact]
    public void BuildNewRulePrompt_Contains_ExistingRules()
    {
        var categories = CreateCategories();
        var rules = CreateRules(categories);
        var transactions = new List<Transaction>
        {
            CreateTransaction("WALMART", 10m),
        };

        var prompt = RuleSuggestionPromptBuilder.BuildNewRulePrompt(transactions, categories, rules);

        Assert.Contains("Grocery Rule", prompt.UserPrompt);
        Assert.Contains("GROCERY", prompt.UserPrompt);
    }

    [Fact]
    public void FormatDescriptionGroups_Formats_With_Count_And_AmountRange()
    {
        var groups = new List<DescriptionGroup>
        {
            new("AMAZON MKTPL", 47, 5.99m, 249.99m),
            new("STARBUCKS", 12, 4.50m, 7.25m),
        };

        var result = AiPrompts.FormatDescriptionGroups(groups);

        Assert.Contains("1. AMAZON MKTPL — 47 transactions, $5.99–$249.99", result);
        Assert.Contains("2. STARBUCKS — 12 transactions, $4.50–$7.25", result);
    }

    [Fact]
    public void FormatDescriptionGroups_Handles_Single_Transaction()
    {
        var groups = new List<DescriptionGroup>
        {
            new("RARE PURCHASE", 1, 100.00m, 100.00m),
        };

        var result = AiPrompts.FormatDescriptionGroups(groups);

        Assert.Contains("1. RARE PURCHASE — 1 transaction, $100.00", result);
    }

    [Fact]
    public void FormatDescriptionGroups_Handles_Null_Amounts()
    {
        var groups = new List<DescriptionGroup>
        {
            new("UNKNOWN STORE", 5, null, null),
        };

        var result = AiPrompts.FormatDescriptionGroups(groups);

        Assert.Contains("1. UNKNOWN STORE — 5 transactions", result);
        Assert.DoesNotContain("$", result);
    }

    [Fact]
    public void BuildNewRulePrompt_Contains_FewShot_Examples()
    {
        var categories = CreateCategories();
        var rules = CreateRules(categories);
        var transactions = new List<Transaction>
        {
            CreateTransaction("WALMART", 10m),
        };

        var prompt = RuleSuggestionPromptBuilder.BuildNewRulePrompt(transactions, categories, rules);

        Assert.Contains("GOOD SUGGESTION EXAMPLE", prompt.UserPrompt);
        Assert.Contains("BAD SUGGESTION EXAMPLE", prompt.UserPrompt);
    }

    [Fact]
    public void SystemPrompt_Contains_DomainSpecific_Guidance()
    {
        Assert.Contains("merchant", AiPrompts.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bank", AiPrompts.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildNewRulePrompt_Includes_Dismissed_Patterns_In_Prompt()
    {
        var categories = CreateCategories();
        var rules = CreateRules(categories);
        var transactions = new List<Transaction>
        {
            CreateTransaction("WALMART", 10m),
        };
        var feedback = new SuggestionFeedbackContext(
            DismissedPatterns: new[] { "PAYPAL", "VENMO PAYMENT" },
            AcceptedExamples: Array.Empty<AcceptedSuggestionExample>());

        var prompt = RuleSuggestionPromptBuilder.BuildNewRulePrompt(
            transactions, categories, rules, feedback);

        Assert.Contains("PAYPAL", prompt.UserPrompt);
        Assert.Contains("VENMO PAYMENT", prompt.UserPrompt);
        Assert.Contains("DO NOT", prompt.UserPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildNewRulePrompt_Includes_Accepted_Examples_In_Prompt()
    {
        var categories = CreateCategories();
        var rules = CreateRules(categories);
        var transactions = new List<Transaction>
        {
            CreateTransaction("WALMART", 10m),
        };
        var feedback = new SuggestionFeedbackContext(
            DismissedPatterns: Array.Empty<string>(),
            AcceptedExamples: new[]
            {
                new AcceptedSuggestionExample("STARBUCKS", "Dining"),
                new AcceptedSuggestionExample("KROGER", "Groceries"),
            });

        var prompt = RuleSuggestionPromptBuilder.BuildNewRulePrompt(
            transactions, categories, rules, feedback);

        Assert.Contains("STARBUCKS", prompt.UserPrompt);
        Assert.Contains("Dining", prompt.UserPrompt);
        Assert.Contains("KROGER", prompt.UserPrompt);
    }

    [Fact]
    public void BuildNewRulePrompt_Omits_Feedback_Sections_When_No_Feedback()
    {
        var categories = CreateCategories();
        var rules = CreateRules(categories);
        var transactions = new List<Transaction>
        {
            CreateTransaction("WALMART", 10m),
        };
        var feedback = new SuggestionFeedbackContext(
            DismissedPatterns: Array.Empty<string>(),
            AcceptedExamples: Array.Empty<AcceptedSuggestionExample>());

        var prompt = RuleSuggestionPromptBuilder.BuildNewRulePrompt(
            transactions, categories, rules, feedback);

        Assert.DoesNotContain("PREVIOUSLY DISMISSED", prompt.UserPrompt);
        Assert.DoesNotContain("PREVIOUSLY ACCEPTED", prompt.UserPrompt);
    }

    [Fact]
    public void BuildNewRulePrompt_Without_Feedback_Has_Same_Output_As_Null_Feedback()
    {
        var categories = CreateCategories();
        var rules = CreateRules(categories);
        var transactions = new List<Transaction>
        {
            CreateTransaction("WALMART", 10m),
        };

        var promptWithoutFeedback = RuleSuggestionPromptBuilder.BuildNewRulePrompt(
            transactions, categories, rules);
        var promptWithNullFeedback = RuleSuggestionPromptBuilder.BuildNewRulePrompt(
            transactions, categories, rules, feedback: null);

        Assert.Equal(promptWithoutFeedback.UserPrompt, promptWithNullFeedback.UserPrompt);
    }

    [Fact]
    public void FormatDismissedPatterns_Formats_As_Bullet_List()
    {
        var patterns = new[] { "PAYPAL", "VENMO", "ZELLE" };

        var result = AiPrompts.FormatDismissedPatterns(patterns);

        Assert.Contains("- PAYPAL", result);
        Assert.Contains("- VENMO", result);
        Assert.Contains("- ZELLE", result);
    }

    [Fact]
    public void FormatAcceptedExamples_Formats_With_Pattern_And_Category()
    {
        var examples = new[]
        {
            new AcceptedSuggestionExample("STARBUCKS", "Dining"),
            new AcceptedSuggestionExample("WALMART", "Groceries"),
        };

        var result = AiPrompts.FormatAcceptedExamples(examples);

        Assert.Contains("STARBUCKS", result);
        Assert.Contains("Dining", result);
        Assert.Contains("WALMART", result);
        Assert.Contains("Groceries", result);
    }

    private static IReadOnlyList<BudgetCategory> CreateCategories()
    {
        var groceries = BudgetCategory.Create("Groceries", CategoryType.Expense);
        typeof(BudgetCategory).GetProperty("Id")!.SetValue(groceries, GroceryCategoryId);
        var dining = BudgetCategory.Create("Dining", CategoryType.Expense);
        typeof(BudgetCategory).GetProperty("Id")!.SetValue(dining, DiningCategoryId);
        return new List<BudgetCategory> { groceries, dining };
    }

    private static IReadOnlyList<CategorizationRule> CreateRules(IReadOnlyList<BudgetCategory> categories)
    {
        return new List<CategorizationRule>
        {
            CategorizationRule.Create("Grocery Rule", RuleMatchType.Contains, "GROCERY", GroceryCategoryId),
        };
    }

    private static Transaction CreateTransaction(string description, decimal amount)
    {
        return Transaction.Create(
            Guid.NewGuid(),
            MoneyValue.Create("USD", amount),
            DateOnly.FromDateTime(DateTime.UtcNow),
            description);
    }
}
