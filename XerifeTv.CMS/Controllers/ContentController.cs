using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.Content.Interfaces;
using XerifeTv.CMS.Modules.Common.Dtos;
using XerifeTv.CMS.Modules.Content.Dtos.Request;

namespace XerifeTv.CMS.Controllers;

[Route("Api/Content")]
[ApiController]
public class ContentController(IContentService _service, ILogger<ContentController> _logger) : ControllerBase
{
	[HttpGet]
	[Route("Movies")]
	public async Task<IActionResult> Movies(string categories = "", int? currentPage = 1, int? limit = 10)
	{
		var _dto = new GetGroupByCategoryRequestDto(
		  [.. categories.Split(',').Select(x => x.Trim())],
		  currentPage ?? 1,
		  limit ?? 5);

		var response = await _service.GetMoviesGroupByCategoryAsync(_dto);
		_logger.LogInformation("Request Content API /Movies");

		return Ok(response.IsSuccess ? response.Data : []);
	}

	[HttpGet]
	[Route("Movies/{category}")]
	public async Task<IActionResult> MoviesCategory(string category, int? currentPage, int? limit)
	{
		var response = await _service.GetMoviesByCategoryAsync(new GetContentsRequestDto(category, currentPage, limit));
		_logger.LogInformation($"Request Content API /Movies/{category}");

		return Ok(response.IsSuccess ? response.Data : new object());
	}

	[HttpGet]
	[Route("Series")]
	public async Task<IActionResult> Series(string categories = "", int? currentPage = 1, int? limit = 10)
	{
		var _dto = new GetGroupByCategoryRequestDto(
		  [.. categories.Split(',').Select(x => x.Trim())],
		  currentPage ?? 1,
		  limit ?? 5);

		var response = await _service.GetSeriesGroupByCategoryAsync(_dto);
		_logger.LogInformation("Request Content API /Series");

		return Ok(response.IsSuccess ? response.Data : []);
	}

	[HttpGet]
	[Route("Series/{category}")]
	public async Task<IActionResult> SeriesCategory(string category, int? currentPage, int? limit)
	{
		var response = await _service.GetSeriesByCategoryAsync(new GetContentsRequestDto(category, currentPage, limit));
		_logger.LogInformation($"Request Content API /Series/{category}");

		return Ok(response.IsSuccess ? response.Data : []);
	}

	[HttpGet]
	[Route("Series/Episodes/{serieId}/{season}")]
	public async Task<IActionResult> SeriesEpisodes(string serieId, int season)
	{
		var response = await _service.GetEpisodesSeriesBySeasonAsync(serieId, season);
		_logger.LogInformation($"Request Content API /Series/Episodes/{serieId}/{season}");

		return Ok(response.IsSuccess ? response.Data : []);
	}

	[HttpGet]
	[Route("Channels")]
	public async Task<IActionResult> Channels(string categories = "", int? currentPage = 1, int? limit = 10)
	{
		var _dto = new GetGroupByCategoryRequestDto(
		  [.. categories.Split(',').Select(x => x.Trim())],
		  currentPage ?? 1,
		  limit ?? 5);

		var response = await _service.GetChannelsGroupByCategoryAsync(_dto);
		_logger.LogInformation("Request Content API /Channels");

		return Ok(response.IsSuccess ? response.Data : []);
	}

	[HttpGet]
	[Route("Search/{title}")]
	public async Task<IActionResult> ContentsByTitle(string title, int? currentPage, int? limit)
	{
		var response = await _service.GetContentsByTitleAsync(new GetContentsRequestDto(title, currentPage, limit));
		_logger.LogInformation($"Request Content API /Search/{title}");

		return Ok(response.IsSuccess ? response.Data : new object());
	}
}
