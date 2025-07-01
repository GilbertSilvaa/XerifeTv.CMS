using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Response;
using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;

public interface IBackgroundJobQueueService
{
    Task<Result<AddJobQueueResponseDto>> AddJobInQueueAsync(AddSpreadsheetJobQueueRequestDto dto);
    Task<Result<AddJobQueueResponseDto>> AddJobInQueueAsync(AddImportEpisodesJobQueueRequestDto dto);
    Task<Result<PagedList<GetBackgroundJobResponseDto>>> GetByFilterAsync(GetBackgroundJobsByFilterRequestDto dto);
    Task<Result<IEnumerable<GetJobsToNotifyResponseDto>>> GetJobsToNotifyAsync(string username);
	Task<Result<string>> UpdateAsync(UpdateBackgroundJobRequestDto dto);
	Task<Result<bool>> DeleteAsync(string id);
}