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
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    [HttpGet]
    [Route("movies")]
    public async Task<IActionResult> Movies()
    {
        logger.LogInformation("Request Content API v2 /movies");

        var cacheKey = "content_v2_movies";

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var response = await service.GetMoviesAsync(ContentConstants.DefaultPageSizeMin);
            return response.IsSuccess ? response.Data : default;
        });

        if (data is null) return BadRequest();

        return Ok(data);
    }

    [HttpGet]
    [Route("series")]
    public async Task<IActionResult> Series()
    {
        logger.LogInformation("Request Content API v2 /series");

        var cacheKey = "content_v2_series";

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var response = await service.GetSeriesAsync(ContentConstants.DefaultPageSizeMin);
            return response.IsSuccess ? response.Data : default;
        });

        if (data is null) return BadRequest();

        return Ok(data);
    }

    [HttpGet]
    [Route("movies/{id}")]
    public async Task<IActionResult> MovieById(string id)
    {
        logger.LogInformation("Request Content API v2 /movies/{id}", id);

        var cacheKey = $"content_v2_movie_{id}";
        var notFound = false;
        var isFailure = false;

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var response = await service.GetMovieByIdAsync(id);

            if (response.IsSuccess)
            {
                if (response.Data is null) notFound = true;
                return response.Data;
            }

            if (response.IsFailure && response.Error.Code == "404") notFound = true;
            else isFailure = true;

            return null;
        });

        if (notFound) return NotFound();
        if (isFailure) return BadRequest();
        if (data is null) return NotFound();

        return Ok(data);
    }

    [HttpGet]
    [Route("series/{id}")]
    public async Task<IActionResult> SeriesById(string id)
    {
        logger.LogInformation("Request Content API v2 /series/{id}", id);

        var cacheKey = $"content_v2_series_{id}";
        var notFound = false;
        var isFailure = false;

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var response = await service.GetSeriesByIdAsync(id);

            if (response.IsSuccess)
            {
                if (response.Data is null) notFound = true;
                return response.Data;
            }

            if (response.IsFailure && response.Error.Code == "404") notFound = true;
            else isFailure = true;

            return null;
        });

        if (notFound) return NotFound();
        if (isFailure) return BadRequest();
        if (data is null) return NotFound();

        return Ok(data);
    }

    [HttpGet]
    [Route("series/{seriesId}/seasons/{seasonNumber}/episodes")]
    public async Task<IActionResult> EpisodesBySeriesIdAndSeason(string seriesId, int seasonNumber)
    {
        logger.LogInformation("Request Content API v2 /series/{seriesId}/season/{seasonNumber}/episodes", seriesId, seasonNumber);

        var cacheKey = $"content_v2_episodes_{seriesId}_{seasonNumber}";

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var response = await service.GetEpisodesBySeriesIdAndSeasonAsync(seriesId, seasonNumber);

            if (!response.IsSuccess) return null;

            return new
            {
                seriesId,
                seasonNumber,
                episodes = response.Data
            };
        });

        if (data is null) return BadRequest();

        return Ok(data);
    }

    [HttpGet]
    [Route("movies/categories")]
    public async Task<IActionResult> MoviesCategories()
    {
        logger.LogInformation("Request Content API v2 /movies/categories");

        var cacheKey = "content_v2_movies_categories";

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var response = await service.GetMoviesCategoriesAsync(ContentConstants.DefaultPageSizeContent);
            return response.IsSuccess ? response.Data : default;
        });

        if (data is null) return BadRequest();

        return Ok(data);
    }

    [HttpGet]
    [Route("series/categories")]
    public async Task<IActionResult> SeriesCategories()
    {
        logger.LogInformation("Request Content API v2 /series/categories");

        var cacheKey = "content_v2_series_categories";

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var response = await service.GetSeriesCategoriesAsync(ContentConstants.DefaultPageSizeContent);
            return response.IsSuccess ? response.Data : default;
        });

        if (data is null) return BadRequest();

        return Ok(data);
    }

    [HttpGet]
    [Route("movies/category/{category}")]
    public async Task<IActionResult> MoviesByCategory(string category, int page = 1, int pageSize = 10)
    {
        logger.LogInformation("Request Content API v2 /movies/category/{category} page={page} pageSize={pageSize}", category, page, pageSize);

        var norm = NormalizeCsv(category);
        var cacheKey = $"content_v2_movies_by_category-{norm}-{page}-{pageSize}";

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var response = await service.GetMoviesByCategoryAsync(category, page, pageSize);
            return response.IsSuccess ? response.Data : default;
        });

        if (data is null) return BadRequest();

        return Ok(data);
    }

    [HttpGet]
    [Route("series/category/{category}")]
    public async Task<IActionResult> SeriesByCategory(string category, int page = 1, int pageSize = 10)
    {
        logger.LogInformation("Request Content API v2 /series/category/{category} page={page} pageSize={pageSize}", category, page, pageSize);

        var norm = NormalizeCsv(category);
        var cacheKey = $"content_v2_series_by_category-{norm}-{page}-{pageSize}";

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var response = await service.GetSeriesByCategoryAsync(category, page, pageSize);
            return response.IsSuccess ? response.Data : default;
        });

        if (data is null) return BadRequest();

        return Ok(data);
    }

    [HttpGet]
    [Route("movies/{movieId}/recommended")]
    public async Task<IActionResult> MoviesRecommended(string movieId)
    {
        logger.LogInformation("Request Content API v2 /movies/{movieId}/recommended", movieId);

        var cacheKey = $"content_v2_movies_recommended_{movieId}";

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var response = await service.GetMoviesRecommendedAsync(movieId);
            return response.IsSuccess ? response.Data : default;
        });

        if (data is null) return BadRequest();

        return Ok(data);
    }

    [HttpGet]
    [Route("series/{seriesId}/recommended")]
    public async Task<IActionResult> SeriesRecommended(string seriesId)
    {
        logger.LogInformation("Request Content API v2 /series/{seriesId}/recommended", seriesId);

        var cacheKey = $"content_v2_series_recommended_{seriesId}";

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var response = await service.GetSeriesRecommendedAsync(seriesId);
            return response.IsSuccess ? response.Data : default;
        });

        if (data is null) return BadRequest();

        return Ok(data);
    }

    [HttpGet]
    [Route("search")]
    public async Task<IActionResult> Search(string term)
    {
        logger.LogInformation("Request Content API v2 /search term={term}", term);

        var cacheKey = $"content_v2_search_{term}";

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var moviesResponse = await service.GetMoviesByTermAsync(term, limit: ContentConstants.DefaultPageSizeContent);
            var seriesResponse = await service.GetSeriesByTermAsync(term, limit: ContentConstants.DefaultPageSizeContent);

            if (!moviesResponse.IsSuccess || !seriesResponse.IsSuccess) return null;

            return new
            {
                movies = moviesResponse.Data,
                series = seriesResponse.Data
            };
        });

        if (data is null) return BadRequest();

        return Ok(data);
    }

    [HttpGet]
    [Route("home")]
    public async Task<IActionResult> Home()
    {
        logger.LogInformation("Request Content API v2 /home");

        var cacheKey = "content_v2_home";

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var response = await service.GetHomeContentAsync();

            if (!response.IsSuccess) return null; 

            return new
            {
                featureds = response.Data?.FeaturedContents,
                movieCategories = response.Data?.MovieCategores,
                seriesCategories = response.Data?.SeriesCategores
            };
        });

        if (data is null) return BadRequest();

        return Ok(data);
    }

    [HttpGet]
    [Route("movies/categories/groups")]
    public async Task<IActionResult> MoviesByCategories([FromQuery] List<string> categories, int page = 1, int pageSize = 10)
    {
        logger.LogInformation("Request Content API v2 /movies/categories/groups categories={categories}", string.Join(", ", categories));

        var norm = NormalizeCsv(string.Join('_', categories));
        var cacheKey = $"content_v2_movies_by_categories-{norm}";

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var response = await service.GetMoviesByCategoriesListAsync(categories, page, pageSize);
            return response.IsSuccess ? response.Data : default;
        });

        if (data is null) return BadRequest();

        return Ok(data);
    }

    [HttpGet]
    [Route("series/categories/groups")]
    public async Task<IActionResult> SeriesByCategories([FromQuery] List<string> categories, int page = 1, int pageSize = 10)
    {
        logger.LogInformation("Request Content API v2 /series/categories/groups categories={categories}", string.Join(", ", categories));

        var norm = NormalizeCsv(string.Join('_', categories));
        var cacheKey = $"content_v2_series_by_categories-{norm}";

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var response = await service.GetSeriesByCategoriesListAsync(categories, page, pageSize);
            return response.IsSuccess ? response.Data : default;
        });

        if (data is null) return BadRequest();

        return Ok(data);
    }

    private static string NormalizeCsv(string csv)
        => string.Join(',',
            csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
               .Select(x => x.ToLowerInvariant())
               .OrderBy(x => x)
        );
}