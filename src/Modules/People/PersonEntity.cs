using XerifeTv.CMS.Modules.Abstractions.Entities;

namespace XerifeTv.CMS.Modules.People;

public sealed class PersonEntity : BaseEntity
{
    public int? TmdbId { get; set; }
    public string? ImdbId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Biography { get; set; }
    public DateTime? Birthday { get; set; }
    public DateTime? Deathday { get; set; }
    public string? PlaceOfBirth { get; set; }
    public string? ProfileUrl { get; set; }
}
