using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Models;

namespace CashFlow.Launch.Domain.Interfaces.Services;

public interface ICreditCardService
{
    Task<CreditCard?> CreateCardAsync(string name, decimal limit, int closingDay, int dueDay);
    Task<List<CreditCard>> GetCardsAsync();
    Task<CreditCardPurchase?> CreatePurchaseAsync(Guid creditCardId, Guid? categoryId, string description, decimal totalAmount, int installmentsCount, DateTime purchaseDate);
    Task<CreditCardInvoiceSummary?> GetInvoiceAsync(Guid creditCardId, int year, int month);
    Task<bool> MarkInstallmentPaidAsync(Guid installmentId);
}
