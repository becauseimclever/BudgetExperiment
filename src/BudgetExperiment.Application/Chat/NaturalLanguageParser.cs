// <copyright file="NaturalLanguageParser.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Chat;

/// <summary>
/// AI-powered implementation of <see cref="INaturalLanguageParser"/>.
/// Orchestrates prompt building, AI invocation, and response parsing
/// via <see cref="ChatActionParser"/>.
/// </summary>
public sealed class NaturalLanguageParser : INaturalLanguageParser
{
    private readonly IAiService _aiService;

    /// <summary>
    /// Initializes a new instance of the <see cref="NaturalLanguageParser"/> class.
    /// </summary>
    /// <param name="aiService">The AI service for processing commands.</param>
    public NaturalLanguageParser(IAiService aiService)
    {
        _aiService = aiService;
    }

    /// <inheritdoc />
    public async Task<ParseResult> ParseCommandAsync(
        string input,
        IReadOnlyList<AccountInfo> accounts,
        IReadOnlyList<CategoryInfo> categories,
        ChatContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new ParseResult(
                Success: false,
                Action: null,
                ResponseText: "Please enter a command.",
                ErrorMessage: "Empty input");
        }

        var accountsText = FormatAccounts(accounts);
        var categoriesText = FormatCategories(categories);
        var contextText = FormatContext(context);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var systemPrompt = ChatAiPrompts.BuildSystemPrompt(accountsText, categoriesText, contextText, today);

        var prompt = new AiPrompt(
            SystemPrompt: systemPrompt,
            UserPrompt: input,
            Temperature: 0.2m,
            MaxTokens: 1500);

        var response = await _aiService.CompleteAsync(prompt, cancellationToken);

        if (!response.Success)
        {
            return new ParseResult(
                Success: false,
                Action: null,
                ResponseText: "Sorry, I couldn't process that. Please try again.",
                ErrorMessage: response.ErrorMessage);
        }

        return ChatActionParser.ParseResponse(response.Content, accounts, categories, context);
    }

    private static string FormatAccounts(IReadOnlyList<AccountInfo> accounts)
    {
        if (accounts.Count == 0)
        {
            return "No accounts configured.";
        }

        var lines = accounts.Select(a => $"- {a.Name} (ID: {a.Id}, Type: {a.Type})");
        return string.Join("\n", lines);
    }

    private static string FormatCategories(IReadOnlyList<CategoryInfo> categories)
    {
        if (categories.Count == 0)
        {
            return "No categories configured.";
        }

        var lines = categories.Select(c => $"- {c.Name} (ID: {c.Id})");
        return string.Join("\n", lines);
    }

    private static string FormatContext(ChatContext? context)
    {
        if (context == null)
        {
            return "No specific context.";
        }

        var parts = new List<string>();

        if (context.CurrentDate.HasValue)
        {
            var dateText = context.CurrentDate.Value.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
            parts.Add($"The user has selected {dateText} on the calendar. Use this date for transactions unless they specify otherwise.");
        }

        if (!string.IsNullOrEmpty(context.CurrentAccountName))
        {
            parts.Add($"The user is viewing the '{context.CurrentAccountName}' account. Use this account as the default unless they specify otherwise.");
        }

        if (!string.IsNullOrEmpty(context.CurrentCategoryName))
        {
            parts.Add($"The user is viewing the '{context.CurrentCategoryName}' category. Use this category as the default unless they specify otherwise.");
        }

        if (!string.IsNullOrEmpty(context.CurrentPage))
        {
            parts.Add($"The user is on the {context.CurrentPage} page.");
        }

        return parts.Count > 0
            ? "Context from the user's current view:\n" + string.Join("\n", parts)
            : "No specific context.";
    }
}
