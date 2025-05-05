using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Common.Dtos;
using XerifeTv.CMS.Modules.Movie;
using XerifeTv.CMS.Modules.Movie.Dtos.Request;

namespace XerifeTv.CMS.Modules.Movie.Interfaces;

public interface IMovieRepository : IBaseRepository<MovieEntity>
{
  Task<PagedList<MovieEntity>> GetByFilterAsync(GetMoviesByFilterRequestDto dto);
  Task<IEnumerable<ItemsByCategory<MovieEntity>>> GetGroupByCategoryAsync(GetGroupByCategoryRequestDto dto);
  Task<MovieEntity?> GetByImdbIdAsync(string imdbId);
}
