using XerifeTv.CMS.Modules.Abstractions.Exceptions;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Common.Dtos;
using XerifeTv.CMS.Modules.Franchise.Interfaces;
using XerifeTv.CMS.Modules.Integrations.Imdb.Services;
using XerifeTv.CMS.Modules.Media.Delivery.Intefaces;
using XerifeTv.CMS.Modules.Movie.Dtos.Request;
using XerifeTv.CMS.Modules.Movie.Dtos.Response;
using XerifeTv.CMS.Modules.Movie.Interfaces;

namespace XerifeTv.CMS.Modules.Movie.Importers;

public class MoviesSpreadsheetImporter(
  IMovieService service,
  IImdbService imdbService,
  ICacheService cacheService,
  ISpreadsheetReaderService spreadsheetReaderService,
  IMediaDeliveryProfileService mediaDeliveryProfileService,
  IFranchiseService franchiseService) : ISpreadsheetBatchImporter<IMovieService>
{
    public async Task<Result<string>> ImportAsync(IFormFile file)
    {
        var importId = Guid.NewGuid().ToString();
        var emptyDto = new ImportSpreadsheetResponseDto(ErrorList: []);
        cacheService.SetValue<ImportSpreadsheetResponseDto>(importId, emptyDto);

        _ = HandleImportAsync(file, importId);

        return Result<string>.Success(importId);
    }

    public async Task<Result<ImportSpreadsheetResponseDto>> MonitorImportAsync(string importId)
    {
        var response = cacheService.GetValue<ImportSpreadsheetResponseDto>(importId);

        if (response == null)
            return Result<ImportSpreadsheetResponseDto>.Failure(
              new Error("400", $"Import Id {importId} não encontrado"));

        return Result<ImportSpreadsheetResponseDto>.Success(response);
    }

    public async Task<Result<bool>> CancelImportAsync(string importId)
    {
        var response = cacheService.GetValue<ImportSpreadsheetResponseDto>(importId);

        if (response == null)
            return Result<bool>.Failure(new Error("400", $"Import Id {importId} não encontrado"));

        cacheService.SetValue<bool>($"cancelled_{importId}", true);
        return Result<bool>.Success(true);
    }

    private async Task HandleImportAsync(IFormFile file, string importId)
    {
        try
        {
            string[] expectedColluns =
            [
                "IMDB ID (REQUIRED)",
                "PARENTAL RATING (REQUIRED)",
                "MEDIA DELIVERY PROFILE NAME",
                "MEDIA PATH",
                "URL VIDEO FIXED",
                "STREAM FORMAT",
                "URL SUBTITLES",
                "TRAILER YOUTUBE VIDEO ID",
                "FRANCHISE"
            ];

            using var stream = new MemoryStream();
            file.CopyTo(stream);

            int successCount = 0;
            int failCount = 0;
            ICollection<string> errorList = [];

            var spreadsheetResult = spreadsheetReaderService.Read(expectedColluns, stream);
            ICollection<SpreadsheetMovieResponseDto> movieList = [];

            void UpdateProgress()
            {
                bool importCancelled = cacheService.GetValue<bool>($"cancelled_{importId}") == true;

                var progressCount = (int)(((float)(failCount + successCount) / spreadsheetResult.Length) * 100);

                var _dto = new ImportSpreadsheetResponseDto(
                    TotalItemsCount: spreadsheetResult.Length,
                    SuccessCount: successCount,
                    FailCount: failCount,
                    ProcessedCount: successCount + failCount,
                    ErrorList: [.. errorList],
                    ProgressCount: progressCount,
                    IsCancelled: importCancelled);

                cacheService.SetValue<ImportSpreadsheetResponseDto>(importId, _dto);

                if (importCancelled)
                    throw new OperationCanceledException("Importação cancelada pelo usuário");
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
                if (!string.IsNullOrWhiteSpace(movieItem.MediaDeliveryProfileName))
                {
                    var mediaProfileResponse = await mediaDeliveryProfileService.GetByNameAsync(movieItem.MediaDeliveryProfileName);

                    if (mediaProfileResponse.IsFailure)
                    {
                        failCount++;
                        errorList.Add($"[{movieItem.ImdbId}] {mediaProfileResponse.Error?.Description ?? string.Empty}");
                        UpdateProgress();
                        continue;
                    }

                    movieItem.MediaDeliveryProfileId = mediaProfileResponse.Data!.Id;
                }

                if (!string.IsNullOrWhiteSpace(movieItem.FranchiseName))
                {
                    var franchiseResponse = await franchiseService.GetByNameAsync(movieItem.FranchiseName);

                    if (franchiseResponse.IsFailure)
                    {
                        failCount++;
                        errorList.Add($"[{movieItem.ImdbId}] {franchiseResponse.Error?.Description ?? string.Empty}");
                        UpdateProgress();
                        continue;
                    }

                    movieItem.FranchiseId = franchiseResponse.Data!.Id;
                }

                var movieImdbAPIResponse = await imdbService.GetMovieByImdbIdAsync(movieItem.ImdbId);

                if (movieImdbAPIResponse.IsFailure)
                {
                    failCount++;
                    errorList.Add($"[{movieItem.ImdbId}] {movieImdbAPIResponse.Error?.Description ?? string.Empty}");
                    UpdateProgress();
                    continue;
                }

                var movieByImdbIdResponse = await service.GetByImdbIdAsync(movieItem.ImdbId);

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
                        VideoSubtitle = movieItem.Video?.Subtitle,
                        MediaDeliveryProfileId = movieItem.MediaDeliveryProfileId,
                        MediaRoute = movieItem.MediaRoute,
                        TrailerVideoYoutubeId = movieItem.TrailerVideoYoutubeId,
                        FranchiseId = movieItem.FranchiseId
                    };

                    responseCreateOrUpdate = await service.UpdateAsync(updateMovieDto);
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
                        VideoSubtitle = movieItem.Video?.Subtitle,
                        MediaDeliveryProfileId = movieItem.MediaDeliveryProfileId,
                        MediaRoute = movieItem.MediaRoute,
                        TrailerVideoYoutubeId = movieItem.TrailerVideoYoutubeId,
                        FranchiseId = movieItem.FranchiseId
                    };

                    responseCreateOrUpdate = await service.CreateAsync(createMovieDto);
                }

                if (responseCreateOrUpdate.IsSuccess)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                    errorList.Add($"[{movieItem.ImdbId}] {responseCreateOrUpdate.Error?.Description ?? string.Empty}");
                }

                UpdateProgress();
                await Task.Delay(1200);
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

                cacheService.SetValue<ImportSpreadsheetResponseDto>(importId, progress);
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

                cacheService.SetValue<ImportSpreadsheetResponseDto>(importId, progress);
            }
        }
    }

}