using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Models.Abstractions;
using XerifeTv.CMS.Models.Series.Dtos.Request;
using XerifeTv.CMS.Models.Series.Dtos.Response;
using XerifeTv.CMS.Models.Series.Enums;
using XerifeTv.CMS.Models.Series.Interfaces;

namespace XerifeTv.CMS.Controllers;

[Authorize]
public class SeriesController(ISeriesService _service, ILogger<SeriesController> _logger) : Controller
{
  private const int limitResultsPage = 15;

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
    await _service.Create(dto);

    _logger.LogInformation($"{User.Identity?.Name} registered the serie {dto.Title}");

    return RedirectToAction("Index");
  }

  [Authorize(Roles = "admin, common")]
  public async Task<IActionResult> Update(UpdateSeriesRequestDto dto)
  {
    await _service.Update(dto);

    _logger.LogInformation($"{User.Identity?.Name} updated the serie {dto.Title}");

    return RedirectToAction("Index");
  }

  [Authorize(Roles = "admin, common")]
  public async Task<IActionResult> Delete(string? id)
  {
    if (id is not null)
    {
      await _service.Delete(id);
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
    await _service.CreateEpisode(dto);

    _logger.LogInformation($"{User.Identity?.Name} registered episode {dto.Number} of season {dto.Season} of the serie with id = {dto.SerieId}");

    return RedirectToAction("Episodes", new { id = dto.SerieId });
  }

  [Authorize(Roles = "admin, common")]
  public async Task<IActionResult> UpdateEpisode(UpdateEpisodeRequestDto dto)
  {
    await _service.UpdateEpisode(dto);

    _logger.LogInformation($"{User.Identity?.Name} updated episode {dto.Number} of season {dto.Season} of the serie with id = {dto.SerieId}");

    return RedirectToAction("Episodes", new { id = dto.SerieId });
  }

  [Authorize(Roles = "admin, common")]
  public async Task<IActionResult> DeleteEpisode(string? serieId, string? id)
  {
    if (serieId is not null && id is not null)
    {
      await _service.DeleteEpisode(serieId, id);
      _logger.LogInformation($"{User.Identity?.Name} deleted episode with id = {id} of the serie with id = {serieId}");
    }
      
    return RedirectToAction("Episodes", new { id = serieId });
  }
}
