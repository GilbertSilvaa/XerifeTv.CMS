using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Movie.Enums;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Modules.Movie.Dtos.Request;
using XerifeTv.CMS.Modules.Movie.Dtos.Response;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Franchise.Dtos.Response;
using XerifeTv.CMS.Modules.Franchise.Interfaces;
using XerifeTv.CMS.Modules.Integrations.Imdb.Services;
using XerifeTv.CMS.Shared.Helpers;
using XerifeTv.CMS.Views.Movies.Models;
using XerifeTv.CMS.Modules.Media.Delivery.Intefaces;
using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Response;
using XerifeTv.CMS.Modules.AuditLog.Interfaces;
using XerifeTv.CMS.Shared.Extensions;

namespace XerifeTv.CMS.Controllers;

[Authorize]
public class MoviesController(
  IMovieService service,
  IImdbService imdbService,
  ILogger<MoviesController> logger,
  ISpreadsheetBatchImporter<IMovieService> spreadsheetBatchImporter,
  IMediaDeliveryProfileService mediaDeliveryProfileService,
  IFranchiseService franchiseService,
  IAuditLogService auditLogService) : Controller
{
    private const int limitResultsPage = 20;

    public async Task<IActionResult> Index(int? currentPage, EMovieSearchFilter? filter, string? search)
    {
        Result<PagedList<GetMovieResponseDto>>? result;

        logger.LogInformation($"{User.Identity?.Name} accessed the movies page");

        if (filter is EMovieSearchFilter && !string.IsNullOrEmpty(search))
        {
            result = await service.GetByFilterAsync(
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

        return View(Enumerable.Empty<GetMovieResponseDto>());
    }

    [Authorize(Roles = "admin, common")]
    public async Task<IActionResult> Form(string? id)
    {
        IEnumerable<GetMediaDeliveryProfileResponseDto> mediaDeliveryProfiles = [];
        IEnumerable<GetFranchiseResponseDto> franchises = [];
        string? selectedFranchiseName = null;

        var mediaProfilesResponse = await mediaDeliveryProfileService.GetAllAsync(isIncludeDisabled: false);
        if (mediaProfilesResponse.IsSuccess) mediaDeliveryProfiles = mediaProfilesResponse.Data ?? [];

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

                return View(new MovieFormModelView(response.Data, mediaDeliveryProfiles, franchises, selectedFranchiseName));
            }
        }

        return View(new MovieFormModelView(null, mediaDeliveryProfiles, franchises, selectedFranchiseName));
    }

    [Authorize(Roles = "admin, common")]
    public async Task<IActionResult> Create(CreateMovieRequestDto dto)
    {
        var response = await service.CreateAsync(dto);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson($"Filme {dto.ImdbId} cadastrado com sucesso");

        logger.LogInformation($"{User.Identity?.Name} registered the movie {dto.Title}");

        if (response.IsSuccess)
            await this.AddAuditLogAsync(auditLogService, "Movie", response.Data ?? string.Empty, $"adicionou o filme {dto.Title}");

        return RedirectToAction("Index");
    }

    [Authorize(Roles = "admin, common")]
    public async Task<IActionResult> Update(UpdateMovieRequestDto dto)
    {
        var response = await service.UpdateAsync(dto);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson($"Filme {dto.ImdbId} atualizado com sucesso");

        logger.LogInformation($"{User.Identity?.Name} updated the movie {dto.Title}");

        if (response.IsSuccess)
            await this.AddAuditLogAsync(auditLogService, "Movie", dto.Id, $"atualizou o filme {dto.Title}");

        return RedirectToAction("Index");
    }

    [Authorize(Roles = "admin, common")]
    public async Task<IActionResult> Delete(string? id)
    {
        if (id is not null)
        {
            var movieResponse = await service.GetAsync(id);
            var title = movieResponse.IsSuccess && movieResponse.Data is not null
                ? movieResponse.Data.Title
                : id;

            var response = await service.DeleteAsync(id);

            TempData["Notification"] = response.IsFailure
              ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
              : MessageViewHelper.SuccessJson($"Filme deletado com sucesso");

            logger.LogInformation($"{User.Identity?.Name} removed the movie with id = {id}");

            if (response.IsSuccess)
                await this.AddAuditLogAsync(auditLogService, "Movie", id, $"removeu o filme {title}");
        }

        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> GetByImdbId(string imdbId)
    {
        if (string.IsNullOrEmpty(imdbId)) return BadRequest();

        var response = await imdbService.GetMovieByImdbIdAsync(imdbId);

        return response.IsFailure ? BadRequest() : Ok(response.Data);
    }

    [Authorize(Roles = "admin, common")]
    [HttpPost]
    public async Task<IActionResult> RegisterBySpreadsheet(IFormFile file)
    {
        if (file is null || file.Length == 0) return BadRequest();

        var response = await spreadsheetBatchImporter.ImportAsync(file);

        if (response.IsFailure)
            return BadRequest(response.Error.Description ?? string.Empty);

        await this.AddAuditLogAsync(auditLogService, "Movie", file.FileName, $"iniciou a importação de filmes da planilha {file.FileName}");

        return Ok(response.Data);
    }

    [Authorize(Roles = "admin, common")]
    [HttpGet]
    public async Task<IActionResult> MonitorSpreadsheetRegistration(string importId)
    {
        var response = await spreadsheetBatchImporter.MonitorImportAsync(importId);

        if (response.IsSuccess && response.Data?.ProgressCount == 100 && response.Data.SuccessCount > 1)
            TempData["Notification"] = MessageViewHelper
              .SuccessJson($"{response.Data.SuccessCount} filmes cadastrados/atualizados com sucesso");

        if (response.IsSuccess)
            return Ok(response.Data);

        return BadRequest(response.Error.Description ?? string.Empty);
    }
}

