namespace CashFlow.Launch.Api.Dtos.Requests;

public class CreateCreditCardPurchaseRequest
{
    public Guid CreditCardId { get; set; }
    public Guid? CategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int InstallmentsCount { get; set; } = 1;
    public DateTime PurchaseDate { get; set; }
}
