// <copyright file="AccountsViewModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Components;

namespace BudgetExperiment.Client.ViewModels;

/// <summary>
/// ViewModel for the Accounts page. Contains all handler logic, state fields,
/// and methods extracted from the Accounts.razor @code block.
/// </summary>
public sealed class AccountsViewModel
{
    private readonly IBudgetApiService _apiService;
    private readonly IToastService _toastService;
    private readonly NavigationManager _navigation;
    private readonly IApiErrorContext _apiErrorContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountsViewModel"/> class.
    /// </summary>
    /// <param name="apiService">The budget API service.</param>
    /// <param name="toastService">The toast notification service.</param>
    /// <param name="navigation">The navigation manager.</param>
    /// <param name="apiErrorContext">The API error context for traceId capture.</param>
    public AccountsViewModel(
        IBudgetApiService apiService,
        IToastService toastService,
        NavigationManager navigation,
        IApiErrorContext apiErrorContext)
    {
        _apiService = apiService;
        _toastService = toastService;
        _navigation = navigation;
        _apiErrorContext = apiErrorContext;
    }

    /// <summary>
    /// Gets or sets the callback to notify the Razor page that state has changed and it should re-render.
    /// </summary>
    public Action? OnStateChanged
    {
        get; set;
    }

    /// <summary>
    /// Gets a value indicating whether accounts are loading.
    /// </summary>
    public bool IsLoading { get; private set; } = true;

    /// <summary>
    /// Gets a value indicating whether a retry load is in progress.
    /// </summary>
    public bool IsRetrying
    {
        get; private set;
    }

    /// <summary>
    /// Gets a value indicating whether a form submission is in progress.
    /// </summary>
    public bool IsSubmitting
    {
        get; private set;
    }

    /// <summary>
    /// Gets a value indicating whether a delete operation is in progress.
    /// </summary>
    public bool IsDeleting
    {
        get; private set;
    }

    /// <summary>
    /// Gets the current error message, if any.
    /// </summary>
    public string? ErrorMessage
    {
        get; private set;
    }

    /// <summary>
    /// Gets the traceId from the API error response that caused the current error, if any.
    /// </summary>
    public string? ErrorTraceId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the list of all accounts.
    /// </summary>
    public List<AccountDto> Accounts { get; private set; } = new();

    /// <summary>
    /// Gets a value indicating whether the add account form is visible.
    /// </summary>
    public bool ShowAddForm
    {
        get; private set;
    }

    /// <summary>
    /// Gets or sets the new account form model.
    /// </summary>
    public AccountCreateDto NewAccount { get; set; } = new();

    /// <summary>
    /// Gets a value indicating whether the edit account form is visible.
    /// </summary>
    public bool ShowEditForm
    {
        get; private set;
    }

    /// <summary>
    /// Gets or sets the edit account form model.
    /// </summary>
    public AccountCreateDto EditAccount { get; set; } = new();

    /// <summary>
    /// Gets the ID of the account currently being edited.
    /// </summary>
    public Guid? EditingAccountId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the concurrency version of the account being edited.
    /// </summary>
    public string? EditingVersion
    {
        get; private set;
    }

    /// <summary>
    /// Gets a value indicating whether the transfer dialog is visible.
    /// </summary>
    public bool ShowTransferDialog
    {
        get; private set;
    }

    /// <summary>
    /// Gets or sets the new transfer form model.
    /// </summary>
    public CreateTransferRequest NewTransfer { get; set; } = new();

    /// <summary>
    /// Gets the pre-selected source account ID for transfers, if any.
    /// </summary>
    public Guid? PreSelectedSourceAccountId
    {
        get; private set;
    }

    /// <summary>
    /// Gets a value indicating whether the delete confirmation dialog is visible.
    /// </summary>
    public bool ShowDeleteConfirm
    {
        get; private set;
    }

    /// <summary>
    /// Gets the account pending deletion.
    /// </summary>
    public AccountDto? DeletingAccount
    {
        get; private set;
    }

    /// <summary>
    /// Initializes the ViewModel and loads accounts.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task InitializeAsync()
    {
        await this.LoadAccountsAsync();
    }

    /// <summary>
    /// Loads accounts from the API.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task LoadAccountsAsync()
    {
        this.IsLoading = true;
        this.ErrorMessage = null;
        this.ErrorTraceId = null;

        try
        {
            this.Accounts = (await _apiService.GetAccountsAsync()).ToList();
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to load accounts: {ex.Message}";
            this.ErrorTraceId = _apiErrorContext.LastTraceId;
        }
        finally
        {
            this.IsLoading = false;
        }
    }

    /// <summary>
    /// Retries loading accounts after a failure.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task RetryLoadAsync()
    {
        this.IsRetrying = true;
        this.NotifyStateChanged();

        try
        {
            await this.LoadAccountsAsync();
        }
        finally
        {
            this.IsRetrying = false;
        }
    }

