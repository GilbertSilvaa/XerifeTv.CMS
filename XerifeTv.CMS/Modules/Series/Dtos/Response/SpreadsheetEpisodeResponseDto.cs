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
    public string? MediaDeliveryProfileName { get; private set; }
    public string? MediaRoute { get; private set; }
    public string? MediaDeliveryProfileId { get; set; }

    public static SpreadsheetEpisodeResponseDto FromCollunsStr(string[] cols)
	{
		string? seriesImdbId = cols[0];
		string? season = cols[1];
		string? episode = cols[2];
		string? title = cols[3];
		string? bannerUrl = cols[4];
        string? mediaDeliveryProfileName = cols[5];
        string? mediaPath = cols[6];
        string? videoUrl = cols[7];
		string? videoStreamFormat = cols[8];
		string? videoDuration = cols[9];
		string? videoSubtitleUrl = cols[10];

		List<string?> requiredValues = [
			seriesImdbId,
			season,
			episode,
			title,
			bannerUrl,
			videoDuration,
		];

		if (requiredValues.Any(string.IsNullOrEmpty))
			throw new SpreadsheetInvalidException($"[{seriesImdbId}:S{season}E{episode}] algum campo obrigatorio esta vazio");

		if (!int.TryParse(season, out var seasonResult))
			throw new SpreadsheetInvalidException($"[{seriesImdbId}:S{season}E{episode}] season em formato invalido");

		if (!int.TryParse(episode, out var episodeResult))
			throw new SpreadsheetInvalidException($"[{seriesImdbId}:S{season}E{episode}] numero de episodio em formato invalido");

		if (!string.IsNullOrWhiteSpace(videoStreamFormat)
            && !StreamFormatsHelper.Streaming.Contains(videoStreamFormat)
			&& !StreamFormatsHelper.Vod.Contains(videoStreamFormat))
			throw new SpreadsheetInvalidException($"[{seriesImdbId}:S{season}E{episode}] stream format invalido");

		if (!long.TryParse(videoDuration, out var videoDurationResult))
			throw new SpreadsheetInvalidException($"[{seriesImdbId}:S{season}E{episode}] duracao de video em formato invalido");

        var hasMediaDeliveryProfile =
            !string.IsNullOrWhiteSpace(mediaDeliveryProfileName) &&
            !string.IsNullOrWhiteSpace(mediaPath);

        var hasFixedVideo =
            !string.IsNullOrWhiteSpace(videoUrl) &&
            !string.IsNullOrWhiteSpace(videoStreamFormat);

        if (!hasMediaDeliveryProfile && !hasFixedVideo)
            throw new SpreadsheetInvalidException($"[{seriesImdbId}:S{season}E{episode}] obrigatorio informar Media Delivery Profile/Media Path ou URL Video Fixed/Stream Format");


        return new SpreadsheetEpisodeResponseDto
		{
			Title = title,
			BannerUrl = bannerUrl,
			Season = seasonResult,
			Episode = episodeResult,
			SeriesImdbId = seriesImdbId,
			Video = new Video(videoUrl, videoDurationResult, videoStreamFormat, videoSubtitleUrl),
            MediaDeliveryProfileName = mediaDeliveryProfileName,
            MediaRoute = mediaPath
        };
	}
}
