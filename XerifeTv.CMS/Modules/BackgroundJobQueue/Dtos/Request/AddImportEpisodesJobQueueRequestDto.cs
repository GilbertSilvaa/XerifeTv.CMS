namespace XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;

public class AddImportEpisodesJobQueueRequestDto
{
    public string RequestedByUsername { get; set; } = string.Empty;
    public string SeriesId { get; init; } = string.Empty;
}