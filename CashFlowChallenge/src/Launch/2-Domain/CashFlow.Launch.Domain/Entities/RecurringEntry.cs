using CashFlow.Launch.Domain.Entities.Base;
using CashFlow.Launch.Domain.Enums;

namespace CashFlow.Launch.Domain.Entities;

public class RecurringEntry : Entity
{
    public decimal Amount { get; set; }
    public EntryType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? AccountId { get; set; }
    public Guid? CategoryId { get; set; }
    public RecurrenceFrequency Frequency { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public DateTime NextOccurrenceAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
