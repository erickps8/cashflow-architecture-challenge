using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Models;

namespace CashFlow.Launch.Domain.Interfaces.Services;

public interface IMonthlyBudgetService
{
    Task<MonthlyBudget?> SetAsync(int year, int month, Guid categoryId, decimal plannedAmount);
    Task<MonthlyBudgetSummary?> GetSummaryAsync(int year, int month);
}
