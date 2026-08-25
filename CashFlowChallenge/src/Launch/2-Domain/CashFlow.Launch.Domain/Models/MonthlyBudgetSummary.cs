namespace CashFlow.Launch.Domain.Models;

public class MonthlyBudgetSummary
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal PlannedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal RemainingAmount => PlannedAmount - ActualAmount;
    public bool IsOverBudget => ActualAmount > PlannedAmount;
    public List<MonthlyBudgetCategorySummary> Categories { get; set; } = [];
}

public class MonthlyBudgetCategorySummary
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal PlannedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal RemainingAmount => PlannedAmount - ActualAmount;
    public bool IsOverBudget => ActualAmount > PlannedAmount;
}
