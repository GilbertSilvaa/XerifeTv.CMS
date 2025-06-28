using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Response;

public class GetBackgroundJobResponseDto
{
	public string Id { get; private set; } = string.Empty;
	public string JobName { get; private set; } = string.Empty;
	public EBackgroundJobType Type { get; private set; }
	public EBackgroundJobStatus Status { get; private set; }
	public int TotalRecordsToProcess { get; private set; }
	public int TotalFailedRecords { get; private set; }
	public int TotalSuccessfulRecords { get; private set; }
	public int TotalProcessedRecords { get; private set; }
	public DateTime? ProcessedAt { get; private set; }
	public ICollection<string> ErrorList { get; private set; } = [];
	public DateTime CreateAt { get; private set; }
	public DateTime? LastUpdatDate { get; private set; }
	public string? SpreadsheetFileUrl { get; private set; } = null;
	public string? SeriesIdImportEpisodes { get; private set; } = null;

	public static GetBackgroundJobResponseDto FromEntity(BackgroundJobEntity entity)
	{
		return new GetBackgroundJobResponseDto
		{
			Id = entity.Id,
			JobName = entity.JobName,
			Type = entity.Type,
			Status = entity.Status,
			TotalRecordsToProcess = entity.TotalRecordsToProcess,
			TotalFailedRecords = entity.TotalFailedRecords,
			TotalSuccessfulRecords = entity.TotalSuccessfulRecords,
			TotalProcessedRecords = entity.TotalProcessedRecords,
			ProcessedAt = entity.ProcessedAt,
			ErrorList = entity.ErrorList,
			CreateAt = entity.CreateAt,
			LastUpdatDate = entity.UpdateAt,
			SeriesIdImportEpisodes = entity.SeriesIdImportEpisodes,
			SpreadsheetFileUrl = entity.SpreadsheetFileUrl,
		};
	}
}