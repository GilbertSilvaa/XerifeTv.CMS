using XerifeTv.CMS.Modules.Channel.Dtos.Request;
using XerifeTv.CMS.Modules.Channel.Enums;
using XerifeTv.CMS.Modules.Channel.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Common.Dtos;
using XerifeTv.CMS.Modules.Content.Dtos.Request;
using XerifeTv.CMS.Modules.Content.Dtos.Response;
using XerifeTv.CMS.Modules.Content.Interfaces;
using XerifeTv.CMS.Modules.Movie.Dtos.Request;
using XerifeTv.CMS.Modules.Movie.Enums;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Modules.Series;
using XerifeTv.CMS.Modules.Series.Dtos.Request;
using XerifeTv.CMS.Modules.Series.Enums;
using XerifeTv.CMS.Modules.Series.Interfaces;

namespace XerifeTv.CMS.Modules.Content;

public sealed class ContentV1Service(
  IMovieRepository _movieRepository,
  ISeriesRepository _seriesRepository,
  IChannelRepository _channelRepository,
  IConfiguration _configuration) : IContentV1Service
{
    const int limitTotalResult = 50;

    public async Task<Result<IEnumerable<ItemsByCategory<GetMovieContentResponseDto>>>> GetMoviesGroupByCategoryAsync(GetGroupByCategoryRequestDto dto)
    {
        var response = await _movieRepository.GetGroupByCategoryAsync(dto);

        var result = response.Select(x =>
          new ItemsByCategory<GetMovieContentResponseDto>(
              x.Category,
              x.Items.Select(i => GetMovieContentResponseDto.FromEntity(i, _configuration["SecuritySettings:ContentEncryptionKey"]!))))
          .OrderBy(x => x.Category);

        return Result<IEnumerable<ItemsByCategory<GetMovieContentResponseDto>>>
          .Success(result);
    }

    public async Task<Result<PagedList<GetMovieContentResponseDto>>> GetMoviesByCategoryAsync(GetContentsRequestDto dto)
    {
        var response = await _movieRepository.GetByFilterAsync(
                new GetMoviesByFilterRequestDto(
                    filter: EMovieSearchFilter.CATEGORY,
                    order: EMovieOrderFilter.REGISTRATION_DATE_DESC,
                    search: dto.Search,
                    limitResults: dto.Limit ?? limitTotalResult,
                    currentPage: dto.CurrentPage ?? 1,
                    isIncludeDisabled: false));

        var result = new PagedList<GetMovieContentResponseDto>(
          response.CurrentPage,
          response.TotalPageCount,
          response.Items.Select(i => GetMovieContentResponseDto.FromEntity(i, _configuration["SecuritySettings:ContentEncryptionKey"]!)));

        return Result<PagedList<GetMovieContentResponseDto>>.Success(result);
    }

    public async Task<Result<IEnumerable<ItemsByCategory<GetSeriesContentResponseDto>>>> GetSeriesGroupByCategoryAsync(GetGroupByCategoryRequestDto dto)
    {
        var response = await _seriesRepository.GetGroupByCategoryAsync(dto);

        var result = response.Select(x =>
          new ItemsByCategory<GetSeriesContentResponseDto>(
            x.Category, x.Items.Select(GetSeriesContentResponseDto.FromEntity)))
          .OrderBy(x => x.Category);

        return Result<IEnumerable<ItemsByCategory<GetSeriesContentResponseDto>>>
          .Success(result);
    }

    public async Task<Result<IEnumerable<GetSeriesContentResponseDto>>> GetSeriesByCategoryAsync(GetContentsRequestDto dto)
    {
        var response = await _seriesRepository.GetByFilterAsync(
            new GetSeriesByFilterRequestDto(
                filter: ESeriesSearchFilter.CATEGORY,
                search: dto.Search,
                limitResults: dto.Limit ?? limitTotalResult,
                currentPage: dto.CurrentPage,
                isIncludeDisabled: false));

        return Result<IEnumerable<GetSeriesContentResponseDto>>
          .Success(response.Items.Select(GetSeriesContentResponseDto.FromEntity));
    }

    public async Task<Result<IEnumerable<Episode>>> GetEpisodesSeriesBySeasonAsync(string serieId, int season)
    {
        var response = await _seriesRepository.GetEpisodesBySeasonAsync(serieId, season, includeDisabled: false);

        List<Episode> episodes = [];

        foreach (var episode in response?.Episodes ?? [])
        {
            episode.SetUrlResolverPath(_configuration["SecuritySettings:ContentEncryptionKey"]!);
            episodes.Add(episode);
        }

        return Result<IEnumerable<Episode>>.Success(episodes);
    }

    public async Task<Result<IEnumerable<ItemsByCategory<GetChannelContentResponseDto>>>> GetChannelsGroupByCategoryAsync(GetGroupByCategoryRequestDto dto)
    {
        var response = await _channelRepository.GetGroupByCategoryAsync(dto);

        var result = response.Select(x =>
          new ItemsByCategory<GetChannelContentResponseDto>(
            x.Category, x.Items.Select(i => GetChannelContentResponseDto.FromEntity(i, _configuration["SecuritySettings:ContentEncryptionKey"]!))))
          .OrderBy(x => x.Category);

        return Result<IEnumerable<ItemsByCategory<GetChannelContentResponseDto>>>
          .Success(result);
    }

    public async Task<Result<GetContentsByNameResponseDto>> GetContentsByTitleAsync(GetContentsRequestDto dto)
    {
        var moviesTask = _movieRepository.GetByFilterAsync(
            new GetMoviesByFilterRequestDto(
                filter: EMovieSearchFilter.TITLE,
                order: EMovieOrderFilter.TITLE,
                search: dto.Search,
                limitResults: dto.Limit ?? limitTotalResult,
                currentPage: dto.CurrentPage,
                isIncludeDisabled: false));

        var seriesTask = _seriesRepository.GetByFilterAsync(
            new GetSeriesByFilterRequestDto(
                filter: ESeriesSearchFilter.TITLE,
                search: dto.Search,
                limitResults: dto.Limit ?? limitTotalResult,
                currentPage: dto.CurrentPage,
                isIncludeDisabled: false));

        var channelsTask = _channelRepository.GetByFilterAsync(
            new GetChannelsByFilterRequestDto(
                filter: EChannelSearchFilter.TITLE,
                search: dto.Search,
                limitResults: dto.Limit ?? limitTotalResult,
                currentPage: dto.CurrentPage,
                isIncludeDisabled: false));

        await Task.WhenAll(moviesTask, seriesTask, channelsTask);

        var response = new GetContentsByNameResponseDto(
            moviesTask.Result.Items.Select(i => GetMovieContentResponseDto.FromEntity(i, _configuration["SecuritySettings:ContentEncryptionKey"]!)),
            seriesTask.Result.Items.Select(GetSeriesContentResponseDto.FromEntity),
            channelsTask.Result.Items.Select(i => GetChannelContentResponseDto.FromEntity(i, _configuration["SecuritySettings:ContentEncryptionKey"]!)));

        return Result<GetContentsByNameResponseDto>.Success(response);
    }
}