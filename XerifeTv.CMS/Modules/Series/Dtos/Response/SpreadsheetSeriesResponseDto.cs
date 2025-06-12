using XerifeTv.CMS.Modules.Abstractions.Exceptions;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Modules.Series.Dtos.Response;

public sealed class SpreadsheetSeriesResponseDto
{
	public string ImdbId { get; private set; } = string.Empty;
	public int ParentalRating { get; private set; }
	public string Title { get; private set; } = string.Empty;

	public static SpreadsheetSeriesResponseDto FromCollunsStr(string[] cols)
	{
		string? imdbId = cols[0];
		string? title = cols[1];
		string? parentalRating = cols[2];

		List<string?> requiredValues = [imdbId, title, parentalRating];

		if (requiredValues.Any(string.IsNullOrEmpty))
			throw new SpreadsheetInvalidException($"[{imdbId}] algum campo obrigatorio esta vazio");

		if (!int.TryParse(parentalRating, out var parentalRatingResult))
			throw new SpreadsheetInvalidException($"[{imdbId}] classificacao indicativa em formato invalido");

		if (!ParentalRatingHelper.ParentalRatingList.Contains(parentalRatingResult))
			throw new SpreadsheetInvalidException($"[{imdbId}] classificacao indicativa invalida");

		return new SpreadsheetSeriesResponseDto
		{
			ImdbId = imdbId,
			ParentalRating = parentalRatingResult,
			Title = title
		};
	}
}
