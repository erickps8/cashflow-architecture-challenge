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
}