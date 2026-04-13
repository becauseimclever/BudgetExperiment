// <copyright file="RecurringTransferActionParser.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.Json;

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Chat;

/// <summary>
/// Parses the <c>recurring_transfer</c> intent from an AI response data element
/// into a <see cref="CreateRecurringTransferAction"/>.
/// </summary>
internal static class RecurringTransferActionParser
{
    /// <summary>
    /// Builds a <see cref="CreateRecurringTransferAction"/> from the data element.
    /// </summary>
    /// <param name="data">The <c>data</c> JSON element from the AI response.</param>
    /// <param name="accounts">Available accounts for name-to-ID resolution.</param>
    /// <returns>The constructed action, or <c>null</c> if accounts or recurrence cannot be resolved.</returns>
    internal static CreateRecurringTransferAction? Parse(
        JsonElement data,
        IReadOnlyList<AccountInfo> accounts)
    {
        if (data.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        var fromAccountId = ChatParserHelpers.ParseGuid(data, "fromAccountId");
        var fromAccountName = data.TryGetProperty("fromAccountName", out var fanProp) ? fanProp.GetString() ?? string.Empty : string.Empty;
        var toAccountId = ChatParserHelpers.ParseGuid(data, "toAccountId");
        var toAccountName = data.TryGetProperty("toAccountName", out var tanProp) ? tanProp.GetString() ?? string.Empty : string.Empty;
        var amount = data.TryGetProperty("amount", out var amtProp) ? amtProp.GetDecimal() : 0m;
        var description = data.TryGetProperty("description", out var descProp) && descProp.ValueKind == JsonValueKind.String
            ? descProp.GetString()
            : null;
        var startDate = ChatParserHelpers.ParseDate(data, "startDate") ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = ChatParserHelpers.ParseDate(data, "endDate");

        var recurrence = ChatParserHelpers.ParseRecurrencePattern(data);
        if (recurrence == null)
        {
            return null;
        }

        if (!fromAccountId.HasValue)
        {
            var matchedFrom = accounts.FirstOrDefault(a =>
                a.Name.Equals(fromAccountName, StringComparison.OrdinalIgnoreCase));
            if (matchedFrom != null)
            {
                fromAccountId = matchedFrom.Id;
                fromAccountName = matchedFrom.Name;
            }
            else
            {
                return null;
            }
        }

        if (!toAccountId.HasValue)
        {
            var matchedTo = accounts.FirstOrDefault(a =>
                a.Name.Equals(toAccountName, StringComparison.OrdinalIgnoreCase));
            if (matchedTo != null)
            {
                toAccountId = matchedTo.Id;
                toAccountName = matchedTo.Name;
            }
            else
            {
                return null;
            }
        }

        return new CreateRecurringTransferAction
        {
            FromAccountId = fromAccountId.Value,
            FromAccountName = fromAccountName,
            ToAccountId = toAccountId.Value,
            ToAccountName = toAccountName,
            Amount = Math.Abs(amount),
            Description = description,
            Recurrence = recurrence,
            StartDate = startDate,
            EndDate = endDate,
        };
    }
}
