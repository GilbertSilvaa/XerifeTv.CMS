using Newtonsoft.Json;

namespace XerifeTv.CMS.Modules.Integrations.Tmdb.Dtos;

public sealed class TmdbPersonSearchResponseDto
{
    [JsonProperty("results")]
    public List<TmdbPersonSearchResultDto> Results { get; set; } = [];
}

public sealed class TmdbPersonSearchResultDto
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("profile_path")]
    public string? ProfilePath { get; set; }

    [JsonIgnore]
    public string? ProfileUrl => TmdbImageUrl.Build(ProfilePath);
}

public sealed class TmdbPersonDetailsResponseDto
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("biography")]
    public string? Biography { get; set; }

    [JsonProperty("birthday")]
    public string? Birthday { get; set; }

    [JsonProperty("deathday")]
    public string? Deathday { get; set; }

    [JsonProperty("place_of_birth")]
    public string? PlaceOfBirth { get; set; }

    [JsonProperty("profile_path")]
    public string? ProfilePath { get; set; }

    [JsonProperty("imdb_id")]
    public string? ImdbId { get; set; }

    [JsonIgnore]
    public string? ProfileUrl => TmdbImageUrl.Build(ProfilePath);
}

public sealed class TmdbPersonDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Biography { get; set; }
    public DateTime? Birthday { get; set; }
    public DateTime? Deathday { get; set; }
    public string? PlaceOfBirth { get; set; }
    public string? ProfileUrl { get; set; }
    public string? ImdbId { get; set; }
}

public static class TmdbImageUrl
{
    private const string TmdbBaseUrl = "https://image.tmdb.org/t/p/original";
    private const string ProxyPrefix = "https://images.plex.tv/photo?size=medium-360&scale=1&url=";

    public static string? Build(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        var normalizedPath = path.StartsWith('/') ? path : $"/{path}";

        return $"{ProxyPrefix}{TmdbBaseUrl}{normalizedPath}";
    }

    public static string? NormalizeStoredUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        const string tmdbPrefix = "https://image.tmdb.org/t/p/";

        var tmdbUrl = url.StartsWith(ProxyPrefix, StringComparison.OrdinalIgnoreCase)
            ? url[ProxyPrefix.Length..]
            : url;

        if (!tmdbUrl.StartsWith(tmdbPrefix, StringComparison.OrdinalIgnoreCase))
            return url;

        var remainder = tmdbUrl[tmdbPrefix.Length..];
        var separator = remainder.IndexOf('/');

        if (separator < 0) return url;

        return $"{ProxyPrefix}{TmdbBaseUrl}/{remainder[(separator + 1)..]}";
    }
}
