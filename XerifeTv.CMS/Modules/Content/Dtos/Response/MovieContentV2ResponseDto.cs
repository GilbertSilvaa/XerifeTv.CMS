using XerifeTv.CMS.Modules.Movie;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Modules.Content.Dtos.Response;

public class MovieContentV2ResponseDto
{
    public string Id { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string[] Categories { get; private set; } = [];
    public string PosterURL { get; private set; } = string.Empty;
    public string BannerURL { get; private set; } = string.Empty;
    public string ParentalRating { get; private set; } = string.Empty;
    public int ReleaseYear { get; private set; }
    public string Synopsis { get; private set; } = string.Empty;
    public float RatingImdb { get; private set; }
    public string Duration { get; private set; } = string.Empty;
    public long DurationSeconds { get; private set; }
    public string VideoResolverURL { get; private set; } = string.Empty;
    public string? SubtitleURL { get; private set; }

    public static MovieContentV2ResponseDto FromEntity(MovieEntity entity)
    {
        string videoResolverPath = !string.IsNullOrWhiteSpace(entity.MediaDeliveryProfileId)
            ? $"/ResolveUrl?mediaDeliveryProfileId={entity.MediaDeliveryProfileId}&mediaPath={Uri.EscapeDataString(entity.MediaRoute ?? "")}&isCached=true"
            : $"/ResolveUrlFixed?urlFixed={Uri.EscapeDataString(entity.Video?.Url ?? "")}&streamFormat={entity.Video?.StreamFormat}&isCached=true";

        return new()
        {
            Id = entity.Id,
            Title = entity.Title,
            Categories = [.. entity.Categories],
            PosterURL = entity.PosterUrl,
            BannerURL = entity.BannerUrl,
            ParentalRating = entity.ParentalRating == 0 ? "L" : entity.ParentalRating.ToString(),
            ReleaseYear = entity.ReleaseYear,
            Synopsis = entity.Synopsis,
            RatingImdb = entity.Review,
            Duration = DateTimeHelper.ConvertSecondsToHHmm(entity.Video?.Duration ?? 0),
            DurationSeconds = entity.Video?.Duration ?? 0,
            VideoResolverURL = $"/MediaDeliveryProfiles{videoResolverPath}",
            SubtitleURL = entity.Video?.Subtitle
        };
    }
}
