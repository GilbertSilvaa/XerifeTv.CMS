using XerifeTv.CMS.Modules.Abstractions.Exceptions;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Common.Dtos;
using XerifeTv.CMS.Modules.Franchise.Interfaces;
using XerifeTv.CMS.Modules.Integrations.Imdb.Services;
using XerifeTv.CMS.Modules.Media.Delivery.Intefaces;
using XerifeTv.CMS.Modules.Series.Dtos.Request;
using XerifeTv.CMS.Modules.Series.Dtos.Response;
using XerifeTv.CMS.Modules.Series.Interfaces;

namespace XerifeTv.CMS.Modules.Series.Importers;

public class SeriesSpreadsheetImporter(
    ISeriesService service,
    IImdbService imdbService,
    ICacheService cacheService,
    ISpreadsheetReaderService spreadsheetReaderService,
    IMediaDeliveryProfileService mediaDeliveryProfileService,
    IFranchiseService franchiseService) : ISpreadsheetBatchImporter<ISeriesService>
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
            string[] expectedCollunsSeriesWorksheet =
            [
                "IMDB ID (REQUIRED)",
                "TITLE (REQUIRED)",
                "PARENTAL RATING (REQUIRED)",
                "TRAILER YOUTUBE VIDEO ID",
                "FRANCHISE"
            ];

            string[] expectedCollunsEpisodesWorksheet =
            [
                "SERIES IMDB ID (REQUIRED)",
                "SEASON (REQUIRED)",
                "EPISODE (REQUIRED)",
                "TITLE (REQUIRED)",
                "URL BANNER (REQUIRED)",
                "MEDIA DELIVERY PROFILE NAME",
                "MEDIA PATH",
                "URL VIDEO FIXED",
                "STREAM FORMAT",
                "DURATION INSECONDS (REQUIRED)",
                "URL SUBTITLES"
            ];

            using var stream = new MemoryStream();
            file.CopyTo(stream);

            int seriesSuccessCount = 0;
            int seriesFailCount = 0;
            int episodesSuccessCount = 0;
            int episodesFailCount = 0;
            ICollection<string> errorList = [];

            var spreadsheetSeriesResult = spreadsheetReaderService.Read(expectedCollunsSeriesWorksheet, stream, worksheetIndex: 0);
            var spreadsheetEpisodesResult = spreadsheetReaderService.Read(expectedCollunsEpisodesWorksheet, stream, worksheetIndex: 1);

            ICollection<SpreadsheetSeriesResponseDto> seriesList = [];
            ICollection<SpreadsheetEpisodeResponseDto> episodeList = [];

            async Task UpdateProgress()
            {
                bool importCancelled = await cacheService.GetValueAsync<bool>($"cancelled_{importId}") == true;

                var successCount = seriesSuccessCount + episodesSuccessCount;
                var failCount = seriesFailCount + episodesFailCount;
                var totalCount = spreadsheetSeriesResult.Length + spreadsheetEpisodesResult.Length;

                var progressCount = (int)(((float)(failCount + successCount) / totalCount) * 100);
                var _dto = new ImportSpreadsheetResponseDto(
                    TotalItemsCount: totalCount,
                    SuccessCount: successCount,
                    FailCount: failCount,
                    ProcessedCount: failCount + successCount,
                    ErrorList: [.. errorList],
                    ProgressCount: progressCount,
                    IsCancelled: importCancelled);

                await cacheService.SetValueAsync(importId, TimeSpan.FromMinutes(5), _dto);

                if (importCancelled)
                    throw new OperationCanceledException("Importação cancelada pelo usuário");
            }

            foreach (var item in spreadsheetSeriesResult)
            {
                try
                {
                    var spreadsheetSeriesDto = SpreadsheetSeriesResponseDto.FromCollunsStr(item);
                    seriesList.Add(spreadsheetSeriesDto);
                }
                catch (SpreadsheetInvalidException ex)
                {
                    seriesFailCount++;
                    errorList.Add(ex.Message);
                    await UpdateProgress();
                }
            }

            foreach (var item in spreadsheetEpisodesResult)
            {
                try
                {
                    var spreadsheetEpisodeDto = SpreadsheetEpisodeResponseDto.FromCollunsStr(item);
                    episodeList.Add(spreadsheetEpisodeDto);
                }
                catch (SpreadsheetInvalidException ex)
                {
                    episodesFailCount++;
                    errorList.Add(ex.Message);
                    await UpdateProgress();
                }
            }

            foreach (var seriesItem in seriesList)
            {
                if (!string.IsNullOrWhiteSpace(seriesItem.FranchiseName))
                {
                    var franchiseResponse = await franchiseService.GetByNameAsync(seriesItem.FranchiseName);

                    if (franchiseResponse.IsFailure)
                    {
                        seriesFailCount++;
                        errorList.Add($"[{seriesItem.ImdbId}] {franchiseResponse.Error.Description ?? string.Empty}");
                        await UpdateProgress();
                        continue;
                    }

                    seriesItem.FranchiseId = franchiseResponse.Data!.Id;
                }

                var seriesByImdbResponse = await imdbService.GetSeriesByImdbIdAsync(seriesItem.ImdbId);

                if (seriesByImdbResponse.IsFailure)
                {
                    seriesFailCount++;
                    errorList.Add($"[{seriesItem.ImdbId}] {seriesByImdbResponse.Error.Description ?? string.Empty}");
                    await UpdateProgress();
                    continue;
                }

                var createSeriesDto = new CreateSeriesRequestDto
                {
                    ImdbId = seriesItem.ImdbId,
                    Title = seriesItem.Title,
                    Synopsis = seriesByImdbResponse?.Data?.Overview ?? string.Empty,
                    Categories = String.Join(", ", seriesByImdbResponse?.Data?.Genres.Select(g => g.Name.ToLower()) ?? []),
                    PosterUrl = seriesByImdbResponse?.Data?.PosterUrl ?? string.Empty,
                    BannerUrl = seriesByImdbResponse?.Data?.BannerUrl ?? string.Empty,
                    ReleaseYear = int.Parse(seriesByImdbResponse?.Data?.ReleaseYear ?? "0"),
                    ParentalRating = seriesItem.ParentalRating,
                    Review = seriesByImdbResponse?.Data?.VoteAverage ?? 0,
                    NumberSeasons = seriesByImdbResponse?.Data?.NumberSeasons ?? 0,
                    TrailerVideoYoutubeId = seriesItem.TrailerVideoYoutubeId,
                    FranchiseId = seriesItem.FranchiseId
                };

                var response = await service.CreateAsync(createSeriesDto);

                if (response.IsSuccess)
                {
                    seriesSuccessCount++;
                }
                else
                {
                    seriesFailCount++;
                    errorList.Add($"[{seriesItem.ImdbId}] {response.Error?.Description ?? string.Empty}");
                }

                await UpdateProgress();
            }

            foreach (var item in episodeList)
            {
                if (!string.IsNullOrWhiteSpace(item.MediaDeliveryProfileName))
                {
                    var mediaProfileResponse = await mediaDeliveryProfileService.GetByNameAsync(item.MediaDeliveryProfileName);

                    if (mediaProfileResponse.IsFailure)
                    {
                        episodesFailCount++;
                        errorList.Add($"[{item.SeriesImdbId}:S{item.Season}E{item.Episode}] {mediaProfileResponse.Error.Description ?? string.Empty}");
                        await UpdateProgress();
                        continue;
                    }

                    item.MediaDeliveryProfileId = mediaProfileResponse.Data!.Id;
                }

                var seriesResult = await service.GetByImdbIdAsync(item.SeriesImdbId);

                if (seriesResult.IsFailure)
                {
                    episodesFailCount++;
                    errorList.Add($"[{item.SeriesImdbId}:S{item.Season}E{item.Episode}] {seriesResult.Error?.Description ?? string.Empty}");
                    await UpdateProgress();
                    continue;
                }

                var episodeResponse = await service.GetEpisodesBySeasonAsync(
                    serieId: seriesResult?.Data?.Id ?? string.Empty,
                    season: item.Season,
                    includeDisabled: true,
                    specificEpisode: item.Episode);

                Result<string>? responseCreateOrUpdate = null;

                if (episodeResponse.IsSuccess && episodeResponse.Data!.Episodes.Any())
                {
                    var episode = episodeResponse.Data.Episodes.First();

                    var updateEpisodeDto = new UpdateEpisodeRequestDto
                    {
                        Id = episode.Id,
                        SerieId = seriesResult?.Data?.Id ?? string.Empty,
                        Title = item.Title,
                        BannerUrl = item.BannerUrl,
                        Number = episode.Number,
                        Season = episode.Season,
                        VideoUrl = item.Video?.Url ?? episode.Video?.Url ?? string.Empty,
                        VideoDuration = item.Video?.Duration ?? episode.Video?.Duration ?? 0,
                        VideoStreamFormat = item.Video?.StreamFormat ?? episode.Video?.StreamFormat ?? string.Empty,
                        VideoSubtitle = item.Video?.Subtitle ?? episode.Video?.Subtitle ?? string.Empty,
                        MediaDeliveryProfileId = item.MediaDeliveryProfileId ?? episode.MediaDeliveryProfileId,
                        MediaRoute = item.MediaRoute ?? episode.MediaRoute,
                        Disabled = false
                    };

                    responseCreateOrUpdate = await service.UpdateEpisodeAsync(updateEpisodeDto);
                }
                else
                {
                    var createEpisodeDto = new CreateEpisodeRequestDto
                    {
                        SerieId = seriesResult?.Data?.Id ?? string.Empty,
                        Title = item.Title,
                        BannerUrl = item.BannerUrl,
                        Number = item.Episode,
                        Season = item.Season,
                        VideoUrl = item.Video?.Url ?? string.Empty,
                        VideoDuration = item.Video?.Duration ?? 0,
                        VideoStreamFormat = item.Video?.StreamFormat ?? string.Empty,
                        VideoSubtitle = item.Video?.Subtitle ?? string.Empty,
                        MediaDeliveryProfileId = item.MediaDeliveryProfileId,
                        MediaRoute = item.MediaRoute
                    };

                    responseCreateOrUpdate = await service.CreateEpisodeAsync(createEpisodeDto);
                }

                if (responseCreateOrUpdate.IsSuccess)
                {
                    episodesSuccessCount++;
                }
                else
                {
                    episodesFailCount++;
                    errorList.Add($"[{item.SeriesImdbId}:S{item.Season}E{item.Episode}] {responseCreateOrUpdate.Error?.Description ?? string.Empty}");
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

