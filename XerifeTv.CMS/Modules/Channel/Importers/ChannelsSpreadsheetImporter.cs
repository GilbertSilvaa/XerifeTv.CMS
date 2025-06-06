using XerifeTv.CMS.Modules.Abstractions.Exceptions;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Channel.Dtos.Request;
using XerifeTv.CMS.Modules.Channel.Dtos.Response;
using XerifeTv.CMS.Modules.Channel.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Common.Dtos;

namespace XerifeTv.CMS.Modules.Channel.Importers;

public class ChannelsSpreadsheetImporter(
    IChannelService _service,
    ICacheService _cacheService,
    ISpreadsheetReaderService _spreadsheetReaderService) : ISpreadsheetBatchImporter<IChannelService>
{
    public async Task<Result<string>> ImportAsync(IFormFile file)
    {
        var importId = Guid.NewGuid().ToString();
        var emptyDto = new ImportSpreadsheetResponseDto(0, 0, [], 0);
        _cacheService.SetValue<ImportSpreadsheetResponseDto>(importId, emptyDto);

        HandleImportAsync(file, importId);
        return Result<string>.Success(importId);
    }

    public async Task<Result<ImportSpreadsheetResponseDto>> MonitorImportAsync(string importId)
    {
        var response = _cacheService.GetValue<ImportSpreadsheetResponseDto>(importId);

        if (response == null)
            return Result<ImportSpreadsheetResponseDto>.Failure(
                new Error("400", $"Import Id {importId} nao encontrado"));

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
                "URL VIDEO (REQUIRED)",
                "STREAM FORMAT (REQUIRED)"
            ];

            using var stream = new MemoryStream();
            file.CopyTo(stream);

            int successCount = 0;
            int failCount = 0;
            ICollection<string> errorList = [];

            var spreadsheetResponse = _spreadsheetReaderService.Read(expectedColluns, stream);
            ICollection<SpreadsheetChannelResponseDto> channelList = [];

            Action UpdateProgress = () =>
            {
                var progressCount = (int)(((float)(failCount + successCount) / spreadsheetResponse.Length) * 100);
                var _dto = new ImportSpreadsheetResponseDto(successCount, failCount, errorList.ToArray(), progressCount);
                _cacheService.SetValue<ImportSpreadsheetResponseDto>(importId, _dto);
            };

            foreach (var item in spreadsheetResponse)
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
                var createChannelDto = new CreateChannelRequestDto
                {
                    Title = channelItem.Title,
                    Categories = channelItem.Categories,
                    VideoStreamFormat = channelItem.Video.StreamFormat,
                    LogoUrl = channelItem.LogoUrl,
                    VideoUrl = channelItem.Video.Url
                };

                var response = await _service.Create(createChannelDto);

                if (response.IsSuccess)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                    errorList.Add(response.Error?.Description ?? string.Empty);
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
                var failCount = currentProgress.FailCount;
                var errorList = currentProgress.ErrorList.ToList();
                errorList.Add(ex.InnerException?.Message ?? ex.Message);

                var _newDto = new ImportSpreadsheetResponseDto(
                  currentProgress.SuccessCount, failCount, errorList.ToArray(), 100);
                _cacheService.SetValue<ImportSpreadsheetResponseDto>(importId, _newDto);
            }
        }
    }
}