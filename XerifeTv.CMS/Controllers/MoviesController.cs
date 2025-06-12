using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Movie.Enums;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Modules.Movie.Dtos.Request;
using XerifeTv.CMS.Modules.Movie.Dtos.Response;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Integrations.Imdb.Services;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Controllers;

[Authorize]
public class MoviesController(
  IMovieService _service,
  IImdbService _imdbService,
  ILogger<MoviesController> _logger,
  ISpreadsheetBatchImporter<IMovieService> _spreadsheetBatchImporter) : Controller
{
	private const int limitResultsPage = 20;

	public async Task<IActionResult> Index(int? currentPage, EMovieSearchFilter? filter, string? search)
	{
		Result<PagedList<GetMovieResponseDto>>? result;

		_logger.LogInformation($"{User.Identity?.Name} accessed the movies page");

		if (filter is EMovieSearchFilter && !string.IsNullOrEmpty(search))
		{
			result = await _service.GetByFilter(
			  new GetMoviesByFilterRequestDto(
				filter,
				EMovieOrderFilter.TITLE,
				search,
				limitResultsPage,
				currentPage,
				isIncludeDisabled: true));

			ViewBag.Search = search;
			ViewBag.Filter = filter.ToString()?.ToLower();
		}
		else
		{
			result = await _service.Get(currentPage ?? 1, limitResultsPage);
		}

		if (result.IsSuccess)
		{
			ViewBag.CurrentPage = result.Data?.CurrentPage;
			ViewBag.TotalPages = result.Data?.TotalPageCount ?? 1;
			ViewBag.HasNextPage = result.Data?.HasNext;
			ViewBag.HasPrevPage = result.Data?.HasPrevious;

			return View(result.Data?.Items);
		}

		return View(Enumerable.Empty<GetMovieResponseDto>());
	}

	[Authorize(Roles = "admin, common")]
	public async Task<IActionResult> Form(string? id)
	{
		if (id is not null)
		{
			var response = await _service.Get(id);
			if (response.IsSuccess) return View(response.Data);
		}

		return View();
	}

	[Authorize(Roles = "admin, common")]
	public async Task<IActionResult> Create(CreateMovieRequestDto dto)
	{
		var response = await _service.Create(dto);

		TempData["Notification"] = response.IsFailure
		  ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
		  : MessageViewHelper.SuccessJson($"Filme {dto.ImdbId} cadastrado com sucesso");

		_logger.LogInformation($"{User.Identity?.Name} registered the movie {dto.Title}");

		return RedirectToAction("Index");
	}

	[Authorize(Roles = "admin, common")]
	public async Task<IActionResult> Update(UpdateMovieRequestDto dto)
	{
		var response = await _service.Update(dto);

		TempData["Notification"] = response.IsFailure
		  ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
		  : MessageViewHelper.SuccessJson($"Filme {dto.ImdbId} atualizado com sucesso");

		_logger.LogInformation($"{User.Identity?.Name} updated the movie {dto.Title}");

		return RedirectToAction("Index");
	}

	[Authorize(Roles = "admin, common")]
	public async Task<IActionResult> Delete(string? id)
	{
		if (id is not null)
		{
			var response = await _service.Delete(id);

			TempData["Notification"] = response.IsFailure
			  ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
			  : MessageViewHelper.SuccessJson($"Filme deletado com sucesso");

			_logger.LogInformation($"{User.Identity?.Name} removed the movie with id = {id}");
		}

		return RedirectToAction("Index");
	}

	[HttpGet]
	public async Task<IActionResult> GetByImdbId(string imdbId)
	{
		if (string.IsNullOrEmpty(imdbId)) return BadRequest();

		var response = await _imdbService.GetMovieByImdbIdAsync(imdbId);

		return response.IsFailure ? BadRequest() : Ok(response.Data);
	}

	[Authorize(Roles = "admin, common")]
	[HttpPost]
	public async Task<IActionResult> RegisterBySpreadsheet(IFormFile file)
	{
		if (file is null || file.Length == 0) return BadRequest();

		var response = await _spreadsheetBatchImporter.ImportAsync(file);

		if (response.IsFailure)
			return BadRequest(response.Error.Description ?? string.Empty);

		return Ok(response.Data);
	}

	[Authorize(Roles = "admin, common")]
	[HttpGet]
	public async Task<IActionResult> MonitorSpreadsheetRegistration(string importId)
	{
		var response = await _spreadsheetBatchImporter.MonitorImportAsync(importId);

		if (response.IsSuccess && response.Data?.ProgressCount == 100 && response.Data.SuccessCount > 1)
			TempData["Notification"] = MessageViewHelper
			  .SuccessJson($"{response.Data.SuccessCount} filmes cadastrados com sucesso");

		if (response.IsSuccess)
			return Ok(response.Data);

		return BadRequest(response.Error.Description ?? string.Empty);
	}
}
