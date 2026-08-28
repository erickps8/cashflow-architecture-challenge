using Asp.Versioning;
using CashFlow.Launch.Api.Dtos.Requests;
using CashFlow.Launch.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Launch.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/budgets")]
[Authorize]
public class BudgetsController : ControllerBase
{
    private readonly IMonthlyBudgetService _service;
    public BudgetsController(IMonthlyBudgetService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Set(SetMonthlyBudgetRequest request)
    {
        var result = await _service.SetAsync(request.Year, request.Month, request.CategoryId, request.PlannedAmount);
        return result is null ? BadRequest() : Ok(result);
    }

    [HttpGet("{year:int}/{month:int}")]
    public async Task<IActionResult> GetSummary(int year, int month)
    {
        var result = await _service.GetSummaryAsync(year, month);
        return result is null ? BadRequest() : Ok(result);
    }

    [HttpDelete("{year:int}/categories/{categoryId:guid}")]
    public async Task<IActionResult> RemoveCategory(int year, Guid categoryId) =>
        await _service.RemoveCategoryFromYearAsync(year, categoryId) ? NoContent() : BadRequest();

    [HttpDelete("{year:int}")]
    public async Task<IActionResult> ClearYear(int year) =>
        await _service.ClearYearAsync(year) ? NoContent() : BadRequest();
}
