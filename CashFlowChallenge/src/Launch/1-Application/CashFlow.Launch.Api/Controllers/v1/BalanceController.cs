using Asp.Versioning;
using CashFlow.Launch.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Launch.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/balance")]
[Authorize]
public class BalanceController : ControllerBase
{
    private readonly IMonthlyBalanceService _service;

    public BalanceController(IMonthlyBalanceService service) => _service = service;

    [HttpGet("monthly/{year:int}/{month:int}")]
    public async Task<IActionResult> GetMonth(int year, int month, [FromQuery] decimal openingBalance = 0)
    {
        if (month is < 1 or > 12) return BadRequest("Month must be between 1 and 12.");
        return Ok(await _service.GetMonthAsync(year, month, openingBalance));
    }

    [HttpGet("projection")]
    public async Task<IActionResult> GetProjection([FromQuery] int startYear, [FromQuery] int startMonth, [FromQuery] int months = 12, [FromQuery] decimal initialBalance = 0)
    {
        if (startMonth is < 1 or > 12 || months is < 1 or > 60) return BadRequest();
        return Ok(await _service.GetProjectionAsync(startYear, startMonth, months, initialBalance));
    }
}
