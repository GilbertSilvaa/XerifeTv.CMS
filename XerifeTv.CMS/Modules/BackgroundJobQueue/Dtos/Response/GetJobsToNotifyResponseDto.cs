using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;

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
			JobTimeAgo = GetTimeAgo(entity.FinishedAt ?? DateTime.UtcNow)
		};
	}

	private static string GetTimeAgo(DateTime finishedAt)
	{
		var ts = DateTime.UtcNow - finishedAt;

		if (ts.TotalSeconds < 10)
			return "Agora mesmo";
		if (ts.TotalSeconds < 60)
			return $"há {Math.Floor(ts.TotalSeconds)}s";
		if (ts.TotalMinutes < 60)
			return $"há {Math.Floor(ts.TotalMinutes)}min";
		if (ts.TotalHours < 24)
			return $"há {Math.Floor(ts.TotalHours)}h";
		if (ts.TotalDays < 30)
			return $"há {Math.Floor(ts.TotalDays)} dia{(ts.TotalDays < 2 ? "" : "s")}";

		return finishedAt.ToLocalTime().ToString("dd/MM/yyyy");
	}

}
