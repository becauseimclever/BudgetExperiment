// <copyright file="CategorizationEngine.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Implementation of <see cref="ICategorizationEngine"/> for applying auto-categorization rules to transactions.
/// </summary>
public class CategorizationEngine : ICategorizationEngine
{
    private readonly ICategorizationRuleRepository _ruleRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategorizationEngine"/> class.
    /// </summary>
    /// <param name="ruleRepository">The categorization rule repository.</param>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="unitOfWork">The unit of work for persisting changes.</param>
    public CategorizationEngine(
        ICategorizationRuleRepository ruleRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork)
    {
        this._ruleRepository = ruleRepository;
        this._transactionRepository = transactionRepository;
        this._unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Guid?> FindMatchingCategoryAsync(
        string description,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var rules = await this._ruleRepository.GetActiveByPriorityAsync(cancellationToken);

        foreach (var rule in rules)
        {
            if (rule.Matches(description))
            {
                return rule.CategoryId;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<CategorizationResult> ApplyRulesAsync(
        IEnumerable<Guid>? transactionIds,
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default)
    {
        var rules = await this._ruleRepository.GetActiveByPriorityAsync(cancellationToken);

        var totalProcessed = 0;
        var categorized = 0;
        var skipped = 0;
        var errors = 0;
        var errorMessages = new List<string>();

        IReadOnlyList<Transaction> transactions;
        var skippedDueToExisting = 0;

        if (transactionIds == null)
        {
            // Process all uncategorized transactions
            transactions = await this._transactionRepository.GetUncategorizedAsync(cancellationToken);
        }
        else
        {
            // Process specific transactions
            var transactionList = new List<Transaction>();
            foreach (var id in transactionIds)
            {
                var transaction = await this._transactionRepository.GetByIdAsync(id, cancellationToken);
                if (transaction != null)
                {
                    // Skip already-categorized transactions unless overwriteExisting is true
                    if (overwriteExisting || transaction.CategoryId == null)
                    {
                        transactionList.Add(transaction);
                    }
                    else
                    {
                        // Track as skipped due to existing category
                        skippedDueToExisting++;
                    }
                }
            }

            transactions = transactionList;
        }

        foreach (var transaction in transactions)
        {
            totalProcessed++;

            try
            {
                Guid? matchedCategoryId = null;

                foreach (var rule in rules)
                {
                    if (rule.Matches(transaction.Description))
                    {
                        matchedCategoryId = rule.CategoryId;
                        break;
                    }
                }

                if (matchedCategoryId.HasValue)
                {
                    transaction.UpdateCategory(matchedCategoryId.Value);
                    categorized++;
                }
                else
                {
                    skipped++;
                }
            }
            catch (Exception ex)
            {
                errors++;
                errorMessages.Add($"Error processing transaction {transaction.Id}: {ex.Message}");
            }
        }

        if (categorized > 0)
        {
            await this._unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new CategorizationResult
        {
            TotalProcessed = totalProcessed + skippedDueToExisting,
            Categorized = categorized,
            Skipped = skipped + skippedDueToExisting,
            Errors = errors,
            ErrorMessages = errorMessages,
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> TestPatternAsync(
        RuleMatchType matchType,
        string pattern,
        bool caseSensitive,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        // Create a temporary rule to leverage the Matches logic
        var testRule = CategorizationRule.Create(
            name: "Test Rule",
            pattern: pattern,
            matchType: matchType,
            categoryId: Guid.NewGuid(), // Dummy category, we just need the Matches method
            caseSensitive: caseSensitive);

        var allDescriptions = await this._transactionRepository.GetAllDescriptionsAsync(cancellationToken);

        var matchingDescriptions = new List<string>();

        foreach (var description in allDescriptions)
        {
            if (matchingDescriptions.Count >= limit)
            {
                break;
            }

            try
            {
                if (testRule.Matches(description))
                {
                    matchingDescriptions.Add(description);
                }
            }
            catch
            {
                // Skip descriptions that cause regex errors
            }
        }

        return matchingDescriptions;
    }
}
