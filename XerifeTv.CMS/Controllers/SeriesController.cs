﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Integrations.Imdb.Services;
using XerifeTv.CMS.Modules.Series.Dtos.Request;
using XerifeTv.CMS.Modules.Series.Dtos.Response;
using XerifeTv.CMS.Modules.Series.Enums;
using XerifeTv.CMS.Modules.Series.Interfaces;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Controllers;

[Authorize]
public class SeriesController(
  ISeriesService _service,
  IImdbService _imdbService,
  ILogger<SeriesController> _logger,
  IEpisodesImporter _episodesImporter,
  ISpreadsheetBatchImporter<ISeriesService> _spreadsheetBatchImporter) : Controller
{
	private const int limitResultsPage = 20;

	public async Task<IActionResult> Index(int? currentPage, ESeriesSearchFilter? filter, string? search)
	{
		Result<PagedList<GetSeriesResponseDto>> result;

		_logger.LogInformation($"{User.Identity?.Name} accessed the series page");

		if (filter is ESeriesSearchFilter && !string.IsNullOrEmpty(search))
		{
			result = await _service.GetByFilterAsync(
			  new GetSeriesByFilterRequestDto(
				filter,
				search,
				limitResultsPage,
				currentPage,
				isIncludeDisabled: true));

			ViewBag.Search = search;
			ViewBag.Filter = filter.ToString()?.ToLower();
		}
		else
		{
			result = await _service.GetAsync(currentPage ?? 1, limitResultsPage);
		}

		if (result.IsSuccess)
		{
			ViewBag.CurrentPage = result.Data?.CurrentPage;
			ViewBag.TotalPages = result.Data?.TotalPageCount ?? 1;
			ViewBag.HasNextPage = result.Data?.HasNext;
			ViewBag.HasPrevPage = result.Data?.HasPrevious;

			return View(result.Data?.Items);
		}

		return View(Enumerable.Empty<GetSeriesResponseDto>());
	}

	[Authorize(Roles = "admin, common")]
	public async Task<IActionResult> Form(string? id)
	{
		if (id is not null)
		{
			var response = await _service.GetAsync(id);
			if (response.IsSuccess) return View(response.Data);
		}

		return View();
	}

	[Authorize(Roles = "admin, common")]
	public async Task<IActionResult> Create(CreateSeriesRequestDto dto)
	{
		var response = await _service.CreateAsync(dto);

		TempData["Notification"] = response.IsFailure
		  ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
		  : MessageViewHelper.SuccessJson($"Serie {dto.ImdbId} cadastrada com sucesso");

		_logger.LogInformation($"{User.Identity?.Name} registered the serie {dto.Title}");

		return RedirectToAction("Index");
	}

	[Authorize(Roles = "admin, common")]
	public async Task<IActionResult> Update(UpdateSeriesRequestDto dto)
	{
		var response = await _service.UpdateAsync(dto);

		TempData["Notification"] = response.IsFailure
		  ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
		  : MessageViewHelper.SuccessJson($"Serie {dto.ImdbId} atualizada com sucesso");

		_logger.LogInformation($"{User.Identity?.Name} updated the serie {dto.Title}");

		return RedirectToAction("Index");
	}

	[Authorize(Roles = "admin, common")]
	public async Task<IActionResult> Delete(string? id)
	{
		if (id is not null)
		{
			var response = await _service.DeleteAsync(id);

			TempData["Notification"] = response.IsFailure
			  ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
			  : MessageViewHelper.SuccessJson($"Serie deletada com sucesso");

			_logger.LogInformation($"{User.Identity?.Name} removed the serie with id = {id}");
		}

		return RedirectToAction("Index");
	}

	public async Task<IActionResult> Episodes(string? id, int? seasonFilter)
	{
		if (id is null) return RedirectToAction("Index");

		ViewBag.SerieId = id;
		ViewBag.SeasonFilter = seasonFilter;

		var response = await _service.GetEpisodesBySeasonAsync(id, seasonFilter ?? 1, includeDisabled: true);

		if (response.IsSuccess)
		{
			ViewBag.NumberSeasons = response.Data?.NumberSeasons;
			_logger.LogInformation($"{User.Identity?.Name} accessed the series episodes with id = {id}");

			return View(response.Data);
		}

		return RedirectToAction("Index");
	}

	[Authorize(Roles = "admin, common")]
	public async Task<IActionResult> CreateEpisode(CreateEpisodeRequestDto dto)
	{
		var response = await _service.CreateEpisodeAsync(dto);

		TempData["Notification"] = response.IsFailure
		  ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
		  : MessageViewHelper.SuccessJson($"Episodio T{dto.Season}:EP{dto.Number} cadastrado com sucesso");

		_logger.LogInformation($"{User.Identity?.Name} registered episode {dto.Number} of season {dto.Season} of the serie with id = {dto.SerieId}");

		return RedirectToAction("Episodes", new { id = dto.SerieId });
	}

	[Authorize(Roles = "admin, common")]
	public async Task<IActionResult> UpdateEpisode(UpdateEpisodeRequestDto dto)
	{
		var response = await _service.UpdateEpisodeAsync(dto);

		TempData["Notification"] = response.IsFailure
		  ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
		  : MessageViewHelper.SuccessJson($"Episodio T{dto.Season}:EP{dto.Number} atualizado com sucesso");

		_logger.LogInformation($"{User.Identity?.Name} updated episode {dto.Number} of season {dto.Season} of the serie with id = {dto.SerieId}");

		return RedirectToAction("Episodes", new { id = dto.SerieId });
	}

	[Authorize(Roles = "admin, common")]
	public async Task<IActionResult> DeleteEpisode(string? serieId, string? id)
	{
		if (serieId is not null && id is not null)
		{
			var response = await _service.DeleteEpisodeAsync(serieId, id);

			TempData["Notification"] = response.IsFailure
			  ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
			  : MessageViewHelper.SuccessJson($"Episodio deletado com sucesso");

			_logger.LogInformation($"{User.Identity?.Name} deleted episode with id = {id} of the serie with id = {serieId}");
		}

		return RedirectToAction("Episodes", new { id = serieId });
	}

	[HttpGet]
	public async Task<IActionResult> GetSeriesByImdbId(string imdbId)
	{
		if (string.IsNullOrEmpty(imdbId)) return BadRequest();

		var response = await _imdbService.GetSeriesByImdbIdAsync(imdbId);

		if (response.IsFailure) return BadRequest(response.Error.Description);

		return Ok(response.Data);
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
			  .SuccessJson($"{response.Data.SuccessCount} series cadastradas com sucesso");

		if (response.IsSuccess)
			return Ok(response.Data);

		return BadRequest(response.Error.Description ?? string.Empty);
	}

	[Authorize(Roles = "admin, common")]
	[HttpPost]
	public async Task<IActionResult> ImportEpisodesByImdbId(ImportEpisodesRequestDto dto)
	{
		if (string.IsNullOrEmpty(dto.SeriesId))
		{
			TempData["Notification"] = MessageViewHelper.ErrorJson("Ops! Houve um problema [serie invalida]");
			return BadRequest();
		}

		var response = await _episodesImporter.ImportAsync(dto.SeriesId);

		if (response.IsFailure)
			return BadRequest(response.Error.Description ?? string.Empty);

		return Ok(response.Data);
	}

	[Authorize(Roles = "admin, common")]
	[HttpGet]
	public async Task<IActionResult> MonitorImdbEpisodesImport(string importId)
	{
		var response = await _episodesImporter.MonitorImportAsync(importId);

		if (response.IsSuccess && response.Data?.ProgressCount == 100 && response.Data.ImportedCount > 1)
			TempData["Notification"] = MessageViewHelper
			  .SuccessJson($"{response.Data.ImportedCount} episodios importados com sucesso");

		if (response.IsSuccess)
			return Ok(response.Data);

		return BadRequest(response.Error.Description ?? string.Empty);
	}
}