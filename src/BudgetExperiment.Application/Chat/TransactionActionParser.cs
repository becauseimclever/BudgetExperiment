// <copyright file="TransactionActionParser.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.Json;

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Chat;

/// <summary>
/// Parses the <c>transaction</c> intent from an AI response data element
/// into a <see cref="CreateTransactionAction"/>.
/// </summary>
internal static class TransactionActionParser
{
    /// <summary>
    /// Builds a <see cref="CreateTransactionAction"/> from the data element, resolving account by name
    /// when no <c>accountId</c> is present.
    /// </summary>
    /// <param name="data">The <c>data</c> JSON element from the AI response.</param>
    /// <param name="accounts">Available accounts for name-to-ID resolution.</param>
    /// <param name="categories">Available categories for Kakeibo resolution.</param>
    /// <param name="context">Optional chat context with date/account defaults.</param>
    /// <returns>The constructed action, or <c>null</c> if account cannot be resolved.</returns>
    internal static CreateTransactionAction? Parse(
        JsonElement data,
        IReadOnlyList<AccountInfo> accounts,
        IReadOnlyList<CategoryInfo> categories,
        ChatContext? context)
    {
        if (data.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        var accountId = ChatParserHelpers.ParseGuid(data, "accountId");
        var accountName = data.TryGetProperty("accountName", out var anProp) ? anProp.GetString() ?? string.Empty : string.Empty;
        var amount = data.TryGetProperty("amount", out var amtProp) ? amtProp.GetDecimal() : 0m;
        var date = ChatParserHelpers.ParseDate(data, "date") ?? context?.CurrentDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var description = data.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? string.Empty : string.Empty;
        var category = data.TryGetProperty("category", out var catProp) && catProp.ValueKind == JsonValueKind.String
            ? catProp.GetString()
            : null;
        var categoryId = ChatParserHelpers.ParseGuid(data, "categoryId");
        var kakeiboCategory = ChatParserHelpers.ResolveKakeiboCategory(categoryId, categories);

        if (!accountId.HasValue)
        {
            var matchedAccount = accounts.FirstOrDefault(a =>
                a.Name.Equals(accountName, StringComparison.OrdinalIgnoreCase));
            if (matchedAccount != null)
            {
                accountId = matchedAccount.Id;
                accountName = matchedAccount.Name;
            }
            else if (accounts.Count == 1)
            {
                accountId = accounts[0].Id;
                accountName = accounts[0].Name;
            }
            else
            {
                return null;
            }
        }

        return new CreateTransactionAction
        {
            AccountId = accountId.Value,
            AccountName = accountName,
            Amount = amount,
            Date = date,
            Description = description,
            Category = category,
            CategoryId = categoryId,
            KakeiboCategory = kakeiboCategory,
        };
    }
}
