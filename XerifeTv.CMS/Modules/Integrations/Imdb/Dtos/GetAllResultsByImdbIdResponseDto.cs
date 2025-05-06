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
	public string Title { get; set; } = string.Empty;

	[JsonProperty("original_name")]
	public string OriginalName { get; set; } = string.Empty;

	public string Overview { get; set; } = string.Empty;
	
	[JsonProperty("genre_ids")]
	public List<int> GenreIds { get; set; } = new();

	[JsonProperty("vote_average")]
	public float VoteAverage { get; set; }

	[JsonProperty("popularity")]
	public float Popularity { get; set; }

	[JsonProperty("vote_count")]
	public int VoteCount { get; set; }

	[JsonProperty("origin_country")]
	public List<string> OriginCountry { get; set; } = new();

	[JsonProperty("original_language")]
	public string OriginalLanguage { get; set; } = string.Empty;

	public bool Adult { get; set; }

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

	private string _firstAirDate = string.Empty;
	[JsonProperty("first_air_date")]
	public string? ReleaseYear
	{
		get => _firstAirDate.Split('-').FirstOrDefault();
		set => _firstAirDate = value;
	}
}