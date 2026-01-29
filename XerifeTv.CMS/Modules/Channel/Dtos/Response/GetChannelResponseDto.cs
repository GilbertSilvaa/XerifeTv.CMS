using XerifeTv.CMS.Modules.Abstractions.ValueObjects;

namespace XerifeTv.CMS.Modules.Channel.Dtos.Response;

public class GetChannelResponseDto
{
    public string Id { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Categories { get; private set; } = string.Empty;
    public string LogoUrl { get; private set; } = string.Empty;
    public Video? Video { get; private set; }
    public string? MediaDeliveryProfileId { get;private set; }
    public string? MediaRoute { get; private set; }
    public DateTime RegistrationDate { get; private set; }
    public bool Disabled { get; private set; } = false;

    public string? UrlResolverPath
        => !string.IsNullOrWhiteSpace(MediaDeliveryProfileId)
            ? $"/MediaDeliveryProfiles/ResolveUrl?mediaDeliveryProfileId={MediaDeliveryProfileId}&mediaPath={Uri.EscapeDataString(MediaRoute ?? "")}"
            : $"/MediaDeliveryProfiles/ResolveUrlFixed?urlFixed={Uri.EscapeDataString(Video?.Url ?? "")}&streamFormat={Video?.StreamFormat}";

    public static GetChannelResponseDto FromEntity(ChannelEntity entity)
    {
        return new GetChannelResponseDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Categories = string.Join(", ", entity.Categories),
            LogoUrl = entity.LogoUrl,
            Video = entity.Video,
            MediaDeliveryProfileId = entity.MediaDeliveryProfileId,
            MediaRoute = entity.MediaRoute,
            RegistrationDate = entity.CreateAt,
            Disabled = entity.Disabled
        };
    }
}
