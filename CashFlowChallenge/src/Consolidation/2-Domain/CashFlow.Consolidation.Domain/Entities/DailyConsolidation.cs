using CashFlow.Consolidation.Domain.Entities.Base;

namespace CashFlow.Consolidation.Domain.Entities;

public class DailyConsolidation : Entity
{
    public DateTime Date { get; set; }

    public decimal TotalCredits { get; set; }

    public decimal TotalDebits { get; set; }

    public decimal Balance { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}