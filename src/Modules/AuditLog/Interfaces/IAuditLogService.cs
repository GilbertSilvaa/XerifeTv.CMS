using XerifeTv.CMS.Modules.AuditLog.Dtos.Request;
using XerifeTv.CMS.Modules.AuditLog.Dtos.Response;
using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Modules.AuditLog.Interfaces;

public interface IAuditLogService
{
    Task<Result<PagedList<GetAuditLogResponseDto>>> GetAsync(int currentPage, int limit);
    Task<Result<PagedList<GetAuditLogResponseDto>>> GetAsync(string userId, int currentPage, int limit);
    Task<Result<bool>> AddAsync(AddAuditLogRequestDto dto);
}
