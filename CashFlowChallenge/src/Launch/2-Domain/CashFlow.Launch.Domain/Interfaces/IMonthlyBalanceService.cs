using CashFlow.Launch.Domain.Models;

namespace CashFlow.Launch.Domain.Interfaces.Services;

public interface IMonthlyBalanceService
{
    Task<MonthlyBalanceSummary> GetMonthAsync(int year, int month, decimal openingBalance = 0);
    Task<BalanceProjectionSummary> GetProjectionAsync(int startYear, int startMonth, int months, decimal initialBalance = 0);
    Task<BalanceProjectionSummary> GetPlannedProjectionAsync(int startYear, int startMonth, int months, decimal initialBalance = 0);
}
