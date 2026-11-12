using XerifeTv.CMS.Modules.Abstractions.ValueObjects;
using XerifeTv.CMS.Modules.People.Dtos.Request;

namespace XerifeTv.CMS.Modules.Movie.Dtos.Request;

public class UpdateMovieRequestDto
{
    public string Id { get; init; } = string.Empty;
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
    public string VideoUrl { get; init; } = string.Empty;
    public long VideoDuration { get; init; }
    public string VideoStreamFormat { get; init; } = string.Empty;
    public string? VideoSubtitle { get; init; }
    public string? MediaDeliveryProfileId { get; init; }
    public string? MediaRoute { get; init; }
    public string? TrailerVideoYoutubeId { get; init; }
    public bool Disabled { get; init; } = false;
    public ICollection<CreditRequestDto> Credits { get; init; } = [];

    public MovieEntity ToEntity()
    {
        var categorieList = Categories.Split(",").ToList()
          .Select(x => x.Trim())
          .Where(x => !string.IsNullOrEmpty(x))
          .ToList();

        return new MovieEntity
        {
            Id = Id,
            Title = Title,
            ImdbId = ImdbId,
            FranchiseId = string.IsNullOrWhiteSpace(FranchiseId) ? null : FranchiseId,
            Synopsis = Synopsis,
            Categories = categorieList,
            PosterUrl = PosterUrl,
            BannerUrl = BannerUrl,
            MobileBannerUrl = MobileBannerUrl,
            TitleImageUrl = TitleImageUrl,
            ReleaseYear = ReleaseYear,
            ParentalRating = ParentalRating,
            Review = Review,
            Video = new Video(VideoUrl, VideoDuration, VideoStreamFormat, VideoSubtitle),
            Disabled = Disabled,
            MediaRoute = MediaRoute,
            MediaDeliveryProfileId = MediaDeliveryProfileId,
            TrailerVideoYoutubeId = TrailerVideoYoutubeId,
            Credits = (Credits ?? []).Select(x => x.ToCredit()).ToList()
        };
    }
}
