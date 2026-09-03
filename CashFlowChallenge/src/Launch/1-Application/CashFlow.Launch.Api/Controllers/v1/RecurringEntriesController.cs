using Asp.Versioning;
using CashFlow.Launch.Api.Dtos.Requests;
using CashFlow.Launch.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Launch.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/recurring-entries")]
[Authorize]
public sealed class RecurringEntriesController : ControllerBase
{
    private readonly IRecurringEntryService _recurringEntryService;

    public RecurringEntriesController(IRecurringEntryService recurringEntryService)
    {
        _recurringEntryService = recurringEntryService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRecurringEntryRequest request)
    {
        var recurringEntry = await _recurringEntryService.CreateAsync(
            request.Amount,
            request.Type,
            request.Description,
            request.AccountId,
            request.CategoryId,
            request.Frequency,
            request.StartAt,
            request.EndAt);

        return recurringEntry is null ? BadRequest() : Ok(recurringEntry);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateRecurringEntryRequest request)
    {
        var recurringEntry = await _recurringEntryService.UpdateAsync(
            id,
            request.Amount,
            request.Type,
            request.Description,
            request.AccountId,
            request.CategoryId,
            request.Frequency,
            request.StartAt,
            request.EndAt,
            request.IsActive);

        return recurringEntry is null ? BadRequest() : Ok(recurringEntry);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id) =>
        await _recurringEntryService.DeleteAsync(id) ? NoContent() : NotFound();

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _recurringEntryService.GetAllAsync());

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromQuery] DateTime until)
    {
        var generated = await _recurringEntryService.GenerateDueEntriesAsync(until);
        return Ok(new { generated });
    }
}
