using XerifeTv.CMS.Models.Abstractions.ValueObjects;
using XerifeTv.CMS.Helpers;

namespace XerifeTv.CMS.Models.Movie.Dtos.Response;

public sealed class GetMovieResponseDto
{
  public string Id { get; private set; } = string.Empty;
  public string ImdbId { get; private set; } = string.Empty;
  public string Title { get; private set; } = string.Empty;
  public string Synopsis { get; private set; } = string.Empty;
  public string Category { get; private set; } = string.Empty;
  public string PosterUrl { get; private set; } = string.Empty;
  public string BannerUrl { get; private set; } = string.Empty;
  public int ReleaseYear { get; private set; }
  public int ParentalRating { get; private set; }
  public float Review { get; private set; } 
  public DateTime RegistrationDate { get; private set; }
  public Video? Video { get; private set; }
  public string DurationHHmm => DateTimeHelper.ConvertSecondsToHHmm(Video?.Duration ?? 0);
  public bool Disabled { get; private set; } = false;

  public static GetMovieResponseDto FromEntity(MovieEntity entity)
  {
    return new GetMovieResponseDto
    {
      Id = entity.Id,
      ImdbId = entity.ImdbId,
      Title = entity.Title,
      Synopsis = entity.Synopsis,
      Category = entity.Category,
      PosterUrl = entity.PosterUrl,
      BannerUrl = entity.BannerUrl,
      ReleaseYear = entity.ReleaseYear,
      ParentalRating = entity.ParentalRating,
      Review = entity.Review,
      RegistrationDate = entity.CreateAt,
      Video = entity.Video,
      Disabled = entity.Disabled
    };
  }
}