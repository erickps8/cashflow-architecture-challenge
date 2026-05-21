using Asp.Versioning;
using CashFlow.Consolidation.Api.Controllers.Base;
using CashFlow.Consolidation.Domain.Interfaces;
using CashFlow.Consolidation.Domain.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Consolidation.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/daily-consolidations")]
public class DailyConsolidationController : MainController
{
    private readonly IDailyConsolidationService _service;

    public DailyConsolidationController(
        IDailyConsolidationService service,
        INotificator notificator)
        : base(notificator)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();

        return CustomResponse(result);
    }

    [HttpPost("reprocess")]
    public async Task<IActionResult> Reprocess()
    {
        var result = await _service.ReprocessAsync();

        return CustomResponse(result);
    }
}