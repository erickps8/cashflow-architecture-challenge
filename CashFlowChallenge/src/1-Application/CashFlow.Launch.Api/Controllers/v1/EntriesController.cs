using Asp.Versioning;
using CashFlow.Launch.Api.Controllers.Base;
using CashFlow.Launch.Api.Dtos.Requests;
using CashFlow.Launch.Domain.Interfaces.Services;
using CashFlow.Launch.Domain.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Launch.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/entries")]
public class EntriesController : MainController
{
    private readonly IEntryService _service;

    public EntriesController(
        IEntryService service,
        INotificator notificator)
        : base(notificator)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateEntryRequest request)
    {
        var result = await _service.CreateAsync(
            request.Amount,
            request.Type,
            request.Description,
            request.OccurredAt);

        return CustomResponse(result);
    }
}