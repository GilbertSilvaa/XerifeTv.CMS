using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.Abstractions.ValueObjects;

namespace XerifeTv.CMS.Modules.Movie;

public class MovieEntity : Midia
{
    public string ImdbId { get; set; } = string.Empty;
    public ICollection<string> Categories { get; set; } = [];
    public float Review { get; set; } = 0;
    public Video? Video { get; set; }
    public bool Disabled { get; set; } = false;
}
