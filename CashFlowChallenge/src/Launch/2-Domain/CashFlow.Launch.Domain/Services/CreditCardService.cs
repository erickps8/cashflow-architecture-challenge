using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Enums;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Domain.Interfaces.Services;
using CashFlow.Launch.Domain.Models;

namespace CashFlow.Launch.Domain.Services;

public sealed class CreditCardService : ICreditCardService
{
    private const int MinimumDay = 1;
    private const int MaximumBillingDay = 28;
    private const int MaximumInstallments = 120;

    private readonly ICreditCardRepository _cardRepository;
    private readonly ICreditCardPurchaseRepository _purchaseRepository;
    private readonly ICreditCardInstallmentRepository _installmentRepository;
    private readonly ICategoryRepository _categoryRepository;

    public CreditCardService(
        ICreditCardRepository cardRepository,
        ICreditCardPurchaseRepository purchaseRepository,
        ICreditCardInstallmentRepository installmentRepository,
        ICategoryRepository categoryRepository)
    {
        _cardRepository = cardRepository;
        _purchaseRepository = purchaseRepository;
        _installmentRepository = installmentRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<CreditCard?> CreateCardAsync(
        string name,
        decimal limit,
        int closingDay,
        int dueDay)
    {
        if (!IsCardValid(name, limit, closingDay, dueDay))
            return null;

        var card = new CreditCard
        {
            Name = name.Trim(),
            Limit = limit,
            ClosingDay = closingDay,
            DueDay = dueDay
        };

        await _cardRepository.AddAsync(card);
        await _cardRepository.SaveChangesAsync();
        return card;
    }

    public async Task<CreditCard?> UpdateCardAsync(
        Guid id,
        string name,
        decimal limit,
        int closingDay,
        int dueDay)
    {
        var card = await _cardRepository.GetByIdAsync(id);
        if (card is null || !IsCardValid(name, limit, closingDay, dueDay))
            return null;

        card.Name = name.Trim();
        card.Limit = limit;
        card.ClosingDay = closingDay;
        card.DueDay = dueDay;

        _cardRepository.Update(card);
        await _cardRepository.SaveChangesAsync();
        return card;
    }

    public async Task<bool> DeleteCardAsync(Guid id)
    {
        var card = await _cardRepository.GetByIdAsync(id);
        if (card is null)
            return false;

        card.IsActive = false;
        _cardRepository.Update(card);
        await _cardRepository.SaveChangesAsync();
        return true;
    }

    public Task<List<CreditCard>> GetCardsAsync() => _cardRepository.GetAllAsync();

    public async Task<CreditCardPurchase?> CreatePurchaseAsync(
        Guid cardId,
        Guid? categoryId,
        string description,
        decimal totalAmount,
        int installmentsCount,
        DateTime purchaseDate)
    {
        var card = await GetValidCardForPurchaseAsync(
            cardId,
            categoryId,
            description,
            totalAmount,
            installmentsCount);

        if (card is null)
            return null;

        var purchase = new CreditCardPurchase
        {
            CreditCardId = cardId,
            CategoryId = categoryId,
            Description = description.Trim(),
            TotalAmount = totalAmount,
            InstallmentsCount = installmentsCount,
            PurchaseDate = ToUtc(purchaseDate)
        };

        await _purchaseRepository.AddAsync(purchase);
        await AddInstallmentsAsync(purchase, card);
        await _purchaseRepository.SaveChangesAsync();
        return purchase;
    }

    public async Task<CreditCardPurchase?> UpdatePurchaseAsync(
        Guid id,
        Guid cardId,
        Guid? categoryId,
        string description,
        decimal totalAmount,
        int installmentsCount,
        DateTime purchaseDate)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(id);
        if (purchase is null)
            return null;

        var card = await GetValidCardForPurchaseAsync(
            cardId,
            categoryId,
            description,
            totalAmount,
            installmentsCount);

        if (card is null)
            return null;

        var existingInstallments = await GetPurchaseInstallmentsAsync(id);
        if (existingInstallments.Any(item => item.IsPaid))
            return null;

        foreach (var installment in existingInstallments)
            _installmentRepository.Remove(installment);

        purchase.CreditCardId = cardId;
        purchase.CategoryId = categoryId;
        purchase.Description = description.Trim();
        purchase.TotalAmount = totalAmount;
        purchase.InstallmentsCount = installmentsCount;
        purchase.PurchaseDate = ToUtc(purchaseDate);

        _purchaseRepository.Update(purchase);
        await AddInstallmentsAsync(purchase, card);
        await _purchaseRepository.SaveChangesAsync();
        return purchase;
    }

    public async Task<bool> DeletePurchaseAsync(Guid id)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(id);
        if (purchase is null)
            return false;

        var installments = await GetPurchaseInstallmentsAsync(id);
        if (installments.Any(item => item.IsPaid))
            return false;

