using CashFlow.Launch.Domain.Entities.Base;
using CashFlow.Launch.Domain.Enums;

namespace CashFlow.Launch.Domain.Entities;

public class Category : Entity
{
    public string Name { get; set; } = string.Empty;
    public EntryType Type { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
