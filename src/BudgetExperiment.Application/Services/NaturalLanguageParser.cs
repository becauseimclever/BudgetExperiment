// <copyright file="NaturalLanguageParser.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;
using System.Text.Json;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// AI-powered implementation of <see cref="INaturalLanguageParser"/>.
/// </summary>
public sealed class NaturalLanguageParser : INaturalLanguageParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly IAiService _aiService;

    /// <summary>
    /// Initializes a new instance of the <see cref="NaturalLanguageParser"/> class.
    /// </summary>
    /// <param name="aiService">The AI service for processing commands.</param>
    public NaturalLanguageParser(IAiService aiService)
    {
        this._aiService = aiService;
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

        // Build the system prompt with context
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

        var response = await this._aiService.CompleteAsync(prompt, cancellationToken);

        if (!response.Success)
        {
            return new ParseResult(
                Success: false,
                Action: null,
                ResponseText: "Sorry, I couldn't process that. Please try again.",
                ErrorMessage: response.ErrorMessage);
        }

        return ParseAiResponse(response.Content, accounts, categories);
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
        if (!string.IsNullOrEmpty(context.CurrentAccountName))
        {
            parts.Add($"Current account: {context.CurrentAccountName}");
        }

        if (!string.IsNullOrEmpty(context.CurrentCategoryName))
        {
            parts.Add($"Current category: {context.CurrentCategoryName}");
        }

        if (context.CurrentDate.HasValue)
        {
            parts.Add($"Viewing date: {context.CurrentDate.Value:yyyy-MM-dd}");
        }

        if (!string.IsNullOrEmpty(context.CurrentPage))
        {
            parts.Add($"Current page: {context.CurrentPage}");
        }

        return parts.Count > 0 ? string.Join(", ", parts) : "No specific context.";
    }

    private static ParseResult ParseAiResponse(
        string content,
        IReadOnlyList<AccountInfo> accounts,
        IReadOnlyList<CategoryInfo> categories)
    {
        try
        {
            // Try to extract JSON from the response (in case AI included extra text)
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

            // Check if clarification is needed
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

            // Parse the action based on intent
            var data = root.TryGetProperty("data", out var dataProp) ? dataProp : default;
            ChatAction? action = intent switch
            {
                "transaction" => ParseTransactionAction(data, accounts, categories),
                "transfer" => ParseTransferAction(data, accounts),
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

    private static ClarificationNeededAction ParseClarification(JsonElement clarProp)
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
                var label = opt.TryGetProperty("label", out var lProp) ? lProp.GetString() ?? string.Empty : string.Empty;
                var value = opt.TryGetProperty("value", out var vProp) ? vProp.GetString() ?? string.Empty : string.Empty;
                Guid? entityId = opt.TryGetProperty("entityId", out var eProp) && eProp.ValueKind == JsonValueKind.String
                    ? Guid.TryParse(eProp.GetString(), out var eid) ? eid : null
                    : null;

                options.Add(new ClarificationOption { Label = label, Value = value, EntityId = entityId });
            }
        }

        return new ClarificationNeededAction
        {
            Question = question,
            FieldName = fieldName,
            Options = options,
        };
    }

    private static CreateTransactionAction? ParseTransactionAction(
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
        var date = ParseDate(data, "date") ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var description = data.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? string.Empty : string.Empty;
        var category = data.TryGetProperty("category", out var catProp) && catProp.ValueKind == JsonValueKind.String
            ? catProp.GetString()
            : null;
        var categoryId = ParseGuid(data, "categoryId");

        // Validate account exists
        if (!accountId.HasValue)
        {
            // Try to find account by name
            var matchedAccount = accounts.FirstOrDefault(a =>
                a.Name.Equals(accountName, StringComparison.OrdinalIgnoreCase));
            if (matchedAccount != null)
            {
                accountId = matchedAccount.Id;
                accountName = matchedAccount.Name;
            }
            else if (accounts.Count == 1)
            {
                // Default to only account
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

    private static CreateTransferAction? ParseTransferAction(
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
        var date = ParseDate(data, "date") ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var description = data.TryGetProperty("description", out var descProp) && descProp.ValueKind == JsonValueKind.String
            ? descProp.GetString()
            : null;

        // Resolve accounts by name if IDs not provided
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
            Amount = Math.Abs(amount), // Transfers are always positive
            Date = date,
            Description = description,
        };
    }

    private static CreateRecurringTransactionAction? ParseRecurringTransactionAction(
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

        // Resolve account by name if ID not provided
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

    private static CreateRecurringTransferAction? ParseRecurringTransferAction(
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

        // Resolve accounts by name
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

    private static RecurrencePattern? ParseRecurrencePattern(JsonElement data)
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
            "daily" => RecurrencePattern.CreateDaily(interval),
            "weekly" when dayOfWeek.HasValue => RecurrencePattern.CreateWeekly(interval, dayOfWeek.Value),
            "weekly" => RecurrencePattern.CreateWeekly(interval, DayOfWeek.Monday),
            "biweekly" when dayOfWeek.HasValue => RecurrencePattern.CreateBiWeekly(dayOfWeek.Value),
            "biweekly" => RecurrencePattern.CreateBiWeekly(DayOfWeek.Friday),
            "monthly" when dayOfMonth.HasValue => RecurrencePattern.CreateMonthly(interval, dayOfMonth.Value),
            "monthly" => RecurrencePattern.CreateMonthly(interval, 1),
            "quarterly" when dayOfMonth.HasValue => RecurrencePattern.CreateQuarterly(dayOfMonth.Value),
            "quarterly" => RecurrencePattern.CreateQuarterly(1),
            "yearly" when dayOfMonth.HasValue => RecurrencePattern.CreateYearly(1, dayOfMonth.Value),
            "yearly" => RecurrencePattern.CreateYearly(1, 1),
            _ => RecurrencePattern.CreateMonthly(1, 1),
        };
    }

    private static Guid? ParseGuid(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return Guid.TryParse(prop.GetString(), out var id) ? id : null;
        }

        return null;
    }

    private static DateOnly? ParseDate(JsonElement element, string propertyName)
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
}
