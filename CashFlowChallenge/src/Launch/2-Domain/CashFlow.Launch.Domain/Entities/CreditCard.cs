using CashFlow.Launch.Domain.Entities.Base;

namespace CashFlow.Launch.Domain.Entities;

public class CreditCard : Entity
{
    public string Name { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public int ClosingDay { get; set; }
    public int DueDay { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
