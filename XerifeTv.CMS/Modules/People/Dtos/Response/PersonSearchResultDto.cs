namespace XerifeTv.CMS.Modules.People.Dtos.Response;

public sealed class PersonSearchResultDto
{
    public int TmdbId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ProfileUrl { get; init; }
    public string? LocalId { get; init; }
    public bool IsRegistered => !string.IsNullOrWhiteSpace(LocalId);
}
