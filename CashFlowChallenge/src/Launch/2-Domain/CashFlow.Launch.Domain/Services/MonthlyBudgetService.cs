using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Enums;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Domain.Interfaces.Services;
using CashFlow.Launch.Domain.Models;

namespace CashFlow.Launch.Domain.Services;

public class MonthlyBudgetService : IMonthlyBudgetService
{
    private readonly IMonthlyBudgetRepository _budgetRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IEntryRepository _entryRepository;
    private readonly ICreditCardInstallmentRepository _installmentRepository;

    public MonthlyBudgetService(IMonthlyBudgetRepository budgetRepository, ICategoryRepository categoryRepository, IEntryRepository entryRepository, ICreditCardInstallmentRepository installmentRepository)
    {
        _budgetRepository = budgetRepository;
        _categoryRepository = categoryRepository;
        _entryRepository = entryRepository;
        _installmentRepository = installmentRepository;
    }

    public async Task<MonthlyBudget?> SetAsync(int year, int month, Guid categoryId, decimal plannedAmount)
    {
        if (year < 2000 || month is < 1 or > 12 || plannedAmount < 0) return null;

        var category = await _categoryRepository.GetByIdAsync(categoryId);
        if (category is null || category.Type != EntryType.Debit) return null;

        var budget = await _budgetRepository.GetByMonthAndCategoryAsync(year, month, categoryId);
        if (budget is null)
        {
            budget = new MonthlyBudget
            {
                Year = year,
                Month = month,
                CategoryId = categoryId,
                PlannedAmount = plannedAmount
            };
            await _budgetRepository.AddAsync(budget);
        }
        else
        {
            budget.PlannedAmount = plannedAmount;
            budget.UpdatedAt = DateTime.UtcNow;
        }

        await _budgetRepository.SaveChangesAsync();
        return budget;
    }

    public async Task<MonthlyBudgetSummary?> GetSummaryAsync(int year, int month)
    {
        if (year < 2000 || month is < 1 or > 12) return null;

        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var budgets = await _budgetRepository.GetByMonthAsync(year, month);
        var entries = await _entryRepository.GetByPeriodAsync(start, end);
        var installments = await _installmentRepository.GetByReferenceAsync(year, month);
        var categories = (await _categoryRepository.GetAllAsync())
            .Where(x => x.Type == EntryType.Debit)
            .ToList();

        var directByCategory = entries
            .Where(x => x.Type == EntryType.Debit && x.CategoryId.HasValue)
            .GroupBy(x => x.CategoryId!.Value)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.Amount));

        var cardByCategory = installments
            .Where(x => x.CreditCardPurchase?.CategoryId != null)
            .GroupBy(x => x.CreditCardPurchase!.CategoryId!.Value)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.Amount));

        var budgetByCategory = budgets.ToDictionary(x => x.CategoryId, x => x.PlannedAmount);
        var categoryIds = budgetByCategory.Keys
            .Union(directByCategory.Keys)
            .Union(cardByCategory.Keys)
            .Distinct()
            .ToList();

        var items = categoryIds.Select(categoryId =>
        {
            var category = categories.FirstOrDefault(x => x.Id == categoryId);
            var direct = directByCategory.GetValueOrDefault(categoryId);
            var card = cardByCategory.GetValueOrDefault(categoryId);
            return new MonthlyBudgetCategorySummary
            {
                CategoryId = categoryId,
                CategoryName = category?.Name ?? "Categoria não encontrada",
                PlannedAmount = budgetByCategory.GetValueOrDefault(categoryId),
                ActualAmount = direct + card
            };
        })
        .OrderBy(x => x.CategoryName)
        .ToList();

        return new MonthlyBudgetSummary
        {
            Year = year,
            Month = month,
            PlannedAmount = items.Sum(x => x.PlannedAmount),
            ActualAmount = items.Sum(x => x.ActualAmount),
            Categories = items
        };
    }
}
