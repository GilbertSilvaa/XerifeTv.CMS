using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Common.Dtos;
using XerifeTv.CMS.Modules.Content.Dtos.Request;
using XerifeTv.CMS.Modules.Content.Dtos.Response;
using XerifeTv.CMS.Modules.Content.Interfaces;
using XerifeTv.CMS.Modules.Series;

namespace XerifeTv.CMS.Controllers;

[Route("Api/Content")]
[ApiController]
public class ContentController(
	IContentService _service, 
	ILogger<ContentController> _logger,
    ICacheService _cacheService) : ControllerBase
{
	[HttpGet]
	[Route("Movies")]
	public async Task<IActionResult> Movies(string categories = "", int? currentPage = 1, int? limit = 10)
	{
		_logger.LogInformation("Request Content API /Movies");

        var cacheKey = $"moviesGroupByCategory-{NormalizeCsv(categories)}-{currentPage}-{limit}";
        var responseCache = _cacheService.GetValue<IEnumerable<ItemsByCategory<GetMovieContentResponseDto>>>(cacheKey);

        if (responseCache != null) return Ok(responseCache);

        var _dto = new GetGroupByCategoryRequestDto(
		  [.. categories.Split(',').Select(x => x.Trim())],
		  currentPage ?? 1,
		  limit ?? 5);

		var response = await _service.GetMoviesGroupByCategoryAsync(_dto);

		if (response.IsFailure) return BadRequest();

        _cacheService.SetValue(cacheKey, response.Data);

        return Ok(response.Data);
	}

	[HttpGet]
	[Route("Movies/{category}")]
	public async Task<IActionResult> MoviesCategory(string category, int? currentPage, int? limit)
	{
		_logger.LogInformation("Request Content API /Movies/{category}", category);

        var cacheKey = $"moviesByCategory-{category}-{currentPage}-{limit}";
        var responseCache = _cacheService.GetValue<PagedList<GetMovieContentResponseDto>>(cacheKey);

		if (responseCache != null) return Ok(responseCache);

        var response = await _service.GetMoviesByCategoryAsync(new GetContentsRequestDto(category, currentPage, limit));

        if (response.IsFailure) return BadRequest();

        _cacheService.SetValue(cacheKey, response.Data);

        return Ok(response.Data);
	}

	[HttpGet]
	[Route("Series")]
	public async Task<IActionResult> Series(string categories = "", int? currentPage = 1, int? limit = 10)
	{
		_logger.LogInformation("Request Content API /Series");

        var cacheKey = $"seriesGroupByCategory-{NormalizeCsv(categories)}-{currentPage}-{limit}";
        var responseCache = _cacheService.GetValue<IEnumerable<ItemsByCategory<GetSeriesContentResponseDto>>>(cacheKey);

        if (responseCache != null) return Ok(responseCache);

        var _dto = new GetGroupByCategoryRequestDto(
		  [.. categories.Split(',').Select(x => x.Trim())],
		  currentPage ?? 1,
		  limit ?? 5);

		var response = await _service.GetSeriesGroupByCategoryAsync(_dto);

        if (response.IsFailure) return BadRequest();

        _cacheService.SetValue(cacheKey, response.Data);

        return Ok(response.Data);
	}

	[HttpGet]
	[Route("Series/{category}")]
	public async Task<IActionResult> SeriesCategory(string category, int? currentPage, int? limit)
	{
		_logger.LogInformation("Request Content API /Series/{category}", category);

        var cacheKey = $"seriesGroupByCategory-{category}-{currentPage}-{limit}";
        var responseCache = _cacheService.GetValue<IEnumerable<GetSeriesContentResponseDto>>(cacheKey);

        if (responseCache != null) return Ok(responseCache);

        var response = await _service.GetSeriesByCategoryAsync(new GetContentsRequestDto(category, currentPage, limit));

        if (response.IsFailure) return BadRequest();

        _cacheService.SetValue(cacheKey, response.Data);

        return Ok(response.Data);
	}

	[HttpGet]
	[Route("Series/Episodes/{serieId}/{season}")]
	public async Task<IActionResult> SeriesEpisodes(string serieId, int season)
	{
		_logger.LogInformation("Request Content API /Series/Episodes/{serieId}/{season}", serieId, season);

        var cacheKey = $"episodesSeriesBySeason-{serieId}-{season}";
        var responseCache = _cacheService.GetValue<IEnumerable<Episode>>(cacheKey);

        if (responseCache != null) return Ok(responseCache);

        var response = await _service.GetEpisodesSeriesBySeasonAsync(serieId, season);

        if (response.IsFailure) return BadRequest();

        _cacheService.SetValue(cacheKey, response.Data);

        return Ok(response.Data);
	}

	[HttpGet]
	[Route("Channels")]
	public async Task<IActionResult> Channels(string categories = "", int? currentPage = 1, int? limit = 10)
	{
		_logger.LogInformation("Request Content API /Channels");

        var cacheKey = $"channelsGroupByCategory-{NormalizeCsv(categories)}-{currentPage}-{limit}";
        var responseCache = _cacheService.GetValue<IEnumerable<ItemsByCategory<GetChannelContentResponseDto>>>(cacheKey);

        if (responseCache != null) return Ok(responseCache);

        var _dto = new GetGroupByCategoryRequestDto(
		  [.. categories.Split(',').Select(x => x.Trim())],
		  currentPage ?? 1,
		  limit ?? 5);

		var response = await _service.GetChannelsGroupByCategoryAsync(_dto);

        if (response.IsFailure) return BadRequest();

        _cacheService.SetValue(cacheKey, response.Data);

        return Ok(response.Data);
	}

	[HttpGet]
	[Route("Search/{title}")]
	public async Task<IActionResult> ContentsByTitle(string title, int? currentPage, int? limit)
	{
		_logger.LogInformation("Request Content API /Search/{title}", title);

        var cacheKey = $"contentsByTitle-{title}-{currentPage}-{limit}";
		var responseCache = _cacheService.GetValue<GetContentsByNameResponseDto>(cacheKey);

        if (responseCache != null) return Ok(responseCache);

        var response = await _service.GetContentsByTitleAsync(new(title, currentPage, limit));

        if (response.IsFailure) return BadRequest();

        _cacheService.SetValue(cacheKey, response.Data);

        return Ok(response.Data);
    }

    private static string NormalizeCsv(string csv)
        => string.Join(',',
            csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
               .Select(x => x.ToLowerInvariant())
               .OrderBy(x => x)
        );
}