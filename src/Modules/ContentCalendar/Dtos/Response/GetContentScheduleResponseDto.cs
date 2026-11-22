using XerifeTv.CMS.Modules.ContentCalendar.Entities;
using XerifeTv.CMS.Modules.ContentCalendar.Enums;

namespace XerifeTv.CMS.Modules.ContentCalendar.Dtos.Response;

public class GetContentScheduleResponseDto
{
    public string Id { get; init; } = string.Empty;
    public EContentScheduleType ContentType { get; init; }
    public int TmdbId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public DateOnly ScheduledDate { get; init; }
    public EScheduleOrigin Origin { get; init; }

    /// <summary>Effective status derived from the persisted status and the business date.</summary>
    public EScheduleStatus Status { get; init; }

    public EScheduleStatus PersistedStatus { get; init; }
    public int? SeasonNumber { get; init; }
    public int? EpisodeNumber { get; init; }
    public string? ImdbId { get; init; }
    public string? RegisteredContentId { get; init; }
    public DateTime? RegisteredAt { get; init; }
    public DateTime CreateAt { get; init; }
    public int DaysLate { get; init; }

    public bool IsRegistered => Status == EScheduleStatus.REGISTERED;
    public bool CanEdit => !IsRegistered;
    public bool IsEpisode => ContentType == EContentScheduleType.EPISODE;

    public static GetContentScheduleResponseDto FromEntity(ContentScheduleEntity entity, DateOnly today)
    {
        var effectiveStatus = ResolveStatus(entity, today);

        return new GetContentScheduleResponseDto
        {
            Id = entity.Id,
            ContentType = entity.ContentType,
            TmdbId = entity.TmdbId,
            Title = entity.Title,
            ImageUrl = entity.ImageUrl,
            ScheduledDate = entity.ScheduledDate,
            Origin = entity.Origin,
            Status = effectiveStatus,
            PersistedStatus = entity.Status,
            SeasonNumber = entity.SeasonNumber,
            EpisodeNumber = entity.EpisodeNumber,
            ImdbId = entity.ImdbId,
            RegisteredContentId = entity.RegisteredContentId,
            RegisteredAt = entity.RegisteredAt,
            CreateAt = entity.CreateAt,
            DaysLate = effectiveStatus == EScheduleStatus.PENDING
                ? Math.Max(0, today.DayNumber - entity.ScheduledDate.DayNumber)
                : 0
        };
    }

    public static EScheduleStatus ResolveStatus(ContentScheduleEntity entity, DateOnly today)
    {
        if (entity.Status == EScheduleStatus.REGISTERED || entity.RegisteredContentId is not null)
            return EScheduleStatus.REGISTERED;

        return entity.ScheduledDate <= today
            ? EScheduleStatus.PENDING
            : EScheduleStatus.SCHEDULED;
    }
}
