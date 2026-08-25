using CashFlow.Launch.Domain.Enums;

namespace CashFlow.Launch.Api.Dtos.Requests;

public class CreateAccountRequest
{
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal InitialBalance { get; set; }
}
