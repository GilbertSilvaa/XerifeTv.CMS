using XerifeTv.CMS.Modules.Abstractions.Exceptions;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Common.Dtos;
using XerifeTv.CMS.Modules.Integrations.Imdb.Services;
using XerifeTv.CMS.Modules.Movie.Dtos.Request;
using XerifeTv.CMS.Modules.Movie.Dtos.Response;
using XerifeTv.CMS.Modules.Movie.Interfaces;

namespace XerifeTv.CMS.Modules.Movie.Importers;

public class MoviesSpreadsheetImporter(
  IMovieService _service,
  IImdbService _imdbService,
  ICacheService _cacheService,
  ISpreadsheetReaderService _spreadsheetReaderService) : ISpreadsheetBatchImporter<IMovieService>
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
			string[] expectedColluns =
			[
				"IMDB ID (REQUIRED)",
				"PARENTAL RATING (REQUIRED)",
				"URL VIDEO (REQUIRED)",
				"STREAM FORMAT (REQUIRED)",
				"URL SUBTITLES"
			];

			using var stream = new MemoryStream();
			file.CopyTo(stream);

			int successCount = 0;
			int failCount = 0;
			ICollection<string> errorList = [];

			var spreadsheetResult = _spreadsheetReaderService.Read(expectedColluns, stream);
			ICollection<SpreadsheetMovieResponseDto> movieList = [];

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
					var spreadsheetMovieDto = SpreadsheetMovieResponseDto.FromCollunsStr(item);
					movieList.Add(spreadsheetMovieDto);
				}
				catch (SpreadsheetInvalidException ex)
				{
					failCount++;
					errorList.Add(ex.Message);
					UpdateProgress();
				}
			}

			foreach (var movieItem in movieList)
			{
				var movieImdbAPIResponse = await _imdbService.GetMovieByImdbIdAsync(movieItem.ImdbId);

				if (movieImdbAPIResponse.IsFailure)
				{
					failCount++;
					errorList.Add(movieImdbAPIResponse.Error.Description ?? string.Empty);
					UpdateProgress();
					continue;
				}

				var movieByImdbIdResponse = await _service.GetByImdbIdAsync(movieItem.ImdbId);

				Result<string>? responseCreateOrUpdate = null;

				if (movieByImdbIdResponse.IsSuccess)
				{
					var updateMovieDto = new UpdateMovieRequestDto
					{
						Id = movieByImdbIdResponse.Data!.Id,
						ImdbId = movieItem.ImdbId,
						Title = movieByImdbIdResponse.Data!.Title,
						Synopsis = movieByImdbIdResponse.Data!.Synopsis,
						Categories = movieByImdbIdResponse.Data!.Categories,
						PosterUrl = movieByImdbIdResponse.Data!.PosterUrl,
						BannerUrl = movieByImdbIdResponse.Data!.BannerUrl,
						ReleaseYear = movieByImdbIdResponse.Data!.ReleaseYear,
						Review = movieByImdbIdResponse.Data!.Review,
						ParentalRating = movieItem.ParentalRating,
						VideoUrl = movieItem.Video?.Url ?? string.Empty,
						VideoDuration = movieByImdbIdResponse.Data!.Video?.Duration ?? 0,
						VideoStreamFormat = movieItem.Video?.StreamFormat ?? string.Empty,
						VideoSubtitle = movieItem.Video?.Subtitle
					};

					responseCreateOrUpdate = await _service.UpdateAsync(updateMovieDto);									
				}
				else
				{
					var createMovieDto = new CreateMovieRequestDto
					{
						ImdbId = movieItem.ImdbId,
						Title = movieImdbAPIResponse.Data?.Title ?? string.Empty,
						Synopsis = movieImdbAPIResponse.Data?.Overview ?? string.Empty,
						Categories = String.Join(", ", movieImdbAPIResponse?.Data?.Genres.Select(g => g.Name.ToLower()) ?? []),
						PosterUrl = movieImdbAPIResponse?.Data?.PosterUrl ?? string.Empty,
						BannerUrl = movieImdbAPIResponse?.Data?.BannerUrl ?? string.Empty,
						ReleaseYear = int.Parse(movieImdbAPIResponse?.Data?.ReleaseYear ?? "0"),
						Review = movieImdbAPIResponse?.Data?.VoteAverage ?? 0,
						ParentalRating = movieItem.ParentalRating,
						VideoUrl = movieItem.Video?.Url ?? string.Empty,
						VideoDuration = movieImdbAPIResponse?.Data?.DurationInSeconds ?? 0,
						VideoStreamFormat = movieItem.Video?.StreamFormat ?? string.Empty,
						VideoSubtitle = movieItem.Video?.Subtitle
					};

					responseCreateOrUpdate = await _service.CreateAsync(createMovieDto);
				}

				if (responseCreateOrUpdate.IsSuccess)
				{
					successCount++;
				}
				else
				{
					failCount++;
					errorList.Add(responseCreateOrUpdate.Error?.Description ?? string.Empty);
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