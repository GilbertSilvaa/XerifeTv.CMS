using XerifeTv.CMS.Modules.Series;

namespace XerifeTv.CMS.Modules.Series.Dtos.Request;

public class UpdateSeriesRequestDto
{
  public string Id { get; init; } = string.Empty;
  public string Title { get; init; } = string.Empty;
  public string Synopsis { get; init; } = string.Empty;
  public string Category { get; init; } = string.Empty;
  public string Categories { get; init; } = string.Empty;
  public string PosterUrl { get; init; } = string.Empty;
  public string BannerUrl { get; init; } = string.Empty;
  public int ReleaseYear { get; init; }
  public int ParentalRating { get; init; }
  public float Review { get; init; }
  public int NumberSeasons { get; init; }
  public bool Disabled { get; init; } = false;

  public SeriesEntity ToEntity()
  {
    var categorieList = Categories.Split(",").ToList()
      .Select(x => x.Trim())
      .Where(x => !string.IsNullOrEmpty(x))
      .ToList();
    
    return new SeriesEntity
    {
      Id = Id,
      Title = Title,
      Synopsis = Synopsis,
      Category = Category,
      Categories = categorieList,
      PosterUrl = PosterUrl,
      BannerUrl = BannerUrl,
      ReleaseYear = ReleaseYear,
      ParentalRating = ParentalRating,
      NumberSeasons = NumberSeasons,
      Review = Review,
      Disabled = Disabled
    };
  }
}
