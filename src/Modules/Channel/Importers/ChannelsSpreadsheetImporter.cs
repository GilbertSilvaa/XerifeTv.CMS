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
    IChannelService service,
    ICacheService cacheService,
    ISpreadsheetReaderService spreadsheetReaderService,
    IMediaDeliveryProfileService mediaDeliveryProfileService) : ISpreadsheetBatchImporter<IChannelService>
{
    public async Task<Result<string>> ImportAsync(IFormFile file)
    {
        var importId = Guid.NewGuid().ToString();
        var emptyDto = new ImportSpreadsheetResponseDto(ErrorList: []);
        await cacheService.SetValueAsync(importId, TimeSpan.FromMinutes(160), emptyDto);

        _ = HandleImportAsync(file, importId);

        return Result<string>.Success(importId);
    }

    public async Task<Result<ImportSpreadsheetResponseDto>> MonitorImportAsync(string importId)
    {
        var response = await cacheService.GetValueAsync<ImportSpreadsheetResponseDto>(importId);

        if (response == null)
            return Result<ImportSpreadsheetResponseDto>.Failure(
                new Error("400", $"Import Id {importId} não encontrado"));

        return Result<ImportSpreadsheetResponseDto>.Success(response);
    }

    public async Task<Result<bool>> CancelImportAsync(string importId)
    {
        var response = await cacheService.GetValueAsync<ImportSpreadsheetResponseDto>(importId);

        if (response == null)
            return Result<bool>.Failure(new Error("400", $"Import Id {importId} não encontrado"));

        await cacheService.SetValueAsync($"cancelled_{importId}", TimeSpan.FromMinutes(60), true);
        return Result<bool>.Success(true);
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

            var spreadsheetResult = spreadsheetReaderService.Read(expectedColluns, stream);
            ICollection<SpreadsheetChannelResponseDto> channelList = [];

            async Task UpdateProgress()
            {
                bool importCancelled = await cacheService.GetValueAsync<bool>($"cancelled_{importId}") == true;

                var progressCount = (int)(((float)(failCount + successCount) / spreadsheetResult.Length) * 100);
                var _dto = new ImportSpreadsheetResponseDto(
                    TotalItemsCount: spreadsheetResult.Length,
                    SuccessCount: successCount,
                    FailCount: failCount,
                    ProcessedCount: successCount + failCount,
                    ErrorList: [.. errorList],
                    ProgressCount: progressCount,
                    IsCancelled: importCancelled);

                await cacheService.SetValueAsync(importId, TimeSpan.FromMinutes(5), _dto);

                if (importCancelled)
                    throw new OperationCanceledException("Importação cancelada pelo usuário");
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
                    await UpdateProgress();
                }
            }

            foreach (var channelItem in channelList)
            {
                if (!string.IsNullOrWhiteSpace(channelItem.MediaDeliveryProfileName))
                {
                    var mediaProfileResponse = await mediaDeliveryProfileService.GetByNameAsync(channelItem.MediaDeliveryProfileName);

                    if (mediaProfileResponse.IsFailure)
                    {
                        failCount++;
                        errorList.Add($"[{channelItem.Title[..8]}] {mediaProfileResponse.Error?.Description ?? string.Empty}");
                        await UpdateProgress();
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

                var response = await service.CreateAsync(createChannelDto);

                if (response.IsSuccess)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                    errorList.Add($"[{channelItem.Title[..8]}] {response.Error?.Description ?? string.Empty}");
                }

                await UpdateProgress();
            }
        }
        catch (OperationCanceledException)
        {
            var monitorResponse = await MonitorImportAsync(importId);

            if (monitorResponse.IsSuccess)
            {
                var currentProgress = monitorResponse.Data ?? new ImportSpreadsheetResponseDto(ErrorList: []);
                var errorList = currentProgress?.ErrorList.ToList() ?? [];

                var progress = currentProgress! with
                {
                    ErrorList = [.. errorList],
                    ProgressCount = 100,
                    IsCancelled = true
                };

                await cacheService.SetValueAsync(importId, TimeSpan.FromMinutes(5), progress);
            }
        }
        catch (Exception ex)
        {
            var monitorResponse = await MonitorImportAsync(importId);

            if (monitorResponse.IsSuccess)
            {
                var currentProgress = monitorResponse.Data ?? new ImportSpreadsheetResponseDto(ErrorList: []);
                var errorList = currentProgress?.ErrorList.ToList() ?? [];
                errorList.Add(ex.InnerException?.Message ?? ex.Message);

                var progress = currentProgress! with
                {
                    ErrorList = [.. errorList],
                    ProgressCount = 100
                };

                await cacheService.SetValueAsync(importId, TimeSpan.FromMinutes(5), progress);
            }
        }
    }
}

