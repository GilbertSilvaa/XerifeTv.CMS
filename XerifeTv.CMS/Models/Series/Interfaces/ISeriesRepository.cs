using XerifeTv.CMS.Models.Abstractions;
using XerifeTv.CMS.Models.Abstractions.Interfaces;
using XerifeTv.CMS.Models.Series.Dtos.Request;

namespace XerifeTv.CMS.Models.Series.Interfaces;

public interface ISeriesRepository : IBaseRepository<SeriesEntity>
{
  Task<PagedList<SeriesEntity>> GetByFilterAsync(GetSeriesByFilterRequestDto dto);
  Task<IEnumerable<ItemsByCategory<SeriesEntity>>> GetGroupByCategoryAsync(int limit);
  Task<SeriesEntity?> GetEpisodesBySeasonAsync(string serieId, int season, bool includeDisabled);
  Task<string> CreateEpisodeAsync(string serieId, Episode episode);
  Task UpdateEpisodeAsync(string serieId, Episode episode);
  Task<bool> DeleteEpisodeAsync(string serieId, string episodeId);
}