    /// <summary>
    /// Dismisses the current error message.
    /// </summary>
    public void DismissError()
    {
        this.ErrorMessage = null;
        this.ErrorTraceId = null;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Opens the add account form.
    /// </summary>
    public void ShowAddAccount()
    {
        this.NewAccount = new AccountCreateDto { Type = "Checking" };
        this.ShowAddForm = true;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Closes the add account form.
    /// </summary>
    public void HideAddAccount()
    {
        this.ShowAddForm = false;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Opens the edit account form for the specified account.
    /// </summary>
    /// <param name="account">The account to edit.</param>
    public void ShowEditAccount(AccountDto account)
    {
        this.EditingAccountId = account.Id;
        this.EditingVersion = account.Version;
        this.EditAccount = new AccountCreateDto
        {
            Name = account.Name,
            Type = account.Type,
            InitialBalance = account.InitialBalance,
            InitialBalanceCurrency = account.InitialBalanceCurrency,
            InitialBalanceDate = account.InitialBalanceDate,
        };
        this.ShowEditForm = true;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Closes the edit account form.
    /// </summary>
    public void HideEditAccount()
    {
        this.ShowEditForm = false;
        this.EditingAccountId = null;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Updates an account via the API.
    /// </summary>
    /// <param name="model">The account update data.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task UpdateAccountAsync(AccountCreateDto model)
    {
        if (this.EditingAccountId == null)
        {
            return;
        }

        this.IsSubmitting = true;
        try
        {
            var updateDto = new AccountUpdateDto
            {
                Name = model.Name,
                Type = model.Type,
                InitialBalance = model.InitialBalance,
                InitialBalanceCurrency = model.InitialBalanceCurrency,
                InitialBalanceDate = model.InitialBalanceDate,
            };

            var result = await _apiService.UpdateAccountAsync(this.EditingAccountId.Value, updateDto, this.EditingVersion);
            if (result.IsConflict)
            {
                _toastService.ShowWarning("This account was modified by another user. Data has been refreshed.", "Conflict");
                this.HideEditAccount();
                await this.LoadAccountsAsync();
                return;
            }

            if (result.IsSuccess)
            {
                this.ShowEditForm = false;
                this.EditingAccountId = null;
                await this.LoadAccountsAsync();
            }
        }
        finally
        {
            this.IsSubmitting = false;
        }
    }

    /// <summary>
    /// Shows the transfer dialog without a pre-selected source account.
    /// </summary>
    public void ShowTransfer()
    {
        this.PreSelectedSourceAccountId = null;
        this.NewTransfer = new CreateTransferRequest();
        this.ShowTransferDialog = true;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Shows the transfer dialog with a pre-selected source account.
    /// </summary>
    /// <param name="accountId">The source account ID.</param>
    public void ShowTransferFrom(Guid accountId)
    {
        this.PreSelectedSourceAccountId = accountId;
        this.NewTransfer = new CreateTransferRequest { SourceAccountId = accountId };
        this.ShowTransferDialog = true;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Closes the transfer dialog.
    /// </summary>
    public void HideTransfer()
    {
        this.ShowTransferDialog = false;
        this.PreSelectedSourceAccountId = null;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Creates a transfer via the API.
    /// </summary>
    /// <param name="model">The transfer creation data.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task CreateTransferAsync(CreateTransferRequest model)
    {
        var result = await _apiService.CreateTransferAsync(model);
        if (result != null)
        {
            this.ShowTransferDialog = false;
        }
    }

    /// <summary>
    /// Creates a new account via the API.
    /// </summary>
    /// <param name="model">The account creation data.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task CreateAccountAsync(AccountCreateDto model)
    {
        this.IsSubmitting = true;
        try
        {
            var result = await _apiService.CreateAccountAsync(model);
            if (result != null)
            {
                this.ShowAddForm = false;
                await this.LoadAccountsAsync();
            }
        }
        finally
        {
            this.IsSubmitting = false;
        }
    }

    /// <summary>
    /// Navigates to the account transactions page.
    /// </summary>
    /// <param name="id">The account ID.</param>
    public void ViewAccount(Guid id)
    {
        _navigation.NavigateTo($"/transactions?account={id}");
    }

    /// <summary>
    /// Shows the delete confirmation dialog for the specified account.
    /// </summary>
    /// <param name="id">The account ID to delete.</param>
    public void DeleteAccount(Guid id)
    {
        this.DeletingAccount = this.Accounts.FirstOrDefault(a => a.Id == id);
        if (this.DeletingAccount != null)
        {
            this.ShowDeleteConfirm = true;
            this.NotifyStateChanged();
        }
    }

    /// <summary>
    /// Confirms and executes the delete operation.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task ConfirmDeleteAsync()
    {
        if (this.DeletingAccount == null)
        {
            return;
        }

        this.IsDeleting = true;
        this.NotifyStateChanged();

        try
        {
            var deleted = await _apiService.DeleteAccountAsync(this.DeletingAccount.Id);
            if (deleted)
            {
                this.ShowDeleteConfirm = false;
                this.DeletingAccount = null;
                await this.LoadAccountsAsync();
            }
        }
        finally
        {
            this.IsDeleting = false;
        }
    }

    /// <summary>
    /// Cancels the delete confirmation.
    /// </summary>
    public void CancelDelete()
    {
        this.ShowDeleteConfirm = false;
        this.DeletingAccount = null;
        this.NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        this.OnStateChanged?.Invoke();
    }
}
