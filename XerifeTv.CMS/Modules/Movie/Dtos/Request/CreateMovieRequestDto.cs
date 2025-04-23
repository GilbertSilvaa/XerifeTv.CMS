﻿using XerifeTv.CMS.Modules.Abstractions.ValueObjects;

namespace XerifeTv.CMS.Modules.Movie.Dtos.Request;

public class CreateMovieRequestDto
{
  public string ImdbId { get; init; } = string.Empty;
  public string Title { get; init; } = string.Empty;
  public string Synopsis { get; init; } = string.Empty;
  public string Categories { get; init; } = string.Empty;
  public string PosterUrl { get; init; } = string.Empty;
  public string BannerUrl { get; init; } = string.Empty;
  public int ReleaseYear { get; init; }
  public int ParentalRating { get; init; }
  public float Review { get; init; }
  public string VideoUrl { get; init; } = string.Empty;
  public long VideoDuration { get; init; }
  public string VideoStreamFormat { get; init; } = string.Empty;
  public string? VideoSubtitle { get; init; }

  public MovieEntity ToEntity()
  {
    var categorieList = Categories.Split(",").ToList()
      .Select(x => x.Trim())
      .Where(x => !string.IsNullOrEmpty(x))
      .ToList();
    
    return new MovieEntity
    {
      Title = Title,
      ImdbId = ImdbId,
      Synopsis = Synopsis,
      Categories = categorieList,
      PosterUrl = PosterUrl,
      BannerUrl = BannerUrl,
      ReleaseYear = ReleaseYear,
      ParentalRating = ParentalRating,
      Review = Review,
      Video = new Video(VideoUrl,VideoDuration,VideoStreamFormat, VideoSubtitle)
    };
  }
}
