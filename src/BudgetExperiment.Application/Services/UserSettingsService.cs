// <copyright file="UserSettingsService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Service for user settings operations.
/// </summary>
public sealed class UserSettingsService : IUserSettingsService
{
    private readonly IUserSettingsRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserSettingsService"/> class.
    /// </summary>
    /// <param name="repository">The user settings repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="userContext">The user context.</param>
    public UserSettingsService(
        IUserSettingsRepository repository,
        IUnitOfWork unitOfWork,
        IUserContext userContext)
    {
        this._repository = repository;
        this._unitOfWork = unitOfWork;
        this._userContext = userContext;
    }

    /// <inheritdoc />
    public UserProfileDto GetCurrentUserProfile()
    {
        return new UserProfileDto
        {
            UserId = this._userContext.UserIdAsGuid ?? Guid.Empty,
            Username = this._userContext.Username,
            Email = this._userContext.Email,
            DisplayName = this._userContext.DisplayName,
            AvatarUrl = this._userContext.AvatarUrl,
        };
    }

    /// <inheritdoc />
    public async Task<UserSettingsDto> GetCurrentUserSettingsAsync(CancellationToken cancellationToken = default)
    {
        var userId = this._userContext.UserIdAsGuid
            ?? throw new DomainException("User is not authenticated.");

        var settings = await this._repository.GetByUserIdAsync(userId, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        return this.ToDto(settings);
    }

    /// <inheritdoc />
    public async Task<UserSettingsDto> UpdateCurrentUserSettingsAsync(
        UserSettingsUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = this._userContext.UserIdAsGuid
            ?? throw new DomainException("User is not authenticated.");

        var settings = await this._repository.GetByUserIdAsync(userId, cancellationToken);

        if (dto.DefaultScope != null)
        {
            if (!Enum.TryParse<BudgetScope>(dto.DefaultScope, ignoreCase: true, out var scope))
            {
                throw new DomainException($"Invalid scope: {dto.DefaultScope}. Valid values are 'Shared' or 'Personal'.");
            }

            settings.UpdateDefaultScope(scope);
        }

        if (dto.AutoRealizePastDueItems.HasValue)
        {
            settings.UpdateAutoRealize(dto.AutoRealizePastDueItems.Value);
        }

        if (dto.PastDueLookbackDays.HasValue)
        {
            settings.UpdatePastDueLookbackDays(dto.PastDueLookbackDays.Value);
        }

        if (dto.PreferredCurrency != null)
        {
            settings.UpdatePreferredCurrency(dto.PreferredCurrency);
        }

        if (dto.TimeZoneId != null)
        {
            settings.UpdateTimeZoneId(dto.TimeZoneId);
        }

        await this._repository.SaveAsync(settings, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        return this.ToDto(settings);
    }

    /// <inheritdoc />
    public ScopeDto GetCurrentScope()
    {
        return new ScopeDto
        {
            Scope = this._userContext.CurrentScope?.ToString(),
        };
    }

    /// <inheritdoc />
    public void SetCurrentScope(ScopeDto dto)
    {
        BudgetScope? scope = null;

        if (!string.IsNullOrWhiteSpace(dto.Scope))
        {
            if (!Enum.TryParse<BudgetScope>(dto.Scope, ignoreCase: true, out var parsedScope))
            {
                throw new DomainException($"Invalid scope: {dto.Scope}. Valid values are 'Shared', 'Personal', or null for All.");
            }

            scope = parsedScope;
        }

        this._userContext.SetScope(scope);
    }

    private UserSettingsDto ToDto(UserSettings settings)
    {
        return new UserSettingsDto
        {
            UserId = settings.UserId,
            DefaultScope = settings.DefaultScope.ToString(),
            AutoRealizePastDueItems = settings.AutoRealizePastDueItems,
            PastDueLookbackDays = settings.PastDueLookbackDays,
            PreferredCurrency = settings.PreferredCurrency,
            TimeZoneId = settings.TimeZoneId,
        };
    }
}
