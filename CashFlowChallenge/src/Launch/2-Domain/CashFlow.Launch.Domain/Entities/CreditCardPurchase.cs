using CashFlow.Launch.Domain.Entities.Base;

namespace CashFlow.Launch.Domain.Entities;

public class CreditCardPurchase : Entity
{
    public Guid CreditCardId { get; set; }
    public CreditCard? CreditCard { get; set; }
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int InstallmentsCount { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
