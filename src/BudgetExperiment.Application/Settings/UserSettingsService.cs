// <copyright file="UserSettingsService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Settings;

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
        _repository = repository;
        _unitOfWork = unitOfWork;
        _userContext = userContext;
    }

    /// <inheritdoc />
    public UserProfileDto GetCurrentUserProfile()
    {
        return new UserProfileDto
        {
            UserId = _userContext.UserIdAsGuid ?? Guid.Empty,
            Username = _userContext.Username,
            Email = _userContext.Email,
            DisplayName = _userContext.DisplayName,
            AvatarUrl = _userContext.AvatarUrl,
        };
    }

    /// <inheritdoc />
    public async Task<UserSettingsDto> GetCurrentUserSettingsAsync(CancellationToken cancellationToken = default)
    {
        var userId = _userContext.UserIdAsGuid
            ?? throw new DomainException("User is not authenticated.");

        var settings = await _repository.GetByUserIdAsync(userId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return this.ToDto(settings);
    }

    /// <inheritdoc />
    public async Task<UserSettingsDto> UpdateCurrentUserSettingsAsync(
        UserSettingsUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = _userContext.UserIdAsGuid
            ?? throw new DomainException("User is not authenticated.");

        var settings = await _repository.GetByUserIdAsync(userId, cancellationToken);

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

        if (dto.FirstDayOfWeek.HasValue)
        {
            settings.UpdateFirstDayOfWeek(dto.FirstDayOfWeek.Value);
        }

        await _repository.SaveAsync(settings, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return this.ToDto(settings);
    }

    /// <inheritdoc />
    public async Task<UserSettingsDto> CompleteOnboardingAsync(CancellationToken cancellationToken = default)
    {
        var userId = _userContext.UserIdAsGuid
            ?? throw new DomainException("User is not authenticated.");

        var settings = await _repository.GetByUserIdAsync(userId, cancellationToken);
        settings.CompleteOnboarding();

        await _repository.SaveAsync(settings, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return this.ToDto(settings);
    }

    /// <inheritdoc />
    public ScopeDto GetCurrentScope()
    {
        return new ScopeDto
        {
            Scope = _userContext.CurrentScope?.ToString(),
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

        _userContext.SetScope(scope);
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
            FirstDayOfWeek = settings.FirstDayOfWeek,
            IsOnboarded = settings.IsOnboarded,
        };
    }
}
