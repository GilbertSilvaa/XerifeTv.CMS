using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Content.Dtos.Response;
using XerifeTv.CMS.Modules.Content.Interfaces;
using XerifeTv.CMS.Modules.Movie.Enums;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Modules.Series.Enums;
using XerifeTv.CMS.Modules.Series.Interfaces;

namespace XerifeTv.CMS.Modules.Content;

public class ContentV2Service(
    IMovieRepository _movieRepository,
    ISeriesRepository _seriesRepository) : IContentV2Service
{
    public async Task<Result<IEnumerable<MovieContentV2ResponseDto>>> GetMoviesAsync(int limit)
    {
        try
        {
            var movies = await _movieRepository.GetAsync(currentPage: 1, limit);

            return Result<IEnumerable<MovieContentV2ResponseDto>>.Success(movies.Items.Select(MovieContentV2ResponseDto.FromEntity));
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<MovieContentV2ResponseDto>>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<IEnumerable<SeriesSummaryContentV2ResponseDto>>> GetSeriesAsync(int limit)
    {
        try
        {
            var series = await _seriesRepository.GetAsync(currentPage: 1, limit);

            return Result<IEnumerable<SeriesSummaryContentV2ResponseDto>>.Success(series.Items.Select(SeriesSummaryContentV2ResponseDto.FromEntity));
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<SeriesSummaryContentV2ResponseDto>>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<MovieContentV2ResponseDto?>> GetMovieByIdAsync(string id)
    {
        try
        {
            var movie = await _movieRepository.GetAsync(id);

            if (movie is null)
                return Result<MovieContentV2ResponseDto?>.Failure(new("404", "Conteudo nao encontrado"));

            return Result<MovieContentV2ResponseDto?>.Success(MovieContentV2ResponseDto.FromEntity(movie));
        }
        catch (Exception ex)
        {
            return Result<MovieContentV2ResponseDto?>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<SeriesSummaryContentV2ResponseDto?>> GetSeriesByIdAsync(string id)
    {
        try
        {
            var series = await _seriesRepository.GetAsync(id);

            if (series is null)
                return Result<SeriesSummaryContentV2ResponseDto?>.Failure(new("404", "Conteudo nao encontrado"));

            return Result<SeriesSummaryContentV2ResponseDto?>.Success(SeriesSummaryContentV2ResponseDto.FromEntity(series));
        }
        catch (Exception ex)
        {
            return Result<SeriesSummaryContentV2ResponseDto?>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<PagedList<ItemsByCategory<MovieContentV2ResponseDto>>>> GetMoviesByCategoryAsync(string category, int page = 1, int pageSize = 1)
    {
        try
        {
            var movies = await _movieRepository.GetByFilterAsync(new(
                filter: EMovieSearchFilter.CATEGORY,
                order: EMovieOrderFilter.REGISTRATION_DATE_DESC,
                search: category,
                limitResults: pageSize,
                currentPage: page,
                isIncludeDisabled: false));

            var moviesByCategory = movies.Items;

            return Result<PagedList<ItemsByCategory<MovieContentV2ResponseDto>>>.Success(new(
                currentPage: movies.CurrentPage,
                totalPageCount: movies.TotalPageCount,
                items: [new ItemsByCategory<MovieContentV2ResponseDto>(category, moviesByCategory.Select(MovieContentV2ResponseDto.FromEntity))]));
        }
        catch (Exception ex)
        {
            return Result<PagedList<ItemsByCategory<MovieContentV2ResponseDto>>>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<PagedList<ItemsByCategory<SeriesSummaryContentV2ResponseDto>>>> GetSeriesByCategoryAsync(string category, int page = 1, int pageSize = 1)
    {
        try
        {
            var series = await _seriesRepository.GetByFilterAsync(new(
                filter: ESeriesSearchFilter.CATEGORY,
                search: category,
                limitResults: pageSize,
                currentPage: page,
                isIncludeDisabled: false));

            var seriesByCategory = series.Items;

            return Result<PagedList<ItemsByCategory<SeriesSummaryContentV2ResponseDto>>>.Success(new(
                currentPage: series.CurrentPage,
                totalPageCount: series.TotalPageCount,
                items: [new ItemsByCategory<SeriesSummaryContentV2ResponseDto>(category, seriesByCategory.Select(SeriesSummaryContentV2ResponseDto.FromEntity))]));
        }
        catch (Exception ex)
        {
            return Result<PagedList<ItemsByCategory<SeriesSummaryContentV2ResponseDto>>>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<IEnumerable<EpisodeContentV2ResponseDto>>> GetEpisodesBySeriesIdAndSeasonAsync(string seriesId, int seasonNumber)
    {
        try
        {
            var seriesResult = await _seriesRepository.GetEpisodesBySeasonAsync(seriesId, seasonNumber, false);

            return Result<IEnumerable<EpisodeContentV2ResponseDto>>.Success(
                seriesResult?.Episodes.Select(EpisodeContentV2ResponseDto.FromEntity) ?? []);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<EpisodeContentV2ResponseDto>>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<string[]>> GetMoviesCategoriesAsync(int limit = 10)
    {
        try
        {
            var moviesCategoriesResult = await _movieRepository.GetAllCategoriesAsync();
            return Result<string[]>.Success([.. moviesCategoriesResult.Take(limit)]);
        }
        catch (Exception ex)
        {
            return Result<string[]>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<string[]>> GetSeriesCategoriesAsync(int limit = 10)
    {
        try
        {
            var seriesCategoriesResult = await _seriesRepository.GetAllCategoriesAsync();
            return Result<string[]>.Success([.. seriesCategoriesResult.Take(limit)]);
        }
        catch (Exception ex)
        {
            return Result<string[]>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<IEnumerable<MovieContentV2ResponseDto>>> GetMoviesRecommendedAsync(string movieId)
    {
        try
        {
            var movieResult = await _movieRepository.GetAsync(movieId);

            if (movieResult is null)
                return Result<IEnumerable<MovieContentV2ResponseDto>>.Failure(new("404", "Conteudo nao encontrado"));

            List<MovieContentV2ResponseDto> recommededMoviesList = [];

            foreach (var category in movieResult.Categories.Take(3))
            {
                var moviesByCategoryResult = await _movieRepository.GetByFilterAsync(new(
                    filter: EMovieSearchFilter.CATEGORY,
                    order: EMovieOrderFilter.REGISTRATION_DATE_DESC,
                    search: category,
                    limitResults: 15,
                    currentPage: 1,
                    isIncludeDisabled: false));

                recommededMoviesList.AddRange(moviesByCategoryResult.Items.Select(MovieContentV2ResponseDto.FromEntity));
            }

            recommededMoviesList = [.. recommededMoviesList.OrderByDescending(x => x.RatingImdb).Take(15).Where(x => x.Id != movieId)];

            return Result<IEnumerable<MovieContentV2ResponseDto>>.Success(recommededMoviesList.DistinctBy(x => x.Id));
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<MovieContentV2ResponseDto>>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<IEnumerable<SeriesSummaryContentV2ResponseDto>>> GetSeriesByTermAsync(string searchTerm, int limit = 10)
    {
        try
        {
            var seriesResult = await _seriesRepository.GetByFilterAsync(new(
                filter: ESeriesSearchFilter.TITLE,
                search: searchTerm,
                limitResults: limit,
                currentPage: 1,
                isIncludeDisabled: false));

            return Result<IEnumerable<SeriesSummaryContentV2ResponseDto>>.Success(seriesResult.Items.Select(SeriesSummaryContentV2ResponseDto.FromEntity));
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<SeriesSummaryContentV2ResponseDto>>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<IEnumerable<MovieContentV2ResponseDto>>> GetMoviesByTermAsync(string searchTerm, int limit = 10)
    {
        try
        {
            var moviesResult = await _movieRepository.GetByFilterAsync(new(
                filter: EMovieSearchFilter.TITLE,
                order: EMovieOrderFilter.TITLE,
                search: searchTerm,
                limitResults: limit,
                currentPage: 1,
                isIncludeDisabled: false));

            return Result<IEnumerable<MovieContentV2ResponseDto>>.Success(moviesResult.Items.Select(MovieContentV2ResponseDto.FromEntity));
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<MovieContentV2ResponseDto>>.Failure(new("500", ex.Message));
        }
    }
}
