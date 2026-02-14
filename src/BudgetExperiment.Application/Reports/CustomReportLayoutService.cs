// <copyright file="CustomReportLayoutService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Reports;

namespace BudgetExperiment.Application.Reports;

/// <summary>
/// Application service for custom report layout operations.
/// </summary>
public sealed class CustomReportLayoutService : ICustomReportLayoutService
{
    private readonly ICustomReportLayoutRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomReportLayoutService"/> class.
    /// </summary>
    /// <param name="repository">Layout repository.</param>
    /// <param name="unitOfWork">Unit of work.</param>
    /// <param name="userContext">User context.</param>
    public CustomReportLayoutService(
        ICustomReportLayoutRepository repository,
        IUnitOfWork unitOfWork,
        IUserContext userContext)
    {
        this._repository = repository;
        this._unitOfWork = unitOfWork;
        this._userContext = userContext;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CustomReportLayoutDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var layouts = await this._repository.GetAllAsync(cancellationToken);
        return layouts.Select(ToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<CustomReportLayoutDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var layout = await this._repository.GetByIdAsync(id, cancellationToken);
        return layout is null ? null : ToDto(layout);
    }

    /// <inheritdoc />
    public async Task<CustomReportLayoutDto> CreateAsync(CustomReportLayoutCreateDto dto, CancellationToken cancellationToken = default)
    {
        var userId = this._userContext.UserIdAsGuid
            ?? throw new DomainException("User is not authenticated.");

        var scope = ResolveScope(dto.Scope);

        CustomReportLayout layout = scope switch
        {
            BudgetScope.Personal => CustomReportLayout.CreatePersonal(dto.Name, dto.LayoutJson, userId),
            BudgetScope.Shared => CustomReportLayout.CreateShared(dto.Name, dto.LayoutJson, userId),
            _ => throw new DomainException($"Invalid scope: {scope}"),
        };

        await this._repository.AddAsync(layout, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(layout);
    }

    /// <inheritdoc />
    public async Task<CustomReportLayoutDto?> UpdateAsync(Guid id, CustomReportLayoutUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var layout = await this._repository.GetByIdAsync(id, cancellationToken);
        if (layout is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            layout.UpdateName(dto.Name);
        }

        if (!string.IsNullOrWhiteSpace(dto.LayoutJson))
        {
            layout.UpdateLayout(dto.LayoutJson);
        }

        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(layout);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var layout = await this._repository.GetByIdAsync(id, cancellationToken);
        if (layout is null)
        {
            return false;
        }

        await this._repository.RemoveAsync(layout, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private BudgetScope ResolveScope(string? scope)
    {
        if (!string.IsNullOrWhiteSpace(scope))
        {
            if (!Enum.TryParse<BudgetScope>(scope, ignoreCase: true, out var parsedScope))
            {
                throw new DomainException($"Invalid scope: {scope}. Valid values are 'Shared' or 'Personal'.");
            }

            return parsedScope;
        }

        return this._userContext.CurrentScope ?? BudgetScope.Shared;
    }

    private static CustomReportLayoutDto ToDto(CustomReportLayout layout)
    {
        return new CustomReportLayoutDto
        {
            Id = layout.Id,
            Name = layout.Name,
            LayoutJson = layout.LayoutJson,
            Scope = layout.Scope.ToString(),
            CreatedAtUtc = layout.CreatedAtUtc,
            UpdatedAtUtc = layout.UpdatedAtUtc,
        };
    }
}
