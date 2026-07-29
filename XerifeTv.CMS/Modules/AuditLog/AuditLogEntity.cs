using XerifeTv.CMS.Modules.Abstractions.Entities;

namespace XerifeTv.CMS.Modules.AuditLog;

public sealed class AuditLogEntity : BaseEntity
{
    public string UserId { get; init; } = Guid.NewGuid().ToString();
    public string UserName { get; init; } = null!;
    public string EntityName { get; init; } = null!;
    public string EntityId { get; init; } = null!;
    public string Description { get; init; } = null!;
    public string? MetadataJson { get; init; }
}