using XerifeTv.CMS.Modules.Abstractions.Exceptions;
using XerifeTv.CMS.Modules.Abstractions.ValueObjects;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Modules.Series.Dtos.Response;

public sealed class SpreadsheetEpisodeResponseDto
{
	public string SeriesImdbId { get; private set; } = string.Empty;
	public int Season { get; private set; }
	public int Episode { get; private set; }
	public string Title { get; private set; } = string.Empty;
	public string BannerUrl { get; private set; } = string.Empty;
	public Video? Video { get; private set; }

	public static SpreadsheetEpisodeResponseDto FromCollunsStr(string[] cols)
	{
		string? seriesImdbId = cols[0];
		string? season = cols[1];
		string? episode = cols[2];
		string? title = cols[3];
		string? bannerUrl = cols[4];
		string? videoUrl = cols[5];
		string? videoStreamFormat = cols[6];
		string? videoDuration = cols[7];
		string? videoSubtitleUrl = cols[8];

		List<string?> requiredValues = [
			seriesImdbId,
			season,
			episode,
			title,
			bannerUrl,
			videoUrl,
			videoStreamFormat,
			videoDuration,
		];

		if (requiredValues.Any(string.IsNullOrEmpty))
			throw new SpreadsheetInvalidException($"[{seriesImdbId}:EPs] algum campo obrigatorio esta vazio");

		if (!int.TryParse(season, out var seasonResult))
			throw new SpreadsheetInvalidException($"[{seriesImdbId}:EPs] season em formato invalido");

		if (!int.TryParse(episode, out var episodeResult))
			throw new SpreadsheetInvalidException($"[{seriesImdbId}:EPs] numero de episodio em formato invalido");

		if (!StreamFormatsHelper.Streaming.Contains(videoStreamFormat)
			&& !StreamFormatsHelper.Vod.Contains(videoStreamFormat))
			throw new SpreadsheetInvalidException($"[{seriesImdbId}:EPs] stream format invalido");

		if (!long.TryParse(videoDuration, out var videoDurationResult))
			throw new SpreadsheetInvalidException($"[{seriesImdbId}:EPs] duracao de video em formato invalido");

		return new SpreadsheetEpisodeResponseDto
		{
			Title = title,
			BannerUrl = bannerUrl,
			Season = seasonResult,
			Episode = episodeResult,
			SeriesImdbId = seriesImdbId,
			Video = new Video(videoUrl, videoDurationResult, videoStreamFormat, videoSubtitleUrl)
		};
	}
}
