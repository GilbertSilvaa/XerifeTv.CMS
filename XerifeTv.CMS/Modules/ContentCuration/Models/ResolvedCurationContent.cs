using XerifeTv.CMS.Modules.ContentCuration.Enums;
using XerifeTv.CMS.Modules.Movie;
using XerifeTv.CMS.Modules.Series;

namespace XerifeTv.CMS.Modules.ContentCuration.Models;

public sealed record CurationContentReference(string ContentId, EContentType ContentType);

public sealed record ResolvedCurationContent(
    string ContentId,
    EContentType ContentType,
    MovieEntity? Movie,
    SeriesEntity? Series)
{
    public string Title => Movie?.Title ?? Series?.Title ?? string.Empty;
    public string PosterUrl => Movie?.PosterUrl ?? Series?.PosterUrl ?? string.Empty;
    public string BannerUrl => Movie?.BannerUrl ?? Series?.BannerUrl ?? string.Empty;
    public string Synopsis => Movie?.Synopsis ?? Series?.Synopsis ?? string.Empty;
    public int ReleaseYear => Movie?.ReleaseYear ?? Series?.ReleaseYear ?? 0;
    public float Review => Movie?.Review ?? Series?.Review ?? 0;
    public DateTime CreateAt => Movie?.CreateAt ?? Series?.CreateAt ?? DateTime.MinValue;
}
