namespace CashFlow.Launch.Domain.Models;

public class MonthlyBalanceSummary
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal IncomeAmount { get; set; }
    public decimal DirectExpenseAmount { get; set; }
    public decimal CreditCardAmount { get; set; }
    public decimal TotalExpenseAmount => DirectExpenseAmount + CreditCardAmount;
    public decimal NetAmount => IncomeAmount - TotalExpenseAmount;
    public decimal ClosingBalance => OpeningBalance + NetAmount;
    public bool IsNegative => ClosingBalance < 0;
}

public class BalanceProjectionSummary
{
    public decimal InitialBalance { get; set; }
    public decimal FinalBalance { get; set; }
    public bool HasNegativeMonth { get; set; }
    public List<MonthlyBalanceSummary> Months { get; set; } = [];
}
