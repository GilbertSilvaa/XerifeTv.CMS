using XerifeTv.CMS.Models.Abstractions.ValueObjects;
using XerifeTv.CMS.Models.Channel;

namespace XerifeTv.CMS.Models.Content.Dtos.Response;

public class GetChannelContentResponseDto
{
  public string Id { get; private set; } = string.Empty;
  public string Title { get; private set; } = string.Empty;
  public string Category { get; private set; } = string.Empty;
  public string LogoUrl { get; private set; } = string.Empty;
  public Video? Video { get; private set; }

  public static GetChannelContentResponseDto FromEntity(ChannelEntity entity)
  {
    return new GetChannelContentResponseDto
    {
      Id = entity.Id,
      Title = entity.Title,
      Category = entity.Category,
      LogoUrl = entity.LogoUrl,
      Video = entity.Video
    };
  }
}