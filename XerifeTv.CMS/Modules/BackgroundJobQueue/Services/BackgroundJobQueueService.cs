using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Response;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Series.Interfaces;
using XerifeTv.CMS.Modules.User.Interfaces;
using Error = XerifeTv.CMS.Modules.Common.Error;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue.Services;

public class BackgroundJobQueueService(
    IStorageFilesService storageFilesService,
    IBackgroundJobQueueRepository repository,
    IUserService userService) : IBackgroundJobQueueService
{
    private readonly string[] _acceptedExtensions = [".xlsx", ".xls"];

    public async Task<Result<AddJobQueueResponseDto>> AddJobInQueueAsync(AddSpreadsheetJobQueueRequestDto dto)
    {
        try
        {
            var fileExtension = Path.GetExtension(dto.SpreadsheetFile?.FileName);

            if (dto.SpreadsheetFile == null || !_acceptedExtensions.Contains(fileExtension))
                return Result<AddJobQueueResponseDto>.Failure(new Error("400", "Arquivo de planilha inválido"));

            var jobGuidId = Guid.NewGuid();

            using var stream = dto.SpreadsheetFile.OpenReadStream();
            var uploadSpreadsheetResult = await storageFilesService.UploadFileAsync(stream, $"{jobGuidId}{fileExtension}", "jobqueuefiles");

            if (uploadSpreadsheetResult.IsFailure)
                return Result<AddJobQueueResponseDto>.Failure(uploadSpreadsheetResult.Error);

            var userResult = await userService.GetByUsernameAsync(dto.RequestedByUsername);

            if (userResult.IsFailure)
                return Result<AddJobQueueResponseDto>.Failure(userResult.Error);

            var backgroundJob = BackgroundJobEntity.Create(
                id: jobGuidId.ToString(),
                type: dto.Type,
                spreadsheetFileName: dto.SpreadsheetFile.FileName,
                spreadsheetFileUrl: uploadSpreadsheetResult.Data ?? string.Empty,
                userId: userResult?.Data?.Id ?? string.Empty);

            var resultId = await repository.CreateAsync(backgroundJob);

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
            var userResult = await userService.GetByUsernameAsync(dto.RequestedByUsername);

            if (userResult.IsFailure)
                return Result<AddJobQueueResponseDto>.Failure(userResult.Error);

            var backgroundJob = BackgroundJobEntity.Create(
                seriesId: dto.SeriesId,
                seriesTitle: dto.SeriesTitle,
                userId: userResult?.Data?.Id ?? string.Empty);

            var resultId = await repository.CreateAsync(backgroundJob);

            return Result<AddJobQueueResponseDto>.Success(new AddJobQueueResponseDto(resultId));
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<AddJobQueueResponseDto>.Failure(error);
        }
    }

    public async Task<Result<AddJobQueueResponseDto>> AddJobInQueueAsync(ECalculateCategoryDistributionJobQueueType type)
    {
        try
        {
            var backgroundJob = BackgroundJobEntity.Create(type);

            var resultId = await repository.CreateAsync(backgroundJob);

            return Result<AddJobQueueResponseDto>.Success(new AddJobQueueResponseDto(resultId));
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<AddJobQueueResponseDto>.Failure(error);
        }
    }

    public async Task<Result<AddJobQueueResponseDto>> AddJobInQueueAsync(EDispatchWebhooksJobQueueType type, string dispatchWebhooksEntityId)
    {
        try
        {
            var backgroundJob = BackgroundJobEntity.Create(type, dispatchWebhooksEntityId);

            var resultId = await repository.CreateAsync(backgroundJob);

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
                var userResult = await userService.GetByUsernameAsync(username);

                if (userResult.IsFailure)
                    return Result<PagedList<GetBackgroundJobResponseDto>>.Failure(userResult.Error);

                dto.ResponsibleUserId = userResult?.Data?.Id ?? string.Empty;
            }

            var response = await repository.GetByFilterAsync(dto);

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
            var response = await repository.GetAsync(dto.Id);

            if (response == null)
                return Result<string>.Failure(new Error("404", "Background Job não encontrado"));

            await repository.UpdateAsync(response.Update(dto));

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
            var entity = await repository.GetAsync(id);

            if (entity == null)
                return Result<bool>.Failure(new Error("404", "Background Job não encontrado"));

            await repository.DeleteAsync(id);

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
            var userResult = await userService.GetByUsernameAsync(username);

            if (userResult.IsFailure)
                return Result<IEnumerable<GetJobsToNotifyResponseDto>>.Failure(userResult.Error);

            var response = await repository.GetCompletedOrFailedJobsNotNotifiedAsync(userResult.Data?.Id ?? string.Empty);

            foreach (var jobEntity in response)
            {
                jobEntity.UserNotify();
                await repository.UpdateAsync(jobEntity);
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

