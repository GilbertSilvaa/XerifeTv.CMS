using System.Security.Cryptography.Xml;
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

    public static MovieContentV2ResponseDto FromEntity(MovieEntity entity, string encryptKey)
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
