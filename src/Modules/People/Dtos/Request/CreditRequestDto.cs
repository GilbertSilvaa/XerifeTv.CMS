using XerifeTv.CMS.Modules.Abstractions.Enums;
using XerifeTv.CMS.Modules.Abstractions.ValueObjects;
using XerifeTv.CMS.Modules.People.Dtos.Response;

namespace XerifeTv.CMS.Modules.People.Dtos.Request;

public sealed class CreditRequestDto
{
    public string PersonId { get; init; } = string.Empty;
    public ECreditType Type { get; init; } = ECreditType.Cast;
    public string? Character { get; init; }
    public string? Department { get; init; }
    public string? Job { get; init; }
    public int? Order { get; init; }

    public static CreditRequestDto FromResponse(GetCreditResponseDto credit)
        => new()
        {
            PersonId = credit.PersonId,
            Type = Enum.TryParse<ECreditType>(credit.Type, ignoreCase: true, out var type)
                ? type
                : ECreditType.Cast,
            Character = credit.Character,
            Department = credit.Department,
            Job = credit.Job,
            Order = credit.Order
        };

    public Credit ToCredit()
        => new()
        {
            PersonId = PersonId?.Trim() ?? string.Empty,
            Type = Type,
            Character = Character?.Trim() ?? string.Empty,
            Department = Department?.Trim() ?? string.Empty,
            Job = Job?.Trim() ?? string.Empty,
            Order = Type == ECreditType.Cast ? Order : null
        };
}
