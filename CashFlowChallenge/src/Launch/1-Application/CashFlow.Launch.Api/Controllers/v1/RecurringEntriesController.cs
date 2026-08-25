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
public class RecurringEntriesController : ControllerBase
{
    private readonly IRecurringEntryService _service;

    public RecurringEntriesController(IRecurringEntryService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRecurringEntryRequest request)
    {
        var result = await _service.CreateAsync(request.Amount, request.Type, request.Description, request.AccountId, request.CategoryId, request.Frequency, request.StartAt, request.EndAt);
        return result is null ? BadRequest() : Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromQuery] DateTime until)
    {
        var generated = await _service.GenerateDueEntriesAsync(until);
        return Ok(new { generated });
    }
}
