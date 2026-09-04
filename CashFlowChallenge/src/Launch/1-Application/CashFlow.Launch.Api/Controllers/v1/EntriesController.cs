using Asp.Versioning;
using CashFlow.Launch.Api.Controllers.Base;
using CashFlow.Launch.Api.Dtos.Requests;
using CashFlow.Launch.Domain.Interfaces.Services;
using CashFlow.Launch.Domain.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Launch.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/entries")]
[Authorize]
public sealed class EntriesController : MainController
{
    private readonly IEntryService _entryService;

    public EntriesController(IEntryService entryService, INotificator notificator) : base(notificator) => _entryService = entryService;

    [HttpPost]
    public async Task<IActionResult> Create(CreateEntryRequest request) => CustomResponse(await _entryService.CreateAsync(request.Amount, request.Type, request.Description, request.OccurredAt, request.AccountId, request.CategoryId, request.IsRecurring));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CreateEntryRequest request) => CustomResponse(await _entryService.UpdateAsync(id, request.Amount, request.Type, request.Description, request.OccurredAt, request.AccountId, request.CategoryId, request.IsRecurring));

    [HttpPost("{id:guid}/defer")]
    public async Task<IActionResult> Defer(Guid id) => CustomResponse(await _entryService.DeferToNextMonthAsync(id));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id) => CustomResponse(await _entryService.DeleteAsync(id));

    [HttpGet]
    public async Task<IActionResult> GetAll() => CustomResponse(await _entryService.GetAllAsync());

    [HttpGet("monthly/{year:int}/{month:int}")]
    public async Task<IActionResult> GetByMonth(int year, int month)
    {
        if (month is < 1 or > 12) return BadRequest("Month must be between 1 and 12.");
        return CustomResponse(await _entryService.GetByMonthAsync(year, month));
    }
}
