using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Series;
using XerifeTv.CMS.Modules.Series.Dtos.Request;

namespace XerifeTv.CMS.Modules.Series.Interfaces;

public interface ISeriesRepository : IBaseRepository<SeriesEntity>
{
  Task<PagedList<SeriesEntity>> GetByFilterAsync(GetSeriesByFilterRequestDto dto);
  Task<IEnumerable<ItemsByCategory<SeriesEntity>>> GetGroupByCategoryAsync(int limit);
  Task<SeriesEntity?> GetEpisodesBySeasonAsync(string serieId, int season, bool includeDisabled);
  Task<string> CreateEpisodeAsync(string serieId, Episode episode);
  Task UpdateEpisodeAsync(string serieId, Episode episode);
  Task<bool> DeleteEpisodeAsync(string serieId, string episodeId);
}