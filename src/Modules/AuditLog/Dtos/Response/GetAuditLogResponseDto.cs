namespace XerifeTv.CMS.Modules.AuditLog.Dtos.Response;

public sealed class GetAuditLogResponseDto
{
    public string AuditLogId { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public string UserName { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? MetadataJson { get; private set; }
    public DateTime CreateAt { get; private set; }

    public static GetAuditLogResponseDto FromEntity(AuditLogEntity entity)
    {
        return new GetAuditLogResponseDto
        {
            AuditLogId = entity.Id,
            UserId = entity.UserId,
            UserName = entity.UserName,
            EntityName = entity.EntityName,
            EntityId = entity.EntityId,
            Description = entity.Description,
            MetadataJson = entity.MetadataJson,
            CreateAt = entity.CreateAt
        };
    }
}
