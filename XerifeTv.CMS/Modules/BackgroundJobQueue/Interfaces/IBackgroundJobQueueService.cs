using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Response;
using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;

public interface IBackgroundJobQueueService
{
    public Task<Result<AddJobQueueResponseDto>> AddJobInQueue(AddSpreadsheetJobQueueRequestDto dto);
    public Task<Result<AddJobQueueResponseDto>> AddJobInQueue(AddImportEpisodesJobQueueRequestDto dto);
}