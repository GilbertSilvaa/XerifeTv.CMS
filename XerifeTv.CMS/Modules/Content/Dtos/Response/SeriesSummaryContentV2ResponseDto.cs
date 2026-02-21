using XerifeTv.CMS.Modules.Series;

namespace XerifeTv.CMS.Modules.Content.Dtos.Response;

public class SeriesSummaryContentV2ResponseDto
{
    public string Id { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string[] Categories { get; private set; } = [];
    public string PosterURL { get; private set; } = string.Empty;
    public string BannerURL { get; private set; } = string.Empty;
    public string ParentalRating { get; private set; } = string.Empty;
    public int ReleaseYear { get; private set; }
    public double RatingImdb { get; private set; }
    public string Synopsis { get; private set; } = string.Empty;
    public int TotalSeasons { get; private set; }
    public bool HasSubtitles { get; private set; }

    public static SeriesSummaryContentV2ResponseDto FromEntity(SeriesEntity entity)
    {
        return new()
        {
            Id = entity.Id,
            Title = entity.Title,
            Categories = [.. entity.Categories],
            PosterURL = entity.PosterUrl,
            BannerURL = entity.BannerUrl,
            ParentalRating = entity.ParentalRating == 0 ? "L" : entity.ParentalRating.ToString(),
            ReleaseYear = entity.ReleaseYear,
            RatingImdb = entity.Review,
            Synopsis = entity.Synopsis,
            TotalSeasons = entity.NumberSeasons,
            HasSubtitles = false
        };
    }
}
