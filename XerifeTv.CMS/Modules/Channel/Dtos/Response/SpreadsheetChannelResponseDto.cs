using XerifeTv.CMS.Modules.Abstractions.Exceptions;
using XerifeTv.CMS.Modules.Abstractions.ValueObjects;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Modules.Channel.Dtos.Response;

public class SpreadsheetChannelResponseDto
{
	public string Title { get; init; } = string.Empty;
	public string Categories { get; init; } = string.Empty;
	public string LogoUrl { get; init; } = string.Empty;
	public Video? Video { get; private set; }
    public string? MediaDeliveryProfileName { get; private set; }
    public string? MediaRoute { get; private set; }
    public string? MediaDeliveryProfileId { get; set; }

    public static SpreadsheetChannelResponseDto FromCollunsStr(string[] cols)
	{
		string? title = cols[0];
		string? categories = cols[1];
		string? logoUrl = cols[2];
        string? mediaDeliveryProfileName = cols[3];
        string? mediaPath = cols[4];
        string? videoUrl = cols[5];
		string? videoStreamFormat = cols[6];

		List<string?> requiredValues = [
			title,
			logoUrl, 
			categories,
		];

		if (requiredValues.Any(string.IsNullOrEmpty))
			throw new SpreadsheetInvalidException($"[{title[..8]}] algum campo obrigatorio esta vazio");
		
		if (!string.IsNullOrWhiteSpace(videoStreamFormat)
            && !StreamFormatsHelper.Streaming.Contains(videoStreamFormat) 
		    && !StreamFormatsHelper.Vod.Contains(videoStreamFormat))
			throw new SpreadsheetInvalidException($"[{title[..8]}] stream format invalido");

        var hasMediaDeliveryProfile =
            !string.IsNullOrWhiteSpace(mediaDeliveryProfileName) &&
            !string.IsNullOrWhiteSpace(mediaPath);

        var hasFixedVideo =
            !string.IsNullOrWhiteSpace(videoUrl) &&
            !string.IsNullOrWhiteSpace(videoStreamFormat);

        if (!hasMediaDeliveryProfile && !hasFixedVideo)
            throw new SpreadsheetInvalidException($"[{title[..8]}] obrigatorio informar Media Delivery Profile/Media Path ou URL Video Fixed/Stream Format");


        return new SpreadsheetChannelResponseDto
		{
			Title = title,
			Categories = categories,
			LogoUrl = logoUrl,
			Video = new Video(videoUrl, 0, videoStreamFormat),
            MediaDeliveryProfileName = mediaDeliveryProfileName,
            MediaRoute = mediaPath
        };
	}
}