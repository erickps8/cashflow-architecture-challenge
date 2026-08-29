using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Interfaces.Base;

namespace CashFlow.Launch.Domain.Interfaces;

public interface IMonthlyBudgetRepository : IBaseRepository<MonthlyBudget>
{
    Task<MonthlyBudget?> GetByMonthAndCategoryAsync(int year, int month, Guid categoryId);
    Task<List<MonthlyBudget>> GetByMonthAsync(int year, int month);
    Task<int> RemoveByYearAndCategoryAsync(int year, Guid categoryId);
    Task<int> RemoveByYearAsync(int year);
}
