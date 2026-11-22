namespace XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;

public record CancelJobRequestDto(string JobId, string JobName);