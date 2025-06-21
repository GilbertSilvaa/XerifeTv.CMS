using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue;

public class BackgroundJobEntity : BaseEntity
{
    public string JobName { get; set; } = string.Empty;
    public EBackgroundJobType Type { get; set; }
    public EBackgroundJobStatus Status { get; set; }
    public string RequestedByUserId { get; set; } = string.Empty;
    public int TotalRecordsToProcess { get; set; }
    public int TotalFailedRecords { get; set; }
    public int TotalSuccessfulRecords { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public ICollection<string> ErrorList { get; set; } = [];
    public string? SpreadsheetFileUrl = null;
    public string? SeriesIdImportEpisodes = null;
}