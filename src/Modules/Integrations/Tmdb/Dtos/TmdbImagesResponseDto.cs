using Newtonsoft.Json;

namespace XerifeTv.CMS.Modules.Integrations.Tmdb.Dtos;

public sealed class TmdbImagesResponseDto
{
    [JsonProperty("logos")]
    public List<TmdbImageDto> Logos { get; set; } = [];

    [JsonProperty("posters")]
    public List<TmdbImageDto> Posters { get; set; } = [];
}

public sealed class TmdbImageDto
{
    [JsonProperty("file_path")]
    public string FilePath { get; set; } = string.Empty;

    [JsonProperty("iso_639_1")]
    public string? Language { get; set; }

    public string Url => string.IsNullOrWhiteSpace(FilePath)
        ? string.Empty
        : $"https://image.tmdb.org/t/p/original{FilePath}";
}
