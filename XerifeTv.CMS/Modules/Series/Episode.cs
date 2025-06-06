﻿using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.Abstractions.ValueObjects;

namespace XerifeTv.CMS.Modules.Series;

public class Episode : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string BannerUrl { get; set; } = string.Empty;
    public int Number { get; set; }
    public int Season { get; set; }
    public Video? Video { get; set; }
    public bool Disabled { get; set; } = false;
}
