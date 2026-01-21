// <copyright file="BudgetCategoryService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Budgeting;

/// <summary>
/// Application service for budget category operations.
/// </summary>
public sealed class BudgetCategoryService : IBudgetCategoryService
{
    private readonly IBudgetCategoryRepository _repository;
    private readonly IBudgetGoalRepository _goalRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetCategoryService"/> class.
    /// </summary>
    /// <param name="repository">The budget category repository.</param>
    /// <param name="goalRepository">The budget goal repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public BudgetCategoryService(
        IBudgetCategoryRepository repository,
        IBudgetGoalRepository goalRepository,
        IUnitOfWork unitOfWork)
    {
        this._repository = repository;
        this._goalRepository = goalRepository;
        this._unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<BudgetCategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await this._repository.GetByIdAsync(id, cancellationToken);
        return category is null ? null : BudgetMapper.ToDto(category);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BudgetCategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await this._repository.GetAllAsync(cancellationToken);
        return categories.Select(BudgetMapper.ToDto).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BudgetCategoryDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var categories = await this._repository.GetActiveAsync(cancellationToken);
        return categories.Select(BudgetMapper.ToDto).ToList();
    }

    /// <inheritdoc/>
    public async Task<BudgetCategoryDto> CreateAsync(BudgetCategoryCreateDto dto, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<CategoryType>(dto.Type, ignoreCase: true, out var categoryType))
        {
            throw new DomainException($"Invalid category type: {dto.Type}");
        }

        var category = BudgetCategory.Create(dto.Name, categoryType, dto.Icon, dto.Color);
        await this._repository.AddAsync(category, cancellationToken);

        // If initial budget is provided and category is Expense type, create a budget goal for the current month
        if (dto.InitialBudget != null && dto.InitialBudget.Amount > 0 && categoryType == CategoryType.Expense)
        {
            var now = DateTime.UtcNow;
            var targetAmount = MoneyValue.Create(dto.InitialBudget.Currency ?? "USD", dto.InitialBudget.Amount);
            var goal = BudgetGoal.Create(category.Id, now.Year, now.Month, targetAmount);
            await this._goalRepository.AddAsync(goal, cancellationToken);
        }

        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return BudgetMapper.ToDto(category);
    }

    /// <inheritdoc/>
    public async Task<BudgetCategoryDto?> UpdateAsync(Guid id, BudgetCategoryUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var category = await this._repository.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return null;
        }

        category.Update(
            dto.Name ?? category.Name,
            dto.Icon ?? category.Icon,
            dto.Color ?? category.Color,
            dto.SortOrder ?? category.SortOrder);

        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return BudgetMapper.ToDto(category);
    }

    /// <inheritdoc/>
    public async Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await this._repository.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return false;
        }

        category.Deactivate();
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await this._repository.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return false;
        }

        category.Activate();
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await this._repository.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return false;
        }

        await this._repository.RemoveAsync(category, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
