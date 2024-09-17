﻿using MongoDB.Driver.Linq;
using XerifeTv.CMS.Models.Abstractions;
using XerifeTv.CMS.Models.Series.Dtos.Request;
using XerifeTv.CMS.Models.Series.Dtos.Response;
using XerifeTv.CMS.Models.Series.Interfaces;

namespace XerifeTv.CMS.Models.Series;

public class SeriesService(ISeriesRepository _repository) : ISeriesService
{
  public async Task<Result<PagedList<GetSeriesResponseDto>>> Get(int currentPage, int limit)
  {
    try
    {
      var response = await _repository.GetAsync(currentPage, limit);

      var result = new PagedList<GetSeriesResponseDto>(
        response.CurrentPage,
        response.TotalPageCount,
        response.Items.Select(GetSeriesResponseDto.FromEntity));

      return Result<PagedList<GetSeriesResponseDto>>.Success(result);
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<PagedList<GetSeriesResponseDto>>.Failure(error);
    }
  }

  public async Task<Result<GetSeriesResponseDto?>> Get(string id)
  {
    try
    {
      var response = await _repository.GetAsync(id);

      if (response is null)
        return Result<GetSeriesResponseDto?>
          .Failure(new Error("404", "content not found"));

      return Result<GetSeriesResponseDto?>
        .Success(GetSeriesResponseDto.FromEntity(response));
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<GetSeriesResponseDto?>.Failure(error);
    }
  }

  public async Task<Result<string>> Create(CreateSeriesRequestDto dto)
  {
    try
    {
      var entity = dto.ToEntity();
      await _repository.CreateAsync(entity);
      return Result<string>.Success(entity.Id);
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<string>.Failure(error);
    }
  }

  public async Task<Result<string>> Update(UpdateSeriesRequestDto dto)
  {
    try
    {
      var entity = dto.ToEntity();
      var response = await _repository.GetAsync(entity.Id);

      if (response is null)
        return Result<string>.Failure(new Error("404", "content not found"));

      entity.CreateAt = response.CreateAt;
      entity.Episodes = response.Episodes;
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
        return Result<bool>.Failure(new Error("404", "content not found"));

      await _repository.DeleteAsync(id);
      return Result<bool>.Success(true);
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<bool>.Failure(error);
    }
  }

  public async Task<Result<PagedList<GetSeriesResponseDto>>> GetByFilter(GetSeriesByFilterRequestDto dto)
  {
    try
    {
      var response = await _repository.GetByFilterAsync(dto);

      var result = new PagedList<GetSeriesResponseDto>(
        response.CurrentPage,
        response.TotalPageCount,
        response.Items.Select(GetSeriesResponseDto.FromEntity));

      return Result<PagedList<GetSeriesResponseDto>>.Success(result);
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<PagedList<GetSeriesResponseDto>>.Failure(error);
    }
  }

  public async Task<Result<GetEpisodesResponseDto>> GetEpisodesBySeason(string serieId, int season)
  {
    try
    {
      var response = await _repository.GetAsync(serieId);

      if (response is null)
        return Result<GetEpisodesResponseDto>
          .Failure(new Error("404", "content not found"));

      response.Episodes = response.Episodes
        .Where(r => r.Season.Equals(season))
        .ToList();

      var result = GetEpisodesResponseDto.FromEntity(response);

      return Result<GetEpisodesResponseDto>.Success(result);
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<GetEpisodesResponseDto>.Failure(error);
    }
  }

  public async Task<Result<string>> CreateEpisode(CreateEpisodeRequestDto dto)
  {
    try
    {
      var response = await _repository.GetAsync(dto.SerieId);

      if (response is null)
        return Result<string>.Failure(new Error("404", "content not found"));

      var result = await _repository.CreateEpisodeAsync(dto.SerieId, dto.ToEntity());

      return Result<string>.Success(result);
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<string>.Failure(error);
    }
  }
}
