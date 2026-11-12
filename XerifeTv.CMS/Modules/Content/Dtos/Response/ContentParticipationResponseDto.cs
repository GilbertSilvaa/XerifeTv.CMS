namespace XerifeTv.CMS.Modules.Content.Dtos.Response;

public sealed class ContentParticipationResponseDto
{
    public string Type { get; init; } = string.Empty;
    public string? Character { get; init; }
    public string? Department { get; init; }
    public string? Job { get; init; }
    public int? Order { get; init; }
}
