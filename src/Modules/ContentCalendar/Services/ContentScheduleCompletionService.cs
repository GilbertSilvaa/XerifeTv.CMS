using XerifeTv.CMS.Modules.ContentCalendar.Enums;
using XerifeTv.CMS.Modules.ContentCalendar.Interfaces;

namespace XerifeTv.CMS.Modules.ContentCalendar.Services;

public sealed class ContentScheduleCompletionService(
    IContentScheduleRepository repository,
    ILogger<ContentScheduleCompletionService> logger) : IContentScheduleCompletionService
{
    public async Task CompleteByMovieAsync(string imdbId, string movieId, bool disabled)
    {
        if (disabled || string.IsNullOrWhiteSpace(imdbId)) return;

        await CompleteAsync(
            () => repository.GetByImdbIdAsync(imdbId, EContentScheduleType.MOVIE),
            movieId,
            $"movie {movieId} / IMDb {imdbId}");
    }

    public async Task CompleteBySeriesAsync(string imdbId, string seriesId, bool disabled)
    {
        if (disabled || string.IsNullOrWhiteSpace(imdbId)) return;

        await CompleteAsync(
            () => repository.GetByImdbIdAsync(imdbId, EContentScheduleType.SERIES),
            seriesId,
            $"series {seriesId} / IMDb {imdbId}");
    }

    public async Task CompleteByEpisodeAsync(
        string seriesImdbId,
        int seasonNumber,
        int episodeNumber,
        string episodeId,
        string seriesId,
        bool disabled)
    {
        if (disabled || string.IsNullOrWhiteSpace(seriesImdbId)) return;

        try
        {
            var schedule = await repository.GetEpisodeByImdbIdAsync(seriesImdbId, seasonNumber, episodeNumber);

            if (schedule is null || schedule.Status == EScheduleStatus.REGISTERED) return;

            schedule.Complete(episodeId, DateTime.UtcNow);
            await repository.UpdateAsync(schedule);

            logger.LogInformation(
                "Content schedule {ScheduleId} completed by episode {EpisodeId} of series {SeriesId}",
                schedule.Id, episodeId, seriesId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to complete content schedule for episode S{Season}E{Episode} of series {SeriesId}",
                seasonNumber, episodeNumber, seriesId);
        }
    }

    private async Task CompleteAsync(
        Func<Task<IReadOnlyCollection<Entities.ContentScheduleEntity>>> lookup,
        string contentId,
        string description)
    {
        try
        {
            var schedules = await lookup();

            foreach (var schedule in schedules.Where(s => s.Status != EScheduleStatus.REGISTERED))
            {
                schedule.Complete(contentId, DateTime.UtcNow);
                await repository.UpdateAsync(schedule);

                logger.LogInformation(
                    "Content schedule {ScheduleId} completed by {Description}",
                    schedule.Id, description);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to complete content schedule for {Description}", description);
        }
    }
}
