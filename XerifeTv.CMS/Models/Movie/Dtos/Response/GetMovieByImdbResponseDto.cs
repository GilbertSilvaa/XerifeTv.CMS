using Newtonsoft.Json;

namespace XerifeTv.CMS.Models.Movie.Dtos.Response;

public class GetMovieByImdbResponseDto
{
  public string Title { get; set; } = string.Empty;
  public string Overview { get; set; } = string.Empty;
  public List<GenreDto> Genres { get; set; }
  
  [JsonProperty("vote_average")]
  public double VoteAverage { get; set; } = 0.0;

  private string _posterUrl = string.Empty;
  [JsonProperty("poster_path")]
  public string PosterUrl
  {
    get => _posterUrl;
    set => _posterUrl = 
      $"https://images.plex.tv/photo?size=medium-360&scale=1&url=https://image.tmdb.org/t/p/original{value}";
  }
  
  private string _bannerUrl = string.Empty;
  [JsonProperty("backdrop_path")]
  public string BannerUrl
  {
    get => _bannerUrl;
    set => _bannerUrl = $"https://image.tmdb.org/t/p/original{value}";
  }

  private string _releaseYear = string.Empty;
  [JsonProperty("release_date")]
  public string? ReleaseYear
  {
    get => _releaseYear.Split("-").FirstOrDefault();
    set => _releaseYear = value;
  }

  public record GenreDto(int Id, string Name);
}