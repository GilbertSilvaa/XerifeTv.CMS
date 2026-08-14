using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Integrations.Imdb.Services;
using XerifeTv.CMS.Modules.Series.Dtos.Request;
using XerifeTv.CMS.Modules.Series.Dtos.Response;
using XerifeTv.CMS.Modules.Series.Interfaces;

namespace XerifeTv.CMS.Modules.Series.Importers;

public class EpisodesImdbImporter(
    ISeriesService service,
    IImdbService imdbService,
    ICacheService cacheService) : IEpisodesImporter
{
    public async Task<Result<string>> ImportAsync(string seriesId)
    {
        var importId = Guid.NewGuid().ToString();
        var emptyDto = new ImportEpisodesResponseDto(0, 0, 0, 0);
        await cacheService.SetValueAsync(importId, TimeSpan.FromMinutes(160), emptyDto);

        _ = HandleImportAsync(seriesId, importId);

        return Result<string>.Success(importId);
    }

    public async Task<Result<ImportEpisodesResponseDto>> MonitorImportAsync(string importId)
    {
        var response = await cacheService.GetValueAsync<ImportEpisodesResponseDto>(importId);

        if (response == null)
            return Result<ImportEpisodesResponseDto>.Failure(
              new Error("400", $"Import Id {importId} não encontrado"));

        return Result<ImportEpisodesResponseDto>.Success(response);
    }

    public async Task<Result<bool>> CancelImportAsync(string importId)
    {
        var response = await cacheService.GetValueAsync<ImportEpisodesResponseDto>(importId);

        if (response == null)
            return Result<bool>.Failure(new Error("400", $"Import Id {importId} não encontrado"));

        await cacheService.SetValueAsync($"cancelled_{importId}", TimeSpan.FromMinutes(60), true);
        return Result<bool>.Success(true);
    }

    private async Task HandleImportAsync(string seriesId, string importId)
    {
        try
        {
            var seriesResult = await service.GetAsync(seriesId);
            if (seriesResult.IsFailure) throw new Exception(seriesResult.Error.Description);

            var seriesImdbResult = await imdbService.GetSeriesByImdbIdAsync(seriesResult.Data?.ImdbId ?? string.Empty);
            if (seriesImdbResult.IsFailure) throw new Exception(seriesImdbResult.Error.Description);

            var seriesEpisodesImdbCount = seriesImdbResult.Data?.NumberEpisodes ?? 0;
            var createdEpisodesCount = 0;
            var episodeCreationAttemptsCount = 0;

            async Task UpdateProgress()
            {
                bool importCancelled = await cacheService.GetValueAsync<bool>($"cancelled_{importId}") == true;

                var progressCount = (int)(((float)episodeCreationAttemptsCount / seriesEpisodesImdbCount) * 100);
                var _dto = new ImportEpisodesResponseDto(
                    TotalItemsCount: seriesEpisodesImdbCount,
                    ImportedCount: createdEpisodesCount,
                    ProgressCount: progressCount,
                    ProcessedCount: episodeCreationAttemptsCount,
                    IsCancelled: importCancelled);

                await cacheService.SetValueAsync(importId, TimeSpan.FromMinutes(5), _dto);

                if (importCancelled)
                    throw new OperationCanceledException("Importação cancelada pelo usuário");
            }

            for (int i = 1; i <= seriesImdbResult?.Data?.NumberSeasons; i++)
            {
                var result = await imdbService.GetSeriesEpisodesBySeasonAsync(seriesResult.Data!.ImdbId, i);
                if (result.IsFailure || result.Data == null) continue;

                foreach (var episode in result.Data.Episodes)
                {
                    var newEpisodeResult = await service.CreateEpisodeAsync(new CreateEpisodeRequestDto
                    {
                        SerieId = seriesResult.Data.Id,
                        Title = episode.Name,
                        BannerUrl = episode.BannerUrl,
                        Number = episode.EpisodeNumber,
                        Season = episode.SeasonNumber,
                        VideoDuration = episode.DurationInSeconds,
                        IsDisabled = true
                    });

                    if (newEpisodeResult.IsSuccess) createdEpisodesCount++;

                    episodeCreationAttemptsCount++;
                    await UpdateProgress();
                }
            }
        }
        catch (OperationCanceledException)
        {
            var monitorResponse = await MonitorImportAsync(importId);

            if (monitorResponse.IsSuccess)
            {
                var currentProgress = monitorResponse.Data ?? new ImportEpisodesResponseDto(0, 0, 0, 0);
                var progress = currentProgress with
                {
                    ProgressCount = 100,
                    IsCancelled = true
                };

                await cacheService.SetValueAsync(importId, TimeSpan.FromMinutes(5), progress);
            }
        }
        catch (Exception)
        {
            var monitorResponse = await MonitorImportAsync(importId);

            if (monitorResponse.IsSuccess)
            {
                var currentProgress = monitorResponse.Data ?? new ImportEpisodesResponseDto(0, 0, 0, 0);
                var progress = currentProgress with
                {
                    ProgressCount = 100
                };

                await cacheService.SetValueAsync(importId, TimeSpan.FromMinutes(5), progress);
            }
        }
    }
}

