using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Modules.AuditLog.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLogEntity entity);
    Task<PagedList<AuditLogEntity>> GetAsync(int currentPage, int limit);
    Task<PagedList<AuditLogEntity>> GetAsync(string userId, int currentPage, int limit);
}
