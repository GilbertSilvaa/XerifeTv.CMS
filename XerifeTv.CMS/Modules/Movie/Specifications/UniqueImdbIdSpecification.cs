using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Movie.Interfaces;

namespace XerifeTv.CMS.Modules.Movie.Specifications;

public class UniqueImdbIdSpecification(IMovieRepository _repository) : ISpecification<MovieEntity>
{
    public async Task<bool> IsSatisfiedByAsync(MovieEntity movie)
    {
        try
        {
            var movieByImdb = await _repository.GetByImdbIdAsync(movie.ImdbId);
            return movieByImdb == null || movieByImdb.Id == movie.Id;
        }
        catch
        {
            return false;
        }
    }
}