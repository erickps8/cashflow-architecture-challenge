using CashFlow.Launch.Domain.Enums;

namespace CashFlow.Launch.Api.Dtos.Requests;

public class CreateRecurringEntryRequest
{
    public decimal Amount { get; set; }
    public EntryType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? AccountId { get; set; }
    public Guid? CategoryId { get; set; }
    public RecurrenceFrequency Frequency { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime? EndAt { get; set; }
}
