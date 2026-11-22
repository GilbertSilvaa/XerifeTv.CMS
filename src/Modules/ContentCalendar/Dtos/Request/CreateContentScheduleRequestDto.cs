using XerifeTv.CMS.Modules.ContentCalendar.Enums;

namespace XerifeTv.CMS.Modules.ContentCalendar.Dtos.Request;

public class CreateContentScheduleRequestDto
{
    public EContentScheduleType ContentType { get; init; }
    public int TmdbId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public DateOnly ScheduledDate { get; init; }
    public int? SeasonNumber { get; init; }
    public int? EpisodeNumber { get; init; }
}
