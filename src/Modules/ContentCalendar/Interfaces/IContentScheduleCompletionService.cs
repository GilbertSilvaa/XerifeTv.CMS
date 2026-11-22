namespace XerifeTv.CMS.Modules.ContentCalendar.Interfaces;

public interface IContentScheduleCompletionService
{
    Task CompleteByMovieAsync(string imdbId, string movieId, bool disabled);
    Task CompleteBySeriesAsync(string imdbId, string seriesId, bool disabled);
    Task CompleteByEpisodeAsync(
        string seriesImdbId, int seasonNumber, int episodeNumber, string episodeId, string seriesId, bool disabled);
}
