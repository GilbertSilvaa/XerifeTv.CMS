using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Franchise.Dtos.Response;
using XerifeTv.CMS.Modules.Franchise.Interfaces;
using XerifeTv.CMS.Modules.Integrations.Imdb.Services;
using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Response;
using XerifeTv.CMS.Modules.Media.Delivery.Intefaces;
using XerifeTv.CMS.Modules.Series.Dtos.Request;
using XerifeTv.CMS.Modules.Series.Dtos.Response;
using XerifeTv.CMS.Modules.Series.Enums;
using XerifeTv.CMS.Modules.Series.Interfaces;
using XerifeTv.CMS.Shared.Helpers;
using XerifeTv.CMS.Views.Series.Models;

namespace XerifeTv.CMS.Controllers;

[Authorize]
public class SeriesController(
  ISeriesService service,
  IImdbService imdbService,
  ILogger<SeriesController> logger,
  IEpisodesImporter episodesImporter,
  ISpreadsheetBatchImporter<ISeriesService> spreadsheetBatchImporter,
  IMediaDeliveryProfileService mediaDeliveryProfileService,
  IFranchiseService franchiseService) : Controller
{
	private const int limitResultsPage = 20;

	public async Task<IActionResult> Index(int? currentPage, ESeriesSearchFilter? filter, string? search)
	{
		Result<PagedList<GetSeriesResponseDto>> result;

		logger.LogInformation($"{User.Identity?.Name} accessed the series page");

		if (filter is ESeriesSearchFilter && !string.IsNullOrEmpty(search))
		{
			result = await service.GetByFilterAsync(
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
			result = await service.GetAsync(currentPage ?? 1, limitResultsPage);
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
        IEnumerable<GetFranchiseResponseDto> franchises = [];
        string? selectedFranchiseName = null;

		if (id is not null)
		{
			var response = await service.GetAsync(id);
			if (response.IsSuccess)
            {
                if (!string.IsNullOrWhiteSpace(response.Data?.FranchiseId))
                {
                    var franchiseResponse = await franchiseService.GetAsync(response.Data.FranchiseId);
                    if (franchiseResponse.IsSuccess && franchiseResponse.Data is not null)
                    {
                        selectedFranchiseName = franchiseResponse.Data.Name;
                        franchises = [franchiseResponse.Data];
                    }
                }

                return View(new SeriesFormModelView(response.Data, franchises, selectedFranchiseName));
            }
		}

		return View(new SeriesFormModelView(null, franchises, selectedFranchiseName));
	}

	[Authorize(Roles = "admin, common")]
	public async Task<IActionResult> Create(CreateSeriesRequestDto dto)
	{
		var response = await service.CreateAsync(dto);

		TempData["Notification"] = response.IsFailure
		  ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
		  : MessageViewHelper.SuccessJson($"Série {dto.ImdbId} cadastrada com sucesso");

		logger.LogInformation($"{User.Identity?.Name} registered the serie {dto.Title}");

		return RedirectToAction("Index");
	}

	[Authorize(Roles = "admin, common")]
	public async Task<IActionResult> Update(UpdateSeriesRequestDto dto)
	{
		var response = await service.UpdateAsync(dto);

		TempData["Notification"] = response.IsFailure
		  ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
		  : MessageViewHelper.SuccessJson($"Série {dto.ImdbId} atualizada com sucesso");

		logger.LogInformation($"{User.Identity?.Name} updated the serie {dto.Title}");

		return RedirectToAction("Index");
	}

	[Authorize(Roles = "admin, common")]
	public async Task<IActionResult> Delete(string? id)
	{
		if (id is not null)
		{
			var response = await service.DeleteAsync(id);

			TempData["Notification"] = response.IsFailure
			  ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
			  : MessageViewHelper.SuccessJson($"Série deletada com sucesso");

			logger.LogInformation($"{User.Identity?.Name} removed the serie with id = {id}");
		}

		return RedirectToAction("Index");
	}

	public async Task<IActionResult> Episodes(string? id, int? seasonFilter)
	{
		if (id is null) return RedirectToAction("Index");

		ViewBag.SerieId = id;
		ViewBag.SeasonFilter = seasonFilter;

		var response = await service.GetEpisodesBySeasonAsync(id, seasonFilter ?? 1, includeDisabled: true);

		if (response.IsSuccess)
		{
			ViewBag.NumberSeasons = response.Data?.NumberSeasons;
			logger.LogInformation($"{User.Identity?.Name} accessed the series episodes with id = {id}");

            IEnumerable<GetMediaDeliveryProfileResponseDto> mediaDeliveryProfiles = [];
            var mediaProfilesResponse = await mediaDeliveryProfileService.GetAllAsync(isIncludeDisabled: false);
            if (mediaProfilesResponse.IsSuccess) mediaDeliveryProfiles = mediaProfilesResponse.Data ?? [];

            return View(new EpisodesModelView(response.Data, mediaDeliveryProfiles));
		}

		return RedirectToAction("Index");
	}

	[Authorize(Roles = "admin, common")]
	public async Task<IActionResult> CreateEpisode(CreateEpisodeRequestDto dto)
	{
		var response = await service.CreateEpisodeAsync(dto);

		TempData["Notification"] = response.IsFailure
		  ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
		  : MessageViewHelper.SuccessJson($"Episódio T{dto.Season}:EP{dto.Number} cadastrado com sucesso");

		logger.LogInformation($"{User.Identity?.Name} registered episode {dto.Number} of season {dto.Season} of the serie with id = {dto.SerieId}");

		return RedirectToAction("Episodes", new { id = dto.SerieId, seasonFilter = dto.Season });
	}

	[Authorize(Roles = "admin, common")]
	public async Task<IActionResult> UpdateEpisode(UpdateEpisodeRequestDto dto)
	{
		var response = await service.UpdateEpisodeAsync(dto);

		TempData["Notification"] = response.IsFailure
		  ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
		  : MessageViewHelper.SuccessJson($"Episódio T{dto.Season}:EP{dto.Number} atualizado com sucesso");

		logger.LogInformation($"{User.Identity?.Name} updated episode {dto.Number} of season {dto.Season} of the serie with id = {dto.SerieId}");

		return RedirectToAction("Episodes", new { id = dto.SerieId, seasonFilter = dto.Season });
	}

	[Authorize(Roles = "admin, common")]
	public async Task<IActionResult> DeleteEpisode(string? serieId, string? id)
	{
		if (serieId is not null && id is not null)
		{
			var response = await service.DeleteEpisodeAsync(serieId, id);

			TempData["Notification"] = response.IsFailure
			  ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
			  : MessageViewHelper.SuccessJson($"Episódio deletado com sucesso");

			logger.LogInformation($"{User.Identity?.Name} deleted episode with id = {id} of the serie with id = {serieId}");
		}

		return RedirectToAction("Episodes", new { id = serieId });
	}

	[HttpGet]
	public async Task<IActionResult> GetSeriesByImdbId(string imdbId)
	{
		if (string.IsNullOrEmpty(imdbId)) return BadRequest();

		var response = await imdbService.GetSeriesByImdbIdAsync(imdbId);

		if (response.IsFailure) return BadRequest(response.Error.Description);

		return Ok(response.Data);
	}

	[Authorize(Roles = "admin, common")]
	[HttpPost]
	public async Task<IActionResult> RegisterBySpreadsheet(IFormFile file)
	{
		if (file is null || file.Length == 0) return BadRequest();

		var response = await spreadsheetBatchImporter.ImportAsync(file);

		if (response.IsFailure)
			return BadRequest(response.Error.Description ?? string.Empty);

		return Ok(response.Data);
	}

	[Authorize(Roles = "admin, common")]
	[HttpGet]
	public async Task<IActionResult> MonitorSpreadsheetRegistration(string importId)
	{
		var response = await spreadsheetBatchImporter.MonitorImportAsync(importId);

		if (response.IsSuccess && response.Data?.ProgressCount == 100 && response.Data.SuccessCount > 1)
			TempData["Notification"] = MessageViewHelper
			  .SuccessJson($"{response.Data.SuccessCount} séries/episódios cadastrados/atualizados com sucesso");

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
			TempData["Notification"] = MessageViewHelper.ErrorJson("Ops! Houve um problema [série inválida]");
			return BadRequest();
		}

		var response = await episodesImporter.ImportAsync(dto.SeriesId);

		if (response.IsFailure)
			return BadRequest(response.Error.Description ?? string.Empty);

		return Ok(response.Data);
	}

	[Authorize(Roles = "admin, common")]
	[HttpGet]
	public async Task<IActionResult> MonitorImdbEpisodesImport(string importId)
	{
		var response = await episodesImporter.MonitorImportAsync(importId);

		if (response.IsSuccess && response.Data?.ProgressCount == 100 && response.Data.ImportedCount > 1)
			TempData["Notification"] = MessageViewHelper
			  .SuccessJson($"{response.Data.ImportedCount} episódios importados com sucesso");

		if (response.IsSuccess)
			return Ok(response.Data);

		return BadRequest(response.Error.Description ?? string.Empty);
	}
}

