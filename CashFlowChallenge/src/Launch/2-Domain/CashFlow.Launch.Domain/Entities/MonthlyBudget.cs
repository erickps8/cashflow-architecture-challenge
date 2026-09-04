using CashFlow.Launch.Domain.Entities.Base;

namespace CashFlow.Launch.Domain.Entities;

public class MonthlyBudget : Entity
{
    public int Year { get; set; }
    public int Month { get; set; }
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public decimal PlannedAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
