using XerifeTv.CMS.Modules.Series;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Modules.Content.Dtos.Response;

public class EpisodeContentV2ResponseDto
{
    public string Id { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string BannerURL { get; private set; } = string.Empty;
    public string Duration { get; private set; } = string.Empty;
    public long DurationSeconds { get; private set; }
    public string SubtitleURL { get; private set; } = string.Empty;
    public string VideoResolverURL { get; private set; } = string.Empty;

    public static EpisodeContentV2ResponseDto FromEntity(Episode entity)
    {
        string videoResolverPath = !string.IsNullOrWhiteSpace(entity.MediaDeliveryProfileId)
            ? $"/ResolveUrl?mediaDeliveryProfileId={entity.MediaDeliveryProfileId}&mediaPath={Uri.EscapeDataString(entity.MediaRoute ?? "")}&isCached=true"
            : $"/ResolveUrlFixed?urlFixed={Uri.EscapeDataString(entity.Video?.Url ?? "")}&streamFormat={entity.Video?.StreamFormat}&isCached=true";
        
        return new()
        {
            Id = entity.Id,
            Title = entity.Title,
            BannerURL = entity.BannerUrl,
            Duration = DateTimeHelper.ConvertSecondsToHHmm(entity.Video?.Duration ?? 0),
            DurationSeconds = entity.Video?.Duration ?? 0,
            SubtitleURL = entity.Video?.Subtitle ?? string.Empty,
            VideoResolverURL = $"/MediaDeliveryProfiles{videoResolverPath}"
        };
    }
}
