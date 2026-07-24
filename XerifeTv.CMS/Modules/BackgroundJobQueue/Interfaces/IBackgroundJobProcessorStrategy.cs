using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Response;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;
using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;

public interface IBackgroundJobProcessorStrategy
{
    Task ProcessJobAsync(GetBackgroundJobResponseDto job);
    bool CanProcess(EBackgroundJobType jobType);
}