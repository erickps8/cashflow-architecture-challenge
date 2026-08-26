namespace CashFlow.Launch.Domain.Interfaces;
public interface ITenantContext { Guid GroupId { get; } bool HasGroup { get; } }