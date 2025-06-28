using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;
using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;

public interface IBackgroundJobQueueRepository : IBaseRepository<BackgroundJobEntity>
{
	Task<PagedList<BackgroundJobEntity>> GetByFilterAsync(GetBackgroundJobsByFilterRequestDto dto);
}