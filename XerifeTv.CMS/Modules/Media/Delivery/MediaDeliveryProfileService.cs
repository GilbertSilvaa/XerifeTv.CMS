using System.Collections.Generic;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Request;
using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Response;
using XerifeTv.CMS.Modules.Media.Delivery.Intefaces;

namespace XerifeTv.CMS.Modules.Media.Delivery;

public class MediaDeliveryProfileService(IMediaDeliveryProfileRepository _repository) : IMediaDeliveryProfileService
{
    public async Task<Result<PagedList<GetMediaDeliveryProfileResponseDto>>> GetAsync(int currentPage, int limit)
    {
        try
        {
            var response = await _repository.GetAsync(currentPage, limit);

            var result = new PagedList<GetMediaDeliveryProfileResponseDto>(
                response.CurrentPage,
                response.TotalPageCount,
                response.Items.Select(GetMediaDeliveryProfileResponseDto.FromEntity));

            return Result<PagedList<GetMediaDeliveryProfileResponseDto>>.Success(result);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<PagedList<GetMediaDeliveryProfileResponseDto>>.Failure(error);
        }
    }

    public async Task<Result<IEnumerable<GetMediaDeliveryProfileResponseDto>>> GetAllAsync(bool isIncludeDisabled = false)
    {
        try
        {
            var response = await _repository.GetAsync(isIncludeDisabled);

            return Result<IEnumerable<GetMediaDeliveryProfileResponseDto>>.Success(
                response
                .OrderBy(p => p.Name)
                .Select(GetMediaDeliveryProfileResponseDto.FromEntity));
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<IEnumerable<GetMediaDeliveryProfileResponseDto>>.Failure(error);
        }
    }

    public async Task<Result<GetMediaDeliveryProfileResponseDto?>> GetAsync(string id)
    {
        try
        {
            var response = await _repository.GetAsync(id);

            if (response == null)
                return Result<GetMediaDeliveryProfileResponseDto?>.Failure(new Error("404", "Perfil de entrega de midia nao encontrado"));

            return Result<GetMediaDeliveryProfileResponseDto?>.Success(GetMediaDeliveryProfileResponseDto.FromEntity(response));
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<GetMediaDeliveryProfileResponseDto?>.Failure(error);
        }
    }

    public async Task<Result<GetMediaDeliveryProfileResponseDto?>> GetByNameAsync(string name)
    {
        try
        {
            var response = await _repository.GetByNameAsync(name);

            if (response == null)
                return Result<GetMediaDeliveryProfileResponseDto?>.Failure(new Error("404", "Perfil de entrega de midia nao encontrado"));

            return Result<GetMediaDeliveryProfileResponseDto?>.Success(GetMediaDeliveryProfileResponseDto.FromEntity(response));
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<GetMediaDeliveryProfileResponseDto?>.Failure(error);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateMediaDeliveryProfileRequestDto dto)
    {
        try
        {
            var entity = dto.ToEntity();
            var response = await _repository.CreateAsync(entity);

            return Result<string>.Success(response);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<string>.Failure(error);
        }
    }

    public async Task<Result<string>> UpdateAsync(UpdateMediaDeliveryProfileRequestDto dto)
    {
        try
        {
            var entity = dto.ToEntity();
            var response = await _repository.GetAsync(dto.Id);

            if (response == null)
                return Result<string>.Failure(new Error("404", "Perfil de entrega de midia nao encontrado"));

            entity.CreateAt = response.CreateAt;
            await _repository.UpdateAsync(entity);
            return Result<string>.Success(entity.Id);
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
            var response = await _repository.GetAsync(id);

            if (response == null)
                return Result<bool>.Failure(new Error("404", "Perfil de entrega de midia nao encontrado"));

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