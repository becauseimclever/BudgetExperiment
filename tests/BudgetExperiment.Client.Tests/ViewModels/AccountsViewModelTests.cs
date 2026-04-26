// <copyright file="AccountsViewModelTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Client.ViewModels;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Components;

using Shouldly;

namespace BudgetExperiment.Client.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="AccountsViewModel"/>.
/// </summary>
public sealed class AccountsViewModelTests
{
    private readonly StubBudgetApiService _apiService = new();
    private readonly StubToastService _toastService = new();
    private readonly StubNavigationManager _navigationManager = new();
    private readonly StubApiErrorContext _apiErrorContext = new();
    private readonly AccountsViewModel _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountsViewModelTests"/> class.
    /// </summary>
    public AccountsViewModelTests()
    {
        _sut = new AccountsViewModel(
            _apiService,
            _toastService,
            _navigationManager,
            _apiErrorContext);
    }

    // --- Initialization ---

    /// <summary>
    /// Verifies that InitializeAsync loads accounts from the API.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsAccounts()
    {
        _apiService.Accounts.Add(CreateAccount("Checking Account"));

        await _sut.InitializeAsync();

        _sut.Accounts.Count.ShouldBe(1);
        _sut.Accounts[0].Name.ShouldBe("Checking Account");
    }

