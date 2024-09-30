﻿using XerifeTv.CMS.Helpers;
using XerifeTv.CMS.Models.Abstractions.ValueObjects;
using XerifeTv.CMS.Models.Movie;

namespace XerifeTv.CMS.Models.Content.Dtos.Response;

public class GetMovieContentResponseDto
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
  public Video? Video { get; private set; }
  public string DurationHHmm => DateTimeHelper.ConvertSecondsToHHmm(Video?.Duration ?? 0);

  public static GetMovieContentResponseDto FromEntity(MovieEntity entity)
  {
    return new GetMovieContentResponseDto
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
      Video = entity.Video
    };
  }
}