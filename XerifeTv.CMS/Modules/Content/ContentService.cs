using XerifeTv.CMS.Modules.Movie.Dtos.Request;
using XerifeTv.CMS.Modules.Series.Dtos.Request;
using XerifeTv.CMS.Modules.Channel.Dtos.Request;
using XerifeTv.CMS.Modules.Movie;
using XerifeTv.CMS.Modules.Series;
using XerifeTv.CMS.Modules.Channel;
using XerifeTv.CMS.Modules.Movie.Enums;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Modules.Series.Enums;
using XerifeTv.CMS.Modules.Series.Interfaces;
using XerifeTv.CMS.Modules.Content.Interfaces;
using XerifeTv.CMS.Modules.Channel.Enums;
using XerifeTv.CMS.Modules.Channel.Interfaces;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Content.Dtos.Response;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Common.Dtos;

namespace XerifeTv.CMS.Modules.Content;

public sealed class ContentService(
  IMovieRepository _movieRepository,
  ISeriesRepository _seriesRepository,
  IChannelRepository _channelRepository,
  ICacheService _cacheService) : IContentService
{
    const int limitTotalResult = 50;
    const int limitPartialResult = 2;

    public async Task<Result<IEnumerable<ItemsByCategory<GetMovieContentResponseDto>>>> GetMoviesGroupByCategory(
      GetGroupByCategoryRequestDto dto)
    {
        var cacheKey = $"moviesGroupByCategory-{String.Join("_", dto.Categories)}-{dto.CurrentPage}-{dto.LimitResults}";
        var response = _cacheService.GetValue<IEnumerable<ItemsByCategory<MovieEntity>>>(cacheKey);

        if (response is null)
        {
            response = await _movieRepository.GetGroupByCategoryAsync(dto);
            _cacheService.SetValue(cacheKey, response);
        }

        var result = response.Select(x =>
          new ItemsByCategory<GetMovieContentResponseDto>(
            x.Category, x.Items.Select(GetMovieContentResponseDto.FromEntity)))
          .OrderBy(x => x.Category);

        return Result<IEnumerable<ItemsByCategory<GetMovieContentResponseDto>>>
          .Success(result);
    }

    public async Task<Result<PagedList<GetMovieContentResponseDto>>> GetMoviesByCategory(
      string category,
      int? currentPage,
      int? limit)
    {
        var cacheKey = $"moviesByCategory-{category}-{currentPage}-{limit}";
        var response = _cacheService.GetValue<PagedList<MovieEntity>>(cacheKey);

        if (response is null)
        {
            response = await _movieRepository.GetByFilterAsync(
              new GetMoviesByFilterRequestDto(
                filter: EMovieSearchFilter.CATEGORY,
                order: EMovieOrderFilter.REGISTRATION_DATE_DESC,
                category,
                limit ?? limitTotalResult,
                currentPage ?? 1,
                isIncludeDisabled: false));

            _cacheService.SetValue(cacheKey, response);
        }

        var result = new PagedList<GetMovieContentResponseDto>(
          response.CurrentPage,
          response.TotalPageCount,
          response.Items.Select(GetMovieContentResponseDto.FromEntity));

        return Result<PagedList<GetMovieContentResponseDto>>.Success(result);
    }

    public async Task<Result<IEnumerable<ItemsByCategory<GetSeriesContentResponseDto>>>> GetSeriesGroupByCategory(
      GetGroupByCategoryRequestDto dto)
    {
        var cacheKey = $"seriesGroupByCategory-{String.Join("_", dto.Categories)}-{dto.CurrentPage}-{dto.LimitResults}";
        var response = _cacheService.GetValue<IEnumerable<ItemsByCategory<SeriesEntity>>>(cacheKey);

        if (response is null)
        {
            response = await _seriesRepository.GetGroupByCategoryAsync(dto);
            _cacheService.SetValue(cacheKey, response);
        }

        var result = response.Select(x =>
          new ItemsByCategory<GetSeriesContentResponseDto>(
            x.Category, x.Items.Select(GetSeriesContentResponseDto.FromEntity)))
          .OrderBy(x => x.Category);

        return Result<IEnumerable<ItemsByCategory<GetSeriesContentResponseDto>>>
          .Success(result);
    }

    public async Task<Result<IEnumerable<GetSeriesContentResponseDto>>> GetSeriesByCategory(
      string category, int? limit)
    {
        var cacheKey = $"seriesByCategory-{category}-{limit}";
        var response = _cacheService.GetValue<PagedList<SeriesEntity>>(cacheKey);

        if (response is not null)
            return Result<IEnumerable<GetSeriesContentResponseDto>>
              .Success(response.Items.Select(GetSeriesContentResponseDto.FromEntity));

        response = await _seriesRepository.GetByFilterAsync(
          new GetSeriesByFilterRequestDto(
            ESeriesSearchFilter.CATEGORY,
            category,
            limit ?? limitTotalResult,
            currentPage: 1,
            isIncludeDisabled: false));

        _cacheService.SetValue(cacheKey, response);

        return Result<IEnumerable<GetSeriesContentResponseDto>>
          .Success(response.Items.Select(GetSeriesContentResponseDto.FromEntity));
    }

    public async Task<Result<IEnumerable<Episode>>> GetEpisodesSeriesBySeason(string serieId, int season)
    {
        var cacheKey = $"episodesSeriesBySeason-{serieId}-{season}";
        var response = _cacheService.GetValue<SeriesEntity>(cacheKey);

        if (response is not null)
            return Result<IEnumerable<Episode>>
              .Success(response?.Episodes ?? Enumerable.Empty<Episode>());

        response = await _seriesRepository.GetEpisodesBySeasonAsync(serieId, season, includeDisabled: false);
        _cacheService.SetValue(cacheKey, response);

        return Result<IEnumerable<Episode>>
          .Success(response?.Episodes ?? Enumerable.Empty<Episode>());
    }

    public async Task<Result<IEnumerable<ItemsByCategory<GetChannelContentResponseDto>>>> GetChannelsGroupByCategory(
      GetGroupByCategoryRequestDto dto)
    {
        var cacheKey = $"channelsGroupByCategory-{String.Join("_", dto.Categories)}-{dto.CurrentPage}-{dto.LimitResults}";
        var response = _cacheService.GetValue<IEnumerable<ItemsByCategory<ChannelEntity>>>(cacheKey);

        if (response is null)
        {
            response = await _channelRepository.GetGroupByCategoryAsync(dto);
            _cacheService.SetValue(cacheKey, response);
        }

        var result = response.Select(x =>
          new ItemsByCategory<GetChannelContentResponseDto>(
            x.Category, x.Items.Select(GetChannelContentResponseDto.FromEntity)))
          .OrderBy(x => x.Category);

        return Result<IEnumerable<ItemsByCategory<GetChannelContentResponseDto>>>
          .Success(result);
    }

    public async Task<Result<GetContentsByNameResponseDto>> GetContentsByTitle(string title, int? limit)
    {
        var cacheKey = $"contentsByTitle-{title}-{limit}";
        var response = _cacheService.GetValue<GetContentsByNameResponseDto>(cacheKey);

        if (response is not null) return Result<GetContentsByNameResponseDto>.Success(response);

        var moviesTask = _movieRepository.GetByFilterAsync(
          new GetMoviesByFilterRequestDto(
            filter: EMovieSearchFilter.TITLE,
            order: EMovieOrderFilter.TITLE,
            title,
            limit ?? limitTotalResult,
            currentPage: 1,
            isIncludeDisabled: false));

        var seriesTask = _seriesRepository.GetByFilterAsync(
          new GetSeriesByFilterRequestDto(
            ESeriesSearchFilter.TITLE,
            title,
            limit ?? limitTotalResult,
            currentPage: 1,
            isIncludeDisabled: false));

        var channelsTask = _channelRepository.GetByFilterAsync(
          new GetChannelsByFilterRequestDto(
            EChannelSearchFilter.TITLE,
            title,
            limit ?? limitTotalResult,
            currentPage: 1,
            isIncludeDisabled: false));

        await Task.WhenAll(moviesTask, seriesTask, channelsTask);

        var movieListResponse = await moviesTask;
        var seriesListResponse = await seriesTask;
        var channelListResponse = await channelsTask;

        response = new GetContentsByNameResponseDto(
          movieListResponse.Items.Select(GetMovieContentResponseDto.FromEntity),
          seriesListResponse.Items.Select(GetSeriesContentResponseDto.FromEntity),
          channelListResponse.Items.Select(GetChannelContentResponseDto.FromEntity));

        _cacheService.SetValue(cacheKey, response);

        return Result<GetContentsByNameResponseDto>.Success(response);
    }
}