using CashFlow.Launch.Domain.Interfaces;

namespace CashFlow.Launch.Api;

public sealed class HttpTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _http;

    public HttpTenantContext(IHttpContextAccessor http) => _http = http;

    public Guid GroupId => Guid.TryParse(
        _http.HttpContext?.User.FindFirst("group_id")?.Value,
        out var id) ? id : Guid.Empty;

    public bool HasGroup => GroupId != Guid.Empty;
    public bool IsSystem => false;
}
