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

    public static EpisodeContentV2ResponseDto FromEntity(Episode entity, string encryptKey)
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
            BannerURL = entity.BannerUrl,
            Duration = DateTimeHelper.ConvertSecondsToHHmm(entity.Video?.Duration ?? 0),
            DurationSeconds = entity.Video?.Duration ?? 0,
            SubtitleURL = entity.Video?.Subtitle ?? string.Empty,
            VideoResolverURL = $"/MediaDeliveryProfiles{videoResolverPath}"
        };
    }
}
