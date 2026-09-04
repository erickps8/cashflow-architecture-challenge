namespace CashFlow.Launch.Domain.Models;

public class MonthlyBalanceSummary
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal IncomeAmount { get; set; }
    public decimal RecurringIncomeAmount { get; set; }
    public decimal DirectExpenseAmount { get; set; }
    public decimal RecurringExpenseAmount { get; set; }
    public decimal CreditCardAmount { get; set; }
    public decimal PlannedExpenseAmount { get; set; }
    public decimal TotalIncomeAmount => IncomeAmount + RecurringIncomeAmount;
    public decimal TotalExpenseAmount => DirectExpenseAmount + RecurringExpenseAmount + CreditCardAmount + PlannedExpenseAmount;
    public decimal NetAmount => TotalIncomeAmount - TotalExpenseAmount;
    public decimal ClosingBalance => OpeningBalance + NetAmount;
    public bool IsNegative => ClosingBalance < 0;
}

public class BalanceProjectionSummary
{
    public decimal InitialBalance { get; set; }
    public decimal FinalBalance { get; set; }
    public bool HasNegativeMonth { get; set; }
    public decimal TotalIncomeAmount => Months.Sum(x => x.TotalIncomeAmount);
    public decimal TotalExpenseAmount => Months.Sum(x => x.TotalExpenseAmount);
    public decimal NetAmount => TotalIncomeAmount - TotalExpenseAmount;
    public List<MonthlyBalanceSummary> Months { get; set; } = [];
}
