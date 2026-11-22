namespace XerifeTv.CMS.Modules.AuditLog.Dtos.Request;

public sealed class AddAuditLogRequestDto
{
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string EntityName { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? MetadataJson { get; init; }

    public AuditLogEntity ToEntity()
    {
        return new AuditLogEntity
        {
            UserId = UserId,
            UserName = UserName,
            EntityId = EntityId,
            EntityName = EntityName,
            Description = Description,
            MetadataJson = MetadataJson
        };
    }
}
