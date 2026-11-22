using SharpCompress.Common;
using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.Abstractions.ValueObjects;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Modules.Series;

public class Episode : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string BannerUrl { get; set; } = string.Empty;
    public int Number { get; set; }
    public int Season { get; set; }
    public Video? Video { get; set; }
    public string? MediaDeliveryProfileId { get; set; }
    public string? MediaRoute { get; set; }
    public bool Disabled { get; set; } = false;
    public string? UrlResolverPath { get; private set; }

    public void SetUrlResolverPath(string encryptKey)
    {
        string videoResolverPath;

        if (!string.IsNullOrWhiteSpace(MediaDeliveryProfileId))
        {
            string mdp = CryptographyHelper.Encrypt(MediaDeliveryProfileId, encryptKey);
            string mp = CryptographyHelper.Encrypt(MediaRoute ?? string.Empty, encryptKey);
            videoResolverPath = $"/ResolveUrlMdp?mdp={Uri.EscapeDataString(mdp)}&mp={Uri.EscapeDataString(mp)}";
        }
        else
        {
            string uf = CryptographyHelper.Encrypt(Video?.Url ?? string.Empty, encryptKey);
            string sf = CryptographyHelper.Encrypt(Video?.StreamFormat ?? string.Empty, encryptKey);
            videoResolverPath = $"/ResolveUrlFx?uf={Uri.EscapeDataString(uf)}&sf={Uri.EscapeDataString(sf)}";
        }

        UrlResolverPath = $"/MediaDeliveryProfiles{videoResolverPath}";
    }
}