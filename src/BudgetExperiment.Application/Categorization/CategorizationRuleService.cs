// <copyright file="CategorizationRuleService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Mapping;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Application service for categorization rule operations.
/// </summary>
public sealed class CategorizationRuleService : ICategorizationRuleService
{
    private readonly ICategorizationRuleRepository _repository;
    private readonly ICategorizationEngine _engine;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategorizationRuleService"/> class.
    /// </summary>
    /// <param name="repository">The categorization rule repository.</param>
    /// <param name="engine">The categorization engine.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public CategorizationRuleService(
        ICategorizationRuleRepository repository,
        ICategorizationEngine engine,
        IUnitOfWork unitOfWork)
    {
        this._repository = repository;
        this._engine = engine;
        this._unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CategorizationRuleDto>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CategorizationRule> rules;
        if (activeOnly)
        {
            rules = await this._repository.GetActiveByPriorityAsync(cancellationToken);
        }
        else
        {
            rules = await this._repository.ListAsync(0, int.MaxValue, cancellationToken);
        }

        return rules.Select(DomainToDtoMapper.ToDto).ToList();
    }

    /// <inheritdoc/>
    public async Task<CategorizationRuleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await this._repository.GetByIdAsync(id, cancellationToken);
        return rule is null ? null : DomainToDtoMapper.ToDto(rule);
    }

    /// <inheritdoc/>
    public async Task<CategorizationRuleDto> CreateAsync(CategorizationRuleCreateDto dto, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<RuleMatchType>(dto.MatchType, ignoreCase: true, out var matchType))
        {
            throw new DomainException($"Invalid match type: {dto.MatchType}");
        }

        var priority = dto.Priority ?? await this._repository.GetNextPriorityAsync(cancellationToken);

        var rule = CategorizationRule.Create(
            dto.Name,
            matchType,
            dto.Pattern,
            dto.CategoryId,
            priority,
            dto.CaseSensitive);

        await this._repository.AddAsync(rule, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        // Re-fetch to include category navigation property
        var created = await this._repository.GetByIdAsync(rule.Id, cancellationToken);
        return DomainToDtoMapper.ToDto(created!);
    }

    /// <inheritdoc/>
    public async Task<CategorizationRuleDto?> UpdateAsync(Guid id, CategorizationRuleUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var rule = await this._repository.GetByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return null;
        }

        if (!Enum.TryParse<RuleMatchType>(dto.MatchType, ignoreCase: true, out var matchType))
        {
            throw new DomainException($"Invalid match type: {dto.MatchType}");
        }

        rule.Update(dto.Name, matchType, dto.Pattern, dto.CategoryId, dto.CaseSensitive);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        // Re-fetch to include category navigation property
        var updated = await this._repository.GetByIdAsync(rule.Id, cancellationToken);
        return DomainToDtoMapper.ToDto(updated!);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await this._repository.GetByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return false;
        }

        await this._repository.RemoveAsync(rule, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await this._repository.GetByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return false;
        }

        rule.Activate();
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await this._repository.GetByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return false;
        }

        rule.Deactivate();
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task ReorderAsync(IReadOnlyList<Guid> ruleIds, CancellationToken cancellationToken = default)
    {
        var priorities = ruleIds.Select((id, index) => (RuleId: id, NewPriority: index + 1)).ToList();
        await this._repository.ReorderPrioritiesAsync(priorities, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<TestPatternResponse> TestPatternAsync(TestPatternRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<RuleMatchType>(request.MatchType, ignoreCase: true, out var matchType))
        {
            throw new DomainException($"Invalid match type: {request.MatchType}");
        }

        var matches = await this._engine.TestPatternAsync(
            matchType,
            request.Pattern,
            request.CaseSensitive,
            request.Limit,
            cancellationToken);

        return new TestPatternResponse
        {
            MatchingDescriptions = matches,
            MatchCount = matches.Count,
        };
    }

    /// <inheritdoc/>
    public async Task<ApplyRulesResponse> ApplyRulesAsync(ApplyRulesRequest request, CancellationToken cancellationToken = default)
    {
        var result = await this._engine.ApplyRulesAsync(
            request.TransactionIds,
            request.OverwriteExisting,
            cancellationToken);

        return new ApplyRulesResponse
        {
            TotalProcessed = result.TotalProcessed,
            Categorized = result.Categorized,
            Skipped = result.Skipped,
            Errors = result.Errors,
            ErrorMessages = result.ErrorMessages,
        };
    }
}
