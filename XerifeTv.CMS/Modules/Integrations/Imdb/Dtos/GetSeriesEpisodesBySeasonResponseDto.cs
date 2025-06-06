using Newtonsoft.Json;

namespace XerifeTv.CMS.Modules.Integrations.Imdb.Dtos;

public class GetSeriesEpisodesBySeasonResponseDto
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("air_date")]
    public DateTime? AirDate { get; set; }

    [JsonProperty("episodes")]
    public List<EpisodeDto> Episodes { get; set; } = [];

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("overview")]
    public string Overview { get; set; } = string.Empty;

    private string _posterUrl = string.Empty;
    [JsonProperty("poster_path")]
    public string PosterUrl
    {
        get => _posterUrl;
        set => _posterUrl = $"https://images.plex.tv/photo?size=medium-360&scale=1&url=https://image.tmdb.org/t/p/original{value}";
    }

    [JsonProperty("season_number")]
    public int SeasonNumber { get; set; }
}

public class EpisodeDto
{
    [JsonProperty("air_date")]
    public DateTime? AirDate { get; set; }

    [JsonProperty("episode_number")]
    public int EpisodeNumber { get; set; }

    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("overview")]
    public string Overview { get; set; } = string.Empty;

    [JsonProperty("production_code")]
    public string ProductionCode { get; set; } = string.Empty;

    [JsonProperty("season_number")]
    public int SeasonNumber { get; set; }

    private string _bannerUrl = string.Empty;
    [JsonProperty("still_path")]
    public string BannerUrl
    {
        get => _bannerUrl;
        set => _bannerUrl = $"https://images.plex.tv/photo?size=medium-360&scale=1&url=https://image.tmdb.org/t/p/original{value}";
    }

    [JsonProperty("vote_average")]
    public double VoteAverage { get; set; }

    [JsonProperty("vote_count")]
    public int VoteCount { get; set; }

    [JsonProperty("runtime")]
    public int? Runtime { get; set; }
}
