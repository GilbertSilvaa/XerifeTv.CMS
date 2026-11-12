namespace XerifeTv.CMS.Modules.People.Dtos.Request;

public sealed class ImportWorkCreditsRequestDto
{
    public string ImdbId { get; init; } = string.Empty;
    public string? WorkId { get; init; }
    public string? WorkTitle { get; init; }
    public ICollection<CreditRequestDto> Credits { get; init; } = [];
}
