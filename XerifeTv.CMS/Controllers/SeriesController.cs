using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Series.Dtos.Request;
using XerifeTv.CMS.Modules.Series.Dtos.Response;
using XerifeTv.CMS.Modules.Series.Enums;
using XerifeTv.CMS.Modules.Series.Interfaces;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Controllers;

[Authorize]
public class SeriesController(ISeriesService _service, ILogger<SeriesController> _logger) : Controller
{
  private const int limitResultsPage = 20;

  public async Task<IActionResult> Index(int? currentPage, ESeriesSearchFilter? filter, string? search)
  {
    Result<PagedList<GetSeriesResponseDto>> result;

    _logger.LogInformation($"{User.Identity?.Name} accessed the series page");

    if (filter is ESeriesSearchFilter && !string.IsNullOrEmpty(search))
    {
      result = await _service.GetByFilter(
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

    return View(Enumerable.Empty<GetSeriesResponseDto>());
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
  public async Task<IActionResult> Create(CreateSeriesRequestDto dto)
  {
     var response = await _service.Create(dto);
     
     TempData["Notification"] = response.IsFailure
       ? MessageViewHelper.ErrorJson(response.Error.Description)
       : MessageViewHelper.SuccessJson($"Serie {dto.ImdbId} cadastrada com sucesso");

    _logger.LogInformation($"{User.Identity?.Name} registered the serie {dto.Title}");

    return RedirectToAction("Index");
  }

  [Authorize(Roles = "admin, common")]
  public async Task<IActionResult> Update(UpdateSeriesRequestDto dto)
  {
    var response = await _service.Update(dto);
    
    TempData["Notification"] = response.IsFailure
      ? MessageViewHelper.ErrorJson(response.Error.Description)
      : MessageViewHelper.SuccessJson($"Serie {dto.ImdbId} atualizada com sucesso");

    _logger.LogInformation($"{User.Identity?.Name} updated the serie {dto.Title}");

    return RedirectToAction("Index");
  }

  [Authorize(Roles = "admin, common")]
  public async Task<IActionResult> Delete(string? id)
  {
    if (id is not null)
    {
      var response = await _service.Delete(id);
      
      TempData["Notification"] = response.IsFailure
        ? MessageViewHelper.ErrorJson(response.Error.Description)
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

    var response = await _service.GetEpisodesBySeason(id, seasonFilter ?? 1, includeDisabled: true);

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
    var response = await _service.CreateEpisode(dto);
    
    TempData["Notification"] = response.IsFailure
      ? MessageViewHelper.ErrorJson(response.Error.Description)
      : MessageViewHelper.SuccessJson($"Episodio cadastrado com sucesso");

    _logger.LogInformation($"{User.Identity?.Name} registered episode {dto.Number} of season {dto.Season} of the serie with id = {dto.SerieId}");

    return RedirectToAction("Episodes", new { id = dto.SerieId });
  }

  [Authorize(Roles = "admin, common")]
  public async Task<IActionResult> UpdateEpisode(UpdateEpisodeRequestDto dto)
  {
    var response = await _service.UpdateEpisode(dto);
    
    TempData["Notification"] = response.IsFailure
      ? MessageViewHelper.ErrorJson(response.Error.Description)
      : MessageViewHelper.SuccessJson($"Episodio atualizado com sucesso");

    _logger.LogInformation($"{User.Identity?.Name} updated episode {dto.Number} of season {dto.Season} of the serie with id = {dto.SerieId}");

    return RedirectToAction("Episodes", new { id = dto.SerieId });
  }

  [Authorize(Roles = "admin, common")]
  public async Task<IActionResult> DeleteEpisode(string? serieId, string? id)
  {
    if (serieId is not null && id is not null)
    {
      var response = await _service.DeleteEpisode(serieId, id);
      
      TempData["Notification"] = response.IsFailure
        ? MessageViewHelper.ErrorJson(response.Error.Description)
        : MessageViewHelper.SuccessJson($"Episodio deletado com sucesso");
      
      _logger.LogInformation($"{User.Identity?.Name} deleted episode with id = {id} of the serie with id = {serieId}");
    }
      
    return RedirectToAction("Episodes", new { id = serieId });
  }
}
