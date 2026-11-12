using XerifeTv.CMS.Modules.People.Dtos.Request;
using XerifeTv.CMS.Modules.Series;

namespace XerifeTv.CMS.Modules.Series.Dtos.Request;

public class CreateSeriesRequestDto
{
    public string ImdbId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Synopsis { get; init; } = string.Empty;
    public string Categories { get; init; } = string.Empty;
    public string? FranchiseId { get; init; }
    public string PosterUrl { get; init; } = string.Empty;
    public string BannerUrl { get; init; } = string.Empty;
    public string MobileBannerUrl { get; init; } = string.Empty;
    public string? TitleImageUrl { get; init; }
    public int ReleaseYear { get; init; }
    public int ParentalRating { get; init; }
    public float Review { get; init; }
    public int NumberSeasons { get; init; }
    public string? TrailerVideoYoutubeId { get; init; }
    public ICollection<CreditRequestDto> Credits { get; init; } = [];

    public SeriesEntity ToEntity()
    {
        var categorieList = Categories.Split(",").ToList()
          .Select(x => x.Trim())
          .Where(x => !string.IsNullOrEmpty(x))
          .ToList();

        return new SeriesEntity
        {
            ImdbId = ImdbId,
            FranchiseId = string.IsNullOrWhiteSpace(FranchiseId) ? null : FranchiseId,
            Title = Title,
            Synopsis = Synopsis,
            Categories = categorieList,
            PosterUrl = PosterUrl,
            BannerUrl = BannerUrl,
            MobileBannerUrl = MobileBannerUrl,
            TitleImageUrl = TitleImageUrl,
            ReleaseYear = ReleaseYear,
            ParentalRating = ParentalRating,
            NumberSeasons = NumberSeasons,
            Review = Review,
            TrailerVideoYoutubeId = TrailerVideoYoutubeId,
            Credits = (Credits ?? []).Select(x => x.ToCredit()).ToList()
        };
    }
}
