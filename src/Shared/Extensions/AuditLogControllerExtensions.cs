using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.AuditLog.Dtos.Request;
using XerifeTv.CMS.Modules.AuditLog.Interfaces;

namespace XerifeTv.CMS.Shared.Extensions;

public static class AuditLogControllerExtensions
{
    public static async Task AddAuditLogAsync(
        this Controller controller,
        IAuditLogService auditLogService,
        string entityName,
        string entityId,
        string description,
        string? metadataJson = null)
    {
        var userName = controller.User.Identity?.Name ?? string.Empty;
        var userId = controller.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? userName;

        if (string.IsNullOrWhiteSpace(userName))
            return;

        await auditLogService.AddAsync(new AddAuditLogRequestDto
        {
            UserId = userId,
            UserName = userName,
            EntityName = entityName,
            EntityId = entityId,
            Description = description,
            MetadataJson = metadataJson
        });
    }
}
