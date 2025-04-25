using XerifeTv.CMS.Modules.Series;

namespace XerifeTv.CMS.Modules.Content.Dtos.Response;

public class GetSeriesContentResponseDto
{
  public string Id { get; private set; } = string.Empty;
  public string Title { get; private set; } = string.Empty;
  public string Synopsis { get; private set; } = string.Empty;
  public ICollection<string> Categories { get; private set; } = [];
  public string PosterUrl { get; private set; } = string.Empty;
  public string BannerUrl { get; private set; } = string.Empty;
  public int ReleaseYear { get; private set; }
  public int ParentalRating { get; private set; }
  public float Review { get; private set; }
  public int NumberSeasons { get; private set; }

  public static GetSeriesContentResponseDto FromEntity(SeriesEntity entity)
  {
    return new GetSeriesContentResponseDto
    {
      Id = entity.Id,
      Title = entity.Title,
      Synopsis = entity.Synopsis,
      Categories = entity.Categories,
      PosterUrl = entity.PosterUrl,
      BannerUrl = entity.BannerUrl,
      ReleaseYear = entity.ReleaseYear,
      ParentalRating = entity.ParentalRating,
      Review = entity.Review,
      NumberSeasons = entity.NumberSeasons
    };
  }
}
