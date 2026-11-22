namespace XerifeTv.CMS.Modules.ContentCalendar.Dtos.Request;

public class AutomaticEpisodeScheduleRequestDto
{
    public int SeriesTmdbId { get; init; }
    public string? SeriesImdbId { get; init; }
    public string SeriesTitle { get; init; } = string.Empty;
    public int SeasonNumber { get; init; }
    public int EpisodeNumber { get; init; }
    public string EpisodeTitle { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public DateOnly AirDate { get; init; }

    /// <summary>
    /// When false, an existing schedule is still updated but a new one is not created.
    /// Used when the episode was already registered before the import run.
    /// </summary>
    public bool AllowCreate { get; init; } = true;
}
