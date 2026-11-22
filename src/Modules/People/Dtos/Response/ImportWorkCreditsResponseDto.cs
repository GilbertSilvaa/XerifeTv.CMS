namespace XerifeTv.CMS.Modules.People.Dtos.Response;

public sealed class ImportWorkCreditsResponseDto
{
    public IReadOnlyCollection<GetCreditResponseDto> Credits { get; init; } = [];
    public int Imported { get; init; }
    public int Ignored { get; init; }
    public int PeopleCreated { get; init; }
    public int PeopleReused { get; init; }
}
