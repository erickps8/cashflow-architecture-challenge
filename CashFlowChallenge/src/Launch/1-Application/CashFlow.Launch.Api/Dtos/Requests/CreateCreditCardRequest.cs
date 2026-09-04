namespace CashFlow.Launch.Api.Dtos.Requests;

public class CreateCreditCardRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public int ClosingDay { get; set; }
    public int DueDay { get; set; }
}