    /// <summary>
    /// Verifies that InitializeAsync sets IsLoading to false after loading.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsIsLoadingToFalse()
    {
        await _sut.InitializeAsync();

        _sut.IsLoading.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that InitializeAsync handles API failure gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsErrorMessage_WhenApiFails()
    {
        _apiService.GetAccountsException = new HttpRequestException("Server error");

        await _sut.InitializeAsync();

        _sut.ErrorMessage.ShouldNotBeNull();
        _sut.ErrorMessage!.ShouldContain("Failed to load accounts");
        _sut.IsLoading.ShouldBeFalse();
    }

    // --- LoadAccountsAsync ---

    /// <summary>
    /// Verifies that LoadAccountsAsync clears the error message before loading.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadAccountsAsync_ClearsErrorMessage()
    {
        _apiService.GetAccountsException = new HttpRequestException("fail");
        await _sut.InitializeAsync();
        _sut.ErrorMessage.ShouldNotBeNull();

        _apiService.GetAccountsException = null;
        await _sut.LoadAccountsAsync();

        _sut.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that LoadAccountsAsync refreshes the accounts list.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadAccountsAsync_RefreshesAccountsList()
    {
        await _sut.InitializeAsync();
        _sut.Accounts.Count.ShouldBe(0);

        _apiService.Accounts.Add(CreateAccount("New Account"));
        await _sut.LoadAccountsAsync();

        _sut.Accounts.Count.ShouldBe(1);
    }

    // --- RetryLoadAsync ---

    /// <summary>
    /// Verifies that RetryLoadAsync reloads accounts.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RetryLoadAsync_ReloadsAccounts()
    {
        await _sut.InitializeAsync();
        _apiService.Accounts.Add(CreateAccount("Retried"));

        await _sut.RetryLoadAsync();

        _sut.Accounts.Count.ShouldBe(1);
        _sut.IsRetrying.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that RetryLoadAsync notifies state changed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RetryLoadAsync_NotifiesStateChanged()
    {
        int callCount = 0;
        _sut.OnStateChanged = () => callCount++;

        await _sut.RetryLoadAsync();

        callCount.ShouldBeGreaterThan(0);
    }

    // --- DismissError ---

    /// <summary>
    /// Verifies that DismissError clears the error message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DismissError_ClearsErrorMessage()
    {
        _apiService.GetAccountsException = new HttpRequestException("fail");
        await _sut.InitializeAsync();

        _sut.DismissError();

        _sut.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that DismissError notifies state changed.
    /// </summary>
    [Fact]
    public void DismissError_NotifiesStateChanged()
    {
        bool notified = false;
        _sut.OnStateChanged = () => notified = true;

        _sut.DismissError();

        notified.ShouldBeTrue();
    }

    // --- ShowAddAccount ---

    /// <summary>
    /// Verifies that ShowAddAccount shows the add form and resets the model.
    /// </summary>
    [Fact]
    public void ShowAddAccount_ShowsFormAndResetsModel()
    {
        _sut.ShowAddAccount();

        _sut.ShowAddForm.ShouldBeTrue();
        _sut.NewAccount.Type.ShouldBe("Checking");
    }

    /// <summary>
    /// Verifies state change notification on ShowAddAccount.
    /// </summary>
    [Fact]
    public void ShowAddAccount_NotifiesStateChanged()
    {
        bool notified = false;
        _sut.OnStateChanged = () => notified = true;

        _sut.ShowAddAccount();

        notified.ShouldBeTrue();
    }

    // --- HideAddAccount ---

    /// <summary>
    /// Verifies that HideAddAccount hides the add form.
    /// </summary>
    [Fact]
    public void HideAddAccount_HidesForm()
    {
        _sut.ShowAddAccount();

        _sut.HideAddAccount();

        _sut.ShowAddForm.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies state change notification on HideAddAccount.
    /// </summary>
    [Fact]
    public void HideAddAccount_NotifiesStateChanged()
    {
        bool notified = false;
        _sut.OnStateChanged = () => notified = true;

        _sut.HideAddAccount();

        notified.ShouldBeTrue();
    }

    // --- ShowEditAccount ---

    /// <summary>
    /// Verifies that ShowEditAccount populates the edit form with account data.
    /// </summary>
    [Fact]
    public void ShowEditAccount_PopulatesEditForm()
    {
        var account = CreateAccount("Test Account", type: "Savings", balance: 2000m);

        _sut.ShowEditAccount(account);

        _sut.ShowEditForm.ShouldBeTrue();
        _sut.EditingAccountId.ShouldBe(account.Id);
        _sut.EditingVersion.ShouldBe(account.Version);
        _sut.EditAccount.Name.ShouldBe("Test Account");
        _sut.EditAccount.Type.ShouldBe("Savings");
        _sut.EditAccount.InitialBalance.ShouldBe(2000m);
    }

    /// <summary>
    /// Verifies state change notification on ShowEditAccount.
    /// </summary>
    [Fact]
    public void ShowEditAccount_NotifiesStateChanged()
    {
        bool notified = false;
        _sut.OnStateChanged = () => notified = true;

        _sut.ShowEditAccount(CreateAccount("Test"));

        notified.ShouldBeTrue();
    }

    // --- HideEditAccount ---

    /// <summary>
    /// Verifies that HideEditAccount hides the form and clears editing state.
    /// </summary>
    [Fact]
    public void HideEditAccount_HidesFormAndClearsState()
    {
        _sut.ShowEditAccount(CreateAccount("Test"));

        _sut.HideEditAccount();

        _sut.ShowEditForm.ShouldBeFalse();
        _sut.EditingAccountId.ShouldBeNull();
    }

    /// <summary>
    /// Verifies state change notification on HideEditAccount.
    /// </summary>
    [Fact]
    public void HideEditAccount_NotifiesStateChanged()
    {
        bool notified = false;
        _sut.OnStateChanged = () => notified = true;

        _sut.HideEditAccount();

        notified.ShouldBeTrue();
    }

    // --- UpdateAccountAsync ---

    /// <summary>
    /// Verifies that UpdateAccountAsync succeeds and reloads accounts.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateAccountAsync_ClosesFormAndReloads_WhenSuccessful()
    {
        var account = CreateAccount("Original");
        _apiService.Accounts.Add(account);
        await _sut.InitializeAsync();
        _sut.ShowEditAccount(account);

        _apiService.UpdateAccountResult = ApiResult<AccountDto>.Success(account);

        await _sut.UpdateAccountAsync(new AccountCreateDto { Name = "Updated" });

        _sut.ShowEditForm.ShouldBeFalse();
        _sut.EditingAccountId.ShouldBeNull();
        _sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that UpdateAccountAsync does nothing when no account is being edited.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateAccountAsync_DoesNothing_WhenNoEditingAccount()
    {
        await _sut.UpdateAccountAsync(new AccountCreateDto { Name = "Test" });

        _sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that UpdateAccountAsync handles conflict response.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateAccountAsync_ShowsWarning_WhenConflict()
    {
        var account = CreateAccount("Conflicting");
        _apiService.Accounts.Add(account);
        await _sut.InitializeAsync();
        _sut.ShowEditAccount(account);

        _apiService.UpdateAccountResult = ApiResult<AccountDto>.Conflict();

        await _sut.UpdateAccountAsync(new AccountCreateDto { Name = "Updated" });

        _toastService.WarningShown.ShouldBeTrue();
        _sut.ShowEditForm.ShouldBeFalse();
        _sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that UpdateAccountAsync sets IsSubmitting to false after failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateAccountAsync_ClearsIsSubmitting_WhenApiReturnsFailure()
    {
        var account = CreateAccount("Test");
        _apiService.Accounts.Add(account);
        await _sut.InitializeAsync();
        _sut.ShowEditAccount(account);

        _apiService.UpdateAccountResult = ApiResult<AccountDto>.Failure();

        await _sut.UpdateAccountAsync(new AccountCreateDto { Name = "Updated" });

        _sut.IsSubmitting.ShouldBeFalse();
    }

    // --- ShowTransfer ---

    /// <summary>
    /// Verifies that ShowTransfer opens the transfer dialog without a pre-selected source.
    /// </summary>
    [Fact]
    public void ShowTransfer_OpensDialogWithoutPreselection()
    {
        _sut.ShowTransfer();

        _sut.ShowTransferDialog.ShouldBeTrue();
        _sut.PreSelectedSourceAccountId.ShouldBeNull();
        _sut.NewTransfer.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies state change notification on ShowTransfer.
    /// </summary>
    [Fact]
    public void ShowTransfer_NotifiesStateChanged()
    {
        bool notified = false;
        _sut.OnStateChanged = () => notified = true;

        _sut.ShowTransfer();

        notified.ShouldBeTrue();
    }

    // --- ShowTransferFrom ---

    /// <summary>
    /// Verifies that ShowTransferFrom opens the transfer dialog with a pre-selected source.
    /// </summary>
    [Fact]
    public void ShowTransferFrom_OpensDialogWithPreselection()
    {
        var accountId = Guid.NewGuid();

        _sut.ShowTransferFrom(accountId);

        _sut.ShowTransferDialog.ShouldBeTrue();
        _sut.PreSelectedSourceAccountId.ShouldBe(accountId);
        _sut.NewTransfer.SourceAccountId.ShouldBe(accountId);
    }

    /// <summary>
    /// Verifies state change notification on ShowTransferFrom.
    /// </summary>
    [Fact]
    public void ShowTransferFrom_NotifiesStateChanged()
    {
        bool notified = false;
        _sut.OnStateChanged = () => notified = true;

        _sut.ShowTransferFrom(Guid.NewGuid());

        notified.ShouldBeTrue();
    }

    // --- HideTransfer ---

    /// <summary>
    /// Verifies that HideTransfer closes the dialog and clears pre-selection.
    /// </summary>
    [Fact]
    public void HideTransfer_ClosesDialogAndClearsState()
    {
        _sut.ShowTransferFrom(Guid.NewGuid());

        _sut.HideTransfer();

        _sut.ShowTransferDialog.ShouldBeFalse();
        _sut.PreSelectedSourceAccountId.ShouldBeNull();
    }

    /// <summary>
    /// Verifies state change notification on HideTransfer.
    /// </summary>
    [Fact]
    public void HideTransfer_NotifiesStateChanged()
    {
        bool notified = false;
        _sut.OnStateChanged = () => notified = true;

        _sut.HideTransfer();

        notified.ShouldBeTrue();
    }

    // --- CreateTransferAsync ---

    /// <summary>
    /// Verifies that CreateTransferAsync closes the dialog on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateTransferAsync_ClosesDialog_WhenSuccessful()
    {
        _sut.ShowTransfer();
        _apiService.CreateTransferResult = new TransferResponse();

        await _sut.CreateTransferAsync(new CreateTransferRequest());

        _sut.ShowTransferDialog.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that CreateTransferAsync keeps dialog open on failure (null result).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateTransferAsync_KeepsDialogOpen_WhenApiFails()
    {
        _sut.ShowTransfer();
        _apiService.CreateTransferResult = null;

        await _sut.CreateTransferAsync(new CreateTransferRequest());

        _sut.ShowTransferDialog.ShouldBeTrue();
    }

    // --- CreateAccountAsync ---

    /// <summary>
    /// Verifies that CreateAccountAsync closes the form and reloads on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateAccountAsync_ClosesFormAndReloads_WhenSuccessful()
    {
        _sut.ShowAddAccount();
        _apiService.CreateAccountResult = CreateAccount("New");

        await _sut.CreateAccountAsync(new AccountCreateDto { Name = "New" });

        _sut.ShowAddForm.ShouldBeFalse();
        _sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that CreateAccountAsync keeps form open when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateAccountAsync_KeepsFormOpen_WhenApiFails()
    {
        _sut.ShowAddAccount();
        _apiService.CreateAccountResult = null;

        await _sut.CreateAccountAsync(new AccountCreateDto { Name = "Fail" });

        _sut.ShowAddForm.ShouldBeTrue();
        _sut.IsSubmitting.ShouldBeFalse();
    }

    // --- ViewAccount ---

    /// <summary>
    /// Verifies that ViewAccount navigates to the account transactions page.
    /// </summary>
    [Fact]
    public void ViewAccount_NavigatesToTransactionsPage()
    {
        var id = Guid.NewGuid();

        _sut.ViewAccount(id);

        _navigationManager.LastNavigatedUri.ShouldBe($"/transactions?account={id}");
    }

    // --- DeleteAccount ---

    /// <summary>
    /// Verifies that DeleteAccount shows the confirmation dialog for a valid account.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteAccount_ShowsConfirmDialog_WhenAccountExists()
    {
        var account = CreateAccount("ToDelete");
        _apiService.Accounts.Add(account);
        await _sut.InitializeAsync();

        _sut.DeleteAccount(account.Id);

        _sut.ShowDeleteConfirm.ShouldBeTrue();
        _sut.DeletingAccount.ShouldBe(account);
    }

    /// <summary>
    /// Verifies that DeleteAccount does not show dialog for unknown account.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteAccount_DoesNotShowDialog_WhenAccountNotFound()
    {
        await _sut.InitializeAsync();

        _sut.DeleteAccount(Guid.NewGuid());

        _sut.ShowDeleteConfirm.ShouldBeFalse();
        _sut.DeletingAccount.ShouldBeNull();
    }

    /// <summary>
    /// Verifies state change notification on DeleteAccount.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteAccount_NotifiesStateChanged()
    {
        var account = CreateAccount("Test");
        _apiService.Accounts.Add(account);
        await _sut.InitializeAsync();

        bool notified = false;
        _sut.OnStateChanged = () => notified = true;

        _sut.DeleteAccount(account.Id);

        notified.ShouldBeTrue();
    }

    // --- ConfirmDeleteAsync ---

    /// <summary>
    /// Verifies that ConfirmDeleteAsync deletes the account and reloads.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfirmDeleteAsync_DeletesAndReloads_WhenSuccessful()
    {
        var account = CreateAccount("ToDelete");
        _apiService.Accounts.Add(account);
        await _sut.InitializeAsync();
        _sut.DeleteAccount(account.Id);

        _apiService.DeleteAccountResult = true;
        await _sut.ConfirmDeleteAsync();

        _sut.ShowDeleteConfirm.ShouldBeFalse();
        _sut.DeletingAccount.ShouldBeNull();
        _sut.IsDeleting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ConfirmDeleteAsync does nothing when no account is pending deletion.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfirmDeleteAsync_DoesNothing_WhenNoDeletingAccount()
    {
        await _sut.ConfirmDeleteAsync();

        _sut.IsDeleting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ConfirmDeleteAsync keeps dialog open when API returns false.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfirmDeleteAsync_KeepsDialogOpen_WhenDeleteFails()
    {
        var account = CreateAccount("KeepMe");
        _apiService.Accounts.Add(account);
        await _sut.InitializeAsync();
        _sut.DeleteAccount(account.Id);

        _apiService.DeleteAccountResult = false;
        await _sut.ConfirmDeleteAsync();

        _sut.ShowDeleteConfirm.ShouldBeTrue();
        _sut.IsDeleting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies ConfirmDeleteAsync notifies state changed for loading indicator.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfirmDeleteAsync_NotifiesStateChanged()
    {
        var account = CreateAccount("Test");
        _apiService.Accounts.Add(account);
        await _sut.InitializeAsync();
        _sut.DeleteAccount(account.Id);

        int callCount = 0;
        _sut.OnStateChanged = () => callCount++;
        _apiService.DeleteAccountResult = true;

        await _sut.ConfirmDeleteAsync();

        callCount.ShouldBeGreaterThan(0);
    }

    // --- CancelDelete ---

    /// <summary>
    /// Verifies that CancelDelete hides the dialog and clears the deleting account.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CancelDelete_HidesDialogAndClearsState()
    {
        var account = CreateAccount("Test");
        _apiService.Accounts.Add(account);
        await _sut.InitializeAsync();
        _sut.DeleteAccount(account.Id);

        _sut.CancelDelete();

        _sut.ShowDeleteConfirm.ShouldBeFalse();
        _sut.DeletingAccount.ShouldBeNull();
    }

    /// <summary>
    /// Verifies state change notification on CancelDelete.
    /// </summary>
    [Fact]
    public void CancelDelete_NotifiesStateChanged()
    {
        bool notified = false;
        _sut.OnStateChanged = () => notified = true;

        _sut.CancelDelete();

        notified.ShouldBeTrue();
    }

    // --- OnStateChanged ---

    /// <summary>
    /// Verifies that OnStateChanged callback is invoked after state mutations.
    /// </summary>
    [Fact]
    public void OnStateChanged_InvokedAfterStateMutations()
    {
        int callCount = 0;
        _sut.OnStateChanged = () => callCount++;

        _sut.ShowAddAccount();
        _sut.HideAddAccount();
        _sut.ShowTransfer();
        _sut.HideTransfer();
        _sut.DismissError();
        _sut.CancelDelete();

        callCount.ShouldBe(6);
    }

    private static AccountDto CreateAccount(
        string name = "Test Account",
        string type = "Checking",
        decimal balance = 1000m,
        string version = "v1")
    {
        return new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            InitialBalance = balance,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
            Version = version,
        };
    }

    /// <summary>
    /// Stub toast service for ViewModel testing.
    /// </summary>
    private sealed class StubToastService : IToastService
    {
        /// <inheritdoc/>
        public event Action? OnChange;

        /// <summary>
        /// Gets a value indicating whether a warning was shown.
        /// </summary>
        public bool WarningShown
        {
            get; private set;
        }

        /// <inheritdoc/>
        public IReadOnlyList<ToastItem> Toasts { get; } = [];

        /// <inheritdoc/>
        public void ShowSuccess(string message, string? title = null)
        {
        }

        /// <inheritdoc/>
        public void ShowError(string message, string? title = null)
        {
        }

        /// <inheritdoc/>
        public void ShowInfo(string message, string? title = null)
        {
        }

        /// <inheritdoc/>
        public void ShowWarning(string message, string? title = null)
        {
            this.WarningShown = true;
            this.OnChange?.Invoke();
        }

        /// <inheritdoc/>
        public void Remove(Guid id)
        {
        }
    }

    /// <summary>
    /// Stub NavigationManager for testing navigation calls.
    /// </summary>
    private sealed class StubNavigationManager : NavigationManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StubNavigationManager"/> class.
        /// </summary>
        public StubNavigationManager()
        {
            this.Initialize("https://localhost/", "https://localhost/");
        }

        /// <summary>
        /// Gets the last URI that was navigated to.
        /// </summary>
        public string? LastNavigatedUri
        {
            get; private set;
        }

        /// <inheritdoc/>
        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            this.LastNavigatedUri = uri;
        }
    }
}
