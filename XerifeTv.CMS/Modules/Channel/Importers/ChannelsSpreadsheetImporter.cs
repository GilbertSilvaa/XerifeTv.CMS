using XerifeTv.CMS.Modules.Abstractions.Exceptions;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Channel.Dtos.Request;
using XerifeTv.CMS.Modules.Channel.Dtos.Response;
using XerifeTv.CMS.Modules.Channel.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Common.Dtos;
using XerifeTv.CMS.Modules.Media.Delivery.Intefaces;

namespace XerifeTv.CMS.Modules.Channel.Importers;

public class ChannelsSpreadsheetImporter(
	IChannelService _service,
	ICacheService _cacheService,
	ISpreadsheetReaderService _spreadsheetReaderService,
    IMediaDeliveryProfileService _mediaDeliveryProfileService) : ISpreadsheetBatchImporter<IChannelService>
{
	public async Task<Result<string>> ImportAsync(IFormFile file)
	{
		var importId = Guid.NewGuid().ToString();
		var emptyDto = new ImportSpreadsheetResponseDto(0, 0, 0, 0, [], 0);
		_cacheService.SetValue<ImportSpreadsheetResponseDto>(importId, emptyDto);

		_ = HandleImportAsync(file, importId);

		await Task.Delay(500);
		return Result<string>.Success(importId);
	}

	public async Task<Result<ImportSpreadsheetResponseDto>> MonitorImportAsync(string importId)
	{
		var response = _cacheService.GetValue<ImportSpreadsheetResponseDto>(importId);

		if (response == null)
			return Result<ImportSpreadsheetResponseDto>.Failure(
				new Error("400", $"Import Id {importId} nao encontrado"));

		await Task.Delay(500);
		return Result<ImportSpreadsheetResponseDto>.Success(response);
	}

	private async Task HandleImportAsync(IFormFile file, string importId)
	{
		try
		{
			string[] expectedColluns =
			[
				"TITLE (REQUIRED)",
				"CATEGORIES (REQUIRED)",
				"URL LOGO (REQUIRED)",
                "MEDIA DELIVERY PROFILE NAME",
                "MEDIA PATH",
				"URL VIDEO FIXED",
				"STREAM FORMAT"
			];

			using var stream = new MemoryStream();
			file.CopyTo(stream);

			int successCount = 0;
			int failCount = 0;
			ICollection<string> errorList = [];

			var spreadsheetResult = _spreadsheetReaderService.Read(expectedColluns, stream);
			ICollection<SpreadsheetChannelResponseDto> channelList = [];

			void UpdateProgress()
			{
				var progressCount = (int)(((float)(failCount + successCount) / spreadsheetResult.Length) * 100);
				var _dto = new ImportSpreadsheetResponseDto(
					TotalItemsCount: spreadsheetResult.Length,
					SuccessCount: successCount,
					FailCount: failCount,
					ProcessedCount: successCount + failCount,
					ErrorList: [.. errorList],
					ProgressCount: progressCount);

				_cacheService.SetValue<ImportSpreadsheetResponseDto>(importId, _dto);
			}

			foreach (var item in spreadsheetResult)
			{
				try
				{
					var spreadsheetChannelDto = SpreadsheetChannelResponseDto.FromCollunsStr(item);
					channelList.Add(spreadsheetChannelDto);
				}
				catch (SpreadsheetInvalidException ex)
				{
					failCount++;
					errorList.Add(ex.Message);
					UpdateProgress();
				}
			}

			foreach (var channelItem in channelList)
			{
                if (!string.IsNullOrWhiteSpace(channelItem.MediaDeliveryProfileName))
                {
                    var mediaProfileResponse = await _mediaDeliveryProfileService.GetByNameAsync(channelItem.MediaDeliveryProfileName);

                    if (mediaProfileResponse.IsFailure)
                    {
                        failCount++;
						errorList.Add($"[{channelItem.Title[..8]}] {mediaProfileResponse.Error?.Description ?? string.Empty}");
                        UpdateProgress();
                        continue;
                    }

                    channelItem.MediaDeliveryProfileId = mediaProfileResponse.Data!.Id;
                }

                var createChannelDto = new CreateChannelRequestDto
				{
					Title = channelItem.Title,
					Categories = channelItem.Categories,
					VideoStreamFormat = channelItem.Video?.StreamFormat ?? string.Empty,
					LogoUrl = channelItem.LogoUrl,
					VideoUrl = channelItem.Video?.Url ?? string.Empty,
					MediaDeliveryProfileId = channelItem.MediaDeliveryProfileId,
					MediaRoute = channelItem.MediaRoute
				};

				var response = await _service.CreateAsync(createChannelDto);

				if (response.IsSuccess)
				{
					successCount++;
				}
				else
				{
					failCount++;
                    errorList.Add($"[{channelItem.Title[..8]}] {response.Error?.Description ?? string.Empty}");
                }

				UpdateProgress();
				await Task.Delay(1200);
			}
		}
		catch (Exception ex)
		{
			var monitorResponse = await MonitorImportAsync(importId);

			if (monitorResponse.IsSuccess)
			{
				var currentProgress = monitorResponse.Data;
				var failCount = currentProgress?.FailCount ?? 0;
				var errorList = currentProgress?.ErrorList.ToList() ?? [];
				errorList.Add(ex.InnerException?.Message ?? ex.Message);
				
				var _newDto = new ImportSpreadsheetResponseDto(
					TotalItemsCount: currentProgress?.TotalItemsCount,
					SuccessCount: currentProgress?.SuccessCount,
					FailCount: failCount,
					ProcessedCount: currentProgress?.ProcessedCount,
					ErrorList: [.. errorList],
					ProgressCount: 100);

				_cacheService.SetValue<ImportSpreadsheetResponseDto>(importId, _newDto);
			}
		}
	}
}