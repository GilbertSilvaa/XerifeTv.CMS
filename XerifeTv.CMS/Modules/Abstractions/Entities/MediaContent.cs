namespace XerifeTv.CMS.Modules.Abstractions.Entities;

public abstract class MediaContent : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Synopsis { get; set; } = string.Empty;
    public string BannerUrl { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public int ReleaseYear { get; set; } = 1800;
    public int ParentalRating { get; set; } = 0;
}
