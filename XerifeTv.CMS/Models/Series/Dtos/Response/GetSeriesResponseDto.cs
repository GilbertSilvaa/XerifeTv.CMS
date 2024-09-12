﻿namespace XerifeTv.CMS.Models.Series.Dtos.Response;

public class GetSeriesResponseDto
{
  public string Id { get; private set; } = string.Empty;
  public string Title { get; private set; } = string.Empty;
  public string Synopsis { get; private set; } = string.Empty;
  public string Category { get; private set; } = string.Empty;
  public string PosterUrl { get; private set; } = string.Empty;
  public string BannerUrl { get; private set; } = string.Empty;
  public int ReleaseYear { get; private set; }
  public int ParentalRating { get; private set; }
  public float Review { get; private set; }
  public DateTime RegistrationDate { get; private set; }
  public int NumberSeasons { get; private set; }

  public static GetSeriesResponseDto FromEntity(SeriesEntity entity)
  {
    return new GetSeriesResponseDto
    {
      Id = entity.Id,
      Title = entity.Title,
      Synopsis = entity.Synopsis,
      Category = entity.Category,
      PosterUrl = entity.PosterUrl,
      BannerUrl = entity.BannerUrl,
      ReleaseYear = entity.ReleaseYear,
      ParentalRating = entity.ParentalRating,
      Review = entity.Review,
      RegistrationDate = entity.CreateAt,
      NumberSeasons = entity.NumberSeasons
    };
  }
}
