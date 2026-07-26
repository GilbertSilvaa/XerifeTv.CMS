using XerifeTv.CMS.Modules.Abstractions.Exceptions;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Modules.Series.Dtos.Response;

public sealed class SpreadsheetSeriesResponseDto
{
    public string ImdbId { get; private set; } = string.Empty;
    public int ParentalRating { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? TrailerVideoYoutubeId { get; set; }
    public string? FranchiseName { get; set; }
    public string? FranchiseId { get; set; }

    public static SpreadsheetSeriesResponseDto FromCollunsStr(string[] cols)
    {
        string? imdbId = cols[0];
        string? title = cols[1];
        string? parentalRating = cols[2];
        string? trailerVideoYoutubeId = cols[3];
        string? franchiseName = cols[4];

        List<string?> requiredValues = [imdbId, title, parentalRating];

        if (requiredValues.Any(string.IsNullOrEmpty))
            throw new SpreadsheetInvalidException($"[{imdbId}] algum campo obrigatório está vazio");

        if (!int.TryParse(parentalRating, out var parentalRatingResult))
            throw new SpreadsheetInvalidException($"[{imdbId}] classificação indicativa em formato inválido");

        if (!ParentalRatingHelper.ParentalRatingList.Contains(parentalRatingResult))
            throw new SpreadsheetInvalidException($"[{imdbId}] classificação indicativa inválida");

        return new SpreadsheetSeriesResponseDto
        {
            ImdbId = imdbId,
            ParentalRating = parentalRatingResult,
            Title = title,
            TrailerVideoYoutubeId = trailerVideoYoutubeId,
            FranchiseName = franchiseName,
        };
    }
}
