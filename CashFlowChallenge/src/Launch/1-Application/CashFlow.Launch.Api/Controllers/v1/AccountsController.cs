using Asp.Versioning;
using CashFlow.Launch.Api.Dtos.Requests;
using CashFlow.Launch.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Launch.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/accounts")]
[Authorize]
public sealed class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAccountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Account name is required.");

        return Ok(await _accountService.CreateAsync(
            request.Name,
            request.Type,
            request.InitialBalance));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CreateAccountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Account name is required.");

        var account = await _accountService.UpdateAsync(
            id,
            request.Name,
            request.Type,
            request.InitialBalance);

        return account is null ? NotFound() : Ok(account);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id) =>
        await _accountService.DeleteAsync(id) ? NoContent() : NotFound();

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _accountService.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var account = await _accountService.GetByIdAsync(id);
        return account is null ? NotFound() : Ok(account);
    }
}
