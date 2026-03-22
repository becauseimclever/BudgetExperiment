// <copyright file="ChatActionParser.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;
using System.Text.Json;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Chat;

/// <summary>
/// Parses AI response JSON into <see cref="ChatAction"/> objects.
/// Handles JSON extraction, intent routing, and action DTO construction.
/// </summary>
public static class ChatActionParser
{
    /// <summary>
    /// Parses a raw AI response string into a <see cref="ParseResult"/>.
    /// Extracts JSON, determines intent, and constructs the appropriate action.
    /// </summary>
    /// <param name="content">The raw AI response text.</param>
    /// <param name="accounts">Available accounts for name-to-ID resolution.</param>
    /// <param name="categories">Available categories for name-to-ID resolution.</param>
    /// <param name="context">Optional chat context with defaults.</param>
    /// <returns>The parsed result.</returns>
    public static ParseResult ParseResponse(
        string content,
        IReadOnlyList<AccountInfo> accounts,
        IReadOnlyList<CategoryInfo> categories,
        ChatContext? context)
    {
        try
        {
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');

            if (jsonStart < 0 || jsonEnd < 0)
            {
                return new ParseResult(
                    Success: false,
                    Action: null,
                    ResponseText: "I couldn't understand that. Could you rephrase?",
                    ErrorMessage: "No JSON found in AI response");
            }

            var jsonContent = content.Substring(jsonStart, jsonEnd - jsonStart + 1);
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            var intent = root.GetProperty("intent").GetString() ?? "unknown";
            var confidence = root.TryGetProperty("confidence", out var confProp) ? confProp.GetDecimal() : 0.5m;
            var responseText = root.TryGetProperty("response", out var respProp)
                ? respProp.GetString() ?? "I processed your request."
                : "I processed your request.";

            if (root.TryGetProperty("clarification", out var clarProp) &&
                clarProp.TryGetProperty("needed", out var neededProp) &&
                neededProp.GetBoolean())
            {
                var clarificationAction = ParseClarification(clarProp);
                return new ParseResult(
                    Success: true,
                    Action: clarificationAction,
                    ResponseText: responseText,
                    Confidence: confidence);
            }

            var data = root.TryGetProperty("data", out var dataProp) ? dataProp : default;
            ChatAction? action = intent switch
            {
                "transaction" => ParseTransactionAction(data, accounts, categories, context),
                "transfer" => ParseTransferAction(data, accounts, context),
                "recurring_transaction" => ParseRecurringTransactionAction(data, accounts, categories),
                "recurring_transfer" => ParseRecurringTransferAction(data, accounts),
                "unknown" => null,
                _ => null,
            };

            if (action == null && intent != "unknown")
            {
                return new ParseResult(
                    Success: false,
                    Action: null,
                    ResponseText: responseText,
                    ErrorMessage: $"Failed to parse {intent} action");
            }

            return new ParseResult(
                Success: action != null,
                Action: action,
                ResponseText: responseText,
                Confidence: confidence);
        }
        catch (JsonException ex)
        {
            return new ParseResult(
                Success: false,
                Action: null,
                ResponseText: "I had trouble understanding that. Could you try again?",
                ErrorMessage: $"JSON parse error: {ex.Message}");
        }
    }

    internal static ClarificationNeededAction ParseClarification(JsonElement clarProp)
    {
        var question = clarProp.TryGetProperty("question", out var qProp)
            ? qProp.GetString() ?? "Could you provide more details?"
            : "Could you provide more details?";

        var fieldName = clarProp.TryGetProperty("field", out var fProp)
            ? fProp.GetString() ?? "unknown"
            : "unknown";

        var options = new List<ClarificationOption>();
        if (clarProp.TryGetProperty("options", out var optsProp) && optsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var opt in optsProp.EnumerateArray())
            {
                options.Add(ParseClarificationOption(opt));
            }
        }

