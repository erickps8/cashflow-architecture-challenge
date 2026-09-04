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
public sealed class CreditCardsController : ControllerBase
{
    private readonly ICreditCardService _creditCardService;

    public CreditCardsController(ICreditCardService creditCardService)
    {
        _creditCardService = creditCardService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCreditCardRequest request)
    {
        var card = await _creditCardService.CreateCardAsync(
            request.Name,
            request.Limit,
            request.ClosingDay,
            request.DueDay);

        return card is null ? BadRequest() : Ok(card);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CreateCreditCardRequest request)
    {
        var card = await _creditCardService.UpdateCardAsync(
            id,
            request.Name,
            request.Limit,
            request.ClosingDay,
            request.DueDay);

        return card is null ? BadRequest() : Ok(card);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id) =>
        await _creditCardService.DeleteCardAsync(id) ? NoContent() : NotFound();

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _creditCardService.GetCardsAsync());

    [HttpPost("purchases")]
    public async Task<IActionResult> CreatePurchase(CreateCreditCardPurchaseRequest request)
    {
        var purchase = await _creditCardService.CreatePurchaseAsync(
            request.CreditCardId,
            request.CategoryId,
            request.Description,
            request.TotalAmount,
            request.InstallmentsCount,
            request.PurchaseDate);

        return purchase is null ? BadRequest() : Ok(purchase);
    }

    [HttpPut("purchases/{id:guid}")]
    public async Task<IActionResult> UpdatePurchase(Guid id, CreateCreditCardPurchaseRequest request)
    {
        var purchase = await _creditCardService.UpdatePurchaseAsync(
            id,
            request.CreditCardId,
            request.CategoryId,
            request.Description,
            request.TotalAmount,
            request.InstallmentsCount,
            request.PurchaseDate);

        return purchase is null
            ? BadRequest("Não foi possível editar. Compras com parcela já paga não podem ser alteradas.")
            : Ok(purchase);
    }

    [HttpDelete("purchases/{id:guid}")]
    public async Task<IActionResult> DeletePurchase(Guid id) =>
        await _creditCardService.DeletePurchaseAsync(id)
            ? NoContent()
            : BadRequest("Não foi possível excluir. Compras com parcela já paga são preservadas.");

    [HttpGet("{creditCardId:guid}/invoices/{year:int}/{month:int}")]
    public async Task<IActionResult> GetInvoice(Guid creditCardId, int year, int month)
    {
        var invoice = await _creditCardService.GetInvoiceAsync(creditCardId, year, month);
        return invoice is null ? NotFound() : Ok(invoice);
    }

    [HttpPost("installments/{installmentId:guid}/pay")]
    public async Task<IActionResult> Pay(Guid installmentId) =>
        await _creditCardService.MarkInstallmentPaidAsync(installmentId) ? Ok() : NotFound();
}
