using XerifeTv.CMS.Modules.Abstractions.ValueObjects;
using XerifeTv.CMS.Modules.Channel;

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
    public string? UrlResolverPath
        => !string.IsNullOrWhiteSpace(MediaDeliveryProfileId)
            ? $"/MediaDeliveryProfiles/ResolveUrl?mediaDeliveryProfileId={MediaDeliveryProfileId}&mediaPath={Uri.EscapeDataString(MediaRoute ?? "")}"
            : null;

    public static GetChannelContentResponseDto FromEntity(ChannelEntity entity)
    {
        return new GetChannelContentResponseDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Categories = entity.Categories,
            LogoUrl = entity.LogoUrl,
            Video = entity.Video,
            MediaRoute = entity.MediaRoute,
            MediaDeliveryProfileId = entity.MediaDeliveryProfileId
        };
    }
}