        foreach (var installment in installments)
            _installmentRepository.Remove(installment);

        _purchaseRepository.Remove(purchase);
        await _purchaseRepository.SaveChangesAsync();
        return true;
    }

    public async Task<CreditCardInvoiceSummary?> GetInvoiceAsync(Guid id, int year, int month)
    {
        var card = await _cardRepository.GetByIdAsync(id);
        if (card is null || month is < 1 or > 12)
            return null;

        var installments = await _installmentRepository.GetByCardAndReferenceAsync(id, year, month);
        var reference = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);

        return new CreditCardInvoiceSummary
        {
            CreditCardId = card.Id,
            CreditCardName = card.Name,
            Year = year,
            Month = month,
            DueDate = CalculateDueDate(reference, card.ClosingDay, card.DueDay),
            TotalAmount = installments.Sum(item => item.Amount),
            PaidAmount = installments.Where(item => item.IsPaid).Sum(item => item.Amount),
            Items = installments.Select(item => new CreditCardInvoiceItem
            {
                InstallmentId = item.Id,
                PurchaseId = item.CreditCardPurchaseId,
                Description = item.CreditCardPurchase?.Description ?? string.Empty,
                PurchaseTotalAmount = item.CreditCardPurchase?.TotalAmount ?? item.Amount,
                PurchaseDate = item.CreditCardPurchase?.PurchaseDate ?? reference,
                InstallmentNumber = item.Number,
                InstallmentsCount = item.CreditCardPurchase?.InstallmentsCount ?? 0,
                Amount = item.Amount,
                IsPaid = item.IsPaid,
                CategoryId = item.CreditCardPurchase?.CategoryId
            }).ToList()
        };
    }

    public async Task<bool> MarkInstallmentPaidAsync(Guid id)
    {
        var installment = await _installmentRepository.GetByIdAsync(id);
        if (installment is null)
            return false;

        installment.IsPaid = true;
        _installmentRepository.Update(installment);
        await _installmentRepository.SaveChangesAsync();
        return true;
    }

    private static bool IsCardValid(string name, decimal limit, int closingDay, int dueDay) =>
        !string.IsNullOrWhiteSpace(name)
        && limit >= 0
        && closingDay is >= MinimumDay and <= MaximumBillingDay
        && dueDay is >= MinimumDay and <= MaximumBillingDay;

    private async Task<CreditCard?> GetValidCardForPurchaseAsync(
        Guid cardId,
        Guid? categoryId,
        string description,
        decimal totalAmount,
        int installmentsCount)
    {
        var card = await _cardRepository.GetByIdAsync(cardId);
        if (card is null
            || !card.IsActive
            || string.IsNullOrWhiteSpace(description)
            || totalAmount <= 0
            || installmentsCount is < 1 or > MaximumInstallments)
        {
            return null;
        }

        if (!categoryId.HasValue)
            return card;

        var category = await _categoryRepository.GetByIdAsync(categoryId.Value);
        return category is not null && category.Type == EntryType.Debit ? card : null;
    }

    private async Task<List<CreditCardInstallment>> GetPurchaseInstallmentsAsync(Guid purchaseId)
    {
        var installments = await _installmentRepository.GetAllAsync();
        return installments.Where(item => item.CreditCardPurchaseId == purchaseId).ToList();
    }

    private async Task AddInstallmentsAsync(CreditCardPurchase purchase, CreditCard card)
    {
        var baseAmount = Math.Floor((purchase.TotalAmount / purchase.InstallmentsCount) * 100m) / 100m;
        var firstReference = GetFirstReference(purchase.PurchaseDate, card.ClosingDay);

        for (var number = 1; number <= purchase.InstallmentsCount; number++)
        {
            var reference = firstReference.AddMonths(number - 1);
            var amount = number == purchase.InstallmentsCount
                ? purchase.TotalAmount - (baseAmount * (purchase.InstallmentsCount - 1))
                : baseAmount;

            await _installmentRepository.AddAsync(new CreditCardInstallment
            {
                CreditCardPurchaseId = purchase.Id,
                Number = number,
                Amount = amount,
                ReferenceDate = reference,
                DueDate = CalculateDueDate(reference, card.ClosingDay, card.DueDay)
            });
        }
    }

    private static DateTime ToUtc(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static DateTime GetFirstReference(DateTime purchaseDate, int closingDay)
    {
        var reference = new DateTime(
            purchaseDate.Year,
            purchaseDate.Month,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc);

        return purchaseDate.Day <= closingDay ? reference : reference.AddMonths(1);
    }

    private static DateTime CalculateDueDate(DateTime reference, int closingDay, int dueDay)
    {
        var dueMonth = dueDay > closingDay ? reference : reference.AddMonths(1);
        return new DateTime(dueMonth.Year, dueMonth.Month, dueDay, 0, 0, 0, DateTimeKind.Utc);
    }
}
