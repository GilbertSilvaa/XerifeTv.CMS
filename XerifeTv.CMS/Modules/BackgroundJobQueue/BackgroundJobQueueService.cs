using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Response;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Series.Interfaces;
using XerifeTv.CMS.Modules.User.Interfaces;
using Error = XerifeTv.CMS.Modules.Common.Error;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue;

public class BackgroundJobQueueService(
	IStorageFilesService _storageFilesService,
	IBackgroundJobQueueRepository _repository,
	ISeriesService _seriesService,
	IUserService _userService) : IBackgroundJobQueueService
{
	private readonly string[] _acceptedExtensions = [".xlsx", ".xls"];

	public async Task<Result<AddJobQueueResponseDto>> AddJobInQueue(AddSpreadsheetJobQueueRequestDto dto)
	{
		try
		{
			var fileExtension = Path.GetExtension(dto.SpreadsheetFile?.FileName);

			if (dto.SpreadsheetFile == null || !_acceptedExtensions.Contains(fileExtension))
				return Result<AddJobQueueResponseDto>.Failure(new Error("400", "Arquivo de planilha invalido"));

			var jobGuidId = Guid.NewGuid();

			using var stream = dto.SpreadsheetFile.OpenReadStream();
			var uploadSpreadsheetResult = await _storageFilesService.UploadFileAsync(stream, $"{jobGuidId}{fileExtension}", "jobqueuefiles");

			if (uploadSpreadsheetResult.IsFailure)
				return Result<AddJobQueueResponseDto>.Failure(uploadSpreadsheetResult.Error);

			var userResult = await _userService.GetByUsername(dto.RequestedByUsername);

			if (userResult.IsFailure)
				return Result<AddJobQueueResponseDto>.Failure(userResult.Error);

			var backgroundJob = new BackgroundJobEntity
			{
				Id = jobGuidId.ToString(),
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
				Type = EBackgroundJobType.IMPORT_EPISODES_FROM_SERIES_IMDB,
				JobName = $"Importacao de Episodios via IMDB [{seriesResult?.Data?.ImdbId}]",
				Status = EBackgroundJobStatus.PENDING,
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

	public async Task<Result<PagedList<GetBackgroundJobResponseDto>>> GetByFilter(GetBackgroundJobsByFilterRequestDto dto)
	{
		try
		{
			var response = await _repository.GetByFilterAsync(dto);

			var result = new PagedList<GetBackgroundJobResponseDto>(
				response.CurrentPage,
				response.TotalPageCount,
				response.Items.Select(GetBackgroundJobResponseDto.FromEntity));

			return Result<PagedList<GetBackgroundJobResponseDto>>.Success(result);
		}
		catch (Exception ex)
		{
			var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
			return Result<PagedList<GetBackgroundJobResponseDto>>.Failure(error);
		}
	}

	public async Task<Result<string>> Update(UpdateBackgroundJobRequestDto dto)
	{
		try
		{
			var entity = await _repository.GetAsync(dto.Id);

			if (entity == null)
				return Result<string>.Failure(new Error("404", "Background Job nao encontrado"));

			if (entity.Status == EBackgroundJobStatus.COMPLETED || entity.Status == EBackgroundJobStatus.FAILED)
				return Result<string>.Failure(new Error("400", "Background Job ja concluido"));

			if (entity.TotalProcessedRecords > dto.TotalProcessedRecords)
				return Result<string>.Failure(new Error("409", "Nao foi possível reduzir o progresso. O valor atual ja esta maior ao informado"));

			if (dto.Status == EBackgroundJobStatus.PROCESSING && entity.Status != EBackgroundJobStatus.PROCESSING)
				entity.ProcessedAt = DateTime.UtcNow;

			entity.Status = dto.Status;
			entity.TotalRecordsToProcess = dto.TotalRecordsToProcess;
			entity.TotalFailedRecords = dto.TotalFailedRecords;
			entity.TotalSuccessfulRecords = dto.TotalSuccessfulRecords;
			entity.TotalProcessedRecords = dto.TotalProcessedRecords;
			entity.ErrorList = dto.ErrorList;
			entity.UpdateAt = DateTime.UtcNow;

			await _repository.UpdateAsync(entity);

			return Result<string>.Success(entity.Id);
		}
		catch (Exception ex)
		{
			var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
			return Result<string>.Failure(error);
		}
	}

	public async Task<Result<bool>> Delete(string id)
	{
		try
		{
			var entity = await _repository.GetAsync(id);

			if (entity == null)
				return Result<bool>.Failure(new Error("404", "Background Job nao encontrado"));

			await _repository.DeleteAsync(id);

			return Result<bool>.Success(true);
		}
		catch (Exception ex)
		{
			var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
			return Result<bool>.Failure(error);
		}
	}
}