using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.ContentCalendar.Entities;
using XerifeTv.CMS.Modules.ContentCalendar.Enums;

namespace XerifeTv.CMS.Modules.ContentCalendar.Interfaces;

public interface IContentScheduleRepository : IBaseRepository<ContentScheduleEntity>
{
    Task<IReadOnlyCollection<ContentScheduleEntity>> GetByPeriodAsync(DateOnly start, DateOnly end);
    Task<IReadOnlyCollection<ContentScheduleEntity>> GetDueUnnotifiedAsync(DateOnly today, int limit);
    Task<IReadOnlyCollection<ContentScheduleEntity>> GetPendingAsync(DateOnly today, int limit);
    Task<ContentScheduleEntity?> GetByExternalKeyAsync(
        EContentScheduleType contentType, int tmdbId, int? seasonNumber, int? episodeNumber);
    Task<IReadOnlyCollection<ContentScheduleEntity>> GetByImdbIdAsync(string imdbId, EContentScheduleType contentType);
    Task<ContentScheduleEntity?> GetEpisodeByImdbIdAsync(string imdbId, int seasonNumber, int episodeNumber);
    Task<ContentScheduleEntity?> GetByRegisteredContentIdAsync(string registeredContentId);
    Task<bool> ExistsByExternalKeyAsync(
        EContentScheduleType contentType, int tmdbId, int? seasonNumber, int? episodeNumber);
    Task<bool> UpdateScheduledDateAsync(string id, DateOnly scheduledDate);
    Task<ContentScheduleEntity?> TryClaimAsync(string id, TimeSpan lease);
    Task MarkProcessedAsync(string id);
    Task ReleaseClaimAsync(string id);
    Task ReleaseStaleClaimsAsync(TimeSpan lease);
}
