using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Response;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Series.Interfaces;
using XerifeTv.CMS.Modules.User.Interfaces;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue;

public class BackgroundJobQueueService(
    IStorageFilesService _storageFilesService,
    IBackgroundJobQueueRepository _repository,
    ISeriesService _seriesService,
    IUserService _userService) : IBackgroundJobQueueService
{
    public async Task<Result<AddJobQueueResponseDto>> AddJobInQueue(AddSpreadsheetJobQueueRequestDto dto)
    {
        try
        {
            if (dto.SpreadsheetFile == null)
                return Result<AddJobQueueResponseDto>.Failure(new Error("400", "O arquivo de planilha invalido"));

            using var stream = dto.SpreadsheetFile.OpenReadStream();
            var uploadSpreadsheetResult = await _storageFilesService.UploadFileAsync(stream, dto.SpreadsheetFile.FileName, "jobqueuefiles");

            if (uploadSpreadsheetResult.IsFailure)
                return Result<AddJobQueueResponseDto>.Failure(uploadSpreadsheetResult.Error);

            var userResult = await _userService.GetByUsername(dto.RequestedByUsername);

            if (userResult.IsFailure)
                return Result<AddJobQueueResponseDto>.Failure(userResult.Error);

            var backgroundJob = new BackgroundJobEntity
            {
                Type = dto.Type,
                JobName = dto.Type switch
                {
                    EBackgroundJobType.REGISTER_SPREADSHEET_MOVIES => $"Cadastro de Filmes ({dto.SpreadsheetFile.FileName})",
                    EBackgroundJobType.REGISTER_SPREADSHEET_SERIES => $"Cadastro de Series ({dto.SpreadsheetFile.FileName})",
                    EBackgroundJobType.REGISTER_SPREADSHEET_CHANNELS => $"Cadastro de Canais ({dto.SpreadsheetFile.FileName})",
                    _ => string.Empty
                },
                Status = EBackgroundJobStatus.PENDING,
                RequestedByUserId = userResult?.Data?.Id ?? string.Empty,
                SpreadsheetFileUrl = uploadSpreadsheetResult.Data   
            };

            var resultId = await _repository.CreateAsync(backgroundJob);

            return Result<AddJobQueueResponseDto>.Success(new AddJobQueueResponseDto(resultId));
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<AddJobQueueResponseDto>.Failure(error);
        }
    }

    public async Task<Result<AddJobQueueResponseDto>> AddJobInQueue(AddImportEpisodesJobQueueRequestDto dto)
    {
        try
        {
            var seriesResult = await _seriesService.Get(dto.SeriesId);
            if (seriesResult.IsFailure) return Result<AddJobQueueResponseDto>.Failure(seriesResult.Error);

            var userResult = await _userService.GetByUsername(dto.RequestedByUsername);

            if (userResult.IsFailure)
                return Result<AddJobQueueResponseDto>.Failure(userResult.Error);

            var backgroundJob = new BackgroundJobEntity
            {
                Type = Enums.EBackgroundJobType.IMPORT_EPISODES_FROM_SERIES_IMDB,
                JobName = $"Importacao de Episodios via IMDB [{seriesResult?.Data?.ImdbId}]",
                Status = Enums.EBackgroundJobStatus.PENDING,
                RequestedByUserId = userResult?.Data?.Id ?? string.Empty,
                SeriesIdImportEpisodes = dto.SeriesId
            };

            var resultId = await _repository.CreateAsync(backgroundJob);

            return Result<AddJobQueueResponseDto>.Success(new AddJobQueueResponseDto(resultId));
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<AddJobQueueResponseDto>.Failure(error);
        }
    }
}