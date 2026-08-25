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
public class EntriesController : MainController
{
    private readonly IEntryService _service;

    public EntriesController(IEntryService service, INotificator notificator) : base(notificator)
    {
        _service = service;
    }

    [Authorize(Roles = "entries-create,gestor")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateEntryRequest request)
    {
        var result = await _service.CreateAsync(request.Amount, request.Type, request.Description, request.OccurredAt, request.AccountId, request.CategoryId, request.IsRecurring);
        return CustomResponse(result);
    }

    [Authorize(Roles = "entries,gestor")]
    [HttpGet]
    public async Task<IActionResult> GetAll() => CustomResponse(await _service.GetAllAsync());

    [Authorize(Roles = "entries,gestor")]
    [HttpGet("monthly/{year:int}/{month:int}")]
    public async Task<IActionResult> GetByMonth(int year, int month)
    {
        if (month < 1 || month > 12) return BadRequest("Month must be between 1 and 12.");
        return CustomResponse(await _service.GetByMonthAsync(year, month));
    }
}
