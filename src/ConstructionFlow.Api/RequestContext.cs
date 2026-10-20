using ConstructionFlow.Application;

namespace ConstructionFlow.Api;

public sealed class RequestContext(IHttpContextAccessor accessor) : IAuthenticatedRequestContext
{
    public Guid TenantId => Guid.TryParse(accessor.HttpContext?.User.FindFirst("tenant_id")?.Value, out var id) ? id : Guid.Empty;
    public string Subject => accessor.HttpContext?.User.FindFirst("sub")?.Value ?? "";
    public Guid UserId { get; internal set; }
    public void SetUserId(Guid userId) => UserId = userId;
}
