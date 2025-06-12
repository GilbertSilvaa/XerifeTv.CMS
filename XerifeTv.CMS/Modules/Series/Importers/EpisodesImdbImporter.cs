using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Integrations.Imdb.Services;
using XerifeTv.CMS.Modules.Series.Dtos.Request;
using XerifeTv.CMS.Modules.Series.Dtos.Response;
using XerifeTv.CMS.Modules.Series.Interfaces;

namespace XerifeTv.CMS.Modules.Series.Importers;

public class EpisodesImdbImporter(
    ISeriesService _service,
    IImdbService _imdbService,
	ICacheService _cacheService) : IEpisodesImporter
{
    public async Task<Result<string>> ImportAsync(string seriesId)
    {
		var importId = Guid.NewGuid().ToString();
		var emptyDto = new ImportEpisodesResponseDto(0, 0);
		_cacheService.SetValue<ImportEpisodesResponseDto>(importId, emptyDto);

		_ = HandleImportAsync(seriesId, importId);

		await Task.Delay(300);
		return Result<string>.Success(importId);
	}

	public async Task<Result<ImportEpisodesResponseDto>> MonitorImportAsync(string importId)
	{
		var response = _cacheService.GetValue<ImportEpisodesResponseDto>(importId);

		if (response == null)
			return Result<ImportEpisodesResponseDto>.Failure(
			  new Error("400", $"Import Id {importId} nao encontrado"));

		await Task.Delay(500);
		return Result<ImportEpisodesResponseDto>.Success(response);
	}

    private async Task HandleImportAsync(string seriesId, string importId)
    {
		try
		{
			var seriesResult = await _service.Get(seriesId);
			if (seriesResult.IsFailure) throw new Exception(seriesResult.Error.Description);

			var seriesImdbResult = await _imdbService.GetSeriesByImdbIdAsync(seriesResult.Data?.ImdbId ?? string.Empty);
			if (seriesImdbResult.IsFailure) throw new Exception(seriesImdbResult.Error.Description);

			var seriesEpisodesImdbCount = seriesImdbResult.Data?.NumberEpisodes ?? 0;
			var createdEpisodesCount = 0;
			var episodeCreationAttemptsCount = 0;

			void UpdateProgress()
			{
				var progressCount = (int)(((float) episodeCreationAttemptsCount / seriesEpisodesImdbCount) * 100);
				var _dto = new ImportEpisodesResponseDto(createdEpisodesCount, progressCount);
				_cacheService.SetValue<ImportEpisodesResponseDto>(importId, _dto);
			}

			for (int i = 1; i <= seriesResult.Data?.NumberSeasons; i++)
			{
				var result = await _imdbService.GetSeriesEpisodesBySeasonAsync(seriesResult.Data.ImdbId, i);
				if (result.IsFailure || result.Data == null) continue;

				foreach (var episode in result.Data.Episodes)
				{
					var newEpisodeResult = await _service.CreateEpisode(new CreateEpisodeRequestDto
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
					UpdateProgress();
				}
			}		
		}
		catch (Exception)
		{
			var monitorResponse = await MonitorImportAsync(importId);

			if (monitorResponse.IsSuccess)
			{
				var currentProgress = monitorResponse.Data;
				var _newDto = new ImportEpisodesResponseDto(currentProgress?.ImportedCount ?? 0, 100);
				_cacheService.SetValue<ImportEpisodesResponseDto>(importId, _newDto);
			}
		}
	}
}
