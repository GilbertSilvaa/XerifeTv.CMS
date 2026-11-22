using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Content;
using XerifeTv.CMS.Modules.Content.Dtos.Response;
using XerifeTv.CMS.Modules.Content.Interfaces;
using XerifeTv.CMS.Modules.ContentCuration.Interfaces;

namespace XerifeTv.CMS.Modules.Content.Controllers.ContentAPI;

[Route("Api/Content/v2")]
[ApiController]
public class ContentV2Controller(
    IContentV2Service service,
    ICurationCompositionService curationService,
    ILogger<ContentV2Controller> logger,
    ICacheService cacheService,
    IOptions<JsonOptions> jsonOptions) : ControllerBase
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
    public async Task<IActionResult> EpisodesBySeriesIdAndSeason(
        string seriesId, int seasonNumber, bool includeEpisodesScheduleds = false)
    {
        logger.LogInformation(
            "Request Content API v2 /series/{seriesId}/season/{seasonNumber}/episodes (includeEpisodesScheduleds={includeEpisodesScheduleds})",
            seriesId, seasonNumber, includeEpisodesScheduleds);

        var cacheKey = $"content_v2_episodes_{seriesId}_{seasonNumber}"
            + (includeEpisodesScheduleds ? "_with_schedules" : string.Empty);

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var response = await service.GetEpisodesBySeriesIdAndSeasonAsync(
                seriesId, seasonNumber, includeEpisodesScheduleds);

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
    [Route("curation/home")]
    public async Task<IActionResult> CurationHome()
    {
        logger.LogInformation("Request Content API v2 /curation/home");

        var version = await curationService.GetCacheVersionAsync();
        var cacheKey = $"content_v2_curation_home_v{version}";

        var payload = await cacheService.GetOrCreateAsync<string>(cacheKey, CacheTtl, async () =>
        {
            var response = await curationService.GetHomeAsync();

            if (!response.IsSuccess || response.Data is null) return null;

            return JsonSerializer.Serialize(new
            {
                featureds = response.Data.Featureds
                    .Select(f => new { content = f.Content, type = f.Type }),
                categories = response.Data.Categories
            }, jsonOptions.Value.JsonSerializerOptions);
        });

        if (payload is null) return BadRequest();

        return Content(payload, "application/json");
    }

    [HttpGet]
    [Route("curation/category/{category}")]
    public async Task<IActionResult> CurationCategory(string category, int page = 1, int pageSize = 0)
    {
        logger.LogInformation(
            "Request Content API v2 /curation/category/{category} page={page} pageSize={pageSize}",
            category, page, pageSize);

        if (string.IsNullOrWhiteSpace(category)) return NotFound(new { error = "Categoria não encontrada." });

        page = Math.Max(1, page);
        var requestedPageSize = Math.Max(0, pageSize);

        var version = await curationService.GetCacheVersionAsync();
        var cacheKey =
            $"content_v2_curation_category_{category.Trim().ToLowerInvariant()}_v{version}_p{page}_s{requestedPageSize}";

        var payload = await cacheService.GetOrCreateAsync<string>(cacheKey, CacheTtl, async () =>
        {
            var response = await curationService.GetCategoryAsync(category, page, requestedPageSize);

            if (!response.IsSuccess || response.Data is null) return null;

            var data = response.Data;

            object body = data.IsPaginated
                ? new
                {
                    category = data.Category,
                    currentPage = data.CurrentPage,
                    totalPageCount = data.TotalPageCount,
                    hasPrevious = data.HasPrevious,
                    hasNext = data.HasNext,
                    items = data.Items
                }
                : new
                {
                    category = data.Category,
                    items = data.Items
                };

            return JsonSerializer.Serialize(body, jsonOptions.Value.JsonSerializerOptions);
        });

        if (payload is null) return NotFound(new { error = "Categoria não encontrada." });

        return Content(payload, "application/json");
    }

    [HttpGet]
    [Route("curation/categories/groups")]
    public async Task<IActionResult> CurationCategoriesGroups([FromQuery] List<string> categories)
    {
        logger.LogInformation(
            "Request Content API v2 /curation/categories/groups categories={categories}",
            string.Join(", ", categories));

        var norm = NormalizeCsvPreservingOrder(categories);
        var version = await curationService.GetCacheVersionAsync();
        var cacheKey = $"content_v2_curation_categories_groups-{norm}_v{version}";

        var payload = await cacheService.GetOrCreateAsync<string>(cacheKey, CacheTtl, async () =>
        {
            var response = await curationService.GetCategoriesGroupsAsync(categories);

            if (!response.IsSuccess || response.Data is null) return null;

            return JsonSerializer.Serialize(response.Data, jsonOptions.Value.JsonSerializerOptions);
        });

        if (payload is null) return BadRequest();

        return Content(payload, "application/json");
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

    [HttpGet]
    [Route("movies/{id}/credits")]
    public async Task<IActionResult> MovieCredits(string id)
    {
        logger.LogInformation("Request Content API v2 /movies/{id}/credits", id);

        var cacheKey = $"content_v2_movie_credits_{id}";
        var notFound = false;
        var isFailure = false;

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var response = await service.GetMovieCreditsAsync(id);

            if (response.IsSuccess) return response.Data;

            if (response.Error.Code == "404") notFound = true;
            else isFailure = true;

            return null;
        });

        if (notFound) return NotFound();
        if (isFailure) return BadRequest();
        if (data is null) return NotFound();

        return Ok(data);
    }

    [HttpGet]
    [Route("series/{id}/credits")]
    public async Task<IActionResult> SeriesCredits(string id)
    {
        logger.LogInformation("Request Content API v2 /series/{id}/credits", id);

        var cacheKey = $"content_v2_series_credits_{id}";
        var notFound = false;
        var isFailure = false;

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var response = await service.GetSeriesCreditsAsync(id);

            if (response.IsSuccess) return response.Data;

            if (response.Error.Code == "404") notFound = true;
            else isFailure = true;

            return null;
        });

        if (notFound) return NotFound();
        if (isFailure) return BadRequest();
        if (data is null) return NotFound();

        return Ok(data);
    }

    [HttpGet]
    [Route("people/{id}")]
    public async Task<IActionResult> PersonById(string id)
    {
        logger.LogInformation("Request Content API v2 /people/{id}", id);

        var cacheKey = $"content_v2_person_{id}";
        var notFound = false;
        var isFailure = false;

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var response = await service.GetPersonByIdAsync(id);

            if (response.IsSuccess)
            {
                if (response.Data is null) notFound = true;
                return response.Data;
            }

            if (response.Error.Code == "404") notFound = true;
            else isFailure = true;

            return null;
        });

        if (notFound) return NotFound();
        if (isFailure) return BadRequest();
        if (data is null) return NotFound();

        return Ok(data);
    }

    private static string NormalizeCsv(string csv)
        => string.Join(',',
            csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
               .Select(x => x.ToLowerInvariant())
               .OrderBy(x => x)
        );

    private static string NormalizeCsvPreservingOrder(IEnumerable<string> values)
        => string.Join(',',
            values
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToLowerInvariant())
        );
}