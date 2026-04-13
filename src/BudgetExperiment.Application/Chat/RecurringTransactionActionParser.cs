// <copyright file="RecurringTransactionActionParser.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.Json;

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Chat;

/// <summary>
/// Parses the <c>recurring_transaction</c> intent from an AI response data element
/// into a <see cref="CreateRecurringTransactionAction"/>.
/// </summary>
internal static class RecurringTransactionActionParser
{
    /// <summary>
    /// Builds a <see cref="CreateRecurringTransactionAction"/> from the data element.
    /// </summary>
    /// <param name="data">The <c>data</c> JSON element from the AI response.</param>
    /// <param name="accounts">Available accounts for name-to-ID resolution.</param>
    /// <param name="categories">Available categories (unused currently; reserved for future use).</param>
    /// <returns>The constructed action, or <c>null</c> if account or recurrence cannot be resolved.</returns>
    internal static CreateRecurringTransactionAction? Parse(
        JsonElement data,
        IReadOnlyList<AccountInfo> accounts,
        IReadOnlyList<CategoryInfo> categories)
    {
        if (data.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        var accountId = ChatParserHelpers.ParseGuid(data, "accountId");
        var accountName = data.TryGetProperty("accountName", out var anProp) ? anProp.GetString() ?? string.Empty : string.Empty;
        var amount = data.TryGetProperty("amount", out var amtProp) ? amtProp.GetDecimal() : 0m;
        var description = data.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? string.Empty : string.Empty;
        var category = data.TryGetProperty("category", out var catProp) && catProp.ValueKind == JsonValueKind.String
            ? catProp.GetString()
            : null;
        var startDate = ChatParserHelpers.ParseDate(data, "startDate") ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = ChatParserHelpers.ParseDate(data, "endDate");

        var recurrence = ChatParserHelpers.ParseRecurrencePattern(data);
        if (recurrence == null)
        {
            return null;
        }

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

        return new CreateRecurringTransactionAction
        {
            AccountId = accountId.Value,
            AccountName = accountName,
            Amount = amount,
            Description = description,
            Category = category,
            Recurrence = recurrence,
            StartDate = startDate,
            EndDate = endDate,
        };
    }
}
