namespace XerifeTv.CMS.Modules.People.Dtos.Request;

public sealed class UpdatePersonRequestDto
{
    public string Id { get; init; } = string.Empty;
    public int? TmdbId { get; init; }
    public string? ImdbId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Biography { get; init; }
    public DateTime? Birthday { get; init; }
    public DateTime? Deathday { get; init; }
    public string? PlaceOfBirth { get; init; }
    public string? ProfileUrl { get; init; }
}
