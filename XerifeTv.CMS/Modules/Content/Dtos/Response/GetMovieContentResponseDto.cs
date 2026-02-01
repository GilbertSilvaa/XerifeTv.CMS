using XerifeTv.CMS.Modules.Abstractions.ValueObjects;
using XerifeTv.CMS.Modules.Movie;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Modules.Content.Dtos.Response;

public class GetMovieContentResponseDto
{
    public string Id { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Synopsis { get; private set; } = string.Empty;
    public ICollection<string> Categories { get; private set; } = [];
    public string PosterUrl { get; private set; } = string.Empty;
    public string BannerUrl { get; private set; } = string.Empty;
    public int ReleaseYear { get; private set; }
    public int ParentalRating { get; private set; }
    public float Review { get; private set; }
    public Video? Video { get; private set; }
    public string? MediaDeliveryProfileId { get; private set; }
    public string? MediaRoute { get; private set; }
    public string DurationHHmm => DateTimeHelper.ConvertSecondsToHHmm(Video?.Duration ?? 0);
    public string? UrlResolverPath
        => !string.IsNullOrWhiteSpace(MediaDeliveryProfileId)
            ? $"/MediaDeliveryProfiles/ResolveUrl?mediaDeliveryProfileId={MediaDeliveryProfileId}&mediaPath={Uri.EscapeDataString(MediaRoute ?? "")}&isCached=true"
            : $"/MediaDeliveryProfiles/ResolveUrlFixed?urlFixed={Uri.EscapeDataString(Video?.Url ?? "")}&streamFormat={Video?.StreamFormat}";

    public static GetMovieContentResponseDto FromEntity(MovieEntity entity)
    {
        return new GetMovieContentResponseDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Synopsis = entity.Synopsis,
            Categories = entity.Categories,
            PosterUrl = entity.PosterUrl,
            BannerUrl = entity.BannerUrl,
            ReleaseYear = entity.ReleaseYear,
            ParentalRating = entity.ParentalRating,
            Review = entity.Review,
            Video = entity.Video,
            MediaDeliveryProfileId = entity.MediaDeliveryProfileId,
            MediaRoute = entity.MediaRoute
        };
    }
}
