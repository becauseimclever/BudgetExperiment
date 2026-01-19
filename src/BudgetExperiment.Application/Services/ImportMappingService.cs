// <copyright file="ImportMappingService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Service for managing import mappings.
/// </summary>
public sealed class ImportMappingService : IImportMappingService
{
    private readonly IImportMappingRepository _repository;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportMappingService"/> class.
    /// </summary>
    /// <param name="repository">The import mapping repository.</param>
    /// <param name="userContext">The user context.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public ImportMappingService(IImportMappingRepository repository, IUserContext userContext, IUnitOfWork unitOfWork)
    {
        this._repository = repository;
        this._userContext = userContext;
        this._unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ImportMappingDto>> GetUserMappingsAsync(CancellationToken cancellationToken = default)
    {
        var userId = this.GetRequiredUserId();
        var mappings = await this._repository.GetByUserAsync(userId, cancellationToken);
        return mappings.Select(ToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<ImportMappingDto?> GetMappingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var mapping = await this._repository.GetByIdAsync(id, cancellationToken);
        if (mapping is null)
        {
            return null;
        }

        // Verify user owns this mapping
        var userId = this.GetRequiredUserId();
        if (mapping.UserId != userId)
        {
            return null;
        }

        return ToDto(mapping);
    }

    /// <inheritdoc />
    public async Task<ImportMappingDto> CreateMappingAsync(CreateImportMappingRequest request, CancellationToken cancellationToken = default)
    {
        var userId = this.GetRequiredUserId();

        // Check for duplicate name
        var existing = await this._repository.GetByNameAsync(userId, request.Name, cancellationToken);
        if (existing is not null)
        {
            throw new DomainException($"A mapping with the name '{request.Name}' already exists.");
        }

        var columnMappings = request.ColumnMappings.Select(ToDomain).ToList();
        var duplicateSettings = request.DuplicateSettings is not null
            ? ToDomain(request.DuplicateSettings)
            : null;

        var mapping = ImportMapping.Create(userId, request.Name, columnMappings);

        if (!string.IsNullOrWhiteSpace(request.DateFormat))
        {
            mapping.SetDateFormat(request.DateFormat);
        }

        if (request.AmountMode.HasValue)
        {
            mapping.SetAmountMode(request.AmountMode.Value);
        }

        if (duplicateSettings is not null)
        {
            mapping.SetDuplicateSettings(duplicateSettings);
        }

        await this._repository.AddAsync(mapping, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(mapping);
    }

    /// <inheritdoc />
    public async Task<ImportMappingDto?> UpdateMappingAsync(Guid id, UpdateImportMappingRequest request, CancellationToken cancellationToken = default)
    {
        var userId = this.GetRequiredUserId();
        var mapping = await this._repository.GetByIdAsync(id, cancellationToken);

        if (mapping is null || mapping.UserId != userId)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != mapping.Name)
        {
            // Check for duplicate name
            var existing = await this._repository.GetByNameAsync(userId, request.Name, cancellationToken);
            if (existing is not null && existing.Id != id)
            {
                throw new DomainException($"A mapping with the name '{request.Name}' already exists.");
            }

            mapping.Rename(request.Name);
        }

        if (request.ColumnMappings is not null)
        {
            var columnMappings = request.ColumnMappings.Select(ToDomain).ToList();
            mapping.UpdateMappings(columnMappings);
        }

        if (request.DateFormat is not null)
        {
            mapping.SetDateFormat(request.DateFormat);
        }

        if (request.AmountMode.HasValue)
        {
            mapping.SetAmountMode(request.AmountMode.Value);
        }

        if (request.DuplicateSettings is not null)
        {
            mapping.SetDuplicateSettings(ToDomain(request.DuplicateSettings));
        }

        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(mapping);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteMappingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = this.GetRequiredUserId();
        var mapping = await this._repository.GetByIdAsync(id, cancellationToken);

        if (mapping is null || mapping.UserId != userId)
        {
            return false;
        }

        await this._repository.RemoveAsync(mapping, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<ImportMappingDto?> SuggestMappingAsync(IReadOnlyList<string> headers, CancellationToken cancellationToken = default)
    {
        if (headers.Count == 0)
        {
            return null;
        }

        var userId = this.GetRequiredUserId();
        var mappings = await this._repository.GetByUserAsync(userId, cancellationToken);

        // Find a mapping where the headers match (by checking if column headers match)
        foreach (var mapping in mappings)
        {
            if (HeadersMatch(mapping.ColumnMappings, headers))
            {
                return ToDto(mapping);
            }
        }

        return null;
    }

    private static bool HeadersMatch(IReadOnlyList<ColumnMapping> mappings, IReadOnlyList<string> headers)
    {
        // Check if all mapped column headers exist in the provided headers
        var headerSet = new HashSet<string>(headers, StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in mappings)
        {
            if (!string.IsNullOrWhiteSpace(mapping.ColumnHeader) &&
                !headerSet.Contains(mapping.ColumnHeader))
            {
                return false;
            }
        }

        // Require at least some overlap
        return mappings.Any(m => !string.IsNullOrWhiteSpace(m.ColumnHeader) && headerSet.Contains(m.ColumnHeader!));
    }

    private static ImportMappingDto ToDto(ImportMapping mapping)
    {
        return new ImportMappingDto
        {
            Id = mapping.Id,
            Name = mapping.Name,
            ColumnMappings = mapping.ColumnMappings.Select(ToDto).ToList(),
            DateFormat = mapping.DateFormat,
            AmountMode = mapping.AmountMode,
            DuplicateSettings = mapping.DuplicateSettings is not null
                ? ToDto(mapping.DuplicateSettings)
                : null,
            CreatedAtUtc = mapping.CreatedAtUtc,
            UpdatedAtUtc = mapping.UpdatedAtUtc,
        };
    }

    private static ColumnMappingDto ToDto(ColumnMapping mapping)
    {
        return new ColumnMappingDto
        {
            ColumnIndex = mapping.ColumnIndex,
            ColumnHeader = mapping.ColumnHeader,
            TargetField = mapping.TargetField,
            DateFormat = mapping.DateFormat,
        };
    }

    private static DuplicateDetectionSettingsDto ToDto(DuplicateDetectionSettings settings)
    {
        return new DuplicateDetectionSettingsDto
        {
            Enabled = settings.Enabled,
            LookbackDays = settings.LookbackDays,
            DescriptionMatch = settings.DescriptionMatch,
        };
    }

    private static ColumnMapping ToDomain(ColumnMappingDto dto)
    {
        return new ColumnMapping
        {
            ColumnIndex = dto.ColumnIndex,
            ColumnHeader = dto.ColumnHeader ?? string.Empty,
            TargetField = dto.TargetField,
            DateFormat = dto.DateFormat,
        };
    }

    private static DuplicateDetectionSettings ToDomain(DuplicateDetectionSettingsDto dto)
    {
        return new DuplicateDetectionSettings
        {
            Enabled = dto.Enabled,
            LookbackDays = dto.LookbackDays,
            DescriptionMatch = dto.DescriptionMatch,
        };
    }

    private Guid GetRequiredUserId()
    {
        return this._userContext.UserIdAsGuid
            ?? throw new DomainException("User ID is required for import mapping operations.");
    }
}
