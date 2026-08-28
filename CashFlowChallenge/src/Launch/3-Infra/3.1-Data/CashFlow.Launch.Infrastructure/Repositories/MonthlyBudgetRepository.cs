using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Infrastructure.Context;
using CashFlow.Launch.Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Launch.Infrastructure.Repositories;

public class MonthlyBudgetRepository : BaseRepository<MonthlyBudget>, IMonthlyBudgetRepository
{
    private readonly CashFlowDbContext _context;

    public MonthlyBudgetRepository(CashFlowDbContext context) : base(context) => _context = context;

    public Task<MonthlyBudget?> GetByMonthAndCategoryAsync(int year, int month, Guid categoryId)
    {
        return _context.MonthlyBudgets
            .FirstOrDefaultAsync(x => x.Year == year && x.Month == month && x.CategoryId == categoryId);
    }

    public Task<List<MonthlyBudget>> GetByMonthAsync(int year, int month)
    {
        return _context.MonthlyBudgets
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x => x.Year == year && x.Month == month)
            .ToListAsync();
    }

    public Task<int> RemoveByYearAndCategoryAsync(int year, Guid categoryId)
    {
        return _context.MonthlyBudgets
            .Where(x => x.Year == year && x.CategoryId == categoryId)
            .ExecuteDeleteAsync();
    }

    public Task<int> RemoveByYearAsync(int year)
    {
        return _context.MonthlyBudgets
            .Where(x => x.Year == year)
            .ExecuteDeleteAsync();
    }
}
