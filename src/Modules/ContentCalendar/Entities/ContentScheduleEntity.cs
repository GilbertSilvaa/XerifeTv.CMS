using MongoDB.Bson.Serialization.Attributes;
using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.ContentCalendar.Enums;

namespace XerifeTv.CMS.Modules.ContentCalendar.Entities;

public sealed class ContentScheduleEntity : BaseEntity
{
    public EContentScheduleType ContentType { get; set; }
    public int TmdbId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    [BsonSerializer(typeof(DateOnlyBsonSerializer))]
    public DateOnly ScheduledDate { get; set; }

    public EScheduleOrigin Origin { get; set; } = EScheduleOrigin.MANUAL;
    public EScheduleStatus Status { get; set; } = EScheduleStatus.SCHEDULED;
    public int? SeasonNumber { get; set; }
    public int? EpisodeNumber { get; set; }
    public string? ImdbId { get; set; }
    public string? RegisteredContentId { get; set; }
    public DateTime? RegisteredAt { get; set; }
    public DateTime? NotificationProcessedAt { get; set; }
    public DateTime? NotificationClaimedAt { get; set; }
    public int NotificationAttempts { get; set; }

    public string GetExternalKey()
        => ContentType == EContentScheduleType.EPISODE
            ? $"{ContentType}:{TmdbId}:{SeasonNumber}:{EpisodeNumber}"
            : $"{ContentType}:{TmdbId}";

    public void Complete(string registeredContentId, DateTime registeredAt)
    {
        Status = EScheduleStatus.REGISTERED;
        RegisteredContentId = registeredContentId;
        RegisteredAt = registeredAt;
        NotificationClaimedAt = null;
    }
}
