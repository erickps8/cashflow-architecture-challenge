namespace CashFlow.Launch.Api.Dtos.Requests;

public class SetMonthlyBudgetRequest
{
    public int Year { get; set; }
    public int Month { get; set; }
    public Guid CategoryId { get; set; }
    public decimal PlannedAmount { get; set; }
}
