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
    ISeriesRepository _seriesRepository,
    IConfiguration _configuration) : IContentV2Service
{
    public async Task<Result<IEnumerable<MovieContentV2ResponseDto>>> GetMoviesAsync(int limit)
    {
        try
        {
            var movies = await _movieRepository.GetAsync(currentPage: 1, limit);

            return Result<IEnumerable<MovieContentV2ResponseDto>>.Success(
                movies.Items.Select(
                    i => MovieContentV2ResponseDto.FromEntity(i, _configuration["SecuritySettings:ContentEncryptionKey"]!)));
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

            return Result<MovieContentV2ResponseDto?>.Success(
                MovieContentV2ResponseDto.FromEntity(movie, _configuration["SecuritySettings:ContentEncryptionKey"]!));
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
                items: [new ItemsByCategory<MovieContentV2ResponseDto>(
                    category,
                    moviesByCategory.Select(i => MovieContentV2ResponseDto.FromEntity(i, _configuration["SecuritySettings:ContentEncryptionKey"]!)))
                ]));
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
                seriesResult?.Episodes.Select(
                    i =>  EpisodeContentV2ResponseDto.FromEntity(i, _configuration["SecuritySettings:ContentEncryptionKey"]!)) ?? []);
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
            var moviesCategoriesResult = await _movieRepository.GetCategoriesWithCountAsync();
            return Result<string[]>.Success([.. moviesCategoriesResult.Where(c => c.Count >= 10).Take(limit).Select(c => c.Category)]);
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
            var seriesCategoriesResult = await _seriesRepository.GetCategoriesWithCountAsync();
            return Result<string[]>.Success([.. seriesCategoriesResult.Where(c => c.Count >= 10).Take(limit).Select(c => c.Category)]);
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

                recommededMoviesList.AddRange(
                    moviesByCategoryResult.Items.Select(i => MovieContentV2ResponseDto.FromEntity(i, _configuration["SecuritySettings:ContentEncryptionKey"]!)));
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

            return Result<IEnumerable<MovieContentV2ResponseDto>>.Success(
                moviesResult.Items.Select(i => MovieContentV2ResponseDto.FromEntity(i, _configuration["SecuritySettings:ContentEncryptionKey"]!)));
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<MovieContentV2ResponseDto>>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<GetHomeContentV2ResponseDto>> GetHomeContentAsync()
    {
        try
        {
            var random = new Random();
            int randomValue = random.Next(1, 21);

            bool isMovieFeatured = randomValue % 2 == 0;

            object? featuredContent;
            EFeaturedContentType featuredType = EFeaturedContentType.MOVIE;

            if (isMovieFeatured)
            {
                var moviesResult = await _movieRepository.GetByFilterAsync(new(
                    filter: EMovieSearchFilter.TITLE,
                    order: EMovieOrderFilter.REGISTRATION_DATE_DESC,
                    search: string.Empty,
                    limitResults: 1,
                    currentPage: 1,
                    isIncludeDisabled: false));

                featuredContent = moviesResult.Items.Select(
                    i => MovieContentV2ResponseDto.FromEntity(i, _configuration["SecuritySettings:ContentEncryptionKey"]!)).FirstOrDefault();
            }
            else
            {
                var seriesResult = await _seriesRepository.GetByFilterAsync(new(
                     filter: ESeriesSearchFilter.TITLE,
                     search: string.Empty,
                     limitResults: 1,
                     currentPage: 1,
                     isIncludeDisabled: false));

                featuredContent = seriesResult.Items.Select(SeriesSummaryContentV2ResponseDto.FromEntity).FirstOrDefault();
                featuredType = EFeaturedContentType.SERIES;
            }

            return Result<GetHomeContentV2ResponseDto>.Success(new()
            {
                FeaturedContent = featuredContent,
                FeaturedContentType = featuredType,
                MovieCategores = (await GetMoviesCategoriesAsync(5)).Data ?? [],
                SeriesCategores = (await GetSeriesCategoriesAsync(5)).Data ?? []
            });
        }
        catch (Exception ex)
        {
            return Result<GetHomeContentV2ResponseDto>.Failure(new("500", ex.Message));
        }
    }
}
