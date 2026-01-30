using XerifeTv.CMS.Modules.Abstractions.ValueObjects;

namespace XerifeTv.CMS.Modules.Channel.Dtos.Request;

public class UpdateChannelRequestDto
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Categories { get; init; } = string.Empty;
    public string LogoUrl { get; init; } = string.Empty;
    public string VideoUrl { get; init; } = string.Empty;
    public string VideoStreamFormat { get; init; } = string.Empty;
    public string? MediaDeliveryProfileId { get; init; }
    public string? MediaRoute { get; init; }
    public bool Disabled { get; init; } = false;

    public ChannelEntity ToEntity()
    {
        var categorieList = Categories.Split(",").ToList()
          .Select(x => x.Trim())
          .Where(x => !string.IsNullOrEmpty(x))
          .ToList();

        return new ChannelEntity
        {
            Id = Id,
            Title = Title,
            Categories = categorieList,
            LogoUrl = LogoUrl,
            Video = new Video(VideoUrl, 0, VideoStreamFormat),
            MediaDeliveryProfileId = MediaDeliveryProfileId,
            MediaRoute = MediaRoute,
            Disabled = Disabled
        };
    }
}
