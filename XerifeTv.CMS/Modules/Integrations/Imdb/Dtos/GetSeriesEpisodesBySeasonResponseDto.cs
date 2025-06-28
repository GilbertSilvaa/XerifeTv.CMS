using Newtonsoft.Json;

namespace XerifeTv.CMS.Modules.Integrations.Imdb.Dtos;

public class GetSeriesEpisodesBySeasonResponseDto
{
    [JsonProperty("episodes")]
    public List<EpisodeDto> Episodes { get; set; } = [];
}

public class EpisodeDto
{
    const string BannerUrlDefault = "https://img.freepik.com/fotos-gratis/melhores-amigos-a-ver-o-servico-de-streaming-juntos-em-casa_23-2149007840.jpg";

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

    [JsonProperty("season_number")]
    public int SeasonNumber { get; set; }

    private string _bannerUrl = string.Empty;
    [JsonProperty("still_path")]
    public string BannerUrl
    {
        get => _bannerUrl;
        set => _bannerUrl = !string.IsNullOrEmpty(value)
            ? $"https://images.plex.tv/photo?size=medium-360&scale=1&url=https://image.tmdb.org/t/p/original{value}"
            : $"https://images.plex.tv/photo?size=medium-360&scale=1&url={BannerUrlDefault}";
	}

    [JsonProperty("runtime")]
    public int? Runtime { get; set; }

    public long DurationInSeconds => (Runtime ?? 0) * 60L;
}
