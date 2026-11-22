using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;
using XerifeTv.CMS.Shared.Extensions;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Response;

public class GetJobsToNotifyResponseDto
{
    public string JobId { get; init; } = string.Empty;
    public string JobName { get; init; } = string.Empty;
    public EBackgroundJobStatus JobStatus { get; init; }
    public string JobTimeAgo { get; init; } = "Agora mesmo";


    public static GetJobsToNotifyResponseDto FromEntity(BackgroundJobEntity entity)
    {
        return new GetJobsToNotifyResponseDto
        {
            JobId = entity.Id,
            JobName = entity.JobName,
            JobStatus = entity.Status,
            JobTimeAgo = (entity.FinishedAt ?? DateTime.UtcNow).ToRelativeString()
        };
    }
}
