using XerifeTv.CMS.Models.Abstractions.Entities;
using XerifeTv.CMS.Models.Abstractions.ValueObjects;

namespace XerifeTv.CMS.Models.Movie;

public class MovieEntity : Midia
{
  public string ImdbId { get; set; } = string.Empty;
  public string Category { get; set; } = string.Empty;
  public float Review { get; set; } = 0;
  public Video? Video { get; set; }
  public bool Disabled { get; set; } = false;
}
