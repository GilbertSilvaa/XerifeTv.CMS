using XerifeTv.CMS.Models.Abstractions;
using XerifeTv.CMS.Models.Channel.Dtos.Request;
using XerifeTv.CMS.Models.Channel.Dtos.Response;
using XerifeTv.CMS.Models.Channel.Interfaces;

namespace XerifeTv.CMS.Models.Channel;

public sealed class ChannelService(IChannelRepository _repository) : IChannelService
{
  public async Task<Result<PagedList<GetChannelResponseDto>>> Get(int currentPage, int limit)
  {
    try
    {
      var response = await _repository.GetAsync(currentPage, limit);

      var result = new PagedList<GetChannelResponseDto>(
        response.CurrentPage,
        response.TotalPageCount,
        response.Items.Select(GetChannelResponseDto.FromEntity));

      return Result<PagedList<GetChannelResponseDto>>.Success(result);
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<PagedList<GetChannelResponseDto>>.Failure(error);
    }
  }

  public async Task<Result<GetChannelResponseDto?>> Get(string id)
  {
    try
    {
      var response = await _repository.GetAsync(id);

      if (response is null)
        return Result<GetChannelResponseDto?>
          .Failure(new Error("404", "Conteudo nao encontrado"));

      return Result<GetChannelResponseDto?>
        .Success(GetChannelResponseDto.FromEntity(response));
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<GetChannelResponseDto?>.Failure(error);
    }
  }

  public async Task<Result<string>> Create(CreateChannelRequestDto dto)
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

  public async Task<Result<string>> Update(UpdateChannelRequestDto dto)
  {
    try
    {
      var entity = dto.ToEntity();
      var response = await _repository.GetAsync(entity.Id);

      if (response is null)
        return Result<string>.Failure(new Error("404", "Conteudo nao encontrado"));

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

  public async Task<Result<bool>> Delete(string id)
  {
    try
    {
      var response = await _repository.GetAsync(id);

      if (response is null)
        return Result<bool>.Failure(new Error("404", "Conteudo nao encontrado"));

      await _repository.DeleteAsync(id);
      return Result<bool>.Success(true);
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<bool>.Failure(error);
    }
  }

  public async Task<Result<PagedList<GetChannelResponseDto>>> GetByFilter(GetChannelsByFilterRequestDto dto)
  {
    try
    {
      var response = await _repository.GetByFilterAsync(dto);

      var result = new PagedList<GetChannelResponseDto>(
        response.CurrentPage,
        response.TotalPageCount,
        response.Items.Select(GetChannelResponseDto.FromEntity));
      
      return Result<PagedList<GetChannelResponseDto>>.Success(result);
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<PagedList<GetChannelResponseDto>>.Failure(error);
    }
  }
}
