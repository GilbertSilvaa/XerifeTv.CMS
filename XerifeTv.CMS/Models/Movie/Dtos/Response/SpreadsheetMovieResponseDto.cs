using XerifeTv.CMS.Models.Abstractions.Exceptions;
using XerifeTv.CMS.Models.Abstractions.ValueObjects;

namespace XerifeTv.CMS.Models.Movie.Dtos.Response;

public sealed class SpreadsheetMovieResponseDto
{
	public string ImdbId { get; private set; } = string.Empty;
	public int ParentalRating { get; private set; }
	public Video? Video { get; private set; }

	public static SpreadsheetMovieResponseDto FromCollunsStr(string[] cols)
	{
		string? imdbId = cols[0];
		string? parentalRating = cols[1];
		string? videoUrl = cols[2];
		string? videoStreamFormat = cols[3];
		string? videoDuration = cols[4];
		string? videoSubtitleUrl = cols[5];

		if (imdbId is null 
			|| videoUrl is null 
			|| videoDuration is null
			|| parentalRating is null 
			|| videoStreamFormat is null)
		{
			throw new SpreadsheetInvalidException("some mandatory field is empty");
		}
		
		if (!int.TryParse(parentalRating, out int parentalRatingResult))
			throw new SpreadsheetInvalidException("some parental rating value is not in integer format");

		if (!long.TryParse(videoDuration, out long videoDurationResult))
			throw new SpreadsheetInvalidException("some duration value is not in long format");

		return new SpreadsheetMovieResponseDto
		{
			ImdbId = imdbId,
			ParentalRating = parentalRatingResult,
			Video = new Video(videoUrl, videoDurationResult, videoStreamFormat, videoSubtitleUrl)
		};
	}
}
