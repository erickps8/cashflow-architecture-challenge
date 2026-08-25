using CashFlow.Launch.Domain.Enums;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Domain.Interfaces.Services;
using CashFlow.Launch.Domain.Models;

namespace CashFlow.Launch.Domain.Services;

public class MonthlyBalanceService : IMonthlyBalanceService
{
    private readonly IEntryRepository _entryRepository;
    private readonly ICreditCardInstallmentRepository _installmentRepository;
    private readonly IRecurringEntryRepository _recurringEntryRepository;

    public MonthlyBalanceService(IEntryRepository entryRepository, ICreditCardInstallmentRepository installmentRepository, IRecurringEntryRepository recurringEntryRepository)
    {
        _entryRepository = entryRepository;
        _installmentRepository = installmentRepository;
        _recurringEntryRepository = recurringEntryRepository;
    }

    public async Task<MonthlyBalanceSummary> GetMonthAsync(int year, int month, decimal openingBalance = 0)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        var entries = await _entryRepository.GetByPeriodAsync(start, end);
        var installments = await _installmentRepository.GetByReferenceAsync(year, month);
        var recurringEntries = await _recurringEntryRepository.GetAllAsync();

        decimal recurringIncome = 0;
        decimal recurringExpense = 0;

        foreach (var recurring in recurringEntries.Where(x => x.IsActive))
        {
            var occurrence = recurring.NextOccurrenceAt;
            while (occurrence < end)
            {
                if (recurring.EndAt.HasValue && occurrence > recurring.EndAt.Value) break;

                if (occurrence >= start)
                {
                    if (recurring.Type == EntryType.Credit) recurringIncome += recurring.Amount;
                    else if (recurring.Type == EntryType.Debit) recurringExpense += recurring.Amount;
                }

                occurrence = recurring.Frequency switch
                {
                    RecurrenceFrequency.Weekly => occurrence.AddDays(7),
                    RecurrenceFrequency.Yearly => occurrence.AddYears(1),
                    _ => occurrence.AddMonths(1)
                };
            }
        }

        return new MonthlyBalanceSummary
        {
            Year = year,
            Month = month,
            OpeningBalance = openingBalance,
            IncomeAmount = entries.Where(x => x.Type == EntryType.Credit).Sum(x => x.Amount),
            RecurringIncomeAmount = recurringIncome,
            DirectExpenseAmount = entries.Where(x => x.Type == EntryType.Debit).Sum(x => x.Amount),
            RecurringExpenseAmount = recurringExpense,
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
