using CashFlow.Launch.Domain.Enums;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Domain.Interfaces.Services;
using CashFlow.Launch.Domain.Models;

namespace CashFlow.Launch.Domain.Services;

public class MonthlyBalanceService : IMonthlyBalanceService
{
    private readonly IEntryRepository _entryRepository;
    private readonly ICreditCardInstallmentRepository _installmentRepository;

    public MonthlyBalanceService(IEntryRepository entryRepository, ICreditCardInstallmentRepository installmentRepository)
    {
        _entryRepository = entryRepository;
        _installmentRepository = installmentRepository;
    }

    public async Task<MonthlyBalanceSummary> GetMonthAsync(int year, int month, decimal openingBalance = 0)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        var entries = await _entryRepository.GetByPeriodAsync(start, end);
        var installments = await _installmentRepository.GetByReferenceAsync(year, month);

        return new MonthlyBalanceSummary
        {
            Year = year,
            Month = month,
            OpeningBalance = openingBalance,
            IncomeAmount = entries.Where(x => x.Type == EntryType.Credit).Sum(x => x.Amount),
            DirectExpenseAmount = entries.Where(x => x.Type == EntryType.Debit).Sum(x => x.Amount),
            CreditCardAmount = installments.Sum(x => x.Amount)
        };
    }

    public async Task<BalanceProjectionSummary> GetProjectionAsync(int startYear, int startMonth, int months, decimal initialBalance = 0)
    {
        if (months < 1 || months > 60) return new BalanceProjectionSummary { InitialBalance = initialBalance, FinalBalance = initialBalance };

        var result = new BalanceProjectionSummary { InitialBalance = initialBalance };
        var current = new DateTime(startYear, startMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var balance = initialBalance;

        for (var i = 0; i < months; i++)
        {
            var month = await GetMonthAsync(current.Year, current.Month, balance);
            result.Months.Add(month);
            balance = month.ClosingBalance;
            if (month.IsNegative) result.HasNegativeMonth = true;
            current = current.AddMonths(1);
        }

        result.FinalBalance = balance;
        return result;
    }
}
