using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Domain.Interfaces.Services;
using CashFlow.Launch.Domain.Models;

namespace CashFlow.Launch.Domain.Services;

public class CreditCardService : ICreditCardService
{
    private readonly ICreditCardRepository _cardRepository;
    private readonly ICreditCardPurchaseRepository _purchaseRepository;
    private readonly ICreditCardInstallmentRepository _installmentRepository;
    private readonly ICategoryRepository _categoryRepository;

    public CreditCardService(ICreditCardRepository cardRepository, ICreditCardPurchaseRepository purchaseRepository, ICreditCardInstallmentRepository installmentRepository, ICategoryRepository categoryRepository)
    {
        _cardRepository = cardRepository;
        _purchaseRepository = purchaseRepository;
        _installmentRepository = installmentRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<CreditCard?> CreateCardAsync(string name, decimal limit, int closingDay, int dueDay)
    {
        if (string.IsNullOrWhiteSpace(name) || limit < 0 || closingDay is < 1 or > 28 || dueDay is < 1 or > 28) return null;
        var card = new CreditCard { Name = name.Trim(), Limit = limit, ClosingDay = closingDay, DueDay = dueDay };
        await _cardRepository.AddAsync(card);
        await _cardRepository.SaveChangesAsync();
        return card;
    }

    public Task<List<CreditCard>> GetCardsAsync() => _cardRepository.GetAllAsync();

    public async Task<CreditCardPurchase?> CreatePurchaseAsync(Guid creditCardId, Guid? categoryId, string description, decimal totalAmount, int installmentsCount, DateTime purchaseDate)
    {
        var card = await _cardRepository.GetByIdAsync(creditCardId);
        if (card is null || totalAmount <= 0 || installmentsCount < 1 || installmentsCount > 120) return null;
        if (categoryId.HasValue && await _categoryRepository.GetByIdAsync(categoryId.Value) is null) return null;

        purchaseDate = DateTime.SpecifyKind(purchaseDate, DateTimeKind.Utc);
        var purchase = new CreditCardPurchase
        {
            CreditCardId = creditCardId,
            CategoryId = categoryId,
            Description = description.Trim(),
            TotalAmount = totalAmount,
            InstallmentsCount = installmentsCount,
            PurchaseDate = purchaseDate
        };

        await _purchaseRepository.AddAsync(purchase);

        var baseAmount = Math.Floor((totalAmount / installmentsCount) * 100m) / 100m;
        var firstReference = GetFirstReferenceDate(purchaseDate, card.ClosingDay);

        for (var i = 1; i <= installmentsCount; i++)
        {
            var reference = firstReference.AddMonths(i - 1);
            var amount = i == installmentsCount ? totalAmount - baseAmount * (installmentsCount - 1) : baseAmount;
            await _installmentRepository.AddAsync(new CreditCardInstallment
            {
                CreditCardPurchaseId = purchase.Id,
                Number = i,
                Amount = amount,
                ReferenceDate = reference,
                DueDate = GetDueDate(reference, card.ClosingDay, card.DueDay)
            });
        }

        await _purchaseRepository.SaveChangesAsync();
        return purchase;
    }

    public async Task<CreditCardInvoiceSummary?> GetInvoiceAsync(Guid creditCardId, int year, int month)
    {
        var card = await _cardRepository.GetByIdAsync(creditCardId);
        if (card is null || month is < 1 or > 12) return null;
        var installments = await _installmentRepository.GetByCardAndReferenceAsync(creditCardId, year, month);
        var reference = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        return new CreditCardInvoiceSummary
        {
            CreditCardId = card.Id,
            CreditCardName = card.Name,
            Year = year,
            Month = month,
            DueDate = GetDueDate(reference, card.ClosingDay, card.DueDay),
            TotalAmount = installments.Sum(x => x.Amount),
            PaidAmount = installments.Where(x => x.IsPaid).Sum(x => x.Amount),
            Items = installments.Select(x => new CreditCardInvoiceItem
            {
                InstallmentId = x.Id,
                PurchaseId = x.CreditCardPurchaseId,
                Description = x.CreditCardPurchase?.Description ?? string.Empty,
                InstallmentNumber = x.Number,
                InstallmentsCount = x.CreditCardPurchase?.InstallmentsCount ?? 0,
                Amount = x.Amount,
                IsPaid = x.IsPaid,
                CategoryId = x.CreditCardPurchase?.CategoryId
            }).ToList()
        };
    }

    public async Task<bool> MarkInstallmentPaidAsync(Guid installmentId)
    {
        var installment = await _installmentRepository.GetByIdAsync(installmentId);
        if (installment is null) return false;
        installment.IsPaid = true;
        await _installmentRepository.SaveChangesAsync();
        return true;
    }

    private static DateTime GetFirstReferenceDate(DateTime purchaseDate, int closingDay)
    {
        var month = new DateTime(purchaseDate.Year, purchaseDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return purchaseDate.Day <= closingDay ? month : month.AddMonths(1);
    }

    private static DateTime GetDueDate(DateTime referenceDate, int closingDay, int dueDay)
    {
        var dueMonth = dueDay > closingDay ? referenceDate : referenceDate.AddMonths(1);
        return new DateTime(dueMonth.Year, dueMonth.Month, dueDay, 0, 0, 0, DateTimeKind.Utc);
    }
}
