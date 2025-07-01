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

	public async Task<Result<AddJobQueueResponseDto>> AddJobInQueueAsync(AddSpreadsheetJobQueueRequestDto dto)
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

			var userResult = await _userService.GetByUsernameAsync(dto.RequestedByUsername);

			if (userResult.IsFailure)
				return Result<AddJobQueueResponseDto>.Failure(userResult.Error);

			var backgroundJob = BackgroundJobEntity.Create(
				id: jobGuidId.ToString(),
				type: dto.Type,
				spreadsheetFileName: dto.SpreadsheetFile.FileName,
				spreadsheetFileUrl: uploadSpreadsheetResult.Data ?? string.Empty,
				userId: userResult?.Data?.Id ?? string.Empty);

			var resultId = await _repository.CreateAsync(backgroundJob);

			return Result<AddJobQueueResponseDto>.Success(new AddJobQueueResponseDto(resultId));
		}
		catch (Exception ex)
		{
			var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
			return Result<AddJobQueueResponseDto>.Failure(error);
		}
	}

	public async Task<Result<AddJobQueueResponseDto>> AddJobInQueueAsync(AddImportEpisodesJobQueueRequestDto dto)
	{
		try
		{
			var seriesResult = await _seriesService.GetAsync(dto.SeriesId);
			if (seriesResult.IsFailure) return Result<AddJobQueueResponseDto>.Failure(seriesResult.Error);

			var userResult = await _userService.GetByUsernameAsync(dto.RequestedByUsername);

			if (userResult.IsFailure)
				return Result<AddJobQueueResponseDto>.Failure(userResult.Error);

			var backgroundJob = BackgroundJobEntity.Create(
				seriesId: dto.SeriesId,
				seriesImdbId: seriesResult?.Data?.ImdbId ?? string.Empty,
				userId: userResult?.Data?.Id ?? string.Empty);

			var resultId = await _repository.CreateAsync(backgroundJob);

			return Result<AddJobQueueResponseDto>.Success(new AddJobQueueResponseDto(resultId));
		}
		catch (Exception ex)
		{
			var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
			return Result<AddJobQueueResponseDto>.Failure(error);
		}
	}

	public async Task<Result<PagedList<GetBackgroundJobResponseDto>>> GetByFilterAsync(GetBackgroundJobsByFilterRequestDto dto)
	{
		try
		{
			if (dto.ResponsibleUsername is string username)
			{
				var userResult = await _userService.GetByUsernameAsync(username);

				if (userResult.IsFailure)
					return Result<PagedList<GetBackgroundJobResponseDto>>.Failure(userResult.Error);

				dto.ResponsibleUserId = userResult?.Data?.Id ?? string.Empty;
			}

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

	public async Task<Result<string>> UpdateAsync(UpdateBackgroundJobRequestDto dto)
	{
		try
		{
			var response = await _repository.GetAsync(dto.Id);

			if (response == null)
				return Result<string>.Failure(new Error("404", "Background Job nao encontrado"));

			await _repository.UpdateAsync(response.Update(dto));

			return Result<string>.Success(response.Id);
		}
		catch (Exception ex)
		{
			var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
			return Result<string>.Failure(error);
		}
	}

	public async Task<Result<bool>> DeleteAsync(string id)
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

	public async Task<Result<IEnumerable<GetJobsToNotifyResponseDto>>> GetJobsToNotifyAsync(string username)
	{
		try
		{
			var userResult = await _userService.GetByUsernameAsync(username);

			if (userResult.IsFailure)
				return Result<IEnumerable<GetJobsToNotifyResponseDto>>.Failure(userResult.Error);

			var response = await _repository.GetCompletedOrFailedJobsNotNotifiedAsync(userResult.Data?.Id ?? string.Empty);

			foreach (var jobEntity in response)
			{
				jobEntity.UserNotify();
				await _repository.UpdateAsync(jobEntity);
			}

			return Result<IEnumerable<GetJobsToNotifyResponseDto>>
				.Success(response.Select(GetJobsToNotifyResponseDto.FromEntity));
		}
		catch (Exception ex)
		{
			var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
			return Result<IEnumerable<GetJobsToNotifyResponseDto>>.Failure(error);
		}
	}
}