using XerifeTv.CMS.Modules.Abstractions.Enums;

namespace XerifeTv.CMS.Modules.Abstractions.ValueObjects;

public sealed class Credit
{
    public string PersonId { get; set; } = string.Empty;
    public ECreditType Type { get; set; } = ECreditType.Cast;
    public string Character { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Job { get; set; } = string.Empty;
    public int? Order { get; set; }
}