        return new ClarificationNeededAction
        {
            Question = question,
            FieldName = fieldName,
            Options = options,
        };
    }

    internal static CreateTransactionAction? ParseTransactionAction(
        JsonElement data,
        IReadOnlyList<AccountInfo> accounts,
        IReadOnlyList<CategoryInfo> categories,
        ChatContext? context)
    {
        if (data.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        var accountId = ParseGuid(data, "accountId");
        var accountName = data.TryGetProperty("accountName", out var anProp) ? anProp.GetString() ?? string.Empty : string.Empty;
        var amount = data.TryGetProperty("amount", out var amtProp) ? amtProp.GetDecimal() : 0m;
        var date = ParseDate(data, "date") ?? context?.CurrentDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var description = data.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? string.Empty : string.Empty;
        var category = data.TryGetProperty("category", out var catProp) && catProp.ValueKind == JsonValueKind.String
            ? catProp.GetString()
            : null;
        var categoryId = ParseGuid(data, "categoryId");

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
        };
    }

    internal static CreateTransferAction? ParseTransferAction(
        JsonElement data,
        IReadOnlyList<AccountInfo> accounts,
        ChatContext? context)
    {
        if (data.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        var fromAccountId = ParseGuid(data, "fromAccountId");
        var fromAccountName = data.TryGetProperty("fromAccountName", out var fanProp) ? fanProp.GetString() ?? string.Empty : string.Empty;
        var toAccountId = ParseGuid(data, "toAccountId");
        var toAccountName = data.TryGetProperty("toAccountName", out var tanProp) ? tanProp.GetString() ?? string.Empty : string.Empty;
        var amount = data.TryGetProperty("amount", out var amtProp) ? amtProp.GetDecimal() : 0m;
        var date = ParseDate(data, "date") ?? context?.CurrentDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var description = data.TryGetProperty("description", out var descProp) && descProp.ValueKind == JsonValueKind.String
            ? descProp.GetString()
            : null;

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

        return new CreateTransferAction
        {
            FromAccountId = fromAccountId.Value,
            FromAccountName = fromAccountName,
            ToAccountId = toAccountId.Value,
            ToAccountName = toAccountName,
            Amount = Math.Abs(amount),
            Date = date,
            Description = description,
        };
    }

    internal static CreateRecurringTransactionAction? ParseRecurringTransactionAction(
        JsonElement data,
        IReadOnlyList<AccountInfo> accounts,
        IReadOnlyList<CategoryInfo> categories)
    {
        if (data.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        var accountId = ParseGuid(data, "accountId");
        var accountName = data.TryGetProperty("accountName", out var anProp) ? anProp.GetString() ?? string.Empty : string.Empty;
        var amount = data.TryGetProperty("amount", out var amtProp) ? amtProp.GetDecimal() : 0m;
        var description = data.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? string.Empty : string.Empty;
        var category = data.TryGetProperty("category", out var catProp) && catProp.ValueKind == JsonValueKind.String
            ? catProp.GetString()
            : null;
        var startDate = ParseDate(data, "startDate") ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = ParseDate(data, "endDate");

        var recurrence = ParseRecurrencePattern(data);
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

    internal static CreateRecurringTransferAction? ParseRecurringTransferAction(
        JsonElement data,
        IReadOnlyList<AccountInfo> accounts)
    {
        if (data.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        var fromAccountId = ParseGuid(data, "fromAccountId");
        var fromAccountName = data.TryGetProperty("fromAccountName", out var fanProp) ? fanProp.GetString() ?? string.Empty : string.Empty;
        var toAccountId = ParseGuid(data, "toAccountId");
        var toAccountName = data.TryGetProperty("toAccountName", out var tanProp) ? tanProp.GetString() ?? string.Empty : string.Empty;
        var amount = data.TryGetProperty("amount", out var amtProp) ? amtProp.GetDecimal() : 0m;
        var description = data.TryGetProperty("description", out var descProp) && descProp.ValueKind == JsonValueKind.String
            ? descProp.GetString()
            : null;
        var startDate = ParseDate(data, "startDate") ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = ParseDate(data, "endDate");

        var recurrence = ParseRecurrencePattern(data);
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

    internal static RecurrencePatternValue? ParseRecurrencePattern(JsonElement data)
    {
        var frequencyStr = data.TryGetProperty("frequency", out var freqProp)
            ? freqProp.GetString()?.ToLowerInvariant() ?? "monthly"
            : "monthly";

        var interval = data.TryGetProperty("interval", out var intProp) ? intProp.GetInt32() : 1;
        var dayOfMonth = data.TryGetProperty("dayOfMonth", out var domProp) && domProp.ValueKind == JsonValueKind.Number
            ? domProp.GetInt32()
            : (int?)null;
        var dayOfWeekStr = data.TryGetProperty("dayOfWeek", out var dowProp) && dowProp.ValueKind == JsonValueKind.String
            ? dowProp.GetString()
            : null;
        var dayOfWeek = !string.IsNullOrEmpty(dayOfWeekStr) && Enum.TryParse<DayOfWeek>(dayOfWeekStr, true, out var dow)
            ? dow
            : (DayOfWeek?)null;

        return frequencyStr switch
        {
            "daily" => RecurrencePatternValue.CreateDaily(interval),
            "weekly" when dayOfWeek.HasValue => RecurrencePatternValue.CreateWeekly(interval, dayOfWeek.Value),
            "weekly" => RecurrencePatternValue.CreateWeekly(interval, DayOfWeek.Monday),
            "biweekly" when dayOfWeek.HasValue => RecurrencePatternValue.CreateBiWeekly(dayOfWeek.Value),
            "biweekly" => RecurrencePatternValue.CreateBiWeekly(DayOfWeek.Friday),
            "monthly" when dayOfMonth.HasValue => RecurrencePatternValue.CreateMonthly(interval, dayOfMonth.Value),
            "monthly" => RecurrencePatternValue.CreateMonthly(interval, 1),
            "quarterly" when dayOfMonth.HasValue => RecurrencePatternValue.CreateQuarterly(dayOfMonth.Value),
            "quarterly" => RecurrencePatternValue.CreateQuarterly(1),
            "yearly" when dayOfMonth.HasValue => RecurrencePatternValue.CreateYearly(1, dayOfMonth.Value),
            "yearly" => RecurrencePatternValue.CreateYearly(1, 1),
            _ => RecurrencePatternValue.CreateMonthly(1, 1),
        };
    }

    internal static Guid? ParseGuid(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return Guid.TryParse(prop.GetString(), out var id) ? id : null;
        }

        return null;
    }

    internal static DateOnly? ParseDate(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            var dateStr = prop.GetString();
            if (DateOnly.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }
        }

        return null;
    }

    private static ClarificationOption ParseClarificationOption(JsonElement opt)
    {
        var label = opt.TryGetProperty("label", out var lProp) ? lProp.GetString() ?? string.Empty : string.Empty;
        var value = opt.TryGetProperty("value", out var vProp) ? vProp.GetString() ?? string.Empty : string.Empty;
        return new ClarificationOption { Label = label, Value = value, EntityId = ParseEntityId(opt) };
    }

    private static Guid? ParseEntityId(JsonElement opt)
    {
        if (!opt.TryGetProperty("entityId", out var eProp) || eProp.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return Guid.TryParse(eProp.GetString(), out var eid) ? eid : null;
    }
}
