using CashFlow.Consolidation.Domain.Notifications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CashFlow.Consolidation.Api.Controllers.Base;

[ApiController]
public abstract class MainController : ControllerBase
{
    private readonly INotificator _notificator;

    protected MainController(INotificator notificator)
    {
        _notificator = notificator;
    }

    protected bool OperacaoValida()
    {
        return !_notificator.HasNotification();
    }

    protected ActionResult CustomResponse(object? result = null)
    {
        if (OperacaoValida())
        {
            return Ok(new
            {
                success = true,
                data = result
            });
        }

        return BadRequest(new
        {
            success = false,
            errors = _notificator
                .GetNotifications()
                .Select(x => x.Message)
        });
    }

    protected ActionResult CustomResponse(ModelStateDictionary modelState)
    {
        if (!modelState.IsValid)
            NotifyModelStateErrors(modelState);

        return CustomResponse();
    }

    protected void NotifyModelStateErrors(ModelStateDictionary modelState)
    {
        var errors = modelState.Values.SelectMany(x => x.Errors);

        foreach (var error in errors)
        {
            var errorMessage = error.Exception == null
                ? error.ErrorMessage
                : error.Exception.Message;

            NotifyError(errorMessage);
        }
    }

    protected void NotifyError(string message)
    {
        _notificator.Handle(new Notification(message));
    }
}