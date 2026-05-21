using CashFlow.Launch.Domain.Enums;

namespace CashFlow.Launch.Api.Dtos.Requests;

public class CreateEntryRequest
{
    public decimal Amount { get; set; }

    public EntryType Type { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }
}