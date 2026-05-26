using Asp.Versioning;
using CashFlow.Launch.Api.Controllers.Base;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Domain.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Launch.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/outbox-messages")]
public class OutboxMessagesController : MainController
{
    private readonly IOutboxMessageRepository _repository;

    public OutboxMessagesController(
        IOutboxMessageRepository repository,
        INotificator notificator)
        : base(notificator)
    {
        _repository = repository;
    }

    [Authorize(Roles = "outbox-messages")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _repository.GetAllAsync();

        return CustomResponse(result);
    }
}