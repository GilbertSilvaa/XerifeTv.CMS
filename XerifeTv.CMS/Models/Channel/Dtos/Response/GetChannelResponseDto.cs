using XerifeTv.CMS.Models.Abstractions.ValueObjects;

namespace XerifeTv.CMS.Models.Channel.Dtos.Response;

public class GetChannelResponseDto
{
  public string Id { get; private set; } = string.Empty;
  public string Title { get; private set; } = string.Empty;
  public string Category { get; private set; } = string.Empty;
  public string LogoUrl { get; private set; } = string.Empty;
  public Video? Video { get; private set; }
  public DateTime RegistrationDate { get; private set; }
  public bool Disabled { get; private set; } = false;

  public static GetChannelResponseDto FromEntity(ChannelEntity entity)
  {
    return new GetChannelResponseDto
    {
      Id = entity.Id,
      Title = entity.Title,
      Category = entity.Category, 
      LogoUrl = entity.LogoUrl,
      Video = entity.Video,
      RegistrationDate = entity.CreateAt,
      Disabled = entity.Disabled
    };
  }
}
