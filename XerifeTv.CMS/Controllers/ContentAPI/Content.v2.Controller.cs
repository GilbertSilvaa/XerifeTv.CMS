using Microsoft.AspNetCore.Mvc;
using System.Net;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Content;
using XerifeTv.CMS.Modules.Content.Dtos.Response;
using XerifeTv.CMS.Modules.Content.Interfaces;

namespace XerifeTv.CMS.Controllers.ContentAPI;

[Route("Api/Content/v2")]
[ApiController]
public class ContentV2Controller(
    IContentV2Service service,
    ILogger<ContentV2Controller> logger,
    ICacheService cacheService) : ControllerBase
{
    [HttpGet]
    [Route("movies")]
    public async Task<IActionResult> Movies()
    {
        logger.LogInformation("Request Content API v2 /movies");

        var cacheKey = "content_v2_movies";
        var responseCache = cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await service.GetMoviesAsync(ContentConstants.DefaultPageSizeMin);

        if (response.IsSuccess)
        {
            cacheService.SetValue(cacheKey, response.Data);
            return Ok(response.Data);
        }

        return BadRequest();
    }

    [HttpGet]
    [Route("series")]
    public async Task<IActionResult> Series()
    {
        logger.LogInformation("Request Content API v2 /series");

        var cacheKey = "content_v2_series";
        var responseCache = cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await service.GetSeriesAsync(ContentConstants.DefaultPageSizeMin);

        if (response.IsSuccess)
        {
            cacheService.SetValue(cacheKey, response.Data);
            return Ok(response.Data);
        }

        return BadRequest();
    }

    [HttpGet]
    [Route("movies/{id}")]
    public async Task<IActionResult> MovieById(string id)
    {
        logger.LogInformation("Request Content API v2 /movies/{id}", id);

        var cacheKey = $"content_v2_movie_{id}";
        var responseCache = cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await service.GetMovieByIdAsync(id);

        if (response.IsSuccess)
        {
            if (response.Data is null) return NotFound();

            cacheService.SetValue(cacheKey, response.Data);
            return Ok(response.Data);
        }

        if (response.IsFailure && response.Error.Code == "404")
            return NotFound();

        return BadRequest();
    }

    [HttpGet]
    [Route("series/{id}")]
    public async Task<IActionResult> SeriesById(string id)
    {
        logger.LogInformation("Request Content API v2 /series/{id}", id);

        var cacheKey = $"content_v2_series_{id}";
        var responseCache = cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await service.GetSeriesByIdAsync(id);

        if (response.IsSuccess)
        {
            if (response.Data is null) return NotFound();

            cacheService.SetValue(cacheKey, response.Data);
            return Ok(response.Data);
        }

        if (response.IsFailure && response.Error.Code == "404")
            return NotFound();

        return BadRequest();
    }

    [HttpGet]
    [Route("series/{seriesId}/seasons/{seasonNumber}/episodes")]
    public async Task<IActionResult> EpisodesBySeriesIdAndSeason(string seriesId, int seasonNumber)
    {
        logger.LogInformation("Request Content API v2 /series/{seriesId}/season/{seasonNumber}/episodes", seriesId, seasonNumber);

        var cacheKey = $"content_v2_episodes_{seriesId}_{seasonNumber}";
        var responseCache = cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await service.GetEpisodesBySeriesIdAndSeasonAsync(seriesId, seasonNumber);

        if (response.IsSuccess)
        {
            var result = new
            {
                seriesId,
                seasonNumber,
                episodes = response.Data
            };

            cacheService.SetValue(cacheKey, result);
            return Ok(result);
        }

        return BadRequest();
    }

    [HttpGet]
    [Route("movies/categories")]
    public async Task<IActionResult> MoviesCategories()
    {
        logger.LogInformation("Request Content API v2 /movies/categories");

        var cacheKey = "content_v2_movies_categories";
        var responseCache = cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await service.GetMoviesCategoriesAsync(ContentConstants.DefaultPageSizeContent);

        if (response.IsSuccess)
        {
            cacheService.SetValue(cacheKey, response.Data);
            return Ok(response.Data);
        }

        return BadRequest();
    }

    [HttpGet]
    [Route("series/categories")]
    public async Task<IActionResult> SeriesCategories()
    {
        logger.LogInformation("Request Content API v2 /series/categories");

        var cacheKey = "content_v2_series_categories";
        var responseCache = cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await service.GetSeriesCategoriesAsync(ContentConstants.DefaultPageSizeContent);

        if (response.IsSuccess)
        {
            cacheService.SetValue(cacheKey, response.Data);
            return Ok(response.Data);
        }

        return BadRequest();
    }

    [HttpGet]
    [Route("movies/category/{category}")]
    public async Task<IActionResult> MoviesByCategory(string category, int page = 1, int pageSize = 10)
    {
        logger.LogInformation("Request Content API v2 /movies/category/{category} page={page} pageSize={pageSize}", category, page, pageSize);

        var norm = NormalizeCsv(category);
        var cacheKey = $"content_v2_movies_by_category-{norm}-{page}-{pageSize}";
        var responseCache = cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await service.GetMoviesByCategoryAsync(category, page, pageSize);

        if (response.IsSuccess)
        {
            cacheService.SetValue(cacheKey, response.Data);
            return Ok(response.Data);
        }

        return BadRequest();
    }

    [HttpGet]
    [Route("series/category/{category}")]
    public async Task<IActionResult> SeriesByCategory(string category, int page = 1, int pageSize = 10)
    {
        logger.LogInformation("Request Content API v2 /series/category/{category} page={page} pageSize={pageSize}", category, page, pageSize);

        var norm = NormalizeCsv(category);
        var cacheKey = $"content_v2_series_by_category-{norm}-{page}-{pageSize}";
        var responseCache = cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await service.GetSeriesByCategoryAsync(category, page, pageSize);

        if (response.IsSuccess)
        {
            cacheService.SetValue(cacheKey, response.Data);
            return Ok(response.Data);
        }

        return BadRequest();
    }

    [HttpGet]
    [Route("movies/{movieId}/recommended")]
    public async Task<IActionResult> MoviesRecommended(string movieId)
    {
        logger.LogInformation("Request Content API v2 /movies/{movieId}/recommended", movieId);

        var cacheKey = $"content_v2_movies_recommended_{movieId}";
        var responseCache = cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await service.GetMoviesRecommendedAsync(movieId);

        if (response.IsSuccess)
        {
            cacheService.SetValue(cacheKey, response.Data);
            return Ok(response.Data);
        }

        return BadRequest();
    }

    [HttpGet]
    [Route("series/{seriesId}/recommended")]
    public async Task<IActionResult> SeriesRecommended(string seriesId)
    {
        logger.LogInformation("Request Content API v2 /series/{seriesId}/recommended", seriesId);

        var cacheKey = $"content_v2_series_recommended_{seriesId}";
        var responseCache = cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await service.GetSeriesRecommendedAsync(seriesId);
        if (response.IsSuccess)
        {
            cacheService.SetValue(cacheKey, response.Data);
            return Ok(response.Data);
        }

        return BadRequest();
    }

    [HttpGet]
    [Route("search")]
    public async Task<IActionResult> Search(string term)
    {
        logger.LogInformation("Request Content API v2 /search term={term}", term);

        var cacheKey = $"content_v2_search_{term}";
        var responseCache = cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var moviesResponse = await service.GetMoviesByTermAsync(term, limit: ContentConstants.DefaultPageSizeContent);
        var seriesResponse = await service.GetSeriesByTermAsync(term, limit: ContentConstants.DefaultPageSizeContent);

        if (moviesResponse.IsSuccess && seriesResponse.IsSuccess)
        {
            var result = new
            {
                movies = moviesResponse.Data,
                series = seriesResponse.Data
            };

            cacheService.SetValue(cacheKey, result);
            return Ok(result);
        }

        return BadRequest();
    }

    [HttpGet]
    [Route("home")]
    public async Task<IActionResult> Home()
    {
        logger.LogInformation("Request Content API v2 /home");

        var cacheKey = "content_v2_home";
        var responseCache = cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await service.GetHomeContentAsync();

        if (response.IsSuccess)
        {
            var result = new
            {
                featured = response.Data?.FeaturedContent,
                featuredType = response.Data?.FeaturedContentType == EFeaturedContentType.MOVIE ? "movie" : "series",
                movieCategories = response.Data?.MovieCategores,
                seriesCategories = response.Data?.SeriesCategores
            };

            cacheService.SetValue(cacheKey, result);
            return Ok(result);
        }

        return BadRequest();
    }

    [HttpGet]
    [Route("movies/categories/groups")]
    public async Task<IActionResult> MoviesByCategories([FromQuery] List<string> categories, int page = 1, int pageSize = 10)
    {
        logger.LogInformation("Request Content API v2 /movies/categories/groups categories={categories}", string.Join(", ", categories));

        var norm = NormalizeCsv(string.Join('_', categories));
        var cacheKey = $"content_v2_movies_by_categories-{norm}";
        var responseCache = cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await service.GetMoviesByCategoriesListAsync(categories, page, pageSize);

        if (response.IsSuccess)
        {
            cacheService.SetValue(cacheKey, response.Data);
            return Ok(response.Data);
        }

        return BadRequest();
    }

    [HttpGet]
    [Route("series/categories/groups")]
    public async Task<IActionResult> SeriesByCategories([FromQuery] List<string> categories, int page = 1, int pageSize = 10)
    {
        logger.LogInformation("Request Content API v2 /series/categories/groups categories={categories}", string.Join(", ", categories));

        var norm = NormalizeCsv(string.Join('_', categories));
        var cacheKey = $"content_v2_series_by_categories-{norm}";
        var responseCache = cacheService.GetValue<object>(cacheKey);
        if (responseCache != null) return Ok(responseCache);

        var response = await service.GetSeriesByCategoriesListAsync(categories, page, pageSize);

        if (response.IsSuccess)
        {
            cacheService.SetValue(cacheKey, response.Data);
            return Ok(response.Data);
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
