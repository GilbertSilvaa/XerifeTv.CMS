using Newtonsoft.Json;

namespace XerifeTv.CMS.Modules.Integrations.Imdb.Dtos;

public class GetSeriesByImdbResponseDto
{
	public int Id { get; set; }

	[JsonProperty("name")]
	public string Title { get; set; } = string.Empty;

	public string Overview { get; set; } = string.Empty;
	public List<GenreDto> Genres { get; set; } = [];

	[JsonProperty("vote_average")]
	public float VoteAverage { get; set; }

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
	[JsonProperty("first_air_date")]
	public string? ReleaseYear
	{
		get => _releaseYear.Split("-").FirstOrDefault();
		set => _releaseYear = value ?? string.Empty;
	}

	[JsonProperty("number_of_seasons")]
	public int NumberSeasons { get; set; }

	[JsonProperty("number_of_episodes")]
	public int NumberEpisodes { get; set; }

	public string? TitleImageUrl { get; private set; }
	public string MobileBannerUrl { get; private set; } = string.Empty;

	public void ApplyImages(TmdbImagesResponseDto images)
	{
		TitleImageUrl = images.Logos.FirstOrDefault(image => image.Language == "pt")?.Url
			?? images.Logos.FirstOrDefault(image => image.Language is null)?.Url;
		MobileBannerUrl = images.Posters.FirstOrDefault(image => image.Language is null)?.Url
			?? BannerUrl;
	}

	public record GenreDto(int Id, string Name);
}
