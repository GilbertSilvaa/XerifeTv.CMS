using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;

public class UpdateBackgroundJobRequestDto
{
	public string Id { get; init; } = string.Empty;
	public EBackgroundJobStatus Status { get; set; }
	public int TotalRecordsToProcess { get; init; }
	public int TotalFailedRecords { get; init; }
	public int TotalSuccessfulRecords { get; init; }
	public int TotalProcessedRecords { get; init; }
	public ICollection<string> ErrorList { get; init; } = [];
}
