using Newtonsoft.Json;
using XerifeTv.CMS.Modules.Integrations.Tmdb.Dtos;

namespace XerifeTv.CMS.Modules.Integrations.Tmdb.Dtos;

public sealed class TmdbCreditsResponseDto
{
    [JsonProperty("cast")]
    public List<TmdbCastCreditDto> Cast { get; set; } = [];

    [JsonProperty("crew")]
    public List<TmdbCrewCreditDto> Crew { get; set; } = [];
}

public sealed class TmdbCastCreditDto
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("character")]
    public string? Character { get; set; }

    [JsonProperty("order")]
    public int? Order { get; set; }

    [JsonProperty("profile_path")]
    public string? ProfilePath { get; set; }

    [JsonIgnore]
    public string? ProfileUrl => TmdbImageUrl.Build(ProfilePath);
}

public sealed class TmdbCrewCreditDto
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("department")]
    public string? Department { get; set; }

    [JsonProperty("job")]
    public string? Job { get; set; }

    [JsonProperty("profile_path")]
    public string? ProfilePath { get; set; }

    [JsonIgnore]
    public string? ProfileUrl => TmdbImageUrl.Build(ProfilePath);
}
