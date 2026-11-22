namespace XerifeTv.CMS.Modules.ContentCalendar.Dtos.Request;

public class LinkContentScheduleRequestDto
{
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Internal content id. For episodes this is the episode id and <see cref="SeriesId"/>
    /// must also be informed.
    /// </summary>
    public string ContentId { get; init; } = string.Empty;

    /// <summary>Internal series id, required when linking an episode schedule.</summary>
    public string? SeriesId { get; init; }
}
