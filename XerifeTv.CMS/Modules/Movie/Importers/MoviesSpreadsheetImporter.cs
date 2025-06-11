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
        var emptyDto = new ImportSpreadsheetResponseDto(0, 0, [], 0);
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

            var spreadsheetResponse = _spreadsheetReaderService.Read(expectedColluns, stream);
            ICollection<SpreadsheetMovieResponseDto> movieList = [];

            void UpdateProgress()
            {
                var progressCount = (int)(((float)(failCount + successCount) / spreadsheetResponse.Length) * 100);
                var _dto = new ImportSpreadsheetResponseDto(successCount, failCount, errorList.ToArray(), progressCount);
                _cacheService.SetValue<ImportSpreadsheetResponseDto>(importId, _dto);
            }

            foreach (var item in spreadsheetResponse)
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
                var movieByImdbResponse = await _imdbService.GetMovieByImdbIdAsync(movieItem.ImdbId);

                if (movieByImdbResponse.IsFailure)
                {
                    failCount++;
                    errorList.Add(movieByImdbResponse.Error.Description ?? string.Empty);
                    UpdateProgress();
                    continue;
                }

                var createMovieDto = new CreateMovieRequestDto
                {
                    ImdbId = movieItem.ImdbId,
                    Title = movieByImdbResponse.Data?.Title ?? string.Empty,
                    Synopsis = movieByImdbResponse.Data?.Overview ?? string.Empty,
                    Categories = String.Join(", ", movieByImdbResponse?.Data?.Genres.Select(g => g.Name.ToLower()) ?? []),
                    PosterUrl = movieByImdbResponse?.Data?.PosterUrl ?? string.Empty,
                    BannerUrl = movieByImdbResponse?.Data?.BannerUrl ?? string.Empty,
                    ReleaseYear = int.Parse(movieByImdbResponse?.Data?.ReleaseYear ?? "0"),
                    Review = movieByImdbResponse?.Data?.VoteAverage ?? 0,
                    ParentalRating = movieItem.ParentalRating,
                    VideoUrl = movieItem.Video?.Url ?? string.Empty,
                    VideoDuration = movieByImdbResponse?.Data?.DurationInSeconds ?? 0,
                    VideoStreamFormat = movieItem.Video?.StreamFormat ?? string.Empty,
                    VideoSubtitle = movieItem.Video?.Subtitle
                };

                var response = await _service.Create(createMovieDto);

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
                var failCount = currentProgress?.FailCount ?? 0;
                var errorList = currentProgress?.ErrorList.ToList() ?? [];
                errorList.Add(ex.InnerException?.Message ?? ex.Message);

                var _newDto = new ImportSpreadsheetResponseDto(
                  currentProgress?.SuccessCount, failCount, [.. errorList], 100);
                _cacheService.SetValue<ImportSpreadsheetResponseDto>(importId, _newDto);
            }
        }
    }
}