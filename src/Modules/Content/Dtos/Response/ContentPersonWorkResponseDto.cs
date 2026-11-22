namespace XerifeTv.CMS.Modules.Content.Dtos.Response;

public sealed class ContentPersonWorkResponseDto
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string PosterURL { get; init; } = string.Empty;
    public int ReleaseYear { get; init; }
    public string Type { get; init; } = string.Empty;
    public IReadOnlyCollection<ContentParticipationResponseDto> Participations { get; init; } = [];
}
