using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Common.Dtos;
using XerifeTv.CMS.Modules.Content.Dtos.Request;
using XerifeTv.CMS.Modules.Content.Dtos.Response;
using XerifeTv.CMS.Modules.Series;

namespace XerifeTv.CMS.Modules.Content.Interfaces;

public interface IContentService
{
  Task<Result<IEnumerable<ItemsByCategory<GetMovieContentResponseDto>>>> GetMoviesGroupByCategoryAsync(GetGroupByCategoryRequestDto dto);
  Task<Result<PagedList<GetMovieContentResponseDto>>> GetMoviesByCategoryAsync(GetContentsRequestDto dto);
  Task<Result<IEnumerable<ItemsByCategory<GetSeriesContentResponseDto>>>> GetSeriesGroupByCategoryAsync(GetGroupByCategoryRequestDto dto);
  Task<Result<IEnumerable<GetSeriesContentResponseDto>>> GetSeriesByCategoryAsync(GetContentsRequestDto dto);
  Task<Result<IEnumerable<Episode>>> GetEpisodesSeriesBySeasonAsync(string serieId, int season);
  Task<Result<IEnumerable<ItemsByCategory<GetChannelContentResponseDto>>>> GetChannelsGroupByCategoryAsync(GetGroupByCategoryRequestDto dto);
  Task<Result<GetContentsByNameResponseDto>> GetContentsByTitleAsync(GetContentsRequestDto dto);
}
