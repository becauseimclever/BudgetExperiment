// <copyright file="AccountsController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Application.Services;

using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for account operations.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class AccountsController : ControllerBase
{
    private readonly AccountService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountsController"/> class.
    /// </summary>
    /// <param name="service">The account service.</param>
    public AccountsController(AccountService service)
    {
        this._service = service;
    }

    /// <summary>
    /// Gets all accounts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of accounts.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AccountDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        var accounts = await this._service.GetAllAsync(cancellationToken);
        return this.Ok(accounts);
    }

    /// <summary>
    /// Gets a specific account by ID.
    /// </summary>
    /// <param name="id">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The account if found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<AccountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await this._service.GetByIdAsync(id, cancellationToken);
        if (account is null)
        {
            return this.NotFound();
        }

        return this.Ok(account);
    }

    /// <summary>
    /// Creates a new account.
    /// </summary>
    /// <param name="dto">The account creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created account.</returns>
    [HttpPost]
    [ProducesResponseType<AccountDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync([FromBody] AccountCreateDto dto, CancellationToken cancellationToken)
    {
        var account = await this._service.CreateAsync(dto, cancellationToken);
        return this.CreatedAtAction("GetById", new { id = account.Id }, account);
    }

    /// <summary>
    /// Updates an existing account.
    /// </summary>
    /// <param name="id">The account identifier.</param>
    /// <param name="dto">The account update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated account.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<AccountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] AccountUpdateDto dto, CancellationToken cancellationToken)
    {
        var account = await this._service.UpdateAsync(id, dto, cancellationToken);
        if (account is null)
        {
            return this.NotFound();
        }

        return this.Ok(account);
    }

    /// <summary>
    /// Deletes an account by ID.
    /// </summary>
    /// <param name="id">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var removed = await this._service.RemoveAsync(id, cancellationToken);
        if (!removed)
        {
            return this.NotFound();
        }

        return this.NoContent();
    }
}
