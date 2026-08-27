using CashFlow.Launch.Domain.Interfaces;

namespace CashFlow.Worker;

public sealed class SystemTenantContext : ITenantContext
{
    public Guid GroupId => Guid.Empty;
    public bool HasGroup => false;
    public bool IsSystem => true;
}
