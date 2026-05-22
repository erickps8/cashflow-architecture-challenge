using CashFlow.Launch.Domain.Entities.Base;

namespace CashFlow.Launch.Domain.Entities;

public class OutboxMessage : Entity
{
    public string Type { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    public int RetryCount { get; set; }

    public string? Error { get; set; }
}