using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Response;
using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;

public interface IBackgroundJobQueueService
{
    Task<Result<AddJobQueueResponseDto>> AddJobInQueue(AddSpreadsheetJobQueueRequestDto dto);
    Task<Result<AddJobQueueResponseDto>> AddJobInQueue(AddImportEpisodesJobQueueRequestDto dto);
    Task<Result<PagedList<GetBackgroundJobResponseDto>>> GetByFilter(GetBackgroundJobsByFilterRequestDto dto);
    Task<Result<string>> Update(UpdateBackgroundJobRequestDto dto);
	Task<Result<bool>> Delete(string id);
}