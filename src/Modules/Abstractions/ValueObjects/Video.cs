namespace XerifeTv.CMS.Modules.Abstractions.ValueObjects;

public record Video(
  string Url,
  long Duration,
  string StreamFormat,
  string? Subtitle = null);