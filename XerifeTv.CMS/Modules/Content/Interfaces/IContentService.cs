using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Content.Dtos.Response;
using XerifeTv.CMS.Modules.Movie.Dtos.Request;
using XerifeTv.CMS.Modules.Series;
using XerifeTv.CMS.Modules.Common.Dtos;

namespace XerifeTv.CMS.Modules.Content.Interfaces;

public interface IContentService
{
  Task<Result<IEnumerable<ItemsByCategory<GetMovieContentResponseDto>>>> GetMoviesGroupByCategory(
    GetGroupByCategoryRequestDto dto);
  Task<Result<PagedList<GetMovieContentResponseDto>>> GetMoviesByCategory(string category, int? currentPage, int? limit);
  Task<Result<IEnumerable<ItemsByCategory<GetSeriesContentResponseDto>>>> GetSeriesGroupByCategory(
    GetGroupByCategoryRequestDto dto);
  Task<Result<IEnumerable<GetSeriesContentResponseDto>>> GetSeriesByCategory(string category, int? limit);
  Task<Result<IEnumerable<Episode>>> GetEpisodesSeriesBySeason(string serieId, int season);
  Task<Result<IEnumerable<ItemsByCategory<GetChannelContentResponseDto>>>> GetChannelsGroupByCategory(int? limit);
  Task<Result<GetContentsByNameResponseDto>> GetContentsByTitle(string title, int? limit);
}
