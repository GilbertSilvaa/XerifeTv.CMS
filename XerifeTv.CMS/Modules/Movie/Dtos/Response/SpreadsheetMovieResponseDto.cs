using XerifeTv.CMS.Modules.Abstractions.Exceptions;
using XerifeTv.CMS.Modules.Abstractions.ValueObjects;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Modules.Movie.Dtos.Response;

public sealed class SpreadsheetMovieResponseDto
{
    public string ImdbId { get; private set; } = string.Empty;
    public int ParentalRating { get; private set; }
    public Video? Video { get; private set; }
    public string? MediaDeliveryProfileName { get; private set; }
    public string? MediaRoute { get; private set; }
    public string? MediaDeliveryProfileId { get; set; }

    public static SpreadsheetMovieResponseDto FromCollunsStr(string[] cols)
    {
        string? imdbId = cols[0];
        string? parentalRating = cols[1];
        string? mediaDeliveryProfileName = cols[2];
        string? mediaPath = cols[3];
        string? videoUrl = cols[4];
        string? videoStreamFormat = cols[5];
        string? videoSubtitleUrl = cols[6];

        List<string?> requiredValues = [
            imdbId,
            parentalRating,
        ];

        if (requiredValues.Any(string.IsNullOrEmpty))
            throw new SpreadsheetInvalidException($"[{imdbId}] algum campo obrigatorio esta vazio");

        if (!int.TryParse(parentalRating, out var parentalRatingResult))
            throw new SpreadsheetInvalidException($"[{imdbId}] classificacao indicativa em formato invalido");

        if (!ParentalRatingHelper.ParentalRatingList.Contains(parentalRatingResult))
            throw new SpreadsheetInvalidException($"[{imdbId}] classificacao indicativa invalida");

        if (!string.IsNullOrWhiteSpace(videoStreamFormat)
            && !StreamFormatsHelper.Streaming.Contains(videoStreamFormat)
            && !StreamFormatsHelper.Vod.Contains(videoStreamFormat))
            throw new SpreadsheetInvalidException($"[{imdbId}] stream format invalido");

        var hasMediaDeliveryProfile =
            !string.IsNullOrWhiteSpace(mediaDeliveryProfileName) &&
            !string.IsNullOrWhiteSpace(mediaPath);

        var hasFixedVideo =
            !string.IsNullOrWhiteSpace(videoUrl) &&
            !string.IsNullOrWhiteSpace(videoStreamFormat);

        if (!hasMediaDeliveryProfile && !hasFixedVideo)
            throw new SpreadsheetInvalidException( $"[{imdbId}] obrigatorio informar Media Delivery Profile/Media Path ou URL Video Fixed/Stream Format");

        return new SpreadsheetMovieResponseDto
        {
            ImdbId = imdbId,
            ParentalRating = parentalRatingResult,
            Video = new Video(videoUrl, 0, videoStreamFormat, videoSubtitleUrl),
            MediaDeliveryProfileName = mediaDeliveryProfileName,
            MediaRoute = mediaPath
        };
    }
}
