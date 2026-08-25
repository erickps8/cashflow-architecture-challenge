using Asp.Versioning;
using CashFlow.Launch.Api.Dtos.Requests;
using CashFlow.Launch.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Launch.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/credit-cards")]
[Authorize]
public class CreditCardsController : ControllerBase
{
    private readonly ICreditCardService _service;

    public CreditCardsController(ICreditCardService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Create(CreateCreditCardRequest request)
    {
        var card = await _service.CreateCardAsync(request.Name, request.Limit, request.ClosingDay, request.DueDay);
        return card is null ? BadRequest() : Ok(card);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetCardsAsync());

    [HttpPost("purchases")]
    public async Task<IActionResult> CreatePurchase(CreateCreditCardPurchaseRequest request)
    {
        var purchase = await _service.CreatePurchaseAsync(request.CreditCardId, request.CategoryId, request.Description, request.TotalAmount, request.InstallmentsCount, request.PurchaseDate);
        return purchase is null ? BadRequest() : Ok(purchase);
    }

    [HttpGet("{creditCardId:guid}/invoices/{year:int}/{month:int}")]
    public async Task<IActionResult> GetInvoice(Guid creditCardId, int year, int month)
    {
        var invoice = await _service.GetInvoiceAsync(creditCardId, year, month);
        return invoice is null ? NotFound() : Ok(invoice);
    }

    [HttpPost("installments/{installmentId:guid}/pay")]
    public async Task<IActionResult> PayInstallment(Guid installmentId)
    {
        return await _service.MarkInstallmentPaidAsync(installmentId) ? Ok() : NotFound();
    }
}
