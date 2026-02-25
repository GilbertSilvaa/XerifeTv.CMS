using System.Security.Cryptography.Xml;
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
    public string? UrlResolverPath { get; private set; }

    public static GetMovieContentResponseDto FromEntity(MovieEntity entity, string encryptKey)
    {
        string videoResolverPath;

        if (!string.IsNullOrWhiteSpace(entity.MediaDeliveryProfileId))
        {
            string mdp = CryptographyHelper.Encrypt(entity.MediaDeliveryProfileId, encryptKey);
            string mp = CryptographyHelper.Encrypt(entity.MediaRoute ?? string.Empty, encryptKey);
            videoResolverPath = $"/ResolveUrlMdp?mdp={Uri.EscapeDataString(mdp)}&mp={Uri.EscapeDataString(mp)}";
        }
        else
        {
            string uf = CryptographyHelper.Encrypt(entity.Video?.Url ?? string.Empty, encryptKey);
            string sf = CryptographyHelper.Encrypt(entity.Video?.StreamFormat ?? string.Empty, encryptKey);
            videoResolverPath = $"/ResolveUrlFx?uf={Uri.EscapeDataString(uf)}&sf={Uri.EscapeDataString(sf)}";
        }

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
            MediaRoute = entity.MediaRoute,
            UrlResolverPath = $"/MediaDeliveryProfiles{videoResolverPath}"
        };
    }
}
