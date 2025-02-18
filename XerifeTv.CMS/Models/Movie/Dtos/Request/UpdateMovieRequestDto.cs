using XerifeTv.CMS.Models.Abstractions.ValueObjects;

namespace XerifeTv.CMS.Models.Movie.Dtos.Request;

public class UpdateMovieRequestDto
{
  public string Id {  get; init; } = string.Empty;
  public string Title { get; init; } = string.Empty;
  public string Synopsis { get; init; } = string.Empty;
  public string Category { get; init; } = string.Empty;
  public string PosterUrl { get; init; } = string.Empty;
  public string BannerUrl { get; init; } = string.Empty;
  public int ReleaseYear { get; init; }
  public int ParentalRating { get; init; }
  public float Review { get; init; }
  public string VideoUrl { get; init; } = string.Empty;
  public long VideoDuration { get; init; }
  public string VideoStreamFormat { get; init; } = string.Empty;
  public string? VideoSubtitle { get; init; }
  public bool Disabled { get; init; } = false;

  public MovieEntity ToEntity()
  {
    return new MovieEntity
    {
      Id = Id,
      Title = Title,
      Synopsis = Synopsis,
      Category = Category,
      PosterUrl = PosterUrl,
      BannerUrl = BannerUrl,
      ReleaseYear = ReleaseYear,
      ParentalRating = ParentalRating,
      Review = Review,
      Video = new Video(VideoUrl, VideoDuration, VideoStreamFormat, VideoSubtitle),
      Disabled = Disabled
    };
  }
}
