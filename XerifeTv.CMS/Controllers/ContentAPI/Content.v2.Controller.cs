using Microsoft.AspNetCore.Mvc;
using System.Collections;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Content.Interfaces;
using XerifeTv.CMS.Modules.Movie.Enums;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Modules.Series.Enums;
using XerifeTv.CMS.Modules.Series.Interfaces;

namespace XerifeTv.CMS.Controllers.ContentAPI;

[Route("Api/Content/v2")]
[ApiController]
public class ContentV2Controller(
    IContentV2Service _service,
    IMovieService _movieService,
    ISeriesService _seriesService,
    ILogger<ContentV2Controller> _logger,
    ICacheService _cacheService) : ControllerBase
{
    [HttpGet]
    [Route("movies")]
    public async Task<IActionResult> Movies()
    {
        _logger.LogInformation("Request Content API v2 /movies");

        var cacheKey = "content_v2_movies";
        var responseCache = _cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await _service.GetMoviesAsync(200);

        if (response.IsSuccess)
        {
            _cacheService.SetValue(cacheKey, response.Data);
            return Ok(response.Data);
        }

        return BadRequest();
    }

    [HttpGet]
    [Route("series")]
    public async Task<IActionResult> Series()
    {
        _logger.LogInformation("Request Content API v2 /series");

        var cacheKey = "content_v2_series";
        var responseCache = _cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await _service.GetSeriesAsync(200);

        if (response.IsSuccess)
        {
            _cacheService.SetValue(cacheKey, response.Data);
            return Ok(response.Data);
        }

        return BadRequest();
    }

    [HttpGet]
    [Route("movies/{id}")]
    public async Task<IActionResult> MovieById(string id)
    {
        _logger.LogInformation("Request Content API v2 /movies/{id}", id);

        var cacheKey = $"content_v2_movie_{id}";
        var responseCache = _cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await _service.GetMovieByIdAsync(id);

        if (response.IsSuccess)
        {
            if (response.Data is null) return NotFound();

            _cacheService.SetValue(cacheKey, response.Data);
            return Ok(response.Data);
        }

        return BadRequest();
    }

    [HttpGet]
    [Route("series/{id}")]
    public async Task<IActionResult> SeriesById(string id)
    {
        _logger.LogInformation("Request Content API v2 /series/{id}", id);

        var cacheKey = $"content_v2_series_{id}";
        var responseCache = _cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await _service.GetSeriesByIdAsync(id);

        if (response.IsSuccess)
        {
            if (response.Data is null) return NotFound();

            _cacheService.SetValue(cacheKey, response.Data);
            return Ok(response.Data);
        }

        return BadRequest();
    }

    [HttpGet]
    [Route("series/{seriesId}/seasons/{seasonNumber}/episodes")]
    public async Task<IActionResult> EpisodesBySeriesIdAndSeason(string seriesId, int seasonNumber)
    {
        _logger.LogInformation("Request Content API v2 /series/{seriesId}/season/{seasonNumber}/episodes", seriesId, seasonNumber);

        var cacheKey = $"content_v2_episodes_{seriesId}_{seasonNumber}";
        var responseCache = _cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await _service.GetEpisodesBySeriesIdAndSeasonAsync(seriesId, seasonNumber);

        if (response.IsSuccess)
        {
            var result = new
            {
                seriesId,
                seasonNumber,
                episodes = response.Data
            };

            _cacheService.SetValue(cacheKey, result);
            return Ok(result);
        }

        return BadRequest();
    }

    [HttpGet]
    [Route("movies/categories")]
    public async Task<IActionResult> MoviesCategories()
    {
        _logger.LogInformation("Request Content API v2 /movies/categories");

        var cacheKey = "content_v2_movies_categories";
        var responseCache = _cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await _service.GetMoviesCategoriesAsync();

        if (response.IsSuccess)
        {
            _cacheService.SetValue(cacheKey, response.Data);
            return Ok(response.Data);
        }

        return BadRequest();
    }

    [HttpGet]
    [Route("series/categories")]
    public async Task<IActionResult> SeriesCategories()
    {
        _logger.LogInformation("Request Content API v2 /series/categories");

        var cacheKey = "content_v2_series_categories";
        var responseCache = _cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await _service.GetSeriesCategoriesAsync();

        if (response.IsSuccess)
        {
            _cacheService.SetValue(cacheKey, response.Data);
            return Ok(response.Data);
        }

        return BadRequest();
    }

    [HttpGet]
    [Route("movies/category/{category}")]
    public async Task<IActionResult> MoviesByCategory(string category, int page = 1, int pageSize = 10)
    {
        _logger.LogInformation("Request Content API v2 /movies/category/{category} page={page} pageSize={pageSize}", category, page, pageSize);

        var norm = NormalizeCsv(category);
        var cacheKey = $"content_v2_movies_by_category-{norm}-{page}-{pageSize}";
        var responseCache = _cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await _service.GetMoviesByCategoryAsync(category, page, pageSize);

        if (response.IsSuccess)
        {
            _cacheService.SetValue(cacheKey, response.Data);
            return Ok(response.Data);
        }

        return BadRequest();
    }

    [HttpGet]
    [Route("series/category/{category}")]
    public async Task<IActionResult> SeriesByCategory(string category, int page = 1, int pageSize = 10)
    {
        _logger.LogInformation("Request Content API v2 /series/category/{category} page={page} pageSize={pageSize}", category, page, pageSize);

        var norm = NormalizeCsv(category);
        var cacheKey = $"content_v2_series_by_category-{norm}-{page}-{pageSize}";
        var responseCache = _cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await _service.GetSeriesByCategoryAsync(category, page, pageSize);

        if (response.IsSuccess)
        {
            _cacheService.SetValue(cacheKey, response.Data);
            return Ok(response.Data);
        }

        return BadRequest();
    }

    [HttpGet]
    [Route("movies/{movieId}/recommended")]
    public async Task<IActionResult> MoviesRecommended(string movieId)
    {
        _logger.LogInformation("Request Content API v2 /movies/{movieId}/recommended", movieId);

        var cacheKey = $"content_v2_movies_recommended_{movieId}";
        var responseCache = _cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await _service.GetMoviesRecommendedAsync(movieId);

        if (response.IsSuccess)
        {
            _cacheService.SetValue(cacheKey, response.Data);
            return Ok(response.Data);
        }

        return BadRequest();
    }

    [HttpGet]
    [Route("search")]
    public async Task<IActionResult> Search(string term)
    {
        _logger.LogInformation("Request Content API v2 /search term={term}", term);

        var cacheKey = $"content_v2_search_{term}";
        var responseCache = _cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var moviesResponse = await _service.GetMoviesByTermAsync(term, limit: 15);
        var seriesResponse = await _service.GetSeriesByTermAsync(term, limit: 15);

        if (moviesResponse.IsSuccess && seriesResponse.IsSuccess)
        {
            var result = new
            {
                movies = moviesResponse.Data,
                series = seriesResponse.Data
            };

            _cacheService.SetValue(cacheKey, result);
            return Ok(result);
        }

        return BadRequest();
    }

    [HttpGet]
    [Route("home")]
    public async Task<IActionResult> Home()
    {
        _logger.LogInformation("Request Content API v2 /home");

        var cacheKey = "content_v2_home";
        var responseCache = _cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var moviesCategoriesResponse = await _service.GetMoviesCategoriesAsync(5);
        var seriesCategoriesResponse = await _service.GetSeriesCategoriesAsync(5);

        if (moviesCategoriesResponse.IsSuccess && seriesCategoriesResponse.IsSuccess)
        {
            var random = new Random();
            int randomValue = random.Next(1, 105);

            string featuredType = "movie";
            object? featuredContent = new();

            if (randomValue % 2 == 0)
            {
                var moviesResponse = await _movieService.GetByFilterAsync(new(
                    filter: EMovieSearchFilter.TITLE,
                    order: EMovieOrderFilter.REGISTRATION_DATE_DESC,
                    search: "",
                    limitResults: 1,
                    currentPage: 1,
                    isIncludeDisabled: false));

                if (moviesResponse.IsSuccess)
                {
                    featuredContent = moviesResponse.Data?.Items.FirstOrDefault();
                }
            }
            else
            {
                var seriesResponse = await _seriesService.GetByFilterAsync(new(
                    filter: ESeriesSearchFilter.TITLE,
                    search: "a",
                    limitResults: 25,
                    currentPage: 1,
                    isIncludeDisabled: false));

                if (seriesResponse.IsSuccess)
                {
                    featuredType = "series";
                    featuredContent = seriesResponse.Data?.Items.ToArray()[Random.Shared.Next(seriesResponse.Data?.Items.Count() ?? 0)];
                }
            }

            var result = new
            {
                featured = featuredContent,
                featuredType,
                movieCategories = moviesCategoriesResponse.Data,
                seriesCategories = seriesCategoriesResponse.Data
            };

            _cacheService.SetValue(cacheKey, result);
            return Ok(result);
        }

        return BadRequest();
    }

    private static string NormalizeCsv(string csv)
        => string.Join(',',
            csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
               .Select(x => x.ToLowerInvariant())
               .OrderBy(x => x)
        );
}