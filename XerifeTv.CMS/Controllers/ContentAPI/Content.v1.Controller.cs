using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Common.Dtos;
using XerifeTv.CMS.Modules.Content.Dtos.Request;
using XerifeTv.CMS.Modules.Content.Dtos.Response;
using XerifeTv.CMS.Modules.Content.Interfaces;
using XerifeTv.CMS.Modules.Series;

namespace XerifeTv.CMS.Controllers.ContentAPI;

[Route("Api/Content")]
[ApiController]
public class ContentV1Controller(
    IContentV1Service service,
    ILogger<ContentV1Controller> logger,
    ICacheService cacheService) : ControllerBase
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    [HttpGet]
    [Route("Movies")]
    public async Task<ActionResult<IEnumerable<ItemsByCategory<GetMovieContentResponseDto>>>> Movies(
        string categories = "ação, terror",
        int? currentPage = 1,
        int? limit = 10)
    {
        logger.LogInformation("Request Content API /Movies");

        var cacheKey = $"moviesGroupByCategory-{NormalizeCsv(categories)}-{currentPage}-{limit}";

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var _dto = new GetGroupByCategoryRequestDto(
              [.. categories.Split(',').Select(x => x.Trim())],
              currentPage ?? 1,
              limit ?? 5);

            var response = await service.GetMoviesGroupByCategoryAsync(_dto);

            return response.IsFailure ? null : response.Data;
        });

        if (data is null) return BadRequest();

        return Ok(data);
    }

    [HttpGet]
    [Route("Movies/{category}")]
    public async Task<ActionResult<PagedList<GetMovieContentResponseDto>>> MoviesCategory(
        string category,
        int? currentPage,
        int? limit)
    {
        logger.LogInformation("Request Content API /Movies/{category}", category);

        var cacheKey = $"moviesByCategory-{category}-{currentPage}-{limit}";

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var response = await service.GetMoviesByCategoryAsync(new GetContentsRequestDto(category, currentPage, limit));

            return response.IsFailure ? null : response.Data;
        });

        if (data is null) return BadRequest();

        return Ok(data);
    }

    [HttpGet]
    [Route("Series")]
    public async Task<ActionResult<IEnumerable<ItemsByCategory<GetSeriesContentResponseDto>>>> Series(
        string categories = "ação, terror",
        int? currentPage = 1,
        int? limit = 10)
    {
        logger.LogInformation("Request Content API /Series");

        var cacheKey = $"seriesGroupByCategory-{NormalizeCsv(categories)}-{currentPage}-{limit}";

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var _dto = new GetGroupByCategoryRequestDto(
              [.. categories.Split(',').Select(x => x.Trim())],
              currentPage ?? 1,
              limit ?? 5);

            var response = await service.GetSeriesGroupByCategoryAsync(_dto);

            return response.IsFailure ? null : response.Data;
        });

        if (data is null) return BadRequest();

        return Ok(data);
    }

    [HttpGet]
    [Route("Series/{category}")]
    public async Task<ActionResult<IEnumerable<GetSeriesContentResponseDto>>> SeriesCategory(
        string category,
        int? currentPage,
        int? limit)
    {
        logger.LogInformation("Request Content API /Series/{category}", category);

        var cacheKey = $"seriesGroupByCategory-{category}-{currentPage}-{limit}";

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var response = await service.GetSeriesByCategoryAsync(new GetContentsRequestDto(category, currentPage, limit));

            return response.IsFailure ? null : response.Data;
        });

        if (data is null) return BadRequest();

        return Ok(data);
    }

    [HttpGet]
    [Route("Series/Episodes/{serieId}/{season}")]
    public async Task<ActionResult<IEnumerable<Episode>>> SeriesEpisodes(string serieId, int season)
    {
        logger.LogInformation("Request Content API /Series/Episodes/{serieId}/{season}", serieId, season);

        var cacheKey = $"episodesSeriesBySeason-{serieId}-{season}";

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var response = await service.GetEpisodesSeriesBySeasonAsync(serieId, season);

            return response.IsFailure ? null : response.Data;
        });

        if (data is null) return BadRequest();

        return Ok(data);
    }

    [HttpGet]
    [Route("Channels")]
    public async Task<ActionResult<IEnumerable<ItemsByCategory<GetChannelContentResponseDto>>>> Channels(
        string categories = "noticias, esporte",
        int? currentPage = 1,
        int? limit = 10)
    {
        logger.LogInformation("Request Content API /Channels");

        var cacheKey = $"channelsGroupByCategory-{NormalizeCsv(categories)}-{currentPage}-{limit}";

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var _dto = new GetGroupByCategoryRequestDto(
              [.. categories.Split(',').Select(x => x.Trim())],
              currentPage ?? 1,
              limit ?? 5);

            var response = await service.GetChannelsGroupByCategoryAsync(_dto);

            return response.IsFailure ? null : response.Data;
        });

        if (data is null) return BadRequest();

        return Ok(data);
    }

    [HttpGet]
    [Route("Search/{title}")]
    public async Task<ActionResult<GetContentsByNameResponseDto>> ContentsByTitle(
        string title,
        int? currentPage,
        int? limit)
    {
        logger.LogInformation("Request Content API /Search/{title}", title);

        var cacheKey = $"contentsByTitle-{title}-{currentPage}-{limit}";

        var data = await cacheService.GetOrCreateAsync<object>(cacheKey, CacheTtl, async () =>
        {
            var response = await service.GetContentsByTitleAsync(new(title, currentPage, limit));

            return response.IsFailure ? null : response.Data;
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