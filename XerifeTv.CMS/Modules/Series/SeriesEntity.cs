﻿using XerifeTv.CMS.Modules.Abstractions.Entities;

namespace XerifeTv.CMS.Modules.Series;

public sealed class SeriesEntity : Midia
{
  public string Category { get; set; } = string.Empty;
  public float Review { get; set; }
  public int NumberSeasons { get; set; } = 1;
  public ICollection<Episode> Episodes { get; set; } = [];
  public bool Disabled { get; set; } = false;
}
