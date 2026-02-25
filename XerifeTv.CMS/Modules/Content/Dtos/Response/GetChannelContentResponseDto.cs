using XerifeTv.CMS.Modules.Abstractions.ValueObjects;
using XerifeTv.CMS.Modules.Channel;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Modules.Content.Dtos.Response;

public class GetChannelContentResponseDto
{
    public string Id { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public ICollection<string> Categories { get; private set; } = [];
    public string LogoUrl { get; private set; } = string.Empty;
    public Video? Video { get; private set; }
    public string? MediaDeliveryProfileId { get; private set; }
    public string? MediaRoute { get; private set; }
    public string? UrlResolverPath { get; private set; }

    public static GetChannelContentResponseDto FromEntity(ChannelEntity entity, string encryptKey)
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

        return new GetChannelContentResponseDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Categories = entity.Categories,
            LogoUrl = entity.LogoUrl,
            Video = entity.Video,
            MediaRoute = entity.MediaRoute,
            MediaDeliveryProfileId = entity.MediaDeliveryProfileId,
            UrlResolverPath = $"/MediaDeliveryProfiles{videoResolverPath}"
        };
    }
}
