using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Dashboard.Dtos.Response;

namespace XerifeTv.CMS.Modules.Dashboard.Interfaces;

public interface IDashboardService
{
    Task<Result<GetDashboardDataRequestDto>> Get();
}
