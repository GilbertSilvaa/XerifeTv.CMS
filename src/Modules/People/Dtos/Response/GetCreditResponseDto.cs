using XerifeTv.CMS.Modules.Abstractions.Enums;
using XerifeTv.CMS.Modules.Abstractions.ValueObjects;

namespace XerifeTv.CMS.Modules.People.Dtos.Response;

public sealed class GetCreditResponseDto
{
    public string PersonId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ProfileUrl { get; init; }
    public string Type { get; init; } = nameof(ECreditType.Cast);
    public string? Character { get; init; }
    public string? Department { get; init; }
    public string? Job { get; init; }
    public int? Order { get; init; }

    public static GetCreditResponseDto FromCredit(Credit credit, PersonEntity? person = null)
        => new()
        {
            PersonId = credit.PersonId,
            Name = person?.Name ?? string.Empty,
            ProfileUrl = person?.ProfileUrl,
            Type = credit.Type.ToString(),
            Character = credit.Character,
            Department = credit.Department,
            Job = credit.Job,
            Order = credit.Order
        };
}
