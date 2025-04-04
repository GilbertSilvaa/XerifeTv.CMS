using XerifeTv.CMS.Modules.Abstractions.ValueObjects;

namespace XerifeTv.CMS.Modules.Series.Dtos.Request;

public class CreateEpisodeRequestDto
{
  public string SerieId { get; init; } = string.Empty;
  public string Title { get; init; } = string.Empty;
  public string BannerUrl { get; init; } = string.Empty;
  public int Number { get; init; }
  public int Season { get; init; }
  public string VideoUrl { get; init; } = string.Empty;
  public long VideoDuration { get; init; }
  public string VideoStreamFormat { get; init; } = string.Empty;
  public string? VideoSubtitle { get; init; }

  public Episode ToEntity()
  {
    return new Episode
    {
      Title = Title,
      BannerUrl = BannerUrl,
      Number = Number,
      Season = Season,
      Video = new Video(VideoUrl, VideoDuration, VideoStreamFormat, VideoSubtitle)
    };
  }
}
