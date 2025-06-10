using Newtonsoft.Json;

namespace XerifeTv.CMS.Modules.Integrations.Imdb.Dtos;

public class GetAllResultsByImdbIdResponseDto
{
    [JsonProperty("tv_results")]
    public List<TmdbTvShowDto> TvResults { get; set; } = new();

    [JsonProperty("movie_results")]
    public List<object> MovieResults { get; set; } = new();

    [JsonProperty("person_results")]
    public List<object> PersonResults { get; set; } = new();

    [JsonProperty("tv_episode_results")]
    public List<object> TvEpisodeResults { get; set; } = new();

    [JsonProperty("tv_season_results")]
    public List<object> TvSeasonResults { get; set; } = new();
}

public class TmdbTvShowDto
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    public string Title { get; set; } = string.Empty;

    private string _posterUrl = string.Empty;
    [JsonProperty("poster_path")]
    public string PosterUrl
    {
        get => _posterUrl;
        set => _posterUrl =
            $"https://images.plex.tv/photo?size=medium-360&scale=1&url=https://image.tmdb.org/t/p/original{value}";
    }
}