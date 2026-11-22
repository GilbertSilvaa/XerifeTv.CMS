using XerifeTv.CMS.Modules.Abstractions.Enums;
using XerifeTv.CMS.Modules.Abstractions.ValueObjects;
using XerifeTv.CMS.Modules.People;

namespace XerifeTv.CMS.Modules.Content.Dtos.Response;

public sealed class ContentCreditResponseDto
{
    public string PersonId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ProfileURL { get; init; }
    public string Type { get; init; } = nameof(ECreditType.Cast);
    public string? Character { get; init; }
    public string? Department { get; init; }
    public string? Job { get; init; }
    public int? Order { get; init; }

    public static ContentCreditResponseDto FromCredit(Credit credit, PersonEntity? person)
        => new()
        {
            PersonId = credit.PersonId,
            Name = person?.Name ?? string.Empty,
            ProfileURL = person?.ProfileUrl,
            Type = credit.Type.ToString(),
            Character = credit.Type == ECreditType.Cast
                ? (string.IsNullOrWhiteSpace(credit.Character) ? null : credit.Character)
                : null,
            Department = credit.Type == ECreditType.Crew
                ? (string.IsNullOrWhiteSpace(credit.Department) ? null : credit.Department)
                : null,
            Job = credit.Type == ECreditType.Crew
                ? (string.IsNullOrWhiteSpace(credit.Job) ? null : credit.Job)
                : null,
            Order = credit.Type == ECreditType.Cast ? credit.Order : null
        };
}
