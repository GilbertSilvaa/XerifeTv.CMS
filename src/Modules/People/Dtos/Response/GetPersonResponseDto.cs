namespace XerifeTv.CMS.Modules.People.Dtos.Response;

public sealed class GetPersonResponseDto
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

    public static GetPersonResponseDto FromEntity(PersonEntity entity)
        => new()
        {
            Id = entity.Id,
            TmdbId = entity.TmdbId,
            ImdbId = entity.ImdbId,
            Name = entity.Name,
            Biography = entity.Biography,
            Birthday = entity.Birthday,
            Deathday = entity.Deathday,
            PlaceOfBirth = entity.PlaceOfBirth,
            ProfileUrl = entity.ProfileUrl
        };
}
