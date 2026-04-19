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
        _repository = repository;
        _unitOfWork = unitOfWork;
        _userContext = userContext;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CustomReportLayoutDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var layouts = await _repository.GetAllAsync(cancellationToken);
        return layouts.Select(l => ToDto(l)).ToList();
    }

    /// <inheritdoc />
    public async Task<CustomReportLayoutDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var layout = await _repository.GetByIdAsync(id, cancellationToken);
        if (layout is null)
        {
            return null;
        }

        var version = _unitOfWork.GetConcurrencyToken(layout);
        return ToDto(layout, version);
    }

    /// <inheritdoc />
    public async Task<CustomReportLayoutDto> CreateAsync(CustomReportLayoutCreateDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _userContext.UserIdAsGuid
            ?? throw new DomainException("User is not authenticated.");

        var layout = CustomReportLayout.CreateShared(dto.Name, dto.LayoutJson, userId);

        await _repository.AddAsync(layout, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(layout);
    }

    /// <inheritdoc />
    public async Task<CustomReportLayoutDto?> UpdateAsync(Guid id, CustomReportLayoutUpdateDto dto, string? expectedVersion = null, CancellationToken cancellationToken = default)
    {
        var layout = await _repository.GetByIdAsync(id, cancellationToken);
        if (layout is null)
        {
            return null;
        }

        if (expectedVersion is not null)
        {
            _unitOfWork.SetExpectedConcurrencyToken(layout, expectedVersion);
        }

        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            layout.UpdateName(dto.Name);
        }

        if (!string.IsNullOrWhiteSpace(dto.LayoutJson))
        {
            layout.UpdateLayout(dto.LayoutJson);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var version = _unitOfWork.GetConcurrencyToken(layout);
        return ToDto(layout, version);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var layout = await _repository.GetByIdAsync(id, cancellationToken);
        if (layout is null)
        {
            return false;
        }

        await _repository.RemoveAsync(layout, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static CustomReportLayoutDto ToDto(CustomReportLayout layout, string? version = null)
    {
        return new CustomReportLayoutDto
        {
            Id = layout.Id,
            Name = layout.Name,
            LayoutJson = layout.LayoutJson,
            CreatedAtUtc = layout.CreatedAtUtc,
            UpdatedAtUtc = layout.UpdatedAtUtc,
            Version = version,
        };
    }
}
