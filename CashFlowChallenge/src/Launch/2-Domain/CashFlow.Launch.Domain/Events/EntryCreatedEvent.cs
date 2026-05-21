namespace CashFlow.Launch.Domain.Events;

public class EntryCreatedEvent
{
    public Guid EntryId { get; set; }

    public decimal Amount { get; set; }

    public int Type { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }
}