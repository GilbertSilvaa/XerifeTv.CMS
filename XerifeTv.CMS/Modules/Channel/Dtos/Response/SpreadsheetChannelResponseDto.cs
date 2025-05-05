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
	
	public static SpreadsheetChannelResponseDto FromCollunsStr(string[] cols)
	{
		string? title = cols[0];
		string? categories = cols[1];
		string? logoUrl = cols[2];
		string? videoUrl = cols[3];
		string? videoStreamFormat = cols[4];

		List<string?> requiredValues = [
			title, 
			videoUrl, 
			logoUrl, 
			categories,
			videoStreamFormat
		];

		if (requiredValues.Any(v => string.IsNullOrEmpty(v)))
			throw new SpreadsheetInvalidException($"[{title.Substring(0, 8)}] algum campo obrigatorio esta vazio");
		
		if (!StreamFormatsHelper.Streaming.Contains(videoStreamFormat) 
		    && !StreamFormatsHelper.Vod.Contains(videoStreamFormat))
			throw new SpreadsheetInvalidException($"[{title.Substring(0, 8)}] stream format invalido"); 

		return new SpreadsheetChannelResponseDto
		{
			Title = title,
			Categories = categories,
			LogoUrl = logoUrl,
			Video = new Video(videoUrl, 0, videoStreamFormat)
		};
	}
}