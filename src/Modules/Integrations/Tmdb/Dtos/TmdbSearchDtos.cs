using Newtonsoft.Json;

namespace XerifeTv.CMS.Modules.Integrations.Tmdb.Dtos;

public class TmdbSearchResponseDto<T>
{
    [JsonProperty("page")]
    public int Page { get; set; }

    [JsonProperty("total_pages")]
    public int TotalPages { get; set; }

    [JsonProperty("results")]
    public List<T> Results { get; set; } = [];
}

public class TmdbSearchMovieResultDto
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("release_date")]
    public string? ReleaseDate { get; set; }

    [JsonProperty("poster_path")]
    public string? PosterPath { get; set; }

    [JsonProperty("overview")]
    public string Overview { get; set; } = string.Empty;

    [JsonProperty("vote_average")]
    public float VoteAverage { get; set; }
}

public class TmdbSearchSeriesResultDto
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("first_air_date")]
    public string? FirstAirDate { get; set; }

    [JsonProperty("poster_path")]
    public string? PosterPath { get; set; }

    [JsonProperty("overview")]
    public string Overview { get; set; } = string.Empty;

    [JsonProperty("vote_average")]
    public float VoteAverage { get; set; }
}

public class TmdbExternalIdsResponseDto
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("imdb_id")]
    public string? ImdbId { get; set; }
}

/// <summary>Normalized search result used by the content calendar modal.</summary>
public record TmdbSearchResultDto(
    int TmdbId,
    string Title,
    string? ReleaseYear,
    string? ImageUrl,
    string Overview,
    float VoteAverage)
{
    private const string ImageBaseUrl = "https://image.tmdb.org/t/p/w342";

    public static TmdbSearchResultDto FromMovie(TmdbSearchMovieResultDto movie)
        => new(
            movie.Id,
            movie.Title,
            ExtractYear(movie.ReleaseDate),
            BuildImageUrl(movie.PosterPath),
            movie.Overview,
            movie.VoteAverage);

    public static TmdbSearchResultDto FromSeries(TmdbSearchSeriesResultDto series)
        => new(
            series.Id,
            series.Title,
            ExtractYear(series.FirstAirDate),
            BuildImageUrl(series.PosterPath),
            series.Overview,
            series.VoteAverage);

    private static string? ExtractYear(string? date)
        => string.IsNullOrWhiteSpace(date) ? null : date.Split('-').FirstOrDefault();

    private static string? BuildImageUrl(string? posterPath)
        => string.IsNullOrWhiteSpace(posterPath) ? null : $"{ImageBaseUrl}{posterPath}";
}
