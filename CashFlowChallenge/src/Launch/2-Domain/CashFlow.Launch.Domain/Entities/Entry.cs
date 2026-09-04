using CashFlow.Launch.Domain.Entities.Base;
using CashFlow.Launch.Domain.Enums;

namespace CashFlow.Launch.Domain.Entities;

public class Entry : Entity
{
    public decimal Amount { get; set; }
    public EntryType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Personal finance fields are optional to preserve compatibility with existing entries.
    public Guid? AccountId { get; set; }
    public Account? Account { get; set; }
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public bool IsRecurring { get; set; }

    // When a generated recurrence is postponed, keep its first due date for history/audit.
    public DateTime? OriginalOccurredAt { get; set; }
}
