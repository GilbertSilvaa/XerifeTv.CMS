using XerifeTv.CMS.Modules.Abstractions.Exceptions;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Common.Dtos;
using XerifeTv.CMS.Modules.Integrations.Imdb.Services;
using XerifeTv.CMS.Modules.Series.Dtos.Request;
using XerifeTv.CMS.Modules.Series.Dtos.Response;
using XerifeTv.CMS.Modules.Series.Interfaces;

namespace XerifeTv.CMS.Modules.Series.Importers;

public class SeriesSpreadsheetImporter(
	ISeriesService _service,
	IImdbService _imdbService,
	ICacheService _cacheService,
	ISpreadsheetReaderService _spreadsheetReaderService) : ISpreadsheetBatchImporter<ISeriesService>
{
	public async Task<Result<string>> ImportAsync(IFormFile file)
	{
		var importId = Guid.NewGuid().ToString();
		var emptyDto = new ImportSpreadsheetResponseDto(0, 0, 0, 0, [], 0);
		_cacheService.SetValue<ImportSpreadsheetResponseDto>(importId, emptyDto);

		_ = HandleImportAsync(file, importId);

		await Task.Delay(300);
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
			string[] expectedCollunsSeriesWorksheet =
			[
				"IMDB ID (REQUIRED)",
				"TITLE (REQUIRED)",
				"PARENTAL RATING (REQUIRED)"
			];

			string[] expectedCollunsEpisodesWorksheet =
			[
				"SERIES IMDB ID (REQUIRED)",
				"SEASON (REQUIRED)",
				"EPISODE (REQUIRED)",
				"TITLE (REQUIRED)",
				"URL BANNER (REQUIRED)",
				"URL VIDEO (REQUIRED)",
				"STREAM FORMAT (REQUIRED)",
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

			var spreadsheetSeriesResult = _spreadsheetReaderService.Read(expectedCollunsSeriesWorksheet, stream, worksheetIndex: 0);
			var spreadsheetEpisodesResult = _spreadsheetReaderService.Read(expectedCollunsEpisodesWorksheet, stream, worksheetIndex: 1);

			ICollection<SpreadsheetSeriesResponseDto> seriesList = [];
			ICollection<SpreadsheetEpisodeResponseDto> episodeList = [];

			void UpdateProgress()
			{
				var successCount = seriesSuccessCount + episodesSuccessCount;
				var failCount = seriesFailCount + episodesFailCount;
				var totalCount = spreadsheetSeriesResult.Length + spreadsheetEpisodesResult.Length;

				var progressCount = (int)(((float)(failCount + successCount) / totalCount) * 100);
				var _dto = new ImportSpreadsheetResponseDto(
					TotalItemsCount: totalCount,
					SuccessCount: seriesSuccessCount,
					FailCount: failCount,
					ProcessedCount: failCount + successCount,
					ErrorList: [.. errorList],
					ProgressCount: progressCount);

				_cacheService.SetValue<ImportSpreadsheetResponseDto>(importId, _dto);
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
					UpdateProgress();
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
					UpdateProgress();
				}
			}

			foreach (var seriesItem in seriesList)
			{
				var seriesByImdbResponse = await _imdbService.GetSeriesByImdbIdAsync(seriesItem.ImdbId);

				if (seriesByImdbResponse.IsFailure)
				{
					seriesFailCount++;
					errorList.Add(seriesByImdbResponse.Error.Description ?? string.Empty);
					UpdateProgress();
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
					NumberSeasons = seriesByImdbResponse?.Data?.NumberSeasons ?? 0
				};

				var response = await _service.CreateAsync(createSeriesDto);

				if (response.IsSuccess)
				{
					seriesSuccessCount++;
				}
				else
				{
					seriesFailCount++;
					errorList.Add(response.Error?.Description ?? string.Empty);
				}

				UpdateProgress();
				await Task.Delay(500);
			}

			foreach (var item in episodeList)
			{
				var seriesResult = await _service.GetByImdbIdAsync(item.SeriesImdbId);

				if (seriesResult.IsFailure)
				{
					episodesFailCount++;
					errorList.Add(seriesResult.Error.Description ?? string.Empty);
					UpdateProgress();
					continue;
				}

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
					VideoSubtitle = item.Video?.Subtitle ?? string.Empty
				};

				var response = await _service.CreateEpisodeAsync(createEpisodeDto);

				if (response.IsSuccess)
				{
					episodesSuccessCount++;
				}
				else
				{
					episodesFailCount++;
					errorList.Add(response.Error?.Description ?? string.Empty);
				}

				UpdateProgress();
				await Task.Delay(500);
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
