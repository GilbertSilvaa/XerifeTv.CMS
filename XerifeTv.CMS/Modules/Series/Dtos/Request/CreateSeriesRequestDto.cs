using XerifeTv.CMS.Modules.Series;

namespace XerifeTv.CMS.Modules.Series.Dtos.Request;

public class CreateSeriesRequestDto
{
  public string Title { get; init; } = string.Empty;
  public string Synopsis { get; init; } = string.Empty;
  public string Category { get; init; } = string.Empty;
  public string PosterUrl { get; init; } = string.Empty;
  public string BannerUrl { get; init; } = string.Empty;
  public int ReleaseYear { get; init; }
  public int ParentalRating { get; init; }
  public float Review { get; init; }
  public int NumberSeasons { get; init; }

  public SeriesEntity ToEntity()
  {
    return new SeriesEntity
    {
      Title = Title,
      Synopsis = Synopsis,
      Category = Category,
      PosterUrl = PosterUrl,
      BannerUrl = BannerUrl,
      ReleaseYear = ReleaseYear,
      ParentalRating = ParentalRating,
      NumberSeasons = NumberSeasons,
      Review = Review
    };
  }
}
