using CashFlow.Launch.Domain.Entities.Base;

namespace CashFlow.Launch.Domain.Entities;

public class CreditCardInstallment : Entity
{
    public Guid CreditCardPurchaseId { get; set; }
    public CreditCardPurchase? CreditCardPurchase { get; set; }
    public int Number { get; set; }
    public decimal Amount { get; set; }
    public DateTime ReferenceDate { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsPaid { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
